using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Mecha;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// Reglas de balance de la economía (T-150, D-tony-86). Leen los assets reales: si alguien mueve un precio en el inspector
    /// y rompe una regla, el mensaje dice qué número rompe qué regla.
    /// </summary>
    public class BalanceEconomiaPruebas
    {
        private const string Carpeta = "Assets/_Proyecto/Economia/Datos/";

        private static T Cargar<T>(string ruta) where T : Object
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(ruta);
            Assert.IsNotNull(a, ruta);
            return a;
        }

        private static List<T> Todos<T>() where T : Object
        {
            return AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets/_Proyecto" })
                .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null).ToList();
        }

        private static DatosEconomia Economia => Cargar<DatosEconomia>(Carpeta + "DatosEconomia_Base.asset");
        private static DatosPremioPelea PremioCasera => Cargar<DatosPremioPelea>(Carpeta + "DatosPremioPelea_Casera.asset");
        private static DatosMecha MechaJugador => Cargar<DatosMecha>("Assets/_Proyecto/Mecha/Datos/DatosMecha_Jugador.asset");
        private static ConfigKitReparacion ConfigKit => Cargar<ConfigKitReparacion>("Assets/_Proyecto/Mecha/Datos/ConfigKitReparacion_Base.asset");
        private static DatosObjeto Kit => Cargar<DatosObjeto>(Carpeta + "DatosObjeto_KitReparacion.asset");

        /// <summary>Precio de compra de un objeto: su oferta enlazada o, si no la tiene, la oferta con el mismo nombre.</summary>
        private static int? PrecioDeCompra(DatosObjeto objeto, IEnumerable<DatosOferta> ofertas)
        {
            if (objeto.Oferta != null) return objeto.Oferta.Precio;
            var igual = ofertas.FirstOrDefault(o => o.Nombre == objeto.Nombre);
            return igual != null ? igual.Precio : (int?)null;
        }

        /// <summary>Mensajes de los objetos que se venden por su precio de compra o más.</summary>
        private static List<string> ObjetosQueSeVendenCaro(IEnumerable<DatosObjeto> objetos, IEnumerable<DatosOferta> ofertas)
        {
            var mensajes = new List<string>();
            foreach (DatosObjeto o in objetos)
            {
                int? compra = PrecioDeCompra(o, ofertas);
                if (compra == null || o.ValorVenta < compra.Value) continue;
                mensajes.Add($"REGLA d: '{o.Nombre}' ({o.name}) se vende por ${o.ValorVenta} y se compra por ${compra.Value}: " +
                    "nada puede venderse por su precio de compra o más.");
            }
            return mensajes;
        }

        [Test]
        public void ReglaA_PremioDeLaCasera_AlcanzaParaUnaRenta()
        {
            Assert.GreaterOrEqual(PremioCasera.PremioGanar, Economia.Renta,
                $"REGLA a: el premio de la casera (${PremioCasera.PremioGanar}) es menor que la renta (${Economia.Renta}).");
        }

        [Test]
        public void ReglaB_ReparacionDeKO_CuestaMasQueElPremioDeLaCasera()
        {
            DatosEconomia e = Economia;
            int costo = ReglaReparacion.Costo(MechaJugador.VidaMaxima, e.CostoReparacionPorPunto, e.CostoReparacionMinimo);
            Assert.Greater(costo, PremioCasera.PremioGanar,
                $"REGLA b: reparar de KO a vida completa cuesta ${costo} ({MechaJugador.VidaMaxima} puntos x ${e.CostoReparacionPorPunto}) " +
                $"y el premio de la casera es ${PremioCasera.PremioGanar}: perder debe doler.");
        }

        [Test]
        public void ReglaC_KitCuestaMasPorPuntoQueElTaller()
        {
            float puntosPorKit = ConfigKit.FraccionCuracion * MechaJugador.VidaMaxima;
            Assert.Greater(puntosPorKit, 0f, "REGLA c: el kit no cura nada.");
            float precio = Kit.Oferta.Precio;
            float porPuntoKit = precio / puntosPorKit;
            int porPuntoTaller = Economia.CostoReparacionPorPunto;
            Assert.Greater(porPuntoKit, porPuntoTaller,
                $"REGLA c: el kit cuesta ${porPuntoKit:0.##} por punto (${precio} / {puntosPorKit} puntos) y el taller ${porPuntoTaller}: " +
                "el kit debe ser más caro que el taller.");
        }

        [Test]
        public void ReglaD_NingunObjetoSeVendePorSuPrecioDeCompraOMas()
        {
            List<string> mensajes = ObjetosQueSeVendenCaro(Todos<DatosObjeto>(), Todos<DatosOferta>());
            Assert.IsEmpty(mensajes, string.Join("\n", mensajes));
        }

        [Test]
        public void ReglaD_ConElBrazoVendidoA1300_FallaYNombraElObjeto()
        {
            var ofertas = Todos<DatosOferta>();
            var brazoReal = Cargar<DatosObjeto>(Carpeta + "DatosObjeto_BrazoChatarra.asset");
            var brazoCaro = DatosObjeto.Crear(brazoReal.Id, brazoReal.Nombre, brazoReal.Tipo, 1300, brazoReal.Maximo, brazoReal.Oferta);
            try
            {
                List<string> mensajes = ObjetosQueSeVendenCaro(new[] { brazoCaro }, ofertas);
                Assert.AreEqual(1, mensajes.Count, "El brazo a $1,300 debe romper la regla d.");
                StringAssert.Contains("Brazo de chatarra", mensajes[0]);
                Debug.Log("Corrida que falla a propósito: " + mensajes[0]);
            }
            finally
            {
                Object.DestroyImmediate(brazoCaro);
            }
        }

        [Test]
        public void ReglaE_PiezaEnJuego_ValeMenosQueElPremio()
        {
            // Hoy solo la casera tiene apuesta; su premio es DatosPremioPelea_Casera.
            foreach (DatosApuestaPelea a in Todos<DatosApuestaPelea>().Where(x => x.PiezaEnJuego != null))
            {
                int premio = a.name.Contains("Casera") ? PremioCasera.PremioGanar : int.MaxValue;
                Assert.Less(a.PiezaEnJuego.ValorVenta, premio,
                    $"REGLA e: en {a.name} la pieza en juego '{a.PiezaEnJuego.Nombre}' vale ${a.PiezaEnJuego.ValorVenta} y el premio es ${premio}.");
            }
        }

        [Test]
        public void ReglaF_CadaApuesta_TieneMomioMontosYTopeEnOrden()
        {
            List<DatosApuestaPelea> apuestas = Todos<DatosApuestaPelea>();
            Assert.IsNotEmpty(apuestas, "REGLA f: no hay ninguna DatosApuestaPelea.");
            foreach (DatosApuestaPelea a in apuestas)
            {
                Assert.GreaterOrEqual(a.Momio, 0.2f, $"REGLA f: {a.name} tiene momio {a.Momio} (mínimo 0.2).");
                Assert.LessOrEqual(a.Momio, 3f, $"REGLA f: {a.name} tiene momio {a.Momio} (máximo 3).");
                int[] m = a.Montos;
                Assert.IsNotEmpty(m, $"REGLA f: {a.name} no tiene montos.");
                for (int i = 0; i < m.Length; i++)
                {
                    if (i > 0)
                        Assert.Greater(m[i], m[i - 1], $"REGLA f: en {a.name} los montos no crecen ({m[i - 1]} luego {m[i]}).");
                    Assert.LessOrEqual(m[i], a.ApuestaMaxima, $"REGLA f: en {a.name} el monto ${m[i]} pasa de la apuesta máxima ${a.ApuestaMaxima}.");
                }
                if (a.name.Contains("Casera"))
                    Assert.LessOrEqual(a.ApuestaMaxima, PremioCasera.PremioGanar,
                        $"REGLA f: la apuesta máxima de {a.name} (${a.ApuestaMaxima}) pasa del premio al ganar (${PremioCasera.PremioGanar}).");
            }
        }

        [Test]
        public void ReglaG_ElKitCabeEnElInventario_ParaUsarElTopeDeLaPelea()
        {
            Assert.GreaterOrEqual(Kit.Maximo, ConfigKit.TopePorPelea,
                $"REGLA g: el inventario guarda {Kit.Maximo} kits y la pelea deja usar {ConfigKit.TopePorPelea}.");
        }

        [Test]
        public void TablaDePrecios_SeImprime()
        {
            DatosEconomia e = Economia;
            var brazo = Cargar<DatosObjeto>(Carpeta + "DatosObjeto_BrazoChatarra.asset");
            var chatarra = Cargar<DatosObjeto>(Carpeta + "DatosObjeto_Chatarra.asset");
            var apuesta = Cargar<DatosApuestaPelea>(Carpeta + "DatosApuestaPelea_Casera.asset");
            int koCosto = ReglaReparacion.Costo(MechaJugador.VidaMaxima, e.CostoReparacionPorPunto, e.CostoReparacionMinimo);
            float puntosKit = ConfigKit.FraccionCuracion * MechaJugador.VidaMaxima;
            Debug.Log("TABLA BALANCE\n" +
                $"dineroInicial={e.DineroInicial}; reparacionPorPunto={e.CostoReparacionPorPunto}; reparacionMinimo={e.CostoReparacionMinimo}; " +
                $"renta={e.Renta} cada {e.DiasEntreRentas} dias\n" +
                $"premioCasera={PremioCasera.PremioGanar}/{PremioCasera.PremioPerder}; premioTutorial=" +
                $"{Cargar<DatosPremioPelea>(Carpeta + "DatosPremioPelea_Tutorial.asset").PremioGanar}\n" +
                $"vidaMaxima={MechaJugador.VidaMaxima}; repararKO={koCosto}\n" +
                $"apuesta momio={apuesta.Momio}; montos={string.Join("/", apuesta.Montos)}; maxima={apuesta.ApuestaMaxima}; " +
                $"pieza={apuesta.PiezaEnJuego.Nombre} desde {apuesta.ApuestaMinimaPorPieza}\n" +
                $"brazo venta={brazo.ValorVenta} max={brazo.Maximo} compra={PrecioDeCompra(brazo, Todos<DatosOferta>())}; " +
                $"chatarra venta={chatarra.ValorVenta} max={chatarra.Maximo}\n" +
                $"kit precio={Kit.Oferta.Precio}; cura={puntosKit} puntos; porPunto={Kit.Oferta.Precio / puntosKit}; max={Kit.Maximo}; " +
                $"topePorPelea={ConfigKit.TopePorPelea}");
        }
    }
}

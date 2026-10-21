using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Mecha;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// Reglas de balance del calendario de las ligas (T-158, D-tony-98). Leen los assets reales: si alguien mueve un premio o un
    /// momio en el inspector y rompe una regla, el mensaje dice qué liga, qué rival y qué número la rompe.
    /// </summary>
    public class BalanceLigasPruebas
    {
        private const string RutaCalendario = "Assets/_Proyecto/Pelea/Datos/CalendarioLigas_Base.asset";
        private const string RutaLigas = "Assets/_Proyecto/Progresion/Datos/DatosLigas_Base.asset";
        private const string RutaPremioCasera = "Assets/_Proyecto/Economia/Datos/DatosPremioPelea_Casera.asset";
        private const string RutaEconomia = "Assets/_Proyecto/Economia/Datos/DatosEconomia_Base.asset";
        private const string RutaMechaJugador = "Assets/_Proyecto/Mecha/Datos/DatosMecha_Jugador.asset";
        private const string RutaApuestas = "Assets/_Proyecto/Economia/Datos/DatosApuestaPelea_";
        private const string NombreBrazoChatarra = "Brazo de chatarra";
        private const float MaximoPerderSobreGanar = 0.2f;
        private const int MinimoJefeSobreRival1 = 2;

        private static T Cargar<T>(string ruta) where T : Object
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(ruta);
            Assert.IsNotNull(a, ruta);
            return a;
        }

        private static CalendarioLigas Calendario => Cargar<CalendarioLigas>(RutaCalendario);
        private static DatosLigas Ligas => Cargar<DatosLigas>(RutaLigas);

        private static CartelPelea Cartel(int liga, int indice)
        {
            CartelPelea c = Calendario.Cartel(liga, indice);
            Assert.IsNotNull(c, $"Falta el cartel de la liga {liga}, rival {indice}.");
            return c;
        }

        private static string Nombre(int liga, int indice) => $"liga {liga} rival {indice}";

        private static IEnumerable<(int liga, int indice, CartelPelea cartel)> Todos()
        {
            CalendarioLigas cal = Calendario;
            for (int l = 1; l <= cal.CantidadLigas; l++)
                for (int n = 1; n <= cal.RivalesEn(l); n++)
                    yield return (l, n, cal.Cartel(l, n));
        }

        [Test]
        public void ReglaA_CalendarioCoincideConDatosLigas_YCadaCartelEstaCompleto()
        {
            DatosLigas ligas = Ligas;
            CalendarioLigas cal = Calendario;
            Assert.AreEqual(ligas.CantidadLigas, cal.CantidadLigas,
                $"REGLA a: el calendario tiene {cal.CantidadLigas} ligas y DatosLigas_Base {ligas.CantidadLigas}.");
            for (int l = 1; l <= cal.CantidadLigas; l++)
            {
                Assert.AreEqual(ligas.RivalesPorLiga, cal.RivalesEn(l),
                    $"REGLA a: la liga {l} tiene {cal.RivalesEn(l)} carteles y DatosLigas_Base pide {ligas.RivalesPorLiga}.");
                for (int n = 1; n <= cal.RivalesEn(l); n++)
                {
                    CartelPelea c = cal.Cartel(l, n);
                    string quien = Nombre(l, n);
                    Assert.IsNotNull(c.prefabRival, $"REGLA a: {quien} no tiene prefab de rival.");
                    Assert.IsNotNull(c.apuesta, $"REGLA a: {quien} no tiene apuesta.");
                    Assert.IsNotNull(c.presentador, $"REGLA a: {quien} no tiene presentador.");
                    Assert.AreEqual(l, c.liga, $"REGLA a: {quien} tiene liga = {c.liga}.");
                    Assert.AreEqual(n == cal.RivalesEn(l), c.esJefe,
                        $"REGLA a: {quien} tiene esJefe = {c.esJefe}; solo el último rival de la liga es jefe.");
                }
            }
        }

        [Test]
        public void ReglaB_PremioSubeDentroDeLaLiga_YElJefePagaElDobleDelRival1()
        {
            CalendarioLigas cal = Calendario;
            for (int l = 1; l <= cal.CantidadLigas; l++)
            {
                for (int n = 2; n <= cal.RivalesEn(l); n++)
                {
                    Assert.Greater(Cartel(l, n).premioGanar, Cartel(l, n - 1).premioGanar,
                        $"REGLA b: {Nombre(l, n)} paga ${Cartel(l, n).premioGanar} y {Nombre(l, n - 1)} paga ${Cartel(l, n - 1).premioGanar}: el premio debe subir.");
                }
                int jefe = cal.RivalesEn(l);
                int minimo = MinimoJefeSobreRival1 * Cartel(l, 1).premioGanar;
                Assert.GreaterOrEqual(Cartel(l, jefe).premioGanar, minimo,
                    $"REGLA b: el jefe de la liga {l} paga ${Cartel(l, jefe).premioGanar} y debe pagar al menos el doble del rival 1 (${minimo}).");
            }
        }

        [Test]
        public void ReglaC_Rival1PagaMasQueElRival4DeLaLigaAnterior()
        {
            CalendarioLigas cal = Calendario;
            for (int l = 2; l <= cal.CantidadLigas; l++)
            {
                int ultimoNormal = cal.RivalesEn(l - 1) - 1;
                Assert.Greater(Cartel(l, 1).premioGanar, Cartel(l - 1, ultimoNormal).premioGanar,
                    $"REGLA c: {Nombre(l, 1)} paga ${Cartel(l, 1).premioGanar} y {Nombre(l - 1, ultimoNormal)} paga ${Cartel(l - 1, ultimoNormal).premioGanar}.");
            }
        }

        [Test]
        public void ReglaD_MomioSubeDeLigaEnLiga_YElJefeEsMayorQueSuLiga()
        {
            CalendarioLigas cal = Calendario;
            float momioAnterior = 0f;
            for (int l = 1; l <= cal.CantidadLigas; l++)
            {
                int jefe = cal.RivalesEn(l);
                float maximoNormal = 0f, minimoNormal = float.MaxValue;
                for (int n = 1; n < jefe; n++)
                {
                    float m = Cartel(l, n).apuesta.Momio;
                    maximoNormal = Mathf.Max(maximoNormal, m);
                    minimoNormal = Mathf.Min(minimoNormal, m);
                }
                Assert.Greater(minimoNormal, momioAnterior,
                    $"REGLA d: el momio de la liga {l} (${minimoNormal}) no es mayor que el de la liga anterior ({momioAnterior}).");
                float momioJefe = Cartel(l, jefe).apuesta.Momio;
                Assert.Greater(momioJefe, maximoNormal,
                    $"REGLA d: el momio del jefe de la liga {l} ({momioJefe}) no es mayor que el de su liga ({maximoNormal}).");
                momioAnterior = maximoNormal;
            }
        }

        [Test]
        public void ReglaE_ApuestaMaximaNoPasaDelPremio_YMontosOrdenados()
        {
            foreach (var (liga, indice, c) in Todos())
            {
                string quien = Nombre(liga, indice);
                Assert.LessOrEqual(c.apuesta.ApuestaMaxima, c.premioGanar,
                    $"REGLA e: {quien} deja apostar hasta ${c.apuesta.ApuestaMaxima} y el premio de ganar es ${c.premioGanar}.");
                int[] montos = c.apuesta.Montos;
                Assert.IsNotEmpty(montos, $"REGLA e: {quien} no ofrece montos de apuesta.");
                for (int i = 0; i < montos.Length; i++)
                {
                    Assert.LessOrEqual(montos[i], c.apuesta.ApuestaMaxima,
                        $"REGLA e: {quien} ofrece apostar ${montos[i]} y su máxima es ${c.apuesta.ApuestaMaxima}.");
                    if (i > 0)
                        Assert.Greater(montos[i], montos[i - 1], $"REGLA e: los montos de {quien} no van de menor a mayor.");
                }
            }
        }

        [Test]
        public void ReglaF_PerderPagaComoMuchoElVeintePorCientoDeGanar()
        {
            foreach (var (liga, indice, c) in Todos())
            {
                Assert.LessOrEqual(c.premioPerder, c.premioGanar * MaximoPerderSobreGanar,
                    $"REGLA f: {Nombre(liga, indice)} paga ${c.premioPerder} al perder y ${c.premioGanar} al ganar (tope {MaximoPerderSobreGanar:P0}).");
            }
        }

        [Test]
        public void ReglaG_Rival1DeLaLiga1PagaAlMenosLoDeLaCasera()
        {
            int casera = Cargar<DatosPremioPelea>(RutaPremioCasera).PremioGanar;
            Assert.GreaterOrEqual(Cartel(1, 1).premioGanar, casera,
                $"REGLA g: el rival 1 de la liga 1 paga ${Cartel(1, 1).premioGanar} y la casera paga ${casera}.");
        }

        [Test]
        public void ReglaH_PremioDelJefeSuperaReparacionDeKO()
        {
            DatosEconomia e = Cargar<DatosEconomia>(RutaEconomia);
            DatosMecha mecha = Cargar<DatosMecha>(RutaMechaJugador);
            int costo = ReglaReparacion.Costo(mecha.VidaMaxima, e.CostoReparacionPorPunto, e.CostoReparacionMinimo);
            CalendarioLigas cal = Calendario;
            for (int l = 1; l <= cal.CantidadLigas; l++)
            {
                int jefe = cal.RivalesEn(l);
                Assert.Greater(Cartel(l, jefe).premioGanar, costo,
                    $"REGLA h: el jefe de la liga {l} paga ${Cartel(l, jefe).premioGanar} y reparar de KO cuesta ${costo}.");
            }
        }

        [Test]
        public void ReglaI_PiezaEnJuegoVale_MenosQueElPremio()
        {
            foreach (var (liga, indice, c) in Todos())
            {
                DatosObjeto pieza = c.apuesta.PiezaEnJuego;
                if (pieza == null) continue;
                Assert.Less(pieza.ValorVenta, c.premioGanar,
                    $"REGLA i: la pieza de {Nombre(liga, indice)} vale ${pieza.ValorVenta} y el premio es ${c.premioGanar}.");
            }
        }

        // T-171 (D-tony-121): la tabla de piezas en juego por apuesta (nombre de la pieza, nivel y apuesta mínima = mitad del tope).
        private static readonly (string asset, string pieza, int nivel, int minimo)[] PiezasEnJuego =
        {
            ("Liga1", "PIERNAS CUERPO A CUERPO I", 1, 250), ("Jefe1", "TORSO MILITAR I", 1, 500),
            ("Liga2", "BRAZO IZQ. TECH II", 2, 500), ("Jefe2", "TORSO CUERPO A CUERPO II", 2, 1000),
            ("Liga3", "BRAZO DER. FUEGO III", 3, 750), ("Jefe3", "CABINA TECH III", 3, 1500),
            ("Liga4", "PIERNAS MILITAR IV", 4, 1000), ("Jefe4", "TORSO FUEGO IV", 4, 2500),
        };

        [Test]
        public void PiezasEnJuego_LosOchoAssetsTraenLaPiezaYLaMinimaDeLaTabla()
        {
            foreach (var fila in PiezasEnJuego)
            {
                var apuesta = Cargar<DatosApuestaPelea>(RutaApuestas + fila.asset + ".asset");
                Assert.IsNotNull(apuesta.PiezaEnJuego, $"{fila.asset} no tiene pieza en juego.");
                Assert.AreEqual(fila.pieza, apuesta.PiezaEnJuego.Nombre, $"{fila.asset}: pieza en juego.");
                Assert.AreEqual(fila.minimo, apuesta.ApuestaMinimaPorPieza, $"{fila.asset}: apuesta mínima por la pieza.");
                Assert.AreEqual(apuesta.ApuestaMaxima / 2, fila.minimo, $"{fila.asset}: la mínima es la mitad del tope.");
            }
        }

        [Test]
        public void PiezasEnJuego_LaCaseraSigueConElBrazoDeChatarra()
        {
            var casera = Cargar<DatosApuestaPelea>(RutaApuestas + "Casera.asset");
            Assert.IsNotNull(casera.PiezaEnJuego);
            Assert.AreEqual(NombreBrazoChatarra, casera.PiezaEnJuego.Nombre);
        }

        [Test]
        public void ReglaK_LaPiezaEnJuegoDeCadaLigaEsDeRamaYDeNivelDeLaLiga_YLaMinimaNoPasaDelTope()
        {
            foreach (var (liga, indice, c) in Todos())
            {
                string quien = Nombre(liga, indice);
                var pieza = c.apuesta.PiezaEnJuego as DatosPieza;
                Assert.IsNotNull(pieza, $"REGLA k: la pieza en juego de {quien} no es una pieza de rama.");
                Assert.AreEqual(liga, pieza.Nivel, $"REGLA k: la pieza de {quien} es de nivel {pieza.Nivel} y la liga es {liga}.");
                Assert.LessOrEqual(c.apuesta.ApuestaMinimaPorPieza, c.apuesta.ApuestaMaxima,
                    $"REGLA k: {quien} pide apostar ${c.apuesta.ApuestaMinimaPorPieza} por la pieza y el tope es ${c.apuesta.ApuestaMaxima}.");
                Assert.IsTrue(c.apuesta.Montos.Any(m => m >= c.apuesta.ApuestaMinimaPorPieza),
                    $"REGLA k: {quien} no ofrece ningún monto que alcance para la pieza (${c.apuesta.ApuestaMinimaPorPieza}).");
            }
        }

        [Test]
        public void ReglaJ_FraccionDeRevanchaEstaEntre0Y1SinIncluirlos()
        {
            float f = Ligas.FraccionPremioRevancha;
            Assert.Greater(f, 0f, $"REGLA j: fraccionPremioRevancha es {f}; debe ser mayor que 0.");
            Assert.Less(f, 1f, $"REGLA j: fraccionPremioRevancha es {f}; debe ser menor que 1.");
        }
    }
}

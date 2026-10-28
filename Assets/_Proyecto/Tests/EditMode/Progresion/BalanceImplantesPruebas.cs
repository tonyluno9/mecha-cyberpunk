using System;
using System.Linq;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>Reglas de balance de los implantes (T-191, D-tony-171). Leen los assets reales; si una falla se ajusta la tabla de implantes.</summary>
    public class BalanceImplantesPruebas
    {
        private const string RutaCatalogo = "Assets/_Proyecto/Progresion/Datos/Implantes/CatalogoImplantes_Base.asset";
        private const string RutaCalendario = "Assets/_Proyecto/Pelea/Datos/CalendarioLigas_Base.asset";
        private const string RutaPersonaje = "Assets/_Proyecto/Personaje/Datos/DatosPersonaje_Base.asset";
        private const string RutaMecha = "Assets/_Proyecto/Mecha/Datos/DatosMecha_Jugador.asset";

        // Valor provisional de D-tony-169 hasta que exista el asset del pandillero (T-195).
        private const float VidaPandillero = 60f;
        // Pisos y tope de la regla g (D-tony-171).
        private const float PisoPreparacion = 0.15f;
        private const float PisoGuardia = 0.08f;
        private const float TopeVentana = 0.3f;
        private const float AlturaMaximaSalto = 2f;

        private static CatalogoImplantes Catalogo()
        {
            var c = AssetDatabase.LoadAssetAtPath<CatalogoImplantes>(RutaCatalogo);
            Assert.IsNotNull(c, RutaCatalogo);
            return c;
        }

        private static CalendarioLigas Calendario()
        {
            var c = AssetDatabase.LoadAssetAtPath<CalendarioLigas>(RutaCalendario);
            Assert.IsNotNull(c, RutaCalendario);
            return c;
        }

        private static int Rival1(int liga) => Calendario().Cartel(liga, 1).premioGanar;

        private static int Jefe(int liga) => Calendario().Cartel(liga, Calendario().RivalesEn(liga)).premioGanar;

        private static DatosImplante[] Nivel(int n) => Catalogo().Implantes.Where(d => d.Nivel == n).ToArray();

        [Test]
        public void Tabla_TieneLosOchoConIdsYEspaciosCompletos()
        {
            DatosImplante[] todos = Catalogo().Implantes;
            Assert.AreEqual(8, todos.Length);
            foreach (EspacioImplante e in Enum.GetValues(typeof(EspacioImplante)))
            {
                Assert.IsNotNull(Catalogo().Buscar(e, 1), e + " nivel 1");
                Assert.IsNotNull(Catalogo().Buscar(e, 2), e + " nivel 2");
            }
            foreach (DatosImplante d in todos) Assert.AreEqual("implante_" + d.Espacio.ToString().ToLowerInvariant() + "_" + d.Nivel, d.Id);
        }

        [Test]
        public void ReglaA_NingunImplanteHaceMasLentaLaReaccionDelMecha()
        {
            foreach (DatosImplante d in Catalogo().Implantes)
            {
                Assert.Greater(d.Efectos.multiplicadorReaccionMecha, 0f, d.Id);
                Assert.LessOrEqual(d.Efectos.multiplicadorReaccionMecha, 1f, d.Id);
            }
        }

        [Test]
        public void ReglaB_Nivel1CuestaEntre3VecesElRival1DeLaLiga1YElJefeDeLaLiga2()
        {
            int piso = 3 * Rival1(1), tope = Jefe(2);
            foreach (DatosImplante d in Nivel(1))
            {
                Assert.GreaterOrEqual(d.Precio, piso, $"REGLA b: {d.Id} cuesta ${d.Precio} y el piso es ${piso}.");
                Assert.LessOrEqual(d.Precio, tope, $"REGLA b: {d.Id} cuesta ${d.Precio} y el tope es ${tope}.");
            }
        }

        [Test]
        public void ReglaC_Nivel2CuestaEntreElJefeDeLaLiga3YElDeLaLiga4()
        {
            int piso = Jefe(3), tope = Jefe(4);
            foreach (DatosImplante d in Nivel(2))
            {
                Assert.GreaterOrEqual(d.Precio, piso, $"REGLA c: {d.Id} cuesta ${d.Precio} y el piso es ${piso}.");
                Assert.LessOrEqual(d.Precio, tope, $"REGLA c: {d.Id} cuesta ${d.Precio} y el tope es ${tope}.");
            }
        }

        [Test]
        public void ReglaD_ElNivel2CuestaAlMenosElDobleQueElNivel1()
        {
            foreach (EspacioImplante e in Enum.GetValues(typeof(EspacioImplante)))
            {
                int p1 = Catalogo().Buscar(e, 1).Precio, p2 = Catalogo().Buscar(e, 2).Precio;
                Assert.GreaterOrEqual(p2, 2 * p1, $"REGLA d: {e} nivel 2 ${p2} contra nivel 1 ${p1}.");
            }
        }

        [Test]
        public void ReglaE_LosCuatroDeNivel1CuestanMenosQueGanarUnaVezLasLigas1Y2()
        {
            int suma = Nivel(1).Sum(d => d.Precio);
            int premios = 0;
            for (int liga = 1; liga <= 2; liga++)
            {
                for (int i = 1; i <= Calendario().RivalesEn(liga); i++) premios += Calendario().Cartel(liga, i).premioGanar;
            }
            Assert.Less(suma, premios, $"REGLA e: los 4 de nivel 1 suman ${suma} y las ligas 1 y 2 pagan ${premios}.");
        }

        [Test]
        public void ReglaF_BrazosIINuncaTumbaAUnPandilleroDeUnGolpe()
        {
            var personaje = AssetDatabase.LoadAssetAtPath<DatosPersonaje>(RutaPersonaje);
            Assert.IsNotNull(personaje, RutaPersonaje);
            foreach (DatosImplante d in Catalogo().Implantes)
            {
                float golpe = personaje.DanioGolpe * d.Efectos.multiplicadorGolpeAPie;
                Assert.Less(golpe, VidaPandillero, $"REGLA f: {d.Id} pega {golpe} y el pandillero tiene {VidaPandillero}.");
            }
        }

        [Test]
        public void ReglaG_SinapsisIINoPasaDePisosNiDeTope()
        {
            var mecha = AssetDatabase.LoadAssetAtPath<DatosMecha>(RutaMecha);
            Assert.IsNotNull(mecha, RutaMecha);
            float m = Catalogo().Buscar(EspacioImplante.SistemaNervioso, 2).Efectos.multiplicadorReaccionMecha;
            Assert.GreaterOrEqual(mecha.PreparacionGolpe * m, PisoPreparacion, "REGLA g: preparación del golpe");
            Assert.GreaterOrEqual(mecha.TiempoSubirGuardia * m, PisoGuardia, "REGLA g: subir guardia");
            Assert.LessOrEqual(mecha.VentanaBloqueoPerfecto / m, TopeVentana, "REGLA g: ventana del bloqueo perfecto");
        }

        [Test]
        public void ReglaI_ElSaltoConPiernasIIEsMenorDe2Metros()
        {
            var personaje = AssetDatabase.LoadAssetAtPath<DatosPersonaje>(RutaPersonaje);
            foreach (DatosImplante d in Catalogo().Implantes)
            {
                float salto = personaje.AlturaSalto * d.Efectos.multiplicadorSalto;
                Assert.Less(salto, AlturaMaximaSalto, $"REGLA i: {d.Id} salta {salto} m.");
            }
        }

        [Test]
        public void Bitacora_ImprimeLaTablaDeLosOcho()
        {
            var personaje = AssetDatabase.LoadAssetAtPath<DatosPersonaje>(RutaPersonaje);
            var mecha = AssetDatabase.LoadAssetAtPath<DatosMecha>(RutaMecha);
            var sb = new StringBuilder("[Progresion] Implantes (T-191)\n");
            sb.AppendLine("id | nombre | precio | liga | efecto");
            foreach (DatosImplante d in Catalogo().Implantes)
            {
                EfectosImplantes e = d.Efectos;
                string efecto = e.verVidaRival ? $"vida rival{(e.vidaRivalEnNumeros ? " en números" : "")}, aviso {e.anticipacionAtaque:0.0} s" :
                    e.multiplicadorGolpeAPie != 1f ? $"golpe a pie x{e.multiplicadorGolpeAPie} ({personaje.DanioGolpe * e.multiplicadorGolpeAPie:0.#} de daño)" :
                    e.multiplicadorCorrer != 1f ? $"correr x{e.multiplicadorCorrer}, salto x{e.multiplicadorSalto} ({personaje.AlturaSalto * e.multiplicadorSalto:0.00} m)" :
                    $"reacción x{e.multiplicadorReaccionMecha}: golpe {mecha.PreparacionGolpe * e.multiplicadorReaccionMecha:0.###} s, " +
                    $"guardia {mecha.TiempoSubirGuardia * e.multiplicadorReaccionMecha:0.###} s, ventana {mecha.VentanaBloqueoPerfecto / e.multiplicadorReaccionMecha:0.###} s";
                sb.AppendLine($"{d.Id} | {d.Nombre} | ${d.Precio} | {d.LigaMinima} | {efecto}");
            }
            sb.AppendLine($"b) piso 3 x rival1 liga1 = ${3 * Rival1(1)}, tope jefe liga2 = ${Jefe(2)}");
            sb.AppendLine($"c) piso jefe liga3 = ${Jefe(3)}, tope jefe liga4 = ${Jefe(4)}");
            sb.AppendLine($"e) suma nivel 1 = ${Nivel(1).Sum(d => d.Precio)}");
            Debug.Log(sb.ToString());
            Assert.AreEqual(8, Catalogo().Implantes.Length);
        }
    }
}

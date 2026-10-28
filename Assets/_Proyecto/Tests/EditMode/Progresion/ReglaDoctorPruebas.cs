using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Progresion.Prologo.Clinica;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>Las decisiones del doctor de la clínica (T-196) y su balance contra los premios y la pandilla (regla h de D-tony-171).</summary>
    public class ReglaDoctorPruebas
    {
        private const string RutaDatos = "Assets/_Proyecto/Progresion/Prologo/Datos/DatosDoctor_Base.asset";
        private const string RutaCalendario = "Assets/_Proyecto/Pelea/Datos/CalendarioLigas_Base.asset";
        private const string RutaPandillero = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Base.asset";
        private const float Tolerancia = 0.0001f;

        [Test]
        public void Actua_ConHistoriaLibre_Si()
        {
            Assert.IsTrue(ReglaDoctor.Actua(true, EtapaPrologo.Libre, false, false));
            Assert.IsTrue(ReglaDoctor.Actua(true, EtapaPrologo.Libre, true, false));
        }

        [Test]
        public void Actua_SinHistoria_Si()
        {
            Assert.IsTrue(ReglaDoctor.Actua(false, EtapaPrologo.Ninguna, false, false));
        }

        [Test]
        public void Actua_ConElPrologoEnCurso_No()
        {
            Assert.IsFalse(ReglaDoctor.Actua(true, EtapaPrologo.Emboscada, false, false));
            Assert.IsFalse(ReglaDoctor.Actua(true, EtapaPrologo.PeleaCasera, true, false));
        }

        [Test]
        public void Actua_ElMechaCaeDentroDeUnaPeleaDelFlujo_No()
        {
            Assert.IsFalse(ReglaDoctor.Actua(true, EtapaPrologo.Libre, true, true));
        }

        [Test]
        public void Actua_ElJugadorCaeAPieAunqueHayaPeleaDelFlujo_Si()
        {
            Assert.IsTrue(ReglaDoctor.Actua(true, EtapaPrologo.Libre, false, true));
        }

        [Test]
        public void FraccionMecha_FueraDelTallerConVida_EsLaMenor()
        {
            Assert.AreEqual(0.2f, ReglaDoctor.FraccionMecha(0.8f, 0.2f, true), Tolerancia);
            Assert.AreEqual(0.1f, ReglaDoctor.FraccionMecha(0.1f, 0.2f, true), Tolerancia);
        }

        [Test]
        public void FraccionMecha_FueraDelTallerEnKO_EsCeroYElMechaLaSubeAlPiso()
        {
            Assert.AreEqual(0f, ReglaDoctor.FraccionMecha(0f, 0.2f, true), Tolerancia);
        }

        [Test]
        public void FraccionMecha_EnElTaller_NoSeToca()
        {
            Assert.AreEqual(0.8f, ReglaDoctor.FraccionMecha(0.8f, 0.2f, false), Tolerancia);
            Assert.AreEqual(0f, ReglaDoctor.FraccionMecha(0f, 0.2f, false), Tolerancia);
        }

        [Test]
        public void HoraDeDespertar_PasadaLaMedianoche_DaLaHoraDelDiaSiguiente()
        {
            Assert.AreEqual(5f, ReglaDoctor.HoraDeDespertar(23f, 6f), Tolerancia);
            Assert.AreEqual(16f, ReglaDoctor.HoraDeDespertar(10f, 6f), Tolerancia);
        }

        [Test]
        public void TextoDoctor_ConYSinDeuda()
        {
            Assert.AreEqual("hola", ReglaDoctor.TextoDoctor(0, "hola", "debes"));
            Assert.AreEqual("hola", ReglaDoctor.TextoDoctor(300, "hola", "debes"));
            Assert.AreEqual("hola debes", ReglaDoctor.TextoDoctor(-200, "hola", "debes"));
            Assert.IsTrue(ReglaDoctor.QuedoDebiendo(-1));
            Assert.IsFalse(ReglaDoctor.QuedoDebiendo(0));
        }

        [Test]
        public void Asset_TraeLosValoresDeD170()
        {
            DatosDoctor datos = AssetDatabase.LoadAssetAtPath<DatosDoctor>(RutaDatos);
            Assert.IsNotNull(datos, RutaDatos);
            Assert.AreEqual(500, datos.Cobro);
            Assert.AreEqual(6f, datos.HorasInconsciente, Tolerancia);
            Assert.AreEqual(0.2f, datos.FraccionVidaMecha, Tolerancia);
            Assert.AreEqual("VUELVE A CASA", datos.TextoObjetivo);
            Assert.AreEqual("Clinica_Camilla", datos.IdAparicionCamilla);
            Assert.AreEqual("Casa_Cama", datos.IdAparicionCasa);
        }

        [Test]
        public void ReglaH_ElDoctorCobraMenosQueElRival1DeLaLiga1YMasQueUnaPandillaEntera()
        {
            DatosDoctor datos = AssetDatabase.LoadAssetAtPath<DatosDoctor>(RutaDatos);
            var calendario = AssetDatabase.LoadAssetAtPath<MechaCyberpunk.Pelea.Datos.CalendarioLigas>(RutaCalendario);
            var pandillero = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaPandillero);
            Assert.IsNotNull(calendario, RutaCalendario);
            Assert.IsNotNull(pandillero, RutaPandillero);
            int premio = calendario.Cartel(1, 1).premioGanar;
            int pandilla = pandillero.Dinero * pandillero.TamanoPandilla;
            UnityEngine.Debug.Log("[T-196] regla h: cobro $" + datos.Cobro + ", rival 1 liga 1 $" + premio + ", pandilla entera $" + pandilla);
            Assert.Less(datos.Cobro, premio, "el doctor cobra menos que el premio del rival 1 de la liga 1");
            Assert.Greater(datos.Cobro, pandilla, "el doctor cobra más que lo que suelta una pandilla entera");
        }
    }
}

using System.Collections.Generic;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1360: funciones puras del barrido del mapa (escalones, IoU, agrupado, zona por posición).</summary>
    public class BarridoMapaT1360Pruebas
    {
        [Test]
        public void EscalonesEnPerfil_SaltoDe20Cm_DaElIndiceDeLlegada()
        {
            List<int> r = BarridoMapaT1360.EscalonesEnPerfil(new List<float> { 0f, 0f, 0.2f, 0.2f }, 0.12f);
            Assert.AreEqual(new List<int> { 2 }, r);
        }

        [Test]
        public void EscalonesEnPerfil_PerfilSuave_NoDaNada()
        {
            Assert.IsEmpty(BarridoMapaT1360.EscalonesEnPerfil(new List<float> { 0f, 0.05f, 0.1f, 0.15f }, 0.12f));
        }

        [Test]
        public void EscalonesEnPerfil_MuestraSinPiso_CortaLaComparacion()
        {
            Assert.IsEmpty(BarridoMapaT1360.EscalonesEnPerfil(new List<float> { 0f, float.NaN, 1f }, 0.12f));
        }

        [Test]
        public void GravedadEscalon_Umbrales()
        {
            Assert.AreEqual(0, BarridoMapaT1360.GravedadEscalon(0.1f));
            Assert.AreEqual(3, BarridoMapaT1360.GravedadEscalon(-0.2f));
            Assert.AreEqual(4, BarridoMapaT1360.GravedadEscalon(0.4f));
        }

        [Test]
        public void IoU_CajasIguales_EsUno()
        {
            Bounds b = new Bounds(Vector3.zero, Vector3.one * 2f);
            Assert.AreEqual(1f, BarridoMapaT1360.IoU(b, b), 1e-4f);
        }

        [Test]
        public void IoU_MitadTraslapada_EsUnTercio()
        {
            Bounds a = new Bounds(Vector3.zero, Vector3.one * 2f);
            Bounds b = new Bounds(new Vector3(1f, 0f, 0f), Vector3.one * 2f);
            Assert.AreEqual(1f / 3f, BarridoMapaT1360.IoU(a, b), 1e-4f);
        }

        [Test]
        public void IoU_CajasSeparadas_EsCero()
        {
            Bounds a = new Bounds(Vector3.zero, Vector3.one);
            Bounds b = new Bounds(new Vector3(10f, 0f, 0f), Vector3.one);
            Assert.AreEqual(0f, BarridoMapaT1360.IoU(a, b));
        }

        [Test]
        public void Agrupar_PuntosCercanos_QuedanJuntos()
        {
            List<List<int>> g = BarridoMapaT1360.Agrupar(new List<Vector3> { Vector3.zero, new Vector3(5f, 0f, 0f), new Vector3(50f, 0f, 0f), new Vector3(52f, 0f, 0f) }, 8f);
            Assert.AreEqual(2, g.Count);
            Assert.AreEqual(new List<int> { 0, 1 }, g[0]);
            Assert.AreEqual(new List<int> { 2, 3 }, g[1]);
        }

        [Test]
        public void AreaTraslape_CarasQueSeCruzan_DaElArea()
        {
            Bounds a = new Bounds(Vector3.zero, new Vector3(4f, 0.2f, 4f));
            Bounds b = new Bounds(new Vector3(2f, 0f, 0f), new Vector3(4f, 0.2f, 4f));
            Assert.AreEqual(8f, BarridoMapaT1360.AreaTraslape(a, b, 1), 1e-3f);
        }

        [TestCase(0f, -230f, "Malecon")]
        [TestCase(-400f, 0f, "Cerro")]
        [TestCase(0f, 250f, "Autopista")]
        [TestCase(300f, 100f, "Autopista")]
        [TestCase(250f, -100f, "Puerto")]
        [TestCase(0f, 0f, "Viejo")]
        [TestCase(-47f, -190f, "Union-U1")]
        [TestCase(0f, 500f, "Fuera")]
        public void ZonaPorPosicion_DevuelveLaZona(float x, float z, string esperada)
        {
            Assert.AreEqual(esperada, BarridoMapaT1360.ZonaPorPosicion(new Vector3(x, 0f, z)));
        }

        [Test]
        public void EnAlcance_ViejoYFuera_NoSeRevisan()
        {
            Assert.IsFalse(BarridoMapaT1360.EnAlcance("Viejo"));
            Assert.IsFalse(BarridoMapaT1360.EnAlcance("Fuera"));
            Assert.IsTrue(BarridoMapaT1360.EnAlcance("Union-U4"));
        }
    }
}

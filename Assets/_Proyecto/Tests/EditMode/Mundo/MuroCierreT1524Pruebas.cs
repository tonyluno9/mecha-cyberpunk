using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Mundo.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1524: muro cerrado. Las pruebas de cuentas son puras; la de escena pide haber corrido MuroCierreT1524.Aplicar (escena guardada).
    /// </summary>
    public class MuroCierreT1524Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";

        [Test]
        public void Lados_Rectangulo_CuatroLadosConSuLargo()
        {
            CalculoMuroT1524.Lado[] l = CalculoMuroT1524.Lados(-135f, 135f, -88f, 88f);
            Assert.AreEqual(4, l.Length);
            Assert.AreEqual(88f, l.First(x => x.Nombre == "Este").Mitad, 0.01f);
            Assert.AreEqual(135f, l.First(x => x.Nombre == "Norte").Mitad, 0.01f);
            Assert.AreEqual(new Vector3(135f, 0f, 0f), l.First(x => x.Nombre == "Este").Centro);
            Assert.AreEqual(Vector3.back, l.First(x => x.Nombre == "Sur").Fuera);
        }

        [Test]
        public void Muestras_CadaMetroSinLasEsquinas()
        {
            CalculoMuroT1524.Lado lado = CalculoMuroT1524.Lados(-135f, 135f, -88f, 88f).First(x => x.Nombre == "Este");
            List<float> ts = CalculoMuroT1524.Muestras(lado);
            Assert.AreEqual(-86.5f, ts[0], 0.001f);
            Assert.AreEqual(86.5f, ts[ts.Count - 1], 0.001f);
            Assert.AreEqual(174, ts.Count);
        }

        [Test]
        public void Huecos_AgrupaMuestrasContinuas()
        {
            List<float> t = Enumerable.Range(0, 10).Select(i => (float)i).ToList();
            bool[] cerrado = { true, false, false, true, true, true, false, true, true, true };
            List<CalculoMuroT1524.Hueco> h = CalculoMuroT1524.Huecos(2, t, cerrado);
            Assert.AreEqual(2, h.Count);
            Assert.AreEqual(0.5f, h[0].TIni, 0.001f);
            Assert.AreEqual(2.5f, h[0].TFin, 0.001f);
            Assert.AreEqual(2, h[0].Lado);
            Assert.AreEqual(1f, h[1].Largo, 0.001f);
        }

        [Test]
        public void Huecos_TodoCerrado_Ninguno()
        {
            List<float> t = new List<float> { 0f, 1f, 2f };
            Assert.AreEqual(0, CalculoMuroT1524.Huecos(0, t, new[] { true, true, true }).Count);
        }

        [Test]
        public void EnPuerta_DentroDelVano()
        {
            Assert.IsTrue(CalculoMuroT1524.EnPuerta(new Vector3(135f, 0f, 4f), new Vector3(135f, 0f, 0f), 20f, 2f));
            Assert.IsFalse(CalculoMuroT1524.EnPuerta(new Vector3(135f, 0f, 30f), new Vector3(135f, 0f, 0f), 20f, 2f));
        }

        [Test]
        public void Escena_PerimetroSinHuecosNiTramosBajos()
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            MuroCierreT1524.Escaneo e = MuroCierreT1524.EscanearEscena();
            Assert.Greater(e.Muestras, 600, "el perimetro tiene que haberse recorrido");
            Assert.AreEqual(0, e.Huecos.Count, "huecos: " + string.Join("; ", e.Huecos.Select(h => e.Lados[h.Lado].Nombre + " " + h.TIni + ".." + h.TFin)));
            Assert.AreEqual(0, e.Bajos.Count, "tramos con collider bajo (< 7.5 m)");
        }
    }
}

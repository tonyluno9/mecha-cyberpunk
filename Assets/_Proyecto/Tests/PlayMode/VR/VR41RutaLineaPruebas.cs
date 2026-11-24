using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.VR.Ruta;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// VR41: en VR la ruta vuelve a ser la línea turquesa de antes (LineRenderer plano en el piso, hasta 90 m, sin puntos más juntos que 1.5 m); sin
    /// chevrones; se apaga al llegar o sin objetivo. (Reemplaza a RutaVRPruebas de los chevrones de VR30.)
    /// </summary>
    public class VR41RutaLineaPruebas
    {
        [Test]
        public void Recortar_RutaLarga_SeCortaANoventaMetros()
        {
            var ruta = new List<Vector3> { Vector3.zero, new Vector3(0f, 0f, 50f), new Vector3(60f, 0f, 50f), new Vector3(60f, 0f, 200f) };
            var puntos = new List<Vector3>();
            int n = ReglaRutaVR.Recortar(ruta, puntos);
            Assert.AreEqual(3, n, "0, la esquina de 50 m y el corte a 90 m.");
            float largo = 0f;
            for (int i = 1; i < puntos.Count; i++)
            {
                largo += Vector3.Distance(puntos[i - 1], puntos[i]);
            }
            Assert.AreEqual(ReglaRutaVR.MetrosMaximos, largo, 0.01f, "La línea mide como mucho 90 m.");
        }

        [Test]
        public void Recortar_PuntosMuyJuntos_SeSaltanPeroElUltimoVa()
        {
            var ruta = new List<Vector3> { Vector3.zero, new Vector3(0f, 0f, 0.5f), new Vector3(0f, 0f, 1f), new Vector3(0f, 0f, 1.2f) };
            var puntos = new List<Vector3>();
            ReglaRutaVR.Recortar(ruta, puntos);
            Assert.AreEqual(2, puntos.Count);
            Assert.AreEqual(new Vector3(0f, 0f, 1.2f), puntos[1]);
            Assert.AreEqual(0, ReglaRutaVR.Recortar(null, puntos));
        }

        [Test]
        public void RectaCorta_ApuntaAlObjetivoYMideComoMucho25()
        {
            Vector3 fin = ReglaRutaVR.FinRectaCorta(Vector3.zero, new Vector3(100f, 3f, 0f));
            Assert.AreEqual(new Vector3(ReglaRutaVR.MetrosRectaCorta, 0f, 0f), fin);
            Assert.AreEqual(new Vector3(0f, 0f, 10f), ReglaRutaVR.FinRectaCorta(Vector3.zero, new Vector3(0f, 0f, 10f)));
        }

        [Test]
        public void Llego_AMenosDeTresMetros()
        {
            Assert.IsTrue(ReglaRutaVR.Llego(Vector3.zero, new Vector3(2f, 5f, 0f)), "la altura no cuenta");
            Assert.IsFalse(ReglaRutaVR.Llego(Vector3.zero, new Vector3(4f, 0f, 0f)));
        }

        [UnityTest]
        public IEnumerator SinObjetivo_LaLineaExisteOcultaYNoHayChevrones()
        {
            var objeto = new GameObject("PruebaRutaVR41");
            RutaObjetivoVR ruta = objeto.AddComponent<RutaObjetivoVR>();
            yield return null;
            yield return null;
            Assert.IsNotNull(ruta.Linea, "La ruta es un LineRenderer (la línea de antes).");
            Assert.IsFalse(ruta.Visible, "Sin VR ni objetivo no se ve.");
            Assert.AreEqual(1, objeto.transform.childCount, "Solo la línea: sin chevrones.");
            Assert.AreEqual(0, objeto.GetComponentsInChildren<MeshRenderer>(true).Length, "Sin chevrones.");
            Object.Destroy(objeto);
        }
    }
}

using MechaCyberpunk.UI.Mapa;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-632: la ruta GPS del minimapa va por las calles del barrio de principio a fin.</summary>
    public class CalculoRutaGpsPruebas
    {
        [Test]
        public void Ruta_DeUnaCalleAOtra_VaPorLasCallesSinCortarManzanas()
        {
            Vector3 origen = new Vector3(-47f, 0f, -60f);
            Vector3 destino = new Vector3(47f, 0f, 60f);
            var ruta = CalculoRutaGps.Ruta(origen, destino);
            Assert.GreaterOrEqual(ruta.Count, 4, "Hay curvas por las esquinas");
            Assert.AreEqual(origen, ruta[0]);
            Assert.AreEqual(destino, ruta[ruta.Count - 1]);
            foreach (Vector3 p in ruta)
            {
                Assert.LessOrEqual(CalculoRutaGps.Calles.DistanciaALaCalle(p), 0.01f, "Todo punto está sobre una calle: " + p);
            }
            for (int i = 1; i < ruta.Count; i++)
            {
                Vector3 d = ruta[i] - ruta[i - 1];
                Assert.IsTrue(Mathf.Abs(d.x) < 0.01f || Mathf.Abs(d.z) < 0.01f, "Cada tramo sigue una calle recta: " + ruta[i - 1] + " a " + ruta[i]);
            }
            // La ruta por calles mide más que la recta pero no el doble de la distancia Manhattan.
            Assert.GreaterOrEqual(CalculoRutaGps.Largo(ruta), Vector3.Distance(origen, destino));
            Assert.LessOrEqual(CalculoRutaGps.Largo(ruta), 94f + 120f + 60f + 60f + 1f, "Es la ruta corta: 60 m al sur-norte, 94 m al este y 60 m al norte");
        }

        [Test]
        public void Ruta_DentroDeLaMismaCalle_EsUnaLinea()
        {
            var ruta = CalculoRutaGps.Ruta(new Vector3(-47f, 0f, -30f), new Vector3(-47f, 0f, 30f));
            Assert.AreEqual(60f, CalculoRutaGps.Largo(ruta), 0.5f);
        }

        [Test]
        public void Ruta_OrigenFueraDeLaCalle_EmpiezaConUnTramoAlaCalle()
        {
            Vector3 origen = new Vector3(0f, 0f, 20f); // en el centro de una manzana
            var ruta = CalculoRutaGps.Ruta(origen, new Vector3(47f, 0f, 60f));
            Assert.AreEqual(origen, ruta[0]);
            Assert.LessOrEqual(CalculoRutaGps.Calles.DistanciaALaCalle(ruta[1]), 0.01f, "El segundo punto ya está en la calle");
        }
    }
}

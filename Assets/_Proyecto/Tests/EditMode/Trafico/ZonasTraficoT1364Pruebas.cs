using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1364: las cajas de las zonas de tráfico (función pura sobre la lista).</summary>
    public class ZonasTraficoT1364Pruebas
    {
        private static string ZonaDe(float x, float y, float z) => ColocarT1364ZonasTrafico.ZonaDe(ColocarT1364ZonasTrafico.Cajas, new Vector3(x, y, z));

        [Test]
        [Ignore("T-1482: zonas nuevas quitadas del mapa (revertir mapa 19-nov)")]
        public void ZonaDe_PuntoDelMalecon_DaMalecon()
        {
            Assert.AreEqual("Malecon", ZonaDe(-100f, 0f, -226f));
            Assert.AreEqual("Malecon", ZonaDe(-300f, 0f, -205f));
        }

        [Test]
        [Ignore("T-1482: zonas nuevas quitadas del mapa (revertir mapa 19-nov)")]
        public void ZonaDe_PuntoDelPuerto_DaPuerto()
        {
            Assert.AreEqual("Puerto", ZonaDe(220f, 0f, -150f));
            Assert.AreEqual("Puerto", ZonaDe(160f, 0f, -215f));
        }

        [Test]
        [Ignore("T-1482: zonas nuevas quitadas del mapa (revertir mapa 19-nov)")]
        public void ZonaDe_AutopistaElevada_DaAutopista()
        {
            Assert.AreEqual("Autopista", ZonaDe(0f, 10f, 250f));
            Assert.AreEqual("Autopista", ZonaDe(300f, 5f, 100f));
        }

        [Test]
        public void ZonaDe_LoteBajoLaAutopista_DaDeshuesadero()
        {
            Assert.AreEqual("Deshuesadero", ZonaDe(0f, 0f, 250f));
        }

        [Test]
        public void ZonaDe_BarrioYCorredoresNorte_DanSuZona()
        {
            Assert.AreEqual("NeonAlto", ZonaDe(90f, 0f, 40f));
            Assert.AreEqual("LasLomas", ZonaDe(47f, 0f, 150f));
            Assert.AreEqual("LasLomas", ZonaDe(-47f, 0f, 150f));
        }

        [Test]
        public void ZonaDe_Suburbios_NoDaNinguna()
        {
            Assert.IsNull(ZonaDe(-200f, 0f, 50f));
        }

        [Test]
        public void Cajas_NoSeEnciman()
        {
            var pares = ColocarT1364ZonasTrafico.Encimes(ColocarT1364ZonasTrafico.Cajas);
            Assert.AreEqual(0, pares.Count, pares.Count > 0 ? pares[0].Item1.Objeto + " con " + pares[0].Item2.Objeto : "");
        }

        [Test]
        public void Cajas_UsanNombresDeZonaDeLaConfig()
        {
            string[] validos = { "Deshuesadero", "Malecon", "NeonAlto", "LasLomas", "Avenida", "Puerto", "Autopista", "Cerro" };
            foreach (ColocarT1364ZonasTrafico.Caja c in ColocarT1364ZonasTrafico.Cajas)
            {
                Assert.Contains(c.Zona, validos, c.Objeto);
            }
        }
    }
}

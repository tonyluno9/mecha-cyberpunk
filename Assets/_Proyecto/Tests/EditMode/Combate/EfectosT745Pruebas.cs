using MechaCyberpunk.Combate.Efectos;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Combate
{
    /// <summary>T-745: reglas del trazo de las balas (Combate).</summary>
    public class EfectosT745Pruebas
    {
        [Test]
        public void Trazo_LaCabezaNuncaPasaDelDestino_YLaColaLoAlcanza()
        {
            foreach (float distancia in new[] { 1f, 6f, 25f, 80f })
            {
                float fin = ReglaTrazo.Duracion(distancia);
                for (float t = 0f; t <= fin + 0.05f; t += 0.004f)
                {
                    ReglaTrazo.Tramo(distancia, t, out float cola, out float cabeza);
                    Assert.LessOrEqual(cabeza, distancia + 0.0001f, "la cabeza no pasa del impacto");
                    Assert.LessOrEqual(cola, cabeza + 0.0001f);
                    Assert.GreaterOrEqual(cola, 0f);
                }
                ReglaTrazo.Tramo(distancia, fin, out float colaFinal, out float cabezaFinal);
                Assert.AreEqual(distancia, colaFinal, 0.01f, "al terminar la cola llegó al destino: no queda nada flotando");
                Assert.AreEqual(distancia, cabezaFinal, 0.0001f);
                Assert.IsTrue(ReglaTrazo.Termino(distancia, fin));
                Assert.IsFalse(ReglaTrazo.Termino(distancia, fin * 0.5f));
            }
        }

        [Test]
        public void Trazo_DuraPocoAunALargaDistancia()
        {
            Assert.Less(ReglaTrazo.Duracion(ReglaTrazo.Cola), 0.05f);
            Assert.Less(ReglaTrazo.Duracion(TrazadorBala.AlcanceMaximo), 0.4f);
        }

        [Test]
        public void Opacidad_PlenaSiPego_YSeDesvaneceSiNo()
        {
            Assert.AreEqual(1f, ReglaTrazo.Opacidad(true, 30f, ReglaTrazo.Duracion(30f) * 0.9f), 0.0001f);
            float fin = ReglaTrazo.Duracion(60f);
            Assert.AreEqual(1f, ReglaTrazo.Opacidad(false, 60f, 0f), 0.0001f);
            Assert.Less(ReglaTrazo.Opacidad(false, 60f, fin * 0.5f), 0.6f);
            Assert.AreEqual(0f, ReglaTrazo.Opacidad(false, 60f, fin), 0.0001f);
        }
    }
}

using MechaCyberpunk.Trafico.Persecucion;
using MechaCyberpunk.Trafico.Testigos;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    public class CalculoCamarasPruebas
    {
        private const float Paso = 94f;
        private static readonly Vector2[] Centros =
        {
            new Vector2(-Paso, Paso / 2f), new Vector2(0f, Paso / 2f), new Vector2(Paso, Paso / 2f),
            new Vector2(-Paso, -Paso / 2f), new Vector2(0f, -Paso / 2f), new Vector2(Paso, -Paso / 2f)
        };

        [Test]
        public void Colocaciones_UnaPorEsquinaYCruce_YSoloEnCruces()
        {
            GrafoCalles grafo = GrafoCalles.DeManzanas(Centros, 80f, 0.2f);
            Assert.AreEqual(4, CalculoCamaras.Colocaciones(grafo, 2, 8.5f).Count, "2 cruces x 2 esquinas");
            Assert.AreEqual(8, CalculoCamaras.Colocaciones(grafo, 4, 8.5f).Count, "2 cruces x 4 esquinas");
            Assert.AreEqual(0, CalculoCamaras.Colocaciones(grafo, 0, 8.5f).Count);
        }

        [Test]
        public void Colocaciones_ElBrazoApuntaAlCentroDelCruce()
        {
            GrafoCalles grafo = GrafoCalles.DeManzanas(Centros, 80f, 0.2f);
            foreach (CalculoCamaras.Colocacion c in CalculoCamaras.Colocaciones(grafo, 4, 8.5f))
            {
                Vector3 adelante = Quaternion.Euler(0f, c.Giro, 0f) * Vector3.forward;
                float mejor = float.MaxValue;
                for (int i = 0; i < grafo.Cantidad; i++)
                {
                    if (grafo.Vecinos(i).Count >= 3)
                    {
                        mejor = Mathf.Min(mejor, Vector3.Angle(adelante, grafo.Nodo(i) - c.Posicion));
                    }
                }
                Assert.Less(mejor, 0.5f, "La cámara mira al cruce");
            }
        }
    }
}

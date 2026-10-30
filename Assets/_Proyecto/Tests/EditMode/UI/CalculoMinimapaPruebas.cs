using MechaCyberpunk.UI.Mapa;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-409: el minimapa gira con la cámara, recorta lo de fuera (o lo pega al borde) y el zoom escala la distancia.</summary>
    public class CalculoMinimapaPruebas
    {
        private const float RadioPx = 130f;
        private const float RadioMetros = 60f;
        private const float Tolerancia = 0.01f;

        [Test]
        public void GiraYRecorta()
        {
            float escala = CalculoMinimapa.Escala(RadioPx, RadioMetros, 1f);
            Vector3 jugador = new Vector3(10f, 0f, -5f);

            // Un punto 30 m al norte, mirando al este (rumbo 90°): queda a la izquierda, a 30 m del centro.
            Assert.IsTrue(CalculoMinimapa.Ubicar(jugador, 90f, jugador + Vector3.forward * 30f, escala, RadioPx, false, out Vector2 norte));
            Assert.AreEqual(-30f * escala, norte.x, Tolerancia, "al norte mirando al este: a la izquierda");
            Assert.AreEqual(0f, norte.y, Tolerancia);

            // Mirando al norte, el mismo punto queda arriba; mirando al este, un punto al este también queda arriba.
            CalculoMinimapa.Ubicar(jugador, 0f, jugador + Vector3.forward * 30f, escala, RadioPx, false, out Vector2 arriba);
            Assert.AreEqual(30f * escala, arriba.y, Tolerancia);
            CalculoMinimapa.Ubicar(jugador, 90f, jugador + Vector3.right * 30f, escala, RadioPx, false, out Vector2 alFrente);
            Assert.AreEqual(0f, alFrente.x, Tolerancia);
            Assert.AreEqual(30f * escala, alFrente.y, Tolerancia, "hacia donde miras queda arriba");

            // A 100 m: el objetivo o el punto marcado quedan en el borde, en su dirección; un enemigo no sale.
            Vector3 lejos = jugador + new Vector3(60f, 0f, 80f);
            Assert.IsTrue(CalculoMinimapa.Ubicar(jugador, 0f, lejos, escala, RadioPx, true, out Vector2 borde));
            Assert.AreEqual(RadioPx, borde.magnitude, Tolerancia, "pegado al borde");
            Assert.AreEqual(0.6f, borde.normalized.x, Tolerancia, "en su dirección");
            Assert.AreEqual(0.8f, borde.normalized.y, Tolerancia);
            Assert.IsFalse(CalculoMinimapa.Ubicar(jugador, 0f, lejos, escala, RadioPx, false, out _), "un enemigo a 100 m no sale");

            // Zoom 2x: el mismo punto queda al doble de distancia del centro.
            float escala2 = CalculoMinimapa.Escala(RadioPx, RadioMetros, 2f);
            CalculoMinimapa.Ubicar(jugador, 37f, jugador + new Vector3(-12f, 0f, 9f), escala, RadioPx, false, out Vector2 uno);
            CalculoMinimapa.Ubicar(jugador, 37f, jugador + new Vector3(-12f, 0f, 9f), escala2, RadioPx, false, out Vector2 dos);
            Assert.AreEqual(uno.magnitude * 2f, dos.magnitude, Tolerancia, "zoom 2x: al doble de distancia");
            Assert.AreEqual(15f * escala, uno.magnitude, Tolerancia, "el giro no cambia la distancia");
        }
    }
}

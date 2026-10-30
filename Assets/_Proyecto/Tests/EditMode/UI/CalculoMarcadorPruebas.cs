using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-408: dónde va el rombo o la flecha del borde, con una cámara real que mira hacia +Z desde el origen.</summary>
    public class CalculoMarcadorPruebas
    {
        private const float Margen = 64f;
        private const float Tolerancia = 0.5f;
        private static readonly Vector2 Lienzo = new Vector2(1920f, 1080f);

        [Test]
        public void DentroFueraYDetras()
        {
            GameObject objeto = new GameObject("CamaraPrueba");
            try
            {
                Camera camara = objeto.AddComponent<Camera>();
                camara.aspect = Lienzo.x / Lienzo.y;
                camara.fieldOfView = 60f;

                // En pantalla: donde se proyecta.
                Vector3 enPantalla = new Vector3(2f, 1f, 10f);
                Vector3 vista = camara.WorldToViewportPoint(enPantalla);
                CalculoMarcador.Resultado r = CalculoMarcador.Calcular(vista, Lienzo, Margen);
                Assert.IsTrue(r.EnPantalla, "Enfrente y dentro se ve el rombo");
                Assert.AreEqual(vista.x * Lienzo.x, r.Posicion.x, Tolerancia);
                Assert.AreEqual(vista.y * Lienzo.y, r.Posicion.y, Tolerancia);
                Assert.Greater(r.Posicion.x, Lienzo.x * 0.5f, "Está a la derecha del centro");

                // Fuera a la derecha: al borde derecho, a 64 px, apuntando a la derecha.
                r = CalculoMarcador.Calcular(camara.WorldToViewportPoint(new Vector3(100f, 0f, 10f)), Lienzo, Margen);
                Assert.IsFalse(r.EnPantalla);
                Assert.AreEqual(Lienzo.x - Margen, r.Posicion.x, Tolerancia, "Pegada al borde derecho a 64 px");
                Assert.AreEqual(0f, Mathf.DeltaAngle(r.Angulo, 0f), 2f, "Apunta a la derecha");

                // Detrás y un poco a la derecha: borde derecho, nunca el centro.
                r = CalculoMarcador.Calcular(camara.WorldToViewportPoint(new Vector3(3f, 0f, -10f)), Lienzo, Margen);
                Assert.IsFalse(r.EnPantalla, "Detrás no hay rombo");
                Assert.AreEqual(Lienzo.x - Margen, r.Posicion.x, Tolerancia, "Detrás a la derecha va al borde derecho");
                Assert.Greater(Vector2.Distance(r.Posicion, Lienzo * 0.5f), 300f, "Nunca en el centro");

                // Detrás a la izquierda: borde izquierdo.
                r = CalculoMarcador.Calcular(camara.WorldToViewportPoint(new Vector3(-3f, 0f, -10f)), Lienzo, Margen);
                Assert.AreEqual(Margen, r.Posicion.x, Tolerancia, "Detrás a la izquierda va al borde izquierdo");
                Assert.AreEqual(180f, Mathf.Abs(Mathf.DeltaAngle(r.Angulo, 0f)), 2f, "Apunta a la izquierda");

                // Justo detrás: abajo, a 64 px del borde.
                r = CalculoMarcador.Calcular(camara.WorldToViewportPoint(new Vector3(0f, 0f, -10f)), Lienzo, Margen);
                Assert.IsFalse(r.EnPantalla);
                Assert.AreEqual(Margen, r.Posicion.y, Tolerancia, "Justo detrás va abajo");

                // Distancias.
                Assert.AreEqual("3 m", CalculoMarcador.TextoDistancia(3f));
                Assert.AreEqual("45 m", CalculoMarcador.TextoDistancia(45f));
                Assert.AreEqual("1.2 km", CalculoMarcador.TextoDistancia(1200f));
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }
    }
}

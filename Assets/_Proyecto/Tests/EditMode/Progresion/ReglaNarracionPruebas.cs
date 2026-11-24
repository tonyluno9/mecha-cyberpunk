using MechaCyberpunk.Progresion.Recorrido;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>T-cinematicas-narracion-24nov: la regla de tiempo de un clip con narración (congelar, pausa, sin acelerar ni cortar).</summary>
    public class ReglaNarracionPruebas
    {
        private const float Paso = 0.1f;

        // Avanza en pasos de 0.1 s; el video acaba a los segundosVideo. Devuelve los segundos hasta que el clip queda listo.
        private static float Correr(ReglaNarracion regla, float segundosVideo, out bool estuvoCongelado)
        {
            float t = 0f;
            estuvoCongelado = false;
            while (!regla.Listo && t < 60f)
            {
                t += Paso;
                regla.Avanzar(Paso, t >= segundosVideo - 0.001f);
                estuvoCongelado |= regla.Congelado;
            }
            return t;
        }

        [Test]
        public void Avanzar_VozMasLargaQueVideo_CongelaHastaLaVozYLuegoPausa()
        {
            var regla = new ReglaNarracion(4.3f, 1f);
            float t = Correr(regla, 2.4f, out bool congelado);
            Assert.IsTrue(congelado, "El último cuadro se queda mientras acaba la frase");
            Assert.AreEqual(5.3f, t, 0.25f, "Voz 4.3 s + 1 s de pausa");
        }

        [Test]
        public void Avanzar_VozMasCortaQueVideo_ElVideoTerminaNormalYLuegoPausa()
        {
            var regla = new ReglaNarracion(2.8f, 1f);
            float t = Correr(regla, 3f, out _);
            Assert.AreEqual(4f, t, 0.25f, "Video 3 s + 1 s de pausa");
        }

        [Test]
        public void Avanzar_SinVoz_ListoAlAcabarElVideoSinPausa()
        {
            var regla = new ReglaNarracion(0f, 1f);
            Assert.IsFalse(regla.ConVoz);
            float t = Correr(regla, 3f, out bool congelado);
            Assert.IsFalse(congelado);
            Assert.AreEqual(3f, t, 0.25f);
        }

        [Test]
        public void VozSonando_DuranteLaFrase_TrueYDespuesFalse()
        {
            var regla = new ReglaNarracion(2f, 1f);
            regla.Avanzar(1f, false);
            Assert.IsTrue(regla.VozSonando);
            regla.Avanzar(1.5f, false);
            Assert.IsFalse(regla.VozSonando);
            Assert.IsFalse(regla.Listo, "El video aún no acaba: no se corta");
        }

        [Test]
        public void Tramo_ConYSinVoz_SumaCongeladoYPausa()
        {
            Assert.AreEqual(5.3f, ReglaNarracion.Tramo(2.4f, 4.3f, 1f), 0.001f);
            Assert.AreEqual(4f, ReglaNarracion.Tramo(3f, 2.8f, 1f), 0.001f);
            Assert.AreEqual(3f, ReglaNarracion.Tramo(3f, 0f, 1f), 0.001f);
        }
    }
}

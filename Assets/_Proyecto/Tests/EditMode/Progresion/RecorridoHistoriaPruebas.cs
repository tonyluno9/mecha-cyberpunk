using MechaCyberpunk.Progresion.Recorrido;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1625: el tiempo de una cinemática (dura lo que dice y se salta) y la tabla de la historia nueva.</summary>
    public class RecorridoHistoriaPruebas
    {
        private static float Correr(ReglaCinematica regla, float paso, out float enContenido)
        {
            float total = 0f;
            enContenido = 0f;
            while (regla.Actual != ReglaCinematica.Fase.Terminada && total < 200f)
            {
                bool eraContenido = regla.Actual == ReglaCinematica.Fase.Contenido;
                regla.Avanzar(paso, false);
                if (eraContenido)
                {
                    enContenido += paso;
                }
                total += paso;
            }
            return total;
        }

        [Test]
        public void Cinematica_DuraLaDuracionMasDosFundidos()
        {
            var regla = new ReglaCinematica(9f);
            float total = Correr(regla, 0.01f, out float contenido);
            Assert.AreEqual(9f, contenido, 0.05f, "el contenido dura exactamente la duración");
            Assert.AreEqual(9f + 2f * ReglaCinematica.SegundosFundido, total, 0.05f);
        }

        [Test]
        public void Cinematica_EnterSaltaElContenidoPeroNoElFundidoDeEntrada()
        {
            var regla = new ReglaCinematica(23f);
            regla.Avanzar(0.1f, true);
            Assert.AreEqual(ReglaCinematica.Fase.FundidoEntrada, regla.Actual, "en el fundido de entrada no se salta");
            regla.Avanzar(ReglaCinematica.SegundosFundido, false);
            Assert.AreEqual(ReglaCinematica.Fase.Contenido, regla.Actual);
            Assert.IsTrue(regla.PuedeSaltar);
            regla.Avanzar(0.1f, true);
            Assert.AreEqual(ReglaCinematica.Fase.FundidoSalida, regla.Actual);
            Assert.AreEqual(1f, regla.Alfa, 0.001f, "el fundido de salida empieza en negro");
            regla.Avanzar(ReglaCinematica.SegundosFundido, false);
            Assert.AreEqual(ReglaCinematica.Fase.Terminada, regla.Actual);
        }

        [Test]
        public void Cinematica_LaBarraVaDeCeroAUnoEnElContenido()
        {
            var regla = new ReglaCinematica(10f);
            regla.Avanzar(ReglaCinematica.SegundosFundido, false);
            Assert.AreEqual(0f, regla.Progreso, 0.001f);
            regla.Avanzar(5f, false);
            Assert.AreEqual(0.5f, regla.Progreso, 0.001f);
        }

        [Test]
        public void Tabla_TieneLasNueveCinematicasConLasDuracionesDeLaHistoria()
        {
            float[] esperadas = { 9f, 23f, 22f, 5f, 4f, 8f, 6f, 6f, 4f };
            Assert.AreEqual(esperadas.Length, TablaRecorrido.Cinematicas.Length);
            for (int i = 0; i < esperadas.Length; i++)
            {
                Assert.AreEqual("C0" + (i + 1), TablaRecorrido.Cinematicas[i].Id);
                Assert.AreEqual(esperadas[i], TablaRecorrido.Cinematicas[i].Duracion, 0.001f);
                Assert.IsNotEmpty(TablaRecorrido.Cinematicas[i].Texto);
            }
        }

        [Test]
        public void Tabla_LosObjetivosCabenEnElHud()
        {
            foreach (string objetivo in TablaRecorrido.Objetivos())
            {
                Assert.LessOrEqual(objetivo.Length, 32, objetivo);
            }
        }

        [Test]
        public void Pasos_SonDiecinueveEnOrdenYLibreAlFinal()
        {
            Assert.AreEqual(1, (int)PasoRecorrido.Recuerdo);
            Assert.AreEqual(3, (int)PasoRecorrido.Molestan);
            Assert.AreEqual(14, (int)PasoRecorrido.Hacker);
            Assert.AreEqual(19, (int)PasoRecorrido.Fin);
            Assert.AreEqual(20, (int)PasoRecorrido.Libre);
        }
    }
}

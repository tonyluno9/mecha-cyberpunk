using System.Collections.Generic;
using MechaCyberpunk.Mecha.Encendido;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Mecha
{
    public class SecuenciaEncendidoPruebas
    {
        private const float Paso1 = 1f;
        private const float Mantener = 1.5f;
        private const float Paso3 = 1.2f;
        private const float Pausa = 0.3f;
        private const float Frame = 0.01f;
        private const float Margen = 0.05f;
        private const int MaxFrames = 2000;

        private SecuenciaEncendido secuencia;
        private List<PasoEncendido> pasos;
        private int completados;
        private int equivocadas;

        [SetUp]
        public void Preparar()
        {
            secuencia = new SecuenciaEncendido(Paso1, Mantener, Paso3, Pausa);
            pasos = new List<PasoEncendido>();
            completados = 0;
            equivocadas = 0;
            secuencia.PasoCambio += p => pasos.Add(p);
            secuencia.Completado += () => completados++;
            secuencia.TeclaIncorrecta += () => equivocadas++;
        }

        // Avanza segundos en frames de 0.01 s sin teclas (o con la tecla de mantener fija).
        private void Correr(float segundos, bool mantener = false)
        {
            int frames = (int)(segundos / Frame + 0.5f);
            for (int i = 0; i < frames; i++)
            {
                secuencia.Avanzar(Frame, false, mantener, false);
            }
        }

        // Tecla de un frame: confirmar o cristal.
        private void Pulsar(bool confirmar, bool cristal)
        {
            secuencia.Avanzar(Frame, confirmar, false, cristal);
        }

        // Juega la secuencia con teclas inmediatas y devuelve cuánto tardó (s).
        private float JugarInmediato()
        {
            float tiempo = 0f;
            for (int i = 0; i < MaxFrames && !secuencia.EstaEncendido; i++)
            {
                bool esperando = secuencia.EsperandoTecla;
                secuencia.Avanzar(Frame, esperando && secuencia.Paso == PasoEncendido.Paso1,
                    esperando && secuencia.Paso == PasoEncendido.Paso2,
                    esperando && secuencia.Paso == PasoEncendido.Paso3);
                tiempo += Frame;
            }
            return tiempo;
        }

        [Test]
        public void Nueva_SinReiniciar_EstaApagadaYNoAvanza()
        {
            Assert.AreEqual(PasoEncendido.Apagado, secuencia.Paso);
            secuencia.Avanzar(1f, true, true, true);
            Assert.AreEqual(PasoEncendido.Apagado, secuencia.Paso);
            Assert.IsFalse(secuencia.EstaEncendido);
        }

        [Test]
        public void Reiniciar_EmpiezaEnPaso1EsperandoSuTecla()
        {
            secuencia.Reiniciar();

            Assert.AreEqual(PasoEncendido.Paso1, secuencia.Paso);
            Assert.IsTrue(secuencia.EsperandoTecla);
            Assert.IsFalse(secuencia.EntradaConfirmada);
            Assert.AreEqual(0f, secuencia.Progreso);
            CollectionAssert.AreEqual(new[] { PasoEncendido.Paso1 }, pasos);
        }

        [Test]
        public void Avanzar_ConLasTeclasEnOrden_PasaPorLosTresPasosYCompleta()
        {
            secuencia.Reiniciar();
            JugarInmediato();

            CollectionAssert.AreEqual(new[]
            {
                PasoEncendido.Paso1, PasoEncendido.Paso2, PasoEncendido.Paso3, PasoEncendido.Encendido
            }, pasos);
            Assert.AreEqual(1, completados);
            Assert.IsTrue(secuencia.EstaEncendido);
            Assert.AreEqual(1f, secuencia.Progreso);
        }

        [Test]
        public void Avanzar_ConTeclasInmediatas_DuraLaSumaDeLaConfig()
        {
            secuencia.Reiniciar();
            float duracion = JugarInmediato();

            Assert.AreEqual(Paso1 + Mantener + Paso3 + 2f * Pausa, duracion, Margen);
        }

        [Test]
        public void Avanzar_TeclasDeOtroPasoEnPaso1_NoAvanzanYAvisan()
        {
            secuencia.Reiniciar();

            Pulsar(false, true);
            secuencia.Avanzar(Frame, false, true, false);
            Correr(3f, true);

            Assert.AreEqual(PasoEncendido.Paso1, secuencia.Paso);
            Assert.IsTrue(secuencia.EsperandoTecla);
            Assert.AreEqual(2, equivocadas, "Q y el inicio de Espacio avisan; mantener Espacio no repite el aviso");
        }

        [Test]
        public void Avanzar_TeclaDelPaso1EnPaso2_NoHaceNada()
        {
            secuencia.Reiniciar();
            Pulsar(true, false);
            Correr(Paso1 + Pausa + Frame);
            Assert.AreEqual(PasoEncendido.Paso2, secuencia.Paso);
            equivocadas = 0;

            Pulsar(true, false);
            Pulsar(false, true);

            Assert.AreEqual(PasoEncendido.Paso2, secuencia.Paso);
            Assert.AreEqual(0f, secuencia.Progreso);
            Assert.AreEqual(2, equivocadas);
        }

        [Test]
        public void Avanzar_Paso1_TeclaCorrectaAnimaYLuegoPasaAlPaso2DespuesDeLaPausa()
        {
            secuencia.Reiniciar();
            Pulsar(true, false);
            Assert.IsFalse(secuencia.EsperandoTecla);
            Assert.IsTrue(secuencia.EntradaConfirmada);

            Correr(Paso1 / 2f);
            Assert.AreEqual(0.5f, secuencia.Progreso, 0.03f);
            Assert.AreEqual(PasoEncendido.Paso1, secuencia.Paso);

            Correr(Paso1 / 2f);
            Assert.AreEqual(PasoEncendido.Paso1, secuencia.Paso, "Durante la pausa sigue en el paso 1");
            Correr(Pausa + 2f * Frame);
            Assert.AreEqual(PasoEncendido.Paso2, secuencia.Paso);
        }

        [Test]
        public void Avanzar_SoltarEspacioAntesDeTiempo_ReiniciaElAvanceDelPaso2()
        {
            secuencia.Reiniciar();
            Pulsar(true, false);
            Correr(Paso1 + Pausa + Frame);
            Assert.AreEqual(PasoEncendido.Paso2, secuencia.Paso);

            Correr(1f, true);
            Assert.Greater(secuencia.Progreso, 0.5f);
            Correr(Frame, false);
            Assert.AreEqual(0f, secuencia.Progreso, "Al soltar, el avance vuelve a 0");

            Correr(Mantener - 0.1f, true);
            Assert.AreEqual(PasoEncendido.Paso2, secuencia.Paso, "Mantener menos que el tiempo completo no alcanza");
            Correr(0.2f, true);
            Correr(Pausa + 2f * Frame);
            Assert.AreEqual(PasoEncendido.Paso3, secuencia.Paso);
        }

        [Test]
        public void Avanzar_Paso3_TeclaCorrectaAnimaYCompleta()
        {
            secuencia.Reiniciar();
            Pulsar(true, false);
            Correr(Paso1 + Pausa + Frame);
            Correr(Mantener + Pausa + 3f * Frame, true);
            Assert.AreEqual(PasoEncendido.Paso3, secuencia.Paso);
            Assert.IsTrue(secuencia.EsperandoTecla);

            Pulsar(false, true);
            Assert.AreEqual(0, completados);
            Correr(Paso3 + 2f * Frame);

            Assert.AreEqual(1, completados);
            Assert.IsTrue(secuencia.EstaEncendido);
        }

        [Test]
        public void Completar_DesdeCualquierPaso_DejaEncendidoYAvisaUnaVez()
        {
            secuencia.Reiniciar();
            secuencia.Completar();
            secuencia.Completar();

            Assert.IsTrue(secuencia.EstaEncendido);
            Assert.AreEqual(1, completados);
            Assert.AreEqual(PasoEncendido.Encendido, pasos[pasos.Count - 1]);
        }

        [Test]
        public void Reiniciar_DespuesDeEncendida_PideElEncendidoOtraVez()
        {
            secuencia.Reiniciar();
            JugarInmediato();
            Assert.IsTrue(secuencia.EstaEncendido);

            secuencia.Reiniciar();

            Assert.AreEqual(PasoEncendido.Paso1, secuencia.Paso);
            Assert.IsTrue(secuencia.EsperandoTecla);
            Assert.IsFalse(secuencia.EstaEncendido);
            Assert.AreEqual(Paso1 + Mantener + Paso3 + 2f * Pausa, JugarInmediato(), Margen);
            Assert.AreEqual(2, completados);
        }
    }
}

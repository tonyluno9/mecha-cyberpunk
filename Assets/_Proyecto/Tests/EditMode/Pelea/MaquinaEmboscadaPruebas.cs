using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Pelea.Emboscada;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Pelea
{
    public class MaquinaEmboscadaPruebas
    {
        private const float SegundosAparicion = 2f;
        private const float Paso = 0.5f;
        private const int Rivales = 2;

        private MaquinaEmboscada maquina;
        private List<EstadoEmboscada> estados;
        private int iniciadas;
        private int terminadas;

        [SetUp]
        public void SetUp()
        {
            maquina = new MaquinaEmboscada(SegundosAparicion);
            estados = new List<EstadoEmboscada>();
            iniciadas = 0;
            terminadas = 0;
            maquina.EstadoCambio += estado => estados.Add(estado);
            maquina.Iniciada += () => iniciadas++;
            maquina.Terminada += () => terminadas++;
        }

        private void PasarAparicion(ModoJugador modo)
        {
            for (float t = 0f; t < SegundosAparicion; t += Paso)
            {
                maquina.Avanzar(Paso, false, modo, Rivales);
            }
        }

        [Test]
        public void Avanzar_SinJugadorCerca_SigueEsperando()
        {
            maquina.Avanzar(10f, false, ModoJugador.APie, 0);

            Assert.AreEqual(EstadoEmboscada.Esperando, maquina.Estado);
            Assert.AreEqual(0, iniciadas);
        }

        [Test]
        public void Avanzar_JugadorCerca_PasaAAparicionYAvisaIniciada()
        {
            maquina.Avanzar(0.1f, true, ModoJugador.APie, 0);

            Assert.AreEqual(EstadoEmboscada.Aparicion, maquina.Estado);
            Assert.AreEqual(1, iniciadas);
            CollectionAssert.AreEqual(new[] { EstadoEmboscada.Aparicion }, estados);
        }

        [Test]
        public void Empezar_ManualmenteDosVeces_SoloLaPrimeraEmpieza()
        {
            Assert.IsTrue(maquina.Empezar());
            Assert.IsFalse(maquina.Empezar());

            Assert.AreEqual(1, iniciadas);
            Assert.AreEqual(EstadoEmboscada.Aparicion, maquina.Estado);
        }

        [Test]
        public void Avanzar_AparicionAPie_PasaAEsperandoMechaAlTerminarElTiempo()
        {
            maquina.Empezar();
            maquina.Avanzar(SegundosAparicion - Paso, false, ModoJugador.APie, Rivales);
            Assert.AreEqual(EstadoEmboscada.Aparicion, maquina.Estado, "Aún falta tiempo");

            maquina.Avanzar(Paso, false, ModoJugador.APie, Rivales);

            Assert.AreEqual(EstadoEmboscada.EsperandoMecha, maquina.Estado);
        }

        [Test]
        public void Avanzar_JugadorYaEnElMecha_SaltaEsperandoMecha()
        {
            maquina.Empezar();
            PasarAparicion(ModoJugador.Mecha);

            Assert.AreEqual(EstadoEmboscada.Peleando, maquina.Estado);
            CollectionAssert.DoesNotContain(estados, EstadoEmboscada.EsperandoMecha);
        }

        [Test]
        public void Avanzar_EsperandoMechaYElJugadorSube_PasaAPeleando()
        {
            maquina.Empezar();
            PasarAparicion(ModoJugador.APie);
            maquina.Avanzar(Paso, false, ModoJugador.Carro, Rivales);
            Assert.AreEqual(EstadoEmboscada.EsperandoMecha, maquina.Estado, "Un carro no cuenta");

            maquina.Avanzar(Paso, false, ModoJugador.Mecha, Rivales);

            Assert.AreEqual(EstadoEmboscada.Peleando, maquina.Estado);
        }

        [Test]
        public void Avanzar_BajarAMediaPelea_PausaYSubirReanuda()
        {
            maquina.Empezar();
            PasarAparicion(ModoJugador.Mecha);

            maquina.Avanzar(Paso, false, ModoJugador.APie, Rivales);
            Assert.AreEqual(EstadoEmboscada.Pausada, maquina.Estado);

            maquina.Avanzar(Paso, false, ModoJugador.Mecha, Rivales);
            Assert.AreEqual(EstadoEmboscada.Peleando, maquina.Estado);
        }

        [Test]
        public void Avanzar_SinRivalesEnPieMientrasPelea_TerminaUnaSolaVez()
        {
            maquina.Empezar();
            PasarAparicion(ModoJugador.Mecha);

            maquina.Avanzar(Paso, false, ModoJugador.Mecha, 0);
            maquina.Avanzar(Paso, false, ModoJugador.Mecha, 0);
            maquina.Avanzar(Paso, false, ModoJugador.APie, 0);

            Assert.AreEqual(EstadoEmboscada.Superada, maquina.Estado);
            Assert.AreEqual(1, terminadas);
        }

        [Test]
        public void Avanzar_SinRivalesEnPieEstandoPausada_Termina()
        {
            maquina.Empezar();
            PasarAparicion(ModoJugador.Mecha);
            maquina.Avanzar(Paso, false, ModoJugador.APie, Rivales);

            maquina.Avanzar(Paso, false, ModoJugador.APie, 0);

            Assert.AreEqual(EstadoEmboscada.Superada, maquina.Estado);
            Assert.AreEqual(1, terminadas);
        }

        [Test]
        public void Avanzar_SinRivalesEnPieEsperandoMecha_Termina()
        {
            maquina.Empezar();
            PasarAparicion(ModoJugador.APie);

            maquina.Avanzar(Paso, false, ModoJugador.APie, 0);

            Assert.AreEqual(EstadoEmboscada.Superada, maquina.Estado);
        }

        [Test]
        public void Avanzar_SinRivalesAunEnAparicion_NoTermina()
        {
            maquina.Empezar();

            maquina.Avanzar(Paso, false, ModoJugador.Mecha, 0);

            Assert.AreEqual(EstadoEmboscada.Aparicion, maquina.Estado, "Los rivales aparecen en esta fase: aún no cuentan");
            Assert.AreEqual(0, terminadas);
        }

        [Test]
        public void Avanzar_DespuesDeSuperada_NoVuelveAEmpezarNiAvisaDeNuevo()
        {
            maquina.Empezar();
            PasarAparicion(ModoJugador.Mecha);
            maquina.Avanzar(Paso, false, ModoJugador.Mecha, 0);

            maquina.Avanzar(Paso, true, ModoJugador.APie, Rivales);
            maquina.Avanzar(Paso, true, ModoJugador.Mecha, 0);

            Assert.AreEqual(EstadoEmboscada.Superada, maquina.Estado);
            Assert.AreEqual(1, terminadas);
            Assert.AreEqual(1, iniciadas);
            Assert.IsFalse(maquina.Empezar());
        }

        [Test]
        public void Avanzar_RecorridoCompleto_PasaPorTodosLosEstadosEnOrden()
        {
            maquina.Avanzar(Paso, true, ModoJugador.APie, 0);
            PasarAparicion(ModoJugador.APie);
            maquina.Avanzar(Paso, false, ModoJugador.Mecha, Rivales);
            maquina.Avanzar(Paso, false, ModoJugador.APie, Rivales);
            maquina.Avanzar(Paso, false, ModoJugador.Mecha, Rivales);
            maquina.Avanzar(Paso, false, ModoJugador.Mecha, 0);

            CollectionAssert.AreEqual(new[]
            {
                EstadoEmboscada.Aparicion, EstadoEmboscada.EsperandoMecha, EstadoEmboscada.Peleando,
                EstadoEmboscada.Pausada, EstadoEmboscada.Peleando, EstadoEmboscada.Superada
            }, estados);
        }
    }
}

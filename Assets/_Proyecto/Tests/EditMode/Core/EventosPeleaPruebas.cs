using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Core
{
    public class EventosPeleaPruebas
    {
        private class PeleadorFalso : IPeleador
        {
            public string Nombre => "Falso";
            public LadoPelea Lado => LadoPelea.Jugador;
            public Transform Raiz => null;
            public IVida Vida => null;
            public bool ControlActivo { get; private set; }
            public void ActivarControl(bool activo) => ControlActivo = activo;
            public void PrepararPelea(Pose inicio) { }
        }

        [SetUp]
        public void SetUp() => EventosPelea.LimpiarSuscriptores();

        [TearDown]
        public void TearDown() => EventosPelea.LimpiarSuscriptores();

        [Test]
        public void PublicarConteo_LlegaAlSuscriptorConElNumero()
        {
            int recibido = -1;
            EventosPelea.ConteoCambio += n => recibido = n;

            EventosPelea.PublicarConteo(2);

            Assert.AreEqual(2, recibido);
        }

        [Test]
        public void PublicarInicio_LlegaAlSuscriptor()
        {
            int llamadas = 0;
            EventosPelea.PeleaIniciada += () => llamadas++;

            EventosPelea.PublicarInicio();

            Assert.AreEqual(1, llamadas);
        }

        [Test]
        public void PublicarFin_LlegaAlSuscriptorConElResultado()
        {
            ResultadoPelea recibido = default;
            EventosPelea.PeleaTerminada += r => recibido = r;

            EventosPelea.PublicarFin(new ResultadoPelea(LadoPelea.Rival, 42.5f));

            Assert.AreEqual(LadoPelea.Rival, recibido.Ganador);
            Assert.AreEqual(42.5f, recibido.DuracionSegundos);
        }

        [Test]
        public void PublicarImpacto_LlegaAlSuscriptorConSusArgumentos()
        {
            IPeleador objetivoRecibido = null;
            InfoDanio infoRecibida = default;
            ResultadoDanio resultadoRecibido = default;
            EventosPelea.ImpactoRecibido += (o, i, r) =>
            {
                objetivoRecibido = o;
                infoRecibida = i;
                resultadoRecibido = r;
            };
            var objetivo = new PeleadorFalso();
            var atacante = new PeleadorFalso();
            var info = new InfoDanio(10f, TipoDanio.Golpe, new Vector3(1f, 2f, 3f), Vector3.forward, atacante);
            var resultado = new ResultadoDanio(7f, TipoBloqueo.Normal, true);

            EventosPelea.PublicarImpacto(objetivo, info, resultado);

            Assert.AreSame(objetivo, objetivoRecibido);
            Assert.AreSame(atacante, infoRecibida.Atacante);
            Assert.AreEqual(TipoDanio.Golpe, infoRecibida.Tipo);
            Assert.AreEqual(Vector3.forward, infoRecibida.Direccion);
            Assert.AreEqual(10f, infoRecibida.Cantidad);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), infoRecibida.Punto);
            Assert.AreEqual(7f, resultadoRecibido.DanioAplicado);
            Assert.AreEqual(TipoBloqueo.Normal, resultadoRecibido.Bloqueo);
            Assert.IsTrue(resultadoRecibido.CausoKO);
        }

        [Test]
        public void LimpiarSuscriptores_QuitaATodosLosSuscriptores()
        {
            int llamadas = 0;
            EventosPelea.ConteoCambio += _ => llamadas++;
            EventosPelea.PeleaIniciada += () => llamadas++;
            EventosPelea.PeleaTerminada += _ => llamadas++;
            EventosPelea.ImpactoRecibido += (_, __, ___) => llamadas++;

            EventosPelea.LimpiarSuscriptores();
            EventosPelea.PublicarConteo(1);
            EventosPelea.PublicarInicio();
            EventosPelea.PublicarFin(new ResultadoPelea(LadoPelea.Jugador, 1f));
            EventosPelea.PublicarImpacto(null, default, ResultadoDanio.Ignorado);

            Assert.AreEqual(0, llamadas);
        }
    }
}

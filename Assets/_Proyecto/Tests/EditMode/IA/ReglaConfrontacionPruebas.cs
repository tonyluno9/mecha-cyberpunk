using MechaCyberpunk.IA.APie;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.EditMode.IA
{
    /// <summary>T-1651 (T26): advertencia, empujón, pelea solo si el jugador ataca, y retirada al alejarse.</summary>
    public class ReglaConfrontacionPruebas
    {
        private static ReglaConfrontacion Crear() => new ReglaConfrontacion(new ReglaConfrontacion.Parametros(12f, 20f, 10f, 22f, 3f, 2));

        private static ReglaConfrontacion.Entrada En(float distancia, bool alerta = true, bool hostil = false)
            => new ReglaConfrontacion.Entrada { Distancia = distancia, Alerta = alerta, Hostil = hostil };

        [Test]
        public void Avanzar_JugadorEnDeteccion_EmpiezaAdvertenciaYNoPelea()
        {
            ReglaConfrontacion r = Crear();
            r.Avanzar(0.1f, En(8f));
            Assert.AreEqual(ReglaConfrontacion.Fase.Advertencia, r.Actual);
        }

        [Test]
        public void Avanzar_JugadorFueraDeDeteccion_SigueEnReposo()
        {
            ReglaConfrontacion r = Crear();
            r.Avanzar(0.1f, En(15f));
            Assert.AreEqual(ReglaConfrontacion.Fase.Reposo, r.Actual);
        }

        [Test]
        public void Avanzar_TerminaAdvertenciaConJugadorCerca_PasaAEmpujon()
        {
            ReglaConfrontacion r = Crear();
            r.Avanzar(0.1f, En(8f));
            r.Avanzar(20.1f, En(8f));
            Assert.AreEqual(ReglaConfrontacion.Fase.Empujon, r.Actual);
        }

        [Test]
        public void Avanzar_TerminaAdvertenciaConJugadorMasLejosDeQuedarse_SeRetiran()
        {
            ReglaConfrontacion r = Crear();
            r.Avanzar(0.1f, En(8f));
            r.Avanzar(20.1f, En(11f));
            Assert.AreEqual(ReglaConfrontacion.Fase.Retirada, r.Actual);
        }

        [Test]
        public void Avanzar_JugadorSeAleja_DejanDeSeguir()
        {
            ReglaConfrontacion r = Crear();
            r.Avanzar(0.1f, En(8f));
            r.Avanzar(1f, En(23f));
            Assert.AreEqual(ReglaConfrontacion.Fase.Retirada, r.Actual);
        }

        [Test]
        public void Avanzar_RetiradaYJugadorSigueEnDeteccion_NoVuelveAAdvertir()
        {
            ReglaConfrontacion r = Crear();
            r.Avanzar(0.1f, En(8f));
            r.Avanzar(1f, En(23f));
            r.Avanzar(10f, En(8f));
            Assert.AreEqual(ReglaConfrontacion.Fase.Retirada, r.Actual);
            r.Avanzar(10f, En(20f));
            Assert.AreEqual(ReglaConfrontacion.Fase.Reposo, r.Actual);
        }

        [Test]
        public void Avanzar_JugadorAtaca_PasaAPeleaYAlCalmarseVuelveAReposo()
        {
            ReglaConfrontacion r = Crear();
            r.Avanzar(0.1f, En(8f));
            r.Avanzar(0.1f, En(8f, true, true));
            Assert.AreEqual(ReglaConfrontacion.Fase.Pelea, r.Actual);
            r.Avanzar(0.1f, En(8f, true, false));
            Assert.AreEqual(ReglaConfrontacion.Fase.Reposo, r.Actual);
        }

        [Test]
        public void Avanzar_EmpujonesMaximos_SeAburrenYSeRetiran()
        {
            ReglaConfrontacion r = Crear();
            r.Avanzar(0.1f, En(8f));
            r.Avanzar(20.1f, En(8f));
            r.RegistrarEmpujon();
            r.RegistrarEmpujon();
            r.Avanzar(0.1f, En(8f));
            Assert.AreEqual(ReglaConfrontacion.Fase.Retirada, r.Actual);
        }
    }
}

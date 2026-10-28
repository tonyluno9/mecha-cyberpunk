using MechaCyberpunk.IA.APie;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-195: decisiones de la regla del pandillero a pie y turnos de la pandilla. Los números son de prueba, no de balance.</summary>
    public class ReglaPandilleroAPiePruebas
    {
        private const float Radio = 10f;
        private const float Correa = 25f;
        private const float Alcance = 2f;
        private const float Preparacion = 0.5f;
        private const float Recarga = 1.5f;
        private const float Llegada = 0.5f;
        private const float Paso = 0.1f;

        private static ReglaPandilleroAPie Nueva() => new ReglaPandilleroAPie(Radio, Correa, Alcance, Preparacion, Recarga, Llegada);

        private static ReglaPandilleroAPie.Entrada Ve(float distancia, bool atacable = true, bool turno = true, float jugadorAlPunto = -1f, float alPunto = 0f)
        {
            return new ReglaPandilleroAPie.Entrada
            {
                DistanciaJugador = distancia,
                DistanciaJugadorAlPunto = jugadorAlPunto >= 0f ? jugadorAlPunto : distancia,
                DistanciaAlPunto = alPunto,
                JugadorAtacable = atacable,
                TieneTurno = turno
            };
        }

        [Test]
        public void Avanzar_JugadorFueraDelRadio_SigueEsperando()
        {
            ReglaPandilleroAPie regla = Nueva();
            ReglaPandilleroAPie.Salida salida = regla.Avanzar(Paso, Ve(Radio + 1f));
            Assert.AreEqual(EstadoPandillero.Esperando, regla.Estado);
            Assert.AreEqual(ReglaPandilleroAPie.Orden.Quieto, salida.Orden);
        }

        [Test]
        public void Avanzar_JugadorDentroDelRadio_PersigueAlJugador()
        {
            ReglaPandilleroAPie regla = Nueva();
            ReglaPandilleroAPie.Salida salida = regla.Avanzar(Paso, Ve(Radio - 1f));
            Assert.AreEqual(EstadoPandillero.Persiguiendo, regla.Estado);
            Assert.AreEqual(ReglaPandilleroAPie.Orden.AlJugador, salida.Orden);
        }

        [Test]
        public void Avanzar_AlAlcanceConTurno_AvisaYLuegoGolpeaUnaVez()
        {
            ReglaPandilleroAPie regla = Nueva();
            regla.Avanzar(Paso, Ve(Alcance - 0.5f));
            Assert.AreEqual(EstadoPandillero.Preparando, regla.Estado, "al alcance con turno pasa al aviso");
            int impactos = 0;
            float tiempo = 0f;
            while (regla.Estado == EstadoPandillero.Preparando && tiempo < Preparacion * 3f)
            {
                if (regla.Avanzar(Paso, Ve(Alcance - 0.5f)).Impacto) impactos++;
                tiempo += Paso;
            }
            Assert.AreEqual(1, impactos, "un solo impacto");
            Assert.AreEqual(EstadoPandillero.Recargando, regla.Estado);
            Assert.AreEqual(Preparacion, tiempo, Paso * 1.5f, "el impacto llega tras la preparación");
        }

        [Test]
        public void Avanzar_Recargando_NoGolpeaHastaTerminarLaRecarga()
        {
            ReglaPandilleroAPie regla = Nueva();
            float tiempo = 0f;
            int impactos = 0;
            float segundoImpacto = -1f;
            for (int i = 0; i < 100 && segundoImpacto < 0f; i++)
            {
                if (regla.Avanzar(Paso, Ve(1f)).Impacto)
                {
                    impactos++;
                    if (impactos == 2) segundoImpacto = tiempo;
                }
                tiempo += Paso;
            }
            Assert.AreEqual(Preparacion + Recarga + Preparacion, segundoImpacto, Paso * 3f, "entre golpes pasan recarga y aviso");
        }

        [Test]
        public void Avanzar_AlAlcanceSinTurno_EsperaQuietoSinAvisar()
        {
            ReglaPandilleroAPie regla = Nueva();
            ReglaPandilleroAPie.Salida salida = regla.Avanzar(Paso, Ve(1f, true, false));
            Assert.AreEqual(EstadoPandillero.Persiguiendo, regla.Estado);
            Assert.AreEqual(ReglaPandilleroAPie.Orden.Quieto, salida.Orden);
            Assert.IsFalse(regla.Atacando);
        }

        [Test]
        public void Avanzar_JugadorX_SeRindeYVuelve()
        {
            ReglaPandilleroAPie regla = Nueva();
            regla.Avanzar(Paso, Ve(5f));
            ReglaPandilleroAPie.Salida salida = regla.Avanzar(Paso, Ve(Correa + 5f, true, true, Correa + 1f, 20f));
            Assert.AreEqual(EstadoPandillero.Volviendo, regla.Estado);
            Assert.AreEqual(ReglaPandilleroAPie.Orden.AlPunto, salida.Orden);
        }

        [Test]
        public void Avanzar_Volviendo_NoVuelveAPerseguirHastaLlegar()
        {
            ReglaPandilleroAPie regla = Nueva();
            regla.Avanzar(Paso, Ve(5f));
            regla.Avanzar(Paso, Ve(Correa + 5f, true, true, Correa + 1f, 20f));
            regla.Avanzar(Paso, Ve(3f, true, true, 3f, 10f));
            Assert.AreEqual(EstadoPandillero.Volviendo, regla.Estado, "aunque el jugador regrese, termina de volver");
            regla.Avanzar(Paso, Ve(30f, true, true, 30f, Llegada - 0.1f));
            Assert.AreEqual(EstadoPandillero.Esperando, regla.Estado);
        }

        [Test]
        public void Avanzar_JugadorEnKO_NoAtacaYVuelve()
        {
            ReglaPandilleroAPie regla = Nueva();
            regla.Avanzar(Paso, Ve(5f));
            ReglaPandilleroAPie.Salida salida = regla.Avanzar(Paso, Ve(1f, false));
            Assert.AreEqual(EstadoPandillero.Volviendo, regla.Estado);
            Assert.IsFalse(salida.Impacto);
        }

        [Test]
        public void Avanzar_JugadorNoAtacableDesdeElPrincipio_NoPersigue()
        {
            // Cubre carro y mecha: el que llama manda JugadorAtacable = false.
            ReglaPandilleroAPie regla = Nueva();
            ReglaPandilleroAPie.Salida salida = regla.Avanzar(Paso, Ve(2f, false));
            Assert.AreEqual(EstadoPandillero.Esperando, regla.Estado);
            Assert.AreEqual(ReglaPandilleroAPie.Orden.Quieto, salida.Orden);
        }

        [Test]
        public void Avanzar_JugadorSubeAlMechaEnElAviso_CancelaElGolpe()
        {
            ReglaPandilleroAPie regla = Nueva();
            regla.Avanzar(Paso, Ve(1f));
            Assert.AreEqual(EstadoPandillero.Preparando, regla.Estado);
            bool impacto = false;
            for (int i = 0; i < 20; i++)
            {
                impacto |= regla.Avanzar(Paso, Ve(1f, false)).Impacto;
            }
            Assert.IsFalse(impacto, "sin jugador atacable no hay impacto");
            Assert.AreNotEqual(EstadoPandillero.Preparando, regla.Estado);
        }

        [Test]
        public void ProgresoAviso_DuranteLaPreparacion_SubeDeCeroAUno()
        {
            ReglaPandilleroAPie regla = Nueva();
            regla.Avanzar(Paso, Ve(1f));
            Assert.AreEqual(0f, regla.ProgresoAviso, 0.0001f);
            regla.Avanzar(Preparacion / 2f, Ve(1f));
            Assert.AreEqual(0.5f, regla.ProgresoAviso, 0.01f);
        }

        [Test]
        public void Turnos_TresPidenConMaximoDos_SoloDosLosTienen()
        {
            TurnosPandillaAPie turnos = new TurnosPandillaAPie(2);
            object a = new object(), b = new object(), c = new object();
            Assert.IsTrue(turnos.Pedir(a));
            Assert.IsTrue(turnos.Pedir(b));
            Assert.IsFalse(turnos.Pedir(c), "el tercero espera");
            Assert.IsTrue(turnos.Pedir(a), "quien ya tiene turno lo conserva");
            Assert.AreEqual(2, turnos.Cantidad);
            turnos.Soltar(a);
            Assert.IsTrue(turnos.Pedir(c), "al soltar uno, entra el tercero");
            Assert.AreEqual(2, turnos.Cantidad);
        }
    }
}

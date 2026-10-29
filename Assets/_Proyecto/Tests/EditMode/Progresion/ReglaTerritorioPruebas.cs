using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Progresion
{
    public class ReglaTerritorioPruebas
    {
        private const int Fosa = 0;
        private const int Tianguis = 1;
        private const int Neon = 2;

        private static ReglaTerritorio Nueva()
        {
            return new ReglaTerritorio(new[] { (int)IdPandilla.Jugador, (int)IdPandilla.Calaveras, (int)IdPandilla.Viboras }, (int)IdPandilla.Jugador);
        }

        [Test]
        public void RespetoYConquista()
        {
            ReglaTerritorio regla = Nueva();
            for (int i = 0; i < 6; i++)
            {
                regla.RegistrarVencido(Tianguis, false);
            }
            Assert.IsFalse(regla.JefeDisponible(Tianguis), "Con 60 todavía no sale el jefe.");
            regla.RegistrarVencido(Tianguis, false);
            Assert.AreEqual(70, regla.Respeto(Tianguis));
            Assert.IsTrue(regla.JefeDisponible(Tianguis));
            Assert.AreEqual(14, regla.Respeto(Fosa), "LA FOSA sube 2 por cada vencido.");
            Assert.AreEqual(0, regla.Respeto(Neon));

            // Una caída en el tianguis antes del jefe baja el respeto y el jefe se va.
            regla.RegistrarCaida(Tianguis);
            Assert.AreEqual(60, regla.Respeto(Tianguis));
            Assert.IsFalse(regla.JefeDisponible(Tianguis));
            regla.RegistrarVencido(Tianguis, false);
            Assert.IsTrue(regla.JefeDisponible(Tianguis));

            // Vencer al jefe: la zona es tuya, respeto 100 y LA FOSA +2 +20.
            bool conquistada = regla.RegistrarVencido(Tianguis, true);
            Assert.IsTrue(conquistada);
            Assert.AreEqual((int)IdPandilla.Jugador, regla.Dueno(Tianguis));
            Assert.AreEqual(100, regla.Respeto(Tianguis));
            Assert.AreEqual(14 + 2 + 2 + 20, regla.Respeto(Fosa));
            Assert.IsFalse(regla.JefeDisponible(Tianguis));
        }

        [Test]
        public void Tope_NoPasaDe100NiBajaDe0()
        {
            ReglaTerritorio regla = Nueva();
            regla.RegistrarCaida(Neon);
            Assert.AreEqual(0, regla.Respeto(Neon));
            for (int i = 0; i < 20; i++)
            {
                regla.RegistrarVencido(Neon, false);
            }
            Assert.AreEqual(100, regla.Respeto(Neon));
            Assert.AreEqual(40, regla.Respeto(Fosa));
        }
    }
}

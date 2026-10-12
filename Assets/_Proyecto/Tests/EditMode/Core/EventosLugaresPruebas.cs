using MechaCyberpunk.Core;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Core
{
    public class EventosLugaresPruebas
    {
        [SetUp]
        public void SetUp() => EventosLugares.LimpiarSuscriptores();

        [TearDown]
        public void TearDown() => EventosLugares.LimpiarSuscriptores();

        [Test]
        public void PublicarEntro_LlegaConSuLugar()
        {
            IdLugar recibido = IdLugar.Ninguno;
            EventosLugares.JugadorEntro += l => recibido = l;

            EventosLugares.PublicarEntro(IdLugar.Bar);

            Assert.AreEqual(IdLugar.Bar, recibido);
        }

        [Test]
        public void PublicarSalio_LlegaConSuLugar()
        {
            IdLugar recibido = IdLugar.Ninguno;
            EventosLugares.JugadorSalio += l => recibido = l;

            EventosLugares.PublicarSalio(IdLugar.Casa);

            Assert.AreEqual(IdLugar.Casa, recibido);
        }

        [Test]
        public void PublicarEstadoLugar_LlegaConLugarYValor()
        {
            IdLugar lugar = IdLugar.Ninguno;
            bool abierto = false;
            int llamadas = 0;
            EventosLugares.EstadoLugarCambio += (l, a) => { lugar = l; abierto = a; llamadas++; };

            EventosLugares.PublicarEstadoLugar(IdLugar.Casino, true);

            Assert.AreEqual(1, llamadas);
            Assert.AreEqual(IdLugar.Casino, lugar);
            Assert.IsTrue(abierto);

            EventosLugares.PublicarEstadoLugar(IdLugar.MercadoNegro, false);

            Assert.AreEqual(IdLugar.MercadoNegro, lugar);
            Assert.IsFalse(abierto);
        }

        [Test]
        public void LimpiarSuscriptores_QuitaATodos()
        {
            int llamadas = 0;
            EventosLugares.JugadorEntro += _ => llamadas++;
            EventosLugares.JugadorSalio += _ => llamadas++;
            EventosLugares.EstadoLugarCambio += (_, __) => llamadas++;

            EventosLugares.LimpiarSuscriptores();
            EventosLugares.PublicarEntro(IdLugar.Taller);
            EventosLugares.PublicarSalio(IdLugar.Taller);
            EventosLugares.PublicarEstadoLugar(IdLugar.Taller, true);

            Assert.AreEqual(0, llamadas);
        }
    }
}

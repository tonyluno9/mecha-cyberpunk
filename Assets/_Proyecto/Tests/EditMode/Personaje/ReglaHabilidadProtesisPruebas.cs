using MechaCyberpunk.Personaje.Habilidades;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-992: estados Lista, Activa y Recargando de una habilidad de prótesis, duración por nivel y medidor.</summary>
    public class ReglaHabilidadProtesisPruebas
    {
        private static ReglaHabilidadProtesis Piernas() => new ReglaHabilidadProtesis(6f, 10f, 20f);

        [Test]
        public void Nueva_EstaListaYConElMedidorLleno()
        {
            ReglaHabilidadProtesis r = Piernas();
            Assert.AreEqual(EstadoHabilidad.Lista, r.Estado);
            Assert.AreEqual(1f, r.Medidor, 1e-4f);
        }

        [Test]
        public void Activar_SinImplante_NoHaceNada()
        {
            ReglaHabilidadProtesis r = Piernas();
            Assert.IsFalse(r.Activar(0));
            Assert.AreEqual(EstadoHabilidad.Lista, r.Estado);
        }

        [Test]
        public void Activar_NivelI_DuraSeisSegundos_Luego_Recarga20_YVuelveALista()
        {
            ReglaHabilidadProtesis r = Piernas();
            Assert.IsTrue(r.Activar(1));
            Assert.AreEqual(EstadoHabilidad.Activa, r.Estado);
            r.Avanzar(5.9f);
            Assert.AreEqual(EstadoHabilidad.Activa, r.Estado);
            r.Avanzar(0.2f);
            Assert.AreEqual(EstadoHabilidad.Recargando, r.Estado);
            r.Avanzar(19.9f);
            Assert.AreEqual(EstadoHabilidad.Recargando, r.Estado);
            r.Avanzar(0.2f);
            Assert.AreEqual(EstadoHabilidad.Lista, r.Estado);
        }

        [Test]
        public void Activar_NivelII_DuraDiezSegundos()
        {
            ReglaHabilidadProtesis r = Piernas();
            Assert.IsTrue(r.Activar(2));
            r.Avanzar(9.9f);
            Assert.AreEqual(EstadoHabilidad.Activa, r.Estado);
            r.Avanzar(0.2f);
            Assert.AreEqual(EstadoHabilidad.Recargando, r.Estado);
            Assert.AreEqual(6f, Piernas().Duracion(1), 1e-4f);
            Assert.AreEqual(10f, Piernas().Duracion(2), 1e-4f);
            Assert.AreEqual(0f, Piernas().Duracion(0), 1e-4f);
        }

        [Test]
        public void Activar_YaActivaORecargando_NoSePuede()
        {
            ReglaHabilidadProtesis r = Piernas();
            Assert.IsTrue(r.Activar(1));
            Assert.IsFalse(r.Activar(1), "ya activa");
            r.Avanzar(6.5f);
            Assert.IsFalse(r.Activar(1), "recargando");
        }

        [Test]
        public void Medidor_SeVaciaActiva_YSeLlenaRecargando()
        {
            ReglaHabilidadProtesis r = Piernas();
            r.Activar(1);
            Assert.AreEqual(1f, r.Medidor, 1e-4f);
            r.Avanzar(3f);
            Assert.AreEqual(0.5f, r.Medidor, 1e-4f);
            r.Avanzar(3.0f);
            Assert.AreEqual(EstadoHabilidad.Recargando, r.Estado);
            Assert.AreEqual(0f, r.Medidor, 1e-3f);
            r.Avanzar(10f);
            Assert.AreEqual(0.5f, r.Medidor, 1e-3f);
            Assert.That(r.Medidor, Is.InRange(0f, 1f));
        }

        [Test]
        public void Cortar_ActivaPasaARecargaCompleta_ListaNoCambia()
        {
            ReglaHabilidadProtesis r = Piernas();
            r.Cortar();
            Assert.AreEqual(EstadoHabilidad.Lista, r.Estado);
            r.Activar(1);
            r.Avanzar(2f);
            r.Cortar();
            Assert.AreEqual(EstadoHabilidad.Recargando, r.Estado);
            Assert.AreEqual(20f, r.Restante, 1e-4f);
        }

        [Test]
        public void Avanzar_ConUnPasoGrande_NoSaltaFasesRaras()
        {
            ReglaHabilidadProtesis r = Piernas();
            r.Activar(1);
            r.Avanzar(100f);
            Assert.AreEqual(EstadoHabilidad.Recargando, r.Estado);
        }
    }
}

using MechaCyberpunk.UI.Hud;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-523: la cola de avisos del centro muestra uno a la vez, por prioridad y en orden.</summary>
    public class ReglaColaAvisosPruebas
    {
        private static ReglaColaAvisos Nueva(int tope = 5) => new ReglaColaAvisos(2f, 3.5f, tope);

        private static void Poner(ReglaColaAvisos cola, string texto, PrioridadAviso prioridad = PrioridadAviso.Normal, float segundos = 0f, string clave = null)
        {
            cola.Publicar(texto, prioridad, TipoAviso.Neutro, segundos, clave);
        }

        [Test]
        public void Publicar_UnAviso_EsElActual()
        {
            ReglaColaAvisos cola = Nueva();
            Poner(cola, "A");
            Assert.IsTrue(cola.HayActual);
            Assert.AreEqual("A", cola.Actual.Texto);
            Assert.AreEqual(0, cola.EnEspera);
        }

        [Test]
        public void Publicar_Varios_SoloUnoALaVezYEnOrden()
        {
            ReglaColaAvisos cola = Nueva();
            Poner(cola, "A");
            Poner(cola, "B");
            Poner(cola, "C");
            Assert.AreEqual("A", cola.Actual.Texto);
            Assert.AreEqual(2, cola.EnEspera);
            cola.Avanzar(1f);
            Assert.AreEqual("A", cola.Actual.Texto, "sigue el mismo hasta cumplir su tiempo");
            cola.Avanzar(1.1f);
            Assert.AreEqual("B", cola.Actual.Texto);
            cola.Avanzar(2.1f);
            Assert.AreEqual("C", cola.Actual.Texto);
            cola.Avanzar(2.1f);
            Assert.IsFalse(cola.HayActual);
        }

        [Test]
        public void Publicar_ConPrioridad_PasaAntesQueLosDeMenosPrioridadEnEspera()
        {
            ReglaColaAvisos cola = Nueva();
            Poner(cola, "actual", PrioridadAviso.Baja);
            Poner(cola, "baja", PrioridadAviso.Baja);
            Poner(cola, "alta", PrioridadAviso.Alta);
            Poner(cola, "normal", PrioridadAviso.Normal);
            CollectionAssert.AreEqual(new[] { "alta", "normal", "baja" }, cola.TextosEnEspera);
            Assert.AreEqual("actual", cola.Actual.Texto, "no se interrumpe al que ya se ve");
            cola.Avanzar(2.1f);
            Assert.AreEqual("alta", cola.Actual.Texto);
        }

        [Test]
        public void Publicar_DuracionLargaOCero_SeLimita()
        {
            ReglaColaAvisos cola = Nueva();
            Poner(cola, "larga", PrioridadAviso.Normal, 10f);
            Assert.AreEqual(3.5f, cola.Actual.Segundos, 0.001f);
            Poner(cola, "corta");
            cola.Avanzar(3.6f);
            Assert.AreEqual(2f, cola.Actual.Segundos, 0.001f);
        }

        [Test]
        public void Publicar_TextoRepetido_NoSeDuplica()
        {
            ReglaColaAvisos cola = Nueva();
            Poner(cola, "A");
            Poner(cola, "A");
            Poner(cola, "B");
            Poner(cola, "B");
            Assert.AreEqual(1, cola.EnEspera);
        }

        [Test]
        public void Publicar_ConClave_ReemplazaElTextoEnEsperaYEnPantalla()
        {
            ReglaColaAvisos cola = Nueva();
            Poner(cola, "ocupado");
            Poner(cola, "BARRIO +5", PrioridadAviso.Normal, 0f, "reputacion");
            Poner(cola, "BARRIO +9", PrioridadAviso.Normal, 0f, "reputacion");
            Assert.AreEqual(1, cola.EnEspera);
            cola.Avanzar(2.1f);
            Assert.AreEqual("BARRIO +9", cola.Actual.Texto);
            Poner(cola, "BARRIO +12", PrioridadAviso.Normal, 0f, "reputacion");
            Assert.AreEqual("BARRIO +12", cola.Actual.Texto);
            Assert.AreEqual(0, cola.EnEspera);
        }

        [Test]
        public void Publicar_ColaLlena_DescartaElMenosImportanteYMasViejo()
        {
            ReglaColaAvisos cola = Nueva(2);
            Poner(cola, "actual");
            Poner(cola, "baja vieja", PrioridadAviso.Baja);
            Poner(cola, "baja nueva", PrioridadAviso.Baja);
            Poner(cola, "alta", PrioridadAviso.Alta);
            CollectionAssert.AreEqual(new[] { "alta", "baja nueva" }, cola.TextosEnEspera);
        }

        [Test]
        public void Avanzar_ConCero_NoCorreElTiempo()
        {
            ReglaColaAvisos cola = Nueva();
            Poner(cola, "A");
            Poner(cola, "B");
            cola.Avanzar(0f);
            cola.Avanzar(0f);
            Assert.AreEqual("A", cola.Actual.Texto);
        }

        [Test]
        public void Publicar_TextoVacio_Ignora()
        {
            ReglaColaAvisos cola = Nueva();
            Poner(cola, string.Empty, PrioridadAviso.Alta);
            Poner(cola, null, PrioridadAviso.Alta);
            Assert.IsFalse(cola.HayActual);
        }
    }
}

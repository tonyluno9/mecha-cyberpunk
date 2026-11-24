using MechaCyberpunk.Casino.Meseras;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Casino
{
    /// <summary>T-voces-personajes-23nov: la mesera robot pide su voz grabada en cada momento, con distancias y enfriamientos.</summary>
    public class VozMeseraPruebas
    {
        [Test]
        public void Ids_SonLosGrabados()
        {
            Assert.That(ReglaVozMesera.Saludo, Is.EqualTo("MESERA_ROBOT_SALUDO"));
            Assert.That(ReglaVozMesera.Servir, Is.EqualTo("MESERA_ROBOT_SERVIR"));
            Assert.That(ReglaVozMesera.Suerte, Is.EqualTo("MESERA_ROBOT_SUERTE"));
            Assert.That(ReglaVozMesera.Despedida, Is.EqualTo("MESERA_ROBOT_DESPEDIDA"));
        }

        [Test]
        public void Servir_SoloAlDetenerseConElJugadorCerca()
        {
            Assert.That(ReglaVozMesera.TocaServir(true, false, 4f, 100f, float.NegativeInfinity), Is.True);
            Assert.That(ReglaVozMesera.TocaServir(false, false, 4f, 100f, float.NegativeInfinity), Is.False, "ya estaba parada");
            Assert.That(ReglaVozMesera.TocaServir(true, false, 10f, 100f, float.NegativeInfinity), Is.False, "jugador lejos");
            Assert.That(ReglaVozMesera.TocaServir(true, false, 4f, 100f, 90f), Is.False, "enfriamiento");
        }

        [Test]
        public void Saludo_AlPasarCerca_ConEnfriamiento()
        {
            Assert.That(ReglaVozMesera.TocaSaludo(2f, 100f, float.NegativeInfinity), Is.True);
            Assert.That(ReglaVozMesera.TocaSaludo(5f, 100f, float.NegativeInfinity), Is.False);
            Assert.That(ReglaVozMesera.TocaSaludo(2f, 100f, 80f), Is.False);
        }

        [Test]
        public void MasCercana_EligeLaDeMenorDistanciaDentroDelRadio()
        {
            Vector3[] meseras = { new Vector3(10f, 0f, 0f), new Vector3(3f, 0f, 0f), new Vector3(-20f, 0f, 0f) };
            Assert.That(ReglaVozMesera.MasCercana(meseras, Vector3.zero, ReglaVozMesera.DistanciaSuerte), Is.EqualTo(1));
            Assert.That(ReglaVozMesera.MasCercana(meseras, new Vector3(0f, 0f, 50f), ReglaVozMesera.DistanciaSuerte), Is.EqualTo(-1));
        }
    }
}

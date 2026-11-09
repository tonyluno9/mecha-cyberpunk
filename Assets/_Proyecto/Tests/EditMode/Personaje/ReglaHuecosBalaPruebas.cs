using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-808: metal o concreto y cómo se apaga un hueco de bala.</summary>
    public class ReglaHuecosBalaPruebas
    {
        private static readonly string[] Palabras = { "carro", "metal", "patrulla" };

        [Test]
        public void EsMetal_NombreConCarroOPatrulla_EsMetal()
        {
            Assert.IsTrue(ReglaHuecosBala.EsMetal(new[] { "Puerta", "V03_CarroTaxi" }, false, false, Palabras));
            Assert.IsTrue(ReglaHuecosBala.EsMetal(new[] { "PATRULLA_01" }, false, false, Palabras), "sin distinguir mayúsculas");
            Assert.IsTrue(ReglaHuecosBala.EsMetal(new[] { "Lamina_Metal" }, false, false, Palabras));
        }

        [Test]
        public void EsMetal_CuerpoRigidoOMontable_EsMetal()
        {
            Assert.IsTrue(ReglaHuecosBala.EsMetal(new[] { "Caja" }, true, false, Palabras));
            Assert.IsTrue(ReglaHuecosBala.EsMetal(new[] { "Caja" }, false, true, Palabras));
        }

        [Test]
        public void EsMetal_ParedComun_EsConcreto()
        {
            Assert.IsFalse(ReglaHuecosBala.EsMetal(new[] { "Pared", "Edificio_03", "Greybox_Concreto" }, false, false, Palabras));
            Assert.IsFalse(ReglaHuecosBala.EsMetal(null, false, false, Palabras));
        }

        [Test]
        public void Alfa_ConElTiempo_SeVeYLuegoSeApaga()
        {
            Assert.AreEqual(1f, ReglaHuecosBala.Alfa(5f, 10f, 2f));
            Assert.AreEqual(0.5f, ReglaHuecosBala.Alfa(11f, 10f, 2f), 0.0001f);
            Assert.AreEqual(0f, ReglaHuecosBala.Alfa(13f, 10f, 2f));
        }

        [Test]
        public void HayHueco_SoloSiNoEsTriggerNiPersonaYHaySuperficieVisible()
        {
            Assert.IsTrue(ReglaHuecosBala.HayHueco(false, false, true));
            Assert.IsFalse(ReglaHuecosBala.HayHueco(true, false, true), "un trigger no lleva hueco");
            Assert.IsFalse(ReglaHuecosBala.HayHueco(false, true, true), "una persona no lleva hueco");
            Assert.IsFalse(ReglaHuecosBala.HayHueco(false, false, false), "sin superficie visible no hay hueco");
        }

        [Test]
        public void PuntoEnCaja_ConMargenDe5cm_AceptaElBordeYRechazaMasAlla()
        {
            Vector3 centro = new Vector3(0f, 1f, 0f);
            Vector3 extension = new Vector3(1f, 1f, 0.1f);
            Assert.IsTrue(ReglaHuecosBala.PuntoEnCaja(centro, extension, new Vector3(0.5f, 1f, 0.1f), ReglaHuecosBala.MargenSuperficie), "en la cara");
            Assert.IsTrue(ReglaHuecosBala.PuntoEnCaja(centro, extension, new Vector3(0.5f, 1f, 0.14f), ReglaHuecosBala.MargenSuperficie), "a 4 cm, dentro del margen");
            Assert.IsFalse(ReglaHuecosBala.PuntoEnCaja(centro, extension, new Vector3(0.5f, 1f, 0.2f), ReglaHuecosBala.MargenSuperficie), "a 10 cm, flotando");
            Assert.IsFalse(ReglaHuecosBala.PuntoEnCaja(centro, extension, new Vector3(1.2f, 1f, 0f), ReglaHuecosBala.MargenSuperficie), "fuera de lado");
        }

        [Test]
        public void SeMovio_MasDe2cm_SiNoEsEstable()
        {
            Vector3 donde = new Vector3(3f, 1f, 2f);
            Assert.IsFalse(ReglaHuecosBala.SeMovio(donde, donde + Vector3.up * 0.019f));
            Assert.IsTrue(ReglaHuecosBala.SeMovio(donde, donde + Vector3.up * 0.021f));
            Assert.IsTrue(ReglaHuecosBala.SeMovio(donde, donde + new Vector3(0.02f, 0.02f, 0f)), "en diagonal");
        }
    }
}

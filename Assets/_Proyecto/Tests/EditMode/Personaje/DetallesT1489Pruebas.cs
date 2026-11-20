using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1489: reglas puras de polvo, charcos, temblor, viñeta de vida baja e indicador de dirección del daño.</summary>
    public class DetallesT1489Pruebas
    {
        [Test]
        public void Polvo_CaidaCorta_NoHayParticulas()
        {
            Assert.AreEqual(0, ReglaEfectosPie.ParticulasPolvo(0.5f));
            Assert.AreEqual(0f, ReglaEfectosPie.IntensidadPolvo(0.99f));
        }

        [Test]
        public void Polvo_CaidaMayor_EmiteMasParticulas()
        {
            int poca = ReglaEfectosPie.ParticulasPolvo(1.5f);
            int mucha = ReglaEfectosPie.ParticulasPolvo(4f);
            Assert.Greater(poca, 0);
            Assert.Greater(mucha, poca);
            Assert.AreEqual(ReglaEfectosPie.PolvoMaximo, ReglaEfectosPie.ParticulasPolvo(50f));
        }

        [Test]
        public void Charco_DentroYCerca_Pisa()
        {
            Assert.IsTrue(ReglaEfectosPie.PisaCharco(0.1f, -0.2f, 0.01f));
        }

        [Test]
        public void Charco_FueraDelRadio_NoPisa()
        {
            Assert.IsFalse(ReglaEfectosPie.PisaCharco(0.45f, 0.3f, 0f));
        }

        [Test]
        public void Charco_MuyAlto_NoPisa()
        {
            Assert.IsFalse(ReglaEfectosPie.PisaCharco(0f, 0f, 2f));
        }

        [Test]
        public void Temblor_EscalaConElDanioEntre04Y15()
        {
            Assert.AreEqual(0f, ReglaEfectosPie.FuerzaTemblorDanio(0f));
            Assert.AreEqual(0.4f, ReglaEfectosPie.FuerzaTemblorDanio(1f), 1e-4f);
            Assert.AreEqual(1.5f, ReglaEfectosPie.FuerzaTemblorDanio(500f), 1e-4f);
            Assert.Greater(ReglaEfectosPie.FuerzaTemblorDanio(20f), ReglaEfectosPie.FuerzaTemblorDanio(5f));
        }

        [Test]
        public void Vinieta_VidaSobreUmbral_NoSeVe()
        {
            Assert.AreEqual(0f, ReglaVinietaVida.Alfa(31f, 100f, 0.3f));
            Assert.AreEqual(0f, ReglaVinietaVida.Alfa(100f, 100f, 1f));
        }

        [Test]
        public void Vinieta_MenosVida_EsMasFuerte()
        {
            Assert.Greater(ReglaVinietaVida.AlfaBase(10f, 100f), ReglaVinietaVida.AlfaBase(25f, 100f));
            Assert.Greater(ReglaVinietaVida.AlfaBase(25f, 100f), 0f);
            Assert.AreEqual(ReglaVinietaVida.AlfaMaxima, ReglaVinietaVida.AlfaBase(0.0001f, 100f), 0.01f);
        }

        [Test]
        public void Vinieta_Pulsa_ConValleYCresta()
        {
            float minimo = 1f, maximo = 0f;
            for (float t = 0f; t < 2f; t += 0.01f)
            {
                float a = ReglaVinietaVida.Alfa(10f, 100f, t);
                minimo = Mathf.Min(minimo, a);
                maximo = Mathf.Max(maximo, a);
            }
            Assert.Less(minimo, maximo - 0.05f);
            Assert.Greater(minimo, 0f);
        }

        [Test]
        public void Vinieta_AlCurarse_VuelveACero()
        {
            Assert.Greater(ReglaVinietaVida.Alfa(10f, 100f, 0.4f), 0f);
            Assert.AreEqual(0f, ReglaVinietaVida.Alfa(60f, 100f, 0.4f));
        }

        [Test]
        public void Indicador_GolpeDeFrente_ApuntaAlFrente()
        {
            // El golpe viaja hacia el jugador (que mira a +Z), o sea en -Z: la fuente está enfrente.
            Assert.IsTrue(ReglaIndicadorDanio.Angulo(Vector3.forward, Vector3.back, out float g));
            Assert.AreEqual(0f, g, 0.01f);
        }

        [Test]
        public void Indicador_GolpeDesdeLaDerecha_Da90()
        {
            Assert.IsTrue(ReglaIndicadorDanio.Angulo(Vector3.forward, Vector3.left, out float g));
            Assert.AreEqual(90f, g, 0.01f);
        }

        [Test]
        public void Indicador_CamaraGirada_ApuntaEnCoordenadasDeCamara()
        {
            // Mira a la derecha (+X); la bala viene desde +Z hacia -Z: la fuente queda a su izquierda.
            Assert.IsTrue(ReglaIndicadorDanio.Angulo(Vector3.right, Vector3.back, out float g));
            Assert.AreEqual(-90f, g, 0.01f);
        }

        [Test]
        public void Indicador_CaidaVertical_NoHayDireccion()
        {
            Assert.IsFalse(ReglaIndicadorDanio.Angulo(Vector3.forward, Vector3.down, out _));
        }

        [Test]
        public void Indicador_Alfa_SeDesvanece()
        {
            Assert.AreEqual(1f, ReglaIndicadorDanio.Alfa(0f, 1f), 1e-4f);
            Assert.AreEqual(0.5f, ReglaIndicadorDanio.Alfa(0.5f, 1f), 1e-4f);
            Assert.AreEqual(0f, ReglaIndicadorDanio.Alfa(2f, 1f));
        }
    }
}

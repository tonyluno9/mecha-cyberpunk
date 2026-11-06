using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>T-561: la lógica pura del controlador base (daño, muerte, escala de velocidad, dirección local, giro y frenar).</summary>
    public class ReglasAnimacionPruebas
    {
        private static readonly Vector3 Frente = Vector3.forward;

        [Test]
        public void ElegirDano_GolpeQueVaContraElFrente_EsDeFrente()
        {
            Assert.AreEqual(DanoVisual.Frente, ReglasAnimacion.ElegirDano(Vector3.back, Frente, false));
        }

        [Test]
        public void ElegirDano_GolpeQueVaConElFrente_EsDeEspalda()
        {
            Assert.AreEqual(DanoVisual.Espalda, ReglasAnimacion.ElegirDano(Vector3.forward, Frente, false));
        }

        [Test]
        public void ElegirDano_Fuerte_MandaSobreLaDireccion()
        {
            Assert.AreEqual(DanoVisual.Fuerte, ReglasAnimacion.ElegirDano(Vector3.forward, Frente, true));
        }

        [Test]
        public void ElegirDano_GolpeDeLado_CuentaComoDeFrente()
        {
            Assert.AreEqual(DanoVisual.Frente, ReglasAnimacion.ElegirDano(Vector3.right, Frente, false));
        }

        [Test]
        public void ElegirDano_SinDireccion_EsDeFrente()
        {
            Assert.AreEqual(DanoVisual.Frente, ReglasAnimacion.ElegirDano(Vector3.zero, Frente, false));
        }

        [Test]
        public void MuertesValidas_DeFrente_SonAtrasYDesmayo()
        {
            CollectionAssert.AreEqual(new[] { MuerteVisual.Atras, MuerteVisual.Desmayo }, ReglasAnimacion.MuertesValidas(Vector3.back, Frente));
        }

        [Test]
        public void MuertesValidas_DeEspalda_SonAdelanteYDesmayo()
        {
            CollectionAssert.AreEqual(new[] { MuerteVisual.Adelante, MuerteVisual.Desmayo }, ReglasAnimacion.MuertesValidas(Vector3.forward, Frente));
        }

        [Test]
        public void MuertesValidas_SinDireccion_SonLasTres()
        {
            Assert.AreEqual(3, ReglasAnimacion.MuertesValidas(Vector3.zero, Frente).Length);
        }

        [TestCase(0f, MuerteVisual.Atras)]
        [TestCase(0.49f, MuerteVisual.Atras)]
        [TestCase(0.5f, MuerteVisual.Desmayo)]
        [TestCase(0.999f, MuerteVisual.Desmayo)]
        [TestCase(1f, MuerteVisual.Desmayo)]
        public void ElegirMuerte_DeFrente_UsaElAzarInyectadoEntreLasValidas(float azar, MuerteVisual esperada)
        {
            Assert.AreEqual(esperada, ReglasAnimacion.ElegirMuerte(Vector3.back, Frente, azar));
        }

        [Test]
        public void ElegirMuerte_DeEspalda_NuncaCaeDeEspaldas()
        {
            for (float azar = 0f; azar <= 1f; azar += 0.05f)
            {
                Assert.AreNotEqual(MuerteVisual.Atras, ReglasAnimacion.ElegirMuerte(Vector3.forward, Frente, azar));
            }
        }

        [Test]
        public void ElegirMuerte_SinDireccion_PuedeSerCualquieraDeLasTres()
        {
            Assert.AreEqual(MuerteVisual.Adelante, ReglasAnimacion.ElegirMuerte(Vector3.zero, Frente, 0.1f));
            Assert.AreEqual(MuerteVisual.Atras, ReglasAnimacion.ElegirMuerte(Vector3.zero, Frente, 0.5f));
            Assert.AreEqual(MuerteVisual.Desmayo, ReglasAnimacion.ElegirMuerte(Vector3.zero, Frente, 0.9f));
        }

        [Test]
        public void LevantarseBocaArriba_SoloSiNoCayoDeFrente()
        {
            Assert.IsTrue(ReglasAnimacion.LevantarseBocaArriba(MuerteVisual.Atras));
            Assert.IsTrue(ReglasAnimacion.LevantarseBocaArriba(MuerteVisual.Desmayo));
            Assert.IsFalse(ReglasAnimacion.LevantarseBocaArriba(MuerteVisual.Adelante));
        }

        [Test]
        public void RapidezAgachado_SubeConLaVelocidad_YSeLimita()
        {
            float lento = ReglasAnimacion.RapidezAgachado(1.6f, 1f);
            float rapido = ReglasAnimacion.RapidezAgachado(3.4f, 1f);
            Assert.Greater(lento, 1f, "Agachado el clip va más rápido de lo que viene (el jugador va más rápido que el clip)");
            Assert.Greater(rapido, lento * 1.4f, "Con Shift los pies van bastante más rápido");
            Assert.AreEqual(ReglasAnimacion.RapidezMinimaAgachado, ReglasAnimacion.RapidezAgachado(0f, 1f), 0.0001f, "Parado no se congela");
            Assert.AreEqual(ReglasAnimacion.RapidezMaximaAgachado, ReglasAnimacion.RapidezAgachado(30f, 1f), 0.0001f, "Se limita");
        }

        [Test]
        public void EscalaVelocidad_SigueLaVelocidadRealYSeLimita()
        {
            Assert.AreEqual(1.5f, ReglasAnimacion.EscalaVelocidad(3f, 2f, 0.6f, 2.2f), 0.0001f);
            Assert.AreEqual(2.2f, ReglasAnimacion.EscalaVelocidad(30f, 2f, 0.6f, 2.2f), 0.0001f);
            Assert.AreEqual(0.6f, ReglasAnimacion.EscalaVelocidad(0.1f, 2f, 0.6f, 2.2f), 0.0001f);
        }

        [Test]
        public void EscalaVelocidad_SinVelocidadDeClip_NoAjusta()
        {
            Assert.AreEqual(1f, ReglasAnimacion.EscalaVelocidad(5f, 0f, 0.6f, 2.2f));
        }

        [Test]
        public void DireccionLocal_AvanzarDeFrenteDaZUno()
        {
            Vector2 d = ReglasAnimacion.DireccionLocal(new Vector3(0f, 0f, 5f), Vector3.forward);
            Assert.AreEqual(0f, d.x, 0.001f);
            Assert.AreEqual(1f, d.y, 0.001f);
        }

        [Test]
        public void DireccionLocal_HaciaLaDerechaDelPersonajeDaXUno_YGirado()
        {
            Vector2 d = ReglasAnimacion.DireccionLocal(new Vector3(-3f, 0f, 0f), Vector3.left);
            // El personaje mira a -X: su derecha es +Z, su izquierda -Z; ir hacia -X es avanzar.
            Assert.AreEqual(1f, d.y, 0.001f);
            Vector2 lado = ReglasAnimacion.DireccionLocal(new Vector3(0f, 0f, 3f), Vector3.left);
            Assert.AreEqual(1f, lado.x, 0.001f);
            Assert.AreEqual(0f, lado.y, 0.001f);
        }

        [Test]
        public void DireccionLocal_RetrocederDaZMenosUno()
        {
            Assert.AreEqual(-1f, ReglasAnimacion.DireccionLocal(new Vector3(0f, 0f, -4f), Vector3.forward).y, 0.001f);
        }

        [Test]
        public void DireccionLocal_QuietoOCasiQuieto_DaCero()
        {
            Assert.AreEqual(Vector2.zero, ReglasAnimacion.DireccionLocal(new Vector3(0.05f, 3f, 0f), Vector3.forward));
        }

        [Test]
        public void GiroEnSitio_SoloQuietoYConGiroFuerte()
        {
            Assert.AreEqual(1, ReglasAnimacion.GiroEnSitio(120f, 0f));
            Assert.AreEqual(-1, ReglasAnimacion.GiroEnSitio(-120f, 0f));
            Assert.AreEqual(0, ReglasAnimacion.GiroEnSitio(20f, 0f), "giro leve");
            Assert.AreEqual(0, ReglasAnimacion.GiroEnSitio(300f, 3f), "caminando no gira en el sitio");
        }

        [Test]
        public void DebeFrenar_AlSoltarElAvanceCorriendo()
        {
            Assert.IsTrue(ReglasAnimacion.DebeFrenar(7f, 1f));
            Assert.IsFalse(ReglasAnimacion.DebeFrenar(3f, 0.5f), "caminando solo se detiene");
            Assert.IsFalse(ReglasAnimacion.DebeFrenar(7f, 4f), "todavía corre");
        }

        [Test]
        public void TipoPorArma_PunosPistolaYLarga()
        {
            Assert.AreEqual(TipoArmaVisual.Ninguna, ReglasAnimacion.TipoPorArma(true, false));
            Assert.AreEqual(TipoArmaVisual.Pistola, ReglasAnimacion.TipoPorArma(false, true));
            Assert.AreEqual(TipoArmaVisual.ArmaLarga, ReglasAnimacion.TipoPorArma(false, false));
        }
    }
}

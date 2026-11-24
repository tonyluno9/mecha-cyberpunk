using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.VR;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// VR38: el giro suave con el stick derecho es más lento (60°/s), con zona muerta y aceleración suave; mientras gira la viñeta se cierra un poco
    /// más que al caminar (y al caminar sigue la leve).
    /// </summary>
    public class VR38GiroSuavePruebas : BaseJugadorVR
    {
        public override void Setup()
        {
            base.Setup();
            OpcionesVR.FijarGiro(ModoGiroVR.Suave);
            OpcionesVR.FijarNivelVineta(NivelVinetaVR.Baja);
            OpcionesVR.FijarVelocidadGiro(OpcionesVR.VelocidadGiroPorDefecto);
        }

        [Test]
        public void ReglaGiro_ZonaMuertaYAceleracionSuave()
        {
            Assert.AreEqual(60f, OpcionesVR.VelocidadGiroPorDefecto, 1e-4f, "60°/s por defecto.");
            Assert.AreEqual(0f, ReglaGiroSuaveVR.Proporcion(0.15f), 1e-5f, "Dentro de la zona muerta no gira.");
            Assert.AreEqual(1f, ReglaGiroSuaveVR.Proporcion(1f), 1e-5f);
            Assert.AreEqual(-60f, ReglaGiroSuaveVR.Objetivo(-1f, 60f), 1e-4f);
            // Acelera: en un cuadro de 1/60 s no llega a la máxima.
            float v = ReglaGiroSuaveVR.Siguiente(0f, 60f, 60f, 1f / 60f);
            Assert.Greater(v, 0f);
            Assert.Less(v, 60f * 0.2f, "Arranca suave.");
            // En SegundosAcelerar llega.
            float t = 0f;
            v = 0f;
            while (t < ReglaGiroSuaveVR.SegundosAcelerar + 0.02f)
            {
                v = ReglaGiroSuaveVR.Siguiente(v, 60f, 60f, 0.01f);
                t += 0.01f;
            }
            Assert.AreEqual(60f, v, 1e-3f);
            // Frena más rápido de lo que acelera.
            float frenado = ReglaGiroSuaveVR.Siguiente(60f, 0f, 60f, 0.03f);
            float acelerado = ReglaGiroSuaveVR.Siguiente(0f, 60f, 60f, 0.03f);
            Assert.Greater(60f - frenado, acelerado);
        }

        [Test]
        public void AperturaDeGiro_UnPocoMasCerradaQueLaDeCaminar()
        {
            Assert.Less(OpcionesVR.AperturaVinetaGiro(NivelVinetaVR.Baja), OpcionesVR.AperturaVineta(NivelVinetaVR.Baja));
            Assert.Greater(OpcionesVR.AperturaVinetaGiro(NivelVinetaVR.Baja), OpcionesVR.AperturaVineta(NivelVinetaVR.Media) - 0.1f, "Solo un poco más.");
            Assert.Less(OpcionesVR.AperturaVinetaGiro(NivelVinetaVR.Media), OpcionesVR.AperturaVinetaGiro(NivelVinetaVR.Baja));
            Assert.AreEqual(1f, OpcionesVR.AperturaVinetaGiro(NivelVinetaVR.No), 1e-5f);
        }

        [UnityTest]
        public IEnumerator GirarConElStick_VinetaDeGiroQueEntraYSaleRapido_CaminarLaLeve()
        {
            yield return ArmarVR();
            Stick(true, new Vector2(1f, 0f));
            yield return Segundos(0.3f);
            Assert.IsTrue(adaptador.VinetaDeGiro, "Al girar sale la viñeta de giro.");
            Assert.AreEqual(OpcionesVR.AperturaVinetaGiro(NivelVinetaVR.Baja), adaptador.AperturaVinetaActual, 0.01f);
            Assert.AreEqual(OpcionesVR.VelocidadGiro, Mathf.Abs(adaptador.VelocidadGiroActual), 1f, "Ya llegó a la velocidad de giro.");
            Stick(true, Vector2.zero);
            yield return Segundos(0.25f);
            Assert.IsFalse(adaptador.VinetaPuesta, "Al soltar el stick la viñeta se va rápido.");
            Assert.AreEqual(0f, adaptador.VelocidadGiroActual, 1e-4f);

            Stick(false, new Vector2(0f, 1f));
            yield return Segundos(0.6f);
            Stick(false, Vector2.zero);
            Assert.IsTrue(adaptador.VinetaPuesta, "Al caminar sigue la viñeta leve.");
            Assert.IsFalse(adaptador.VinetaDeGiro);
            Assert.AreEqual(OpcionesVR.AperturaVineta(NivelVinetaVR.Baja), adaptador.AperturaVinetaActual, 0.01f, "Al caminar, la apertura BAJA de siempre.");
        }
    }
}

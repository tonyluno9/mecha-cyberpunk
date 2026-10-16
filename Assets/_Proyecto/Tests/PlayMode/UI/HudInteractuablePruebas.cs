using System.Collections;
using MechaCyberpunk.Personaje.Pruebas;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>Pruebas jugables del aviso del HUD frente a un interactuable, con jugador real.</summary>
    public class HudInteractuablePruebas : BaseHudObjetivo
    {
        private GameObject objetoPrueba;

        public override void TearDown()
        {
            if (objetoPrueba != null) Object.Destroy(objetoPrueba);
            base.TearDown();
        }

        private IEnumerator Preparar(float z)
        {
            CrearJugador();
            CrearHud();
            objetoPrueba = new GameObject("Interactuable de prueba");
            objetoPrueba.transform.position = new Vector3(0f, 0f, z);
            objetoPrueba.AddComponent<InteractuableDePrueba>();
            yield return Calentar();
            yield return Esperar(0.1f);
        }

        [UnityTest]
        public IEnumerator Frente_DiceEMasTexto_YSinPuedeUsarSoloElTexto()
        {
            yield return Preparar(1.5f);
            Assert.IsTrue(hud.GrupoInteraccion.activeSelf);
            Assert.AreEqual("E · Probar interactuable", hud.TextoAvisoInteraccion.text);
            Assert.AreEqual(config.ColorTexto, hud.TextoAvisoInteraccion.color);

            objetoPrueba.GetComponent<InteractuableDePrueba>().PuedeUsar = false;
            yield return null;
            yield return null;

            Assert.AreEqual("Probar interactuable", hud.TextoAvisoInteraccion.text);
            Assert.AreEqual(config.ColorAviso, hud.TextoAvisoInteraccion.color);
        }

        [UnityTest]
        public IEnumerator CambiarElTexto_SeReleeCadaFrame()
        {
            yield return Preparar(1.5f);
            objetoPrueba.GetComponent<InteractuableDePrueba>().Texto = "Dormir";
            yield return null;
            yield return null;
            Assert.AreEqual("E · Dormir", hud.TextoAvisoInteraccion.text);
        }

        [UnityTest]
        public IEnumerator AlAlejarse_SeOculta()
        {
            yield return Preparar(1.5f);
            Assert.IsTrue(hud.GrupoInteraccion.activeSelf);

            objetoPrueba.transform.position = new Vector3(0f, 0f, 6f);
            yield return null;
            yield return null;

            Assert.IsFalse(hud.GrupoInteraccion.activeSelf);
            Assert.AreEqual(string.Empty, hud.TextoAvisoInteraccion.text);
        }
    }
}

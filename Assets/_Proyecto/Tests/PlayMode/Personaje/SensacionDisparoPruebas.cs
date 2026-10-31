using System.Collections;
using System.IO;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Pruebas;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-420: disparar la pistola con clic simulado da patada de cámara, destello y marca de impacto en persona.</summary>
    public class SensacionDisparoPruebas : BaseJugadorAPie
    {
        private GameObject muneco;

        public override void TearDown()
        {
            if (muneco != null)
            {
                Object.Destroy(muneco);
            }
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Disparo_AMunecoEnLaMira_DaPatadaDestelloYConecto()
        {
            yield return Calentar();
            muneco = GameObject.CreatePrimitive(PrimitiveType.Cube);
            muneco.transform.position = new Vector3(0f, 1f, 8f);
            muneco.transform.localScale = new Vector3(6f, 3f, 0.5f);
            muneco.AddComponent<MunecoAPie>();
            Physics.SyncTransforms();
            yield return null;

            SensacionDisparo sensacion = jugador.GetComponent<SensacionDisparo>();
            Assert.IsNotNull(sensacion, "La pistola crea la sensación de disparo");
            var hud = new GameObject("HitmarkerPrueba");
            Hitmarker marca = hud.AddComponent<Hitmarker>();
            yield return null;
            int conectos = 0;
            sensacion.Conecto += mato => conectos++;

            Press(teclado.digit2Key);
            yield return null;
            Release(teclado.digit2Key);
            yield return null;
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);

            Assert.Greater(sensacion.Patada, 0f, "El tiro patea la cámara");
            Assert.IsTrue(sensacion.DestelloVisible, "El cañón destella");
            Assert.AreEqual(1, conectos, "El tiro a la persona avisa a la retícula");
            yield return null;
            Assert.IsTrue(marca.Visible, "La retícula marca el impacto");
            Assert.IsFalse(marca.UltimaMato, "Un tiro que no mata usa la marca normal");
            if (!Application.isBatchMode)
            {
                yield return new WaitForEndOfFrame();
                Texture2D foto = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine("docs", "capturas", "T-420-1.png"), foto.EncodeToPNG());
                Object.Destroy(foto);
            }
            yield return Esperar(sensacion.Datos.DuracionPatada + 0.1f);
            Assert.AreEqual(0f, sensacion.Patada, 0.001f, "La patada vuelve a cero");
            Assert.IsFalse(sensacion.DestelloVisible, "El destello es breve");
            Assert.IsFalse(marca.Visible, "La marca es breve");
            Object.Destroy(hud);
        }
    }
}

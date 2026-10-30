using System.Collections;
using System.IO;
using MechaCyberpunk.UI.Historia;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>La pantalla de "continuará" de T-125, con teclado simulado.</summary>
    public class PantallaContinuaraPruebas : InputTestFixture
    {
        private const string RutaPrefab = "Assets/_Proyecto/UI/Prefabs/PantallasHistoria.prefab";
        private const string RutaCaptura = "docs/capturas/T-125-2.png";
        private const float SegundosAntes = 3f;
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;

        private Keyboard teclado;
        private GameObject instancia;
        private PantallaContinuara pantalla;
        private int seguir;
        private int salir;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            instancia = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab));
            pantalla = instancia.GetComponentInChildren<PantallaContinuara>();
            seguir = 0;
            salir = 0;
            pantalla.SeguirJugando += () => seguir++;
            pantalla.SalirAlTitulo += () => salir++;
        }

        public override void TearDown()
        {
            Object.Destroy(instancia);
            base.TearDown();
        }

        private IEnumerator Tecla(UnityEngine.InputSystem.Controls.KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.unscaledTime + segundos;
            while (Time.unscaledTime < fin)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Prefab_Arranca_ConContinuaraOculta()
        {
            yield return null;
            Assert.IsFalse(pantalla.Visible);
            Assert.IsFalse(pantalla.transform.Find("Contenido").gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator Mostrar_PoneAliasDineroYTextoDeGanar()
        {
            yield return null;
            pantalla.Mostrar("EL RAYO", 1250, true);
            yield return null;
            Assert.IsTrue(pantalla.Visible);
            Assert.AreEqual("EL RAYO", pantalla.Alias);
            Assert.AreEqual("Ƀ1,250", pantalla.Dinero);
            Assert.AreEqual("Ganaste tu primera pelea casera", pantalla.Resultado);
            Assert.IsFalse(pantalla.TeclasVisibles, "Las teclas no se muestran al principio");
        }

        [UnityTest]
        public IEnumerator Mostrar_Perdiendo_PoneTextoDePerder()
        {
            yield return null;
            pantalla.Mostrar("EL RAYO", 0, false);
            Assert.AreEqual("Perdiste tu primera pelea casera, vuelve cuando lo repares", pantalla.Resultado);
            Assert.AreEqual("Ƀ0", pantalla.Dinero);
        }

        [UnityTest]
        public IEnumerator Teclas_AntesDeCuatroSegundos_NoAvisanNada()
        {
            yield return null;
            pantalla.Mostrar("EL RAYO", 1250, true);
            yield return Esperar(SegundosAntes);
            yield return Tecla(teclado.enterKey);
            yield return Tecla(teclado.escapeKey);
            Assert.AreEqual(0, seguir);
            Assert.AreEqual(0, salir);
            Assert.IsFalse(pantalla.TeclasVisibles);
        }

        [UnityTest]
        public IEnumerator Teclas_DespuesDeCuatroSegundos_AvisanUnaVezCadaUna()
        {
            yield return null;
            pantalla.Mostrar("EL RAYO", 1250, true);
            yield return Esperar(4.2f);
            Assert.IsTrue(pantalla.TeclasVisibles);
            Assert.AreEqual("ENTER · seguir jugando   ESC · salir al título", pantalla.Teclas);
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(1, seguir);
            Assert.AreEqual(0, salir);
            yield return Tecla(teclado.escapeKey);
            Assert.AreEqual(1, seguir);
            Assert.AreEqual(1, salir);
        }

        [UnityTest]
        public IEnumerator Ocultar_SinVerse_NoRecibeTeclas()
        {
            yield return null;
            pantalla.Mostrar("EL RAYO", 1250, true);
            yield return Esperar(4.2f);
            pantalla.Ocultar();
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(0, seguir);
            Assert.IsFalse(pantalla.Visible);
        }

        [UnityTest]
        public IEnumerator Mostrar_DejaElCursorLibre()
        {
            yield return null;
            Cursor.lockState = CursorLockMode.Locked;
            pantalla.Mostrar("EL RAYO", 1250, true);
            yield return null;
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
            Assert.IsTrue(Cursor.visible);
        }

        [UnityTest]
        public IEnumerator Captura_ContinuaraConResumenYTeclas_GuardaPng()
        {
            yield return null;
            GameObject camObjeto = new GameObject("CamCaptura", typeof(Camera));
            Camera camara = camObjeto.GetComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = Color.black;
            Canvas canvas = pantalla.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camara;
            canvas.planeDistance = 1f;
            pantalla.Mostrar("EL RAYO", 1250, true);
            yield return Esperar(4.2f);
            yield return null;
            Assert.IsTrue(pantalla.TeclasVisibles);

            RenderTexture destino = RenderTexture.GetTemporary(AnchoCaptura, AltoCaptura, 24);
            camara.targetTexture = destino;
            camara.Render();
            RenderTexture previa = RenderTexture.active;
            RenderTexture.active = destino;
            Texture2D textura = new Texture2D(AnchoCaptura, AltoCaptura, TextureFormat.RGB24, false);
            textura.ReadPixels(new Rect(0, 0, AnchoCaptura, AltoCaptura), 0, 0);
            textura.Apply();
            RenderTexture.active = previa;
            camara.targetTexture = null;
            RenderTexture.ReleaseTemporary(destino);

            bool hayNeon = false;
            for (int x = 0; x < AnchoCaptura && !hayNeon; x += 4)
            {
                Color pixel = textura.GetPixel(x, AltoCaptura * 2 / 3);
                hayNeon = pixel.g > 0.5f && pixel.b > 0.5f;
            }
            Assert.IsTrue(hayNeon, "El título neón se dibuja");
            File.WriteAllBytes(Path.Combine(Application.dataPath, "..", RutaCaptura), textura.EncodeToPNG());
            Object.Destroy(textura);
            Object.Destroy(camObjeto);
        }
    }
}

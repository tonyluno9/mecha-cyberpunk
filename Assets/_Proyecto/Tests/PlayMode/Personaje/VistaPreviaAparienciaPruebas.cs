using System.Collections;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Apariencia;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>VistaPreviaApariencia.prefab: dibuja en su RenderTexture, apagada no deja cámaras y no suma AudioListener.</summary>
    public class VistaPreviaAparienciaPruebas
    {
        private const string RutaVista = "Assets/_Proyecto/Personaje/Prefabs/VistaPreviaApariencia.prefab";

        private GameObject go;

        [SetUp]
        public void SetUp() => Registros.LimpiarTodos();

        [TearDown]
        public void TearDown()
        {
            if (go != null) Object.Destroy(go);
            Registros.LimpiarTodos();
        }

        private VistaPreviaApariencia Crear()
        {
            go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaVista));
            return go.GetComponent<VistaPreviaApariencia>();
        }

        private static int OyentesActivos() =>
            Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Count(o => o.isActiveAndEnabled);

        [UnityTest]
        public IEnumerator Encendida_SuCamaraDibujaEnLaRenderTexture()
        {
            VistaPreviaApariencia vista = Crear();
            yield return null;
            vista.Mostrar(true);
            yield return null;

            Assert.IsNotNull(vista.Textura);
            Assert.IsTrue(vista.Camara.isActiveAndEnabled);
            Assert.AreSame(vista.Textura, vista.Camara.targetTexture);

            // En batchmode WaitForEndOfFrame no corre: se dibuja a mano con la misma cámara y su RenderTexture.
            vista.Camara.Render();
            RenderTexture anterior = RenderTexture.active;
            RenderTexture.active = vista.Textura;
            var lectura = new Texture2D(vista.Textura.width, vista.Textura.height, TextureFormat.RGBA32, false);
            lectura.ReadPixels(new Rect(0, 0, vista.Textura.width, vista.Textura.height), 0, 0);
            lectura.Apply();
            RenderTexture.active = anterior;

            Color fondo = vista.Camara.backgroundColor;
            Color centro = lectura.GetPixel(lectura.width / 2, lectura.height / 2);
            Color esquina = lectura.GetPixel(4, 4);
            Object.Destroy(lectura);
            Assert.AreEqual(fondo.r, esquina.r, 0.03f, "la esquina es el fondo");
            float diferencia = Mathf.Abs(centro.r - fondo.r) + Mathf.Abs(centro.g - fondo.g) + Mathf.Abs(centro.b - fondo.b);
            Assert.Greater(diferencia, 0.2f, "el centro muestra el cuerpo");
        }

        [UnityTest]
        public IEnumerator Apagada_NoQuedaNingunaCamaraSuyaActiva()
        {
            VistaPreviaApariencia vista = Crear();
            yield return null;
            vista.Mostrar(true);
            yield return null;
            Assert.IsTrue(go.GetComponentsInChildren<Camera>().Any(c => c.isActiveAndEnabled));
            vista.Mostrar(false);
            yield return null;
            Assert.IsFalse(vista.EstaEncendida);
            Assert.IsFalse(go.GetComponentsInChildren<Camera>(true).Any(c => c.isActiveAndEnabled));
            Assert.IsFalse(go.GetComponentsInChildren<Light>(true).Any(l => l.isActiveAndEnabled));
        }

        [UnityTest]
        public IEnumerator NoHayMasDeUnAudioListenerActivo()
        {
            int antes = OyentesActivos();
            VistaPreviaApariencia vista = Crear();
            yield return null;
            Assert.AreEqual(0, go.GetComponentsInChildren<AudioListener>(true).Length);
            vista.Mostrar(true);
            yield return null;
            Assert.LessOrEqual(OyentesActivos(), 1);
            Assert.AreEqual(antes, OyentesActivos());
            vista.Mostrar(false);
            yield return null;
            Assert.AreEqual(antes, OyentesActivos());
        }

        [UnityTest]
        public IEnumerator Encendida_ElCuerpoGiraSolo()
        {
            VistaPreviaApariencia vista = Crear();
            yield return null;
            vista.Mostrar(true);
            Transform pivote = vista.Cuerpo.transform.parent;
            float antes = pivote.eulerAngles.y;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreNotEqual(antes, pivote.eulerAngles.y);
        }

        [UnityTest]
        public IEnumerator Prefab_EstaLejosDelMundo()
        {
            Crear();
            yield return null;
            Assert.LessOrEqual(go.transform.position.y, -500f);
        }
    }
}

using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Mundo.Ambiente;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-854 (playtest 6-nov tarde): el espejo del cuarto refleja de verdad, ya no abre el muñeco de la vista previa.</summary>
    public class EspejoS2tPruebas
    {
        [Test]
        public void Cuarto_TieneElEspejoQueReflejaYElViejoApagado()
        {
            Scene escena = EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Additive);
            try
            {
                Transform raiz = escena.GetRootGameObjects().First(g => g.name == ColocarS2tEspejo.NombreRaiz).transform;
                Transform vidrio = raiz.Find("Vidrio_Reflejo");
                Assert.IsNotNull(vidrio.GetComponent<EspejoReflejo>());
                Assert.AreEqual(ColocarS2tEspejo.ShaderEspejo, vidrio.GetComponent<Renderer>().sharedMaterial.shader.name);
                Assert.Greater(Vector3.Dot(-vidrio.forward, Vector3.forward), 0.99f, "el vidrio mira al cuarto");
                Bounds marco = raiz.Find("H03_Espejo").GetComponentInChildren<Renderer>().bounds;
                Assert.Greater(vidrio.position.z, marco.max.z, "el vidrio va delante del espejo del kit");
                Transform viejo = escena.GetRootGameObjects().First(g => g.name == "Barrio").transform.Find(ColocarS2tEspejo.RutaEspejoViejo.Substring("Barrio/".Length));
                Assert.IsNotNull(viejo, "el EspejoJugador sigue en la escena");
                MonoBehaviour e = viejo.GetComponents<MonoBehaviour>().First(m => m.GetType().Name == "EspejoJugador");
                Assert.IsFalse(e.enabled, "ya no abre la vista previa (el muñeco)");
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }

        [Test]
        public void Actualizar_ConLaCamaraEnfrente_PoneLaCamaraDelReflejoDetras()
        {
            GameObject vidrio = GameObject.CreatePrimitive(PrimitiveType.Quad);
            vidrio.transform.SetPositionAndRotation(new Vector3(0f, 1.5f, 0f), Quaternion.LookRotation(Vector3.back));
            EspejoReflejo espejo = vidrio.AddComponent<EspejoReflejo>();
            GameObject ojo = new GameObject("Ojo");
            Camera cam = ojo.AddComponent<Camera>();
            cam.enabled = false;
            try
            {
                ojo.transform.position = new Vector3(0.5f, 1.6f, 2f);
                Assert.IsTrue(espejo.Actualizar(cam));
                Assert.AreEqual(new Vector3(0.5f, 1.6f, -2f).ToString("F2"), espejo.Camara.transform.position.ToString("F2"), "la cámara del reflejo está en el ojo reflejado");
                ojo.transform.position = new Vector3(0f, 1.6f, -2f);
                Assert.IsFalse(espejo.Actualizar(cam), "por detrás de la pared no refleja");
                ojo.transform.position = new Vector3(0f, 1.6f, 40f);
                Assert.IsFalse(espejo.Actualizar(cam), "de lejos no refleja");
            }
            finally
            {
                espejo.Soltar();
                Object.DestroyImmediate(vidrio);
                Object.DestroyImmediate(ojo);
            }
        }
    }
}

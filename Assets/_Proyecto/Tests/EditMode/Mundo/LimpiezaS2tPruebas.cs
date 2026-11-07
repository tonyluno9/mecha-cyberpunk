using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-856 (playtest 6-nov tarde): sin balcones, toldos, tejados ni puentes de primitivos; banquetas con la textura T01.</summary>
    public class LimpiezaS2tPruebas
    {
        private Scene escena;
        private Transform[] todos;

        [OneTimeSetUp]
        public void Abrir()
        {
            escena = EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Additive);
            todos = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>()).Where(t => t.gameObject.activeInHierarchy).ToArray();
        }

        [OneTimeTearDown]
        public void Cerrar() => EditorSceneManager.CloseScene(escena, true);

        [Test]
        public void SinBalconesToldosNiTejadosDePrimitivos()
        {
            string[] t508 = todos.Where(t => t.root.name == ColocarS2tLimpieza.RaizT508 && ColocarS2tLimpieza.GruposT508.Contains(t.name)).Select(t => t.name).ToArray();
            CollectionAssert.IsEmpty(t508, "balcones, toldos, macetas y plantas de cubo");
            string[] tejados = todos.Where(t => t.name == ColocarS2tLimpieza.NombreTejado && t.root.name == "Barrio" && t.parent.parent != null && t.parent.parent.name == "Edificios").Select(t => t.parent.name).ToArray();
            CollectionAssert.IsEmpty(tejados, "tejados Lamina");
            Assert.IsFalse(todos.Any(t => t.name == ColocarS2tLimpieza.RaizPuentes), "sin los puentes de cubos");
        }

        [Test]
        public void Banquetas_ConLaTexturaT01()
        {
            Texture2D t01 = AssetDatabase.LoadAssetAtPath<Texture2D>(ColocarS2tLimpieza.TexturaBanqueta);
            Assert.IsNotNull(t01);
            Renderer[] banquetas = todos.Select(t => t.GetComponent<Renderer>()).Where(r => r != null && r.sharedMaterial != null && r.sharedMaterial.name.StartsWith(ColocarS2tLimpieza.MaterialBanqueta)).ToArray();
            Assert.Greater(banquetas.Length, 10);
            foreach (Renderer r in banquetas)
            {
                Assert.AreSame(t01, r.sharedMaterial.GetTexture("_BaseMap"), r.name);
                Vector2 tiling = r.sharedMaterial.GetTextureScale("_BaseMap");
                Assert.AreEqual(r.bounds.size.x / ColocarS2tLimpieza.LadoLosa, tiling.x, Mathf.Max(1f, tiling.x * 0.5f), r.name + ": losas parejas");
            }
        }
    }
}

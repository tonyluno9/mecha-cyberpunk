using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-500: los prefabs N01-N08 existen con hijo Visual y renderers, y los tres personajes de Tripo cargan con su esqueleto.</summary>
    public class ImportarT500Pruebas
    {
        private static readonly string[] Prefabs =
        {
            "Calle/Prefabs/N01", "Exteriores/Prefabs/N02", "Calle/Prefabs/N03a", "Calle/Prefabs/N03b", "Callejon/Prefabs/N04",
            "Callejon/Prefabs/N05", "Calle/Prefabs/N06", "Exteriores/Prefabs/N07", "Exteriores/Prefabs/N08",
        };

        private static readonly string[] Personajes = { "man", "woman", "woman2" };

        [Test]
        public void Prefabs_N01aN08_TienenVisualConRenderers()
        {
            foreach (string codigo in Prefabs)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/" + codigo + ".prefab");
                Assert.IsNotNull(prefab, codigo + " no tiene prefab");
                Transform visual = prefab.transform.Find("Visual");
                Assert.IsNotNull(visual, codigo + " no tiene hijo Visual");
                Renderer r = visual.GetComponentInChildren<Renderer>();
                Assert.IsNotNull(r, codigo + " no tiene renderer");
                Assert.AreEqual("Universal Render Pipeline/Lit", r.sharedMaterial.shader.name, codigo);
            }
        }

        [Test]
        public void Personajes_ManWomanWoman2_CarganConMallaYAnimaciones()
        {
            foreach (string nombre in Personajes)
            {
                string ruta = "Assets/Arte/Personajes/" + nombre + "/" + nombre + ".fbx";
                GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                Assert.IsNotNull(fbx, ruta);
                Assert.IsNotNull(fbx.GetComponentInChildren<SkinnedMeshRenderer>(true), nombre + " sin malla con esqueleto");
                int clips = 0;
                foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ruta))
                {
                    if (o is AnimationClip c && !c.name.StartsWith("__preview__"))
                    {
                        clips++;
                    }
                }
                Assert.Greater(clips, 0, nombre + " sin animaciones");
            }
        }
    }
}

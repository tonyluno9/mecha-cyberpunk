using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1106: los 4 visuales nuevos (E10, E11, E12, E14) existen, son Humanoid válidos, usan el controlador de su papel y miden lo que deben.</summary>
    public class VisualesEnemigosT1106Pruebas
    {
        private static readonly string[] Nombres = CreadorVisualTripo.NombresT1106;

        private static string RutaPrefab(string n) => "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_" + n + ".prefab";

        private static string PapelDe(string n) => System.Array.Find(ConectarControladorBase.Reparto, r => r.Modelo == n).Papel;

        [Test]
        public void LosCuatroPrefabsExisten()
        {
            foreach (string n in Nombres)
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(n)), "Falta Visual_" + n);
            }
        }

        [Test]
        public void CadaUnoTieneAnimatorConAvatarHumanoidValidoYControladorDeSuPapel()
        {
            foreach (string n in Nombres)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(n));
                Animator a = prefab.GetComponentInChildren<Animator>(true);
                Assert.IsNotNull(a, n + ": Animator");
                Assert.IsNotNull(a.avatar, n + ": avatar");
                Assert.IsTrue(a.avatar.isValid && a.avatar.isHuman, n + ": avatar Humanoid válido");
                Assert.IsNotNull(a.runtimeAnimatorController, n + ": controlador");
                Assert.AreEqual("Override_" + PapelDe(n), a.runtimeAnimatorController.name, n + ": override de su papel");
            }
        }

        [Test]
        public void LaAlturaDeBoundsEstaDentroDeMasMenos10PorCientoDeLaObjetivo()
        {
            foreach (string n in Nombres)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(n)));
                try
                {
                    Bounds? total = null;
                    foreach (Renderer r in inst.GetComponentsInChildren<Renderer>())
                    {
                        if (total == null) { total = r.bounds; } else { Bounds b = total.Value; b.Encapsulate(r.bounds); total = b; }
                    }
                    float objetivo = CreadorVisualTripo.AlturaDe(n);
                    Assert.IsTrue(total.HasValue, n + ": sin renderers");
                    Assert.AreEqual(objetivo, total.Value.size.y, objetivo * 0.1f, n + ": altura");
                }
                finally
                {
                    Object.DestroyImmediate(inst);
                }
            }
        }

        [Test]
        public void ElMaterialTieneSuTexturaBaseAsignadaYSinTinteRaro()
        {
            foreach (string n in Nombres)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab(n));
                SkinnedMeshRenderer r = prefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
                Assert.IsNotNull(r, n + ": SkinnedMeshRenderer");
                Material m = r.sharedMaterial;
                Assert.IsNotNull(m, n + ": material");
                Texture t = m.GetTexture("_BaseMap");
                Assert.IsNotNull(t, n + ": textura base");
                StringAssert.EndsWith("_Base", t.name, n + ": es la textura de color");
                Assert.AreEqual(Color.white, m.GetColor("_BaseColor"), n + ": no se retinta");
            }
        }
    }
}

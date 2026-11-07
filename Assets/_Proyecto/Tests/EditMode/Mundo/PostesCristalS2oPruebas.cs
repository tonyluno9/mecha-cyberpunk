using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-881 (playtest 7-nov, sesión 2): en los postes solo brilla el cristal de la lámpara del modelo; nada de óvalos de primitivo.</summary>
    public class PostesCristalS2oPruebas
    {
        [OneTimeSetUp]
        public void Abrir() => EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);

        [Test]
        public void Modelos_BrillanSoloEnElCristal()
        {
            foreach (var m in ColocarS2oPostes.Modelos)
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(m.material);
                Assert.IsTrue(mat.IsKeywordEnabled("_EMISSION"), m.material + ": emite");
                Assert.AreEqual(MaterialGlobalIlluminationFlags.RealtimeEmissive, mat.globalIlluminationFlags, "si no, URP pierde el keyword al reimportar");
                Texture2D mascara = mat.GetTexture("_EmissionMap") as Texture2D;
                Assert.IsNotNull(mascara, m.material + ": con su máscara de cristal");
                string ruta = AssetDatabase.GetAssetPath(mascara);
                Texture2D leida = new Texture2D(2, 2);
                leida.LoadImage(System.IO.File.ReadAllBytes(ruta));
                float blanco = leida.GetPixels32().Count(p => p.r > 128) / (float)(leida.width * leida.height);
                Object.DestroyImmediate(leida);
                Assert.Greater(blanco, 0.002f, ruta + ": hay cristal");
                Assert.Less(blanco, 0.08f, ruta + ": solo el cristal, no el poste entero");
            }
        }

        [Test]
        public void Cristal_EsLaCaraDeAbajoDeLaCabeza()
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ColocarS2oPostes.Modelos[0].modelo);
            MeshFilter mf = fbx.GetComponentsInChildren<MeshFilter>(true).OrderByDescending(m => m.sharedMesh.vertexCount).First();
            Matrix4x4 parado = ColocarS2oPostes.Parado(mf.transform);
            Vector3 cabeza = PostesS2t.Cabezas(mf.sharedMesh, parado).First();
            Bounds b = mf.sharedMesh.bounds;
            Assert.Greater(cabeza.y, parado.MultiplyPoint3x4(b.center).y, "la cabeza está arriba del poste parado");
            Assert.Greater(ColocarS2oPostes.Cristal(mf.sharedMesh, parado).Count, 20);
        }

        [Test]
        public void Ciudad_SinMicasDePrimitivo()
        {
            Assert.IsFalse(ColocarS2.Todos().Any(t => t.name == "Mica" && t.gameObject.activeInHierarchy), "el óvalo bajo la cabeza se veía como un hueco");
        }
    }
}

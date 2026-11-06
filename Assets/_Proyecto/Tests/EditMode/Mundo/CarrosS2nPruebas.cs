using System.IO;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-810 (noche 6-nov, sesión 2): carros V09-V11 por ImportarS2n cuando lleguen sus zips, y variantes de color en el tráfico.</summary>
    public class CarrosS2nPruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string CarpetaTrafico = "Assets/_Proyecto/Trafico/Prefabs/";

        private static ImportarS2n.Entrada[] Carros() => ImportarS2n.Entradas.Where(e => e.Accion == ImportarS2n.Accion.CarroTripo).ToArray();

        [Test]
        public void Importador_TieneLasFilasDeV09V10V11()
        {
            CollectionAssert.AreEquivalent(new[] { "V09", "V10", "V11" }, Carros().Select(e => ImportarS2nCarros.Codigo(e.Destino)).ToArray());
            Assert.IsTrue(Carros().All(e => ImportarS2nCarros.Medidas.Any(m => m.codigo == ImportarS2nCarros.Codigo(e.Destino) && m.largo > 3f)),
                "Cada carro tiene su largo real");
        }

        [Test]
        public void Importador_SinZip_NoHaceNadaYAnotaQueFalta()
        {
            byte[] escenaAntes = File.ReadAllBytes(RutaEscena);
            ImportarS2n.MeterTodo();
            string informe = ImportarS2n.Informe.ToString();
            bool algunoLlego = false;
            foreach (ImportarS2n.Entrada e in Carros())
            {
                string codigo = ImportarS2nCarros.Codigo(e.Destino);
                string trafico = CarpetaTrafico + "CarroTrafico_" + codigo + ".prefab";
                if (File.Exists(e.Origen))
                {
                    // Ya llegó: el carro queda armado.
                    algunoLlego = true;
                    Assert.IsTrue(File.Exists(e.Destino), codigo + ": se desempacó el fbx");
                    Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(trafico), codigo + ": tiene su prefab de tráfico");
                    continue;
                }
                StringAssert.Contains("falta " + Path.GetFileName(e.Origen), informe);
                Assert.IsFalse(Directory.Exists(ImportarS2nCarros.CarpetaDe(codigo)), codigo + ": sin zip no se crea su carpeta");
                Assert.IsFalse(File.Exists(e.Destino), codigo + ": sin zip no hay fbx");
                Assert.IsNull(AssetDatabase.LoadAssetAtPath<GameObject>(trafico), codigo + ": sin zip no hay prefab de tráfico");
            }
            if (!algunoLlego)
            {
                CollectionAssert.AreEqual(escenaAntes, File.ReadAllBytes(RutaEscena), "Sin zips la escena no se toca");
            }
        }

        [Test]
        public void TaxiYPickup_EstanEnElTraficoYDerechos()
        {
            // T-850: V09 y V10 llegaron con la raíz del FBX girada 90° en X; si se pierde ese giro el carro queda de punta o de lado.
            foreach (string codigo in new[] { "V09", "V10" })
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaTrafico + "CarroTrafico_" + codigo + ".prefab");
                Assert.IsNotNull(prefab, codigo + ": tiene su prefab de tráfico");
                GameObject copia = Object.Instantiate(prefab);
                try
                {
                    Bounds b = ImportarT511.LimitesExactos(copia.transform.Find("Visual_T511").gameObject);
                    float largo = ImportarS2nCarros.Medidas.First(m => m.codigo == codigo).largo;
                    Assert.AreEqual(largo, b.size.z, 0.05f, codigo + ": el largo va en Z");
                    Assert.Less(b.size.y, 2.5f, codigo + ": no queda de punta");
                    Assert.Less(b.size.x, 2.6f, codigo + ": no queda de lado");
                    Assert.AreEqual(0f, b.min.y, 0.05f, codigo + ": las llantas en el piso");
                }
                finally
                {
                    Object.DestroyImmediate(copia);
                }
            }
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Additive);
            try
            {
                Component trafico = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                    .Select(t => t.GetComponent("TraficoCalle")).First(c => c != null);
                SerializedProperty lista = new SerializedObject(trafico).FindProperty("prefabsCarro");
                string[] nombres = Enumerable.Range(0, lista.arraySize).Select(i => lista.GetArrayElementAtIndex(i).objectReferenceValue)
                    .Where(o => o != null).Select(o => o.name).ToArray();
                CollectionAssert.Contains(nombres, "CarroTrafico_V09");
                CollectionAssert.Contains(nombres, "CarroTrafico_V10");
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }

        [Test]
        public void Trafico_TraeVariantesDeColorCiviles()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Additive);
            try
            {
                Component trafico = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                    .Select(t => t.GetComponent("TraficoCalle")).First(c => c != null);
                SerializedProperty lista = new SerializedObject(trafico).FindProperty("prefabsCarro");
                string[] nombres = Enumerable.Range(0, lista.arraySize).Select(i => lista.GetArrayElementAtIndex(i).objectReferenceValue)
                    .Where(o => o != null).Select(o => o.name).ToArray();
                foreach (var v in ColocarS2nCarros.Variantes)
                {
                    CollectionAssert.Contains(nombres, "CarroTrafico_" + v.codigo + "_" + v.color, "La variante está en el tráfico");
                    Material m = AssetDatabase.LoadAssetAtPath<Material>(ColocarS2nCarros.RutaMaterial(v.codigo, v.color));
                    Assert.IsNotNull(m);
                    Color tinte = m.GetColor("_BaseColor");
                    Assert.AreEqual(v.tinte.r, tinte.r, 0.001f, "Tinte de la variante");
                    Assert.AreEqual(v.tinte.g, tinte.g, 0.001f, "Tinte de la variante");
                    Assert.AreEqual(v.tinte.b, tinte.b, 0.001f, "Tinte de la variante");
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ColocarS2nCarros.RutaPrefab(v.codigo, v.color));
                    Assert.IsTrue(prefab.GetComponentsInChildren<Renderer>(true).Any(r => r.sharedMaterials.Contains(m)), v.codigo + " " + v.color + " usa su material");
                }
                Assert.GreaterOrEqual(nombres.Distinct().Count(), 11, "Cinco modelos y seis variantes");
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}

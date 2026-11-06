using System.Collections.Generic;
using System.IO;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-807 (noche 6-nov, sesión 2): importador ImportarS2n y líneas pintadas de la calle (ColocarS2nLineas) en Ciudad_Prologo.</summary>
    public class LineasS2nPruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";

        [Test]
        public void Importador_DosVeces_NoCambiaNada()
        {
            ImportarS2n.MeterTodo();
            string[] rutas = ImportarS2n.Entradas.Select(e => e.Destino).Concat(new[] { ImportarS2n.MaterialAmarilla, ImportarS2n.MaterialBlanca, ImportarS2n.MaterialCebra }).ToArray();
            Dictionary<string, byte[]> antes = rutas.ToDictionary(r => r, File.ReadAllBytes);
            ImportarS2n.MeterTodo();
            string informe = ImportarS2n.Informe.ToString();
            StringAssert.DoesNotContain("copiado", informe);
            StringAssert.DoesNotContain("respaldo hecho", informe);
            StringAssert.DoesNotContain("material nuevo", informe);
            StringAssert.DoesNotContain("ajustes de importación", informe);
            foreach (string r in rutas)
            {
                CollectionAssert.AreEqual(antes[r], File.ReadAllBytes(r), r + " cambió en la segunda pasada");
            }
        }

        [Test]
        public void Importador_MaterialesTransparentesConSuTextura()
        {
            foreach ((string mat, string tex) in new[] { (ImportarS2n.MaterialAmarilla, ImportarS2n.TexturaAmarilla), (ImportarS2n.MaterialCebra, ImportarS2n.TexturaCebra), (ImportarS2n.MaterialBlanca, ImportarS2n.TexturaBlanca) })
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(mat);
                Assert.IsNotNull(m, mat);
                Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(tex), m.GetTexture("_BaseMap"), mat + " usa " + tex);
                Assert.AreEqual(1f, m.GetFloat("_Surface"), mat + " transparente");
                Assert.AreEqual(0f, m.GetFloat("_ZWrite"), mat + " no escribe profundidad");
                Assert.AreEqual((float)BlendMode.SrcAlpha, m.GetFloat("_SrcBlend"), mat + ": mezcla por alfa (el negro no suma brillo)");
                Assert.IsFalse(m.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON"), mat + " sin premultiplicar");
                TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(tex);
                Assert.AreEqual(TextureImporterAlphaSource.FromGrayScale, ti.alphaSource, tex + ": negro = transparente");
                Assert.AreEqual(TextureWrapMode.Repeat, ti.wrapMode, tex);
                Assert.IsTrue(ti.mipmapEnabled, tex);
            }
        }

        [Test]
        public void Escena_LineasEncimaDelAsfalto_SinColliderNiSombra_YSinRayasDeCubo()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Additive);
            try
            {
                List<Transform> todos = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToList();
                Transform raiz = escena.GetRootGameObjects().First(g => g.name == ColocarS2nLineas.NombreRaiz).transform;
                foreach (string grupo in new[] { "Amarillas", "Blancas", "Cebras" })
                {
                    Assert.Greater(raiz.Find(grupo).childCount, 0, grupo);
                }
                Assert.AreEqual(14, raiz.Find("Cebras").childCount, "una cebra por boca: 4 + 4 en los cruces del barrio y 3 + 3 en las vueltas de T-805");
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
                {
                    Assert.IsEmpty(r.GetComponents<Collider>(), r.name + " sin collider");
                    Assert.AreEqual(ShadowCastingMode.Off, r.shadowCastingMode, r.name + " sin sombra");
                    Assert.Greater(r.transform.position.y, 0.005f, r.name + " encima del asfalto");
                    Assert.Less(r.transform.position.y, 0.05f, r.name + " pegado al asfalto");
                    Assert.Greater(Vector3.Dot(r.transform.forward, Vector3.down), 0.99f, r.name + " plano, mirando arriba");
                }
                // Pegadas al asfalto que tienen abajo (ni flotando ni hundidas): rayo desde 1 m hacia abajo en el centro y en las puntas.
                Physics.SyncTransforms();
                List<string> malas = new List<string>();
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
                {
                    Transform t = r.transform;
                    foreach (float f in new[] { -0.45f, 0f, 0.45f })
                    {
                        Vector3 p = t.position + t.up * (t.lossyScale.y * f);
                        // El golpe más bajo es el piso: encima puede haber puestos del tianguis (mesas, tambos) sobre la calle.
                        RaycastHit[] golpes = Physics.RaycastAll(p + Vector3.up, Vector3.down, 2f);
                        if (golpes.Length == 0)
                        {
                            malas.Add(r.name + " " + p + " sin piso");
                            continue;
                        }
                        RaycastHit golpe = golpes.OrderBy(g => g.point.y).First();
                        float sobre = p.y - golpe.point.y;
                        if (sobre < 0.002f || sobre > 0.04f)
                        {
                            malas.Add(r.name + " " + p + " a " + sobre.ToString("F3") + " m de " + golpe.collider.name);
                        }
                    }
                }
                Assert.IsEmpty(malas, "Líneas flotando, hundidas o encima de algo que no es el asfalto: " + string.Join(" | ", malas.Take(10)));
                // Cubre la calle nueva de T-805.
                Assert.IsTrue(raiz.Find("Amarillas").Cast<Transform>().Any(t => Mathf.Abs(t.position.z - ColocarS2nLineas.CentroCruceSur) < 0.1f && Mathf.Abs(t.position.x) < 20f), "doble amarilla en la calle nueva");
                Renderer[] viejas = todos.Where(t => (t.name == "Raya" || t.name == "Cebra") && t.root != raiz).Select(t => t.GetComponent<Renderer>()).Where(r => r != null && r.enabled).ToArray();
                Assert.IsEmpty(viejas, "rayas o cebras de cubo visibles");
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}

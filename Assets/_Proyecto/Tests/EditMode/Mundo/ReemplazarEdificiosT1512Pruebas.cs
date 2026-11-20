using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Mundo.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Tipo = MechaCyberpunk.Mundo.Editor.CalculoEdificiosT1502.Tipo;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1512: edificios ED importados y puestos en Ciudad_Prologo. Las pruebas de paleta y conteo son puras; las de prefabs piden haber
    /// corrido ImportarEdificiosT1512.Aplicar y las de escena, además, ReemplazarEdificiosT1512.Aplicar.
    /// </summary>
    public class ReemplazarEdificiosT1512Pruebas
    {
        private const float ToleranciaPiso = 0.05f;
        private const float ToleranciaAsentado = 0.3f;
        private const float HolguraEncime = -2f;
        private static readonly string[] Muros = { "MU01_MuroPerimetral_a", "MU01_MuroPerimetral_b", "MU02_TorreControl_a", "MU03_PuertaMuro_a" };

        [Test]
        public void NombresPara_CadaBarrioConPaleta_TieneEdificiosEdValidos()
        {
            foreach (string barrio in CalculoEdificiosT1512.BarriosConPaleta())
            {
                string[] nombres = CalculoEdificiosT1512.NombresPara(barrio);
                Assert.Greater(nombres.Length, 0, barrio);
                foreach (string n in nombres)
                {
                    Assert.Contains(n, CalculoEdificiosT1512.TodosLosEd, barrio + " usa " + n);
                }
            }
        }

        [Test]
        public void NombresPara_BarrioDesconocido_DaTodosLosEd()
        {
            CollectionAssert.AreEquivalent(CalculoEdificiosT1512.TodosLosEd, CalculoEdificiosT1512.NombresPara(""));
            CollectionAssert.AreEquivalent(CalculoEdificiosT1512.TodosLosEd, CalculoEdificiosT1512.NombresPara("Otro"));
        }

        [Test]
        public void NombresPara_SuburbiosYNeon_SeparanLosEstilos()
        {
            Assert.Contains("ED01_CasaSuburbios_a", CalculoEdificiosT1512.NombresPara("Suburbios"));
            Assert.Contains("ED07_HotelNeon_a", CalculoEdificiosT1512.NombresPara("NeonAlto"));
            Assert.IsFalse(CalculoEdificiosT1512.NombresPara("NeonAlto").Contains("ED01_CasaSuburbios_a"));
            Assert.IsTrue(CalculoEdificiosT1512.RutasPara("Tianguis").All(r => r.StartsWith("Assets/Arte/Zonas/ED/") && r.EndsWith(".prefab")));
        }

        [Test]
        public void Contar_PrimitivasMezcladas_SeparaAnotadosDeSinModelo()
        {
            var lista = new List<ReemplazarEdificiosT1502.Primitivo>
            {
                new ReemplazarEdificiosT1502.Primitivo { Tipo = Tipo.EdificioAfueras, TienePendiente = true, Ruta = "A/Salida_1/Edificio" },
                new ReemplazarEdificiosT1502.Primitivo { Tipo = Tipo.EdificioAfueras, TienePendiente = false, Ruta = "A/Salida_2/Edificio" },
                new ReemplazarEdificiosT1502.Primitivo { Tipo = Tipo.Torre, Ruta = "T" },
                new ReemplazarEdificiosT1502.Primitivo { Tipo = Tipo.Torre, Ruta = "T2" }
            };
            ReemplazarEdificiosT1512.Resumen r = ReemplazarEdificiosT1512.Contar(lista);
            Assert.AreEqual(1, r.AfuerasAnotados);
            Assert.AreEqual(1, r.AfuerasSinModelo);
            Assert.AreEqual("A/Salida_2/Edificio", r.RutasSinModelo[0]);
            Assert.AreEqual(2, r.PorTipo[Tipo.Torre]);
            Assert.AreEqual(2, r.PorTipo[Tipo.EdificioAfueras]);
        }

        [Test]
        public void ModelosEd_TodosImportadosConPrefab()
        {
            foreach (string nombre in CalculoEdificiosT1512.TodosLosEd)
            {
                string ruta = CalculoEdificiosT1512.RutaPrefab(nombre);
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(ruta), "Falta el prefab " + ruta + " (corre ImportarEdificiosT1512.Aplicar)");
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Zonas/ED/" + nombre + "/" + nombre + ".fbx"), "Falta el FBX de " + nombre);
            }
        }

        [Test]
        public void ModelosMuro_ImportadosSoloConPrefab_SinColocar()
        {
            foreach (string nombre in Muros)
            {
                string ruta = "Assets/Arte/Zonas/MU/" + nombre + ".prefab";
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(ruta), "Falta el prefab " + ruta);
            }
        }

        [Test]
        public void PrefabsEd_AsentadosEnElPisoYConCollider()
        {
            foreach (string nombre in CalculoEdificiosT1512.TodosLosEd)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CalculoEdificiosT1512.RutaPrefab(nombre));
                Assert.IsNotNull(prefab, nombre);
                GameObject temporal = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                try
                {
                    Renderer[] rs = temporal.GetComponentsInChildren<Renderer>(true);
                    Assert.Greater(rs.Length, 0, nombre + " sin renderers");
                    Bounds b = rs[0].bounds;
                    foreach (Renderer r in rs)
                    {
                        b.Encapsulate(r.bounds);
                    }
                    Assert.AreEqual(0f, b.min.y, ToleranciaPiso, nombre + " no está asentado en y = 0");
                    Assert.IsNotNull(temporal.GetComponentInChildren<Collider>(true), nombre + " sin collider");
                    Assert.Greater(b.size.y, 4f, nombre + " mide menos de 4 m de alto");
                }
                finally
                {
                    Object.DestroyImmediate(temporal);
                }
            }
        }

        // Pasa solo después de ReemplazarEdificiosT1512.Aplicar (que guarda la escena): todo cubo de las afueras visible tiene su marca.
        [Test]
        public void Escena_CubosDeLasAfueras_ConModeloOAnotados()
        {
            Scene escena = EditorSceneManager.OpenScene(CreadorPrologoCiudad.RutaEscena, OpenSceneMode.Additive);
            try
            {
                ReemplazarEdificiosT1512.Resumen r = ReemplazarEdificiosT1512.Contar(ReemplazarEdificiosT1502.Escanear(escena));
                Assert.AreEqual(0, r.AfuerasSinModelo, "Cubos de las afueras sin modelo ni marca: " + string.Join(", ", r.RutasSinModelo.Take(5)));
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }

        [Test]
        public void Escena_ModelosEd_AsentadosYSinEncimarse()
        {
            Scene escena = EditorSceneManager.OpenScene(CreadorPrologoCiudad.RutaEscena, OpenSceneMode.Additive);
            try
            {
                List<(GameObject objeto, Bounds caja)> modelos = new List<(GameObject, Bounds)>();
                foreach (GameObject raiz in escena.GetRootGameObjects())
                {
                    foreach (Transform t in raiz.GetComponentsInChildren<Transform>(false))
                    {
                        if (!t.name.StartsWith(ReemplazarEdificiosT1502.PrefijoModelo + "_ED"))
                        {
                            continue;
                        }
                        Renderer[] rs = t.GetComponentsInChildren<Renderer>(false);
                        if (rs.Length == 0)
                        {
                            continue;
                        }
                        Bounds b = rs[0].bounds;
                        foreach (Renderer r in rs)
                        {
                            b.Encapsulate(r.bounds);
                        }
                        modelos.Add((t.gameObject, b));
                        Transform cubo = t.parent != null ? t.parent.Find("Edificio") : null;
                        Collider c = cubo != null ? cubo.GetComponent<Collider>() : null;
                        if (c != null)
                        {
                            Assert.AreEqual(c.bounds.min.y, b.min.y, ToleranciaAsentado, t.name + " no está asentado sobre el piso de su cubo");
                        }
                    }
                }
                for (int i = 0; i < modelos.Count; i++)
                {
                    Bounds a = modelos[i].caja;
                    a.Expand(HolguraEncime);
                    for (int j = i + 1; j < modelos.Count; j++)
                    {
                        Bounds b = modelos[j].caja;
                        b.Expand(HolguraEncime);
                        Assert.IsFalse(a.Intersects(b), modelos[i].objeto.name + " se encima con " + modelos[j].objeto.name);
                    }
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}

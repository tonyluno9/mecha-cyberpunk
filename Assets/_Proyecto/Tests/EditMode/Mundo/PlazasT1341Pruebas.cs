using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1341: las plazas de barrio (existencia, grupos, instancias de prefab, sin primitivos, bancas al kiosco, sin encimarse).</summary>
    public class PlazasT1341Pruebas
    {
        private static readonly string[] Primitivos = { "Cube", "Plane", "Quad", "Cylinder", "Sphere", "Capsule" };
        private static readonly string[] Grupos = { PlazasT1341.GrupoArboles, PlazasT1341.GrupoBancas, PlazasT1341.GrupoJardineras, PlazasT1341.GrupoKiosco };
        private const float ToleranciaGrados = 0.5f;

        private static GameObject Cargar(string plaza)
        {
            string ruta = PlazasT1341.RutaPrefab(plaza);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(ruta), plaza + " no existe en " + ruta);
            return PrefabUtility.LoadPrefabContents(ruta);
        }

        private static int Contar(GameObject raiz, string grupo)
        {
            Transform t = raiz.transform.Find(grupo);
            return t == null ? 0 : t.childCount;
        }

        [TestCase(PlazasT1341.PlazaKiosco, 1, 8, 8, 4)]
        [TestCase(PlazasT1341.PlazaEsquina, 0, 2, 1, 2)]
        [TestCase(PlazasT1341.PlazaBanqueta, 0, 2, 3, 0)]
        public void Plaza_Existe_ConLosGruposPedidos(string plaza, int kiosco, int bancas, int arboles, int jardineras)
        {
            GameObject raiz = Cargar(plaza);
            try
            {
                Assert.AreEqual(kiosco, Contar(raiz, PlazasT1341.GrupoKiosco));
                Assert.AreEqual(bancas, Contar(raiz, PlazasT1341.GrupoBancas));
                Assert.AreEqual(arboles, Contar(raiz, PlazasT1341.GrupoArboles));
                Assert.AreEqual(jardineras, Contar(raiz, PlazasT1341.GrupoJardineras));
                foreach (string g in Grupos)
                {
                    Assert.AreEqual(PlazasT1341.PiezasDe(plaza).Count(p => p.Grupo == g), Contar(raiz, g), g);
                }
                Assert.IsNull(raiz.GetComponent<Collider>(), "la raíz no lleva collider propio");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        [TestCase(PlazasT1341.PlazaKiosco)]
        [TestCase(PlazasT1341.PlazaEsquina)]
        [TestCase(PlazasT1341.PlazaBanqueta)]
        public void Plaza_TodosLosHijosSonInstanciasDePrefab_YSinPrimitivos(string plaza)
        {
            GameObject raiz = Cargar(plaza);
            try
            {
                List<string> permitidas = PlazasT1341.PiezasDe(plaza).Select(p => p.Ruta).Distinct().ToList();
                int revisados = 0;
                foreach (string g in Grupos)
                {
                    Transform grupo = raiz.transform.Find(g);
                    if (grupo == null)
                    {
                        continue;
                    }
                    foreach (Transform hijo in grupo)
                    {
                        GameObject fuente = PrefabUtility.GetCorrespondingObjectFromSource(hijo.gameObject);
                        Assert.IsNotNull(fuente, hijo.name + " no es instancia de prefab");
                        CollectionAssert.Contains(permitidas, AssetDatabase.GetAssetPath(fuente), hijo.name);
                        revisados++;
                    }
                }
                Assert.AreEqual(PlazasT1341.PiezasDe(plaza).Count, revisados);
                foreach (MeshFilter mf in raiz.GetComponentsInChildren<MeshFilter>(true))
                {
                    Assert.IsNotNull(mf.sharedMesh, mf.name);
                    CollectionAssert.DoesNotContain(Primitivos, mf.sharedMesh.name, mf.name + " usa un primitivo");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        [Test]
        public void PlazaKiosco_CadaBancaMiraAlCentro()
        {
            GameObject raiz = Cargar(PlazasT1341.PlazaKiosco);
            try
            {
                Transform bancas = raiz.transform.Find(PlazasT1341.GrupoBancas);
                Assert.AreEqual(8, bancas.childCount);
                foreach (Transform banca in bancas)
                {
                    Vector3 p = banca.localPosition;
                    float esperado = Mathf.Atan2(-p.x, -p.z) * Mathf.Rad2Deg + PlazasT1341.GiroBanca;
                    Assert.AreEqual(0f, Mathf.DeltaAngle(banca.localEulerAngles.y, esperado), ToleranciaGrados, banca.name);
                    Assert.AreEqual(6.5f, new Vector2(p.x, p.z).magnitude, 0.01f, banca.name);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        [Test]
        public void PlazaKiosco_NingunaBancaSeEncimaConElKioscoNiConLosArboles()
        {
            GameObject raiz = Cargar(PlazasT1341.PlazaKiosco);
            try
            {
                Rect kiosco = LimitesXz(raiz.transform.Find(PlazasT1341.GrupoKiosco).gameObject);
                List<Rect> arboles = new List<Rect>();
                foreach (Transform a in raiz.transform.Find(PlazasT1341.GrupoArboles))
                {
                    arboles.Add(LimitesXz(a.gameObject));
                }
                Assert.AreEqual(8, arboles.Count);
                foreach (Transform banca in raiz.transform.Find(PlazasT1341.GrupoBancas))
                {
                    Rect b = LimitesXz(banca.gameObject);
                    Assert.IsFalse(b.Overlaps(kiosco), banca.name + " se encima con el kiosco");
                    for (int i = 0; i < arboles.Count; i++)
                    {
                        Assert.IsFalse(b.Overlaps(arboles[i]), banca.name + " se encima con el árbol " + i);
                    }
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        private static Rect LimitesXz(GameObject objeto)
        {
            Bounds b = ArteT250.Limites(objeto);
            Assert.Greater(b.size.x, 0f, objeto.name + " sin mallas");
            return new Rect(b.min.x, b.min.z, b.size.x, b.size.z);
        }
    }
}

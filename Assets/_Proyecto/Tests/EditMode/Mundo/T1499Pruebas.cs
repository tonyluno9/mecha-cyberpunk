using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1499: maletín MA01, parque en K09, camioneta V11 desde 3 estrellas y mezcla de peatones NP07 a NP14.</summary>
    public class T1499Pruebas
    {
        // ---------------------------------------------------------------- maletín

        [Test]
        public void Maletin_PrefabUsaElFbxDeMA01_ConColliderYMedida()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ImportarMaletinT1499.RutaPrefabResources);
            Assert.IsNotNull(prefab, "correr ImportarMaletinT1499.Importar");
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(ImportarMaletinT1499.RutaPrefabArte));
            Transform visual = prefab.transform.Find("Visual");
            Assert.IsNotNull(visual);
            GameObject fuente = PrefabUtility.GetCorrespondingObjectFromSource(visual.gameObject);
            Assert.AreEqual(ImportarMaletinT1499.RutaFbx, AssetDatabase.GetAssetPath(fuente), "el visual es el FBX de MA01");
            Assert.IsNotNull(prefab.GetComponent<BoxCollider>());
            Assert.IsFalse(prefab.GetComponentsInChildren<MeshFilter>().Any(f => f.sharedMesh != null && f.sharedMesh.name == "Cube"), "sin primitivas");
            BoxCollider c = prefab.GetComponent<BoxCollider>();
            Assert.AreEqual(ImportarMaletinT1499.LargoMetros, Mathf.Max(c.size.x, c.size.z), 0.04f);
            Assert.Less(c.size.y, 0.3f);
            Assert.AreEqual(0f, c.center.y - c.size.y * 0.5f, 0.02f, "la base está en el origen");
        }

        // ---------------------------------------------------------------- parque

        [Test]
        public void Parque_TieneKioscoBancasYArbolesReales_YApagaLasPrimitivas()
        {
            Scene escena = EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Additive);
            try
            {
                Transform plaza = ColocarParqueT1499.BuscarPlaza(escena);
                Assert.IsNotNull(plaza);
                Transform grupo = plaza.Find(ColocarParqueT1499.Grupo);
                Assert.IsNotNull(grupo, "correr ColocarParqueT1499.Aplicar");
                Transform[] piezas = grupo.Cast<Transform>().ToArray();
                Assert.AreEqual(1, Cuantas(piezas, PlazasT1341.RutaKiosco), "kiosco PZ02");
                Assert.GreaterOrEqual(Cuantas(piezas, PlazasT1341.RutaBanca), 2, "bancas PZ01");
                Assert.GreaterOrEqual(Cuantas(piezas, PlazasT1341.RutaArbolChico) + Cuantas(piezas, PlazasT1341.RutaArbolGrande), 2, "árboles AR01/AR02");
                Assert.GreaterOrEqual(Cuantas(piezas, PlazasT1341.RutaJardinera), 1, "jardineras PZ03");
                foreach (Transform t in piezas)
                {
                    Assert.IsTrue(PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject), t.name + " es un prefab, no un invento");
                    Assert.IsFalse(t.GetComponentsInChildren<MeshFilter>().Any(f => f.sharedMesh != null && f.sharedMesh.name == "Cube"), t.name + " sin primitivas");
                }
                foreach (string n in ColocarParqueT1499.ApagarNombres)
                {
                    foreach (Transform viejo in plaza.Cast<Transform>().Where(t => t.name == n))
                    {
                        Assert.IsFalse(viejo.gameObject.activeSelf, n + " viejo apagado");
                    }
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }

        [Test]
        public void Parque_NadaFlotaNiSeEncimaNiPisaLaCalle()
        {
            Scene escena = EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Additive);
            try
            {
                Transform grupo = ColocarParqueT1499.BuscarPlaza(escena).Find(ColocarParqueT1499.Grupo);
                Assert.IsNotNull(grupo, "correr ColocarParqueT1499.Aplicar");
                Physics.SyncTransforms();
                Transform[] piezas = grupo.Cast<Transform>().ToArray();
                foreach (Transform t in piezas)
                {
                    Vector3 p = t.position;
                    Assert.IsTrue(ColocarParqueT1499.BuscarPiso(p, grupo, out float y), t.name + " tiene piso");
                    Assert.AreEqual(y, p.y, 0.15f, t.name + " no flota ni se hunde");
                    Assert.That(p.x, Is.InRange(ColocarT1320Calles.PlazaX0, ColocarT1320Calles.PlazaX1), t.name + " dentro de la plaza (x)");
                    Assert.That(p.z, Is.InRange(ColocarT1320Calles.PlazaZ0, ColocarT1320Calles.PlazaZ1), t.name + " dentro de la plaza (z)");
                    Assert.IsNull(ColocarParqueT1499.ChocaCon(t.gameObject), t.name + " no se encima con otro objeto");
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }

        private static int Cuantas(Transform[] piezas, string rutaPrefab)
        {
            GameObject fuente = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab);
            return piezas.Count(t => PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject) == fuente);
        }

        // ---------------------------------------------------------------- camioneta

        [Test]
        public void Camioneta_NoSaleConMenosDeTresEstrellas()
        {
            for (int estrellas = 0; estrellas <= 2; estrellas++)
            {
                for (int i = 0; i < 4; i++)
                {
                    Assert.IsFalse(CalculoPersecucion.UsaCamioneta(estrellas, i, 3), estrellas + " estrellas, patrulla " + i);
                }
            }
        }

        [Test]
        public void Camioneta_ConTresEstrellasSaleUnaDeCadaDos()
        {
            // Con 3 estrellas hay 3 patrullas (ConfigPersecucion_Trafico): la segunda es camioneta.
            Assert.IsFalse(CalculoPersecucion.UsaCamioneta(3, 0, 3));
            Assert.IsTrue(CalculoPersecucion.UsaCamioneta(3, 1, 3));
            Assert.IsFalse(CalculoPersecucion.UsaCamioneta(3, 2, 3));
        }

        [Test]
        public void Camioneta_ConCincoEstrellasLasExtrasSonTodasCamionetas()
        {
            for (int i = 3; i < 6; i++)
            {
                Assert.IsTrue(CalculoPersecucion.UsaCamioneta(5, i, 3), "patrulla extra " + i);
            }
            Assert.IsFalse(CalculoPersecucion.UsaCamioneta(5, 0, 3));
        }

        [Test]
        public void Camioneta_BarrioLaTieneConectadaAlGestor()
        {
            GameObject barrio = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab");
            Component gestor = barrio.GetComponentsInChildren<MonoBehaviour>(true).First(m => m != null && m.GetType().Name == "GestorPersecucion");
            Object camioneta = new SerializedObject(gestor).FindProperty("prefabCamioneta").objectReferenceValue;
            Assert.IsNotNull(camioneta, "GestorPersecucion.prefabCamioneta");
            Assert.AreEqual("Assets/_Proyecto/Trafico/Prefabs/PatrullaCamioneta.prefab", AssetDatabase.GetAssetPath(camioneta));
        }

        // ---------------------------------------------------------------- peatones

        private static readonly string[] Variantes =
        {
            "NP07_Pescador_a", "NP07_Pescador_b", "NP08_Turista_a", "NP08_Turista_b", "NP09_Corredor_a", "NP09_Corredor_b", "NP10_Elotero_a",
            "NP11_Estibador_a", "NP11_Estibador_b", "NP12_Abuela_a", "NP13_Repartidor_a", "NP13_Repartidor_b", "NP14_Oficinista_a", "NP14_Oficinista_b",
        };

        [Test]
        public void Peatones_PeatonVariadoTieneLasCatorceVariantesNP07aNP14_YCadaUnaSeElige()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Trafico/Prefabs/Peaton.prefab");
            Component variado = prefab.GetComponentsInChildren<MonoBehaviour>(true).First(m => m != null && m.GetType().Name == "PeatonVariado");
            SerializedProperty lista = new SerializedObject(variado).FindProperty("variantes");
            string[] nombres = Enumerable.Range(0, lista.arraySize)
                .Select(i => AssetDatabase.GetAssetPath(lista.GetArrayElementAtIndex(i).objectReferenceValue)).Select(System.IO.Path.GetFileNameWithoutExtension).ToArray();
            CollectionAssert.AreEquivalent(Variantes.Select(v => "Visual_" + v).ToArray(), nombres, "las 14 variantes, sin repetir");
            // PeatonVariado.ElegirVariante: indice = floor(cual * N). Con tiradas parejas sale cada una exactamente una vez.
            int n = nombres.Length;
            int[] salidas = Enumerable.Range(0, n * 10).Select(k => Mathf.Min(n - 1, Mathf.FloorToInt((k + 0.5f) / (n * 10f) * n))).ToArray();
            Assert.AreEqual(n, salidas.Distinct().Count());
        }

        [Test]
        public void Peatones_NP01aNP06SigueSinModeloConRig()
        {
            // No hay FBX con rig de NP01 a NP06: no se inventan. Si esta prueba falla, ya llegaron: importarlos y mezclarlos (docs/assets-faltan.md).
            Assert.IsEmpty(AssetDatabase.FindAssets("t:Model", new[] { "Assets/Arte" }).Select(AssetDatabase.GUIDToAssetPath).Where(r => System.Text.RegularExpressions.Regex.IsMatch(r, "NP0[1-6]_")).ToArray());
        }
    }
}

using System.Collections.Generic;
using MechaCyberpunk.IA.APie;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-1511: los prefabs CB (escala, piso, collider, marca) y su colocación en Ciudad_Prologo (hay que correr antes CoberturaCbT1511.Importar y .Colocar).</summary>
    public class CoberturaCbT1511Pruebas
    {
        private const string Carpeta = "Assets/Arte/Zonas/CB/";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string NombreGrupo = "Cobertura_CB";
        private const int MinimoColocados = 12;
        private const float TolerPiso = 0.15f;

        private static readonly (string nombre, float min, float max)[] Esperados =
        {
            ("CB01_BarreraConcreto_a", 0.9f, 1.1f),
            ("CB01_BarreraConcreto_b", 0.9f, 1.1f),
            ("CB02_Costales_a", 0.9f, 1.1f),
            ("CB02_Costales_b", 0.9f, 1.1f),
            ("CB03_CajasMetal_a", 1.1f, 1.3f),
            ("CB03_CajasMetal_b", 1.1f, 1.3f),
            ("CB04_BarricadaPolicia_a", 1.0f, 1.2f),
        };

        private static Bounds Limites(GameObject o)
        {
            Renderer[] rs = o.GetComponentsInChildren<Renderer>(true);
            Assert.Greater(rs.Length, 0, "Sin renderers: " + o.name);
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        [Test]
        public void Prefabs_Existen_ConEscalaReal_ApoyadosEnElPiso()
        {
            foreach ((string nombre, float min, float max) in Esperados)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Carpeta + nombre + ".prefab");
                Assert.IsNotNull(prefab, "Falta el prefab " + nombre);
                GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                try
                {
                    instancia.transform.position = Vector3.zero;
                    Bounds b = Limites(instancia);
                    Assert.GreaterOrEqual(b.size.y, min, nombre + " muy bajo: " + b.size.y);
                    Assert.LessOrEqual(b.size.y, max, nombre + " muy alto: " + b.size.y);
                    Assert.AreEqual(0f, b.min.y, 0.02f, nombre + " no está apoyado en el piso");
                    Assert.Less(Mathf.Max(b.size.x, b.size.z), 4f, nombre + " demasiado largo");
                }
                finally
                {
                    Object.DestroyImmediate(instancia);
                }
            }
        }

        [Test]
        public void Prefabs_TienenColliderAjustado_YEstanMarcadosComoCobertura()
        {
            foreach ((string nombre, float, float) esperado in Esperados)
            {
                string nombre = esperado.Item1;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Carpeta + nombre + ".prefab");
                Assert.IsNotNull(prefab, "Falta el prefab " + nombre);
                Assert.IsNotNull(prefab.GetComponent<CoberturaCB>(), nombre + " sin CoberturaCB");
                BoxCollider caja = prefab.GetComponent<BoxCollider>();
                Assert.IsNotNull(caja, nombre + " sin BoxCollider");
                Assert.IsFalse(caja.isTrigger, nombre + ": el collider no puede ser trigger");
                GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                try
                {
                    instancia.transform.position = Vector3.zero;
                    Bounds visual = Limites(instancia);
                    Bounds collider = instancia.GetComponent<BoxCollider>().bounds;
                    Assert.AreEqual(visual.size.y, collider.size.y, 0.05f, nombre + ": collider de otra altura que el visual");
                    Assert.AreEqual(visual.size.x, collider.size.x, 0.05f, nombre + ": collider de otro largo que el visual");
                    Assert.AreEqual(visual.size.z, collider.size.z, 0.05f, nombre + ": collider de otro ancho que el visual");
                    Assert.AreEqual(prefab.GetComponent<CoberturaCB>().Altura, visual.size.y, 0.05f, nombre + ": altura marcada distinta");
                }
                finally
                {
                    Object.DestroyImmediate(instancia);
                }
            }
        }

        [Test]
        public void Escena_TieneLasCoberturasColocadas_ApoyadasEnElPiso_YSinEncimarse()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Additive);
            try
            {
                GameObject grupo = null;
                foreach (GameObject raiz in escena.GetRootGameObjects())
                {
                    if (raiz.name == NombreGrupo)
                    {
                        grupo = raiz;
                    }
                }
                Assert.IsNotNull(grupo, "Falta el grupo " + NombreGrupo + " en Ciudad_Prologo (correr CoberturaCbT1511.Colocar)");
                CoberturaCB[] cbs = grupo.GetComponentsInChildren<CoberturaCB>();
                Assert.GreaterOrEqual(cbs.Length, MinimoColocados, "Muy pocas coberturas colocadas");
                Physics.SyncTransforms();
                List<Bounds> cajas = new List<Bounds>();
                foreach (CoberturaCB cb in cbs)
                {
                    BoxCollider caja = cb.GetComponent<BoxCollider>();
                    Assert.IsNotNull(caja, cb.name + " sin collider");
                    Vector3 abajo = cb.transform.position + Vector3.up * 0.5f;
                    RaycastHit[] golpes = Physics.RaycastAll(abajo, Vector3.down, 2f, ~0, QueryTriggerInteraction.Ignore);
                    bool apoyado = false;
                    foreach (RaycastHit g in golpes)
                    {
                        if (g.collider.GetComponentInParent<CoberturaCB>() == null && Mathf.Abs(g.point.y - cb.transform.position.y) <= TolerPiso)
                        {
                            apoyado = true;
                        }
                    }
                    Assert.IsTrue(apoyado, cb.name + " no está apoyada en el piso en " + cb.transform.position);
                    foreach (Bounds otra in cajas)
                    {
                        Assert.IsFalse(otra.Intersects(caja.bounds), cb.name + " se encima con otra cobertura");
                    }
                    cajas.Add(caja.bounds);
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }
    }
}

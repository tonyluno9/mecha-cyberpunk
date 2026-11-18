using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1346: los módulos de Blender importados existen, chocan, usan URP Lit y no salen rosas ni con tamaños absurdos.</summary>
    public class ModulosT1346Pruebas
    {
        private const float LadoMinimo = 0.3f;
        private const float LadoMaximo = 200f;

        private static IEnumerable<string> Nombres()
        {
            return ImportarModulosT1346.NombresPrefabs();
        }

        private static GameObject Prefab(string nombre)
        {
            GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(ImportarModulosT1346.RutaPrefab(nombre));
            Assert.IsNotNull(p, "Falta el prefab " + nombre);
            return p;
        }

        [Test]
        public void Prefabs_Existen_18Modulos_Y4Contenedores()
        {
            Assert.AreEqual(22, Nombres().Count());
            foreach (string n in Nombres())
            {
                Assert.IsNotNull(Prefab(n).transform.Find("Visual"), n + " sin hijo Visual");
            }
        }

        [Test]
        public void Prefabs_TienenCollider()
        {
            foreach (string n in Nombres())
            {
                Assert.IsNotEmpty(Prefab(n).GetComponentsInChildren<Collider>(true), n + " sin collider");
            }
        }

        [Test]
        public void Prefabs_MaterialesNoNulosYUrpLit()
        {
            foreach (string n in Nombres())
            {
                Renderer[] renderers = Prefab(n).GetComponentsInChildren<Renderer>(true);
                Assert.IsNotEmpty(renderers, n + " sin renderers");
                foreach (Renderer r in renderers)
                {
                    foreach (Material m in r.sharedMaterials)
                    {
                        Assert.IsNotNull(m, n + " tiene material nulo");
                        Assert.AreEqual("Universal Render Pipeline/Lit", m.shader.name, n + "/" + m.name + " no es URP Lit");
                    }
                }
            }
        }

        [Test]
        public void Prefabs_SinTexturaRosa_TexturaOColorLiso()
        {
            foreach (string n in Nombres())
            {
                foreach (Renderer r in Prefab(n).GetComponentsInChildren<Renderer>(true))
                {
                    foreach (Material m in r.sharedMaterials)
                    {
                        if (ImportarModulosT1346.EsColorLiso(m.name))
                        {
                            continue;
                        }
                        Assert.IsNotNull(m.GetTexture("_BaseMap"), n + "/" + m.name + " sin _BaseMap");
                    }
                }
            }
        }

        [Test]
        public void RejaDS01_UsaRecorteAlfaYDobleCara()
        {
            Material malla = null;
            foreach (Renderer r in Prefab("DS01_RejaPuas").GetComponentsInChildren<Renderer>(true))
            {
                malla = r.sharedMaterials.FirstOrDefault(m => m != null && m.name == "Malla") ?? malla;
            }
            Assert.IsNotNull(malla, "DS01 sin material Malla");
            Assert.AreEqual(1f, malla.GetFloat("_AlphaClip"));
            Assert.IsTrue(malla.IsKeywordEnabled("_ALPHATEST_ON"));
            Assert.AreEqual(0f, malla.GetFloat("_Cull"));
        }

        [Test]
        public void Prefabs_LimitesRazonables()
        {
            foreach (string n in Nombres())
            {
                GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(Prefab(n));
                Bounds b = new Bounds();
                bool primero = true;
                foreach (Renderer r in p.GetComponentsInChildren<Renderer>(true))
                {
                    if (primero) { b = r.bounds; primero = false; } else { b.Encapsulate(r.bounds); }
                }
                Object.DestroyImmediate(p);
                float lado = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                Assert.GreaterOrEqual(lado, LadoMinimo, n + " demasiado chico: " + b.size);
                Assert.LessOrEqual(lado, LadoMaximo, n + " demasiado grande: " + b.size);
            }
        }
    }
}

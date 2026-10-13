using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-076: los 13 prefabs del kit urbano y el bar (tamaño, base, orientación, materiales y colliders).</summary>
    public class ArtePruebas
    {
        private const string RutaPrefabs = "Assets/_Proyecto/Mundo/Prefabs/Kit/";
        private const string RutaMateriales = "Assets/Arte/Materiales/";
        private const float ToleranciaTamano = 0.02f;
        private const float ToleranciaBase = 0.02f;
        private const float EmisionHdrMinima = 2f;

        // prefab, nombre en dimensiones.json, si debe tener collider
        private static readonly (string prefab, string fuente, bool collider)[] Piezas =
        {
            ("Poste_Luz", "poste_luz", true),
            ("Puesto_Tacos", "puesto_tacos", true),
            ("Letrero_Tacos", "letrero_tacos", false),
            ("Letrero_Farmacia", "letrero_farmacia", false),
            ("Letrero_Cantina", "letrero_cantina", false),
            ("Azotea_AC_Tinaco", "azotea_ac_tinaco", false),
            ("Transformador_Poste", "transformador_poste", true),
            ("Basurero_Grafiti", "basurero_grafiti", true),
            ("Fachada_Modular", "fachada_modular", false),
            ("Prop_Barra", "prop_barra", true),
            ("Prop_Mesa", "prop_mesa", true),
            ("Prop_DJ", "prop_dj", true),
            ("Prop_Letrero", "prop_letrero", false),
        };

        [Serializable]
        private class Dimension
        {
            public string nombre;
            public float ancho;
            public float alto;
            public float fondo;
        }

        [Serializable]
        private class ListaDimensiones
        {
            public Dimension[] piezas;
        }

        private readonly List<GameObject> instancias = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject instancia in instancias)
            {
                UnityEngine.Object.DestroyImmediate(instancia);
            }
            instancias.Clear();
        }

        private static GameObject Prefab(string nombre)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabs + nombre + ".prefab");
            Assert.IsNotNull(prefab, "Falta el prefab " + nombre);
            return prefab;
        }

        private GameObject Instanciar(string nombre)
        {
            GameObject instancia = UnityEngine.Object.Instantiate(Prefab(nombre));
            instancias.Add(instancia);
            return instancia;
        }

        private static Bounds Limites(GameObject objeto)
        {
            Renderer[] renderers = objeto.GetComponentsInChildren<Renderer>();
            Assert.Greater(renderers.Length, 0, objeto.name + " no tiene renderers");
            Bounds limites = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                limites.Encapsulate(renderers[i].bounds);
            }
            return limites;
        }

        private static Dimension DimensionDe(string fuente)
        {
            string json = File.ReadAllText("Assets/Arte/Mundo/dimensiones.json");
            foreach (Dimension d in JsonUtility.FromJson<ListaDimensiones>(json).piezas)
            {
                if (d.nombre == fuente)
                {
                    return d;
                }
            }
            Assert.Fail("dimensiones.json no trae " + fuente);
            return null;
        }

        private static void AssertCerca(float esperado, float real, string que)
        {
            Assert.AreEqual(esperado, real, Mathf.Max(esperado * ToleranciaTamano, 0.005f), que);
        }

        [Test]
        public void Prefabs_Existen_Los13()
        {
            Assert.AreEqual(13, Piezas.Length);
            foreach (var pieza in Piezas)
            {
                Prefab(pieza.prefab);
            }
        }

        [Test]
        public void Prefabs_TamanoCoincideConDimensionesJson()
        {
            foreach (var pieza in Piezas)
            {
                Bounds b = Limites(Instanciar(pieza.prefab));
                Dimension d = DimensionDe(pieza.fuente);
                AssertCerca(d.ancho, b.size.x, pieza.prefab + " ancho");
                AssertCerca(d.alto, b.size.y, pieza.prefab + " alto");
                AssertCerca(d.fondo, b.size.z, pieza.prefab + " fondo");
            }
        }

        [Test]
        public void Prefabs_BaseEnYCero_RaizSinRotacionNiEscala()
        {
            foreach (var pieza in Piezas)
            {
                GameObject instancia = Instanciar(pieza.prefab);
                Assert.AreEqual(0f, Limites(instancia).min.y, ToleranciaBase, pieza.prefab + " base");
                Assert.AreEqual(Quaternion.identity, instancia.transform.rotation, pieza.prefab + " rotación de la raíz");
                Assert.AreEqual(Vector3.one, instancia.transform.localScale, pieza.prefab + " escala de la raíz");
            }
        }

        [Test]
        public void Prefabs_UsanSoloMaterialesDeArteUrpLit()
        {
            foreach (var pieza in Piezas)
            {
                foreach (Renderer renderer in Prefab(pieza.prefab).GetComponentsInChildren<Renderer>())
                {
                    Assert.Greater(renderer.sharedMaterials.Length, 0, pieza.prefab);
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        Assert.IsNotNull(material, pieza.prefab + " tiene un material vacío");
                        string ruta = AssetDatabase.GetAssetPath(material);
                        Assert.IsTrue(ruta.StartsWith(RutaMateriales), pieza.prefab + " usa " + material.name + " fuera de Materiales: " + ruta);
                        Assert.AreEqual("Universal Render Pipeline/Lit", material.shader.name, material.name);
                    }
                }
            }
        }

        [Test]
        public void Prefabs_NeonTieneEmisionHdr()
        {
            int neones = 0;
            foreach (var pieza in Piezas)
            {
                foreach (Renderer renderer in Prefab(pieza.prefab).GetComponentsInChildren<Renderer>())
                {
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (!material.name.StartsWith("Neon_"))
                        {
                            continue;
                        }
                        neones++;
                        Assert.IsTrue(material.IsKeywordEnabled("_EMISSION"), material.name + " sin _EMISSION");
                        Color e = material.GetColor("_EmissionColor");
                        Assert.GreaterOrEqual(Mathf.Max(e.r, e.g, e.b), EmisionHdrMinima, material.name + " no es HDR");
                    }
                }
            }
            Assert.Greater(neones, 0, "Ninguna pieza usa neón");
        }

        [Test]
        public void FachadaModular_FrenteHaciaMasZ()
        {
            GameObject fachada = Prefab("Fachada_Modular");
            MeshFilter filtro = fachada.GetComponentInChildren<MeshFilter>();
            Renderer renderer = filtro.GetComponent<Renderer>();
            Mesh malla = filtro.sharedMesh;
            // El neón (rótulo TIENDA, ventanas, tubos) va pegado al frente de la fachada. Se usa en vez de Concreto_Manchas
            // porque el zócalo y el hueco del local también tienen vértices atrás y su promedio queda bajo el centro de la caja.
            Vector3[] vertices = malla.vertices;
            float suma = 0f;
            int cuenta = 0;
            float minZ = float.MaxValue;
            float maxZ = float.MinValue;
            Material[] materiales = renderer.sharedMaterials;
            for (int sub = 0; sub < materiales.Length; sub++)
            {
                if (!materiales[sub].name.StartsWith("Neon_"))
                {
                    continue;
                }
                foreach (int indice in malla.GetIndices(sub))
                {
                    suma += filtro.transform.TransformPoint(vertices[indice]).z;
                    cuenta++;
                }
            }
            foreach (Vector3 v in vertices)
            {
                float z = filtro.transform.TransformPoint(v).z;
                minZ = Mathf.Min(minZ, z);
                maxZ = Mathf.Max(maxZ, z);
            }
            Assert.Greater(cuenta, 0);
            Assert.Greater(suma / cuenta, (minZ + maxZ) * 0.5f, "El neón del frente debe quedar hacia +Z");
        }

        [Test]
        public void Prefabs_Colliders_SoloDondeSePuedeChocar()
        {
            foreach (var pieza in Piezas)
            {
                Collider[] colliders = Prefab(pieza.prefab).GetComponentsInChildren<Collider>();
                if (!pieza.collider)
                {
                    Assert.AreEqual(0, colliders.Length, pieza.prefab + " no debe tener collider");
                    continue;
                }
                bool haySolido = false;
                foreach (Collider collider in colliders)
                {
                    haySolido |= collider is BoxCollider && !collider.isTrigger;
                }
                Assert.IsTrue(haySolido, pieza.prefab + " necesita un BoxCollider que no sea trigger");
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-506: en Ciudad_Prologo no queda greybox visible (primitiva de Unity con material de color liso, sin textura y sin brillo propio).
    /// Excepciones invisibles o que no son greybox: sin renderer, colliders, triggers, pintura plana del piso, la arena y el maniquí enterrado.
    /// </summary>
    public class SinGreyboxT506Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float UmbralEmision = 0.3f;
        // Raíces fuera de la demo (arena, creador de personaje, negocios y guarida descartados).
        private static readonly string[] RaicesExcluidas = { "JaulaClandestino", "Greybox_Apariencia", "GTA_T429", "GTA_T448" };
        // Charcos, gente de bulto de la arena y carteles con textura; la pintura plana del piso (rayas, cebras, grietas) se salta por grosor.
        private const float GrosorDecal = 0.12f;
        private static readonly string[] MaterialesDecal = { "Charco", "Publico", "Grafiti" };
        private static readonly string[] MallasPrimitivas = { "Cube", "Sphere", "Capsule", "Cylinder", "Plane", "Quad" };

        private Scene escena;
        private Renderer[] renderers;

        [OneTimeSetUp]
        public void Abrir()
        {
            escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Additive);
            renderers = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(false)).ToArray();
        }

        [OneTimeTearDown]
        public void Cerrar()
        {
            EditorSceneManager.CloseScene(escena, true);
        }

        private static bool TieneTextura(Material m)
        {
            Shader s = m.shader;
            for (int i = 0; i < s.GetPropertyCount(); i++)
            {
                if (s.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture && m.GetTexture(s.GetPropertyName(i)) != null) { return true; }
            }
            return false;
        }

        private static bool EsEfecto(Material m)
        {
            if (m.HasProperty("_EmissionColor") && m.GetColor("_EmissionColor").maxColorComponent > UmbralEmision) { return true; }
            if (m.HasProperty("_Surface") && m.GetFloat("_Surface") > 0.5f) { return true; }
            string sh = m.shader.name;
            return sh.Contains("Unlit") || sh.Contains("Particles") || sh.Contains("Sprites") || sh.StartsWith("MechaCyberpunk/Torre") || sh.Contains("TextMeshPro") || sh.Contains("UI/");
        }

        private static bool EnRaizExcluida(Transform t)
        {
            Transform raiz = t.root;
            if (RaicesExcluidas.Contains(raiz.name)) { return true; }
            for (Transform p = t; p != null; p = p.parent)
            {
                if (p.name == "VistaPreviaApariencia") { return true; }
            }
            return false;
        }

        private List<string> Greybox()
        {
            var lista = new List<string>();
            foreach (Renderer r in renderers)
            {
                if (!(r is MeshRenderer) || !r.enabled || !r.gameObject.activeInHierarchy || EnRaizExcluida(r.transform)) { continue; }
                Vector3 tam = r.bounds.size;
                if (Mathf.Min(tam.x, Mathf.Min(tam.y, tam.z)) < GrosorDecal) { continue; }
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null || !MallasPrimitivas.Contains(mf.sharedMesh.name)) { continue; }
                foreach (Material m in r.sharedMaterials)
                {
                    if (m == null) { lista.Add(r.name + " (sin material)"); break; }
                    if (MaterialesDecal.Any(d => m.name.Contains(d)) || EsEfecto(m)) { continue; }
                    if (!TieneTextura(m)) { lista.Add(r.transform.root.name + "/.../" + r.name + " | " + m.name); break; }
                }
            }
            return lista;
        }

        [Test]
        public void Zonas_NoQuedaPrimitivaConMaterialLisoVisible()
        {
            List<string> quedan = Greybox();
            Assert.IsEmpty(quedan, "Greybox visible (" + quedan.Count + "): " + string.Join("; ", quedan.Take(15)));
        }

        [Test]
        public void Shader_MuroMundoExisteYLoUsanLosMateriales()
        {
            Assert.IsNotNull(Shader.Find("MechaCyberpunk/MuroMundo"));
            int usan = renderers.Count(r => r.sharedMaterials.Any(m => m != null && m.shader.name == "MechaCyberpunk/MuroMundo"));
            Assert.Greater(usan, 500, "Los cubos de edificios, banquetas y mobiliario deben usar el material con textura");
        }

        [Test]
        public void PuestosDeCubos_YaNoTienenLonaVerdeNiFaldonPeroConservanLaMesaConCollider()
        {
            Transform[] puestos = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .Where(t => t.name.StartsWith("Puesto_") && t.parent != null && t.parent.name == "Puestos" && t.parent.parent != null && t.parent.parent.name == "Tianguis").ToArray();
            Assert.GreaterOrEqual(puestos.Length, 10);
            foreach (Transform p in puestos)
            {
                foreach (string nombre in new[] { "Lona", "Faldon", "Poste", "Caja1", "Caja2", "Caja3" })
                {
                    Transform c = p.Find(nombre);
                    Assert.IsTrue(c == null || !c.GetComponent<Renderer>().enabled, p.name + "/" + nombre + " sigue visible");
                }
                Transform mesa = p.Find("Mesa");
                Assert.IsNotNull(mesa.GetComponent<Collider>(), p.name + " perdió el collider de la mesa");
                Assert.IsNotNull(mesa.GetComponent<Renderer>().sharedMaterial.GetTexture("_BaseMap"), p.name + " mesa sin textura de madera");
            }
        }

        [Test]
        public void PuestosDeCubos_TienenHuacalesYTamboEnLaRaizT506()
        {
            GameObject raiz = escena.GetRootGameObjects().FirstOrDefault(g => g.name == "T506_SinGreybox");
            Assert.IsNotNull(raiz, "Falta la raíz T506_SinGreybox (Mecha Cyberpunk/T-506/Quitar greybox)");
            Assert.AreEqual(1, escena.GetRootGameObjects().Count(g => g.name == "T506_SinGreybox"));
            Assert.AreEqual(0, raiz.GetComponentsInChildren<Collider>(true).Length, "Los props son solo visuales");
            Assert.GreaterOrEqual(raiz.GetComponentsInChildren<Renderer>().Length, 30);
        }

        [Test]
        public void Mantas_LlevanTelaConTextura()
        {
            Renderer[] mantas = renderers.Where(r => r.name == "Manta").ToArray();
            Assert.GreaterOrEqual(mantas.Length, 4);
            foreach (Renderer m in mantas)
            {
                Assert.IsNotNull(m.sharedMaterial.GetTexture("_BaseMap"), "Manta sin textura");
            }
        }

        [Test]
        public void Colliders_NoSeQuitaronAlRetexturizar()
        {
            // Los cubos del barrio con collider (edificios, mesas, vallas) siguen con él.
            int conCollider = renderers.Count(r => r.sharedMaterial != null && r.sharedMaterial.shader.name == "MechaCyberpunk/MuroMundo" && r.GetComponent<Collider>() != null);
            Assert.Greater(conCollider, 100);
        }
    }
}

using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-513 (paso 5): fondo lejano de la ciudad. Cuatro tramos de cilindro con las pinturas de imageassets/fondos (Nova al norte, Neón Alto
    /// al este, Suburbios al sur y al oeste), sin luces, sombras ni niebla, dentro del plano lejano de las cámaras, y las torres lejanas de
    /// T-501 apagadas mientras las de Neón Alto (T-505) se quedan.
    /// </summary>
    public class FondoCiudadT513Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float PlanoLejanoMinimo = 600f;

        private Scene escena;
        private Transform raiz;

        [OneTimeSetUp]
        public void Abrir()
        {
            escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Additive);
            raiz = escena.GetRootGameObjects().First(g => g.name == "T513_Fondo").transform;
        }

        [OneTimeTearDown]
        public void Cerrar()
        {
            EditorSceneManager.CloseScene(escena, true);
        }

        [Test]
        public void Fondo_CuatroTramosConSuPintura_SinSombrasNiLuces()
        {
            MeshRenderer[] tramos = raiz.GetComponentsInChildren<MeshRenderer>();
            Assert.AreEqual(4, tramos.Length);
            foreach (MeshRenderer r in tramos)
            {
                Assert.AreEqual(ShadowCastingMode.Off, r.shadowCastingMode, r.name + " no proyecta sombra");
                Assert.IsFalse(r.receiveShadows, r.name);
                Assert.AreEqual("MechaCyberpunk/FondoCiudad", r.sharedMaterial.shader.name, r.name);
                Assert.IsNotNull(r.sharedMaterial.GetTexture("_MainTex"), r.name + " tiene su pintura");
            }
            Assert.AreEqual(0, raiz.GetComponentsInChildren<Collider>().Length, "No estorba la física");
        }

        [Test]
        public void Fondo_CadaPinturaVaHaciaSuLado()
        {
            foreach (MeshRenderer r in raiz.GetComponentsInChildren<MeshRenderer>())
            {
                Vector3 c = r.bounds.center;
                string pintura = r.sharedMaterial.name;
                if (pintura == "fondo-nova") { Assert.Greater(c.z, Mathf.Abs(c.x), "Nova al norte"); }
                else if (pintura == "fondo-neon-alto") { Assert.Greater(c.x, Mathf.Abs(c.z), "Neón Alto al este"); }
                else
                {
                    Assert.AreEqual("fondo-suburbios", pintura);
                    Assert.IsTrue(c.z < -Mathf.Abs(c.x) || c.x < -Mathf.Abs(c.z), "Suburbios al sur o al oeste");
                }
            }
        }

        [Test]
        public void Fondo_DentroDelPlanoLejano_YRodeaLaCiudad()
        {
            foreach (MeshFilter f in raiz.GetComponentsInChildren<MeshFilter>())
            {
                foreach (Vector3 v in f.sharedMesh.vertices)
                {
                    float d = new Vector2(v.x, v.z).magnitude;
                    Assert.Less(d, PlanoLejanoMinimo - 20f, "Más cerca que el plano lejano de la cámara (600 m)");
                    Assert.Greater(d, 300f, "Lejos de la ciudad");
                }
            }
        }

        [Test]
        public void Shader_FondoCiudad_SinNieblaYTransparenteArriba()
        {
            Shader s = Shader.Find("MechaCyberpunk/FondoCiudad");
            Assert.IsNotNull(s);
            Assert.IsTrue(s.isSupported);
            string codigo = System.IO.File.ReadAllText(AssetDatabase.GetAssetPath(s));
            StringAssert.DoesNotContain("multi_compile_fog", codigo, "No recibe la niebla del juego");
            StringAssert.Contains("smoothstep(_InicioCielo, _FinCielo", codigo, "Arriba se funde con el cielo");
        }

        [Test]
        public void Torres_LasLejanasApagadas_YLasDeNeonAltoSeQuedan()
        {
            Transform horizonte = escena.GetRootGameObjects().First(g => g.name == "T501_Suburbios").transform.Find("Horizonte");
            Assert.IsFalse(horizonte.gameObject.activeSelf);
            // T-541: las torres de bloques de Neón Alto se cambiaron por torres del kit T20-T23 (T541_Torres).
            Transform neon = escena.GetRootGameObjects().First(g => g.name == "T505_NeonAlto").transform.Find("Torres");
            Assert.IsFalse(neon.gameObject.activeInHierarchy);
            Assert.GreaterOrEqual(escena.GetRootGameObjects().First(g => g.name == "T541_Torres").transform.childCount, 10);
        }
    }
}

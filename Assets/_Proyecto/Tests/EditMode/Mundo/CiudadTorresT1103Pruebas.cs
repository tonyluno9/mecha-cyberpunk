using System.Linq;
using MechaCyberpunk.Mundo.Ambiente;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1103: las torres de Neón Alto y los edificios de Nova usan TorreNitida (niebla atenuada); Nova tiene letrero, haz y luces de aviso.</summary>
    public class CiudadTorresT1103Pruebas
    {
        private const string NombreShader = "MechaCyberpunk/TorreNitida";
        private const int AvisosMinimos = 4;
        private Scene escena;

        [OneTimeSetUp]
        public void Abrir()
        {
            escena = EditorSceneManager.OpenScene("Assets/_Proyecto/Escenas/Ciudad_Prologo.unity", OpenSceneMode.Additive);
        }

        [OneTimeTearDown]
        public void Cerrar()
        {
            EditorSceneManager.CloseScene(escena, true);
        }

        private Transform Raiz(string n) => escena.GetRootGameObjects().First(g => g.name == n).transform;

        private static bool UsaTorreNitida(Renderer r) => r.sharedMaterials.Any(m => m != null && m.shader != null && m.shader.name == NombreShader);

        private static bool EsDeTorre(Material m) => m != null && MechaCyberpunk.Arte.Editor.ColocarT1103Torres.EsMaterialDeTorre(m.name);

        [Test]
        public void Shader_Existe_Compila_Y_TieneNieblaPorMaterial()
        {
            Shader s = Shader.Find(NombreShader);
            Assert.IsNotNull(s);
            Assert.IsTrue(s.isSupported);
            Assert.GreaterOrEqual(s.FindPropertyIndex("_FactorNiebla"), 0);
            Assert.GreaterOrEqual(s.FindPropertyIndex("_NieblaMaxima"), 0);
            Assert.IsNotNull(Shader.Find("MechaCyberpunk/TorreHaz"));
        }

        [Test]
        public void Materiales_DeTorres_NoQuedanConElShaderViejo()
        {
            foreach (string raiz in new[] { "T541_Torres", "S2c_Nova" })
            {
                Renderer[] rs = Raiz(raiz).GetComponentsInChildren<Renderer>(true);
                Renderer[] deTorre = rs.Where(r => r.sharedMaterials.Any(EsDeTorre)).ToArray();
                Assert.Greater(deTorre.Length, 50, raiz);
                foreach (Renderer r in deTorre)
                {
                    foreach (Material m in r.sharedMaterials.Where(EsDeTorre))
                    {
                        Assert.AreEqual(NombreShader, m.shader.name, raiz + "/" + r.name + " con " + m.name);
                        Assert.AreEqual(0.3f, m.GetFloat("_FactorNiebla"), 0.001f);
                        Assert.AreEqual(0.45f, m.GetFloat("_NieblaMaxima"), 0.001f);
                    }
                }
            }
        }

        [Test]
        public void NingunRenderer_FueraDeLasRaicesDeTorres_UsaTorreNitida()
        {
            string[] permitidas = { "T541_Torres", "S2c_Nova", "T1103_Luces" };
            foreach (GameObject raiz in escena.GetRootGameObjects().Where(g => !permitidas.Contains(g.name)))
            {
                foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true).Where(UsaTorreNitida))
                {
                    Assert.Fail(raiz.name + "/" + r.name + " usa TorreNitida y no es de una torre");
                }
            }
        }

        [Test]
        public void Nova_TieneLetrero_Haz_Y_LucesDeAviso()
        {
            Transform luces = Raiz("T1103_Luces");
            Assert.IsNotNull(luces.Find("Letrero_NOVA/Texto"), "Letrero NOVA");
            Transform haz = luces.Find("Haz_Nova");
            Assert.IsNotNull(haz, "Haz de luz");
            Assert.GreaterOrEqual(haz.localScale.y * 2f, 80f, "El haz mide entre 80 y 120 m");
            Assert.LessOrEqual(haz.localScale.y * 2f, 120f);
            Assert.IsNull(haz.GetComponent<Collider>(), "Sin colisión");
            Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off, haz.GetComponent<Renderer>().shadowCastingMode);
            Transform[] avisos = luces.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("LuzAviso_")).ToArray();
            Assert.GreaterOrEqual(avisos.Length, AvisosMinimos);
            Assert.IsEmpty(luces.GetComponentsInChildren<Light>(true), "Sin luces reales nuevas");
            Assert.Greater(luces.GetComponentsInChildren<ParpadeoNeon>(true).Length, 0, "Las luces de aviso parpadean");
            foreach (Transform a in avisos)
            {
                Material m = a.GetComponent<Renderer>().sharedMaterial;
                Assert.Greater(m.GetColor("_EmissionColor").r, 1f, a.name);
                Assert.AreEqual(MaterialGlobalIlluminationFlags.RealtimeEmissive, m.globalIlluminationFlags & MaterialGlobalIlluminationFlags.RealtimeEmissive);
            }
        }
    }
}

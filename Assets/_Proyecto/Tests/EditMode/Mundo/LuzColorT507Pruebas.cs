using System.Linq;
using MechaCyberpunk.Mundo.Datos;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-507: luz y color de noche (ambiente bajo, farolas naranja con sombra cerca de la ruta, fachadas teñidas, un Volume de color por zona).</summary>
    public class LuzColorT507Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RutaCiclo = "Assets/_Proyecto/Mundo/Datos/ConfigCicloDia_Mundo.asset";
        private const float HoraNoche = 23f;
        private const float AmbienteMaximo = 0.6f;
        private const float BlendMinimo = 8f;
        private const int SombrasMinimas = 8;
        private const int SombrasMaximas = 14;
        private const float RadioRuta = 26f;
        // Ruta de la demo (x, z); igual que ColocarT507.Ruta (Arte.Editor no se referencia desde aquí).
        private static readonly Vector2[] Ruta =
        {
            new Vector2(-59f, -77f), new Vector2(-44f, -77f), new Vector2(-73f, -59f), new Vector2(-80f, 0f), new Vector2(-33f, 0f),
            new Vector2(0f, 0f), new Vector2(38f, 0.5f), new Vector2(-22f, 20f), new Vector2(-33f, 29f), new Vector2(-36f, 80f),
            new Vector2(47f, 12f), new Vector2(47f, 47f), new Vector2(74.5f, 47f),
        };

        private Scene escena;

        [OneTimeSetUp]
        public void Abrir()
        {
            escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Additive);
        }

        [OneTimeTearDown]
        public void Cerrar()
        {
            EditorSceneManager.CloseScene(escena, true);
        }

        private Transform Buscar(string nombre)
        {
            return escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).First(t => t.name == nombre);
        }

        [Test]
        public void Ambiente_DeNocheEsBajoYLaExposicionContenida()
        {
            ConfigCicloDia config = AssetDatabase.LoadAssetAtPath<ConfigCicloDia>(RutaCiclo);
            Color a = config.ColorAmbiente(HoraNoche);
            Assert.Less(a.maxColorComponent, AmbienteMaximo, "el ambiente de noche no lava las fachadas");
            VolumeProfile noche = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Proyecto/Mundo/Datos/PerfilNoche.asset");
            Assert.IsTrue(noche.TryGet(out ColorAdjustments ca));
            Assert.LessOrEqual(ca.postExposure.value, 3f, "exposición de noche contenida (antes 2.2 con luces más fuertes)");
        }

        [TestCase("VolumenSuburbios")]
        [TestCase("VolumenCallejon")]
        [TestCase("VolumenTianguis")]
        [TestCase("VolumenNeonAlto")]
        public void CadaZona_TieneSuVolumenLocalConGradingYTransicionSuave(string nombre)
        {
            Volume v = Buscar(nombre).GetComponent<Volume>();
            Assert.IsFalse(v.isGlobal);
            Assert.GreaterOrEqual(v.blendDistance, BlendMinimo, "transición suave entre zonas");
            Assert.IsTrue(v.sharedProfile.TryGet(out ColorAdjustments ca));
            Assert.IsTrue(ca.colorFilter.overrideState);
            Assert.IsTrue(v.sharedProfile.TryGet(out WhiteBalance wb));
            Assert.IsTrue(wb.temperature.overrideState && wb.tint.overrideState);
            Assert.IsTrue(v.GetComponents<MonoBehaviour>().Any(m => m != null && m.GetType().Name == "VolumenSigueNoche"), "solo de noche");
        }

        [Test]
        public void Color_SuburbiosCalido_CallejonYTianguisMagenta_NeonAltoVerde()
        {
            Color Filtro(string n)
            {
                Buscar(n).GetComponent<Volume>().sharedProfile.TryGet(out ColorAdjustments ca);
                return ca.colorFilter.value;
            }
            Color s = Filtro("VolumenSuburbios");
            Assert.Greater(s.r, s.b, "Suburbios cálido");
            foreach (string n in new[] { "VolumenCallejon", "VolumenTianguis" })
            {
                Color c = Filtro(n);
                Assert.IsTrue(c.r > c.g && c.b > c.g, n + " magenta");
            }
            Color v = Filtro("VolumenNeonAlto");
            Assert.IsTrue(v.g > v.r && v.g > v.b, "Neón Alto verde");
        }

        [Test]
        public void Farolas_SonFocosNaranjaHaciaAbajoConSombraSoloCercaDeLaRuta()
        {
            Light[] luces = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light>(true))
                .Where(l => l.transform.parent != null && l.transform.parent.name.StartsWith("Farola_")).ToArray();
            Assert.GreaterOrEqual(luces.Length, 40);
            Assert.IsTrue(luces.All(l => l.type == LightType.Spot), "todas son focos");
            Assert.IsTrue(luces.All(l => l.transform.forward.y < -0.95f), "apuntan hacia abajo");
            // T-509: las de Neón Alto (x 34..60, z 3..93) son verde frío; las demás, naranja.
            bool EnNeonAlto(Light l) => l.transform.position.x >= 34f && l.transform.position.x <= 60f && l.transform.position.z >= 3f && l.transform.position.z <= 93f;
            Assert.IsTrue(luces.Where(l => !EnNeonAlto(l)).All(l => l.color.r > l.color.g && l.color.g > l.color.b), "naranja");
            Assert.IsTrue(luces.Where(EnNeonAlto).All(l => l.color.g > l.color.r), "verde en Neón Alto");
            Light[] conSombra = luces.Where(l => l.shadows != LightShadows.None).ToArray();
            Assert.GreaterOrEqual(conSombra.Length, SombrasMinimas);
            Assert.LessOrEqual(conSombra.Length, SombrasMaximas, "pocas sombras por rendimiento");
            foreach (Light l in conSombra)
            {
                Vector2 p = new Vector2(l.transform.position.x, l.transform.position.z);
                Assert.LessOrEqual(Ruta.Min(r => Vector2.Distance(r, p)), RadioRuta, l.transform.parent.name + " tiene sombra pero está lejos de la ruta");
            }
        }

        [Test]
        public void Fachadas_VariasPinturasEntreEdificios()
        {
            var colores = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true))
                .Select(r => r.sharedMaterial).Where(m => m != null && m.name.StartsWith("T507_C01_")).Select(m => ColorUtility.ToHtmlStringRGB(m.GetColor("_BaseColor"))).Distinct().ToList();
            Assert.GreaterOrEqual(colores.Count, 3, "al menos 3 tonos de fachada entre edificios");
        }

        [Test]
        public void Charcos_YaNoSonCuadrosNegrosOpacos()
        {
            MeshRenderer[] viejos = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true))
                .Where(r => r.sharedMaterial != null && r.sharedMaterial.name == "Barrio_Charco").ToArray();
            Assert.AreEqual(0, viejos.Length);
        }
    }
}

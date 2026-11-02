using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-510: los personajes nuevos de Tripo (visuales, alturas, texturas), sus papeles en Ciudad_Prologo y la gente de banqueta con vida.</summary>
    public class PersonajesT510Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float Tolerancia = 0.12f;
        private static readonly string[] Nuevos = { "SkullHombre", "Doctor", "Inventor", "Hacker", "RobotHombre", "GuardiaNova" };

        private Scene escena;

        [OneTimeSetUp]
        public void AbrirEscena()
        {
            escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Additive);
        }

        [OneTimeTearDown]
        public void CerrarEscena()
        {
            if (escena.IsValid())
            {
                EditorSceneManager.CloseScene(escena, true);
            }
        }

        private static string Controlador(Component c)
        {
            Animator a = c.GetComponentInChildren<Animator>(true);
            return a != null && a.runtimeAnimatorController != null ? AssetDatabase.GetAssetPath(a.runtimeAnimatorController) : string.Empty;
        }

        private IEnumerable<GenteDeBanqueta> Gente() =>
            Object.FindObjectsByType<GenteDeBanqueta>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(g => g.gameObject.scene == escena);

        // ---------- Los visuales ----------

        [TestCaseSource(nameof(Nuevos))]
        public void Visual_DeCadaPersonajeNuevo_EsHumanoideConControladorYCaminaConLosClipsDeWoman2(string nombre)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(nombre));
            Assert.IsNotNull(prefab, "Visual_" + nombre);
            Assert.IsNotNull(prefab.GetComponent<VisualPersonaje>());
            Animator animador = prefab.GetComponentInChildren<Animator>(true);
            Assert.IsTrue(animador.avatar.isValid && animador.avatar.isHuman, "Avatar Humanoid válido");
            AnimatorController c = (AnimatorController)animador.runtimeAnimatorController;
            foreach (string estado in new[] { "Idle", "Caminar", "Correr" })
            {
                ChildAnimatorState e = c.layers[0].stateMachine.states.First(s => s.state.name == estado);
                Assert.IsNotNull(e.state.motion, nombre + " " + estado + " tiene clip");
            }
            Assert.Greater(new SerializedObject(prefab.GetComponent<VisualPersonaje>()).FindProperty("velocidadClipCaminar").floatValue, 0.3f, "Velocidad del clip Caminar medida");
        }

        [TestCaseSource(nameof(Nuevos))]
        public void Visual_DeCadaPersonajeNuevo_MideSuAlturaPedida(string nombre)
        {
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(nombre)));
            try
            {
                Bounds? total = null;
                foreach (Renderer r in instancia.GetComponentsInChildren<Renderer>())
                {
                    if (total.HasValue) { Bounds b = total.Value; b.Encapsulate(r.bounds); total = b; } else { total = r.bounds; }
                }
                float esperada = CreadorVisualTripo.AlturaDe(nombre);
                Assert.AreEqual(esperada, total.Value.size.y, Tolerancia, nombre + ": altura (m)");
            }
            finally
            {
                Object.DestroyImmediate(instancia);
            }
        }

        [Test]
        public void Alturas_DeLosEspeciales_SonLasDelPlan()
        {
            Assert.AreEqual(1.95f, CreadorVisualTripo.AlturaDe("SkullHombre"), 0.001f, "Jefe SKULLS");
            Assert.AreEqual(1.85f, CreadorVisualTripo.AlturaDe("RobotHombre"), 0.001f);
            Assert.AreEqual(1.85f, CreadorVisualTripo.AlturaDe("GuardiaNova"), 0.001f);
            Assert.AreEqual(1.8f, CreadorVisualTripo.AlturaDe("Doctor"), 0.001f);
        }

        [Test]
        public void Mecanico_SeImportaPeroNoSeUsa()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Personajes/Mecanico/Mecanico.fbx"), "Mecanico importado");
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab("Mecanico")), "Sin visual");
        }

        // ---------- man deja de salir negro ----------

        [Test]
        public void Man_UsaLaTexturaDeColor_NoElMapaMetalico()
        {
            Texture2D textura = CreadorVisualTripo.TexturaDeColor("man");
            Assert.IsNotNull(textura);
            StringAssert.DoesNotContain("metallic", textura.name.ToLowerInvariant());
            StringAssert.DoesNotContain("roughness", textura.name.ToLowerInvariant());
            Material baseMat = CreadorVisualTripo.AsegurarMaterialBase("man");
            Assert.AreEqual(textura, baseMat.GetTexture("_BaseMap"), "El material base de man lleva el albedo");
            Assert.AreEqual(Color.white, baseMat.GetColor("_BaseColor"));
            // El albedo no es casi negro (el metálico sí): se promedia una muestra.
            Assert.Greater(PromedioDeBrillo(textura), 0.12f, "La textura de man tiene color (no es un mapa de metal)");
        }

        private static float PromedioDeBrillo(Texture2D t)
        {
            Texture2D copia = new Texture2D(2, 2);
            copia.LoadImage(System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(t)));
            Color32[] pixeles = copia.GetPixels32();
            float suma = 0f;
            int paso = Mathf.Max(1, pixeles.Length / 4000);
            int n = 0;
            for (int i = 0; i < pixeles.Length; i += paso, n++)
            {
                suma += (pixeles[i].r + pixeles[i].g + pixeles[i].b) / (3f * 255f);
            }
            Object.DestroyImmediate(copia);
            return suma / n;
        }

        [Test]
        public void Man_TieneVariantesDeColorDistintas()
        {
            foreach ((string nombre, float _) in ConectarVisualesT510.Tonos)
            {
                Assert.IsNotNull(ConectarVisualesT510.TexturaMan(nombre), "man_" + nombre + ".png");
            }
            Assert.AreNotEqual(AssetDatabase.GetAssetPath(ConectarVisualesT510.TexturaMan("verde")), AssetDatabase.GetAssetPath(ConectarVisualesT510.TexturaMan("azul")));
            Material viboras = AssetDatabase.LoadAssetAtPath<Material>("Assets/Arte/Personajes/Materiales/man_Viboras.mat");
            Material nova = AssetDatabase.LoadAssetAtPath<Material>("Assets/Arte/Personajes/Materiales/man_Nova.mat");
            Assert.IsNotNull(viboras);
            Assert.IsNotNull(nova);
            Assert.AreNotEqual(viboras.GetTexture("_BaseMap"), nova.GetTexture("_BaseMap"), "SNAKES y NOVA tienen otro color");
            Assert.AreNotEqual(viboras.GetTexture("_BaseMap"), CreadorVisualTripo.TexturaDeColor("man"), "SNAKES no es el color de SKULLS");
        }

        [Test]
        public void Pandilleros_DeCadaPandilla_UsanManConSuColor()
        {
            int skulls = 0, viboras = 0, nova = 0;
            foreach (VarianteVisual v in Object.FindObjectsByType<VarianteVisual>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x.gameObject.scene == escena))
            {
                Transform pandilla = v.transform.parent != null ? v.transform.parent.parent : null;
                if (pandilla == null) { continue; }
                string nombre = pandilla.name;
                SerializedObject so = new SerializedObject(v);
                Material m = (Material)so.FindProperty("materiales").GetArrayElementAtIndex(0).objectReferenceValue;
                if (nombre.StartsWith("Pandilla_Calaveras") && m.name == "man_Skulls") { skulls++; }
                if (nombre.StartsWith("Pandilla_Viboras") && m.name == "man_Viboras") { viboras++; }
                if (nombre.StartsWith("Pandilla_Nova") && m.name == "man_Nova") { nova++; }
            }
            Assert.Greater(skulls, 0, "Pandilleros SKULLS con man_Skulls");
            Assert.Greater(viboras, 0, "Pandilleros SNAKES con man_Viboras");
            Assert.Greater(nova, 0, "Pandilleros NOVA con man_Nova");
        }

        // ---------- Papeles en la escena ----------

        [Test]
        public void JefeDeLosSkulls_EsSkullHombreDe195()
        {
            Transform jefe = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.gameObject.scene == escena && t.name == "Jefe" && t.parent != null && t.parent.name == "PuntoJefe_tianguis");
            Assert.IsNotNull(jefe, "El jefe del Tianguis conserva su nombre");
            StringAssert.Contains("/SkullHombre/", Controlador(jefe));
            Assert.AreEqual(Vector3.one, jefe.localScale, "Ya no es el man escalado");
            Assert.IsNotNull(jefe.GetComponent("PandilleroAPie"), "Sigue siendo el pandillero del juego");
        }

        [TestCase("Apoyo_JOEL", "/Doctor/")]
        [TestCase("Apoyo_TINO", "/Inventor/")]
        [TestCase("Apoyo_SERGIO", "/Hacker/")]
        public void Apoyos_DocInventorYHacker_UsanSuPropioModelo(string objeto, string modelo)
        {
            Transform t = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(x => x.gameObject.scene == escena && x.name == objeto);
            StringAssert.Contains(modelo, Controlador(t));
        }

        [Test]
        public void Peaton_LlevaElRobotYSeLoPoneUnoDeCadaCuatro()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Trafico/Prefabs/Peaton.prefab");
            PeatonVariado v = prefab.GetComponent<PeatonVariado>();
            Assert.IsNotNull(v);
            Assert.AreEqual(0.25f, v.ProbabilidadRobot, 0.001f);
            StringAssert.Contains("/RobotHombre/", Controlador(v.Robot.transform));
            StringAssert.Contains("/woman2/", Controlador(v.Persona.transform));
            Assert.IsFalse(v.Robot.activeSelf, "El robot sale apagado hasta que se elige");
            Assert.IsNotNull(v.Persona.GetComponent<VarianteVisual>(), "La persona conserva sus colores");
        }

        [Test]
        public void GenteDeBanqueta_UnoDeCadaCuatroEsRobot()
        {
            List<Transform> gente = Gente().Select(g => g.transform).ToList();
            Assert.GreaterOrEqual(gente.Count, 24, "T-508: de 24 a 48 personas");
            int robots = gente.Count(t => Controlador(t).Contains("/RobotHombre/"));
            Assert.GreaterOrEqual(robots / (float)gente.Count, 0.12f);
            Assert.LessOrEqual(robots / (float)gente.Count, 0.4f);
            foreach (Transform t in gente)
            {
                Assert.IsNull(t.GetComponentInChildren<Collider>(true), "Sin colliders: no tapa caminos (" + t.name + ")");
                Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off, t.GetComponentInChildren<SkinnedMeshRenderer>(true).shadowCastingMode, "Sin sombra");
                Assert.AreEqual(AnimatorCullingMode.CullCompletely, t.GetComponentInChildren<Animator>(true).cullingMode);
            }
        }

        [Test]
        public void GenteDeBanqueta_UnosCaminanYOtrosSeQuedanParados_NadieSigueCongelado()
        {
            List<GenteDeBanqueta> gente = Gente().ToList();
            int caminan = gente.Count(g => g.Modo == ModoGente.Caminar);
            int parados = gente.Count(g => g.Modo != ModoGente.Caminar);
            Assert.GreaterOrEqual(caminan, 4, "Unos caminan por la banqueta");
            Assert.GreaterOrEqual(parados, 10, "Otros de pie");
            Assert.Greater(gente.Count(g => g.Modo == ModoGente.Platicar), 0, "Hay parejas que platican");
            Assert.Greater(gente.Count(g => g.Modo == ModoGente.Quieto), 0, "Hay quien respira de pie");
            Assert.Greater(gente.Select(g => Mathf.Round(g.Fase * 100f)).Distinct().Count(), gente.Count / 3, "Fases desfasadas: no se mueven igual");
            Assert.GreaterOrEqual(gente.Where(g => g.Modo == ModoGente.Caminar).Select(g => Mathf.Round(g.Velocidad * 100f)).Distinct().Count(), 3, "Velocidades distintas");
            foreach (GenteDeBanqueta g in gente.Where(g => g.Modo == ModoGente.Platicar))
            {
                Assert.IsNotNull(g.ClipPlatica, "Platicar lleva su clip");
            }
        }

        [Test]
        public void GenteQueCamina_TieneElRecorridoLibreYSinSalirseDeLaBanqueta()
        {
            foreach (GenteDeBanqueta g in Gente().Where(g => g.Modo == ModoGente.Caminar))
            {
                Vector3 a = g.PuntoA;
                Vector3 b = g.PuntoB;
                Assert.Greater(Vector3.Distance(a, b), 3.5f, "Recorrido de más de 3.5 m: " + g.transform.parent.name);
                for (float f = 0f; f <= 1.001f; f += 0.1f)
                {
                    Vector3 p = Vector3.Lerp(a, b, f);
                    Assert.IsTrue(Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out RaycastHit golpe, 4f, ~0, QueryTriggerInteraction.Ignore), "Hay piso en " + p);
                    Assert.Less(Mathf.Abs(golpe.point.y - a.y), 0.2f, "Mismo nivel (no baja a la calle) en " + p);
                    Assert.IsFalse(Physics.CheckCapsule(p + Vector3.up * 0.4f, p + Vector3.up * 1.5f, 0.3f, ~0, QueryTriggerInteraction.Ignore), "Nada sólido en el camino en " + p);
                }
            }
        }

        private static float DistanciaASegmento(Vector3 p, Vector3 a, Vector3 b)
        {
            p.y = 0f;
            Vector3 ab = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector3.Distance(p, a + ab * t);
        }

        [Test]
        public void GuardiasDeNeonAlto_SonDeDosACuatro_ParadosYLejosDeLaRutaDelHacker()
        {
            List<Transform> guardias = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => t.gameObject.scene == escena && t.name.StartsWith("Guardia_") && t.parent != null && t.parent.name == "T510_Guardias").ToList();
            Assert.GreaterOrEqual(guardias.Count, 2);
            Assert.LessOrEqual(guardias.Count, 4);
            Vector3 casino = new Vector3(57.5f, 0f, 47f);
            Vector3 hacker = new Vector3(74.5f, 0f, 47f);
            foreach (Transform g in guardias)
            {
                StringAssert.Contains("/GuardiaNova/", Controlador(g));
                Assert.IsNull(g.GetComponentInChildren<Collider>(true), "No tapan el paso");
                Assert.AreEqual(ModoGente.Quieto, g.GetComponent<GenteDeBanqueta>().Modo, "Parados, sin pelear");
                Assert.IsNull(g.GetComponent("PandilleroAPie"), "Ambiente: no pelean");
                Assert.Greater(Vector3.Distance(new Vector3(g.position.x, 0f, g.position.z), casino), 7f, "Lejos de la puerta del Casino");
                Assert.Greater(Vector3.Distance(new Vector3(g.position.x, 0f, g.position.z), hacker), 7f, "Lejos de EL HACKER");
                Assert.Less(g.position.z, 90f);
                Assert.Greater(g.position.z, 8f);
                Assert.Less(Mathf.Abs(g.position.x - 47f), 12f, "En la calle de Neón Alto");
                // La ruta calle (47, 47) -> puerta del Casino -> EL HACKER queda libre: ningún guardia a menos de 7 m de ella.
                Assert.Greater(DistanciaASegmento(g.position, new Vector3(47f, 0f, 47f), hacker), 7f, "No están sobre la ruta hacia el Casino y EL HACKER");
            }
        }
    }
}

using System.IO;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1497: los agentes de Nova a escala real (E15 2.6 m, E16 3.5 m, E18 2.2 m, perro E17 1.2 m), su cápsula y el arma W10_ArmaNova en la mano.
    /// Leen los prefabs y datos ya generados con EscalaNovaT1497 (ImportarArma, Visuales y Enemigos, en ese orden).
    /// </summary>
    public class EscalaNovaT1497Pruebas
    {
        private const float Tolerancia = 0.06f;
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RutaConfig = "Assets/_Proyecto/IA/Resources/ConfigRespuestaNova.asset";

        private static readonly string[] Humanoides = { "E15_CentinelaNova_a", "E15_CentinelaNova_b", "E16_RobotPesadoNova_a", "E18_EliteNova_a" };
        // T-1553 (decisión de Tony 20-nov): el E18 (soldado azul) ya no lleva arma; vuela con el jetpack y pelea a puñetazos.
        private static readonly string[] ConArma = { "E15_CentinelaNova_a", "E15_CentinelaNova_b", "E16_RobotPesadoNova_a" };

        private static float AlturaRenderers(GameObject prefab)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                Bounds? total = null;
                foreach (Renderer r in inst.GetComponentsInChildren<Renderer>())
                {
                    if (r.name.ToLowerInvariant().Contains("w10")) { continue; }
                    if (total.HasValue) { Bounds b = total.Value; b.Encapsulate(r.bounds); total = b; }
                    else { total = r.bounds; }
                }
                return total.HasValue ? total.Value.size.y : 0f;
            }
            finally
            {
                Object.DestroyImmediate(inst);
            }
        }

        [Test]
        public void Tabla_AlturasDeNova_SonLasPedidas()
        {
            Assert.AreEqual(2.6f, EscalasNova.AlturaCentinelaE15, 0.0001f);
            Assert.AreEqual(3.5f, EscalasNova.AlturaRobotPesadoE16, 0.0001f);
            Assert.AreEqual(2.2f, EscalasNova.AlturaEliteE18, 0.0001f);
            Assert.AreEqual(1.2f, EscalasNova.AlturaPerroE17, 0.0001f);
            Assert.AreEqual(0f, EscalasNova.Altura("NP07_Pescador_a"), "los demás personajes no se reescalan");
        }

        [Test]
        public void Tabla_Capsula_CrecenConLaAltura()
        {
            Assert.AreEqual(1.8f, EscalasNova.AlturaCapsula(1.85f), 0.06f, "a 1.85 m queda la cápsula original");
            Assert.AreEqual(EscalasNova.RadioMaximo, EscalasNova.RadioCapsula(3.5f), 0.0001f, "el gigante no pasa del radio máximo");
            Assert.Greater(EscalasNova.RadioCapsula(2.6f), EscalasNova.RadioCapsula(2.2f));
            Assert.AreEqual(2.6f / 1.85f, EscalasNova.Factor(2.6f), 0.0001f);
        }

        [TestCase("E15_CentinelaNova_a", 2.6f)]
        [TestCase("E15_CentinelaNova_b", 2.6f)]
        [TestCase("E16_RobotPesadoNova_a", 3.5f)]
        [TestCase("E16_RobotPesadoNova_b", 3.5f)]
        [TestCase("E18_EliteNova_a", 2.2f)]
        public void CreadorVisual_AlturaDe_ViveDeLaTabla(string variante, float esperada)
        {
            Assert.AreEqual(esperada, CreadorVisualTripo.AlturaDe(variante), 0.0001f, variante);
        }

        [TestCaseSource(nameof(Humanoides))]
        public void Visual_DeCadaAgente_MideSuAlturaReal(string variante)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonajesNuevosT1389b.RutaVisual(variante));
            Assert.IsNotNull(prefab, "Visual_" + variante);
            float objetivo = EscalasNova.Altura(variante);
            Assert.AreEqual(objetivo, AlturaRenderers(prefab), objetivo * Tolerancia, variante + " (bounds en reposo)");
        }

        [Test]
        public void Perro_MideUnMetroVeinte_ConLaEscalaEnElMeta()
        {
            string ruta = CreadorVisualTripo.RutaModelo(PersonajesNuevosT1389b.PerroVariante);
            ModelImporter imp = (ModelImporter)AssetImporter.GetAtPath(ruta);
            Assert.IsTrue(imp.userData.StartsWith("escala="), "la escala va en userData, no solo en globalScale: " + imp.userData);
            Assert.AreEqual(PostprocesadorAssetsT250.EscalaPersonaje(imp.userData), imp.globalScale, 0.0001f, "el importador aplica la escala del userData");
            GameObject visual = AssetDatabase.LoadAssetAtPath<GameObject>(PersonajesNuevosT1389b.RutaVisual(PersonajesNuevosT1389b.PerroVariante));
            Assert.AreEqual(EscalasNova.AlturaPerroE17, AlturaRenderers(visual), EscalasNova.AlturaPerroE17 * 0.10f);
        }

        [TestCaseSource(nameof(Humanoides))]
        public void Enemigo_DeCadaAgente_TieneCapsulaYCuerpoDeSuTamano(string variante)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonajesNuevosT1389b.RutaPrefabEnemigo(variante));
            Assert.IsNotNull(prefab, "prefab de enemigo " + variante);
            float altura = EscalasNova.Altura(variante);
            CharacterController cc = prefab.GetComponent<CharacterController>();
            Assert.AreEqual(EscalasNova.AlturaCapsula(altura), cc.height, 0.01f, "alto de la cápsula");
            Assert.AreEqual(EscalasNova.RadioCapsula(altura), cc.radius, 0.01f, "radio de la cápsula");
            Assert.AreEqual(cc.height * 0.5f, cc.center.y, 0.01f, "la cápsula parte del piso");
            Component pandillero = prefab.GetComponents<Component>().First(c => c != null && c.GetType().Name == "PandilleroAPie");
            Assert.AreEqual(EscalasNova.Factor(altura), new SerializedObject(pandillero).FindProperty("escalaCuerpo").floatValue, 0.001f, "escalaCuerpo");
        }

        [Test]
        public void Enemigo_Perro_TieneCapsulaDePerroGrande()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonajesNuevosT1389b.RutaPrefabEnemigo(PersonajesNuevosT1389b.PerroVariante));
            Assert.IsNotNull(prefab);
            CharacterController cc = prefab.GetComponent<CharacterController>();
            Assert.AreEqual(EscalasNova.AlturaPerroE17, cc.height, 0.01f);
            Assert.GreaterOrEqual(cc.height, cc.radius * 2f, "una cápsula válida");
        }

        [Test]
        public void Arma_W10_TieneModeloMaterialYBoca()
        {
            GameObject arma = AssetDatabase.LoadAssetAtPath<GameObject>(EscalaNovaT1497.RutaArmaPrefab);
            Assert.IsNotNull(arma, "W10_ArmaNova.prefab");
            Assert.IsNotNull(arma.transform.Find("Boca"), "Boca");
            Renderer[] renderers = arma.GetComponentsInChildren<Renderer>(true);
            Assert.IsNotEmpty(renderers);
            Assert.IsTrue(renderers.All(r => r.sharedMaterial != null && r.sharedMaterial.name == "W10_ArmaNova"), "todo con el material W10_ArmaNova");
            Assert.Less(arma.transform.Find("Boca").localPosition.z, 1.2f);
            Assert.Greater(arma.transform.Find("Boca").localPosition.z, 0.3f, "el cañón mira a +Z");
        }

        [Test]
        public void Arma_W10_NingunArchivoNiMaterialSeLlamaW09()
        {
            foreach (string ruta in Directory.GetFiles(EscalaNovaT1497.CarpetaArma, "*", SearchOption.AllDirectories).Where(f => !f.EndsWith(".meta")))
            {
                StringAssert.DoesNotContain("W09", Path.GetFileName(ruta), ruta);
            }
            Material material = AssetDatabase.LoadAssetAtPath<Material>(EscalaNovaT1497.RutaArmaMaterial);
            Assert.IsNotNull(material);
            Assert.IsNotNull(material.GetTexture("_BaseMap"));
            StringAssert.StartsWith("W10_", material.GetTexture("_BaseMap").name);
        }

        [TestCaseSource(nameof(ConArma))]
        public void Enemigo_ConArma_LlevaLaW10EnLaMano(string variante)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonajesNuevosT1389b.RutaPrefabEnemigo(variante));
            AsegurarW10(prefab, variante);
        }

        [Test]
        public void Enemigo_E18_NoLlevaArma_YEscalaNovaNoSeLaVuelveAPoner()
        {
            Assert.IsFalse(EscalaNovaT1497.LlevaArma("E18_EliteNova_a"), "decisión de Tony 20-nov");
            Assert.IsTrue(EscalaNovaT1497.LlevaArma("E15_CentinelaNova_a"));
            Assert.IsTrue(EscalaNovaT1497.LlevaArma("E16_RobotPesadoNova_a"));
            Assert.IsTrue(EscalaNovaT1497.LlevaArma(null), "la guardia base de Nova conserva su arma");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonajesNuevosT1389b.RutaPrefabEnemigo("E18_EliteNova_a"));
            Assert.IsNotNull(prefab);
            Transform mano = prefab.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == CreadorVisualTripo.NombreSocket);
            Assert.IsTrue(mano == null || mano.Find(EscalaNovaT1497.NombreArma) == null, "el E18 no tiene W10 en la mano (corre RobotsNovaT1553.Aplicar)");
        }

        [Test]
        public void Enemigo_GuardiaBaseDeNova_LlevaLaW10EnLaMano()
        {
            AsegurarW10(AssetDatabase.LoadAssetAtPath<GameObject>(EscalaNovaT1497.RutaGuardiaBase), "GuardiaNova_Armado");
        }

        private static void AsegurarW10(GameObject prefab, string nombre)
        {
            Assert.IsNotNull(prefab, nombre);
            Transform mano = prefab.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == CreadorVisualTripo.NombreSocket);
            Assert.IsNotNull(mano, nombre + ": ManoDer");
            Transform arma = mano.Find(EscalaNovaT1497.NombreArma);
            Assert.IsNotNull(arma, nombre + ": W10 en ManoDer");
            Assert.AreEqual(EscalaNovaT1497.RutaArmaPrefab, AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(arma.gameObject)), "es una instancia del prefab W10");
            Assert.AreEqual(1, mano.Cast<Transform>().Count(t => t.name == EscalaNovaT1497.NombreArma), "una sola W10");
        }

        [Test]
        public void Enemigo_Perro_NoLlevaArma()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonajesNuevosT1389b.RutaPrefabEnemigo(PersonajesNuevosT1389b.PerroVariante));
            Transform mano = prefab.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == CreadorVisualTripo.NombreSocket);
            Assert.IsTrue(mano == null || mano.Find(EscalaNovaT1497.NombreArma) == null);
        }

        [Test]
        public void Config_NovaTrae_CentinelaEliteRobotYPerro()
        {
            Object config = AssetDatabase.LoadAssetAtPath<Object>(RutaConfig);
            Assert.IsNotNull(config);
            var so = new SerializedObject(config);
            string[] guardias = Rutas(so, "guardiasExtra");
            string[] pesados = Rutas(so, "pesadosExtra");
            CollectionAssert.Contains(guardias, PersonajesNuevosT1389b.RutaPrefabEnemigo("E15_CentinelaNova_a"));
            CollectionAssert.Contains(guardias, PersonajesNuevosT1389b.RutaPrefabEnemigo("E18_EliteNova_a"));
            CollectionAssert.Contains(pesados, PersonajesNuevosT1389b.RutaPrefabEnemigo("E16_RobotPesadoNova_a"));
            CollectionAssert.Contains(Rutas(so, "perros"), PersonajesNuevosT1389b.RutaPrefabEnemigo(PersonajesNuevosT1389b.PerroVariante));
            string rutaB = PersonajesNuevosT1389b.RutaPrefabEnemigo(EscalaNovaT1497.PesadoB);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(rutaB) != null)
            {
                CollectionAssert.Contains(pesados, rutaB, "E16_b tiene prefab: tiene que estar en pesadosExtra");
            }
        }

        [Test]
        public void Escena_LosSkulls_TienenLasVariantesE19YE20()
        {
            string[] guids = PersonajesNuevosT1389b.RutasPandilleroSkulls().Select(AssetDatabase.AssetPathToGUID).ToArray();
            Assert.AreEqual(4, guids.Length);
            foreach (string guid in guids) { Assert.IsNotEmpty(guid); }
            var faltan = guids.ToList();
            foreach (string linea in File.ReadLines(RutaEscena))
            {
                faltan.RemoveAll(g => linea.Contains(g));
                if (faltan.Count == 0) { break; }
            }
            Assert.IsEmpty(faltan, "variantes de SKULLS que no están en Ciudad_Prologo (guids)");
        }

        private static string[] Rutas(SerializedObject so, string propiedad)
        {
            SerializedProperty lista = so.FindProperty(propiedad);
            var rutas = new System.Collections.Generic.List<string>();
            for (int i = 0; i < lista.arraySize; i++)
            {
                Object o = lista.GetArrayElementAtIndex(i).objectReferenceValue;
                if (o != null) { rutas.Add(AssetDatabase.GetAssetPath(o)); }
            }
            return rutas.ToArray();
        }
    }
}

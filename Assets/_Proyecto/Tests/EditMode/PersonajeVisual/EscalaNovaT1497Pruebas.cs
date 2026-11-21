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
    /// T-1497: los agentes de Nova a escala real, su cápsula y el arma W10_ArmaNova en la mano. T-1552 (decisión de Tony, 20-nov): E15 1.94 m, E16 y E14 3.14 m, E18 2.14 m (antes 2.6, 3.5 y 2.2); perro E17 1.2 m.
    /// Leen los prefabs y datos ya generados con EscalaNovaT1497 (ImportarArma, Visuales y Enemigos, en ese orden).
    /// </summary>
    public class EscalaNovaT1497Pruebas
    {
        private const float Tolerancia = 0.06f;
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RutaConfig = "Assets/_Proyecto/IA/Resources/ConfigRespuestaNova.asset";

        private static readonly string[] Humanoides = { "E15_CentinelaNova_a", "E15_CentinelaNova_b", "E16_RobotPesadoNova_a", "E18_EliteNova_a" };
        private static readonly string[] ConArma = { "E15_CentinelaNova_a", "E15_CentinelaNova_b", "E16_RobotPesadoNova_a", "E18_EliteNova_a" };

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
            Assert.AreEqual(1.94f, EscalasNova.AlturaCentinelaE15, 0.0001f);
            Assert.AreEqual(3.14f, EscalasNova.AlturaRobotPesadoE16, 0.0001f);
            Assert.AreEqual(3.14f, EscalasNova.AlturaJefeFinalE14, 0.0001f);
            Assert.AreEqual(3.14f, EscalasNova.Altura("E14_JefeFinalNova"), 0.0001f);
            Assert.AreEqual(2.14f, EscalasNova.AlturaEliteE18, 0.0001f);
            Assert.AreEqual(1.2f, EscalasNova.AlturaPerroE17, 0.0001f);
            Assert.AreEqual(0f, EscalasNova.Altura("NP07_Pescador_a"), "los demás personajes no se reescalan");
        }

        [Test]
        public void Tabla_Capsula_CrecenConLaAltura()
        {
            Assert.AreEqual(1.8f, EscalasNova.AlturaCapsula(1.85f), 0.06f, "a 1.85 m queda la cápsula original");
            Assert.AreEqual(EscalasNova.RadioMaximo, EscalasNova.RadioCapsula(3.14f), 0.0001f, "el gigante no pasa del radio máximo");
            Assert.Greater(EscalasNova.RadioCapsula(2.14f), EscalasNova.RadioCapsula(1.94f));
            Assert.AreEqual(3.14f / 1.85f, EscalasNova.Factor(3.14f), 0.0001f);
        }

        [TestCase("E15_CentinelaNova_a", 1.94f)]
        [TestCase("E15_CentinelaNova_b", 1.94f)]
        [TestCase("E16_RobotPesadoNova_a", 3.14f)]
        [TestCase("E16_RobotPesadoNova_b", 3.14f)]
        [TestCase("E18_EliteNova_a", 2.14f)]
        public void CreadorVisual_AlturaDe_ViveDeLaTabla(string variante, float esperada)
        {
            Assert.AreEqual(esperada, CreadorVisualTripo.AlturaDe(variante), 0.0001f, variante);
        }

        /// <summary>
        /// T-1552: los bounds en reposo (que mienten unos centímetros contra la altura de Blender) ya no son la altura nueva: AlturasT1552.Aplicar multiplica el Modelo por el factor de la tabla,
        /// así que quedan en (bounds de T-1497) x factor. Si el visual se rehízo con CreadorVisualTripo (que ya usa la altura nueva), quedan en la altura nueva: se acepta cualquiera de las dos.
        /// </summary>
        [TestCaseSource(nameof(Humanoides))]
        public void Visual_DeCadaAgente_MideSuAlturaReal(string variante)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonajesNuevosT1389b.RutaVisual(variante));
            Assert.IsNotNull(prefab, "Visual_" + variante);
            float objetivo = EscalasNova.Altura(variante);
            float porFactor = BoundsDeT1497(variante) * EscalasHumanos.Factor("Visual_" + variante);
            float medida = AlturaRenderers(prefab);
            bool ok = Mathf.Abs(medida - objetivo) <= objetivo * ToleranciaT1552 || Mathf.Abs(medida - porFactor) <= porFactor * ToleranciaT1552;
            Assert.IsTrue(ok, variante + ": bounds en reposo " + medida.ToString("F2") + " m; esperado " + objetivo.ToString("F2") + " (altura nueva) o " + porFactor.ToString("F2") + " (bounds viejos x factor)");
        }

        private const float ToleranciaT1552 = 0.10f;

        /// <summary>Los bounds en reposo con los que T-1497 dejó a cada agente (E15 2.6 m, E16 3.5 m, E18 2.2 m): el punto de partida del factor de T-1552.</summary>
        private static float BoundsDeT1497(string variante)
        {
            if (variante.StartsWith("E15")) { return 2.6f; }
            if (variante.StartsWith("E16")) { return 3.5f; }
            return 2.2f;
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

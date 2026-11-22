using System.Collections.Generic;
using System.IO;
using System.Linq;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// T-1621: arma la cápsula de caída CP01 de Nova. Un solo paso, repetible:
    /// <c>-executeMethod MechaCyberpunk.IA.Editor.CapsulasCaidaT1621.Aplicar</c>. Hace, en este orden:
    /// 1) <see cref="Importar"/>: configura la importación de CP01_Capsula.fbx (Legacy, clip CP01_Abrir cuadros 1-90, sin bucle), de los tres clips Humanoid (E15, E16 y E18_Capsula_Salida, mixamorig como sus
    /// otros clips: avatar propio del clip con la descripción del modelo del personaje sin cabeza) y del clip Generic del perro (E17_Capsula_Salida, igual que E17_Caminar); compresión apagada y sin bucle.
    /// 2) <see cref="CrearMaterialesPrefabYConfig"/>: materiales holográficos (shader CapsulaHolograma) en Materiales/, el prefab Prefabs/CP01_Capsula.prefab y el asset Resources/ConfigCapsulaCaida.asset.
    /// El postprocesador <see cref="PostprocesadorCapsulasT1621"/> repite la configuración de los cuatro clips en cada reimportación (corre después de los de PJ01 y de Nova).
    /// </summary>
    public static class CapsulasCaidaT1621
    {
        public const string RutaCapsula = "Assets/Arte/Nova/CP01_Capsula/CP01_Capsula.fbx";
        public const string RutaShader = "Assets/_Proyecto/IA/Shaders/CapsulaHolograma.shader";
        public const string CarpetaMateriales = "Assets/_Proyecto/IA/Materiales/";
        public const string RutaPrefab = "Assets/_Proyecto/IA/Prefabs/CP01_Capsula.prefab";
        public const string RutaConfig = "Assets/_Proyecto/IA/Resources/ConfigCapsulaCaida.asset";

        private const string ModeloE15 = "Assets/Arte/Personajes/E15_CentinelaNova/E15_CentinelaNova_a.fbx";
        private const string ModeloE16 = "Assets/Arte/Personajes/E16_RobotPesadoNova/E16_RobotPesadoNova_a.fbx";
        private const string ModeloE18 = "Assets/Arte/Personajes/E18_EliteNova/E18_EliteNova_a.fbx";
        private const string ModeloE17 = "Assets/Arte/Personajes/E17_PerroRobotNova/E17_PerroRobotNova_esc.fbx";

        private const int UltimoCuadroCapsula = 90;

        /// <summary>Los cuatro clips de salida: ruta del FBX, modelo del personaje, último cuadro (el primero es 1) y si es Humanoid.</summary>
        public static readonly (string Ruta, string Modelo, int Ultimo, bool Humanoide)[] Clips =
        {
            ("Assets/Arte/Personajes/PJ01/Clips/E15_Capsula_Salida.fbx", ModeloE15, 122, true),
            ("Assets/Arte/Personajes/PJ01/Clips/E16_Capsula_Salida.fbx", ModeloE16, 140, true),
            ("Assets/Arte/Personajes/E18_EliteNova/Clips/E18_Capsula_Salida.fbx", ModeloE18, 96, true),
            ("Assets/Arte/Personajes/E17_PerroRobotNova/Clips/E17_Capsula_Salida.fbx", ModeloE17, 98, false),
        };

        private static readonly string[] RutasPrueba =
        {
            "Assets/_Proyecto/IA/Prefabs/GuardiaPesado_E16_RobotPesadoNova_a.prefab",
            "Assets/_Proyecto/IA/Prefabs/GuardiaNova_E18_EliteNova_a.prefab",
            "Assets/_Proyecto/IA/Prefabs/GuardiaNova_E15_CentinelaNova_a.prefab",
            "Assets/_Proyecto/IA/Prefabs/PerroNova_E17_PerroRobotNova_a.prefab",
            "Assets/Arte/Vehiculos/Drones/Prefabs/Dron_DR02_a.prefab",
            "Assets/Arte/Vehiculos/Drones/Prefabs/Dron_E13.prefab",
            "Assets/Arte/Vehiculos/Drones/Prefabs/Dron_DR01.prefab",
            "Assets/Arte/Vehiculos/Drones/Prefabs/Dron_DR04_a.prefab",
        };

        public static bool EsClipDeSalida(string ruta) => Clips.Any(c => c.Ruta == ruta);

        // ------------------------------------------------------------------ Paso completo

        public static void Aplicar()
        {
            Importar();
            CrearMaterialesPrefabYConfig();
            Debug.Log("[IA] T-1621: cápsula CP01 lista (" + RutaPrefab + ", " + RutaConfig + ").");
        }

        // ------------------------------------------------------------------ 1) Importación

        public static void Importar()
        {
            AssetDatabase.Refresh();
            // Cada FBX nuevo se importa una primera vez (Unity crea su .meta), se configura y se reimporta; la segunda pasada ya tiene el esqueleto y el clip por omisión.
            for (int pasada = 0; pasada < 2; pasada++)
            {
                ImportarCapsula();
                foreach (var c in Clips)
                {
                    ImportarClip(c.Ruta);
                }
            }
            AssetDatabase.SaveAssets();
        }

        private static void ImportarCapsula()
        {
            AssetDatabase.ImportAsset(RutaCapsula, ImportAssetOptions.ForceUpdate);
            var importador = AssetImporter.GetAtPath(RutaCapsula) as ModelImporter;
            if (importador == null)
            {
                Debug.LogError("[IA] T-1621: falta " + RutaCapsula);
                return;
            }
            importador.importAnimation = true;
            importador.animationType = ModelImporterAnimationType.Legacy;
            importador.animationCompression = ModelImporterAnimationCompression.Off;
            importador.importCameras = false;
            importador.importLights = false;
            ModelImporterClipAnimation[] clips = importador.defaultClipAnimations;
            if (clips.Length > 0)
            {
                ModelImporterClipAnimation clip = clips[0];
                clip.name = CapsulaCaida.NombreClip;
                clip.firstFrame = 1f;
                clip.lastFrame = UltimoCuadroCapsula;
                clip.loopTime = false;
                clip.loopPose = false;
                clip.wrapMode = WrapMode.Once;
                importador.clipAnimations = new[] { clip };
            }
            importador.SaveAndReimport();
        }

        private static void ImportarClip(string ruta)
        {
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
            var importador = AssetImporter.GetAtPath(ruta) as ModelImporter;
            if (importador == null)
            {
                Debug.LogError("[IA] T-1621: falta " + ruta);
                return;
            }
            ConfigurarClip(importador);
            importador.SaveAndReimport();
        }

        /// <summary>Deja el importador de uno de los cuatro clips de salida como lo piden las reglas de la tarea. Lo llama Importar y el postprocesador.</summary>
        public static void ConfigurarClip(ModelImporter importador)
        {
            (string Ruta, string Modelo, int Ultimo, bool Humanoide) fila = Clips.FirstOrDefault(c => c.Ruta == importador.assetPath);
            if (fila.Ruta == null)
            {
                return;
            }
            string nombre = Path.GetFileNameWithoutExtension(fila.Ruta);
            var modelo = AssetImporter.GetAtPath(fila.Modelo) as ModelImporter;
            importador.importAnimation = true;
            importador.animationCompression = ModelImporterAnimationCompression.Off;
            importador.materialImportMode = ModelImporterMaterialImportMode.None;
            importador.importCameras = false;
            importador.importLights = false;
            importador.importBlendShapes = false;
            importador.useFileScale = modelo != null ? modelo.useFileScale : true;
            if (modelo != null)
            {
                importador.globalScale = modelo.globalScale;
                importador.userData = modelo.userData; // el postprocesador de escala lee userData
            }
            if (fila.Humanoide)
            {
                // Como los E15/E16/E18_Caida_*: Humanoid con avatar propio del clip (Copy From Other Avatar no saca el clip, T-1553) y la descripción del modelo del personaje.
                importador.animationType = ModelImporterAnimationType.Human;
                importador.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importador.humanDescription = DescripcionSinCabeza(modelo, importador.assetPath, importador.humanDescription);
            }
            else
            {
                // Perro: Generic sin avatar, igual que E17_Caminar; el rig y la malla son hermanos, así que se conserva la jerarquía.
                importador.animationType = ModelImporterAnimationType.Generic;
                importador.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
                importador.preserveHierarchy = true;
            }
            ModelImporterClipAnimation[] clips = importador.defaultClipAnimations;
            if (clips.Length == 0)
            {
                return; // primera pasada: todavía sin esqueleto
            }
            ModelImporterClipAnimation clip = clips[0];
            clip.name = nombre;
            clip.firstFrame = 1f;
            clip.lastFrame = fila.Ultimo;
            clip.loopTime = false;
            clip.loopPose = false;
            clip.wrapMode = WrapMode.Once;
            if (fila.Humanoide)
            {
                // En el sitio (la pose de héroe no debe arrastrar al robot ni hundirlo): rotación, altura y XZ en la pose, con la altura original.
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = false;
                clip.rotationOffset = 0f;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = false;
            }
            importador.clipAnimations = new[] { clip };
        }

        /// <summary>
        /// La descripción humanoide del modelo del personaje sin los huesos que el clip no trae (se leen los nombres mixamorig del FBX), y la cabeza mapeada al cuello cuando el clip no trae Head.
        /// Es la misma idea que ClipsRobotsNovaT1553.DescripcionSinCabeza (área Personaje); se repite aquí para no cruzar ensamblados.
        /// </summary>
        private static HumanDescription DescripcionSinCabeza(ModelImporter modelo, string rutaClip, HumanDescription actual)
        {
            if (modelo == null || modelo.humanDescription.human == null || modelo.humanDescription.human.Length == 0)
            {
                return actual;
            }
            HumanDescription d = modelo.humanDescription;
            var enClip = new HashSet<string>();
            if (File.Exists(rutaClip))
            {
                string texto = System.Text.Encoding.GetEncoding(28591).GetString(File.ReadAllBytes(rutaClip));
                foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(texto, "mixamorig:[A-Za-z0-9_]+"))
                {
                    enClip.Add(m.Value);
                }
            }
            string cuello = null;
            foreach (HumanBone h in d.human)
            {
                if (h.humanName == "Neck")
                {
                    cuello = h.boneName;
                }
            }
            var lista = new List<HumanBone>();
            foreach (HumanBone h in d.human)
            {
                if (h.humanName == "Neck" && (enClip.Count == 0 || !enClip.Contains("mixamorig:Head")))
                {
                    continue;
                }
                HumanBone b = h;
                if (h.humanName == "Head" && cuello != null && (enClip.Count == 0 || !enClip.Contains(h.boneName)))
                {
                    b.boneName = cuello;
                }
                if (enClip.Count > 0 && !enClip.Contains(b.boneName))
                {
                    continue;
                }
                lista.Add(b);
            }
            d.human = lista.ToArray();
            return d;
        }

        // ------------------------------------------------------------------ 2) Materiales, prefab y config

        public static void CrearMaterialesPrefabYConfig()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(RutaShader);
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(RutaCapsula);
            if (shader == null || fbx == null)
            {
                Debug.LogError("[IA] T-1621: falta el shader o el FBX de la cápsula.");
                return;
            }
            AnimationClip clipAbrir = CargarClip(RutaCapsula, CapsulaCaida.NombreClip);
            if (clipAbrir == null)
            {
                Debug.LogError("[IA] T-1621: CP01_Capsula.fbx no trae el clip " + CapsulaCaida.NombreClip);
                return;
            }

            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            try
            {
                PrefabUtility.UnpackPrefabInstance(instancia, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                // El clip CP01_Abrir anima la raíz del FBX (posición y escala absolutas): va dentro de un padre que lleva el script, la posición y la escala.
                instancia.name = "CP01";
                GameObject raiz = new GameObject("CP01_Capsula");
                instancia.transform.SetParent(raiz.transform, false);
                instancia.transform.localScale = Vector3.one;

                // Un material holográfico por cada material original (textura y color copiados), en todas las piezas.
                var porOriginal = new Dictionary<Material, Material>();
                foreach (Renderer r in instancia.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] originales = r.sharedMaterials;
                    var nuevos = new Material[originales.Length];
                    for (int i = 0; i < originales.Length; i++)
                    {
                        Material o = originales[i];
                        Material clave = o;
                        if (clave == null)
                        {
                            nuevos[i] = null;
                            continue;
                        }
                        if (!porOriginal.TryGetValue(clave, out Material holo))
                        {
                            holo = CrearMaterial(shader, o);
                            porOriginal[clave] = holo;
                        }
                        nuevos[i] = holo;
                    }
                    r.sharedMaterials = nuevos;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    r.enabled = true; // el FBX de Blender las trae apagadas
                }

                Animation animacion = instancia.GetComponent<Animation>();
                if (animacion == null)
                {
                    animacion = instancia.AddComponent<Animation>();
                }
                animacion.clip = clipAbrir;
                animacion.playAutomatically = false;
                animacion.cullingType = AnimationCullingType.AlwaysAnimate;

                CapsulaCaida script = raiz.GetComponent<CapsulaCaida>();
                if (script == null)
                {
                    script = raiz.AddComponent<CapsulaCaida>();
                }
                Transform tapa = null;
                var resto = new List<Transform>();
                foreach (Transform t in instancia.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.EndsWith("CP01_Tapa"))
                    {
                        tapa = t;
                    }
                    else if (t.name.EndsWith("CP01_Base") || t.name.Contains("CP01_Pared"))
                    {
                        resto.Add(t);
                    }
                }
                if (tapa == null || resto.Count == 0)
                {
                    Debug.LogError("[IA] T-1621: no encontré CP01_Tapa/CP01_Base/CP01_Pared_* en el FBX; nombres: "
                        + string.Join(", ", instancia.GetComponentsInChildren<Transform>(true).Select(t => t.name)));
                }
                var serial = new SerializedObject(script);
                serial.FindProperty("tapa").objectReferenceValue = tapa;
                SerializedProperty lista = serial.FindProperty("baseYParedes");
                lista.arraySize = resto.Count;
                for (int i = 0; i < resto.Count; i++)
                {
                    lista.GetArrayElementAtIndex(i).objectReferenceValue = resto[i];
                }
                serial.FindProperty("animacion").objectReferenceValue = animacion;
                serial.ApplyModifiedPropertiesWithoutUndo();

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instancia.transform.parent.gameObject, RutaPrefab);
                CrearConfig(prefab);
            }
            finally
            {
                Object.DestroyImmediate(instancia.transform.parent != null ? instancia.transform.parent.gameObject : instancia);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static Material CrearMaterial(Shader shader, Material original)
        {
            string ruta = CarpetaMateriales + "CP01_" + Limpiar(original.name) + "_Holo.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, ruta);
            }
            material.shader = shader;
            Texture textura = original.HasProperty("_BaseMap") ? original.GetTexture("_BaseMap") : original.HasProperty("_MainTex") ? original.GetTexture("_MainTex") : null;
            if (textura != null)
            {
                material.SetTexture("_BaseMap", textura);
            }
            Color color = original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") : original.HasProperty("_Color") ? original.GetColor("_Color") : new Color(0.6f, 0.6f, 0.65f, 1f);
            material.SetColor("_BaseColor", color);
            if (original.IsKeywordEnabled("_EMISSION") && original.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmisColor", original.GetColor("_EmissionColor"));
            }
            material.SetFloat("_Holo", 0f);
            material.SetFloat("_Fade", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static string Limpiar(string nombre)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                nombre = nombre.Replace(c, '_');
            }
            return nombre.Replace(" ", "_");
        }

        private static void CrearConfig(GameObject prefab)
        {
            var config = AssetDatabase.LoadAssetAtPath<ConfigCapsulaCaida>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigCapsulaCaida>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            AnimationClip Clip(int i) => CargarClip(Clips[i].Ruta, Path.GetFileNameWithoutExtension(Clips[i].Ruta));
            var prueba = RutasPrueba.Select(r => AssetDatabase.LoadAssetAtPath<GameObject>(r)).ToArray();
            for (int i = 0; i < prueba.Length; i++)
            {
                if (prueba[i] == null)
                {
                    Debug.LogWarning("[IA] T-1621: falta el prefab de prueba " + RutasPrueba[i]);
                }
            }
            config.Configurar(prefab, Clip(0), Clip(1), Clip(2), Clip(3), prueba);
            EditorUtility.SetDirty(config);
        }

        private static AnimationClip CargarClip(string ruta, string nombre)
        {
            return AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<AnimationClip>().FirstOrDefault(c => c.name == nombre);
        }
    }

    /// <summary>
    /// T-1621: repite la configuración de los cuatro clips *_Capsula_Salida en cada importación. Orden 500: corre después de ImportadorClipsPJ01 (200), de los de Nova (300) y de ImportadorClipsT1560 (400),
    /// que de otro modo los dejarían como clips de la armadura PJ01 o con bucle.
    /// </summary>
    public class PostprocesadorCapsulasT1621 : AssetPostprocessor
    {
        public override int GetPostprocessOrder() => 500;

        private void OnPreprocessModel()
        {
            if (CapsulasCaidaT1621.EsClipDeSalida(assetPath))
            {
                CapsulasCaidaT1621.ConfigurarClip((ModelImporter)assetImporter);
            }
            else if (assetPath == CapsulasCaidaT1621.RutaCapsula)
            {
                // PostprocesadorArte (Mundo) apaga las animaciones de todo FBX de Assets/Arte: la cápsula sí trae CP01_Abrir (Legacy).
                var modelo = (ModelImporter)assetImporter;
                modelo.importAnimation = true;
                modelo.animationType = ModelImporterAnimationType.Legacy;
                modelo.animationCompression = ModelImporterAnimationCompression.Off;
            }
        }
    }
}

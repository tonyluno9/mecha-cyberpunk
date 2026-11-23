using System.IO;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.CampoTiro.Editor
{
    /// <summary>
    /// T-1722 (T79): crea los materiales de la sala y el asset DatosCampoTiro (en Resources/) con los prefabs de costales y de los visuales de Nova.
    /// Se puede volver a correr: actualiza lo que ya existe. Menú: Mecha/Campo de tiro/Aplicar. Batchmode: -executeMethod MechaCyberpunk.CampoTiro.Editor.AplicarCampoTiro.AplicarBatch
    /// </summary>
    public static class AplicarCampoTiro
    {
        private const string Carpeta = "Assets/_Proyecto/Personaje/CampoTiro";
        private const string CarpetaMateriales = Carpeta + "/Materiales";
        private const string CarpetaResources = Carpeta + "/Resources";
        private const string RutaDatos = CarpetaResources + "/DatosCampoTiro.asset";
        private const string Visuales = "Assets/_Proyecto/Personaje/Visual/Prefabs/";
        private const string Costales = "Assets/Arte/Zonas/CB/";
        private const string ShaderUnlit = "Universal Render Pipeline/Unlit";
        private const string CarpetaTexturas = Carpeta + "/Texturas";
        private const string Personajes = "Assets/Arte/Personajes/";

        [MenuItem("Mecha/Campo de tiro/Aplicar")]
        public static void AplicarMenu() => Debug.Log(Aplicar());

        public static void AplicarBatch()
        {
            string resultado = Aplicar();
            Debug.Log(resultado);
            EditorApplication.Exit(resultado.StartsWith("OK") ? 0 : 1);
        }

        /// <summary>Crea o actualiza los assets. Devuelve "OK ..." o el motivo del fallo.</summary>
        public static string Aplicar()
        {
            Directory.CreateDirectory(CarpetaMateriales);
            Directory.CreateDirectory(CarpetaResources);
            AssetDatabase.Refresh();

            Material blanco = CrearMaterial(CarpetaMateriales + "/Mat_CampoBlanco.mat", Color.white);
            Material turquesa = CrearMaterial(CarpetaMateriales + "/Mat_CampoTurquesa.mat", new Color(0.361f, 0.890f, 0.839f, 1f));
            if (blanco == null || turquesa == null)
            {
                return "FALLO: no se pudo crear el material (¿shader " + ShaderUnlit + "?)";
            }

            Material rejilla = CrearRejilla();
            Material holo = CrearHolo();
            if (rejilla == null || holo == null)
            {
                return "FALLO: no se pudo crear el material de rejilla u holograma.";
            }
            RuntimeAnimatorController cCentinela = Control("E15_CentinelaNova/E15_CentinelaNova_a");
            RuntimeAnimatorController cPesado = Control("E16_RobotPesadoNova/E16_RobotPesadoNova_a");
            RuntimeAnimatorController cPerro = Control("E17_PerroRobotNova/E17_PerroRobotNova_a");
            RuntimeAnimatorController cElite = Control("E18_EliteNova/E18_EliteNova_a");
            RuntimeAnimatorController cGuardia = Control("GuardiaNova/GuardiaNova");
            if (cCentinela == null || cPesado == null || cPerro == null || cElite == null || cGuardia == null)
            {
                return "FALLO: falta algún AnimatorController de los Nova.";
            }

            GameObject costalA = Cargar(Costales + "CB02_Costales_a.prefab");
            GameObject costalB = Cargar(Costales + "CB02_Costales_b.prefab");
            GameObject centinela = Cargar(Visuales + "Visual_E15_CentinelaNova_a.prefab");
            GameObject pesado = Cargar(Visuales + "Visual_E16_RobotPesadoNova_a.prefab");
            GameObject perro = Cargar(Visuales + "Visual_E17_PerroRobotNova_a.prefab");
            GameObject elite = Cargar(Visuales + "Visual_E18_EliteNova_a.prefab");
            GameObject guardia = Cargar(Visuales + "Visual_GuardiaNova.prefab");
            if (costalA == null || costalB == null || centinela == null || pesado == null || perro == null || elite == null || guardia == null)
            {
                return "FALLO: falta algún prefab de costales o de visuales de Nova.";
            }

            DatosCampoTiro datos = AssetDatabase.LoadAssetAtPath<DatosCampoTiro>(RutaDatos);
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<DatosCampoTiro>();
                AssetDatabase.CreateAsset(datos, RutaDatos);
            }
            datos.Configurar(new[] { costalA, costalB }, centinela, pesado, perro, elite, guardia, blanco, turquesa, rejilla, holo, cCentinela, cPesado, cPerro, cElite, cGuardia);
            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "OK: DatosCampoTiro en " + RutaDatos;
        }

        private static GameObject Cargar(string ruta)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (prefab == null)
            {
                Debug.LogError("[CampoTiro] No existe " + ruta);
            }
            return prefab;
        }

        private static Material CrearMaterial(string ruta, Color color)
        {
            Shader shader = Shader.Find(ShaderUnlit);
            if (shader == null)
            {
                return null;
            }
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, ruta);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", color);
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }
    
        private static RuntimeAnimatorController Control(string rutaSinExtension)
        {
            RuntimeAnimatorController control = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Personajes + rutaSinExtension + ".controller");
            if (control == null)
            {
                Debug.LogError("[CampoTiro] No existe el controller " + rutaSinExtension);
            }
            return control;
        }

        // Fondo casi negro azulado con líneas cian en el borde de cada celda (la textura se repite cada 2 m).
        private static Material CrearRejilla()
        {
            Texture2D textura = CrearTextura(CarpetaTexturas + "/Tex_Rejilla.asset", 128, 128, (x, y) =>
            {
                bool linea = x < 2 || y < 2;
                bool brillo = x < 1 || y < 1;
                Color fondo = new Color(0.012f, 0.025f, 0.045f, 1f);
                Color cian = new Color(0.10f, 0.75f, 0.85f, 1f);
                return linea ? Color.Lerp(fondo, cian, brillo ? 1f : 0.55f) : fondo;
            });
            Material material = CrearMaterial(CarpetaMateriales + "/Mat_CampoRejilla.mat", Color.white);
            if (material == null)
            {
                return null;
            }
            material.SetTexture("_BaseMap", textura);
            EditorUtility.SetDirty(material);
            return material;
        }

        // Cian translúcido con líneas horizontales, URP Unlit transparente (sin escribir profundidad).
        private static Material CrearHolo()
        {
            Texture2D textura = CrearTextura(CarpetaTexturas + "/Tex_HoloLineas.asset", 4, 32, (x, y) => y % 8 < 2 ? new Color(0.8f, 1f, 1f, 1f) : new Color(0.55f, 0.85f, 0.9f, 0.55f));
            Color cian = new Color(0.30f, 0.92f, 1f, 0.55f);
            Material material = CrearMaterial(CarpetaMateriales + "/Mat_CampoHolo.mat", cian);
            if (material == null)
            {
                return null;
            }
            material.SetTexture("_BaseMap", textura);
            material.SetTextureScale("_BaseMap", new Vector2(1f, 6f));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D CrearTextura(string ruta, int ancho, int alto, System.Func<int, int, Color> pixel)
        {
            Directory.CreateDirectory(CarpetaTexturas);
            Texture2D textura = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
            if (textura == null)
            {
                textura = new Texture2D(ancho, alto, TextureFormat.RGBA32, true);
                AssetDatabase.CreateAsset(textura, ruta);
            }
            for (int y = 0; y < alto; y++)
            {
                for (int x = 0; x < ancho; x++)
                {
                    textura.SetPixel(x, y, pixel(x, y));
                }
            }
            textura.wrapMode = TextureWrapMode.Repeat;
            textura.filterMode = FilterMode.Bilinear;
            textura.anisoLevel = 8;
            textura.Apply(true);
            EditorUtility.SetDirty(textura);
            return textura;
        }
    }
}

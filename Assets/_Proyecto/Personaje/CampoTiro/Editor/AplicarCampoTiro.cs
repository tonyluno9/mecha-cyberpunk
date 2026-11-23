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
            datos.Configurar(new[] { costalA, costalB }, centinela, pesado, perro, elite, guardia, blanco, turquesa);
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
    }
}

using System.Collections.Generic;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.UI.Armas;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-814: crea (o rehace) el material holográfico UI/Materiales/HologramaArma.mat y la config del banco de Tino UI/Datos/ConfigMenuArmas_Base.asset:
    /// la pistola con su modelo W01 y las otras tres con su imagen holográfica (H02-H04) mientras no tengan modelo 3D. Idempotente: conserva el asset
    /// (y su guid) y solo vuelve a llenar las listas. Las imágenes las mete antes ImportarS2nHologramas.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorMenuArmas.Crear
    /// </summary>
    public static class CreadorMenuArmas
    {
        public const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigMenuArmas_Base.asset";
        public const string RutaMaterial = "Assets/_Proyecto/UI/Materiales/HologramaArma.mat";
        public const string RutaShader = "Assets/_Proyecto/UI/Armas/Shaders/HologramaArma.shader";
        public const string RutaAditivo = "Assets/_Proyecto/UI/Resources/UIAditivo.mat";
        public const string RutaPistola = "Assets/Arte/Armas/W01_Pistola/W01_Pistola.prefab";
        public const string CarpetaHologramas = "Assets/Arte/UI/Hologramas/";

        [MenuItem("Mecha Cyberpunk/UI/Menú de mejora de armas (T-814)")]
        public static void Crear()
        {
            Material material = CrearMaterial();
            Material aditivo = AssetDatabase.LoadAssetAtPath<Material>(RutaAditivo);
            Texture2D h01 = Textura("H01_Pistola.png");
            Texture2D h02 = Textura("H02_Escopeta.png");
            Texture2D h03 = Textura("H03_Fusil.png");
            Texture2D h04 = Textura("H04_Francotirador.png");
            GameObject pistola = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPistola);

            var armas = new List<EntradaArmaMenu>
            {
                EntradaArmaMenu.Crear(ArmaAPie.Pistola, "PISTOLA", h01, pistola, h01),
                EntradaArmaMenu.Crear(ArmaAPie.Escopeta, "ESCOPETA", h02, null, h02),
                EntradaArmaMenu.Crear(ArmaAPie.Fusil, "FUSIL", h03, null, h03),
                EntradaArmaMenu.Crear(ArmaAPie.Sniper, "FRANCOTIRADOR", h04, null, h04)
            };
            var piezas = new List<EntradaPiezaMenu>
            {
                EntradaPiezaMenu.Crear(PiezaArma.Cargador, "CARGADOR", "MÁS BALAS", EstadisticaArma.Cargador, 0.55f),
                EntradaPiezaMenu.Crear(PiezaArma.Canon, "CAÑÓN", "MÁS DAÑO", EstadisticaArma.Danio, 0.3f),
                EntradaPiezaMenu.Crear(PiezaArma.Mira, "MIRA", "MÁS PRECISIÓN", EstadisticaArma.Dispersion, 0.16f)
            };

            ConfigMenuArmas config = AssetDatabase.LoadAssetAtPath<ConfigMenuArmas>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigMenuArmas>();
                config.Configurar(armas, piezas, material, aditivo);
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            else
            {
                config.Configurar(armas, piezas, material, aditivo);
                EditorUtility.SetDirty(config);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] Menú de armas: config " + RutaConfig + " (pistola 3D: " + (pistola != null) + ", hologramas: "
                + (h01 != null) + (h02 != null) + (h03 != null) + (h04 != null) + ")");
        }

        private static Material CrearMaterial()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(RutaShader);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial);
            if (material == null)
            {
                material = new Material(shader) { name = "HologramaArma" };
                AssetDatabase.CreateAsset(material, RutaMaterial);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
            }
            return material;
        }

        private static Texture2D Textura(string archivo) => AssetDatabase.LoadAssetAtPath<Texture2D>(CarpetaHologramas + archivo);
    }
}

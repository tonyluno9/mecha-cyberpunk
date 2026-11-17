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

        // T-942: las cuatro armas llevan modelo 3D.
        private static GameObject Modelo(string carpeta) => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Armas/" + carpeta + "/" + carpeta + ".prefab");

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
            // T-1014: las armas nuevas (H05-H09) con su modelo; sus modelos no traen piezas separadas (cargador, cañón, mira), así que van sin nombres de pieza y el holograma se ve igual.
            Texture2D h05 = Textura("H05_Subfusil.png");
            Texture2D h06 = Textura("H06_Revolver.png");
            Texture2D h07 = Textura("H07_Lanzagranadas.png");
            Texture2D h08 = Textura("H08_Granada.png");
            Texture2D h09 = Textura("H09_RiflePlasma.png");

            var armas = new List<EntradaArmaMenu>
            {
                EntradaArmaMenu.Crear(ArmaAPie.Pistola, "PISTOLA", h01, pistola, h01, "Bateria", "Corredera", "Mira"),
                EntradaArmaMenu.Crear(ArmaAPie.Escopeta, "ESCOPETA", h02, Modelo("W02_Escopeta"), h02, "Bomba", "Canon", "", false),
                EntradaArmaMenu.Crear(ArmaAPie.Fusil, "FUSIL", h03, Modelo("W03_Fusil"), h03, "Cargador", "Canon", "Mira"),
                EntradaArmaMenu.Crear(ArmaAPie.Sniper, "FRANCOTIRADOR", h04, Modelo("W04_Francotirador"), h04, "Cargador", "Canon", "Mira"),
                EntradaArmaMenu.Crear(ArmaAPie.Subfusil, "SUBFUSIL", h05, Modelo("W05_Subfusil"), h05, "Cargador", "Canon", "Mira"),
                EntradaArmaMenu.Crear(ArmaAPie.Revolver, "REVÓLVER", h06, Modelo("W06_Revolver"), h06, "Tambor", "Canon", "", false),
                EntradaArmaMenu.Crear(ArmaAPie.Lanzagranadas, "LANZAGRANADAS", h07, Modelo("W08_Lanzagranadas"), h07, "Tambor", "Canon", "", false),
                EntradaArmaMenu.Crear(ArmaAPie.Granada, "GRANADA", h08, Modelo("W09_Granada"), h08, "", "", ""),
                EntradaArmaMenu.Crear(ArmaAPie.RiflePlasma, "RIFLE DE PLASMA", h09, Modelo("W07_RiflePlasma"), h09, "Celda", "Canon", "Mira")
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

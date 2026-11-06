using System.IO;
using MechaCyberpunk.UI.Kit;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-814: las imágenes holográficas de las armas (imageassets/ui/hologramas/H01-H04, fondo negro) a Assets/Arte/UI/Hologramas/ para la lista y
    /// el holograma del banco de Tino. Copia solo si el archivo existe y cambió; idempotente. Las deja como textura normal, sin mipmaps, a 1024 px.
    /// Si falta alguna, el menú usa el modelo 3D o un holograma vacío (no se rompe).
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ImportarS2nHologramas.Importar
    /// </summary>
    public static class ImportarS2nHologramas
    {
        public const string CarpetaOrigen = "imageassets/ui/hologramas/";
        public const string CarpetaDestino = "Assets/Arte/UI/Hologramas/";
        private const int TamanoMaximo = 1024;
        public const string RutaPiezas = "Assets/_Proyecto/UI/Resources/PiezasUI.asset";
        public const string RutaMaterialHud = "Assets/_Proyecto/UI/Resources/UIHologramaHud.mat";
        public const string ShaderHud = "MechaCyberpunk/UI/HologramaHud";

        /// <summary>Los archivos que se buscan (sin carpeta), en el orden de las armas: pistola, escopeta, fusil, francotirador.</summary>
        public static readonly string[] Archivos = { "H01_Pistola.png", "H02_Escopeta.png", "H03_Fusil.png", "H04_Francotirador.png" };

        [MenuItem("Mecha Cyberpunk/S2n/Hologramas de armas (H01-H04)")]
        public static void Importar()
        {
            Directory.CreateDirectory(CarpetaDestino);
            int copiados = 0;
            foreach (string archivo in Archivos)
            {
                string origen = CarpetaOrigen + archivo;
                string destino = CarpetaDestino + archivo;
                if (!File.Exists(origen))
                {
                    continue;
                }
                if (!File.Exists(destino) || new FileInfo(origen).Length != new FileInfo(destino).Length)
                {
                    File.Copy(origen, destino, true);
                    copiados++;
                }
            }
            AssetDatabase.Refresh();
            foreach (string archivo in Archivos)
            {
                Configurar(CarpetaDestino + archivo);
            }
            Debug.Log("[Arte] Hologramas de armas: " + copiados + " copiados a " + CarpetaDestino);
            PrepararHud();
        }

        /// <summary>
        /// T-816: pone H01-H04 como icono del arma en el HUD (PiezasUI.armasHolograma, en el orden de ArmaAPie: 1 pistola ... 4 sniper) con el
        /// material UIHologramaHud. Si falta una imagen, esa arma se queda con su icono de siempre. Idempotente.
        /// </summary>
        public static void PrepararHud()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialHud);
            if (material == null)
            {
                Shader shader = Shader.Find(ShaderHud);
                if (shader == null)
                {
                    Debug.LogError("[Arte] Falta el shader " + ShaderHud);
                    return;
                }
                material = new Material(shader) { name = "UIHologramaHud" };
                AssetDatabase.CreateAsset(material, RutaMaterialHud);
            }
            PiezasUI piezas = AssetDatabase.LoadAssetAtPath<PiezasUI>(RutaPiezas);
            if (piezas == null)
            {
                Debug.LogError("[Arte] Falta " + RutaPiezas);
                return;
            }
            SerializedObject so = new SerializedObject(piezas);
            SerializedProperty lista = so.FindProperty("armasHolograma");
            lista.arraySize = Archivos.Length + 1;
            lista.GetArrayElementAtIndex(0).objectReferenceValue = null;
            int puestos = 0;
            for (int i = 0; i < Archivos.Length; i++)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CarpetaDestino + Archivos[i]);
                lista.GetArrayElementAtIndex(i + 1).objectReferenceValue = sprite;
                puestos += sprite != null ? 1 : 0;
            }
            so.FindProperty("materialHolograma").objectReferenceValue = material;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log("[Arte] HUD: " + puestos + " armas con icono holográfico");
        }

        /// <summary>La textura del holograma de esa posición (0 = pistola), o null si no se ha metido.</summary>
        public static Texture2D Cargar(int indice)
        {
            return indice >= 0 && indice < Archivos.Length ? AssetDatabase.LoadAssetAtPath<Texture2D>(CarpetaDestino + Archivos[indice]) : null;
        }

        private static void Configurar(string ruta)
        {
            TextureImporter importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (importador == null)
            {
                return;
            }
            // T-816: como sprite (también sirve de textura al banco) y con mipmaps, que en el HUD se dibuja chica.
            bool cambio = importador.textureType != TextureImporterType.Sprite || importador.spriteImportMode != SpriteImportMode.Single
                || !importador.mipmapEnabled || importador.maxTextureSize != TamanoMaximo || importador.wrapMode != TextureWrapMode.Clamp
                || importador.alphaIsTransparency;
            if (!cambio)
            {
                return;
            }
            importador.textureType = TextureImporterType.Sprite;
            importador.spriteImportMode = SpriteImportMode.Single;
            importador.mipmapEnabled = true;
            importador.maxTextureSize = TamanoMaximo;
            importador.wrapMode = TextureWrapMode.Clamp;
            importador.alphaIsTransparency = false;
            importador.SaveAndReimport();
        }
    }
}

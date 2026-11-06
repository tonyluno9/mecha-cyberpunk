using UnityEditor;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// Importa los modelos de T-250 (Assets/Arte/&lt;Zona&gt;/): 1 unidad = 1 m, ejes horneados, sin cámaras, luces ni animaciones,
    /// y sin materiales del archivo (el prefab les pone su URP Lit). Las texturas quedan a 2048 como máximo.
    /// Corre después del PostprocesadorArte de Alan, que también mira Assets/Arte/.
    /// </summary>
    public class PostprocesadorAssetsT250 : AssetPostprocessor
    {
        private const int OrdenDespuesDeAlan = 100;
        private const int TamanoMaximoTextura = 2048;

        public override int GetPostprocessOrder()
        {
            return OrdenDespuesDeAlan;
        }

        private void OnPreprocessModel()
        {
            if (!ArteT250.EsRutaDeZona(assetPath) || !assetPath.EndsWith(".fbx"))
            {
                return;
            }

            if (assetPath.StartsWith(CarpetaAnimacionesPersonajes))
            {
                // T-520: animaciones de Mixamo (solo el movimiento) para los personajes Humanoid; CreadorVisualTripo las configura.
                ModelImporter animacion = (ModelImporter)assetImporter;
                animacion.importAnimation = true;
                animacion.animationType = ModelImporterAnimationType.Human;
                animacion.materialImportMode = ModelImporterMaterialImportMode.None;
                return;
            }

            if (EsPersonajeDeTripo(assetPath))
            {
                // T-500: personajes de Tripo con esqueleto y animaciones: se conservan (T-520: Humanoid con el avatar de su propio esqueleto).
                ModelImporter personaje = (ModelImporter)assetImporter;
                personaje.importAnimation = true;
                personaje.animationType = ModelImporterAnimationType.Human;
                personaje.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                personaje.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                // T-520: la escala que lleva el modelo a 1.8 m se guarda en userData ("escala=1.41"); PostprocesadorArte la deja en 1.
                personaje.globalScale = EscalaPersonaje(personaje.userData);
                return;
            }

            ModelImporter modelo = (ModelImporter)assetImporter;
            modelo.useFileScale = true;
            modelo.globalScale = 1f;
            modelo.bakeAxisConversion = true;
            modelo.importAnimation = false;
            modelo.animationType = ModelImporterAnimationType.None;
            modelo.importCameras = false;
            modelo.importLights = false;
            modelo.importBlendShapes = false;
            modelo.materialImportMode = ModelImporterMaterialImportMode.None;
            modelo.isReadable = false;
        }

        /// <summary>Personajes de Tripo con esqueleto: man, woman, woman2 (T-500) y los siete de T-510.</summary>
        public static readonly string[] PersonajesDeTripo =
        {
            "man", "woman", "woman2", "SkullHombre", "Doctor", "Inventor", "Hacker", "RobotHombre", "GuardiaNova", "Mecanico", "Policia",
        };

        public static bool EsPersonajeDeTripo(string ruta)
        {
            foreach (string n in PersonajesDeTripo)
            {
                if (ruta.StartsWith("Assets/Arte/Personajes/" + n + "/"))
                {
                    return true;
                }
            }
            return false;
        }

        public const string CarpetaAnimacionesPersonajes = "Assets/Arte/Personajes/Animaciones/";
        public const string PrefijoEscalaPersonaje = "escala=";

        /// <summary>Escala guardada en el userData de un personaje; 1 si no hay.</summary>
        public static float EscalaPersonaje(string userData)
        {
            if (!string.IsNullOrEmpty(userData) && userData.StartsWith(PrefijoEscalaPersonaje)
                && float.TryParse(userData.Substring(PrefijoEscalaPersonaje.Length), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float escala) && escala > 0f)
            {
                return escala;
            }
            return 1f;
        }

        private void OnPreprocessTexture()
        {
            if (!ArteT250.EsRutaDeZona(assetPath))
            {
                return;
            }

            TextureImporter textura = (TextureImporter)assetImporter;
            textura.maxTextureSize = TamanoMaximoTextura;
            textura.mipmapEnabled = true;
            if (assetPath.Contains("_normal."))
            {
                textura.textureType = TextureImporterType.NormalMap;
            }
            else
            {
                textura.textureType = TextureImporterType.Default;
                textura.sRGBTexture = true;
            }
        }
    }
}

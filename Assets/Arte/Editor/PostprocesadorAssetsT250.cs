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

            if (assetPath.StartsWith(CarpetaPerroRobot))
            {
                // T-1389b: el perro robot (E17) es un cuadrúpedo Generic con sus clips (Perro_Idle, Perro_Caminar) dentro del FBX.
                ModelImporter perro = (ModelImporter)assetImporter;
                perro.importAnimation = true;
                perro.animationType = ModelImporterAnimationType.Generic;
                perro.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                perro.useFileScale = true;
                // T-1497: la escala que lleva al perro a su alto real (1.2 m) va en userData ("escala=1.15") para que un reimport no la pierda; 1 si no hay.
                perro.globalScale = EscalaPersonaje(perro.userData);
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
            "man", "woman", "woman2", "SkullHombre", "Doctor", "Inventor", "Hacker", "RobotHombre", "GuardiaNova", "Mecanico", "Policia", "PJ01", "AL10", "AL11",
            "E10_PandilleroSnakes", "E11_JefeSnakes", "E12_GuardiaPesadoNova", "E14_JefeFinalNova",
            // T-1389b: los rigeados en Blender (esqueleto Mixamo), una carpeta por código y un FBX por variante.
            "E15_CentinelaNova", "E16_RobotPesadoNova", "E18_EliteNova", "E19_PandilleroSkulls", "E20_MatonSkulls",
            "NP07_Pescador", "NP08_Turista", "NP09_Corredor", "NP10_Elotero", "NP11_Estibador", "NP12_Abuela", "NP13_Repartidor", "NP14_Oficinista",
        };

        public const string CarpetaPerroRobot = "Assets/Arte/Personajes/E17_PerroRobotNova/";

        public static bool EsPersonajeDeTripo(string ruta)
        {
            // T-892: las meseras R01 se importan igual que los demás personajes de Tripo.
            if (ruta.StartsWith("Assets/Arte/Personajes/" + ColocarS2pMeseras.Nombre + "/"))
            {
                return true;
            }
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

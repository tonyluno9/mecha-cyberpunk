using System.IO;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-640: arregla que el personaje caminaba de espaldas y que caminar y correr se veían igual.
    /// (a) El nodo Giro de los visuales de Tripo queda sin giro (el modelo ya mira a +Z con su Avatar Humanoid).
    /// (b) Se reconstruye el controlador base (umbral Caminar/Correr entre andar y correr, árboles de arma por velocidad real).
    /// (c) Los clips de locomoción hornean la rotación de la raíz (Bake Into Pose, basada en la orientación del cuerpo: así el frente del clip es el del modelo).
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.CorregirAnimacionesT640.Aplicar
    /// </summary>
    public static class CorregirAnimacionesT640
    {
        private const string CarpetaPrefabs = "Assets/_Proyecto/Personaje/Visual/Prefabs";
        private const string CarpetaClips = "Assets/Arte/Personajes/Animaciones";
        private static readonly string[] Prefijos = { "Loco_", "Pistola_", "Rifle_", "Nova_", "Skull_", "Jefe_" };

        [MenuItem("Mecha Cyberpunk/Personajes/T-640 Corregir animaciones")]
        public static void Aplicar()
        {
            CorregirGiro();
            HornearRaiz();
            ConstructorControladorBase.Construir();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Personaje] T-640: giro, raiz de clips y controlador base corregidos.");
        }

        public static void CorregirGiro()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab Visual_", new[] { CarpetaPrefabs }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
                try
                {
                    Transform giro = contenido.transform.Find("Giro");
                    if (giro != null && giro.localRotation != Quaternion.identity)
                    {
                        giro.localRotation = Quaternion.identity;
                        PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contenido);
                }
            }
        }

        // Giros, saltos, caídas y frenadas giran el cuerpo a propósito (el torso de la pose): esos clips no hornean la rotación.
        private static bool EsDeLocomocion(string nombre)
        {
            foreach (string p in Prefijos)
            {
                if (nombre.StartsWith(p))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool DebeHornear(string nombre)
        {
            if (nombre.Contains("Girar") || nombre.Contains("Salto") || nombre.Contains("Caida") || nombre.Contains("Frenar"))
            {
                return false;
            }
            foreach (string p in Prefijos)
            {
                if (nombre.StartsWith(p))
                {
                    return true;
                }
            }
            return false;
        }

        public static void HornearRaiz()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { CarpetaClips }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                string nombre = Path.GetFileNameWithoutExtension(ruta);
                if (!EsDeLocomocion(nombre) || !(AssetImporter.GetAtPath(ruta) is ModelImporter imp))
                {
                    continue;
                }
                bool hornear = DebeHornear(nombre);
                ModelImporterClipAnimation[] clips = imp.clipAnimations;
                bool cambio = false;
                foreach (ModelImporterClipAnimation c in clips)
                {
                    if (c.lockRootRotation != hornear || c.keepOriginalOrientation)
                    {
                        c.lockRootRotation = hornear;
                        c.keepOriginalOrientation = false;
                        cambio = true;
                    }
                }
                if (cambio)
                {
                    imp.clipAnimations = clips;
                    imp.SaveAndReimport();
                }
            }
        }
    }
}

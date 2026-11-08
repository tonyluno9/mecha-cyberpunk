using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-733 (A3): deja listo el Animator del jugador con los clips de Tony. Rearma el controlador base y sus overrides (agachado, rodar, sacar y guardar
    /// la pistola, apuntar en cuatro direcciones), recalibra la velocidad del Loco_Caminar nuevo en los visuales que lo usan y pone la rodada de
    /// DatosCubierta a lo que dura Loco_Rodar (2.2 m en 1 s). Se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.AjustarAnimatorT733.Aplicar
    /// </summary>
    public static class AjustarAnimatorT733
    {
        private const string RutaDatosCubierta = "Assets/_Proyecto/Personaje/Datos/Resources/DatosCubierta.asset";
        private const string CarpetaVisuales = "Assets/_Proyecto/Personaje/Visual/Prefabs";
        /// <summary>El valor con el que quedaron los visuales que usan Loco_Caminar antes del Loco_Caminar ajustado de Tony.</summary>
        private const float VelocidadCaminarAnterior = 1.28f;

        [MenuItem("Mecha Cyberpunk/T-733/Ajustar animator del jugador")]
        public static void Aplicar()
        {
            ConstructorControladorBase.Construir();
            DatosRodada();
            RecalibrarCaminar();
            AssetDatabase.SaveAssets();
        }

        public static void DatosRodada()
        {
            Object datos = AssetDatabase.LoadAssetAtPath<Object>(RutaDatosCubierta);
            var so = new SerializedObject(datos);
            so.FindProperty("distanciaRodada").floatValue = ReglasAnimacion.DistanciaRodada;
            so.FindProperty("duracionRodada").floatValue = ReglasAnimacion.DuracionRodada;
            so.FindProperty("enfriamientoRodada").floatValue = 1.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
        }

        /// <summary>Los visuales con la velocidad vieja de Loco_Caminar (1.28) pasan a la medida del nuevo; los de clips propios (Skull, Nova...) no se tocan.</summary>
        public static void RecalibrarCaminar()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab Visual_", new[] { CarpetaVisuales }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
                try
                {
                    VisualPersonaje visual = contenido.GetComponent<VisualPersonaje>();
                    if (visual == null)
                    {
                        continue;
                    }
                    var so = new SerializedObject(visual);
                    SerializedProperty p = so.FindProperty("velocidadClipCaminar");
                    if (Mathf.Abs(p.floatValue - VelocidadCaminarAnterior) < 0.01f)
                    {
                        p.floatValue = ReglasAnimacion.VelocidadClipCaminar;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
                        Debug.Log("[Personaje] T-733: " + System.IO.Path.GetFileName(ruta) + " velocidadClipCaminar " + VelocidadCaminarAnterior + " -> " + ReglasAnimacion.VelocidadClipCaminar);
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contenido);
                }
            }
        }
    }
}

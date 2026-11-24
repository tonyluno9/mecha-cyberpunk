using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T98 (24-nov): las habilidades de prótesis duran el triple. Escribe en <c>Datos/Resources/ConfigHabilidadesProtesis.asset</c> las duraciones
    /// de la tabla <see cref="Tabla"/> (valores fijos, ya multiplicados por 3), así que correrlo dos veces deja lo mismo. La recarga no cambia.
    /// Batch: <c>-executeMethod MechaCyberpunk.Personaje.Editor.DuracionesProtesisT98.Aplicar</c>.
    /// </summary>
    public static class DuracionesProtesisT98
    {
        /// <summary>Campo del config, duración nivel I antes, nivel II antes (s). Lo que se escribe es el triple.</summary>
        public static readonly (string campo, float nivel1Antes, float nivel2Antes)[] Tabla =
        {
            ("piernas", 6f, 10f),
            ("brazos", 6f, 10f),
            ("cabeza", 8f, 14f),
            ("laser", 4f, 4f),
            ("misiles", 1f, 1f),
        };

        public const float Factor = 3f;

        [MenuItem("Mecha Cyberpunk/Personaje/T98 Duraciones de prótesis x3")]
        public static void Aplicar()
        {
            ConfigHabilidadesProtesis config = AssetDatabase.LoadAssetAtPath<ConfigHabilidadesProtesis>(CreadorHabilidadesProtesis.RutaConfig);
            if (config == null)
            {
                Debug.LogError("[Personaje] T98: no encuentro " + CreadorHabilidadesProtesis.RutaConfig);
                return;
            }
            SerializedObject so = new SerializedObject(config);
            foreach ((string campo, float nivel1Antes, float nivel2Antes) in Tabla)
            {
                SerializedProperty t = so.FindProperty(campo);
                if (t == null)
                {
                    Debug.LogError("[Personaje] T98: el config no tiene el campo " + campo);
                    continue;
                }
                t.FindPropertyRelative("duracionNivel1").floatValue = nivel1Antes * Factor;
                t.FindPropertyRelative("duracionNivel2").floatValue = nivel2Antes * Factor;
                Debug.Log("[Personaje] T98: " + campo + " dura " + (nivel1Antes * Factor) + " s / " + (nivel2Antes * Factor)
                    + " s (recarga " + t.FindPropertyRelative("recarga").floatValue + " s, igual)");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }
    }
}

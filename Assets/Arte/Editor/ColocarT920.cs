using System.Linq;
using MechaCyberpunk.Mundo.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-920: casi sin niebla de noche para que se vea el fondo de la ciudad. Baja la curva de densidad de noche de ConfigCicloDia_Mundo a
    /// <see cref="ColocarS2oLuz.DensidadNiebla"/> y la niebla guardada en la escena (en juego la manda CicloDiaNoche). Repetible y mínimo.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT920.Aplicar
    /// </summary>
    public static class ColocarT920
    {
        public static void Aplicar()
        {
            ConfigCicloDia ciclo = AssetDatabase.LoadAssetAtPath<ConfigCicloDia>(ColocarS2cLook.RutaCicloDia);
            SerializedObject so = new SerializedObject(ciclo);
            AnimationCurve curva = so.FindProperty("densidadNiebla").animationCurveValue;
            Keyframe[] llaves = curva.keys;
            for (int i = 0; i < llaves.Length; i++)
            {
                if (llaves[i].time <= 4.5f || llaves[i].time >= 21f)
                {
                    llaves[i].value = ColocarS2oLuz.DensidadNiebla;
                    llaves[i].inTangent = 0f;
                    llaves[i].outTangent = 0f;
                }
            }
            curva.keys = llaves;
            so.FindProperty("densidadNiebla").animationCurveValue = curva;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(ciclo);

            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            RenderSettings.fogDensity = ColocarS2oLuz.DensidadNiebla;
            ColocarS2.Informe.AppendLine("T-920: niebla de noche " + ColocarS2oLuz.DensidadNiebla + ", llaves: " + string.Join(", ", llaves.Select(k => k.time + "=" + k.value)));
            ColocarS2.Guardar("t920-niebla");
        }
    }
}

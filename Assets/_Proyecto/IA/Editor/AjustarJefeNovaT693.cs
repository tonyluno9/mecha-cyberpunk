using MechaCyberpunk.IA.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// T-693: el Capitán Nova pelea a golpes con patrón (telegrafía, golpe de área, recuperación, carga) en vez de la escopeta que no acertaba.
    /// Pone jefeCuerpoACuerpo en true y usaPistola en false en DatosPandilleroAPie_JefeNova. Se puede correr varias veces.
    /// Uso: Unity -batchmode -executeMethod MechaCyberpunk.IA.Editor.AjustarJefeNovaT693.Aplicar
    /// </summary>
    public static class AjustarJefeNovaT693
    {
        public const string RutaDatos = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_JefeNova.asset";

        public static void Aplicar()
        {
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatos);
            if (datos == null)
            {
                Debug.LogError("[IA] Falta " + RutaDatos);
                return;
            }
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("jefeCuerpoACuerpo").boolValue = true;
            so.FindProperty("usaPistola").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssets();
            Debug.Log("[IA] T-693: Capitán Nova cuerpo a cuerpo con patrón.");
        }
    }
}

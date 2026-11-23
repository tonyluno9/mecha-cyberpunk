using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// T-1660 (T42): el policía a pie aguantaba 2 balazos (los de la cabeza hacen x3). Sube su vida a <see cref="VidaPolicia"/>: la pistola del jugador hace 25 por bala al cuerpo,
    /// así que aguanta 5 tiros al cuerpo y cae al sexto (con la cabeza, 75 por bala, cae al segundo). Repetible: deja siempre el mismo valor.
    /// Batchmode: -executeMethod MechaCyberpunk.IA.Editor.AjustarVidaPoliciaT42.Aplicar
    /// </summary>
    public static class AjustarVidaPoliciaT42
    {
        public const string RutaDatos = AjustarPoliciaT709.RutaDatos;
        /// <summary>Vida máxima del policía a pie (puntos).</summary>
        public const float VidaPolicia = 140f;

        [MenuItem("Mecha Cyberpunk/T-1660/Subir la vida del policia a pie")]
        public static void Aplicar()
        {
            ScriptableObject datos = AssetDatabase.LoadAssetAtPath<ScriptableObject>(RutaDatos);
            if (datos == null)
            {
                Debug.LogError("[IA] T-1660: falta " + RutaDatos);
                return;
            }
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("vidaMaxima").floatValue = VidaPolicia;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssets();
            Debug.Log("[IA] T-1660: vida del policía a pie en " + VidaPolicia + ".");
        }
    }
}

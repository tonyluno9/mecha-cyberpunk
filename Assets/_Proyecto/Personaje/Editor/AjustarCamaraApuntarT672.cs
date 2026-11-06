using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-672: al apuntar el personaje queda centrado en la pantalla (sin desplazamiento lateral) y la cámara va detrás y más alta, de modo que la mira
    /// quede arriba de la cabeza. Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.AjustarCamaraApuntarT672.Ajustar
    /// </summary>
    public static class AjustarCamaraApuntarT672
    {
        private const string RutaCamara = "Assets/_Proyecto/Personaje/Datos/ConfigCamaraHombro_Base.asset";

        [MenuItem("Mecha Cyberpunk/Ajustar T-672 cámara al apuntar")]
        public static void Ajustar()
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(RutaCamara);
            SerializedObject so = new SerializedObject(asset);
            Poner(so, "lateralApuntando", 0f);
            Poner(so, "alturaApuntando", 2.0f);
            Poner(so, "distanciaApuntando", 2.0f);
            Poner(so, "fovApuntando", 45f);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        private static void Poner(SerializedObject so, string campo, float valor)
        {
            so.FindProperty(campo).floatValue = valor;
        }
    }
}

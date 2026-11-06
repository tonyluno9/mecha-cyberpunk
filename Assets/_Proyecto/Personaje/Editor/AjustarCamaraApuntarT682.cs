using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-682: al apuntar la cámara tiene el mismo encuadre que al caminar (misma altura y distancia, personaje centrado) y solo cambia el zoom (FOV menor).
    /// Deshace la subida de T-672. Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.AjustarCamaraApuntarT682.Ajustar
    /// </summary>
    public static class AjustarCamaraApuntarT682
    {
        private const string RutaCamara = "Assets/_Proyecto/Personaje/Datos/ConfigCamaraHombro_Base.asset";
        private const float FovApuntando = 40f;

        [MenuItem("Mecha Cyberpunk/Ajustar T-682 cámara al apuntar con zoom")]
        public static void Ajustar()
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(RutaCamara);
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("lateralApuntando").floatValue = 0f;
            so.FindProperty("alturaApuntando").floatValue = so.FindProperty("alturaNormal").floatValue;
            so.FindProperty("distanciaApuntando").floatValue = so.FindProperty("distanciaNormal").floatValue;
            so.FindProperty("fovApuntando").floatValue = FovApuntando;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }
    }
}

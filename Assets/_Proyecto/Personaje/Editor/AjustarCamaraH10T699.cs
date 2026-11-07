using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-699: cámara como la imagen H10_HUD.png: personaje centrado (lateral 0) y como 1/4 del alto de la pantalla, cámara alta mirando a lo largo
    /// de la calle. Apuntando conserva el encuadre (T-682) y solo hace zoom. Valores medidos en Ciudad_Prologo con CamaraH10T699Pruebas.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.AjustarCamaraH10T699.Ajustar
    /// </summary>
    public static class AjustarCamaraH10T699
    {
        private const string RutaCamara = "Assets/_Proyecto/Personaje/Datos/ConfigCamaraHombro_Base.asset";
        private const float Altura = 3.4f;
        private const float Distancia = 7f;
        private const float FovNormal = 65f;
        private const float FovApuntando = 45f;

        [MenuItem("Mecha Cyberpunk/Ajustar T-699 cámara H10")]
        public static void Ajustar()
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(RutaCamara);
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("lateralNormal").floatValue = 0f;
            so.FindProperty("alturaNormal").floatValue = Altura;
            so.FindProperty("distanciaNormal").floatValue = Distancia;
            so.FindProperty("fovNormal").floatValue = FovNormal;
            so.FindProperty("lateralApuntando").floatValue = 0f;
            so.FindProperty("alturaApuntando").floatValue = Altura;
            so.FindProperty("distanciaApuntando").floatValue = Distancia;
            so.FindProperty("fovApuntando").floatValue = FovApuntando;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }
    }
}

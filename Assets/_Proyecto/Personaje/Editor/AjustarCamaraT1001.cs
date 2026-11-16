using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-1001: cámara a pie como CAM04_SinApuntar.png (personaje casi centrado, un poco a la izquierda, cuerpo completo) y al apuntar como
    /// CAM05_Apuntando_Hombro.png (se acerca y se corre al hombro derecho: personaje a la izquierda, de la cintura para arriba).
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.AjustarCamaraT1001.Ajustar
    /// </summary>
    public static class AjustarCamaraT1001
    {
        private const string RutaCamara = "Assets/_Proyecto/Personaje/Datos/ConfigCamaraHombro_Base.asset";
        public const float LateralNormal = 0.3f;
        public const float LateralApuntando = 0.75f;
        public const float AlturaApuntando = 1.55f;
        public const float DistanciaApuntando = 1.5f;
        public const float FovApuntando = 45f;
        public const float RapidezCambioHombro = 6f;
        public const float MargenHombro = 0.25f;

        [MenuItem("Mecha Cyberpunk/Ajustar T-1001 cámara al hombro")]
        public static void Ajustar()
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(RutaCamara);
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("lateralNormal").floatValue = LateralNormal;
            so.FindProperty("lateralApuntando").floatValue = LateralApuntando;
            so.FindProperty("alturaApuntando").floatValue = AlturaApuntando;
            so.FindProperty("distanciaApuntando").floatValue = DistanciaApuntando;
            so.FindProperty("fovApuntando").floatValue = FovApuntando;
            so.FindProperty("rapidezCambioHombro").floatValue = RapidezCambioHombro;
            so.FindProperty("margenHombro").floatValue = MargenHombro;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] T-1001: cámara al hombro ajustada");
        }
    }
}

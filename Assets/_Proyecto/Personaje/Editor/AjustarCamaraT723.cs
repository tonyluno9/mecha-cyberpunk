using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-723: valores de la cámara a pie con Cinemachine, medidos contra CAM01_APie.png y CAM02_APieCerca.png: detrás y un poco arriba del
    /// hombro, personaje de ~45 % del alto de la pantalla y en la parte baja. Apuntando conserva el encuadre (T-682) y solo hace zoom.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.AjustarCamaraT723.Ajustar
    /// </summary>
    public static class AjustarCamaraT723
    {
        private const string RutaCamara = "Assets/_Proyecto/Personaje/Datos/ConfigCamaraHombro_Base.asset";
        public const float Pivote = 1.55f;
        public const float Brazo = 0.15f;
        public const float Distancia = 3.5f;
        public const float FovNormal = 60f;
        public const float FovApuntando = 40f;

        private const string RutaCamaraCarro = "Assets/_Proyecto/Vehiculos/Datos/ConfigCamaraCarro_Viejo.asset";
        public const float AlturaMiraCarro = 1.0f;

        [MenuItem("Mecha Cyberpunk/Ajustar T-723 cámara Cinemachine")]
        public static void Ajustar()
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(RutaCamara);
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("lateralNormal").floatValue = 0f;
            so.FindProperty("alturaNormal").floatValue = Pivote;
            so.FindProperty("brazoVertical").floatValue = Brazo;
            so.FindProperty("distanciaNormal").floatValue = Distancia;
            so.FindProperty("fovNormal").floatValue = FovNormal;
            so.FindProperty("lateralApuntando").floatValue = 0f;
            so.FindProperty("alturaApuntando").floatValue = Pivote;
            so.FindProperty("distanciaApuntando").floatValue = Distancia;
            so.FindProperty("fovApuntando").floatValue = FovApuntando;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            // Carro y moto: la cámara mira un poco por encima del vehículo para que quede abajo (CAM03_Carro.png).
            Object carro = AssetDatabase.LoadAssetAtPath<Object>(RutaCamaraCarro);
            SerializedObject soCarro = new SerializedObject(carro);
            soCarro.FindProperty("alturaMira").floatValue = AlturaMiraCarro;
            soCarro.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(carro);
            AssetDatabase.SaveAssets();
        }
    }
}

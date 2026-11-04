using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>T-585/T-586: cámara de hombro más pegada y con FOV más cerrado; más vida y regeneración más rápida.</summary>
    public static class AjustarT585
    {
        private const string RutaCamara = "Assets/_Proyecto/Personaje/Datos/ConfigCamaraHombro_Base.asset";
        private const string RutaDatos = "Assets/_Proyecto/Personaje/Datos/DatosPersonaje_Base.asset";

        [MenuItem("Mecha Cyberpunk/Ajustar T-585 cámara y vida")]
        public static void Ajustar()
        {
            Poner(RutaCamara, ("lateralNormal", 0.45f), ("alturaNormal", 1.6f), ("distanciaNormal", 1.9f), ("fovNormal", 50f),
                ("lateralApuntando", 0.6f), ("distanciaApuntando", 1.2f), ("fovApuntando", 40f));
            Poner(RutaDatos, ("vidaMaxima", 150f), ("regeneracionPorSegundo", 8f), ("segundosParaRegenerar", 4f));
            AssetDatabase.SaveAssets();
        }

        private static void Poner(string ruta, params (string campo, float valor)[] valores)
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(ruta);
            SerializedObject so = new SerializedObject(asset);
            foreach ((string campo, float valor) in valores)
            {
                SerializedProperty p = so.FindProperty(campo);
                if (p == null)
                {
                    Debug.LogError("[Personaje] T-585: no existe " + campo + " en " + ruta);
                    continue;
                }
                p.floatValue = valor;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }
    }
}

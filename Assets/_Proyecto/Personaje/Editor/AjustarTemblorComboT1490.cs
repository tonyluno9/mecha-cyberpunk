using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-1490: aplica a los assets existentes los valores del playtest 19-nov: casi sin temblor de cámara, combo de 2 golpes y
    /// puños más flojos contra enemigos. Se puede repetir.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.AjustarTemblorComboT1490.Ajustar
    /// </summary>
    public static class AjustarTemblorComboT1490
    {
        private const string RutaCamara = "Assets/_Proyecto/Personaje/Datos/ConfigCamaraHombro_Base.asset";
        private const string RutaGolpe = "Assets/_Proyecto/Personaje/Datos/ConfigGolpeAPie_Base.asset";
        private const string RutaCarro = "Assets/_Proyecto/Vehiculos/Datos/ConfigCamaraCarro_Viejo.asset";

        [MenuItem("Mecha Cyberpunk/Ajustar T-1490 temblor y combo")]
        public static void Ajustar()
        {
            Poner(RutaCamara, "gananciaTemblor", 0.18f);
            Poner(RutaCamara, "sacudidaZoom", 0.3f);
            Poner(RutaCamara, "balanceoVertical", 0.0175f);
            Poner(RutaCamara, "balanceoLateral", 0.01f);
            Poner(RutaCarro, "gananciaTemblor", 0.5f);
            Poner(RutaGolpe, "multiplicadorPunosContraEnemigos", 0.45f);
            Object golpe = AssetDatabase.LoadAssetAtPath<Object>(RutaGolpe);
            if (golpe != null)
            {
                SerializedObject so = new SerializedObject(golpe);
                so.FindProperty("golpesCombo").intValue = 2;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(golpe);
            }
            AssetDatabase.SaveAssets();
            AjustarSensacionT1486.Ajustar(); // patada de cámara por arma al 20 % (DatosSensacion.AjustesPorArma)
            Debug.Log("[Personaje] T-1490: temblor, combo y puños ajustados.");
        }

        private static void Poner(string ruta, string campo, float valor)
        {
            Object o = AssetDatabase.LoadAssetAtPath<Object>(ruta);
            if (o == null)
            {
                Debug.LogError("[Personaje] Falta " + ruta);
                return;
            }
            SerializedObject so = new SerializedObject(o);
            SerializedProperty p = so.FindProperty(campo);
            if (p == null)
            {
                Debug.LogError("[Personaje] Falta el campo " + campo + " en " + ruta);
                return;
            }
            p.floatValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(o);
        }
    }
}

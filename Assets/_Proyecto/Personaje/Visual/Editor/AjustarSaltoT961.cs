using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-961 (A2): el salto con los clips de Blender. Los clips de la muestra aprobada (BITACORA 03, ajuste 2) pasan 0.47 s en el aire y suben 0.45 m: el CharacterController del
    /// jugador salta 0.45 m con gravedad 16 m/s² (2·√(2·0.45/16) = 0.47 s en el aire). Rearma el controlador base para que el jugador use los PJ01_Loco_Salto* y PJ01_Loco_Caida.
    /// Se puede repetir. Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.AjustarSaltoT961.Aplicar
    /// </summary>
    public static class AjustarSaltoT961
    {
        public const string RutaDatosPersonaje = "Assets/_Proyecto/Personaje/Datos/DatosPersonaje_Base.asset";
        public const string RutaConfigControles = "Assets/_Proyecto/Personaje/Datos/ConfigControlesAPie_Base.asset";
        public const float AlturaSalto = 0.45f;
        public const float Gravedad = 16f;

        [MenuItem("Mecha Cyberpunk/T-961/Ajustar salto del jugador")]
        public static void Aplicar()
        {
            Poner(RutaDatosPersonaje, "alturaSalto", AlturaSalto);
            Poner(RutaConfigControles, "gravedad", Gravedad);
            ConstructorControladorBase.Construir();
            AssetDatabase.SaveAssets();
            Debug.Log("[Personaje] T-961: salto " + AlturaSalto + " m, gravedad " + Gravedad + " m/s².");
        }

        private static void Poner(string ruta, string campo, float valor)
        {
            Object datos = AssetDatabase.LoadAssetAtPath<Object>(ruta);
            var so = new SerializedObject(datos);
            so.FindProperty(campo).floatValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
        }
    }
}

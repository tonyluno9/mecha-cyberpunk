using MechaCyberpunk.IA.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// T-1009: los guardias y el Capitán de Nova se defienden al ser golpeados y son más fuertes (vida y daño altos).
    /// Se puede correr varias veces. Uso: Unity -batchmode -executeMethod MechaCyberpunk.IA.Editor.AjustarNovaT1009.Aplicar
    /// </summary>
    public static class AjustarNovaT1009
    {
        public const string RutaGuardia = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Nova.asset";
        public const string RutaCapitan = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_JefeNova.asset";

        private const float SegundosDefensa = 20f;
        private const float VidaGuardia = 300f;
        private const float DanioGolpeGuardia = 30f;
        private const float DanioDisparoGuardia = 20f;
        private const float VidaCapitan = 1200f;
        private const float DanioGolpeCapitan = 40f;
        private const float DanioCargaCapitan = 60f;

        public static void Aplicar()
        {
            Ajustar(RutaGuardia, new (string, float)[] { ("vidaMaxima", VidaGuardia), ("danioGolpe", DanioGolpeGuardia), ("danioDisparo", DanioDisparoGuardia) });
            Ajustar(RutaCapitan, new (string, float)[] { ("vidaMaxima", VidaCapitan), ("danioGolpeJefe", DanioGolpeCapitan), ("danioCarga", DanioCargaCapitan) });
            AssetDatabase.SaveAssets();
            Debug.Log("[IA] T-1009: guardias y Capitán de Nova se defienden y son fuertes.");
        }

        private static void Ajustar(string ruta, (string campo, float valor)[] valores)
        {
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(ruta);
            if (datos == null)
            {
                Debug.LogError("[IA] Falta " + ruta);
                return;
            }
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("seDefiende").boolValue = true;
            so.FindProperty("segundosDefensa").floatValue = SegundosDefensa;
            foreach ((string campo, float valor) in valores)
            {
                so.FindProperty(campo).floatValue = valor;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
        }
    }
}

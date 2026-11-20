using MechaCyberpunk.IA.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// T-1494: deja los datos de Nova listos. Guardias armados (Nova y Élite): reaccionan a disparos. Guardias y robots pesados: +40 % de vida. Perro robot (E17):
    /// conducta de perro, detección de 20 m y +33 % de vida. Se puede correr varias veces (pone valores absolutos).
    /// Uso: Unity -batchmode -quit -executeMethod MechaCyberpunk.IA.Editor.AplicarIaNovaT1494.Aplicar
    /// </summary>
    public static class AplicarIaNovaT1494
    {
        private const string Carpeta = "Assets/_Proyecto/IA/Datos/";
        public const float VidaGuardiaNova = 420f;     // antes 300
        public const float VidaGuardiaPesadoE12 = 308f; // antes 220
        public const float VidaRobotPesadoE16 = 364f;  // antes 260
        public const float VidaPerroE17 = 60f;         // antes 45
        public const float RadioDeteccionPerro = 20f;

        [MenuItem("Mecha Cyberpunk/T1494/Aplicar IA de Nova")]
        public static void Aplicar()
        {
            Poner("DatosPandilleroAPie_Nova", VidaGuardiaNova, true, false, 0f);
            Poner("DatosPandilleroAPie_EliteNovaE18", -1f, true, false, 0f); // la vida de la élite no se toca
            Poner("DatosPandilleroAPie_GuardiaPesadoE12", VidaGuardiaPesadoE12, false, false, 0f);
            Poner("DatosPandilleroAPie_RobotPesadoE16", VidaRobotPesadoE16, false, false, 0f);
            Poner("DatosPandilleroAPie_PerroRobotE17", VidaPerroE17, false, true, RadioDeteccionPerro);
            AssetDatabase.SaveAssets();
            Debug.Log("[T-1494] datos de Nova aplicados");
        }

        private static void Poner(string nombre, float vida, bool reacciona, bool perro, float radioDeteccion)
        {
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(Carpeta + nombre + ".asset");
            if (datos == null)
            {
                Debug.LogError("[T-1494] falta " + nombre);
                return;
            }
            SerializedObject so = new SerializedObject(datos);
            if (vida > 0f)
            {
                so.FindProperty("vidaMaxima").floatValue = vida;
            }
            so.FindProperty("reaccionaADisparos").boolValue = reacciona;
            so.FindProperty("perroRobot").boolValue = perro;
            if (radioDeteccion > 0f)
            {
                so.FindProperty("radioDeteccion").floatValue = radioDeteccion;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
        }
    }
}

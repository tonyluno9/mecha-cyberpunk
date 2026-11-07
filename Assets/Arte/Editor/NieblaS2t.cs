using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-860 (playtest 6-nov tarde): de noche la niebla tapaba la ciudad junto al callejón (densidad 0.02 exponencial al cuadrado: a 60 m ya
    /// tapa el 76 %). Baja las llaves de noche de la curva densidadNiebla de ConfigCicloDia_Mundo a <see cref="DensidadNoche"/> (a 100 m deja ver
    /// el 44 %); el día no cambia. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.NieblaS2t.Aplicar
    /// </summary>
    public static class NieblaS2t
    {
        public const string RutaConfig = "Assets/_Proyecto/Mundo/Datos/ConfigCicloDia_Mundo.asset";
        public const float DensidadNoche = 0.009f;
        /// <summary>Grupos de planos de niebla que se apagan (el de Neón Alto se queda: es parte de su ambiente).</summary>
        public static readonly string[] CortinasApagadas = { "T502_Callejon/Niebla", "T501_Suburbios/Niebla" };
        /// <summary>Llaves que eran de noche (valían 0.02).</summary>
        private const float DensidadNocheVieja = 0.02f;

        public static void FotoAntes() => Foto("antes");

        public static void FotoDespues() => Foto("despues");

        private static void Foto(string cuando)
        {
            ColocarS2.Abrir();
            ColocarS2t.FotoHora(22f, new Vector3(-58f, 1.7f, -60f), new Vector3(-120f, 8f, -55f), ColocarS2t.CarpetaCapturas + "s2t-niebla-callejon-" + cuando + ".png", 70f);
            ColocarS2t.FotoHora(22f, new Vector3(-47f, 1.7f, -70f), new Vector3(-47f, 10f, 40f), ColocarS2t.CarpetaCapturas + "s2t-niebla-calle-" + cuando + ".png", 70f);
        }

        [MenuItem("Mecha Cyberpunk/S2t/8 Menos niebla de noche")]
        public static void Aplicar()
        {
            Object config = AssetDatabase.LoadMainAssetAtPath(RutaConfig);
            SerializedObject so = new SerializedObject(config);
            SerializedProperty curva = so.FindProperty("densidadNiebla");
            AnimationCurve c = curva.animationCurveValue;
            Keyframe[] llaves = c.keys;
            int cambiadas = 0;
            for (int i = 0; i < llaves.Length; i++)
            {
                if (llaves[i].value > DensidadNoche + 0.0005f && Mathf.Abs(llaves[i].value - DensidadNocheVieja) < 0.001f)
                {
                    llaves[i].value = DensidadNoche;
                    cambiadas++;
                }
            }
            c.keys = llaves;
            curva.animationCurveValue = c;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log("[Arte] T-860: " + cambiadas + " llaves de niebla de noche a " + DensidadNoche);

            // Las cortinas de niebla (planos) atravesadas en el callejón y en la calle de Suburbios tapaban la ciudad de atrás.
            ColocarS2.Abrir();
            int apagadas = 0;
            foreach (string r in CortinasApagadas)
            {
                Transform t = ColocarS2.Buscar(r);
                if (t != null && t.gameObject.activeSelf)
                {
                    t.gameObject.SetActive(false);
                    apagadas++;
                }
            }
            ColocarS2.Guardar("s2t-niebla");
            Debug.Log("[Arte] T-860: " + apagadas + " grupos de cortinas de niebla apagados");
        }
    }
}

using MechaCyberpunk.Progresion.Apoyo;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-813 (playtest del 6-nov): la silla del Doc. Pone una <see cref="SillaClinica"/> en el sillón I07 del consultorio (S2_Interiores/ConsultorioDoc,
    /// de ColocarS2Interiores: caja x -104..-96, z -16..-10.2) con el brazo robótico I06 que cuelga sobre él. El asiento queda en el centro del
    /// sillón mirando a la calle (+Z); el jugador se para y se levanta del lado del cuarto (+X). Antes arma ConfigSillaClinica_Base. Idempotente:
    /// rehace su raíz S2n_SillaClinica.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2nSillaClinica.Colocar
    /// (Solo datos, sin abrir la escena: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2nSillaClinica.PrepararDatos)
    /// </summary>
    public static class ColocarS2nSillaClinica
    {
        public const string NombreRaiz = "S2n_SillaClinica";
        public const string RutaConsultorio = "S2_Interiores/ConsultorioDoc";
        public const string RutaConfig = "Assets/_Proyecto/Progresion/Apoyo/Datos/ConfigSillaClinica_Base.asset";
        /// <summary>Centro del sillón de ColocarS2Interiores.Consultorio, por si el modelo no está.</summary>
        public static readonly Vector3 CentroSillon = new Vector3(-102.1f, 0.22f, -13.4f);
        private const float AlturaAsiento = 0.55f;
        private const float LadoSalida = 0.55f;
        // La cadera va un poco hacia el respaldo (la pared del fondo, -Z): las piernas quedan sobre el sillón.
        private const float HaciaRespaldo = 0.3f;

        [MenuItem("Mecha Cyberpunk/S2n/Silla del Doc (T-813)")]
        public static void Colocar()
        {
            ConfigSillaClinica config = PrepararDatos();
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Object.DestroyImmediate(GameObject.Find(NombreRaiz));

            Transform consultorio = ColocarS2.Buscar(RutaConsultorio);
            Transform sillon = consultorio != null ? consultorio.Find("I07") : null;
            Transform brazo = consultorio != null ? consultorio.Find("I06") : null;
            Bounds limites = sillon != null ? ArteT250.Limites(sillon.gameObject) : new Bounds(CentroSillon + Vector3.up * 0.5f, new Vector3(0.8f, 1f, 2f));
            float piso = limites.min.y;

            GameObject raiz = new GameObject(NombreRaiz);
            // El punto de uso (E) va al lado del sillón, del lado del cuarto.
            raiz.transform.position = new Vector3(limites.max.x + LadoSalida * 0.5f, piso, limites.center.z);
            Transform asiento = new GameObject("Asiento").transform;
            asiento.SetParent(raiz.transform, false);
            asiento.SetPositionAndRotation(new Vector3(limites.center.x, piso + Mathf.Min(AlturaAsiento, limites.size.y * 0.6f), limites.center.z - HaciaRespaldo),
                Quaternion.LookRotation(Vector3.forward));
            Transform salida = new GameObject("Salida").transform;
            salida.SetParent(raiz.transform, false);
            salida.SetPositionAndRotation(new Vector3(limites.max.x + LadoSalida, piso, limites.center.z), Quaternion.LookRotation(Vector3.forward));

            SillaClinica silla = raiz.AddComponent<SillaClinica>();
            SerializedObject so = new SerializedObject(silla);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("asiento").objectReferenceValue = asiento;
            so.FindProperty("salida").objectReferenceValue = salida;
            so.FindProperty("brazo").objectReferenceValue = brazo;
            so.ApplyModifiedPropertiesWithoutUndo();
            ColocarS2.Informe.AppendLine("Silla del Doc en " + asiento.position.ToString("F2")
                + (sillon != null ? " sobre " + ColocarS2.Ruta(sillon) : " (FALTA el sillón I07: posición fija)")
                + (brazo != null ? ", brazo " + ColocarS2.Ruta(brazo) : ", sin brazo I06"));
            ColocarS2.Guardar("s2n-silla-clinica");
        }

        /// <summary>Lo que no toca la escena: la config de la silla.</summary>
        public static ConfigSillaClinica PrepararDatos()
        {
            ConfigSillaClinica config = AssetDatabase.LoadAssetAtPath<ConfigSillaClinica>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigSillaClinica>();
                AssetDatabase.CreateAsset(config, RutaConfig);
                AssetDatabase.SaveAssets();
            }
            return config;
        }
    }
}

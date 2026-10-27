using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Misiones;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Pelea.Editor
{
    /// <summary>
    /// Crea lo de T-185: las tres emboscadas de misión (callejón, baldío y camión), el escolta del camión (el pandillero con +20 % de
    /// vida), ConfigPeleasMision_Base y PeleasDeMision.prefab. Idempotente: vuelve a poner los mismos valores sin duplicar nada.
    /// Batchmode: -executeMethod MechaCyberpunk.Pelea.Editor.CreadorPeleasMision.Crear
    /// </summary>
    public static class CreadorPeleasMision
    {
        public const string IdCallejon = "barrio_callejon";
        public const string IdLimpieza = "corp_limpieza";
        public const string IdPeleaSinJaula = "ilegal_pelea";
        public const string IdCamion = "robo_camion";
        public const float FactorVidaEscolta = 1.2f;

        private const string CarpetaDatos = "Assets/_Proyecto/Pelea/Datos/";
        private const string CarpetaPrefabs = "Assets/_Proyecto/Pelea/Prefabs/";
        private const string RutaRivalPandilla = "Assets/_Proyecto/IA/Prefabs/RivalPandilla.prefab";
        private const string RutaMechaPandilla = "Assets/_Proyecto/IA/Datos/DatosMecha_Pandilla.asset";
        private const string RutaRivalDatosPandilla = "Assets/_Proyecto/IA/Datos/DatosRival_Pandilla.asset";
        private const string RutaMechaEscolta = CarpetaDatos + "DatosMecha_Escolta.asset";
        private const string RutaRivalDatosEscolta = CarpetaDatos + "DatosRival_Escolta.asset";
        private const string RutaRivalEscolta = CarpetaPrefabs + "RivalEscolta.prefab";
        private const string RutaConfig = CarpetaDatos + "ConfigPeleasMision_Base.asset";
        private const string RutaPrefab = CarpetaPrefabs + "PeleasDeMision.prefab";
        private const string NombreEscolta = "ESCOLTA";
        private const float AlturaRival = 0.05f;
        private const float GiroHaciaLaEntrada = 180f;
        private const float RadioActivacion = 12f;
        private const float SegundosAparicion = 2f;
        private const float SegundosRetiroCuerpo = 4f;
        private const float MetrosMechaLlegado = 6f;

        // Dentro del callejón: sus muros van de 2 a 7 m desde el punto (+Z hacia el fondo).
        private static readonly Vector2[] PosesCallejon = { new Vector2(-1.5f, 5f), new Vector2(1.5f, 5f) };
        private static readonly Vector2[] PosesBaldio = { new Vector2(-2f, 4.5f), new Vector2(2f, 4.5f), new Vector2(0f, 6.5f) };
        private static readonly Vector2[] PosesCamion = { new Vector2(-1.5f, 5f), new Vector2(1.5f, 5f) };

        [MenuItem("Mecha Cyberpunk/Crear T-185 peleas de misión")]
        public static void Crear()
        {
            GameObject rivalPandilla = AssetDatabase.LoadAssetAtPath<GameObject>(RutaRivalPandilla);
            DatosMecha mechaPandilla = AssetDatabase.LoadAssetAtPath<DatosMecha>(RutaMechaPandilla);
            DatosRival rivalDatosPandilla = AssetDatabase.LoadAssetAtPath<DatosRival>(RutaRivalDatosPandilla);
            if (rivalPandilla == null || mechaPandilla == null || rivalDatosPandilla == null)
            {
                Debug.LogError("[Pelea] CreadorPeleasMision: falta el pandillero (RivalPandilla, DatosMecha_Pandilla o DatosRival_Pandilla).");
                return;
            }

            GameObject escolta = CrearEscolta(rivalPandilla, mechaPandilla, rivalDatosPandilla);
            DatosEmboscada callejon = CrearDatos("Callejon", rivalPandilla, PosesCallejon);
            DatosEmboscada baldio = CrearDatos("Baldio", rivalPandilla, PosesBaldio);
            DatosEmboscada camion = CrearDatos("Camion", escolta, PosesCamion);
            ConfigPeleasMision config = CrearConfig(callejon, baldio, camion);
            CrearPrefab(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[Pelea] T-185 creada.");
        }

        private static GameObject CrearEscolta(GameObject rivalPandilla, DatosMecha mechaPandilla, DatosRival rivalDatosPandilla)
        {
            DatosMecha mecha = AssetDatabase.LoadAssetAtPath<DatosMecha>(RutaMechaEscolta);
            if (mecha == null)
            {
                mecha = Object.Instantiate(mechaPandilla);
                AssetDatabase.CreateAsset(mecha, RutaMechaEscolta);
            }
            SerializedObject soMecha = new SerializedObject(mecha);
            soMecha.FindProperty("nombre").stringValue = NombreEscolta;
            soMecha.FindProperty("vidaMaxima").floatValue = mechaPandilla.VidaMaxima * FactorVidaEscolta;
            soMecha.ApplyModifiedPropertiesWithoutUndo();

            DatosRival rival = AssetDatabase.LoadAssetAtPath<DatosRival>(RutaRivalDatosEscolta);
            if (rival == null)
            {
                rival = Object.Instantiate(rivalDatosPandilla);
                AssetDatabase.CreateAsset(rival, RutaRivalDatosEscolta);
            }
            SerializedObject soRival = new SerializedObject(rival);
            soRival.FindProperty("alias").stringValue = NombreEscolta;
            soRival.FindProperty("mecha").objectReferenceValue = mecha;
            soRival.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mecha);
            EditorUtility.SetDirty(rival);
            AssetDatabase.SaveAssets();

            GameObject existente = AssetDatabase.LoadAssetAtPath<GameObject>(RutaRivalEscolta);
            if (existente != null) return existente;

            // Variante del pandillero: cambia los dos datos que apuntan al pandillero por los del escolta.
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(rivalPandilla);
            try
            {
                instancia.name = "RivalEscolta";
                int cambios = 0;
                foreach (MonoBehaviour componente in instancia.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (componente == null) continue;
                    SerializedObject so = new SerializedObject(componente);
                    SerializedProperty propiedad = so.FindProperty("datos");
                    if (propiedad == null || propiedad.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (propiedad.objectReferenceValue == mechaPandilla)
                    {
                        propiedad.objectReferenceValue = mecha;
                        cambios++;
                    }
                    else if (propiedad.objectReferenceValue == rivalDatosPandilla)
                    {
                        propiedad.objectReferenceValue = rival;
                        cambios++;
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                if (cambios != 2)
                {
                    Debug.LogError("[Pelea] CreadorPeleasMision: se esperaban 2 datos del pandillero en el rival y hubo " + cambios + ".");
                }
                return PrefabUtility.SaveAsPrefabAsset(instancia, RutaRivalEscolta);
            }
            finally
            {
                Object.DestroyImmediate(instancia);
            }
        }

        private static DatosEmboscada CrearDatos(string nombre, GameObject prefabRival, Vector2[] poses)
        {
            string ruta = CarpetaDatos + "DatosEmboscada_Mision_" + nombre + ".asset";
            DatosEmboscada datos = AssetDatabase.LoadAssetAtPath<DatosEmboscada>(ruta);
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<DatosEmboscada>();
                AssetDatabase.CreateAsset(datos, ruta);
            }
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("prefabRival").objectReferenceValue = prefabRival;
            SerializedProperty lista = so.FindProperty("poses");
            lista.arraySize = poses.Length;
            for (int i = 0; i < poses.Length; i++)
            {
                SerializedProperty pose = lista.GetArrayElementAtIndex(i);
                pose.FindPropertyRelative("posicion").vector3Value = new Vector3(poses[i].x, AlturaRival, poses[i].y);
                pose.FindPropertyRelative("giroY").floatValue = GiroHaciaLaEntrada;
            }
            so.FindProperty("radioActivacion").floatValue = RadioActivacion;
            so.FindProperty("segundosAparicion").floatValue = SegundosAparicion;
            so.FindProperty("segundosRetiroCuerpo").floatValue = SegundosRetiroCuerpo;
            so.FindProperty("metrosMechaLlegado").floatValue = MetrosMechaLlegado;
            so.FindProperty("fijaObjetivos").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<DatosEmboscada>(ruta);
        }

        private static ConfigPeleasMision CrearConfig(DatosEmboscada callejon, DatosEmboscada baldio, DatosEmboscada camion)
        {
            ConfigPeleasMision config = AssetDatabase.LoadAssetAtPath<ConfigPeleasMision>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigPeleasMision>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            var tabla = new List<(string, DatosEmboscada)>
            {
                (IdCallejon, callejon), (IdLimpieza, baldio), (IdPeleaSinJaula, baldio), (IdCamion, camion)
            };
            SerializedObject so = new SerializedObject(config);
            SerializedProperty lista = so.FindProperty("entradas");
            lista.arraySize = tabla.Count;
            for (int i = 0; i < tabla.Count; i++)
            {
                SerializedProperty fila = lista.GetArrayElementAtIndex(i);
                fila.FindPropertyRelative("idMision").stringValue = tabla[i].Item1;
                fila.FindPropertyRelative("datos").objectReferenceValue = tabla[i].Item2;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<ConfigPeleasMision>(RutaConfig);
        }

        private static void CrearPrefab(ConfigPeleasMision config)
        {
            GameObject existente = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            if (existente != null)
            {
                PeleaDeMision actual = existente.GetComponent<PeleaDeMision>();
                if (actual != null && new SerializedObject(actual).FindProperty("config").objectReferenceValue == config) return;
            }
            GameObject raiz = new GameObject("PeleasDeMision");
            try
            {
                PeleaDeMision pelea = raiz.AddComponent<PeleaDeMision>();
                SerializedObject so = new SerializedObject(pelea);
                so.FindProperty("config").objectReferenceValue = config;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }
    }
}

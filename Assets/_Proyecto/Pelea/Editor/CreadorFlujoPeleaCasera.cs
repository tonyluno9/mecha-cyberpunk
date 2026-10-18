using MechaCyberpunk.Economia;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Flujo;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.Pelea.Editor
{
    /// <summary>
    /// Crea lo de T-129 (y la apuesta de T-144): ConfigPresentador_Casera (dos líneas) y FlujoPeleaCasera.prefab (FlujoPelea con RivalCasero, premio de la
    /// casera, sin revancha y sin empezar solo). Idempotente. Necesita RivalCasero.prefab (CreadorRivalCasero.Crear).
    /// Batchmode: -executeMethod MechaCyberpunk.Pelea.Editor.CreadorFlujoPeleaCasera.Crear
    /// </summary>
    public static class CreadorFlujoPeleaCasera
    {
        public const string RutaPrefab = "Assets/_Proyecto/Pelea/Prefabs/FlujoPeleaCasera.prefab";

        private const string RutaApuestaCasera = "Assets/_Proyecto/Economia/Datos/DatosApuestaPelea_Casera.asset";
        private const string RutaConfig = "Assets/_Proyecto/Pelea/Datos/ConfigPresentador_Casera.asset";
        private const string RutaPrefabBase = "Assets/_Proyecto/Pelea/Prefabs/FlujoPelea.prefab";
        private const string RutaRivalCasero = "Assets/_Proyecto/IA/Prefabs/RivalCasero.prefab";
        private const string LineaLugar = "PELEA CASERA EN EL SÓTANO";
        private const string LineaGrito = "¡{alias} CONTRA {rival}!";
        private const int IndiceGrito = 1;
        private const float FraccionBajaApuesta = 0.02f;
        private const float FraccionAltaApuesta = 0.17f;
        private const float FraccionBajaJefe = 0.57f;
        private const float FraccionAltaJefe = 0.74f;

        // T-145: la línea "APUESTA +$500" de la pantalla de la arena, bajo el premio (en la franja de la revancha, que la casera no usa).
        private static void AgregarLineaApuesta(GameObject raiz)
        {
            PantallaPelea pantalla = raiz.GetComponentInChildren<PantallaPelea>(true);
            SerializedObject soPantalla = new SerializedObject(pantalla);
            SerializedProperty propiedad = soPantalla.FindProperty("textoApuesta");
            if (propiedad.objectReferenceValue != null)
            {
                return;
            }
            Text texto = CreadorFlujoPelea.CrearTexto("Apuesta", (RectTransform)pantalla.transform, new Vector2(0f, FraccionBajaApuesta),
                new Vector2(1f, FraccionAltaApuesta));
            propiedad.objectReferenceValue = texto;
            soPantalla.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>T-161: agrega el letrero de jefe a la pantalla de un flujo abierto (idempotente). Franja bajo el texto grande, sobre el premio.</summary>
        public static void AgregarLetreroJefe(GameObject raiz)
        {
            PantallaPelea pantalla = raiz.GetComponentInChildren<PantallaPelea>(true);
            SerializedObject soPantalla = new SerializedObject(pantalla);
            SerializedProperty propiedad = soPantalla.FindProperty("textoJefe");
            if (propiedad.objectReferenceValue != null)
            {
                return;
            }
            Text texto = CreadorFlujoPelea.CrearTexto("Jefe", (RectTransform)pantalla.transform, new Vector2(0f, FraccionBajaJefe),
                new Vector2(1f, FraccionAltaJefe));
            propiedad.objectReferenceValue = texto;
            soPantalla.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("Mecha Cyberpunk/Crear T-129 flujo casero")]
        public static void Crear()
        {
            ConfigPresentador config = AssetDatabase.LoadAssetAtPath<ConfigPresentador>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigPresentador>();
                AssetDatabase.CreateAsset(config, RutaConfig);
                SerializedObject soConfig = new SerializedObject(config);
                SerializedProperty lineas = soConfig.FindProperty("lineas");
                lineas.arraySize = 2;
                lineas.GetArrayElementAtIndex(0).stringValue = LineaLugar;
                lineas.GetArrayElementAtIndex(1).stringValue = LineaGrito;
                soConfig.FindProperty("indiceGrito").intValue = IndiceGrito;
                soConfig.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                config = AssetDatabase.LoadAssetAtPath<ConfigPresentador>(RutaConfig);
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) == null)
            {
                AssetDatabase.CopyAsset(RutaPrefabBase, RutaPrefab);
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                raiz.name = "FlujoPeleaCasera";
                SerializedObject so = new SerializedObject(raiz.GetComponent<DirectorPelea>());
                so.FindProperty("prefabRival").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(RutaRivalCasero);
                so.FindProperty("presentador").objectReferenceValue = config;
                so.FindProperty("empezarAlArrancar").boolValue = false;
                so.FindProperty("permitirRevancha").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
                so.FindProperty("apuesta").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosApuestaPelea>(RutaApuestaCasera);
                so.ApplyModifiedPropertiesWithoutUndo();
                CreadorPremiosPelea.Aplicar(raiz, CreadorPremiosPelea.RutaPremioCasera);
                AgregarLineaApuesta(raiz);
                AgregarLetreroJefe(raiz);
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Pelea] T-129 flujo casero creado.");
        }
    }
}

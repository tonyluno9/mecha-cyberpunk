using System.Collections.Generic;
using MechaCyberpunk.Combate.Bloqueo;
using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.IA.Adaptacion;
using MechaCyberpunk.IA.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// Crea los datos de adaptación de T-199: DatosAdaptacionRival_Base (nivelMaximo 1) y _Avanzado (nivelMaximo 2) con los valores de
    /// D-tony-193 a D-tony-196. Idempotente: un asset que ya existe no se toca, así los valores que Tony cambie se quedan.
    /// T-200: también agrega AdaptacionRival a los 20 prefabs de liga (ligas 1 y 2 con _Base, 3 y 4 con _Avanzado) y a RivalCasero (_Base),
    /// y MemoriaRivales a ServiciosProgresion. Se corre después de CreadorRivalesLiga y CreadorHabilidadesJefes. No toca RivalBasico,
    /// RivalTutorial ni RivalPandilla. Un prefab que ya tiene el componente no se vuelve a guardar.
    /// T-201: a esos mismos prefabs les agrega GuardiaMecha (mando por código, sin bloqueo perfecto) y GuardiaRival, las dos en los
    /// controlados de su PeleadorMecha, y deja en ConfigPresentacionRival_Base el color y el ángulo de la guardia.
    /// Batchmode: -executeMethod MechaCyberpunk.IA.Editor.CreadorAdaptacionRival.Crear
    /// </summary>
    public static class CreadorAdaptacionRival
    {
        public const string RutaBase = "Assets/_Proyecto/IA/Datos/DatosAdaptacionRival_Base.asset";
        public const string RutaAvanzado = "Assets/_Proyecto/IA/Datos/DatosAdaptacionRival_Avanzado.asset";

        public const string RutaRivalCasero = "Assets/_Proyecto/IA/Prefabs/RivalCasero.prefab";
        public const string RutaServicios = "Assets/_Proyecto/Progresion/Prefabs/ServiciosProgresion.prefab";
        private const int LigasConNivelUno = 2;
        public const string RutaConfigPresentacion = "Assets/_Proyecto/IA/Datos/ConfigPresentacionRival_Base.asset";
        private static readonly Color ColorGuardia = new Color(0.2f, 0.45f, 1f);
        private const float AnguloBrazoGuardia = 45f;

        [MenuItem("Mecha Cyberpunk/Crear T-199 adaptación")]
        public static void Crear()
        {
            CrearAsset(RutaBase, 1);
            CrearAsset(RutaAvanzado, 2);
            AssetDatabase.SaveAssets();
            Debug.Log("[IA] T-199 datos de adaptación creados.");
            CrearPrefabs();
        }

        /// <summary>T-200: agrega AdaptacionRival a los rivales de liga y al casero, y MemoriaRivales a los servicios.</summary>
        public static void CrearPrefabs()
        {
            DatosAdaptacionRival nivelUno = AssetDatabase.LoadAssetAtPath<DatosAdaptacionRival>(RutaBase);
            DatosAdaptacionRival nivelDos = AssetDatabase.LoadAssetAtPath<DatosAdaptacionRival>(RutaAvanzado);
            for (int liga = 1; liga <= CreadorRivalesLiga.Ligas; liga++)
            {
                for (int n = 1; n <= CreadorRivalesLiga.RivalesPorLiga; n++)
                {
                    PonerAdaptacion(CreadorRivalesLiga.RutaPrefab(liga, n), liga <= LigasConNivelUno ? nivelUno : nivelDos);
                }
            }
            PonerAdaptacion(RutaRivalCasero, nivelUno);
            PonerMemoria();
            AssetDatabase.SaveAssets();
            Debug.Log("[IA] T-200 adaptación puesta en los rivales de liga, el casero y los servicios.");
            CrearGuardia();
        }

        /// <summary>T-201: GuardiaMecha y GuardiaRival en los rivales que se adaptan; color y ángulo de guardia en la config. Idempotente.</summary>
        public static void CrearGuardia()
        {
            PonerConfigGuardia();
            for (int liga = 1; liga <= CreadorRivalesLiga.Ligas; liga++)
            {
                for (int n = 1; n <= CreadorRivalesLiga.RivalesPorLiga; n++)
                {
                    PonerGuardia(CreadorRivalesLiga.RutaPrefab(liga, n));
                }
            }
            PonerGuardia(RutaRivalCasero);
            AssetDatabase.SaveAssets();
            Debug.Log("[IA] T-201 guardia puesta en los rivales que se adaptan.");
        }

        // Solo escribe los dos campos de la guardia: lo demás de la config no se toca.
        private static void PonerConfigGuardia()
        {
            ConfigPresentacionRival config = AssetDatabase.LoadAssetAtPath<ConfigPresentacionRival>(RutaConfigPresentacion);
            if (config == null)
            {
                Debug.LogError("[IA] Falta " + RutaConfigPresentacion + ": corre antes CreadorRivalBasico.");
                return;
            }
            SerializedObject so = new SerializedObject(config);
            so.FindProperty("colorGuardia").colorValue = ColorGuardia;
            so.FindProperty("anguloBrazoGuardia").floatValue = AnguloBrazoGuardia;
            so.ApplyModifiedPropertiesWithoutUndo();
            // Aunque el valor sea el de la clase, se marca para que el asset guarde los campos nuevos.
            EditorUtility.SetDirty(config);
        }

        private static void PonerGuardia(string rutaPrefab)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab);
            if (prefab == null || prefab.GetComponent<AdaptacionRival>() == null)
            {
                Debug.LogError("[IA] " + rutaPrefab + " no tiene AdaptacionRival: corre antes CrearPrefabs.");
                return;
            }
            if (prefab.GetComponent<GuardiaRival>() != null && prefab.GetComponent<GuardiaMecha>() != null)
            {
                return;
            }
            GameObject contenido = PrefabUtility.LoadPrefabContents(rutaPrefab);
            try
            {
                GuardiaMecha guardia = contenido.GetComponent<GuardiaMecha>();
                if (guardia == null)
                {
                    guardia = contenido.AddComponent<GuardiaMecha>();
                }
                SerializedObject soGuardia = new SerializedObject(guardia);
                soGuardia.FindProperty("mandoPorCodigo").boolValue = true;
                soGuardia.FindProperty("permitePerfecto").boolValue = false;
                soGuardia.ApplyModifiedPropertiesWithoutUndo();
                GuardiaRival guardiaRival = contenido.GetComponent<GuardiaRival>();
                if (guardiaRival == null)
                {
                    guardiaRival = contenido.AddComponent<GuardiaRival>();
                }
                SerializedObject soPeleador = new SerializedObject(contenido.GetComponent<PeleadorMecha>());
                SerializedProperty controlados = soPeleador.FindProperty("controlados");
                AgregarControlado(controlados, guardia);
                AgregarControlado(controlados, guardiaRival);
                soPeleador.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contenido, rutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        private static void AgregarControlado(SerializedProperty lista, Behaviour componente)
        {
            for (int i = 0; i < lista.arraySize; i++)
            {
                if (lista.GetArrayElementAtIndex(i).objectReferenceValue == componente)
                {
                    return;
                }
            }
            lista.arraySize++;
            lista.GetArrayElementAtIndex(lista.arraySize - 1).objectReferenceValue = componente;
        }

        private static void PonerAdaptacion(string rutaPrefab, DatosAdaptacionRival datos)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab);
            if (prefab == null || datos == null)
            {
                Debug.LogError("[IA] Falta " + (prefab == null ? rutaPrefab : "el DatosAdaptacionRival") + ": corre antes CreadorRivalesLiga.");
                return;
            }
            if (prefab.GetComponent<AdaptacionRival>() != null)
            {
                return;
            }
            GameObject contenido = PrefabUtility.LoadPrefabContents(rutaPrefab);
            try
            {
                AdaptacionRival adaptacion = contenido.AddComponent<AdaptacionRival>();
                SerializedObject so = new SerializedObject(adaptacion);
                so.FindProperty("datos").objectReferenceValue = datos;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contenido, rutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        private static void PonerMemoria()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaServicios);
            if (prefab == null)
            {
                Debug.LogError("[IA] Falta " + RutaServicios);
                return;
            }
            if (prefab.GetComponent<MemoriaRivales>() != null)
            {
                return;
            }
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaServicios);
            try
            {
                contenido.AddComponent<MemoriaRivales>();
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaServicios);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        private static void CrearAsset(string ruta, int nivelMaximo)
        {
            if (AssetDatabase.LoadAssetAtPath<DatosAdaptacionRival>(ruta) != null)
            {
                return;
            }
            DatosAdaptacionRival datos = ScriptableObject.CreateInstance<DatosAdaptacionRival>();
            AssetDatabase.CreateAsset(datos, ruta);
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("nivelMaximo").intValue = nivelMaximo;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
        }
    }
}

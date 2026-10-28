using System.IO;
using MechaCyberpunk.Casino.Cajera;
using MechaCyberpunk.Casino.Fichas;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Casino.Editor
{
    /// <summary>
    /// Crea los datos de cambio (DatosFichas_Base.asset, sin pisar uno ajustado), Prefabs/ServicioFichas.prefab y Prefabs/Cajera.prefab (T-302).
    /// Los prefabs se reconstruyen cada vez y conservan su GUID.
    /// Batchmode: -executeMethod MechaCyberpunk.Casino.Editor.CreadorFichas.Crear
    /// </summary>
    public static class CreadorFichas
    {
        public const string RutaDatos = "Assets/_Proyecto/Casino/Datos/DatosFichas_Base.asset";
        public const string RutaServicio = "Assets/_Proyecto/Casino/Prefabs/ServicioFichas.prefab";
        public const string RutaCajera = "Assets/_Proyecto/Casino/Prefabs/Cajera.prefab";
        private const string RutaMateriales = "Assets/_Proyecto/Casino/Materiales/";
        private const int DineroPorLote = 100;
        private const int FichasPorLote = 100;

        private static readonly Vector3 TamanioMostrador = new Vector3(2f, 1.05f, 0.6f);
        private static readonly Vector3 TamanioCuerpo = new Vector3(0.5f, 0.8f, 0.3f);
        private static readonly Vector3 TamanioCabeza = new Vector3(0.28f, 0.28f, 0.28f);
        private static readonly Vector3 TamanioLetrero = new Vector3(1.2f, 0.12f, 0.04f);
        private const float AlturaCuerpo = 1.05f;
        private const float DetrasMostrador = -0.7f;

        [MenuItem("Mecha Cyberpunk/Crear T-302")]
        public static void Crear()
        {
            DatosFichas datos = AssetDatabase.LoadAssetAtPath<DatosFichas>(RutaDatos);
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<DatosFichas>();
                datos.Fijar(DineroPorLote, FichasPorLote);
                AssetDatabase.CreateAsset(datos, RutaDatos);
                AssetDatabase.SaveAssets();
            }
            CrearServicio(datos);
            CrearCajera();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Casino] T-302 creada: " + RutaServicio + " y " + RutaCajera);
        }

        private static void CrearServicio(DatosFichas datos)
        {
            GameObject raiz = new GameObject("ServicioFichas");
            try
            {
                ServicioFichas servicio = raiz.AddComponent<ServicioFichas>();
                SerializedObject so = new SerializedObject(servicio);
                so.FindProperty("datos").objectReferenceValue = datos;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaServicio);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static void CrearCajera()
        {
            Material cuerpo = AssetDatabase.LoadAssetAtPath<Material>(RutaMateriales + "Tragamonedas_Cuerpo.mat");
            Material corona = AssetDatabase.LoadAssetAtPath<Material>(RutaMateriales + "Tragamonedas_Corona.mat");
            Material neon = AssetDatabase.LoadAssetAtPath<Material>(RutaMateriales + "Tragamonedas_Neon.mat");
            if (cuerpo == null || corona == null || neon == null)
            {
                Debug.LogError("[Casino] Faltan los materiales de la tragamonedas; corre CreadorTragamonedas.Crear primero.");
                return;
            }
            GameObject raiz = new GameObject("Cajera");
            try
            {
                // El frente (donde se para el jugador) mira a +Z; la cajera está detrás del mostrador, en -Z.
                Pieza(raiz.transform, "Mostrador", new Vector3(0f, TamanioMostrador.y * 0.5f, 0f), TamanioMostrador, cuerpo);
                Pieza(raiz.transform, "TapaMostrador", new Vector3(0f, TamanioMostrador.y + 0.03f, 0f), new Vector3(TamanioMostrador.x + 0.1f, 0.06f, TamanioMostrador.z + 0.1f), corona);
                Pieza(raiz.transform, "Cuerpo", new Vector3(0f, AlturaCuerpo + TamanioCuerpo.y * 0.5f - 0.1f, DetrasMostrador), TamanioCuerpo, corona);
                Pieza(raiz.transform, "Cabeza", new Vector3(0f, AlturaCuerpo + TamanioCuerpo.y + TamanioCabeza.y * 0.5f - 0.1f, DetrasMostrador), TamanioCabeza, cuerpo);
                Pieza(raiz.transform, "Letrero", new Vector3(0f, AlturaCuerpo + TamanioCuerpo.y + TamanioCabeza.y + 0.25f, DetrasMostrador), TamanioLetrero, neon);

                raiz.AddComponent<CajeraMundo>();
                Directory.CreateDirectory(Path.GetFullPath(Path.GetDirectoryName(RutaCajera)));
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaCajera);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static void Pieza(Transform padre, string nombre, Vector3 posicion, Vector3 escala, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posicion;
            go.transform.localScale = escala;
            Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}

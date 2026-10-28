using System.IO;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// Crea lo de T-195: DatosPandilleroAPie_Base.asset y los prefabs PandilleroAPie y PandillaAPie (grupo de 3 alrededor de su punto).
    /// Idempotente: lo que ya existe no se vuelve a crear (los fileID no cambian).
    /// Batchmode: -executeMethod MechaCyberpunk.IA.Editor.CreadorPandillaAPie.Crear
    /// </summary>
    public static class CreadorPandillaAPie
    {
        public const string RutaPrefabPandilla = RutaIA + "/Prefabs/PandillaAPie.prefab";

        private const string RutaIA = "Assets/_Proyecto/IA";
        private const string RutaDatos = RutaIA + "/Datos/DatosPandilleroAPie_Base.asset";
        private const string RutaPrefabPandillero = RutaIA + "/Prefabs/PandilleroAPie.prefab";
        private const string RutaMaterialCuerpo = RutaIA + "/Materiales/Rival_Cuerpo.mat";
        private const string RutaMaterialBrazo = RutaIA + "/Materiales/Rival_Brazo.mat";

        private const float AlturaCuerpo = 1.8f;
        private const float RadioCuerpo = 0.4f;
        private const float AlturaCabeza = 1.85f;
        private const float LadoCabeza = 0.4f;
        private const float AlturaHombro = 1.45f;
        private const float SeparacionHombro = 0.5f;
        private static readonly Vector3 TamanoBrazo = new Vector3(0.2f, 0.7f, 0.2f);
        private const float MitadBrazo = 0.35f;
        private const float GradosPorVuelta = 360f;

        [MenuItem("Mecha Cyberpunk/Crear T-195 pandilla")]
        public static void Crear()
        {
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatos);
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<DatosPandilleroAPie>();
                AssetDatabase.CreateAsset(datos, RutaDatos);
            }
            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssets();

            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabPandillero) == null)
            {
                ArmarPandillero(datos);
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabPandilla) == null)
            {
                ArmarPandilla(datos);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[IA] T-195 pandilla creada.");
        }

        private static void ArmarPandillero(DatosPandilleroAPie datos)
        {
            Material matCuerpo = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialCuerpo);
            Material matBrazo = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialBrazo);
            GameObject raiz = new GameObject("PandilleroAPie");
            try
            {
                CharacterController cc = raiz.AddComponent<CharacterController>();
                cc.height = AlturaCuerpo;
                cc.radius = RadioCuerpo;
                cc.center = new Vector3(0f, AlturaCuerpo / 2f, 0f);

                GameObject cuerpo = Primitiva(PrimitiveType.Capsule, "Cuerpo", raiz.transform, matCuerpo);
                cuerpo.transform.localPosition = new Vector3(0f, AlturaCuerpo / 2f, 0f);
                cuerpo.transform.localScale = new Vector3(RadioCuerpo * 2f, AlturaCuerpo / 2f, RadioCuerpo * 2f);

                GameObject cabeza = Primitiva(PrimitiveType.Sphere, "Cabeza", raiz.transform, matCuerpo);
                cabeza.transform.localPosition = new Vector3(0f, AlturaCabeza, 0f);
                cabeza.transform.localScale = Vector3.one * LadoCabeza;

                GameObject hombro = new GameObject("Hombro");
                hombro.transform.SetParent(raiz.transform, false);
                hombro.transform.localPosition = new Vector3(SeparacionHombro, AlturaHombro, 0f);
                GameObject brazo = Primitiva(PrimitiveType.Cube, "Brazo", hombro.transform, matBrazo);
                brazo.transform.localPosition = new Vector3(0f, -MitadBrazo, 0f);
                brazo.transform.localScale = TamanoBrazo;

                PandilleroAPie pandillero = raiz.AddComponent<PandilleroAPie>();
                SerializedObject so = new SerializedObject(pandillero);
                so.FindProperty("datos").objectReferenceValue = datos;
                so.FindProperty("cuerpo").objectReferenceValue = cuerpo.GetComponent<Renderer>();
                so.FindProperty("brazo").objectReferenceValue = hombro.transform;
                SerializedProperty piezas = so.FindProperty("piezasBrazo");
                piezas.arraySize = 1;
                piezas.GetArrayElementAtIndex(0).objectReferenceValue = brazo.GetComponent<Renderer>();
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabPandillero);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static void ArmarPandilla(DatosPandilleroAPie datos)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabPandillero);
            GameObject raiz = new GameObject("PandillaAPie");
            try
            {
                for (int i = 0; i < datos.TamanoPandilla; i++)
                {
                    float angulo = GradosPorVuelta * i / datos.TamanoPandilla;
                    Vector3 direccion = Quaternion.Euler(0f, angulo, 0f) * Vector3.forward;
                    GameObject hijo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz.transform);
                    hijo.name = "Pandillero " + (i + 1);
                    hijo.transform.localPosition = direccion * datos.RadioFormacion;
                    hijo.transform.localRotation = Quaternion.LookRotation(-direccion, Vector3.up);
                }
                PandillaAPie pandilla = raiz.AddComponent<PandillaAPie>();
                SerializedObject so = new SerializedObject(pandilla);
                so.FindProperty("datos").objectReferenceValue = datos;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabPandilla);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        // Pieza visual sin collider propio: el CharacterController es el único que choca.
        private static GameObject Primitiva(PrimitiveType tipo, string nombre, Transform padre, Material material)
        {
            GameObject objeto = GameObject.CreatePrimitive(tipo);
            objeto.name = nombre;
            Object.DestroyImmediate(objeto.GetComponent<Collider>());
            objeto.transform.SetParent(padre, false);
            if (material != null)
            {
                objeto.GetComponent<Renderer>().sharedMaterial = material;
            }
            return objeto;
        }
    }
}

using MechaCyberpunk.IA.Aliados;
using MechaCyberpunk.IA.APie;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// Crea el prefab Aliado (T-405): ModeloHumano + CharacterController + PistolaPandillero + AliadoAPie. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.IA.Editor.CreadorAliado.Crear
    /// </summary>
    public static class CreadorAliado
    {
        public const string RutaPrefab = "Assets/_Proyecto/IA/Prefabs/Aliado.prefab";
        private const string RutaModelo = "Assets/Arte/Personajes/MunecoHumano/ModeloHumano.prefab";
        private const string RutaMaterialLaser = "Assets/_Proyecto/IA/Materiales/PistolaPandillero_Laser.mat";
        private const float AlturaCuerpo = 1.8f;
        private const float RadioCuerpo = 0.3f;

        [MenuItem("Mecha Cyberpunk/Crear T-405 aliado")]
        public static GameObject Crear()
        {
            GameObject existente = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            if (existente != null)
            {
                return existente;
            }
            CreadorPandilleroArmado.Crear();
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModelo);
            GameObject raiz = new GameObject("Aliado");
            try
            {
                CharacterController controlador = raiz.AddComponent<CharacterController>();
                controlador.height = AlturaCuerpo;
                controlador.radius = RadioCuerpo;
                controlador.center = new Vector3(0f, AlturaCuerpo * 0.5f, 0f);
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(modelo, raiz.transform);
                visual.name = "Modelo";
                PistolaPandillero pistola = raiz.AddComponent<PistolaPandillero>();
                SerializedObject so = new SerializedObject(pistola);
                so.FindProperty("materialLaser").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialLaser);
                so.ApplyModifiedPropertiesWithoutUndo();
                raiz.AddComponent<AliadoAPie>();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
        }
    }
}

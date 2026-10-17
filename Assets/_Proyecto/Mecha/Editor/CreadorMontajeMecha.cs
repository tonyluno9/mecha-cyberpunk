using MechaCyberpunk.Mecha.Montaje;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.Mecha.Editor
{
    /// <summary>
    /// Agrega el montaje (T-064) a MechaJugador.prefab y crea la variante MechaEstacionado.prefab. Idempotente:
    /// no quita nada del prefab, solo agrega o actualiza MontajeMecha, Asiento y PuntoSubida.
    /// Batchmode: -executeMethod MechaCyberpunk.Mecha.Editor.CreadorMontajeMecha.Crear
    /// </summary>
    public static class CreadorMontajeMecha
    {
        private const string RutaMecha = "Assets/_Proyecto/Mecha";
        private const string RutaPrefab = RutaMecha + "/Prefabs/MechaJugador.prefab";
        private const string RutaVariante = RutaMecha + "/Prefabs/MechaEstacionado.prefab";
        private const string RutaConfig = RutaMecha + "/Datos/ConfigMontajeMecha_Base.asset";
        private const float AlturaAsiento = 3.3f;
        private const float DistanciaPuntoSubida = 1.8f;

        [MenuItem("Mecha Cyberpunk/Crear T-064 montaje")]
        public static void Crear()
        {
            ConfigMontajeMecha config = AssetDatabase.LoadAssetAtPath<ConfigMontajeMecha>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigMontajeMecha>();
                AssetDatabase.CreateAsset(config, RutaConfig);
                AssetDatabase.SaveAssets();
            }

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform asiento = Hijo(raiz.transform, "Asiento", new Vector3(0f, AlturaAsiento, 0f));
                Transform punto = Hijo(raiz.transform, "PuntoSubida", new Vector3(0f, 0f, DistanciaPuntoSubida));
                Camera camara = raiz.GetComponentInChildren<Camera>(true);
                // Pedido de Alan (D-alan-19): la cámara del mecha dibuja el posproceso de los Volumes de su escena.
                camara.GetUniversalAdditionalCameraData().renderPostProcessing = true;

                MontajeMecha montaje = raiz.GetComponent<MontajeMecha>();
                if (montaje == null)
                {
                    montaje = raiz.AddComponent<MontajeMecha>();
                }
                SerializedObject so = new SerializedObject(montaje);
                so.FindProperty("config").objectReferenceValue = config;
                so.FindProperty("arranque").enumValueIndex = (int)ArranqueMontaje.Directo;
                so.FindProperty("asiento").objectReferenceValue = asiento;
                so.FindProperty("camara").objectReferenceValue = camara;
                so.FindProperty("puntoSubida").objectReferenceValue = punto;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }

            CrearVariante();
            AssetDatabase.SaveAssets();
            Debug.Log("[Mecha] T-064 montaje creado.");
        }

        private static Transform Hijo(Transform padre, string nombre, Vector3 posicionLocal)
        {
            Transform hijo = padre.Find(nombre);
            if (hijo == null)
            {
                hijo = new GameObject(nombre).transform;
                hijo.SetParent(padre, false);
            }
            hijo.localPosition = posicionLocal;
            return hijo;
        }

        private static void CrearVariante()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaVariante) == null)
            {
                GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab));
                instancia.name = "MechaEstacionado";
                PrefabUtility.SaveAsPrefabAsset(instancia, RutaVariante);
                Object.DestroyImmediate(instancia);
            }

            GameObject variante = PrefabUtility.LoadPrefabContents(RutaVariante);
            try
            {
                SerializedObject so = new SerializedObject(variante.GetComponent<MontajeMecha>());
                so.FindProperty("arranque").enumValueIndex = (int)ArranqueMontaje.Estacionado;
                so.FindProperty("nombre").stringValue = "Mecha";
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(variante, RutaVariante);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(variante);
            }
        }
    }
}

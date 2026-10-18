using MechaCyberpunk.Economia;
using MechaCyberpunk.IA.Rival;
using MechaCyberpunk.Pelea.Datos;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.UI.Peleas;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// Crea lo de T-129: MostradorPeleas.prefab (un mostrador de caja con su EntradaArena y su FlujoPeleaCasera de hijo). Idempotente.
    /// Necesita FlujoPeleaCasera.prefab, ConfigEntradaArena_Base y PantallasHistoria.prefab.
    /// Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.CreadorMostradorPeleas.Crear
    /// </summary>
    public static class CreadorMostradorPeleas
    {
        public const string RutaPrefab = "Assets/_Proyecto/Progresion/Prologo/Prefabs/MostradorPeleas.prefab";

        private const string RutaCarpeta = "Assets/_Proyecto/Progresion/Prologo/Prefabs";
        private const string RutaFlujo = "Assets/_Proyecto/Pelea/Prefabs/FlujoPeleaCasera.prefab";
        private const string RutaConfigEntrada = "Assets/_Proyecto/Progresion/Prologo/Datos/ConfigEntradaArena_Base.asset";
        private const string RutaPantallasHistoria = "Assets/_Proyecto/UI/Prefabs/PantallasHistoria.prefab";
        private const string RutaPantallaApuesta = "Assets/_Proyecto/UI/Prefabs/PantallaApuesta.prefab";
        private const string RutaPantallaCartelera = "Assets/_Proyecto/UI/Prefabs/PantallaCartelera.prefab";
        private const string RutaPremio = "Assets/_Proyecto/Economia/Datos/DatosPremioPelea_Casera.asset";
        private const string RutaCalendario = "Assets/_Proyecto/Pelea/Datos/CalendarioLigas_Base.asset";
        private const string RutaMaterial = "Assets/_Proyecto/Progresion/Prologo/Materiales/Prologo_Mostrador.mat";
        private static readonly Vector3 TamanoMostrador = new Vector3(2f, 1.1f, 0.8f);
        private static readonly Color ColorMostrador = new Color(0.55f, 0.3f, 0.12f);

        [MenuItem("Mecha Cyberpunk/Crear T-129 mostrador de peleas")]
        public static void Crear()
        {
            if (!AssetDatabase.IsValidFolder(RutaCarpeta))
            {
                AssetDatabase.CreateFolder("Assets/_Proyecto/Progresion/Prologo", "Prefabs");
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) != null)
            {
                AgregarPantallaApuesta();
                AsignarCalendario();
                AgregarPantallaCartelera();
                return;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.SetColor("_BaseColor", ColorMostrador);
                AssetDatabase.CreateAsset(material, RutaMaterial);
            }

            GameObject raiz = new GameObject("MostradorPeleas");
            try
            {
                GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
                caja.name = "Mostrador";
                caja.transform.SetParent(raiz.transform, false);
                caja.transform.localPosition = new Vector3(0f, TamanoMostrador.y / 2f, 0f);
                caja.transform.localScale = TamanoMostrador;
                caja.GetComponent<MeshRenderer>().sharedMaterial = material;

                GameObject flujo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaFlujo));
                flujo.transform.SetParent(raiz.transform, false);

                EntradaArena entrada = raiz.AddComponent<EntradaArena>();
                SerializedObject soEntrada = new SerializedObject(entrada);
                soEntrada.FindProperty("config").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ConfigEntradaArena>(RutaConfigEntrada);
                soEntrada.FindProperty("prefabPantallas").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPantallasHistoria);
                soEntrada.ApplyModifiedPropertiesWithoutUndo();

                MostradorPeleas mostrador = raiz.AddComponent<MostradorPeleas>();
                SerializedObject soMostrador = new SerializedObject(mostrador);
                soMostrador.FindProperty("entrada").objectReferenceValue = entrada;
                soMostrador.FindProperty("flujo").objectReferenceValue = flujo.GetComponent<DirectorPelea>();
                soMostrador.FindProperty("premio").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosPremioPelea>(RutaPremio);
                soMostrador.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
            AssetDatabase.SaveAssets();
            AgregarPantallaApuesta();
            AsignarCalendario();
            AgregarPantallaCartelera();
            Debug.Log("[Progresion] T-129 mostrador de peleas creado.");
        }

        // T-159: le asigna el calendario de las ligas al mostrador. Idempotente: no pisa un calendario ya puesto.
        private static void AsignarCalendario()
        {
            CalendarioLigas calendario = AssetDatabase.LoadAssetAtPath<CalendarioLigas>(RutaCalendario);
            if (calendario == null)
            {
                Debug.LogError("[Progresion] Falta " + RutaCalendario);
                return;
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                SerializedObject so = new SerializedObject(raiz.GetComponent<MostradorPeleas>());
                SerializedProperty propiedad = so.FindProperty("calendario");
                if (propiedad.objectReferenceValue != null)
                {
                    return;
                }
                propiedad.objectReferenceValue = calendario;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Progresion] T-159 calendario de ligas asignado al mostrador.");
        }

        // T-160: le agrega al mostrador la cartelera (hija) y copia el alias del rival casero. Idempotente y conserva todo lo demás del prefab.
        private static void AgregarPantallaCartelera()
        {
            GameObject prefabPantalla = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPantallaCartelera);
            if (prefabPantalla == null)
            {
                Debug.LogError("[Progresion] Falta " + RutaPantallaCartelera + ": corre CreadorPantallaCartelera.Crear primero.");
                return;
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                MostradorPeleas mostrador = raiz.GetComponent<MostradorPeleas>();
                SerializedObject so = new SerializedObject(mostrador);
                bool cambio = false;
                SerializedProperty propiedad = so.FindProperty("pantallaCartelera");
                if (propiedad.objectReferenceValue == null)
                {
                    GameObject hija = (GameObject)PrefabUtility.InstantiatePrefab(prefabPantalla, raiz.transform);
                    propiedad.objectReferenceValue = hija.GetComponent<PantallaCartelera>();
                    cambio = true;
                }
                SerializedProperty alias = so.FindProperty("aliasCasera");
                if (string.IsNullOrEmpty(alias.stringValue))
                {
                    alias.stringValue = AliasDelRivalCasero(raiz.GetComponentInChildren<DirectorPelea>(true));
                    cambio = cambio || !string.IsNullOrEmpty(alias.stringValue);
                }
                if (!cambio)
                {
                    return;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Progresion] T-160 cartelera agregada al mostrador.");
        }

        private static string AliasDelRivalCasero(DirectorPelea director)
        {
            if (director == null)
            {
                return string.Empty;
            }
            GameObject prefabRival = new SerializedObject(director).FindProperty("prefabRival").objectReferenceValue as GameObject;
            RivalBasico rival = prefabRival != null ? prefabRival.GetComponentInChildren<RivalBasico>(true) : null;
            return rival != null && rival.Datos != null ? rival.Datos.Alias : string.Empty;
        }

        // T-145: le agrega al mostrador su pantalla de apuesta (hija) y la conecta. Idempotente y conserva todo lo demás del prefab.
        private static void AgregarPantallaApuesta()
        {
            GameObject prefabPantalla = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPantallaApuesta);
            if (prefabPantalla == null)
            {
                Debug.LogError("[Progresion] Falta " + RutaPantallaApuesta + ": corre CreadorPantallaApuesta.Crear primero.");
                return;
            }
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                MostradorPeleas mostrador = raiz.GetComponent<MostradorPeleas>();
                SerializedObject so = new SerializedObject(mostrador);
                SerializedProperty propiedad = so.FindProperty("pantallaApuesta");
                if (propiedad.objectReferenceValue != null)
                {
                    return;
                }
                GameObject hija = (GameObject)PrefabUtility.InstantiatePrefab(prefabPantalla, raiz.transform);
                propiedad.objectReferenceValue = hija.GetComponent<PantallaApuesta>();
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Progresion] T-145 pantalla de apuesta agregada al mostrador.");
        }
    }
}

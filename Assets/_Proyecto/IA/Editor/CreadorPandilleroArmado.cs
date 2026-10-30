using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.IA.Editor
{
    /// <summary>
    /// Crea lo de T-402: los datos DatosPandilleroAPie_Calaveras y _Viboras, el material del láser y el prefab PandilleroArmado
    /// (ModeloHumano de T-400 + CharacterController + PandilleroAPie + PistolaPandillero). Idempotente: lo que existe no se recrea.
    /// Batchmode: -executeMethod MechaCyberpunk.IA.Editor.CreadorPandilleroArmado.Crear
    /// </summary>
    public static class CreadorPandilleroArmado
    {
        public const string RutaDatosCalaveras = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset";
        public const string RutaDatosViboras = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Viboras.asset";
        public const string RutaPrefab = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        private const string RutaMaterialLaser = "Assets/_Proyecto/IA/Materiales/PistolaPandillero_Laser.mat";
        private const string RutaModelo = "Assets/Arte/Personajes/MunecoHumano/ModeloHumano.prefab";

        private const float AlturaCuerpo = 1.8f;
        private const float RadioCuerpo = 0.3f;
        private const float VidaPandillero = 60f;
        private const float AvisoLaser = 0.35f;
        private const float RecargaTiro = 0.85f;
        private const float VelocidadPandillero = 4.5f;
        private const int DineroPorVencido = 40;
        private const float CalorPorVencido = 8f;
        private const int MaxDisparando = 2;

        [MenuItem("Mecha Cyberpunk/Crear T-402 pandillero armado")]
        public static void Crear()
        {
            Material laser = CrearMaterialLaser();
            DatosPandilleroAPie calaveras = CrearDatos(RutaDatosCalaveras, "SKULL", 3, new Color(1f, 0.1f, 0.8f));
            CrearDatos(RutaDatosViboras, "SNAKE", 3, new Color(0.45f, 1f, 0.1f));
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) == null)
            {
                ArmarPrefab(calaveras, laser);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[IA] T-402 pandillero armado listo.");
        }

        private static Material CrearMaterialLaser()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialLaser);
            if (material != null)
            {
                return material;
            }
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            material = new Material(shader);
            material.SetColor("_BaseColor", Color.red);
            AssetDatabase.CreateAsset(material, RutaMaterialLaser);
            return material;
        }

        private static DatosPandilleroAPie CrearDatos(string ruta, string nombre, int tamano, Color color)
        {
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(ruta);
            if (datos != null)
            {
                return datos;
            }
            datos = ScriptableObject.CreateInstance<DatosPandilleroAPie>();
            AssetDatabase.CreateAsset(datos, ruta);
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("nombre").stringValue = nombre;
            so.FindProperty("vidaMaxima").floatValue = VidaPandillero;
            so.FindProperty("preparacionGolpe").floatValue = AvisoLaser;
            so.FindProperty("recargaGolpe").floatValue = RecargaTiro;
            so.FindProperty("velocidad").floatValue = VelocidadPandillero;
            so.FindProperty("dinero").intValue = DineroPorVencido;
            so.FindProperty("tamanoPandilla").intValue = tamano;
            so.FindProperty("maxAtacando").intValue = MaxDisparando;
            so.FindProperty("usaPistola").boolValue = true;
            so.FindProperty("siempre").boolValue = true;
            so.FindProperty("calorAlVencer").floatValue = CalorPorVencido;
            so.FindProperty("colorNormal").colorValue = color;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            return datos;
        }

        private static void ArmarPrefab(DatosPandilleroAPie datos, Material laser)
        {
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModelo);
            if (modelo == null)
            {
                Debug.LogError("[IA] Falta " + RutaModelo + " (T-400).");
                return;
            }
            GameObject raiz = new GameObject("PandilleroArmado");
            try
            {
                CharacterController controlador = raiz.AddComponent<CharacterController>();
                controlador.height = AlturaCuerpo;
                controlador.radius = RadioCuerpo;
                controlador.center = new Vector3(0f, AlturaCuerpo * 0.5f, 0f);
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(modelo, raiz.transform);
                visual.name = "Modelo";
                PistolaPandillero pistola = raiz.AddComponent<PistolaPandillero>();
                SerializedObject soPistola = new SerializedObject(pistola);
                soPistola.FindProperty("materialLaser").objectReferenceValue = laser;
                soPistola.ApplyModifiedPropertiesWithoutUndo();
                PandilleroAPie pandillero = raiz.AddComponent<PandilleroAPie>();
                SerializedObject so = new SerializedObject(pandillero);
                so.FindProperty("datos").objectReferenceValue = datos;
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

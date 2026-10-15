using MechaCyberpunk.Mecha.Cabina;
using MechaCyberpunk.Mecha.Movimiento;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Mecha.Editor
{
    /// <summary>
    /// Arma lo de T-104 en MechaJugador.prefab (MechaEstacionado.prefab lo hereda): actualiza ConfigMechaDaniado_Base, agrega la
    /// alarma (luz roja y LED en el tablero) a la cabina y asigna la config al ControladorMecha. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Mecha.Editor.CreadorDanioCabina.Crear
    /// </summary>
    public static class CreadorDanioCabina
    {
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaConfig = "Assets/_Proyecto/Mecha/Datos/ConfigMechaDaniado_Base.asset";
        private const string RutaMateriales = "Assets/_Proyecto/Mecha/Materiales/";
        private const string NombreAlarma = "AlarmaDanio";
        private const float AlturaCamara = 3.5f;

        // Sobre el tablero, a la izquierda de la pantalla de vida y fuera del centro de la vista (m, relativo a la cámara en reposo).
        private static readonly Vector3 PosicionLed = new Vector3(-0.22f, -0.29f, 0.6f);
        private static readonly Vector3 PosicionLuz = new Vector3(-0.22f, -0.2f, 0.55f);
        private static readonly Vector3 EscalaLed = new Vector3(0.05f, 0.025f, 0.05f);
        private const float FactorEmisionLed = 4f;
        private static readonly Color ColorLedApagado = new Color(0.12f, 0.02f, 0.02f);

        [MenuItem("Mecha Cyberpunk/Crear T-104 mecha dañado")]
        public static void Crear()
        {
            ConfigMechaDaniado config = AssetDatabase.LoadAssetAtPath<ConfigMechaDaniado>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigMechaDaniado>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            // Escribe en el asset los valores nuevos (los que no estaban guardados toman su valor por defecto).
            EditorUtility.SetDirty(config);

            Material apagado = CrearMaterial(RutaMateriales + "Cabina_AlarmaApagada.mat", ColorLedApagado, null);
            Material prendido = CrearMaterial(RutaMateriales + "Cabina_AlarmaPrendida.mat", config.ColorAlarma * 0.5f,
                config.ColorAlarma * FactorEmisionLed);
            AssetDatabase.SaveAssets();

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform cabina = raiz.transform.Find("Sacudida/Cabina");
                Transform vieja = cabina.Find(NombreAlarma);
                if (vieja != null)
                {
                    Object.DestroyImmediate(vieja.gameObject);
                }

                GameObject alarma = new GameObject(NombreAlarma);
                alarma.transform.SetParent(cabina, false);

                GameObject objetoLed = GameObject.CreatePrimitive(PrimitiveType.Cube);
                objetoLed.name = "Led";
                Object.DestroyImmediate(objetoLed.GetComponent<Collider>());
                objetoLed.transform.SetParent(alarma.transform, false);
                objetoLed.transform.localPosition = new Vector3(PosicionLed.x, PosicionLed.y + AlturaCamara, PosicionLed.z);
                objetoLed.transform.localScale = EscalaLed;
                MeshRenderer render = objetoLed.GetComponent<MeshRenderer>();
                render.sharedMaterial = apagado;
                render.shadowCastingMode = ShadowCastingMode.Off;
                render.receiveShadows = false;

                GameObject objetoLuz = new GameObject("Luz");
                objetoLuz.transform.SetParent(alarma.transform, false);
                objetoLuz.transform.localPosition = new Vector3(PosicionLuz.x, PosicionLuz.y + AlturaCamara, PosicionLuz.z);
                Light luz = objetoLuz.AddComponent<Light>();
                luz.type = LightType.Point;
                luz.color = config.ColorAlarma;
                luz.intensity = config.IntensidadLuzAlarma;
                luz.range = config.AlcanceLuzAlarma;
                luz.shadows = LightShadows.None;
                luz.enabled = false;

                AlarmaCabina componente = alarma.AddComponent<AlarmaCabina>();
                SerializedObject so = new SerializedObject(componente);
                so.FindProperty("config").objectReferenceValue = config;
                so.FindProperty("luz").objectReferenceValue = luz;
                so.FindProperty("led").objectReferenceValue = render;
                so.FindProperty("materialApagado").objectReferenceValue = apagado;
                so.FindProperty("materialPrendido").objectReferenceValue = prendido;
                so.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject soControlador = new SerializedObject(raiz.GetComponent<ControladorMecha>());
                soControlador.FindProperty("configDanio").objectReferenceValue = config;
                soControlador.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Mecha] T-104 mecha dañado creado.");
        }

        private static Material CrearMaterial(string ruta, Color color, Color? emision)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.3f);
            if (emision.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emision.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}

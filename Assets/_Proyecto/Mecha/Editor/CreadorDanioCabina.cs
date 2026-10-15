using MechaCyberpunk.Mecha.Cabina;
using MechaCyberpunk.Mecha.Movimiento;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Mecha.Editor
{
    /// <summary>
    /// Arma lo de T-104 y T-105 en MechaJugador.prefab (MechaEstacionado.prefab lo hereda): actualiza ConfigMechaDaniado_Base, agrega la
    /// alarma (luz roja y LED en el tablero) a la cabina y asigna la config al ControladorMecha y agrega las grietas (CristalCuarteado) al cristal. Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.Mecha.Editor.CreadorDanioCabina.Crear
    /// </summary>
    public static class CreadorDanioCabina
    {
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaConfig = "Assets/_Proyecto/Mecha/Datos/ConfigMechaDaniado_Base.asset";
        private const string RutaMateriales = "Assets/_Proyecto/Mecha/Materiales/";
        private const string NombreAlarma = "AlarmaDanio";
        private const float AlturaCamara = 3.5f;
        private const string NombreCristal = "Cristal";
        private const string NombreGrietas1 = "Grietas1";
        private const string NombreGrietas2 = "Grietas2";
        // Las grietas flotan 1 cm del lado del piloto (el quad mira hacia -z local, escala z = 1).
        private const float ProfundidadGrietas = -0.01f;
        private const int OrdenGrietas = 3001;

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

                CrearGrietas(cabina, config, CrearMaterialGrietas(RutaMateriales + "Cabina_Grietas.mat", config.ColorGrietas));

                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Mecha] T-104 mecha dañado creado.");
        }

        private static void CrearGrietas(Transform cabina, ConfigMechaDaniado config, Material material)
        {
            Transform cristal = cabina.Find(NombreCristal);
            CristalCuarteado viejo = cristal.GetComponent<CristalCuarteado>();
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo);
            }
            DestruirHijo(cristal, NombreGrietas1);
            DestruirHijo(cristal, NombreGrietas2);

            GameObject grupo1 = CrearGrupo(cristal, NombreGrietas1, GeneradorGrietas.Nivel1(), config, material);
            GameObject grupo2 = CrearGrupo(cristal, NombreGrietas2, GeneradorGrietas.Nivel2(), config, material);

            CristalCuarteado componente = cristal.gameObject.AddComponent<CristalCuarteado>();
            SerializedObject so = new SerializedObject(componente);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("grietas1").objectReferenceValue = grupo1;
            so.FindProperty("grietas2").objectReferenceValue = grupo2;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void DestruirHijo(Transform padre, string nombre)
        {
            Transform hijo = padre.Find(nombre);
            if (hijo != null)
            {
                Object.DestroyImmediate(hijo.gameObject);
            }
        }

        private static GameObject CrearGrupo(Transform cristal, string nombre, List<Vector2[]> grietas, ConfigMechaDaniado config,
            Material material)
        {
            GameObject grupo = new GameObject(nombre);
            grupo.transform.SetParent(cristal, false);
            for (int i = 0; i < grietas.Count; i++)
            {
                GameObject objeto = new GameObject("Grieta" + i);
                objeto.transform.SetParent(grupo.transform, false);
                LineRenderer linea = objeto.AddComponent<LineRenderer>();
                linea.useWorldSpace = false;
                linea.positionCount = grietas[i].Length;
                for (int p = 0; p < grietas[i].Length; p++)
                {
                    linea.SetPosition(p, new Vector3(grietas[i][p].x, grietas[i][p].y, ProfundidadGrietas));
                }
                linea.widthMultiplier = config.AnchoGrietas;
                linea.numCapVertices = 0;
                linea.numCornerVertices = 0;
                linea.sharedMaterial = material;
                linea.shadowCastingMode = ShadowCastingMode.Off;
                linea.receiveShadows = false;
            }
            return grupo;
        }

        private static Material CrearMaterialGrietas(string ruta, Color color)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = OrdenGrietas;
            EditorUtility.SetDirty(m);
            return m;
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

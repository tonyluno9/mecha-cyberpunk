using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace MechaCyberpunk.Mecha.Editor
{
    /// <summary>
    /// T-194: el LED SINAPSIS (dos LEDs turquesa y su rótulo) en el tablero de MechaJugador.prefab. Idempotente: si ya existe no se rehace.
    /// Batchmode: -executeMethod MechaCyberpunk.Mecha.Editor.CreadorLedSinapsis.Crear
    /// </summary>
    public static class CreadorLedSinapsis
    {
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaMaterial = "Assets/_Proyecto/Mecha/Materiales/Cabina_NeonTurquesa.mat";
        private const string Nombre = "LedSinapsis";
        private const float AlturaCamara = 3.5f;
        private const float InclinacionRotulo = 60f;
        private const float TamanoLed = 0.022f;
        private const float SeparacionLeds = 0.034f;
        private const float EscalaCanvas = 0.001f;
        private const int TamanoTexto = 18;

        // Esquina derecha del tablero, a un lado de la pantalla de calor (a la izquierda lo tapa el brazo).
        private static readonly Vector3 PosicionBase = new Vector3(0.43f, -0.298f, 0.74f);

        [MenuItem("Mecha Cyberpunk/Crear T-194 LED sinapsis")]
        public static void Crear()
        {
            Material turquesa = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial);
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform cabina = raiz.transform.Find("Sacudida/Cabina");
                Transform existente = cabina.Find(Nombre);
                Vector3 posicion = new Vector3(PosicionBase.x, AlturaCamara + PosicionBase.y, PosicionBase.z);
                if (existente == null)
                {
                    Armar(cabina, turquesa);
                    PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
                }
                else if (existente.localPosition != posicion)
                {
                    // Idempotente: si solo cambió el lugar, se mueve el mismo objeto (no se rehace).
                    existente.localPosition = posicion;
                    PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Mecha] T-194 LED sinapsis creado.");
        }

        private static void Armar(Transform cabina, Material turquesa)
        {
            GameObject grupo = new GameObject(Nombre);
            grupo.transform.SetParent(cabina, false);
            grupo.transform.localPosition = new Vector3(PosicionBase.x, AlturaCamara + PosicionBase.y, PosicionBase.z);
            GameObject led1 = Led("Led1", grupo.transform, 0f, turquesa);
            GameObject led2 = Led("Led2", grupo.transform, SeparacionLeds, turquesa);

            GameObject objetoRotulo = new GameObject("Rotulo", typeof(RectTransform));
            objetoRotulo.transform.SetParent(grupo.transform, false);
            objetoRotulo.transform.localPosition = new Vector3(SeparacionLeds * 0.5f, 0f, -0.03f);
            objetoRotulo.transform.localRotation = Quaternion.Euler(InclinacionRotulo, 0f, 0f);
            objetoRotulo.transform.localScale = Vector3.one * EscalaCanvas;
            ((RectTransform)objetoRotulo.transform).sizeDelta = new Vector2(120f, 24f);
            objetoRotulo.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            Text texto = objetoRotulo.AddComponent<Text>();
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.text = "SINAPSIS";
            texto.fontSize = TamanoTexto;
            texto.alignment = TextAnchor.MiddleCenter;
            texto.color = new Color(0f, 1f, 0.9f, 1f);
            texto.raycastTarget = false;

            LedSinapsis led = grupo.AddComponent<LedSinapsis>();
            SerializedObject so = new SerializedObject(led);
            so.FindProperty("led1").objectReferenceValue = led1;
            so.FindProperty("led2").objectReferenceValue = led2;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject Led(string nombre, Transform padre, float x, Material material)
        {
            GameObject led = GameObject.CreatePrimitive(PrimitiveType.Cube);
            led.name = nombre;
            Object.DestroyImmediate(led.GetComponent<Collider>());
            led.transform.SetParent(padre, false);
            led.transform.localPosition = new Vector3(x, 0f, 0f);
            led.transform.localScale = Vector3.one * TamanoLed;
            MeshRenderer render = led.GetComponent<MeshRenderer>();
            render.sharedMaterial = material;
            render.shadowCastingMode = ShadowCastingMode.Off;
            render.receiveShadows = false;
            return led;
        }
    }
}

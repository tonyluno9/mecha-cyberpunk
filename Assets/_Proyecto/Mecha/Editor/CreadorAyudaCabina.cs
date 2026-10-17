using System.Linq;
using MechaCyberpunk.UI;
using MechaCyberpunk.UI.Cabina;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.Mecha.Editor
{
    /// <summary>
    /// Arma lo de T-134 en MechaJugador.prefab (y por herencia en MechaEstacionado.prefab): la pantalla de ayuda de controles en el
    /// tablero, colgada arriba del cristal, fuera del centro de la vista. Idempotente: rehace la pantalla cada vez.
    /// Batchmode: -executeMethod MechaCyberpunk.Mecha.Editor.CreadorAyudaCabina.Crear
    /// </summary>
    public static class CreadorAyudaCabina
    {
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaEntrada = "Assets/_Proyecto/Mecha/Entrada/ControlesMecha.inputactions";
        private const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigAyudas_Base.asset";
        private const string NombrePantalla = "PantallaAyuda";
        private const string AccionGolpear = "Golpear";
        private const string AccionBloquear = "Bloquear";

        // Colgada bajo la viga superior, arriba del cristal y fuera del centro de la vista: los brazos tapan los lados del tablero.
        // Medidas en metros relativas a la cámara en reposo (como CreadorCabina).
        private const float AlturaCamara = 3.5f;
        private const float XPantalla = 0f;
        private const float YPantalla = 0.285f;
        private const float ZPantalla = 0.7f;
        private const float InclinacionPantalla = 0f;

        // Canvas de 480 x 80 unidades a 0.001 m por unidad (0.48 x 0.08 m).
        private const float AnchoCanvas = 480f;
        private const float AltoCanvas = 80f;
        private const float EscalaCanvas = 0.001f;
        private const float MargenTexto = 8f;

        [MenuItem("Mecha Cyberpunk/Crear T-134 ayuda de la cabina")]
        public static void Crear()
        {
            ConfigAyudas config = AssetDatabase.LoadAssetAtPath<ConfigAyudas>(RutaConfig);
            if (config == null)
            {
                Debug.LogError("[Mecha] Falta " + RutaConfig + ": corre antes CreadorAyudas.Crear");
                return;
            }

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform cabina = raiz.transform.Find("Sacudida/Cabina");
                Transform vieja = cabina.Find(NombrePantalla);
                if (vieja != null)
                {
                    Object.DestroyImmediate(vieja.gameObject);
                }
                ArmarPantalla(cabina, config);
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Mecha] T-134 ayuda de la cabina creada.");
        }

        private static InputActionReference Referencia(string nombreAccion)
        {
            return AssetDatabase.LoadAllAssetsAtPath(RutaEntrada)
                .OfType<InputActionReference>()
                .First(r => r.action != null && r.action.name == nombreAccion);
        }

        private static void ArmarPantalla(Transform cabina, ConfigAyudas config)
        {
            GameObject objeto = new GameObject(NombrePantalla, typeof(RectTransform));
            objeto.transform.SetParent(cabina, false);
            objeto.transform.localPosition = new Vector3(XPantalla, AlturaCamara + YPantalla, ZPantalla);
            objeto.transform.localRotation = Quaternion.Euler(InclinacionPantalla, 0f, 0f);
            objeto.transform.localScale = Vector3.one * EscalaCanvas;
            RectTransform rectCanvas = (RectTransform)objeto.transform;
            rectCanvas.sizeDelta = new Vector2(AnchoCanvas, AltoCanvas);
            Canvas canvas = objeto.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            GameObject objetoFondo = new GameObject("Fondo", typeof(RectTransform));
            RectTransform rectFondo = (RectTransform)objetoFondo.transform;
            rectFondo.SetParent(rectCanvas, false);
            Estirar(rectFondo, 0f);
            Image fondo = objetoFondo.AddComponent<Image>();
            fondo.raycastTarget = false;

            GameObject objetoTexto = new GameObject("Texto", typeof(RectTransform));
            RectTransform rectTexto = (RectTransform)objetoTexto.transform;
            rectTexto.SetParent(rectCanvas, false);
            Estirar(rectTexto, MargenTexto);
            Text texto = objetoTexto.AddComponent<Text>();
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.fontSize = config.TamanoLetraCabina;
            texto.alignment = TextAnchor.MiddleCenter;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Truncate;
            texto.raycastTarget = false;

            PantallaAyudaCabina pantalla = objeto.AddComponent<PantallaAyudaCabina>();
            SerializedObject so = new SerializedObject(pantalla);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("lienzo").objectReferenceValue = canvas;
            so.FindProperty("fondo").objectReferenceValue = fondo;
            so.FindProperty("texto").objectReferenceValue = texto;
            so.FindProperty("golpear").objectReferenceValue = Referencia(AccionGolpear);
            so.FindProperty("bloquear").objectReferenceValue = Referencia(AccionBloquear);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Estirar(RectTransform rect, float margen)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margen, margen);
            rect.offsetMax = new Vector2(-margen, -margen);
        }
    }
}

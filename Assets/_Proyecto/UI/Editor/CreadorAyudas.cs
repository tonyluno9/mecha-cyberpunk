using MechaCyberpunk.UI.Hud;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// Crea lo de T-134 en la UI: ConfigAyudas_Base.asset y el grupo "Ayuda" con AyudaControles dentro de HudAPie.prefab, abajo a la
    /// izquierda, encima de dinero y aviso de dinero y a la izquierda del aviso de llamado. Idempotente: rehace el grupo cada vez.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorAyudas.Crear
    /// </summary>
    public static class CreadorAyudas
    {
        public const string RutaConfig = "Assets/_Proyecto/UI/Datos/ConfigAyudas_Base.asset";
        private const string RutaConfigHud = "Assets/_Proyecto/UI/Datos/ConfigHudAPie_Base.asset";
        private const string RutaPrefab = "Assets/_Proyecto/UI/Prefabs/HudAPie.prefab";
        private const string NombreGrupo = "Ayuda";

        // Mismas medidas que CreadorHudAPie para apilar la ayuda sobre el dinero y su aviso.
        private const float AltoTexto = 36f;
        private const float SeparacionTextoBarra = 4f;
        // Ancho que termina antes del aviso de llamado (centrado, 800 de ancho) a 1920 x 1080 y a 1280 x 720.
        private const float AnchoAyuda = 500f;
        private const int LineasAyuda = 2;
        private static readonly Color ColorSombra = new Color(0f, 0f, 0f, 0.8f);
        private static readonly Vector2 DesplazamientoSombra = new Vector2(1.5f, -1.5f);

        [MenuItem("Mecha Cyberpunk/Crear T-134 ayuda a pie")]
        public static void Crear()
        {
            ConfigAyudas config = AssetDatabase.LoadAssetAtPath<ConfigAyudas>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigAyudas>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            ConfigHudAPie configHud = AssetDatabase.LoadAssetAtPath<ConfigHudAPie>(RutaConfigHud);

            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform vieja = raiz.transform.Find(NombreGrupo);
                if (vieja != null)
                {
                    Object.DestroyImmediate(vieja.gameObject);
                }
                ArmarGrupo(raiz, config, configHud);
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] T-134 ayuda a pie creada.");
        }

        private static void ArmarGrupo(GameObject raiz, ConfigAyudas config, ConfigHudAPie configHud)
        {
            float alturaVida = AltoTexto + SeparacionTextoBarra + configHud.AltoBarra;
            float baseDinero = configHud.Margen + alturaVida + SeparacionTextoBarra;
            float baseAyuda = baseDinero + 2f * AltoTexto + SeparacionTextoBarra;

            GameObject objetoGrupo = new GameObject(NombreGrupo, typeof(RectTransform));
            RectTransform rect = (RectTransform)objetoGrupo.transform;
            rect.SetParent(raiz.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(configHud.Margen, baseAyuda);
            rect.sizeDelta = new Vector2(AnchoAyuda, LineasAyuda * AltoTexto);

            GameObject objetoTexto = new GameObject("Texto", typeof(RectTransform));
            RectTransform rectTexto = (RectTransform)objetoTexto.transform;
            rectTexto.SetParent(rect, false);
            rectTexto.anchorMin = Vector2.zero;
            rectTexto.anchorMax = Vector2.one;
            rectTexto.offsetMin = Vector2.zero;
            rectTexto.offsetMax = Vector2.zero;
            Text texto = objetoTexto.AddComponent<Text>();
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.fontSize = configHud.TamanoTexto;
            texto.fontStyle = FontStyle.Bold;
            texto.alignment = TextAnchor.LowerLeft;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.raycastTarget = false;
            texto.color = configHud.ColorTexto;
            Shadow sombra = objetoTexto.AddComponent<Shadow>();
            sombra.effectColor = ColorSombra;
            sombra.effectDistance = DesplazamientoSombra;

            AyudaControles ayuda = objetoGrupo.AddComponent<AyudaControles>();
            SerializedObject so = new SerializedObject(ayuda);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("configHud").objectReferenceValue = configHud;
            // El componente vive en el mismo objeto que el grupo y lo apaga: se apaga su hijo para que Update siga corriendo.
            so.FindProperty("grupo").objectReferenceValue = objetoTexto;
            so.FindProperty("texto").objectReferenceValue = texto;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

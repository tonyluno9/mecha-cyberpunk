using System.IO;
using MechaCyberpunk.UI.Hud;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// Crea lo de T-056: ConfigHudAPie_Base.asset y UI/Prefabs/HudAPie.prefab (idempotente; conserva el config si ya existe).
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorHudAPie.Crear
    /// </summary>
    public static class CreadorHudAPie
    {
        private const string RutaUI = "Assets/_Proyecto/UI";
        private const string RutaConfig = RutaUI + "/Datos/ConfigHudAPie_Base.asset";
        private const string RutaPrefab = RutaUI + "/Prefabs/HudAPie.prefab";

        private const float AnchoReferencia = 1920f;
        private const float AltoReferencia = 1080f;
        private const float AltoTexto = 36f;
        private const float AnchoTextoLateral = 600f;
        private const float SeparacionTextoBarra = 4f;
        private const float LadoPunto = 4f;
        private const float GrosorMira = 2f;
        private const float HuecoMira = 6f;
        private static readonly Color ColorSombra = new Color(0f, 0f, 0f, 0.8f);
        private static readonly Vector2 DesplazamientoSombra = new Vector2(1.5f, -1.5f);

        [MenuItem("Mecha Cyberpunk/Crear T-056")]
        public static void Crear()
        {
            AsegurarCarpeta(RutaUI + "/Datos");
            AsegurarCarpeta(RutaUI + "/Prefabs");

            ConfigHudAPie config = AssetDatabase.LoadAssetAtPath<ConfigHudAPie>(RutaConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConfigHudAPie>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            AssetDatabase.SaveAssets();
            CrearPrefab(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] T-056 creada.");
        }

        private static void AsegurarCarpeta(string ruta)
        {
            if (!Directory.Exists(ruta))
            {
                Directory.CreateDirectory(ruta);
                AssetDatabase.Refresh();
            }
        }

        private static void CrearPrefab(ConfigHudAPie config)
        {
            GameObject raiz = new GameObject("HudAPie", typeof(RectTransform));
            try
            {
                Canvas canvas = raiz.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler escala = raiz.AddComponent<CanvasScaler>();
                escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                escala.referenceResolution = new Vector2(AnchoReferencia, AltoReferencia);
                escala.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                escala.matchWidthOrHeight = 0.5f;

                // Vida: abajo a la izquierda.
                RectTransform grupoVida = CrearGrupo("Vida", raiz.transform, new Vector2(0f, 0f), new Vector2(config.Margen, config.Margen),
                    new Vector2(config.AnchoBarra, AltoTexto + SeparacionTextoBarra + config.AltoBarra));
                Image hueco = CrearImagen("Hueco", grupoVida, config.ColorHueco);
                Place(hueco.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, config.AltoBarra));
                Image relleno = CrearImagen("Relleno", hueco.rectTransform, config.ColorVida);
                Stretch(relleno.rectTransform);
                Text textoVida = CrearTexto("Texto", grupoVida, config, TextAnchor.LowerLeft);
                Stretch(textoVida.rectTransform);
                textoVida.rectTransform.offsetMin = new Vector2(0f, config.AltoBarra + SeparacionTextoBarra);

                // Mira: centro.
                RectTransform grupoMira = CrearGrupo("Mira", raiz.transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                grupoMira.anchorMin = grupoMira.anchorMax = new Vector2(0.5f, 0.5f);
                Image punto = CrearParteMira("Punto", grupoMira, Vector2.zero, new Vector2(LadoPunto, LadoPunto), config);
                float centroBrazo = HuecoMira + config.LargoMira / 2f;
                Image arriba = CrearParteMira("Arriba", grupoMira, new Vector2(0f, centroBrazo), new Vector2(GrosorMira, config.LargoMira), config);
                Image abajo = CrearParteMira("Abajo", grupoMira, new Vector2(0f, -centroBrazo), new Vector2(GrosorMira, config.LargoMira), config);
                Image izquierda = CrearParteMira("Izquierda", grupoMira, new Vector2(-centroBrazo, 0f), new Vector2(config.LargoMira, GrosorMira), config);
                Image derecha = CrearParteMira("Derecha", grupoMira, new Vector2(centroBrazo, 0f), new Vector2(config.LargoMira, GrosorMira), config);

                // Arma: abajo a la derecha.
                RectTransform grupoArma = CrearGrupo("Arma", raiz.transform, new Vector2(1f, 0f), new Vector2(-config.Margen, config.Margen),
                    new Vector2(AnchoTextoLateral, AltoTexto));
                Text textoArma = CrearTexto("Texto", grupoArma, config, TextAnchor.LowerRight);
                Stretch(textoArma.rectTransform);

                // Canción: arriba a la izquierda.
                RectTransform grupoCancion = CrearGrupo("Cancion", raiz.transform, new Vector2(0f, 1f), new Vector2(config.Margen, -config.Margen),
                    new Vector2(AnchoTextoLateral, AltoTexto));
                Text textoCancion = CrearTexto("Texto", grupoCancion, config, TextAnchor.UpperLeft);
                Stretch(textoCancion.rectTransform);

                HudAPie hud = raiz.AddComponent<HudAPie>();
                SerializedObject so = new SerializedObject(hud);
                so.FindProperty("config").objectReferenceValue = config;
                so.FindProperty("grupoVida").objectReferenceValue = grupoVida.gameObject;
                so.FindProperty("grupoMira").objectReferenceValue = grupoMira.gameObject;
                so.FindProperty("grupoArma").objectReferenceValue = grupoArma.gameObject;
                so.FindProperty("grupoCancion").objectReferenceValue = grupoCancion.gameObject;
                so.FindProperty("relleno").objectReferenceValue = relleno;
                so.FindProperty("hueco").objectReferenceValue = hueco;
                so.FindProperty("textoVida").objectReferenceValue = textoVida;
                so.FindProperty("textoArma").objectReferenceValue = textoArma;
                so.FindProperty("textoCancion").objectReferenceValue = textoCancion;
                Image[] partes = { punto, arriba, abajo, izquierda, derecha };
                SerializedProperty lista = so.FindProperty("partesMira");
                lista.arraySize = partes.Length;
                for (int i = 0; i < partes.Length; i++)
                {
                    lista.GetArrayElementAtIndex(i).objectReferenceValue = partes[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }

        private static RectTransform CrearGrupo(string nombre, Transform padre, Vector2 ancla, Vector2 posicion, Vector2 tamano)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            RectTransform rect = (RectTransform)objeto.transform;
            rect.SetParent(padre, false);
            rect.anchorMin = ancla;
            rect.anchorMax = ancla;
            rect.pivot = ancla;
            rect.anchoredPosition = posicion;
            rect.sizeDelta = tamano;
            return rect;
        }

        private static Image CrearImagen(string nombre, Transform padre, Color color)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padre, false);
            Image imagen = objeto.AddComponent<Image>();
            imagen.color = color;
            imagen.raycastTarget = false;
            return imagen;
        }

        private static Image CrearParteMira(string nombre, Transform padre, Vector2 posicion, Vector2 tamano, ConfigHudAPie config)
        {
            Image imagen = CrearImagen(nombre, padre, config.ColorTexto);
            RectTransform rect = imagen.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = posicion;
            rect.sizeDelta = tamano;
            return imagen;
        }

        private static Text CrearTexto(string nombre, Transform padre, ConfigHudAPie config, TextAnchor alineacion)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padre, false);
            Text texto = objeto.AddComponent<Text>();
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.fontSize = config.TamanoTexto;
            texto.fontStyle = FontStyle.Bold;
            texto.alignment = alineacion;
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.raycastTarget = false;
            texto.color = config.ColorTexto;
            Shadow sombra = objeto.AddComponent<Shadow>();
            sombra.effectColor = ColorSombra;
            sombra.effectDistance = DesplazamientoSombra;
            return texto;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // Ancla inferior: ancho completo (anclas 0 a 1 en x) con la altura dada.
        private static void Place(RectTransform rect, Vector2 anclaMin, Vector2 anclaMax, Vector2 pivote, Vector2 tamano)
        {
            rect.anchorMin = anclaMin;
            rect.anchorMax = anclaMax;
            rect.pivot = pivote;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, tamano.y);
        }
    }
}

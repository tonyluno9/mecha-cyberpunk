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
    public static partial class CreadorHudAPie
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
        private const float AnchoTextoCentral = 800f;
        private const float AlturaAvisoInteraccion = 110f;
        private const float AlturaAvisoLlamado = 160f;
        private const float HuecoMira = 6f;
        private const int NumeroMarcasCalor = 3;
        private const float AlturaAvisoLiga = 200f;
        private const float AnchoAvisoLiga = 1200f;
        private const float AltoAvisoLiga = 130f;
        private const float SeparacionMarcasCalor = 6f;
        private const float AlturaAvisoReputacion = 340f;
        private const float AnchoAvisoReputacion = 1200f;
        private const float AltoAvisoReputacion = 90f;
        private const float AlturaAvisoMision = 450f;
        private const float AltoAvisoMision = 56f;
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
            // Reserializa el config: los campos nuevos con valor por defecto quedan escritos en el asset.
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            // Si el prefab ya existe no se regenera (conserva sus fileID); solo se le agrega lo que falte.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) == null)
            {
                CrearPrefab(config);
            }
            CompletarPrefab(config);
            AcomodarT408(config, CreadorKitUI.AsegurarEstilo());
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

                // Objetivo: arriba al centro, solo el texto (el rombo y la flecha del borde son de MarcadorObjetivo, T-408).
                RectTransform grupoObjetivo = CrearGrupo("Objetivo", raiz.transform, new Vector2(0.5f, 1f), new Vector2(0f, -config.Margen),
                    new Vector2(AnchoTextoCentral, AltoTexto));
                Text textoObjetivo = CrearTexto("Texto", grupoObjetivo, config, TextAnchor.UpperCenter);
                Stretch(textoObjetivo.rectTransform);

                // Interacción: abajo al centro, encima del borde.
                RectTransform grupoInteraccion = CrearGrupo("Interaccion", raiz.transform, new Vector2(0.5f, 0f),
                    new Vector2(0f, AlturaAvisoInteraccion), new Vector2(AnchoTextoCentral, AltoTexto));
                Text textoInteraccion = CrearTexto("Texto", grupoInteraccion, config, TextAnchor.LowerCenter);
                Stretch(textoInteraccion.rectTransform);

                // Llamado del mecha: abajo al centro, encima del aviso de interacción.
                RectTransform grupoLlamado = CrearGrupo("Llamado", raiz.transform, new Vector2(0.5f, 0f),
                    new Vector2(0f, AlturaAvisoLlamado), new Vector2(AnchoTextoCentral, AltoTexto));
                Text textoLlamado = CrearTexto("Texto", grupoLlamado, config, TextAnchor.LowerCenter);
                Stretch(textoLlamado.rectTransform);

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
                so.FindProperty("grupoObjetivo").objectReferenceValue = grupoObjetivo.gameObject;
                so.FindProperty("textoObjetivo").objectReferenceValue = textoObjetivo;
                so.FindProperty("grupoInteraccion").objectReferenceValue = grupoInteraccion.gameObject;
                so.FindProperty("textoInteraccion").objectReferenceValue = textoInteraccion;
                so.FindProperty("grupoLlamado").objectReferenceValue = grupoLlamado.gameObject;
                so.FindProperty("textoLlamado").objectReferenceValue = textoLlamado;
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

        // Agrega al prefab existente el grupo de calor (T-067) y los de dinero y renta (T-110) si todavía no los tiene.
        private static void CompletarPrefab(ConfigHudAPie config)
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                HudAPie hud = raiz.GetComponent<HudAPie>();
                SerializedObject so = new SerializedObject(hud);
                if (so.FindProperty("grupoCalor").objectReferenceValue == null)
                {
                    RectTransform grupo = CrearGrupo("Calor", raiz.transform, new Vector2(1f, 1f), new Vector2(-config.Margen, -config.Margen),
                        new Vector2(AnchoTextoLateral, AltoTexto + SeparacionTextoBarra + config.AltoMarcaCalor));
                    Text texto = CrearTexto("Texto", grupo, config, TextAnchor.UpperRight);
                    Stretch(texto.rectTransform);
                    texto.color = config.ColorCalor;
                    texto.text = config.TextoCalor;
                    Image[] marcas = new Image[NumeroMarcasCalor];
                    for (int i = 0; i < NumeroMarcasCalor; i++)
                    {
                        marcas[i] = CrearImagen("Marca" + (i + 1), grupo, config.ColorMarcaCalorApagada);
                        RectTransform rect = marcas[i].rectTransform;
                        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
                        rect.sizeDelta = new Vector2(config.AnchoMarcaCalor, config.AltoMarcaCalor);
                        // La marca 1 queda a la izquierda y la 3 al borde derecho.
                        rect.anchoredPosition = new Vector2(-(NumeroMarcasCalor - 1 - i) * (config.AnchoMarcaCalor + SeparacionMarcasCalor), 0f);
                    }
                    so.FindProperty("grupoCalor").objectReferenceValue = grupo.gameObject;
                    so.FindProperty("textoCalor").objectReferenceValue = texto;
                    SerializedProperty lista = so.FindProperty("marcasCalor");
                    lista.arraySize = marcas.Length;
                    for (int i = 0; i < marcas.Length; i++)
                    {
                        lista.GetArrayElementAtIndex(i).objectReferenceValue = marcas[i];
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                if (so.FindProperty("grupoDinero").objectReferenceValue == null)
                {
                    // Dinero: abajo a la izquierda, encima de la vida; el aviso encima del dinero; la renta, arriba a la izquierda bajo la canción.
                    float alturaVida = AltoTexto + SeparacionTextoBarra + config.AltoBarra;
                    float baseDinero = config.Margen + alturaVida + SeparacionTextoBarra;
                    RectTransform dinero = CrearGrupo("Dinero", raiz.transform, new Vector2(0f, 0f), new Vector2(config.Margen, baseDinero),
                        new Vector2(AnchoTextoLateral, AltoTexto));
                    Text textoDinero = CrearTexto("Texto", dinero, config, TextAnchor.LowerLeft);
                    Stretch(textoDinero.rectTransform);
                    RectTransform aviso = CrearGrupo("AvisoDinero", raiz.transform, new Vector2(0f, 0f),
                        new Vector2(config.Margen, baseDinero + AltoTexto), new Vector2(AnchoTextoLateral, AltoTexto));
                    Text textoAviso = CrearTexto("Texto", aviso, config, TextAnchor.LowerLeft);
                    Stretch(textoAviso.rectTransform);
                    RectTransform grupoRenta = CrearGrupo("Renta", raiz.transform, new Vector2(0f, 1f),
                        new Vector2(config.Margen, -config.Margen - AltoTexto - SeparacionTextoBarra), new Vector2(AnchoTextoLateral, AltoTexto));
                    Text textoRenta = CrearTexto("Texto", grupoRenta, config, TextAnchor.UpperLeft);
                    Stretch(textoRenta.rectTransform);
                    textoRenta.color = config.ColorAviso;
                    so.FindProperty("grupoDinero").objectReferenceValue = dinero.gameObject;
                    so.FindProperty("textoDinero").objectReferenceValue = textoDinero;
                    so.FindProperty("grupoAvisoDinero").objectReferenceValue = aviso.gameObject;
                    so.FindProperty("textoAvisoDinero").objectReferenceValue = textoAviso;
                    so.FindProperty("grupoRenta").objectReferenceValue = grupoRenta.gameObject;
                    so.FindProperty("textoRenta").objectReferenceValue = textoRenta;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                if (so.FindProperty("grupoAvisoLiga").objectReferenceValue == null)
                {
                    // T-161: aviso grande al centro arriba, bajo el objetivo; dos líneas.
                    RectTransform grupoLiga = CrearGrupo("AvisoLiga", raiz.transform, new Vector2(0.5f, 1f),
                        new Vector2(0f, -AlturaAvisoLiga), new Vector2(AnchoAvisoLiga, AltoAvisoLiga));
                    Text primera = CrearTexto("Primera", grupoLiga, config, TextAnchor.UpperCenter);
                    primera.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                    primera.rectTransform.anchorMax = Vector2.one;
                    primera.rectTransform.offsetMin = Vector2.zero;
                    primera.rectTransform.offsetMax = Vector2.zero;
                    primera.fontSize = config.TamanoAvisoLiga;
                    primera.color = config.ColorAvisoLiga;
                    Text segunda = CrearTexto("Segunda", grupoLiga, config, TextAnchor.UpperCenter);
                    segunda.rectTransform.anchorMin = Vector2.zero;
                    segunda.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                    segunda.rectTransform.offsetMin = Vector2.zero;
                    segunda.rectTransform.offsetMax = Vector2.zero;
                    segunda.fontSize = config.TamanoAvisoLiga2;
                    segunda.color = config.ColorAvisoLiga;
                    so.FindProperty("grupoAvisoLiga").objectReferenceValue = grupoLiga.gameObject;
                    so.FindProperty("textoAvisoLiga").objectReferenceValue = primera;
                    so.FindProperty("textoAvisoLiga2").objectReferenceValue = segunda;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    grupoLiga.gameObject.SetActive(false);
                }
                if (so.FindProperty("grupoAvisoReputacion").objectReferenceValue == null)
                {
                    // T-186: aviso de reputación (dos líneas) y aviso de misión terminada, al centro arriba bajo el aviso de liga.
                    RectTransform grupoReputacion = CrearGrupo("AvisoReputacion", raiz.transform, new Vector2(0.5f, 1f),
                        new Vector2(0f, -AlturaAvisoReputacion), new Vector2(AnchoAvisoReputacion, AltoAvisoReputacion));
                    Text primeraRep = CrearTexto("Primera", grupoReputacion, config, TextAnchor.UpperCenter);
                    primeraRep.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                    primeraRep.rectTransform.anchorMax = Vector2.one;
                    primeraRep.rectTransform.offsetMin = Vector2.zero;
                    primeraRep.rectTransform.offsetMax = Vector2.zero;
                    primeraRep.fontSize = config.TamanoAvisoReputacion;
                    primeraRep.color = config.ColorAvisoReputacion;
                    Text segundaRep = CrearTexto("Segunda", grupoReputacion, config, TextAnchor.UpperCenter);
                    segundaRep.rectTransform.anchorMin = Vector2.zero;
                    segundaRep.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                    segundaRep.rectTransform.offsetMin = Vector2.zero;
                    segundaRep.rectTransform.offsetMax = Vector2.zero;
                    segundaRep.fontSize = config.TamanoAvisoReputacion2;
                    segundaRep.color = config.ColorAvisoReputacion;
                    RectTransform grupoMision = CrearGrupo("AvisoMision", raiz.transform, new Vector2(0.5f, 1f),
                        new Vector2(0f, -AlturaAvisoMision), new Vector2(AnchoAvisoReputacion, AltoAvisoMision));
                    Text textoMision = CrearTexto("Texto", grupoMision, config, TextAnchor.UpperCenter);
                    Stretch(textoMision.rectTransform);
                    textoMision.fontSize = config.TamanoAvisoMision;
                    so.FindProperty("grupoAvisoReputacion").objectReferenceValue = grupoReputacion.gameObject;
                    so.FindProperty("textoAvisoReputacion").objectReferenceValue = primeraRep;
                    so.FindProperty("textoAvisoReputacion2").objectReferenceValue = segundaRep;
                    so.FindProperty("grupoAvisoMision").objectReferenceValue = grupoMision.gameObject;
                    so.FindProperty("textoAvisoMision").objectReferenceValue = textoMision;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    grupoReputacion.gameObject.SetActive(false);
                    grupoMision.gameObject.SetActive(false);
                }
                if (so.FindProperty("grupoVidaRival").objectReferenceValue == null)
                {
                    // T-195: OJOS a pie. Barra delgada turquesa sobre la mira y el aviso de golpe a su derecha.
                    RectTransform grupoRival = CrearGrupo("VidaRival", raiz.transform, new Vector2(0.5f, 0.5f),
                        new Vector2(0f, config.AlturaBarraRival), new Vector2(config.AnchoBarraRival, config.AltoBarraRival));
                    Image huecoRival = CrearImagen("Hueco", grupoRival, config.ColorHueco);
                    Stretch(huecoRival.rectTransform);
                    Image rellenoRival = CrearImagen("Relleno", grupoRival, config.ColorVida);
                    Stretch(rellenoRival.rectTransform);
                    Texture2D blancoRival = Texture2D.whiteTexture;
                    rellenoRival.sprite = Sprite.Create(blancoRival, new Rect(0f, 0f, blancoRival.width, blancoRival.height), new Vector2(0.5f, 0.5f));
                    rellenoRival.type = Image.Type.Filled;
                    rellenoRival.fillMethod = Image.FillMethod.Horizontal;
                    rellenoRival.fillOrigin = (int)Image.OriginHorizontal.Left;
                    Text textoRival = CrearTexto("Numeros", grupoRival, config, TextAnchor.LowerCenter);
                    textoRival.rectTransform.anchorMin = new Vector2(0f, 1f);
                    textoRival.rectTransform.anchorMax = new Vector2(1f, 1f);
                    textoRival.rectTransform.pivot = new Vector2(0.5f, 0f);
                    textoRival.rectTransform.anchoredPosition = Vector2.zero;
                    textoRival.rectTransform.sizeDelta = new Vector2(0f, AltoTexto);
                    textoRival.color = config.ColorVida;
                    RectTransform grupoGolpe = CrearGrupo("GolpeRival", raiz.transform, new Vector2(0.5f, 0.5f),
                        new Vector2(config.DesplazamientoGolpeRival, 0f), new Vector2(AnchoTextoLateral / 2f, AltoTexto));
                    grupoGolpe.pivot = new Vector2(0f, 0.5f);
                    Text textoGolpe = CrearTexto("Texto", grupoGolpe, config, TextAnchor.MiddleLeft);
                    Stretch(textoGolpe.rectTransform);
                    textoGolpe.fontSize = config.TamanoTextoGolpeRival;
                    textoGolpe.color = config.ColorAviso;
                    textoGolpe.text = config.TextoGolpeRival;
                    so.FindProperty("grupoVidaRival").objectReferenceValue = grupoRival.gameObject;
                    so.FindProperty("rellenoVidaRival").objectReferenceValue = rellenoRival;
                    so.FindProperty("textoVidaRival").objectReferenceValue = textoRival;
                    so.FindProperty("grupoGolpeRival").objectReferenceValue = grupoGolpe.gameObject;
                    so.FindProperty("textoGolpeRival").objectReferenceValue = textoGolpe;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    grupoRival.gameObject.SetActive(false);
                    grupoGolpe.gameObject.SetActive(false);
                }
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
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

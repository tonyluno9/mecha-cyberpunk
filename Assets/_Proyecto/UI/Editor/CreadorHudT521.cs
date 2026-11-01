using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Kit;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Editor
{
    /// <summary>
    /// T-521: arma UI/Resources/PiezasUI.asset con los sprites, pone Teko y Rajdhani en EstiloCyberfix_Base y viste HudAPie.prefab con los marcos
    /// (vida, calor de 3 niveles, Biits con su símbolo, panel del arma con 5 casillas). Idempotente.
    /// Batchmode: -executeMethod MechaCyberpunk.UI.Editor.CreadorHudT521.Crear
    /// </summary>
    public static class CreadorHudT521
    {
        public const string RutaPiezas = "Assets/_Proyecto/UI/Resources/PiezasUI.asset";
        private const string RutaSprites = "Assets/Arte/UI/Sprites/";
        private const string RutaFuentes = "Assets/Arte/UI/Fuentes/";
        private const string RutaPrefab = "Assets/_Proyecto/UI/Prefabs/HudAPie.prefab";
        private const float AnchoCompuesto = 880f;
        private const float AltoCompuesto = 196f;
        private const float AnchoBarraCalor = 330f;
        private const float Segmento0 = 243f;
        private const float PasoSegmento = 178f;
        private const float AnchoSegmento = 190f;
        private const float SegmentoArriba = 36f;
        private const float SegmentoAbajo = 160f;

        [MenuItem("Mecha Cyberpunk/Crear T-521 HUD")]
        public static void Crear()
        {
            AssetDatabase.Refresh();
            PiezasUI piezas = CrearPiezas();
            EstiloCyberfix estilo = PonerFuentes();
            CreadorHudAPie.Crear();
            Vestir(piezas, estilo);
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] T-521 creada.");
        }

        private static Sprite Cargar(string grupo, string nombre)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RutaSprites + grupo + "/" + nombre + ".png");
            if (sprite == null)
            {
                Debug.LogError("[UI] T-521: falta el sprite " + grupo + "/" + nombre);
            }
            return sprite;
        }

        private static void Poner(SerializedObject so, string campo, Sprite sprite) => so.FindProperty(campo).objectReferenceValue = sprite;

        private static void PonerLista(SerializedObject so, string campo, string grupo, string[] nombres)
        {
            SerializedProperty lista = so.FindProperty(campo);
            lista.arraySize = nombres.Length;
            for (int i = 0; i < nombres.Length; i++)
            {
                lista.GetArrayElementAtIndex(i).objectReferenceValue = Cargar(grupo, nombres[i]);
            }
        }

        private static PiezasUI CrearPiezas()
        {
            PiezasUI piezas = AssetDatabase.LoadAssetAtPath<PiezasUI>(RutaPiezas);
            if (piezas == null)
            {
                piezas = ScriptableObject.CreateInstance<PiezasUI>();
                AssetDatabase.CreateAsset(piezas, RutaPiezas);
            }
            SerializedObject so = new SerializedObject(piezas);
            Poner(so, "marcoPanel", Cargar("Marcos", "marco_panel"));
            Poner(so, "botonNormal", Cargar("Marcos", "boton_normal"));
            Poner(so, "botonResaltado", Cargar("Marcos", "boton_resaltado"));
            Poner(so, "botonPulsado", Cargar("Marcos", "boton_pulsado"));
            Poner(so, "barraVida", Cargar("Barras", "barra_vida"));
            Poner(so, "barraCalor3", Cargar("Barras", "barra_calor_3"));
            Poner(so, "calorRelleno", Cargar("Barras", "calor_relleno"));
            Poner(so, "simboloBiits", Cargar("Moneda", "simbolo_biits"));
            Poner(so, "marcoMinimapa", Cargar("Marcos", "marco_minimapa"));
            Poner(so, "panelArma", Cargar("Marcos", "panel_arma"));
            Poner(so, "insigniaSkulls", Cargar("Insignias", "insignia_skulls"));
            Poner(so, "insigniaSnakes", Cargar("Insignias", "insignia_snakes"));
            Poner(so, "insigniaJugador", Cargar("Insignias", "insignia_jugador"));
            Poner(so, "logo", Cargar("Marca", "logo_cyberfix"));
            Poner(so, "fondoMenu", Cargar("Marca", "fondo_menu"));
            PonerLista(so, "armas", "Armas", new[] { "arma_punos", "arma_pistola", "arma_escopeta", "arma_fusil", "arma_sniper" });
            PonerLista(so, "estados", "Estado", new[] { "estado_respeto", "estado_calor", "estado_aliados", "estado_implantes", "estado_objetivo", "estado_telefono" });
            PonerLista(so, "iconosMapa", "Mapa", new[] { "mapa_casa", "mapa_mercado_negro", "mapa_clinica", "mapa_taller", "mapa_bar", "mapa_casino", "mapa_mision" });
            PonerLista(so, "reticulas", "Reticulas", new[] { "reticula_mira", "reticula_sniper", "reticula_impacto" });
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(piezas);
            return piezas;
        }

        private static EstiloCyberfix PonerFuentes()
        {
            EstiloCyberfix estilo = CreadorKitUI.AsegurarEstilo();
            SerializedObject so = new SerializedObject(estilo);
            so.FindProperty("fuente").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>(RutaFuentes + "Rajdhani-SemiBold.ttf");
            so.FindProperty("fuenteTitulos").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>(RutaFuentes + "Teko-SemiBold.ttf");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(estilo);
            return estilo;
        }

        private static void Anclar(RectTransform r, float x0, float y0, float x1, float y1)
        {
            r.anchorMin = new Vector2(x0, y0);
            r.anchorMax = new Vector2(x1, y1);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }

        private static Image Nueva(Transform padre, string nombre, Sprite sprite)
        {
            Transform previo = padre.Find(nombre);
            if (previo != null)
            {
                Object.DestroyImmediate(previo.gameObject);
            }
            return KitUI.CrearImagenSprite(padre, nombre, sprite, false);
        }

        private static void Vestir(PiezasUI piezas, EstiloCyberfix estilo)
        {
            KitUI.FijarEstilo(estilo);
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                SerializedObject hud = new SerializedObject(raiz.GetComponent<HudAPie>());
                ConfigHudAPie config = (ConfigHudAPie)hud.FindProperty("config").objectReferenceValue;
                SerializedObject cfg = new SerializedObject(config);
                cfg.FindProperty("colorMarcaCalorApagada").colorValue = new Color(1f, 1f, 1f, 0f);
                cfg.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(config);

                VestirVida(raiz.transform.Find("Vida"), piezas);
                VestirCalor(raiz.transform.Find("Calor"), piezas);
                VestirDinero(raiz.transform.Find("Dinero"), piezas, hud);
                VestirArma(raiz.transform.Find("Arma"), piezas, hud);
                hud.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        private static void VestirVida(Transform grupo, PiezasUI piezas)
        {
            Image marco = Nueva(grupo, "Marco", piezas.BarraVida);
            KitUI.Estirar(marco.rectTransform);
            marco.transform.SetAsFirstSibling();
            Anclar((RectTransform)grupo.Find("Hueco"), 0.168f, 0.22f, 0.972f, 0.76f);
            RectTransform texto = (RectTransform)grupo.Find("Texto");
            Anclar(texto, 0.168f, 0.22f, 0.972f, 0.76f);
            texto.offsetMax = new Vector2(-14f, 0f);
            Text t = texto.GetComponent<Text>();
            t.alignment = TextAnchor.MiddleRight;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private static void VestirCalor(Transform grupo, PiezasUI piezas)
        {
            Transform barraVieja = grupo.Find("Barra");
            for (int i = 0; barraVieja != null && i < 3; i++)
            {
                Transform m = barraVieja.Find("Marca" + (i + 1));
                if (m != null)
                {
                    m.SetParent(grupo, false);
                }
            }
            Image barra = Nueva(grupo, "Barra", piezas.BarraCalor3);
            float fraccion = AnchoBarraCalor / DisposicionHud.AnchoCalor;
            Anclar(barra.rectTransform, 0f, 0f, fraccion, 1f);
            barra.transform.SetAsFirstSibling();
            for (int i = 0; i < 3; i++)
            {
                Transform marca = grupo.Find("Marca" + (i + 1));
                if (marca == null)
                {
                    marca = barra.transform.Find("Marca" + (i + 1));
                }
                marca.SetParent(barra.transform, false);
                float x0 = Segmento0 + PasoSegmento * i;
                Anclar((RectTransform)marca, x0 / AnchoCompuesto, 1f - SegmentoAbajo / AltoCompuesto, (x0 + AnchoSegmento) / AnchoCompuesto, 1f - SegmentoArriba / AltoCompuesto);
                Image img = marca.GetComponent<Image>();
                img.sprite = piezas.CalorRelleno;
                img.type = Image.Type.Simple;
            }
            RectTransform texto = (RectTransform)grupo.Find("Texto");
            Anclar(texto, fraccion + 0.02f, 0f, 1f, 1f);
            texto.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        }

        private static void VestirDinero(Transform grupo, PiezasUI piezas, SerializedObject hud)
        {
            Transform previo = grupo.Find("Marco");
            if (previo != null)
            {
                Object.DestroyImmediate(previo.gameObject);
            }
            Image panel = KitUI.CrearPanelMarco(grupo, "Marco");
            KitUI.Estirar(panel.rectTransform);
            panel.transform.SetAsFirstSibling();
            Image simbolo = Nueva(grupo, "Simbolo", piezas.SimboloBiits);
            simbolo.preserveAspect = true;
            Anclar(simbolo.rectTransform, 0.02f, 0.08f, 0.22f, 0.92f);
            grupo.Find("Texto").GetComponent<Text>().enabled = false;
            Transform vieja = grupo.Find("Cifra");
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja.gameObject);
            }
            Text cifra = KitUI.CrearTexto(grupo, "Cifra", RolTexto.Hud, TextAnchor.MiddleLeft);
            cifra.fontSize = 36;
            Anclar(cifra.rectTransform, 0.25f, 0f, 1f, 1f);
            cifra.rectTransform.offsetMax = new Vector2(-12f, 0f);
            hud.FindProperty("cifraDinero").objectReferenceValue = cifra;
        }

        private static void VestirArma(Transform grupo, PiezasUI piezas, SerializedObject hud)
        {
            Image marco = Nueva(grupo, "Marco", piezas.PanelArma);
            KitUI.Estirar(marco.rectTransform);
            marco.transform.SetAsFirstSibling();
            RectTransform texto = (RectTransform)grupo.Find("Texto");
            Anclar(texto, 0.44f, 0.5f, 0.92f, 0.9f);
            Text t = texto.GetComponent<Text>();
            t.alignment = TextAnchor.MiddleRight;
            Image icono = Nueva(grupo, "Icono", piezas.Arma(1));
            icono.preserveAspect = true;
            Anclar(icono.rectTransform, 0.09f, 0.5f, 0.45f, 0.9f);
            Image[] casillas = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                float x0 = (84f + 132f * i) / 807f;
                Image c = Nueva(grupo, "Casilla" + i, piezas.Arma(i));
                c.preserveAspect = true;
                Anclar(c.rectTransform, x0, 1f - 482f / 553f, x0 + 102f / 807f, 1f - 414f / 553f);
                c.rectTransform.offsetMin = new Vector2(6f, 6f);
                c.rectTransform.offsetMax = new Vector2(-6f, -6f);
                casillas[i] = c;
            }
            Image resalte = Nueva(grupo, "Resalte", null);
            resalte.enabled = true;
            resalte.color = Color.clear;
            Anclar(resalte.rectTransform, 0f, 0f, 0.1f, 0.1f);
            KitUI.CrearBorde(resalte.rectTransform, 3f, KitUI.Estilo.Naranja);
            hud.FindProperty("iconoArma").objectReferenceValue = icono;
            hud.FindProperty("resalteArma").objectReferenceValue = resalte;
            SerializedProperty lista = hud.FindProperty("casillasArma");
            lista.arraySize = casillas.Length;
            for (int i = 0; i < casillas.Length; i++)
            {
                lista.GetArrayElementAtIndex(i).objectReferenceValue = casillas[i];
            }
        }
    }
}

using System.Collections;
using MechaCyberpunk.UI.Inicio;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Historia
{
    /// <summary>
    /// Pantalla de aviso grande (T-689 "HAS MUERTO", T-690 "ARRESTADO"). T-1316: como las referencias M33 y M34, el juego se sigue viendo
    /// detrás, atenuado, con un panel de esquinas cortadas en el centro: título en Teko con neón y glitch, el detalle debajo (puede traer
    /// una segunda línea con '\n' y color con texto enriquecido) y, si se pide, una viñeta del color del aviso en las orillas.
    /// Tras unos segundos ya visible el fondo se cubre del todo, para que no se vea mover al jugador. Se arma por código en su propio
    /// canvas (debajo del fundido) y usa tiempo sin escala.
    /// </summary>
    public class PantallaAviso : MonoBehaviour
    {
        /// <summary>Rojo del aviso HAS MUERTO (M34).</summary>
        public static readonly Color RojoMuerte = new Color(1f, 0.24f, 0.28f, 1f);

        /// <summary>V12-B: nombre del Canvas (antes "Canvas"), para que VR (ReglaLienzosVR) lo reconozca por nombre.</summary>
        public const string NombreCanvas = "CanvasPantallaAviso";

        private const int CantidadCortes = 7;
        private const int TamanoTitulo = 150;
        private const int TamanoDetalle = 54;
        private const int TamanoSegundaLinea = 40;
        private const float AnchoPanel = 1240f;
        private const float AltoPanel = 430f;
        private const float AlturaCentro = 0.54f;
        private const float SeparacionReposo = 3f;
        private const float SeparacionGlitch = 22f;
        private const float AlfaCopias = 0.5f;
        private const float EsperaGlitchMinima = 0.5f;
        private const float EsperaGlitchMaxima = 1.6f;
        private const float DuracionGlitch = 0.3f;
        private const float AlfaFondoVisible = 0.55f;
        private const float AlfaRellenoPanel = 0.72f;
        private const float SegundosAntesDeCubrir = 1.7f;
        private const float SegundosCubrir = 0.45f;
        private const int LadoVineta = 128;
        private const float FraccionColorVineta = 0.6f;

        private static Sprite spriteVineta;

        private Image fondo;
        private Image vineta;
        private Image panel;
        private Image borde;
        private Image bordeHalo;
        private Text principal;
        private Text copiaAcento;
        private Text copiaCian;
        private Text detalle;
        private Image[] cortes;
        private Color acento;
        private float fuerzaVineta;
        private RelojGlitch reloj;
        private System.Random azar;
        private float tiempo;
        private float visibilidad;
        private float segundosVisible;

        public string Titulo => principal != null ? principal.text : string.Empty;
        public string Detalle => detalle != null ? detalle.text : string.Empty;
        public Text TextoPrincipal => principal;
        /// <summary>0 invisible, 1 totalmente visible con el título encendido.</summary>
        public float Visibilidad => visibilidad;
        public bool EnRafaga => reloj != null && reloj.EnRafaga;
        /// <summary>Cuánto tapa el fondo la escena: menos de 1 mientras se ve el juego detrás, 1 cuando ya la cubrió.</summary>
        public float AlfaFondo => fondo != null ? fondo.color.a : 0f;

        /// <summary>Crea la pantalla (invisible). Llamar a <see cref="Aparecer"/> para mostrarla.</summary>
        /// <param name="fuerzaVineta">0 sin viñeta; 1 orillas fuertes del color del aviso (HAS MUERTO).</param>
        public static PantallaAviso Crear(string titulo, string subtitulo, Color color, float fuerzaVineta = 0f)
        {
            GameObject raiz = new GameObject("PantallaAviso_" + titulo);
            PantallaAviso pantalla = raiz.AddComponent<PantallaAviso>();
            pantalla.Armar(titulo, subtitulo, color, Mathf.Clamp01(fuerzaVineta));
            return pantalla;
        }

        private void Armar(string titulo, string subtitulo, Color color, float vinetaPedida)
        {
            acento = color;
            fuerzaVineta = vinetaPedida;
            azar = new System.Random(689);
            reloj = new RelojGlitch(() => (float)azar.NextDouble(), EsperaGlitchMinima, EsperaGlitchMaxima, DuracionGlitch);
            Canvas canvas = KitUI.CrearCanvas(transform, NombreCanvas, PantallaFundido.OrdenDibujo - 1);
            canvas.overrideSorting = true;
            canvas.sortingOrder = PantallaFundido.OrdenDibujo - 1;
            RectTransform lienzo = (RectTransform)canvas.transform;

            fondo = UIInicio.Imagen(lienzo, "Fondo", UIInicio.ConAlfa(UIInicio.Noche, 0f));
            UIInicio.Estirar(fondo.rectTransform);
            fondo.raycastTarget = true;
            vineta = UIInicio.Imagen(lienzo, "Vineta", UIInicio.ConAlfa(acento, 0f), SpriteVineta());
            UIInicio.Estirar(vineta.rectTransform);

            RectTransform centro = UIInicio.Nodo(lienzo, "Centro");
            UIInicio.Punto(centro, new Vector2(0.5f, AlturaCentro), Vector2.zero, new Vector2(AnchoPanel, AltoPanel));
            panel = UIInicio.Imagen(centro, "Panel", UIInicio.ConAlfa(UIInicio.Noche, 0f), SpritesInicio.Panel);
            panel.type = Image.Type.Sliced;
            UIInicio.Estirar(panel.rectTransform);
            bordeHalo = UIInicio.Imagen(centro, "BordeHalo", UIInicio.ConAlfa(acento, 0f), SpritesInicio.PanelBorde, true);
            bordeHalo.type = Image.Type.Sliced;
            UIInicio.Estirar(bordeHalo.rectTransform);
            bordeHalo.rectTransform.offsetMin = new Vector2(-6f, -6f);
            bordeHalo.rectTransform.offsetMax = new Vector2(6f, 6f);
            borde = UIInicio.Imagen(centro, "Borde", UIInicio.ConAlfa(acento, 0f), SpritesInicio.PanelBorde);
            borde.type = Image.Type.Sliced;
            UIInicio.Estirar(borde.rectTransform);

            Font fuente = UIInicio.FuenteTitulos;
            RectTransform zonaTitulo = UIInicio.Nodo(centro, "ZonaTitulo");
            UIInicio.Anclar(zonaTitulo, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(0f, TamanoTitulo + 20f));
            copiaAcento = UIInicio.Texto(zonaTitulo, "CopiaAcento", titulo, fuente, TamanoTitulo, UIInicio.ConAlfa(acento, 0f), TextAnchor.MiddleCenter, true);
            copiaCian = UIInicio.Texto(zonaTitulo, "CopiaCian", titulo, fuente, TamanoTitulo, UIInicio.ConAlfa(UIInicio.Cian, 0f), TextAnchor.MiddleCenter, true);
            principal = UIInicio.Texto(zonaTitulo, "Principal", titulo, fuente, TamanoTitulo, UIInicio.ConAlfa(ColorTitulo(), 0f), TextAnchor.MiddleCenter);
            foreach (Text t in new[] { copiaAcento, copiaCian, principal })
            {
                UIInicio.Estirar(t.rectTransform);
            }
            Outline brillo = principal.gameObject.AddComponent<Outline>();
            brillo.effectColor = UIInicio.ConAlfa(acento, 0.35f);
            brillo.effectDistance = new Vector2(2f, 2f);

            detalle = UIInicio.Texto(centro, "Detalle", subtitulo, UIInicio.FuenteTexto, TamanoDetalle, UIInicio.ConAlfa(UIInicio.Blanco, 0f), TextAnchor.UpperCenter);
            detalle.supportRichText = true;
            detalle.lineSpacing = 1.15f;
            UIInicio.Anclar(detalle.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(-80f, AltoPanel - TamanoTitulo - 110f));
            detalle.text = FormatearDetalle(subtitulo);

            cortes = new Image[CantidadCortes];
            for (int i = 0; i < CantidadCortes; i++)
            {
                Image corte = UIInicio.Imagen(centro, "Corte" + i, UIInicio.ConAlfa(i % 2 == 0 ? UIInicio.Cian : acento, 0f), SpritesInicio.Blanco, true);
                UIInicio.Punto(corte.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 4f));
                cortes[i] = corte;
            }
        }

        /// <summary>El título es casi blanco teñido del color del aviso (cian claro en ARRESTADO, rojo en HAS MUERTO).</summary>
        private Color ColorTitulo() => Color.Lerp(UIInicio.Blanco, acento, 0.45f);

        /// <summary>La primera línea va grande; desde el primer '\n' va más chica y suave.</summary>
        private static string FormatearDetalle(string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return string.Empty;
            }
            int corte = texto.IndexOf('\n');
            if (corte < 0)
            {
                return texto;
            }
            return texto.Substring(0, corte) + "\n<size=" + TamanoSegundaLinea + ">" + texto.Substring(corte + 1) + "</size>";
        }

        /// <summary>Viñeta de las orillas: transparente en el centro y opaca en las esquinas.</summary>
        private static Sprite SpriteVineta()
        {
            if (spriteVineta != null)
            {
                return spriteVineta;
            }
            Texture2D textura = new Texture2D(LadoVineta, LadoVineta, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "VinetaAviso" };
            Color32[] pixeles = new Color32[LadoVineta * LadoVineta];
            for (int y = 0; y < LadoVineta; y++)
            {
                for (int x = 0; x < LadoVineta; x++)
                {
                    float dx = (x + 0.5f) / LadoVineta * 2f - 1f;
                    float dy = (y + 0.5f) / LadoVineta * 2f - 1f;
                    float distancia = Mathf.Sqrt(dx * dx * 0.8f + dy * dy * 1.2f);
                    byte a = (byte)(Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.85f, 1.45f, distancia)) * 255f);
                    pixeles[y * LadoVineta + x] = new Color32(255, 255, 255, a);
                }
            }
            textura.SetPixels32(pixeles);
            textura.Apply();
            spriteVineta = Sprite.Create(textura, new Rect(0, 0, LadoVineta, LadoVineta), new Vector2(0.5f, 0.5f));
            return spriteVineta;
        }

        /// <summary>Entra y enciende el título como un letrero de neón.</summary>
        public IEnumerator Aparecer(float segundos)
        {
            float transcurrido = 0f;
            while (transcurrido < segundos)
            {
                transcurrido += Time.unscaledDeltaTime;
                visibilidad = Mathf.Clamp01(transcurrido / Mathf.Max(0.01f, segundos));
                yield return null;
            }
            visibilidad = 1f;
        }

        /// <summary>Se apaga hasta quedar invisible y se destruye.</summary>
        public IEnumerator Desvanecer(float segundos)
        {
            float inicio = visibilidad;
            float transcurrido = 0f;
            while (transcurrido < segundos)
            {
                transcurrido += Time.unscaledDeltaTime;
                visibilidad = Mathf.Lerp(inicio, 0f, Mathf.Clamp01(transcurrido / Mathf.Max(0.01f, segundos)));
                yield return null;
            }
            Cerrar();
        }

        /// <summary>Ya visible del todo, sin animación de entrada.</summary>
        public void MostrarYa() => visibilidad = 1f;

        /// <summary>Quita la pantalla (destruye el objeto).</summary>
        public void Cerrar()
        {
            Destroy(gameObject);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            tiempo += dt;
            segundosVisible = visibilidad >= 1f ? segundosVisible + dt : 0f;
            reloj.Actualizar(dt);
            Aplicar();
        }

        private void Aplicar()
        {
            if (principal == null)
            {
                return;
            }
            float v = visibilidad;
            float parpadeo = 1f;
            if (v < 1f)
            {
                float onda = Mathf.Sin(tiempo * 47f) * Mathf.Sin(tiempo * 13f + 2f);
                parpadeo = v < 0.2f ? 0f : (onda > -0.3f + v * 0.9f ? 1f : 0.15f);
            }
            float respiracion = 0.85f + 0.15f * Mathf.Sin(tiempo * 2f);
            float intensidad = reloj.Intensidad;
            float separacion = SeparacionReposo + SeparacionGlitch * intensidad;
            float temblor = intensidad > 0f ? (float)(azar.NextDouble() * 2.0 - 1.0) : 0f;

            // El juego se ve detrás, atenuado; tras un rato ya visible se cubre del todo (ahí se mueve al jugador).
            float cubierto = Mathf.Clamp01((segundosVisible - SegundosAntesDeCubrir) / SegundosCubrir);
            fondo.color = UIInicio.ConAlfa(UIInicio.Noche, Mathf.Lerp(AlfaFondoVisible * Mathf.Clamp01(v * 1.4f), 1f, cubierto));
            // Rojo oscuro en las orillas, no el acento puro (M34).
            vineta.color = UIInicio.ConAlfa(Color.Lerp(Color.black, acento, FraccionColorVineta), fuerzaVineta * v * respiracion);

            float alfaPanel = Mathf.Clamp01(v * 1.6f);
            panel.color = UIInicio.ConAlfa(Color.Lerp(UIInicio.Noche, acento, 0.08f), AlfaRellenoPanel * alfaPanel);
            borde.color = UIInicio.ConAlfa(acento, 0.9f * alfaPanel * (intensidad > 0f && azar.NextDouble() < 0.3 ? 0.5f : 1f));
            bordeHalo.color = UIInicio.ConAlfa(acento, 0.35f * alfaPanel * respiracion);

            principal.color = UIInicio.ConAlfa(ColorTitulo(), parpadeo * v * (intensidad > 0f && azar.NextDouble() < 0.25 ? 0.6f : 1f));
            principal.rectTransform.anchoredPosition = new Vector2(temblor * 8f * intensidad, 0f);
            copiaAcento.rectTransform.anchoredPosition = new Vector2(-separacion, separacion * 0.25f * temblor);
            copiaCian.rectTransform.anchoredPosition = new Vector2(separacion, -separacion * 0.25f * temblor);
            copiaAcento.color = UIInicio.ConAlfa(acento, AlfaCopias * parpadeo * v);
            copiaCian.color = UIInicio.ConAlfa(UIInicio.Cian, AlfaCopias * 0.6f * parpadeo * v * intensidad);

            for (int i = 0; i < cortes.Length; i++)
            {
                Image corte = cortes[i];
                if (intensidad > 0f && v >= 1f && azar.NextDouble() < 0.7)
                {
                    // Líneas de glitch que se salen del panel por los lados, como en M34.
                    float x = ((float)azar.NextDouble() - 0.5f) * (AnchoPanel + 400f);
                    float y = ((float)azar.NextDouble() - 0.5f) * AltoPanel;
                    corte.rectTransform.anchoredPosition = new Vector2(x, y);
                    corte.rectTransform.sizeDelta = new Vector2(80f + (float)azar.NextDouble() * 420f, 2f + (float)azar.NextDouble() * 6f);
                    corte.color = UIInicio.ConAlfa(corte.color, 0.55f * intensidad);
                }
                else
                {
                    corte.color = UIInicio.ConAlfa(corte.color, 0f);
                }
            }
            detalle.color = UIInicio.ConAlfa(Color.Lerp(UIInicio.Blanco, acento, 0.25f), Mathf.Clamp01((v - 0.4f) / 0.6f));
        }
    }
}

using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.UI.Mapa;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// Mira del sniper (T-416): mientras dura el zoom, pantalla negra con un círculo transparente del 80 % del alto, una cruz fina turquesa
    /// y la distancia al punto apuntado abajo ("82 m"). Va en un objeto de la escena (busca al <see cref="ZoomSniper"/> del jugador); crea su propio canvas (orden 16).
    /// T-408 la pasa al kit de UI.
    /// </summary>
    public class MiraSniper : MonoBehaviour
    {
        public const int OrdenCanvas = 16;
        private const int LadoTextura = 512;
        private const float FraccionCirculo = 0.8f;
        private const float AlcanceMaximo = 300f;
        private static readonly Color Turquesa = new Color(0.1f, 0.9f, 1f);

        private ZoomSniper zoom;
        private ControladorAPie controlador;
        private Canvas canvas;
        private RectTransform cuadrado;
        private RectTransform barraIzquierda;
        private RectTransform barraDerecha;
        private RectTransform cruzVertical;
        private RectTransform cruzHorizontal;
        private Text textoDistancia;
        private Texture2D textura;
        private readonly RaycastHit[] impactos = new RaycastHit[16];

        /// <summary>True mientras la mira está prendida.</summary>
        public bool Visible => canvas != null && canvas.enabled;
        public string TextoDistancia => textoDistancia != null ? textoDistancia.text : string.Empty;

        private void Awake()
        {
            canvas = FormatoTerritorio.CrearCanvas(transform, "CanvasMiraSniper", OrdenCanvas);
            textura = CrearTextura();
            cuadrado = CrearImagen("Circulo", canvas.transform, Color.white, textura);
            barraIzquierda = CrearImagen("BarraIzquierda", canvas.transform, Color.black, null);
            barraDerecha = CrearImagen("BarraDerecha", canvas.transform, Color.black, null);
            cruzVertical = CrearImagen("CruzVertical", cuadrado, Turquesa, null);
            cruzHorizontal = CrearImagen("CruzHorizontal", cuadrado, Turquesa, null);
            textoDistancia = FormatoTerritorio.CrearTexto(canvas.transform, "Distancia", 22, TextAnchor.LowerCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(300f, 40f));
            textoDistancia.color = Turquesa;
            canvas.enabled = false;
        }

        private void OnDestroy()
        {
            if (textura != null)
            {
                Destroy(textura);
            }
        }

        private void Update()
        {
            if (zoom == null)
            {
                zoom = FindFirstObjectByType<ZoomSniper>();
                controlador = zoom != null ? zoom.GetComponent<ControladorAPie>() : null;
            }
            bool prendida = zoom != null && zoom.Activo;
            if (canvas.enabled != prendida)
            {
                canvas.enabled = prendida;
            }
            if (!prendida)
            {
                return;
            }
            // El cuadrado del círculo mide el alto de la pantalla; las barras negras cubren el ancho que sobra.
            RectTransform raiz = (RectTransform)canvas.transform;
            float alto = raiz.rect.height;
            float sobra = Mathf.Max(0f, (raiz.rect.width - alto) * 0.5f);
            cuadrado.sizeDelta = new Vector2(alto, alto);
            cruzVertical.sizeDelta = new Vector2(2f, alto * FraccionCirculo);
            cruzHorizontal.sizeDelta = new Vector2(alto * FraccionCirculo, 2f);
            barraIzquierda.sizeDelta = new Vector2(sobra + 2f, alto);
            barraIzquierda.anchoredPosition = new Vector2(-(alto * 0.5f + sobra * 0.5f), 0f);
            barraDerecha.sizeDelta = new Vector2(sobra + 2f, alto);
            barraDerecha.anchoredPosition = new Vector2(alto * 0.5f + sobra * 0.5f, 0f);
            textoDistancia.text = TextoDeDistancia(Distancia());
        }

        /// <summary>"82 m", o "--" si no hay nada a la vista.</summary>
        public static string TextoDeDistancia(float metros)
        {
            return metros < 0f ? "--" : Mathf.RoundToInt(metros) + " m";
        }

        // Distancia al primer collider que no sea del jugador, a lo largo del centro de la cámara; -1 si no hay.
        private float Distancia()
        {
            Transform camara = controlador.Camara;
            int cantidad = Physics.RaycastNonAlloc(camara.position, camara.forward, impactos, AlcanceMaximo, ~0, QueryTriggerInteraction.Ignore);
            float mejor = -1f;
            for (int i = 0; i < cantidad; i++)
            {
                RaycastHit impacto = impactos[i];
                if (impacto.collider.transform.root == controlador.transform.root)
                {
                    continue;
                }
                if (mejor < 0f || impacto.distance < mejor)
                {
                    mejor = impacto.distance;
                }
            }
            return mejor;
        }

        private static RectTransform CrearImagen(string nombre, Transform padre, Color color, Texture2D textura)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padre, false);
            RectTransform rect = (RectTransform)objeto.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            if (textura != null)
            {
                RawImage imagen = objeto.AddComponent<RawImage>();
                imagen.texture = textura;
                imagen.color = color;
                imagen.raycastTarget = false;
            }
            else
            {
                Image imagen = objeto.AddComponent<Image>();
                imagen.color = color;
                imagen.raycastTarget = false;
            }
            return rect;
        }

        // Cuadrado negro con un círculo transparente de FraccionCirculo del lado y borde suave.
        private static Texture2D CrearTextura()
        {
            Texture2D t = new Texture2D(LadoTextura, LadoTextura, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "MiraSniper_Circulo"
            };
            float radio = LadoTextura * FraccionCirculo * 0.5f;
            float centro = (LadoTextura - 1) * 0.5f;
            Color32[] pixeles = new Color32[LadoTextura * LadoTextura];
            for (int y = 0; y < LadoTextura; y++)
            {
                for (int x = 0; x < LadoTextura; x++)
                {
                    float d = Mathf.Sqrt((x - centro) * (x - centro) + (y - centro) * (y - centro));
                    float alfa = Mathf.Clamp01(d - radio + 0.5f);
                    pixeles[y * LadoTextura + x] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(alfa * 255f));
                }
            }
            t.SetPixels32(pixeles);
            t.Apply();
            return t;
        }
    }
}

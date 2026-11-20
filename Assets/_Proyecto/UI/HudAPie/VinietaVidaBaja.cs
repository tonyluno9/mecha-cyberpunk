using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// T-1489: viñeta roja en los bordes de la pantalla cuando la vida baja de <see cref="ReglaVinietaVida.FraccionUmbral"/>: pulsa suave y se
    /// intensifica cuanto menos vida; se desvanece al curarse. Es una imagen UI en un canvas propio (no toca el Volume ni los perfiles de noche).
    /// La textura radial se genera por código. La crea <see cref="HudAPie"/>. Lee la vida del jugador registrado (IVida de su raíz).
    /// </summary>
    public class VinietaVidaBaja : MonoBehaviour
    {
        public const int OrdenCanvas = 15;
        private const int LadoTextura = 128;
        /// <summary>Opacidad por segundo con que la viñeta sigue a su objetivo.</summary>
        private const float VelocidadAlfa = 2.5f;
        private const float RadioInterior = 0.45f;

        private static readonly Color Rojo = new Color(0.85f, 0.03f, 0.05f, 1f);

        private Canvas canvas;
        private Image imagen;
        private IVida vida;
        private float alfa;

        /// <summary>Opacidad actual (para pruebas).</summary>
        public float AlfaActual => alfa;

        private void Awake()
        {
            canvas = KitUI.CrearCanvas(transform, "CanvasVinietaVida", OrdenCanvas);
            imagen = KitUI.CrearImagen(canvas.transform, "Vinieta", new Color(Rojo.r, Rojo.g, Rojo.b, 0f));
            RectTransform r = imagen.rectTransform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            Texture2D textura = CrearTextura();
            imagen.sprite = Sprite.Create(textura, new Rect(0f, 0f, LadoTextura, LadoTextura), new Vector2(0.5f, 0.5f));
            imagen.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (imagen != null && imagen.sprite != null)
            {
                Destroy(imagen.sprite.texture);
                Destroy(imagen.sprite);
            }
        }

        private void Update()
        {
            if (vida == null || (vida is Object o && o == null))
            {
                IJugador jugador = Registro<IJugador>.Actual;
                vida = jugador != null && jugador.Raiz != null ? jugador.Raiz.GetComponent<VidaAPie>() : null;
            }
            float objetivo = vida != null ? ReglaVinietaVida.Alfa(vida.Actual, vida.Maxima, Time.unscaledTime) : 0f;
            alfa = Mathf.MoveTowards(alfa, objetivo, VelocidadAlfa * Time.unscaledDeltaTime);
            if (alfa <= 0.001f)
            {
                alfa = 0f;
                if (imagen.gameObject.activeSelf)
                {
                    imagen.gameObject.SetActive(false);
                }
                return;
            }
            if (!imagen.gameObject.activeSelf)
            {
                imagen.gameObject.SetActive(true);
            }
            imagen.color = new Color(Rojo.r, Rojo.g, Rojo.b, alfa);
        }

        // Transparente en el centro y rojo hacia las esquinas.
        private static Texture2D CrearTextura()
        {
            Texture2D t = new Texture2D(LadoTextura, LadoTextura, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            Color32[] pixeles = new Color32[LadoTextura * LadoTextura];
            float centro = (LadoTextura - 1) * 0.5f;
            for (int y = 0; y < LadoTextura; y++)
            {
                for (int x = 0; x < LadoTextura; x++)
                {
                    float dx = (x - centro) / centro;
                    float dy = (y - centro) / centro;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.4142f; // 0 centro, 1 esquina
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(RadioInterior, 1f, d));
                    pixeles[y * LadoTextura + x] = new Color(1f, 1f, 1f, a);
                }
            }
            t.SetPixels32(pixeles);
            t.Apply(false, true);
            return t;
        }
    }
}

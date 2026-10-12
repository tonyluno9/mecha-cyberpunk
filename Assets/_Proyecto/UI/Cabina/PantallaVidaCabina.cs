using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Cabina
{
    /// <summary>Pantalla de vida del tablero: una barra y el texto "VIDA n". Se actualiza con el evento Cambio.</summary>
    public class PantallaVidaCabina : MonoBehaviour
    {
        [Tooltip("Objeto con el IVida del mecha (la raíz del mecha).")]
        [SerializeField] private GameObject fuenteVida;
        [SerializeField] private Image relleno;
        [SerializeField] private Text texto;
        [SerializeField] private Image fondo;
        [SerializeField] private Image hueco;
        [SerializeField] private ConfigPantallaVida config;

        private IVida vida;

        public Image Relleno => relleno;
        public Text Texto => texto;

        private void Awake()
        {
            if (fuenteVida != null)
            {
                vida = fuenteVida.GetComponent<IVida>();
            }
            if (vida == null || relleno == null || texto == null || config == null)
            {
                Debug.LogError("[UI] Faltan referencias en PantallaVidaCabina de " + name, this);
                enabled = false;
                return;
            }

            // Una Image con relleno necesita sprite: se usa un blanco liso.
            if (relleno.sprite == null)
            {
                Texture2D blanco = Texture2D.whiteTexture;
                relleno.sprite = Sprite.Create(blanco, new Rect(0f, 0f, blanco.width, blanco.height), new Vector2(0.5f, 0.5f));
            }
            relleno.type = Image.Type.Filled;
            relleno.fillMethod = Image.FillMethod.Horizontal;
            relleno.fillOrigin = (int)Image.OriginHorizontal.Left;

            if (fondo != null)
            {
                fondo.color = config.ColorFondo;
            }
            if (hueco != null)
            {
                hueco.color = config.ColorHueco;
            }
        }

        private void OnEnable()
        {
            if (vida == null)
            {
                return;
            }
            vida.Cambio += Actualizar;
            Actualizar();
        }

        private void OnDisable()
        {
            if (vida != null)
            {
                vida.Cambio -= Actualizar;
            }
        }

        private void Actualizar()
        {
            float proporcion = vida.Maxima > 0f ? Mathf.Clamp01(vida.Actual / vida.Maxima) : 0f;
            Color color = proporcion <= config.UmbralVidaBaja ? config.ColorBajo : config.ColorNormal;
            relleno.fillAmount = proporcion;
            relleno.color = color;
            texto.color = color;
            texto.text = "VIDA " + Mathf.CeilToInt(vida.Actual);
        }
    }
}

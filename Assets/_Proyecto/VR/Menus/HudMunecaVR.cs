using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.VR.Menus
{
    /// <summary>
    /// VR4: el HUD de la muñeca izquierda. Un panel chico (con el kit de UI) sobre el control izquierdo con la vida y el dinero,
    /// leídos de los servicios reales (<see cref="IVida"/> del jugador y <see cref="ICartera"/> del registro). Se ve al voltear la muñeca.
    /// </summary>
    public sealed class HudMunecaVR
    {
        public const string NombreCanvas = "CanvasMunecaVR";
        public const string TextoVida = "VIDA";
        public const string TextoDinero = "$";
        private const float AnchoPx = 420f;
        private const float AltoPx = 140f;
        private const float RellenoPx = 18f;
        /// <summary>Tamaño de un px del panel (m): 420 px = 21 cm.</summary>
        private const float MetrosPorPx = 0.0005f;
        private const int Orden = 10;
        private const float SegundosRefresco = 0.2f;
        /// <summary>Sobre el dorso, un poco atrás del control (m, espacio del control).</summary>
        private static readonly Vector3 PosicionLocal = new Vector3(0f, 0.045f, -0.1f);
        /// <summary>Inclinado hacia adelante para leerse desde arriba con la muñeca de lado (°).</summary>
        private static readonly Vector3 GiroLocal = new Vector3(60f, 0f, 0f);

        private Canvas canvas;
        private Text texto;
        private Transform mano;
        private float segundos;

        public Canvas Canvas => canvas;
        public bool Existe => canvas != null;
        public Transform Mano => mano;
        public string Texto => texto != null ? texto.text : string.Empty;

        /// <summary>Crea el panel colgado del control izquierdo (si ya estaba en otro, lo cambia).</summary>
        public void Crear(Transform nuevaMano)
        {
            Destruir();
            if (nuevaMano == null)
            {
                return;
            }
            mano = nuevaMano;
            canvas = KitUI.CrearCanvas(null, NombreCanvas, Orden);
            canvas.renderMode = RenderMode.WorldSpace;
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = false;
            }
            var rect = (RectTransform)canvas.transform;
            rect.SetParent(mano, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(AnchoPx, AltoPx);
            rect.localPosition = PosicionLocal;
            rect.localRotation = Quaternion.Euler(GiroLocal);
            rect.localScale = Vector3.one * MetrosPorPx;
            RectTransform panel = KitUI.CrearPanel(rect, "Fondo", new Vector2(AnchoPx, AltoPx));
            texto = KitUI.CrearTexto(panel, "Texto", RolTexto.Hud, TextAnchor.MiddleLeft);
            texto.rectTransform.offsetMin = new Vector2(RellenoPx, RellenoPx);
            texto.rectTransform.offsetMax = new Vector2(-RellenoPx, -RellenoPx);
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            segundos = 0f;
        }

        /// <summary>Refresca vida y dinero unas veces por segundo (tiempo real: también en pausa).</summary>
        public void Actualizar(float dt, IVida vida, ICartera cartera)
        {
            if (texto == null)
            {
                return;
            }
            segundos -= dt;
            if (segundos > 0f)
            {
                return;
            }
            segundos = SegundosRefresco;
            texto.text = Formatear(vida, cartera);
        }

        /// <summary>"VIDA 80 / 100" y "$ 1,234" en dos renglones.</summary>
        public static string Formatear(IVida vida, ICartera cartera)
        {
            string lineaVida = vida != null
                ? TextoVida + " " + Mathf.CeilToInt(vida.Actual) + " / " + Mathf.CeilToInt(vida.Maxima)
                : TextoVida + " --";
            string lineaDinero = cartera != null ? TextoDinero + " " + cartera.Dinero.ToString("N0") : TextoDinero + " --";
            return lineaVida + "\n" + lineaDinero;
        }

        public void Destruir()
        {
            if (canvas != null)
            {
                Object.Destroy(canvas.gameObject);
            }
            canvas = null;
            texto = null;
            mano = null;
        }
    }
}

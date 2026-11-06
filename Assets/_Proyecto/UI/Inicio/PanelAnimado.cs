using UnityEngine;

namespace MechaCyberpunk.UI.Inicio
{
    /// <summary>
    /// Un grupo de la pantalla de inicio que entra y sale deslizándose y desvaneciéndose (transición entre el menú, las opciones y la
    /// creación). Mientras está oculto no recibe clics ni navegación; al ocultarse del todo se desactiva el objeto.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class PanelAnimado : MonoBehaviour
    {
        [Tooltip("Desplazamiento (px) desde el que entra y hacia el que sale.")]
        [SerializeField] private Vector2 desplazamiento = new Vector2(-90f, 0f);
        [Min(0.01f)] [Tooltip("Segundos de la transición.")]
        [SerializeField] private float segundos = 0.35f;

        private CanvasGroup grupo;
        private RectTransform rect;
        private Vector2 posicionBase;
        private bool tomadaBase;
        private float visibilidad;
        private bool deseado;
        private float retraso;

        /// <summary>0 oculto, 1 totalmente visible (animado).</summary>
        public float Visibilidad => visibilidad;
        public bool Deseado => deseado;
        public bool EstaVisible => visibilidad > 0.001f;

        public static PanelAnimado Agregar(RectTransform rect, Vector2 desplazamientoEntrada, float segundosTransicion)
        {
            CanvasGroup g = rect.gameObject.GetComponent<CanvasGroup>();
            if (g == null)
            {
                g = rect.gameObject.AddComponent<CanvasGroup>();
            }
            PanelAnimado panel = rect.gameObject.AddComponent<PanelAnimado>();
            panel.desplazamiento = desplazamientoEntrada;
            panel.segundos = Mathf.Max(0.01f, segundosTransicion);
            return panel;
        }

        private void Awake()
        {
            Preparar();
        }

        private void Preparar()
        {
            if (grupo != null)
            {
                return;
            }
            grupo = GetComponent<CanvasGroup>();
            rect = (RectTransform)transform;
            posicionBase = rect.anchoredPosition;
            tomadaBase = true;
        }

        /// <summary>Muestra u oculta con animación. <paramref name="espera"/> retrasa el inicio (escalonar elementos).</summary>
        public void Fijar(bool visible, float espera = 0f)
        {
            Preparar();
            deseado = visible;
            retraso = espera;
            if (visible)
            {
                gameObject.SetActive(true);
            }
            Aplicar();
        }

        /// <summary>Sin animación.</summary>
        public void FijarYa(bool visible)
        {
            Preparar();
            deseado = visible;
            visibilidad = visible ? 1f : 0f;
            retraso = 0f;
            gameObject.SetActive(visible || false);
            Aplicar();
        }

        private void Update()
        {
            if (retraso > 0f)
            {
                retraso -= Time.unscaledDeltaTime;
                return;
            }
            float objetivo = deseado ? 1f : 0f;
            visibilidad = Mathf.MoveTowards(visibilidad, objetivo, Time.unscaledDeltaTime / segundos);
            Aplicar();
            if (!deseado && visibilidad <= 0f)
            {
                gameObject.SetActive(false);
            }
        }

        private void Aplicar()
        {
            if (!tomadaBase)
            {
                return;
            }
            float suave = visibilidad * visibilidad * (3f - 2f * visibilidad);
            grupo.alpha = suave;
            // Solo se interactúa con el que está llegando o ya llegó; el que se va deja de recibir clics de inmediato.
            grupo.interactable = deseado;
            grupo.blocksRaycasts = deseado;
            rect.anchoredPosition = posicionBase + desplazamiento * (1f - suave);
        }
    }
}

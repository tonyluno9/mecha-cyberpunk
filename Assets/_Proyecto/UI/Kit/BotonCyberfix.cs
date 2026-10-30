using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Kit
{
    /// <summary>
    /// Botón del kit (T-408) con los estados de la guía: normal (fondo negro, texto claro), elegido (borde naranja de 3 px, fondo naranja al
    /// 25 %, texto naranja y "›"), pulsado (fondo naranja al 60 %) y desactivado (texto gris, sin borde). El ratón encima lo elige.
    /// Suena UI_MOVER al elegirlo, UI_ACEPTAR al pulsarlo y UI_ERROR si está desactivado. El teclado lo maneja <see cref="MenuCyberfix"/>.
    /// </summary>
    public class BotonCyberfix : Selectable, IPointerClickHandler
    {
        public const string SonidoMover = "UI_MOVER";
        public const string SonidoAceptar = "UI_ACEPTAR";
        public const string SonidoError = "UI_ERROR";
        private const string PrefijoElegido = "› ";
        private const float SegundosPulsado = 0.12f;

        [SerializeField] private string etiqueta = string.Empty;
        [SerializeField] private Text texto;
        [SerializeField] private Text textoValor;
        [SerializeField] private Image fondo;
        [SerializeField] private GameObject borde;
        [SerializeField] private UnityEvent alPulsar = new UnityEvent();

        private float pulsadoHasta = -1f;
        private bool elegido;

        /// <summary>Si es true, elegir no suena (lo usa el menú al abrir).</summary>
        internal static bool Silencio { get; set; }

        public UnityEvent AlPulsar => alPulsar;
        public string Etiqueta => etiqueta;
        public bool EstaElegido => elegido;
        public Text Texto => texto;
        public Text TextoValor => textoValor;
        public bool EstaPulsado => Time.unscaledTime < pulsadoHasta || currentSelectionState == SelectionState.Pressed;

        /// <summary>Lo llama <see cref="KitUI"/> al crearlo.</summary>
        public void Iniciar(string nuevaEtiqueta, Text nuevoTexto, Text nuevoValor, Image nuevoFondo, GameObject nuevoBorde)
        {
            etiqueta = nuevaEtiqueta;
            texto = nuevoTexto;
            textoValor = nuevoValor;
            fondo = nuevoFondo;
            borde = nuevoBorde;
            targetGraphic = fondo;
            transition = Transition.None;
            navigation = new Navigation { mode = Navigation.Mode.None };
            Refrescar();
        }

        public void CambiarEtiqueta(string nueva)
        {
            etiqueta = nueva;
            Refrescar();
        }

        protected override void Awake()
        {
            base.Awake();
            transition = Transition.None;
            navigation = new Navigation { mode = Navigation.Mode.None };
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Refrescar();
        }

        private void Update()
        {
            if (pulsadoHasta > 0f && Time.unscaledTime >= pulsadoHasta)
            {
                pulsadoHasta = -1f;
                Refrescar();
            }
        }

        /// <summary>Lo elige (sin EventSystem también: el menú lleva su propia cuenta).</summary>
        public void Elegir()
        {
            if (!IsInteractable())
            {
                return;
            }
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != gameObject)
            {
                EventSystem.current.SetSelectedGameObject(gameObject);
            }
            else if (!elegido)
            {
                MarcarElegido(true);
            }
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            MarcarElegido(true);
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            MarcarElegido(false);
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            Elegir();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Pulsar();
            }
        }

        /// <summary>Enter, Espacio o clic. Desactivado suena UI_ERROR y no hace nada. Devuelve true si se pulsó.</summary>
        public bool Pulsar()
        {
            if (!IsInteractable())
            {
                Sonidos.Reproducir(SonidoError);
                return false;
            }
            Sonidos.Reproducir(SonidoAceptar);
            pulsadoHasta = Time.unscaledTime + SegundosPulsado;
            Refrescar();
            AlAceptar();
            alPulsar.Invoke();
            return true;
        }

        /// <summary>
        /// T-413: true si la fila escribe con el teclado (<see cref="CampoTextoCyberfix"/>) mientras está elegida; entonces el menú solo
        /// navega con las flechas y Enter (W, A, S, D y Espacio son letras).
        /// </summary>
        public virtual bool CapturaTeclado => false;

        /// <summary>← → o la rueda. Los botones no cambian nada; las filas de valor sí. Devuelve true si cambió.</summary>
        public virtual bool Cambiar(int direccion) => false;

        /// <summary>Lo que hace la fila al aceptarla (el conmutador cambia).</summary>
        protected virtual void AlAceptar()
        {
        }

        /// <summary>El texto de la derecha de una fila de valor.</summary>
        protected virtual string TextoDelValor() => string.Empty;

        internal void MarcarElegido(bool valor)
        {
            if (elegido == valor)
            {
                return;
            }
            elegido = valor;
            if (valor && !Silencio)
            {
                Sonidos.Reproducir(SonidoMover);
            }
            if (valor)
            {
                GetComponentInParent<MenuCyberfix>()?.AlElegir(this);
            }
            Refrescar();
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            Refrescar();
        }

        protected void Refrescar()
        {
            if (texto == null || fondo == null)
            {
                return;
            }
            EstiloCyberfix e = KitUI.Estilo;
            bool activo = IsInteractable();
            bool marcado = activo && elegido;
            if (borde != null && borde.activeSelf != marcado)
            {
                borde.SetActive(marcado);
            }
            fondo.color = !activo ? e.Fondo : EstaPulsado ? e.FondoPulsado : marcado ? e.FondoElegido : e.Fondo;
            texto.color = !activo ? e.Desactivado : marcado ? e.Naranja : e.Texto;
            texto.text = marcado ? PrefijoElegido + etiqueta : etiqueta;
            if (textoValor != null)
            {
                textoValor.text = TextoDelValor();
                textoValor.color = activo ? e.Turquesa : e.Desactivado;
            }
        }
    }
}

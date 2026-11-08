using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Inicio
{
    /// <summary>
    /// Botón de la pantalla de inicio: texto en Rajdhani que al estar elegido (con teclado, control o ratón encima) se corre a la
    /// derecha, se enciende, saca una punta de flecha a la izquierda y tiñe su fondo; al pulsarlo destella. Elegir por ratón y por teclado es lo mismo (el puntero encima elige el botón), así solo hay un resaltado a la vez.
    /// Suena UI_MOVER al elegirse y UI_ACEPTAR al pulsarse.
    /// </summary>
    public class BotonInicio : Selectable, ISubmitHandler, IPointerClickHandler
    {
        public const string SonidoMover = "UI_MOVER";
        public const string SonidoAceptar = "UI_ACEPTAR";
        public const string SonidoCancelar = "UI_CANCELAR";

        private const float SegundosEncender = 0.16f;
        private const float SegundosApagar = 0.22f;
        private const float VelocidadDestello = 4.5f;
        private const float DesplazamientoElegido = 14f;
        private const float AnchoBarra = 26f;
        private const float AlfaFondo = 0.06f;
        private const float AlfaHalo = 0.22f;

        [SerializeField] private Text texto;
        [SerializeField] private Text halo;
        [SerializeField] private Text subtexto;
        [SerializeField] private Image fondo;
        [SerializeField] private Image barra;
        [SerializeField] private Image destelloImagen;
        [SerializeField] private RectTransform contenido;
        [SerializeField] private Color colorAcento = new Color(0.10f, 0.92f, 0.98f, 1f);
        [SerializeField] private float desplazamiento = DesplazamientoElegido;

        private Color colorSubtexto = new Color(0.4f, 0.43f, 0.52f, 1f);
        private float encendido;
        private float destello;
        private bool elegido;
        private bool silenciarProximo;

        /// <summary>Se dispara al pulsarlo (Enter, A del control o clic).</summary>
        public UnityEvent AlPulsar { get; } = new UnityEvent();

        /// <summary>Se avisa cuando el botón pasa a ser el elegido.</summary>
        public event Action<BotonInicio> Elegido;

        public Text Texto => texto;

        public string Etiqueta
        {
            get => texto != null ? texto.text : string.Empty;
            set
            {
                if (texto != null) texto.text = value;
                if (halo != null) halo.text = value;
            }
        }

        public Text Subtexto => subtexto;

        public bool EstaElegido => elegido;

        /// <summary>0 apagado, 1 totalmente encendido (animado): lo que se ve del resaltado.</summary>
        public float Encendido => encendido;

        /// <summary>Elegido a mano (sin sonido) por el menú al abrir una pantalla.</summary>
        public void ElegirSinSonido()
        {
            silenciarProximo = true;
            Select();
            if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject != gameObject)
            {
                silenciarProximo = false;
            }
        }

        public static BotonInicio Crear(Transform padre, string nombre, string etiqueta, Vector2 tamano, int tamanoTexto, TextAnchor ancla, Color acento)
        {
            RectTransform raiz = UIInicio.Nodo(padre, nombre);
            raiz.sizeDelta = tamano;
            Image zonaRayos = raiz.gameObject.AddComponent<Image>();
            zonaRayos.color = Color.clear;
            zonaRayos.raycastTarget = true;
            BotonInicio boton = raiz.gameObject.AddComponent<BotonInicio>();
            boton.transition = Transition.None;
            boton.targetGraphic = zonaRayos;
            boton.colorAcento = acento;

            boton.fondo = UIInicio.Imagen(raiz, "Fondo", UIInicio.ConAlfa(acento, 0f), TrazoH10.Relleno);
            boton.fondo.type = Image.Type.Sliced;
            UIInicio.Anclar(boton.fondo.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            boton.destelloImagen = UIInicio.Imagen(raiz, "Destello", UIInicio.ConAlfa(Color.white, 0f), SpritesInicio.DegradadoH, true);
            UIInicio.Estirar(boton.destelloImagen.rectTransform);
            boton.barra = UIInicio.Imagen(raiz, "Barra", acento, TrazoH10.Cheuron);
            UIInicio.Anclar(boton.barra.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(AnchoBarra, AnchoBarra));

            boton.contenido = UIInicio.Nodo(raiz, "Contenido");
            UIInicio.Estirar(boton.contenido);
            boton.texto = UIInicio.Texto(boton.contenido, "Texto", etiqueta, TrazoH10.Fuente, tamanoTexto, UIInicio.TextoSuave, ancla);
            UIInicio.Estirar(boton.texto.rectTransform);
            boton.texto.rectTransform.offsetMin = new Vector2(34f, 0f);
            boton.texto.rectTransform.offsetMax = new Vector2(-20f, 0f);
            boton.halo = UIInicio.Halo(boton.texto, acento, 0f, 3f);
            boton.halo.rectTransform.offsetMin = boton.texto.rectTransform.offsetMin;
            boton.halo.rectTransform.offsetMax = boton.texto.rectTransform.offsetMax;
            return boton;
        }

        /// <summary>Agrega una línea chica bajo el texto (resumen de la partida, por ejemplo).</summary>
        public Text AgregarSubtexto(string contenidoTexto, int tamano, Color color)
        {
            colorSubtexto = color;
            subtexto = UIInicio.Texto(contenido, "Subtexto", contenidoTexto, UIInicio.FuenteTexto, tamano, color, TextAnchor.LowerLeft);
            UIInicio.Estirar(subtexto.rectTransform);
            subtexto.rectTransform.offsetMin = new Vector2(36f, 2f);
            subtexto.rectTransform.offsetMax = new Vector2(-20f, 0f);
            texto.rectTransform.offsetMax = new Vector2(-20f, 8f);
            halo.rectTransform.offsetMax = new Vector2(-20f, 8f);
            return subtexto;
        }

        /// <summary>Cambia la línea chica y su color de reposo (al estar elegido se enciende en blanco).</summary>
        public void FijarSubtexto(string contenidoTexto, Color color)
        {
            colorSubtexto = color;
            if (subtexto != null)
            {
                subtexto.text = contenidoTexto;
            }
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            elegido = true;
            if (!silenciarProximo)
            {
                Sonidos.Reproducir(SonidoMover);
            }
            silenciarProximo = false;
            Elegido?.Invoke(this);
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            elegido = false;
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            if (IsInteractable() && EventSystem.current != null && EventSystem.current.currentSelectedGameObject != gameObject)
            {
                EventSystem.current.SetSelectedGameObject(gameObject, eventData);
            }
        }

        public void OnSubmit(BaseEventData eventData)
        {
            Pulsar();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Pulsar();
            }
        }

        /// <summary>Lo mismo que pulsarlo con Enter: suena, destella y avisa.</summary>
        public void Pulsar()
        {
            if (!IsInteractable())
            {
                return;
            }
            destello = 1f;
            Sonidos.Reproducir(SonidoAceptar);
            AlPulsar.Invoke();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Aplicar();
        }

        private void Update()
        {
            float objetivo = elegido ? 1f : 0f;
            float paso = Time.unscaledDeltaTime / (elegido ? SegundosEncender : SegundosApagar);
            encendido = Mathf.MoveTowards(encendido, objetivo, paso);
            destello = Mathf.Max(0f, destello - Time.unscaledDeltaTime * VelocidadDestello);
            Aplicar();
        }

        private void Aplicar()
        {
            if (texto == null)
            {
                return;
            }
            float suave = encendido * encendido * (3f - 2f * encendido);
            Color apagado = IsInteractable() ? UIInicio.TextoSuave : UIInicio.TextoApagado;
            texto.color = Color.Lerp(apagado, UIInicio.Blanco, suave);
            if (subtexto != null)
            {
                subtexto.color = Color.Lerp(colorSubtexto, UIInicio.Blanco, suave);
            }
            halo.color = UIInicio.ConAlfa(colorAcento, AlfaHalo * suave);
            Outline contorno = halo.GetComponent<Outline>();
            if (contorno != null)
            {
                contorno.effectColor = UIInicio.ConAlfa(colorAcento, AlfaHalo * 0.45f * suave);
            }
            contenido.anchoredPosition = new Vector2(desplazamiento * suave, 0f);
            fondo.color = UIInicio.ConAlfa(colorAcento, AlfaFondo * suave);
            fondo.rectTransform.localScale = new Vector3(Mathf.Lerp(0.8f, 1f, suave), 1f, 1f);
            barra.color = UIInicio.ConAlfa(colorAcento, suave);
            barra.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, suave);
            destelloImagen.color = UIInicio.ConAlfa(Color.white, 0.55f * destello);
        }
    }
}

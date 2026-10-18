using System;
using System.Collections.Generic;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Progresion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Peleas
{
    /// <summary>
    /// Pantalla de la cartelera del mostrador de peleas (T-160, D-tony-99): Canvas Overlay a pantalla completa con el encabezado (liga y
    /// campeones), una fila por pelea y la ayuda. ↑/↓ eligen (saltando la bloqueada), Enter avisa <c>alElegir(índice)</c> y Esc avisa
    /// <c>alSalir</c>; quien la abre decide qué hacer y la cierra con <see cref="Ocultar"/>.
    /// </summary>
    public class PantallaCartelera : MonoBehaviour
    {
        [SerializeField] private ConfigPantallaCartelera config;
        [SerializeField] private GameObject contenido;
        [SerializeField] private Image fondo;
        [SerializeField] private Text textoEncabezado;
        [SerializeField] private RectTransform listaFilas;
        [Tooltip("Texto que se copia por cada fila (debe estar apagado).")]
        [SerializeField] private Text plantillaFila;
        [SerializeField] private Text textoAyuda;
        [Tooltip("Sube la elección (↑).")]
        [SerializeField] private InputAction arriba = new InputAction("CarteleraArriba", InputActionType.Button, "<Keyboard>/upArrow");
        [Tooltip("Baja la elección (↓).")]
        [SerializeField] private InputAction abajo = new InputAction("CarteleraAbajo", InputActionType.Button, "<Keyboard>/downArrow");
        [Tooltip("Confirma la pelea elegida (Enter).")]
        [SerializeField] private InputAction confirmar = new InputAction("CarteleraEnter", InputActionType.Button, "<Keyboard>/enter");
        [Tooltip("Sale sin pelear (Esc).")]
        [SerializeField] private InputAction salir = new InputAction("CarteleraEsc", InputActionType.Button, "<Keyboard>/escape");

        private readonly List<Text> textosFilas = new List<Text>();
        private readonly List<FilaCartelera> filas = new List<FilaCartelera>();
        private SeleccionCartelera seleccion;
        private Action<int> alElegir;
        private Action alSalir;
        private bool visible;
        private int cuadroAbierta;

        public bool Visible => visible;
        public int IndiceElegido => seleccion != null ? seleccion.Indice : -1;
        public int CantidadFilas => textosFilas.Count;
        public string Encabezado => textoEncabezado != null ? textoEncabezado.text : string.Empty;
        public string Ayuda => textoAyuda != null ? textoAyuda.text : string.Empty;
        public Canvas Canvas => GetComponent<Canvas>();

        /// <summary>Texto de la fila sin la marca de elegida.</summary>
        public string TextoBaseFila(int indice) => filas[indice].Texto;

        /// <summary>Texto de la fila tal como se ve (la elegida lleva la marca).</summary>
        public string TextoFila(int indice) => textosFilas[indice].text;

        /// <summary>Color de la fila tal como se ve.</summary>
        public Color ColorFila(int indice) => textosFilas[indice].color;

        public Color ColorJefe => config != null ? config.ColorJefe : Color.clear;
        public Color ColorBloqueada => config != null ? config.ColorBloqueada : Color.clear;
        public Color ColorElegido => config != null ? config.ColorElegido : Color.clear;
        public Color ColorNormal => config != null ? config.ColorNormal : Color.clear;

        /// <summary>Texto del encabezado: liga actual y campeones vencidos de las ligas que hay.</summary>
        public string TextoEncabezado(string liga, int campeones, int totalLigas) =>
            string.Format(config.FormatoEncabezado, liga, campeones, totalLigas);

        /// <summary>
        /// Arma la fila de una opción del mostrador. <paramref name="liga"/> es el nombre de la liga de la opción, <paramref name="alias"/>
        /// el rival (en la bloqueada, el jefe que hay que vencer) y <paramref name="rivalesPorLiga"/> el total de rivales de la liga.
        /// </summary>
        public FilaCartelera ArmarFila(OpcionCartelera opcion, string liga, string alias, int rivalesPorLiga)
        {
            string premio = FormatoDinero.Texto(opcion.Premio);
            switch (opcion.Tipo)
            {
                case TipoOpcionCartelera.Siguiente:
                    return opcion.EsJefe
                        ? new FilaCartelera(string.Format(config.FormatoJefe, liga, alias, premio), opcion.Premio, true, true)
                        : new FilaCartelera(string.Format(config.FormatoSiguiente, liga, opcion.Indice, rivalesPorLiga, alias, premio),
                            opcion.Premio, true, false);
                case TipoOpcionCartelera.Revancha:
                    return new FilaCartelera(string.Format(config.FormatoRevancha, alias, premio), opcion.Premio, true, false);
                case TipoOpcionCartelera.Bloqueada:
                    return new FilaCartelera(string.Format(config.FormatoBloqueada, liga, alias), 0, false, false);
                default:
                    return new FilaCartelera(string.Format(config.FormatoCasera, alias, premio), opcion.Premio, true, false);
            }
        }

        private void Awake()
        {
            if (config == null || contenido == null || fondo == null || textoEncabezado == null || listaFilas == null
                || plantillaFila == null || textoAyuda == null)
            {
                Debug.LogError("[UI] Faltan referencias en PantallaCartelera de " + name, this);
                enabled = false;
                return;
            }
            plantillaFila.gameObject.SetActive(false);
            contenido.SetActive(false);
        }

        private void OnDisable()
        {
            DesactivarEntrada();
            visible = false;
            if (contenido != null)
            {
                contenido.SetActive(false);
            }
        }

        /// <summary>Abre la pantalla con el encabezado y las filas; empieza en la primera elegible.</summary>
        public void Mostrar(string encabezado, IReadOnlyList<FilaCartelera> nuevasFilas, Action<int> alElegir, Action alSalir)
        {
            this.alElegir = alElegir;
            this.alSalir = alSalir;
            filas.Clear();
            filas.AddRange(nuevasFilas);
            seleccion = new SeleccionCartelera(filas);

            fondo.color = config.ColorFondo;
            textoEncabezado.text = encabezado;
            textoEncabezado.color = config.ColorEncabezado;
            textoEncabezado.fontSize = config.TamanoEncabezado;
            textoAyuda.text = config.TextoAyuda;
            textoAyuda.color = config.ColorNormal;
            textoAyuda.fontSize = config.TamanoAyuda;

            ConstruirFilas();
            Pintar();
            cuadroAbierta = Time.frameCount;
            visible = true;
            contenido.SetActive(true);
            arriba.Enable();
            abajo.Enable();
            confirmar.Enable();
            salir.Enable();
        }

        public void Ocultar()
        {
            visible = false;
            DesactivarEntrada();
            if (contenido != null)
            {
                contenido.SetActive(false);
            }
        }

        private void DesactivarEntrada()
        {
            arriba.Disable();
            abajo.Disable();
            confirmar.Disable();
            salir.Disable();
        }

        private void Update()
        {
            // La tecla que abrió la pantalla en este cuadro no cuenta como una de la pantalla.
            if (!visible || Time.frameCount == cuadroAbierta)
            {
                return;
            }
            if (salir.WasPressedThisFrame())
            {
                alSalir?.Invoke();
                return;
            }
            if (confirmar.WasPressedThisFrame())
            {
                if (seleccion.Indice >= 0)
                {
                    alElegir?.Invoke(seleccion.Indice);
                }
                return;
            }
            bool movio = false;
            if (arriba.WasPressedThisFrame())
            {
                movio |= seleccion.Mover(-1);
            }
            if (abajo.WasPressedThisFrame())
            {
                movio |= seleccion.Mover(1);
            }
            if (movio)
            {
                Pintar();
            }
        }

        private void ConstruirFilas()
        {
            foreach (Text vieja in textosFilas)
            {
                if (vieja != null)
                {
                    Destroy(vieja.gameObject);
                }
            }
            textosFilas.Clear();
            for (int i = 0; i < filas.Count; i++)
            {
                Text texto = Instantiate(plantillaFila, listaFilas);
                texto.gameObject.SetActive(true);
                texto.fontSize = config.TamanoFilas;
                textosFilas.Add(texto);
            }
        }

        private void Pintar()
        {
            for (int i = 0; i < textosFilas.Count; i++)
            {
                FilaCartelera fila = filas[i];
                bool elegida = i == seleccion.Indice;
                textosFilas[i].text = elegida ? string.Format(config.FormatoElegido, fila.Texto) : fila.Texto;
                textosFilas[i].color = !fila.Elegible ? config.ColorBloqueada : fila.EsJefe ? config.ColorJefe
                    : elegida ? config.ColorElegido : config.ColorNormal;
            }
        }
    }
}

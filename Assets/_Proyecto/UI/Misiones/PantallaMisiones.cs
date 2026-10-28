using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Misiones
{
    /// <summary>
    /// Lista de ofertas de misión (T-186), la misma para el teléfono, el contacto del bar y los vecinos de la calle: título, una fila por
    /// misión (↑ ↓) con pago, reputación y favor, en gris con su motivo si no se puede aceptar, Enter para aceptar y Esc para salir.
    /// Suelta sola el control a pie al cerrar. Embebida (dentro del teléfono) no tapa con fondo, no congela ni lee Esc: lo hace el teléfono.
    /// </summary>
    public class PantallaMisiones : MonoBehaviour
    {
        [SerializeField] private ConfigPantallaMisiones config;
        [SerializeField] private GameObject contenido;
        [SerializeField] private Image fondo;
        [SerializeField] private Text textoTitulo;
        [SerializeField] private RectTransform listaFilas;
        [Tooltip("Texto que se copia por cada fila de la lista (debe estar apagado).")]
        [SerializeField] private Text plantillaFila;
        [SerializeField] private Text textoAviso;
        [SerializeField] private Text textoAyuda;
        [SerializeField] private InputAction arriba = new InputAction("MisionesArriba", InputActionType.Button, "<Keyboard>/upArrow");
        [SerializeField] private InputAction abajo = new InputAction("MisionesAbajo", InputActionType.Button, "<Keyboard>/downArrow");
        [SerializeField] private InputAction confirmar = new InputAction("MisionesEnter", InputActionType.Button, "<Keyboard>/enter");
        [SerializeField] private InputAction salir = new InputAction("MisionesEsc", InputActionType.Button, "<Keyboard>/escape");

        private readonly List<Text> textosFilas = new List<Text>();
        private readonly List<OfertaMision> ofertas = new List<OfertaMision>();
        private readonly CongelaControlAPie congela = new CongelaControlAPie();
        private Action alCerrar;
        private bool embebida;
        private bool visible;
        private int indice;
        private OrigenMision origen;
        private string idEncargo;

        public bool Visible => visible;
        public bool Embebida => embebida;
        public int Indice => indice;
        public int CantidadFilas => textosFilas.Count;
        public string Titulo => textoTitulo != null ? textoTitulo.text : string.Empty;
        public string Aviso => textoAviso != null && textoAviso.gameObject.activeSelf ? textoAviso.text : string.Empty;
        public string Ayuda => textoAyuda != null ? textoAyuda.text : string.Empty;
        public string TextoFila(int fila) => textosFilas[fila].text;
        public Color ColorFila(int fila) => textosFilas[fila].color;
        public OfertaMision Oferta(int fila) => ofertas[fila];

        private void Awake()
        {
            if (config == null || contenido == null || fondo == null || textoTitulo == null || listaFilas == null
                || plantillaFila == null || textoAviso == null || textoAyuda == null)
            {
                Debug.LogError("[UI] Faltan referencias en PantallaMisiones de " + name, this);
                enabled = false;
                return;
            }
            plantillaFila.gameObject.SetActive(false);
            contenido.SetActive(false);
        }

        private void OnEnable()
        {
            Registro<PantallaMisiones>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<PantallaMisiones>.Quitar(this);
            DesactivarEntrada();
            visible = false;
            if (contenido != null)
            {
                contenido.SetActive(false);
            }
            congela.Liberar();
        }

        /// <summary>
        /// Abre la lista con las misiones de ese origen (con <paramref name="idDelEncargo"/> solo las de ese vecino).
        /// <paramref name="alCerrarPantalla"/> avisa cuando se cierra por Esc o al aceptar una misión.
        /// </summary>
        public void Mostrar(string tituloPantalla, OrigenMision origenOfertas, string idDelEncargo, Action alCerrarPantalla, bool dentroDelTelefono)
        {
            origen = origenOfertas;
            idEncargo = idDelEncargo;
            alCerrar = alCerrarPantalla;
            embebida = dentroDelTelefono;
            indice = 0;
            fondo.enabled = !embebida;
            textoTitulo.gameObject.SetActive(!embebida);
            textoAyuda.gameObject.SetActive(!embebida);
            textoTitulo.text = tituloPantalla;
            textoTitulo.color = config.ColorTitulo;
            textoTitulo.fontSize = config.TamanoTitulo;
            textoAyuda.text = config.TextoAyudaLista;
            textoAyuda.color = config.ColorNormal;
            textoAyuda.fontSize = config.TamanoAyuda;
            textoAviso.color = config.ColorAviso;
            textoAviso.fontSize = config.TamanoTexto;
            textoAviso.gameObject.SetActive(false);
            fondo.color = config.ColorFondo;
            Pintar();
            visible = true;
            contenido.SetActive(true);
            arriba.Enable();
            abajo.Enable();
            confirmar.Enable();
            if (!embebida)
            {
                salir.Enable();
                congela.Congelar();
            }
        }

        public void Ocultar()
        {
            if (!visible)
            {
                return;
            }
            visible = false;
            DesactivarEntrada();
            contenido.SetActive(false);
            if (!embebida)
            {
                // El control vuelve un cuadro después: la tecla que cierra no debe llegar al jugador (D-tony-67).
                if (isActiveAndEnabled)
                {
                    StartCoroutine(LiberarDespues());
                }
                else
                {
                    congela.Liberar();
                }
            }
        }

        private IEnumerator LiberarDespues()
        {
            yield return null;
            congela.Liberar();
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
            if (!visible)
            {
                return;
            }
            if (!embebida && salir.WasPressedThisFrame())
            {
                Cerrar();
                return;
            }
            int cantidad = ofertas.Count;
            bool cambio = false;
            if (cantidad > 0)
            {
                if (arriba.WasPressedThisFrame())
                {
                    indice = (indice + cantidad - 1) % cantidad;
                    cambio = true;
                }
                if (abajo.WasPressedThisFrame())
                {
                    indice = (indice + 1) % cantidad;
                    cambio = true;
                }
            }
            if (confirmar.WasPressedThisFrame())
            {
                Aceptar();
                return;
            }
            if (cambio)
            {
                textoAviso.gameObject.SetActive(false);
            }
            // Se repinta con cada cambio y cuando cambia lo que se ofrece (por ejemplo, la hora pasa de día a noche).
            if (cambio || CambioLoOfrecido())
            {
                Pintar();
            }
        }

        private bool CambioLoOfrecido()
        {
            ServicioMisiones servicio = Registro<ServicioMisiones>.Actual;
            if (servicio == null)
            {
                return false;
            }
            List<OfertaMision> actuales = servicio.Ofertas(origen, idEncargo);
            if (actuales.Count != ofertas.Count)
            {
                return true;
            }
            for (int i = 0; i < actuales.Count; i++)
            {
                if (actuales[i].Datos != ofertas[i].Datos || actuales[i].Motivo != ofertas[i].Motivo)
                {
                    return true;
                }
            }
            return false;
        }

        private void Aceptar()
        {
            if (ofertas.Count == 0)
            {
                return;
            }
            OfertaMision oferta = ofertas[Mathf.Clamp(indice, 0, ofertas.Count - 1)];
            ServicioMisiones servicio = Registro<ServicioMisiones>.Actual;
            if (oferta.Motivo != MotivoMision.Disponible || servicio == null || !servicio.Aceptar(oferta.Datos))
            {
                textoAviso.text = oferta.Motivo == MotivoMision.Disponible ? config.TextoFaltanPuntos : Motivo(oferta);
                textoAviso.gameObject.SetActive(true);
                return;
            }
            Cerrar();
        }

        private void Cerrar()
        {
            Action aviso = alCerrar;
            Ocultar();
            aviso?.Invoke();
        }

        private string Motivo(OfertaMision oferta)
        {
            ServicioReputacion reputacion = Registro<ServicioReputacion>.Actual;
            return FormatoMisiones.Motivo(oferta.Motivo, oferta.Datos, reputacion != null ? reputacion.Datos : null, config);
        }

        private void Pintar()
        {
            ServicioMisiones servicio = Registro<ServicioMisiones>.Actual;
            ofertas.Clear();
            if (servicio != null)
            {
                ofertas.AddRange(servicio.Ofertas(origen, idEncargo));
            }
            indice = ofertas.Count == 0 ? 0 : Mathf.Clamp(indice, 0, ofertas.Count - 1);
            foreach (Text viejo in textosFilas)
            {
                if (viejo != null)
                {
                    Destroy(viejo.gameObject);
                }
            }
            textosFilas.Clear();
            if (ofertas.Count == 0)
            {
                AgregarFila(config.TextoSinOfertas, config.ColorApagado);
                return;
            }
            for (int i = 0; i < ofertas.Count; i++)
            {
                OfertaMision oferta = ofertas[i];
                bool disponible = oferta.Motivo == MotivoMision.Disponible;
                string fila = disponible ? FormatoMisiones.Fila(oferta.Datos, config)
                    : FormatoMisiones.FilaNoDisponible(oferta.Datos, Motivo(oferta), config);
                bool elegida = i == indice;
                Color color = !disponible ? config.ColorApagado : elegida ? config.ColorElegido : config.ColorNormal;
                AgregarFila(elegida ? string.Format(config.FormatoElegida, fila) : fila, color);
            }
        }

        private void AgregarFila(string contenidoFila, Color color)
        {
            Text texto = Instantiate(plantillaFila, listaFilas);
            texto.gameObject.SetActive(true);
            texto.fontSize = config.TamanoFilas;
            texto.text = contenidoFila;
            texto.color = color;
            textosFilas.Add(texto);
        }
    }
}

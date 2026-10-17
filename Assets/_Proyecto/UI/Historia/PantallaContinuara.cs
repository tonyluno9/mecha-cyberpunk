using System;
using MechaCyberpunk.Economia;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Historia
{
    /// <summary>
    /// Pantalla de fin de la demo (T-125): "CONTINUARÁ…" con un resumen y dos salidas (Enter seguir, Esc al título).
    /// Las teclas funcionan tras <see cref="ConfigPantallasHistoria.SegundosMinimos"/>. Tiempo sin escala.
    /// </summary>
    public class PantallaContinuara : MonoBehaviour
    {
        [SerializeField] private ConfigPantallasHistoria config;
        [SerializeField] private GameObject contenido;
        [SerializeField] private Text textoTitulo;
        [SerializeField] private Text textoAlias;
        [SerializeField] private Text textoDinero;
        [SerializeField] private Text textoResultado;
        [SerializeField] private Text textoTeclas;

        private InputAction accionEnter;
        private InputAction accionEsc;
        private float transcurrido;
        private bool visible;
        private CursorLockMode bloqueoPrevio;
        private bool cursorVisiblePrevio;

        public event Action SeguirJugando;
        public event Action SalirAlTitulo;

        public bool Visible => visible;
        public bool TeclasActivas => visible && transcurrido >= config.SegundosMinimos;
        public string Alias => textoAlias.text;
        public string Dinero => textoDinero.text;
        public string Resultado => textoResultado.text;
        public string Teclas => textoTeclas.text;
        public bool TeclasVisibles => textoTeclas.gameObject.activeSelf;

        private void Awake()
        {
            accionEnter = new InputAction("ContinuaraEnter", InputActionType.Button, "<Keyboard>/enter");
            accionEnter.AddBinding("<Keyboard>/numpadEnter");
            accionEsc = new InputAction("ContinuaraEsc", InputActionType.Button, "<Keyboard>/escape");
            accionEnter.performed += AlEnter;
            accionEsc.performed += AlEsc;
            if (contenido != null)
            {
                contenido.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            accionEnter.performed -= AlEnter;
            accionEsc.performed -= AlEsc;
            accionEnter.Dispose();
            accionEsc.Dispose();
        }

        private void OnDisable()
        {
            if (visible)
            {
                Ocultar();
            }
        }

        public void Mostrar(string alias, int dinero, bool ganoCasera)
        {
            textoTitulo.text = config.TextoTitulo;
            textoTitulo.color = config.ColorTitulo;
            textoTitulo.fontSize = config.TamanoTitulo;
            textoAlias.text = alias;
            textoDinero.text = FormatoDinero.Texto(dinero);
            textoResultado.text = ganoCasera ? config.TextoGano : config.TextoPerdio;
            textoTeclas.text = config.TextoTeclas;
            foreach (Text texto in new[] { textoAlias, textoDinero, textoResultado, textoTeclas })
            {
                texto.color = config.ColorTexto;
                texto.fontSize = config.TamanoTexto;
            }
            textoTeclas.gameObject.SetActive(false);
            transcurrido = 0f;
            if (!visible)
            {
                bloqueoPrevio = Cursor.lockState;
                cursorVisiblePrevio = Cursor.visible;
            }
            visible = true;
            contenido.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            accionEnter.Enable();
            accionEsc.Enable();
        }

        public void Ocultar()
        {
            if (visible)
            {
                Cursor.lockState = bloqueoPrevio;
                Cursor.visible = cursorVisiblePrevio;
            }
            visible = false;
            accionEnter.Disable();
            accionEsc.Disable();
            if (contenido != null)
            {
                contenido.SetActive(false);
            }
        }

        private void Update()
        {
            if (!visible)
            {
                return;
            }
            transcurrido += Time.unscaledDeltaTime;
            if (!textoTeclas.gameObject.activeSelf && TeclasActivas)
            {
                textoTeclas.gameObject.SetActive(true);
            }
            // Mientras se ve, el cursor se queda libre aunque otro sistema lo bloquee.
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void AlEnter(InputAction.CallbackContext contexto)
        {
            if (TeclasActivas)
            {
                SeguirJugando?.Invoke();
            }
        }

        private void AlEsc(InputAction.CallbackContext contexto)
        {
            if (TeclasActivas)
            {
                SalirAlTitulo?.Invoke();
            }
        }
    }
}

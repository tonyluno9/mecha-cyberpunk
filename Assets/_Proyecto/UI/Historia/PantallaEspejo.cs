using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Historia
{
    /// <summary>
    /// Pantalla del espejo de la casa (T-130): Canvas Overlay con la imagen de la vista previa de apariencia, el alias grande y
    /// "E · salir". Se cierra con E o Esc; avisa con <see cref="Cerrada"/> y quien la abrió decide qué más hacer.
    /// </summary>
    public class PantallaEspejo : MonoBehaviour
    {
        [SerializeField] private GameObject contenido;
        [SerializeField] private RawImage imagen;
        [SerializeField] private Text textoAlias;
        [SerializeField] private Text textoSalir;
        [SerializeField] private string textoTeclaSalir = "E · salir";
        [Tooltip("Cierra la pantalla (E).")]
        [SerializeField] private InputAction cerrarConE = new InputAction("EspejoE", InputActionType.Button, "<Keyboard>/e");
        [Tooltip("Cierra la pantalla (Esc).")]
        [SerializeField] private InputAction cerrarConEsc = new InputAction("EspejoEsc", InputActionType.Button, "<Keyboard>/escape");

        private bool visible;
        // La E que abrió la pantalla sigue pulsada al abrirse: no cierra hasta que se suelte una vez.
        private bool armada;

        public event Action Cerrada;

        public bool Visible => visible;
        public string Alias => textoAlias != null ? textoAlias.text : string.Empty;
        public string TextoSalir => textoSalir != null ? textoSalir.text : string.Empty;

        private void Awake()
        {
            if (contenido == null || imagen == null || textoAlias == null || textoSalir == null)
            {
                Debug.LogError("[UI] Faltan referencias en PantallaEspejo de " + name, this);
                enabled = false;
                return;
            }
            contenido.SetActive(false);
        }

        private void OnDisable()
        {
            cerrarConE.Disable();
            cerrarConEsc.Disable();
            visible = false;
            if (contenido != null)
            {
                contenido.SetActive(false);
            }
        }

        /// <summary>Muestra la imagen (la textura de la vista previa) con el alias.</summary>
        public void Mostrar(Texture textura, string alias)
        {
            imagen.texture = textura;
            textoAlias.text = alias;
            textoSalir.text = textoTeclaSalir;
            visible = true;
            armada = false;
            contenido.SetActive(true);
            cerrarConE.Enable();
            cerrarConEsc.Enable();
        }

        public void Ocultar()
        {
            visible = false;
            cerrarConE.Disable();
            cerrarConEsc.Disable();
            contenido.SetActive(false);
        }

        private void Update()
        {
            if (!visible)
            {
                return;
            }
            if (!armada)
            {
                armada = !cerrarConE.IsPressed() && !cerrarConEsc.IsPressed();
                return;
            }
            if (cerrarConE.WasPressedThisFrame() || cerrarConEsc.WasPressedThisFrame())
            {
                Cerrada?.Invoke();
            }
        }
    }
}

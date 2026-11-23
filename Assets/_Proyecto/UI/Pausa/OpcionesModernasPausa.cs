using System;
using MechaCyberpunk.UI.Inicio;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.MenuPausa
{
    /// <summary>
    /// T48 (22-nov noche): las opciones de la pausa con la interfaz moderna del menú principal. Reutiliza <see cref="PanelOpcionesInicio"/>
    /// (las mismas pestañas GRÁFICOS, AUDIO y CONTROLES, que leen y escriben el ServicioOpciones) dentro de un canvas propio, sobre un velo.
    /// Se apila en <see cref="PilaPantallas"/>; VOLVER, Esc o B la cierran y la pausa recupera su menú. No toca el tiempo: la pausa sigue activa.
    /// </summary>
    public class OpcionesModernasPausa : MonoBehaviour
    {
        public const int OrdenCanvas = 70;
        private const float AlfaVelo = 0.93f;

        private GameObject contenido;
        private PanelOpcionesInicio panel;

        public bool Abierta => contenido != null && contenido.activeSelf;

        /// <summary>Se dispara al cerrarse (VOLVER, Esc o B).</summary>
        public event Action Cerrada;

        private void Armar()
        {
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasOpcionesModernas", OrdenCanvas);
            contenido = canvas.gameObject;
            Image velo = KitUI.CrearFondoPantalla(canvas.transform, "Velo");
            Color color = velo.color;
            color.a = AlfaVelo;
            velo.color = color;
            panel = PanelOpcionesInicio.Crear(canvas.transform);
            // El panel nace corrido a la derecha para convivir con el logo del inicio: aquí va al centro.
            ((RectTransform)panel.transform).anchoredPosition = Vector2.zero;
            panel.PidioVolver += Cerrar;
            contenido.SetActive(false);
        }

        private void Awake()
        {
            if (contenido == null)
            {
                Armar();
            }
        }

        private void OnDisable() => Cerrar();

        private void Update()
        {
            if (!Abierta || PilaPantallas.CambioEsteCuadro)
            {
                return;
            }
            Keyboard teclado = Keyboard.current;
            Gamepad control = Gamepad.current;
            if ((teclado != null && teclado.escapeKey.wasPressedThisFrame) || (control != null && control.buttonEast.wasPressedThisFrame))
            {
                Cerrar();
                return;
            }
            // Si se perdió la selección (clic en el fondo), el teclado recupera la primera pestaña.
            EventSystem sistema = EventSystem.current;
            if (sistema != null && sistema.currentSelectedGameObject == null && teclado != null && teclado.anyKey.wasPressedThisFrame)
            {
                panel.PrimerElegido.ElegirSinSonido();
            }
        }

        public void Abrir()
        {
            if (Abierta)
            {
                return;
            }
            if (contenido == null)
            {
                Armar();
            }
            PilaPantallas.Abrir(this);
            contenido.SetActive(true);
            panel.Refrescar();
            panel.Mostrar(PestanaOpciones.Graficos);
            panel.PrimerElegido.ElegirSinSonido();
        }

        public void Cerrar()
        {
            if (!Abierta)
            {
                return;
            }
            contenido.SetActive(false);
            PilaPantallas.Cerrar(this);
            Cerrada?.Invoke();
        }
    }
}

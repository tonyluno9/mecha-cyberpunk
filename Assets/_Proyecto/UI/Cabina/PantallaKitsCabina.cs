using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Mecha.Encendido;
using MechaCyberpunk.Mecha.Kits;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Cabina
{
    /// <summary>
    /// Pantalla de kits de reparación del tablero (T-148): "KITS 2 · G", "REPARANDO…", "SIN KITS" o "LÍMITE DE KITS".
    /// Se prende con las luces de la cabina y solo si hay <see cref="ServicioInventario"/> en la escena.
    /// </summary>
    public class PantallaKitsCabina : MonoBehaviour
    {
        [SerializeField] private UsoKitReparacion uso;
        [Tooltip("El encendido del mecha: la pantalla se prende con las luces de la cabina. Vacío: siempre prendida.")]
        [SerializeField] private EncendidoMecha encendido;
        [SerializeField] private Canvas lienzo;
        [SerializeField] private Image fondo;
        [SerializeField] private Text texto;
        [SerializeField] private ConfigPantallaKits config;

        public Canvas Lienzo => lienzo;
        public Text Texto => texto;
        public bool Visible => lienzo != null && lienzo.enabled;

        private void Awake()
        {
            if (uso == null || lienzo == null || texto == null || config == null)
            {
                Debug.LogError("[UI] Faltan referencias en PantallaKitsCabina de " + name, this);
                enabled = false;
                return;
            }
            if (fondo != null) fondo.color = config.ColorFondo;
        }

        private void OnEnable()
        {
            uso.Cambio += Actualizar;
            Actualizar();
        }

        private void OnDisable()
        {
            if (uso != null) uso.Cambio -= Actualizar;
        }

        // El inventario y el encendido cambian sin avisar a la pantalla (compras, guardado cargado): se refresca cada cuadro.
        private void Update() => Actualizar();

        private void Actualizar()
        {
            int kits = uso.KitsEnInventario;
            bool prendida = encendido == null || encendido.Paso >= PasoEncendido.Paso2;
            lienzo.enabled = prendida && kits >= 0;
            if (kits < 0) return;

            if (uso.Curando)
            {
                Poner(config.TextoReparando, config.ColorReparando);
            }
            else if (uso.EnPelea && uso.Usados >= uso.Tope)
            {
                Poner(config.TextoLimite, config.ColorAgotado);
            }
            else if (kits == 0)
            {
                Poner(config.TextoSinKits, config.ColorAgotado);
            }
            else
            {
                Poner(string.Format(config.TextoKits, kits), config.ColorNormal);
            }
        }

        private void Poner(string cadena, Color color)
        {
            if (texto.text != cadena) texto.text = cadena;
            texto.color = color;
        }
    }
}

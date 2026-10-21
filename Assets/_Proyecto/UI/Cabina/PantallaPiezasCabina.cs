using System.Text;
using MechaCyberpunk.Mecha;
using MechaCyberpunk.Mecha.Encendido;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Cabina
{
    /// <summary>
    /// Pantalla chica de la cabina (fuera del centro de la vista) que lista las 5 piezas puestas, cada nombre en el color de su rama.
    /// Se prende con las luces de la cabina (paso 2 del encendido), dura todo el encendido y se apaga unos segundos después.
    /// Sin <see cref="EquipoMecha"/> en el mecha (modo Directo) no se muestra. No muestra dinero.
    /// </summary>
    public class PantallaPiezasCabina : MonoBehaviour
    {
        private const int NumeroRanuras = 5;

        [SerializeField] private EncendidoMecha encendido;
        [SerializeField] private Canvas lienzo;
        [SerializeField] private Image fondo;
        [SerializeField] private Text texto;
        [SerializeField] private ConfigPantallaPiezasCabina config;

        private EquipoMecha equipo;
        private float restante;

        public Canvas Lienzo => lienzo;
        public Text Texto => texto;
        public bool Visible => lienzo != null && lienzo.enabled;

        private void Awake()
        {
            if (encendido == null || lienzo == null || texto == null || config == null)
            {
                Debug.LogError("[UI] Faltan referencias en PantallaPiezasCabina de " + name, this);
                enabled = false;
                return;
            }
            if (fondo != null)
            {
                fondo.color = config.ColorFondo;
            }
            equipo = GetComponentInParent<EquipoMecha>();
            lienzo.enabled = false;
            if (equipo == null)
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            equipo.Cambio += ArmarTexto;
            ArmarTexto();
            restante = 0f;
        }

        private void OnDisable()
        {
            if (equipo != null)
            {
                equipo.Cambio -= ArmarTexto;
            }
            if (lienzo != null)
            {
                lienzo.enabled = false;
            }
        }

        private void Update()
        {
            bool visible = false;
            if (encendido.Ocupado)
            {
                PasoEncendido paso = encendido.Paso;
                if (paso == PasoEncendido.Paso2 || paso == PasoEncendido.Paso3)
                {
                    restante = config.SegundosDespues;
                    visible = true;
                }
                else if (paso == PasoEncendido.Encendido && restante > 0f)
                {
                    restante -= Time.deltaTime;
                    visible = restante > 0f;
                }
                else
                {
                    restante = 0f;
                }
            }
            else
            {
                restante = 0f;
            }
            lienzo.enabled = visible;
        }

        private void ArmarTexto()
        {
            var lineas = new StringBuilder();
            for (int i = 0; i < NumeroRanuras; i++)
            {
                DatosPieza pieza = equipo.Puesta((RanuraPieza)i);
                if (pieza == null)
                {
                    continue;
                }
                if (lineas.Length > 0)
                {
                    lineas.Append('\n');
                }
                lineas.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(config.ColorDeRama(pieza.Rama))).Append('>')
                    .Append(pieza.Nombre).Append("</color>");
            }
            texto.text = lineas.ToString();
        }
    }
}

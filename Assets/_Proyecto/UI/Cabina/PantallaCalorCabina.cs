using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Encendido;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Cabina
{
    /// <summary>
    /// Pantalla de calor policial del tablero (T-067): título "CALOR", tres marcas que se llenan por nivel y una barra fina del
    /// valor. Se apaga junto con la cabina (hasta que se confirma la entrada del encendido). Busca el <see cref="ICalor"/> en el
    /// registro, ahora o cuando aparezca.
    /// </summary>
    public class PantallaCalorCabina : MonoBehaviour
    {
        [Tooltip("El encendido del mecha: la pantalla se prende con las luces de la cabina. Vacío: siempre prendida.")]
        [SerializeField] private EncendidoMecha encendido;
        [SerializeField] private Canvas lienzo;
        [SerializeField] private Text titulo;
        [SerializeField] private Image fondo;
        [SerializeField] private Image hueco;
        [SerializeField] private Image relleno;
        [SerializeField] private Image[] marcas;
        [SerializeField] private ConfigPantallaCalor config;

        private ICalor calor;
        private bool suscrito;

        public Text Titulo => titulo;
        public Image Relleno => relleno;
        public Image[] Marcas => marcas;
        public bool Visible => lienzo != null && lienzo.enabled;

        /// <summary>Cuántas marcas están llenas.</summary>
        public int MarcasLlenas
        {
            get
            {
                int cuenta = 0;
                foreach (Image marca in marcas)
                {
                    if (marca.color != config.ColorMarcaApagada) cuenta++;
                }
                return cuenta;
            }
        }

        private void Awake()
        {
            if (lienzo == null || titulo == null || relleno == null || marcas == null || marcas.Length == 0 || config == null)
            {
                Debug.LogError("[UI] Faltan referencias en PantallaCalorCabina de " + name, this);
                enabled = false;
                return;
            }
            if (relleno.sprite == null)
            {
                Texture2D blanco = Texture2D.whiteTexture;
                relleno.sprite = Sprite.Create(blanco, new Rect(0f, 0f, blanco.width, blanco.height), new Vector2(0.5f, 0.5f));
            }
            relleno.type = Image.Type.Filled;
            relleno.fillMethod = Image.FillMethod.Horizontal;
            relleno.fillOrigin = (int)Image.OriginHorizontal.Left;
            if (fondo != null) fondo.color = config.ColorFondo;
            if (hueco != null) hueco.color = config.ColorHueco;
            titulo.text = config.Titulo;
        }

        private void OnEnable()
        {
            if (lienzo == null) return;
            suscrito = true;
            Registro<ICalor>.Agregado += AlAgregarCalor;
            Registro<ICalor>.Quitado += AlQuitarCalor;
            Vincular(Registro<ICalor>.Actual);
            lienzo.enabled = EstaPrendida();
        }

        private void OnDisable()
        {
            if (!suscrito) return;
            suscrito = false;
            Registro<ICalor>.Agregado -= AlAgregarCalor;
            Registro<ICalor>.Quitado -= AlQuitarCalor;
            Desvincular();
        }

        private void Update()
        {
            lienzo.enabled = EstaPrendida();
        }

        // Las luces de la cabina se prenden al confirmar la entrada: desde el paso 2 del encendido.
        private bool EstaPrendida()
        {
            return encendido == null || encendido.Paso >= PasoEncendido.Paso2;
        }

        private void AlAgregarCalor(ICalor nuevo) => Vincular(nuevo);

        private void AlQuitarCalor(ICalor quitado)
        {
            if (quitado == calor) Vincular(Registro<ICalor>.Actual);
        }

        private void Vincular(ICalor nuevo)
        {
            Desvincular();
            calor = nuevo;
            if (calor != null)
            {
                calor.ValorCambio += AlCambiarValor;
                calor.NivelCambio += AlCambiarNivel;
            }
            Actualizar();
        }

        private void Desvincular()
        {
            if (calor != null)
            {
                calor.ValorCambio -= AlCambiarValor;
                calor.NivelCambio -= AlCambiarNivel;
            }
            calor = null;
        }

        private void AlCambiarValor(float valor) => Actualizar();

        private void AlCambiarNivel(int nivel) => Actualizar();

        private void Actualizar()
        {
            int nivel = calor != null ? calor.Nivel : 0;
            float proporcion = calor != null && calor.Maximo > 0f ? Mathf.Clamp01(calor.Valor / calor.Maximo) : 0f;
            Color color = config.ColorNivel(nivel);
            relleno.fillAmount = proporcion;
            relleno.color = color;
            titulo.color = color;
            for (int i = 0; i < marcas.Length; i++)
            {
                marcas[i].color = i < nivel ? color : config.ColorMarcaApagada;
            }
        }
    }
}

using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Pelea.Datos;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.Pelea.Flujo
{
    /// <summary>
    /// Pantalla de la arena (D-011): un Canvas World Space con un texto grande que muestra el conteo, "¡PELEA!", "KO" y el
    /// resultado. El director la coloca sobre <see cref="IArenaPelea.PantallaArena"/> con <see cref="Colocar"/>.
    /// Escucha <see cref="EventosPelea"/>; el KO se lo avisa el director porque no tiene evento propio en Core.
    /// </summary>
    public class PantallaPelea : MonoBehaviour
    {
        // Lo que sobresale hacia la jaula para no hundirse en la pantalla de la arena (D-tony-3).
        private const float SeparacionHaciaLaJaula = 0.05f;
        // El signo menos de verdad (U+2212), como en "APUESTA −$250".
        private const string MenosTipografico = "−";

        [Tooltip("Ritmo, textos y colores.")]
        [SerializeField] private ConfigFlujoPelea config;
        [Tooltip("Canvas World Space de la pantalla.")]
        [SerializeField] private Canvas lienzo;
        [Tooltip("Texto grande: conteo, ¡PELEA!, KO y resultado.")]
        [SerializeField] private Text texto;
        [Tooltip("Línea de abajo con la tecla de revancha. Solo aparece en el resultado.")]
        [SerializeField] private Text textoRevancha;

        [Tooltip("Opcional: línea del premio (+$600) bajo el resultado. Sin ella no se muestra el premio.")]
        [SerializeField] private Text textoPremio;

        [Tooltip("Opcional: línea de la apuesta (APUESTA +$500) bajo el premio. Sin ella no se muestra la apuesta.")]
        [SerializeField] private Text textoApuesta;
        [Tooltip("Palabra que abre la línea de la apuesta.")]
        [SerializeField] private string etiquetaApuesta = "APUESTA";
        [Tooltip("Tamaño de la letra de la línea de la apuesta (puntos).")]
        [Min(1)] [SerializeField] private int tamanoLetraApuesta = 110;
        [Tooltip("Color de la apuesta perdida.")]
        [SerializeField] private Color colorApuestaPerdida = new Color(1f, 0.15f, 0.15f);

        private float segundosParaOcultar;
        private bool ocultando;
        private bool avisoRespaldo;
        private bool mostrarRevancha = true;

        /// <summary>Lo que dice el texto grande ahora ("" si está apagado).</summary>
        public string Texto => texto != null && texto.enabled ? texto.text : string.Empty;
        /// <summary>Lo que dice la línea de revancha ahora ("" si está apagada).</summary>
        public string Revancha => textoRevancha != null && textoRevancha.enabled ? textoRevancha.text : string.Empty;
        /// <summary>Lo que dice la línea del premio ahora ("" si está apagada).</summary>
        public string Premio => textoPremio != null && textoPremio.enabled ? textoPremio.text : string.Empty;
        /// <summary>Lo que dice la línea de la apuesta ahora ("" si está apagada).</summary>
        public string Apuesta => textoApuesta != null && textoApuesta.enabled ? textoApuesta.text : string.Empty;
        public Canvas Lienzo => lienzo;

        /// <summary>Si es false, el resultado no muestra la línea "R: revancha" (pelea sin revancha, T-128).</summary>
        public void FijarRevanchaVisible(bool visible)
        {
            mostrarRevancha = visible;
            if (!visible && textoRevancha != null)
            {
                textoRevancha.enabled = false;
            }
        }

        /// <summary>Apaga todos los textos de la pantalla (conteo, resultado, revancha y premio).</summary>
        public void Ocultar()
        {
            ocultando = false;
            Limpiar();
        }

        private void Awake()
        {
            if (config == null || lienzo == null || texto == null || textoRevancha == null)
            {
                Debug.LogError("[Pelea] Faltan referencias en PantallaPelea de " + name, this);
                enabled = false;
                return;
            }
            // Las líneas del presentador son largas: se parten en vez de salirse de la pantalla.
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            Limpiar();
        }

        private void OnEnable()
        {
            EventosPelea.ConteoCambio += AlCambiarConteo;
            EventosPelea.PeleaTerminada += AlTerminar;
        }

        private void OnDisable()
        {
            EventosPelea.ConteoCambio -= AlCambiarConteo;
            EventosPelea.PeleaTerminada -= AlTerminar;
        }

        private void Update()
        {
            if (!ocultando)
            {
                return;
            }
            segundosParaOcultar -= Time.deltaTime;
            if (segundosParaOcultar <= 0f)
            {
                ocultando = false;
                Limpiar();
            }
        }

        /// <summary>
        /// Pone el lienzo sobre la pantalla de la arena: alineado con ella, 5 cm hacia la jaula y del tamaño de su zona útil.
        /// Si la arena no da pantalla (null) o su tamaño no es mayor que 0, avisa una sola vez y usa un respaldo: detrás del
        /// rival, alto y mirando desde el jugador hacia el rival.
        /// </summary>
        public void Colocar(IArenaPelea arena)
        {
            Transform pantalla = arena.PantallaArena;
            Vector2 tamano = arena.TamanoPantalla;
            bool pantallaValida = pantalla != null;
            bool tamanoValido = tamano.x > 0f && tamano.y > 0f;
            if (!pantallaValida || !tamanoValido)
            {
                if (!avisoRespaldo)
                {
                    avisoRespaldo = true;
                    Debug.LogWarning("[Pelea] La arena " + arena.Nombre + " no da una PantallaArena o un TamanoPantalla válidos; "
                        + "se usa una pantalla de respaldo detrás del rival.", this);
                }
            }
            if (!tamanoValido)
            {
                tamano = config.TamanoPantallaRespaldo;
            }
            Vector3 posicion;
            Quaternion rotacion;
            if (pantallaValida)
            {
                posicion = pantalla.position - pantalla.forward * SeparacionHaciaLaJaula;
                rotacion = pantalla.rotation;
            }
            else
            {
                Vector3 haciaRival = arena.InicioRival.position - arena.InicioJugador.position;
                haciaRival.y = 0f;
                Vector3 direccion = haciaRival.sqrMagnitude > 0f ? haciaRival.normalized : Vector3.forward;
                posicion = arena.InicioRival.position + direccion * config.DistanciaPantallaRespaldo
                    + Vector3.up * config.AlturaPantallaRespaldo;
                rotacion = Quaternion.LookRotation(direccion, Vector3.up);
            }
            RectTransform rect = lienzo.GetComponent<RectTransform>();
            Vector2 lienzoPx = rect.sizeDelta;
            float escala = Mathf.Min(tamano.x / lienzoPx.x, tamano.y / lienzoPx.y);
            lienzo.transform.SetPositionAndRotation(posicion, rotacion);
            lienzo.transform.localScale = Vector3.one * escala;
        }

        /// <summary>El presentador dice una línea (T-070): se muestra en el texto grande con su color y tamaño.</summary>
        public void MostrarLinea(string contenido, Color color, int tamanoLetra)
        {
            ocultando = false;
            textoRevancha.enabled = false;
            OcultarPremio();
            Mostrar(contenido, color, tamanoLetra);
            // Arriba, para que el rival (que está delante de la pantalla) no tape el texto.
            texto.alignment = TextAnchor.UpperCenter;
        }

        /// <summary>Muestra "+$600" bajo el resultado. Con monto 0 (o menos) no muestra nada.</summary>
        public void MostrarPremio(int monto)
        {
            if (textoPremio == null || monto <= 0)
            {
                return;
            }
            textoPremio.text = "+" + FormatoDinero.Texto(monto);
            textoPremio.color = config.ColorPremio;
            textoPremio.fontSize = config.TamanoLetraPremio;
            textoPremio.enabled = true;
        }

        /// <summary>
        /// Muestra el cambio neto de la apuesta bajo el premio: "APUESTA +$500" en el color del premio o "APUESTA −$250" en rojo.
        /// Con 0 (o sin la línea en la pantalla) no muestra nada.
        /// </summary>
        public void MostrarApuesta(int neto)
        {
            if (textoApuesta == null || neto == 0)
            {
                return;
            }
            bool gano = neto > 0;
            textoApuesta.text = etiquetaApuesta + " " + (gano ? "+" : MenosTipografico) + FormatoDinero.Texto(Mathf.Abs(neto));
            textoApuesta.color = gano ? config.ColorPremio : colorApuestaPerdida;
            textoApuesta.fontSize = tamanoLetraApuesta;
            textoApuesta.enabled = true;
        }

        /// <summary>El director avisa que alguien cayó: muestra "KO".</summary>
        public void MostrarKO()
        {
            // Si el KO llega mientras "¡PELEA!" sigue en pantalla, el temporizador no debe borrar el "KO".
            ocultando = false;
            Mostrar(config.TextoKO, config.ColorKO, config.TamanoLetra);
        }

        private void AlCambiarConteo(int numero)
        {
            textoRevancha.enabled = false;
            OcultarPremio();
            if (numero > 0)
            {
                ocultando = false;
                Mostrar(numero.ToString(), config.ColorConteo, config.TamanoLetra);
                return;
            }
            Mostrar(config.TextoPelea, config.ColorPelea, config.TamanoLetra);
            segundosParaOcultar = config.SegundosTextoPelea;
            ocultando = true;
        }

        private void AlTerminar(ResultadoPelea resultado)
        {
            ocultando = false;
            bool gano = resultado.Ganador == LadoPelea.Jugador;
            Mostrar(gano ? config.TextoGanaste : config.TextoPerdiste,
                gano ? config.ColorGanaste : config.ColorPerdiste, config.TamanoLetra);
            textoRevancha.text = config.TextoRevancha;
            textoRevancha.fontSize = config.TamanoLetraRevancha;
            textoRevancha.enabled = mostrarRevancha;
        }

        private void Mostrar(string contenido, Color color, int tamanoLetra)
        {
            texto.text = contenido;
            texto.color = color;
            texto.fontSize = tamanoLetra;
            texto.alignment = TextAnchor.MiddleCenter;
            texto.enabled = true;
        }

        private void OcultarPremio()
        {
            if (textoPremio != null)
            {
                textoPremio.enabled = false;
            }
            if (textoApuesta != null)
            {
                textoApuesta.enabled = false;
            }
        }

        private void Limpiar()
        {
            OcultarPremio();
            texto.enabled = false;
            textoRevancha.enabled = false;
        }
    }
}

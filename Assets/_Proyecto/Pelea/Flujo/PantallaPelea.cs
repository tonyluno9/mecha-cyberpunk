using MechaCyberpunk.Core;
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

        [Tooltip("Ritmo, textos y colores.")]
        [SerializeField] private ConfigFlujoPelea config;
        [Tooltip("Canvas World Space de la pantalla.")]
        [SerializeField] private Canvas lienzo;
        [Tooltip("Texto grande: conteo, ¡PELEA!, KO y resultado.")]
        [SerializeField] private Text texto;
        [Tooltip("Línea de abajo con la tecla de revancha. Solo aparece en el resultado.")]
        [SerializeField] private Text textoRevancha;

        private float segundosParaOcultar;
        private bool ocultando;
        private bool avisoRespaldo;

        /// <summary>Lo que dice el texto grande ahora ("" si está apagado).</summary>
        public string Texto => texto != null && texto.enabled ? texto.text : string.Empty;
        /// <summary>Lo que dice la línea de revancha ahora ("" si está apagada).</summary>
        public string Revancha => textoRevancha != null && textoRevancha.enabled ? textoRevancha.text : string.Empty;
        public Canvas Lienzo => lienzo;

        private void Awake()
        {
            if (config == null || lienzo == null || texto == null || textoRevancha == null)
            {
                Debug.LogError("[Pelea] Faltan referencias en PantallaPelea de " + name, this);
                enabled = false;
                return;
            }
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
            textoRevancha.enabled = true;
        }

        private void Mostrar(string contenido, Color color, int tamanoLetra)
        {
            texto.text = contenido;
            texto.color = color;
            texto.fontSize = tamanoLetra;
            texto.enabled = true;
        }

        private void Limpiar()
        {
            texto.enabled = false;
            textoRevancha.enabled = false;
        }
    }
}

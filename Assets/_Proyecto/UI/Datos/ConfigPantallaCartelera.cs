using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>Textos, colores y tamaños de la pantalla de la cartelera del mostrador de peleas (T-160, D-tony-99).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config pantalla de la cartelera", fileName = "ConfigPantallaCartelera_Nueva")]
    public class ConfigPantallaCartelera : ScriptableObject
    {
        [Tooltip("Color de la fila elegida (neón turquesa).")]
        [SerializeField] private Color colorElegido = new Color(0f, 1f, 0.9f, 1f);

        [Tooltip("Color de las filas elegibles que no están elegidas.")]
        [SerializeField] private Color colorNormal = new Color(0.8f, 0.85f, 0.95f, 1f);

        [Tooltip("Color de la fila del jefe de la liga (naranja).")]
        [SerializeField] private Color colorJefe = new Color(1f, 0.55f, 0.1f, 1f);

        [Tooltip("Color de la fila bloqueada (gris).")]
        [SerializeField] private Color colorBloqueada = new Color(0.35f, 0.38f, 0.45f, 1f);

        [Tooltip("Color del encabezado (magenta).")]
        [SerializeField] private Color colorEncabezado = new Color(1f, 0.2f, 0.7f, 1f);

        [Tooltip("Fondo semitransparente de la pantalla.")]
        [SerializeField] private Color colorFondo = new Color(0.02f, 0.02f, 0.06f, 0.85f);

        [Tooltip("Tamaño de letra del encabezado (puntos).")]
        [Min(8)] [SerializeField] private int tamanoEncabezado = 48;
        [Tooltip("Tamaño de letra de las filas (puntos).")]
        [Min(8)] [SerializeField] private int tamanoFilas = 44;
        [Tooltip("Tamaño de letra de la ayuda (puntos).")]
        [Min(8)] [SerializeField] private int tamanoAyuda = 28;

        [Tooltip("{0} = nombre de la liga, {1} = campeones vencidos, {2} = total de ligas.")]
        [SerializeField] private string formatoEncabezado = "PELEAS DEL CLANDESTINO · LIGA {0} · CAMPEONES {1}/{2}";
        [Tooltip("{0} = liga, {1} = número del rival, {2} = rivales de la liga, {3} = alias, {4} = premio con Ƀ.")]
        [SerializeField] private string formatoSiguiente = "LIGA {0} {1}/{2} · {3} · {4}";
        [Tooltip("{0} = liga, {1} = alias, {2} = premio con Ƀ.")]
        [SerializeField] private string formatoJefe = "JEFE DE LA LIGA {0} · {1} · {2}";
        [Tooltip("{0} = alias, {1} = premio con Ƀ.")]
        [SerializeField] private string formatoRevancha = "REVANCHA · {0} · {1}";
        [Tooltip("{0} = alias del rival casero, {1} = premio con Ƀ.")]
        [SerializeField] private string formatoCasera = "PELEA CASERA · {0} · {1}";
        [Tooltip("{0} = liga que sigue, {1} = alias del jefe que hay que vencer.")]
        [SerializeField] private string formatoBloqueada = "LIGA {0} · BLOQUEADA · VENCE A {1}";
        [SerializeField] private string textoAyuda = "↑ ↓ ELEGIR · ENTER PELEAR · ESC SALIR";
        [Tooltip("Marca que se pone a la izquierda de la fila elegida; {0} = texto de la fila.")]
        [SerializeField] private string formatoElegido = "> {0}";

        public Color ColorElegido => colorElegido;
        public Color ColorNormal => colorNormal;
        public Color ColorJefe => colorJefe;
        public Color ColorBloqueada => colorBloqueada;
        public Color ColorEncabezado => colorEncabezado;
        public Color ColorFondo => colorFondo;
        public int TamanoEncabezado => tamanoEncabezado;
        public int TamanoFilas => tamanoFilas;
        public int TamanoAyuda => tamanoAyuda;
        public string FormatoEncabezado => formatoEncabezado;
        public string FormatoSiguiente => formatoSiguiente;
        public string FormatoJefe => formatoJefe;
        public string FormatoRevancha => formatoRevancha;
        public string FormatoCasera => formatoCasera;
        public string FormatoBloqueada => formatoBloqueada;
        public string TextoAyuda => textoAyuda;
        public string FormatoElegido => formatoElegido;
    }
}

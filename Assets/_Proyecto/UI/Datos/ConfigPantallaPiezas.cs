using MechaCyberpunk.Mecha;
using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>Textos, colores por rama y tamaños de la pantalla de piezas del taller (T-169, D-tony-122).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config pantalla piezas del taller", fileName = "ConfigPantallaPiezas_Nueva")]
    public class ConfigPantallaPiezas : ScriptableObject
    {
        [Header("Colores por rama (D-tony-122: nada en rojo puro)")]
        [SerializeField] private Color colorDeSerie = new Color(0.62f, 0.64f, 0.7f, 1f);
        [SerializeField] private Color colorMilitar = new Color(0.42f, 0.62f, 0.95f, 1f);
        [SerializeField] private Color colorTech = new Color(0f, 1f, 0.9f, 1f);
        [SerializeField] private Color colorCuerpoACuerpo = new Color(1f, 0.55f, 0.1f, 1f);
        [SerializeField] private Color colorFuego = new Color(1f, 0.2f, 0.75f, 1f);

        [Header("Otros colores")]
        [Tooltip("Piezas de la liga que sigue (en gris).")]
        [SerializeField] private Color colorBloqueada = new Color(0.45f, 0.46f, 0.5f, 1f);
        [SerializeField] private Color colorFondo = new Color(0.02f, 0.02f, 0.06f, 0.88f);
        [SerializeField] private Color colorTitulo = new Color(0f, 1f, 0.9f, 1f);
        [SerializeField] private Color colorTexto = new Color(0.8f, 0.85f, 0.95f, 1f);
        [SerializeField] private Color colorDinero = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private Color colorDeuda = new Color(1f, 0.45f, 0.35f, 1f);
        [Tooltip("Lo que sube en la diferencia de estadísticas.")]
        [SerializeField] private Color colorSube = new Color(0.3f, 1f, 0.45f, 1f);
        [Tooltip("Lo que baja en la diferencia de estadísticas.")]
        [SerializeField] private Color colorBaja = new Color(1f, 0.2f, 0.75f, 1f);
        [SerializeField] private Color colorAviso = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private Color colorError = new Color(1f, 0.45f, 0.35f, 1f);

        [Header("Tamaños (puntos, a 1920x1080)")]
        [Min(8)] [SerializeField] private int tamanoTitulo = 72;
        [Min(8)] [SerializeField] private int tamanoSubtitulo = 34;
        [Min(8)] [SerializeField] private int tamanoDinero = 40;
        [Min(8)] [SerializeField] private int tamanoFilas = 38;
        [Min(8)] [SerializeField] private int tamanoEstadisticas = 44;
        [Min(8)] [SerializeField] private int tamanoDiferencia = 40;
        [Min(8)] [SerializeField] private int tamanoAviso = 36;
        [Min(8)] [SerializeField] private int tamanoAyuda = 28;

        [Header("Textos")]
        [SerializeField] private string textoTitulo = "PIEZAS DEL MECHA";
        [SerializeField] private string subtituloRanuras = "ELIGE UNA RANURA";
        [Tooltip("{0} = nombre de la pieza puesta.")]
        [SerializeField] private string formatoSubtituloPiezas = "CAMBIAR {0}";
        [Tooltip("{0} = dinero con $ (negativo: -$150).")]
        [SerializeField] private string formatoDinero = "DINERO {0}";
        [Tooltip("{0} = vida, {1} = daño, {2} = velocidad del mecha armado.")]
        [SerializeField] private string formatoEstadisticas = "VIDA {0} · DAÑO {1} · VELOCIDAD {2}";
        [SerializeField] private string etiquetaVida = "VIDA";
        [SerializeField] private string etiquetaDanio = "DAÑO";
        [SerializeField] private string etiquetaVelocidad = "VEL";
        [Tooltip("T-177. {0} = tecla (1 o 2), {1} = habilidad con su nivel, {2} = munición.")]
        [SerializeField] private string formatoHabilidad = "HABILIDAD {0} · {1} · {2}";
        [Tooltip("T-177. La pieza nueva no trae la habilidad que traía la puesta.")]
        [SerializeField] private string textoSinHabilidad = "SIN HABILIDAD";
        [Tooltip("T-177. {0} = cambio del enfriamiento en puntos de porcentaje (−6: enfría 6 % más rápido).")]
        [SerializeField] private string formatoEnfriamiento = "ENFRIAMIENTO {0} %";
        [SerializeField] private string textoSinCambios = "SIN CAMBIOS";
        [Tooltip("{0} = texto de la fila elegida.")]
        [SerializeField] private string formatoElegida = "[ {0} ]";
        [Tooltip("{0} = nombre de la pieza, {1} = cuántas tienes en el inventario.")]
        [SerializeField] private string formatoPoner = "{0} · PONER · TIENES {1}";
        [Tooltip("{0} = nombre de la pieza, {1} = precio con $.")]
        [SerializeField] private string formatoComprar = "{0} · COMPRAR · {1}";
        [Tooltip("{0} = nombre de la pieza, {1} = liga en que se abre.")]
        [SerializeField] private string formatoBloqueada = "{0} · SE ABRE EN LA LIGA {1}";
        [Tooltip("{0} = nombre de la pieza puesta.")]
        [SerializeField] private string formatoPuesta = "PUESTA: {0}";
        [Tooltip("{0} = dinero con $ que se pagó por la pieza vieja, {1} = su nombre.")]
        [SerializeField] private string formatoVendida = "+{0} · VENDISTE {1}";
        [SerializeField] private string textoNoAlcanza = "NO TE ALCANZA";
        [SerializeField] private string textoTallerBloqueado = "TALLER BLOQUEADO";
        [SerializeField] private string textoNoSePudo = "NO SE PUDO";
        [SerializeField] private string textoSinPiezas = "NO HAY PIEZAS PARA ESTA RANURA";
        [SerializeField] private string ayudaRanuras = "↑ ↓ elegir · Enter o → ver piezas · Esc salir";
        [SerializeField] private string ayudaPiezas = "↑ ↓ elegir · Enter comprar o poner · ← o Esc volver";

        public Color ColorDeRama(RamaPieza rama)
        {
            switch (rama)
            {
                case RamaPieza.Militar: return colorMilitar;
                case RamaPieza.Tech: return colorTech;
                case RamaPieza.CuerpoACuerpo: return colorCuerpoACuerpo;
                case RamaPieza.Fuego: return colorFuego;
                default: return colorDeSerie;
            }
        }

        public Color ColorBloqueada => colorBloqueada;
        public Color ColorFondo => colorFondo;
        public Color ColorTitulo => colorTitulo;
        public Color ColorTexto => colorTexto;
        public Color ColorDinero => colorDinero;
        public Color ColorDeuda => colorDeuda;
        public Color ColorSube => colorSube;
        public Color ColorBaja => colorBaja;
        public Color ColorAviso => colorAviso;
        public Color ColorError => colorError;
        public int TamanoTitulo => tamanoTitulo;
        public int TamanoSubtitulo => tamanoSubtitulo;
        public int TamanoDinero => tamanoDinero;
        public int TamanoFilas => tamanoFilas;
        public int TamanoEstadisticas => tamanoEstadisticas;
        public int TamanoDiferencia => tamanoDiferencia;
        public int TamanoAviso => tamanoAviso;
        public int TamanoAyuda => tamanoAyuda;
        public string TextoTitulo => textoTitulo;
        public string SubtituloRanuras => subtituloRanuras;
        public string FormatoSubtituloPiezas => formatoSubtituloPiezas;
        public string FormatoDinero => formatoDinero;
        public string FormatoEstadisticas => formatoEstadisticas;
        public string EtiquetaVida => etiquetaVida;
        public string EtiquetaDanio => etiquetaDanio;
        public string EtiquetaVelocidad => etiquetaVelocidad;
        public string FormatoHabilidad => formatoHabilidad;
        public string TextoSinHabilidad => textoSinHabilidad;
        public string FormatoEnfriamiento => formatoEnfriamiento;
        public string TextoSinCambios => textoSinCambios;
        public string FormatoElegida => formatoElegida;
        public string FormatoPoner => formatoPoner;
        public string FormatoComprar => formatoComprar;
        public string FormatoBloqueada => formatoBloqueada;
        public string FormatoPuesta => formatoPuesta;
        public string FormatoVendida => formatoVendida;
        public string TextoNoAlcanza => textoNoAlcanza;
        public string TextoTallerBloqueado => textoTallerBloqueado;
        public string TextoNoSePudo => textoNoSePudo;
        public string TextoSinPiezas => textoSinPiezas;
        public string AyudaRanuras => ayudaRanuras;
        public string AyudaPiezas => ayudaPiezas;
    }
}

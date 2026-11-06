using System.Collections.Generic;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.UI.Armas;
using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>
    /// Presentación del banco de armas de Tino (T-814): las armas con su holograma, las tres piezas, el material holográfico, textos,
    /// sonidos y la sensación del giro. Asset: UI/Datos/ConfigMenuArmas_Base.asset (lo arma CreadorMenuArmas).
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Config menú de armas", fileName = "ConfigMenuArmas_Nuevo")]
    public class ConfigMenuArmas : ScriptableObject
    {
        [Header("Armas y piezas")]
        [SerializeField] private List<EntradaArmaMenu> armas = new List<EntradaArmaMenu>();
        [Tooltip("Las tres piezas en el orden de PiezaArma (cargador, cañón, mira).")]
        [SerializeField] private List<EntradaPiezaMenu> piezas = new List<EntradaPiezaMenu>();
        [Tooltip("Material del holograma (shader MechaCyberpunk/UI/HologramaArma).")]
        [SerializeField] private Material materialHolograma;
        [Tooltip("Material aditivo de UI para la imagen del holograma y los íconos (fondo negro).")]
        [SerializeField] private Material materialAditivoUI;

        [Header("Holograma")]
        [Tooltip("Dónde vive el set del holograma, lejos de la ciudad (m).")]
        [SerializeField] private Vector3 posicionSet = new Vector3(0f, -500f, 0f);
        [Tooltip("Ancho de la textura del holograma (px).")]
        [Min(64)] [SerializeField] private int anchoTextura = 1152;
        [Tooltip("Alto de la textura del holograma (px).")]
        [Min(64)] [SerializeField] private int altoTextura = 720;
        [Tooltip("Campo de visión de la cámara del holograma (°).")]
        [Range(10f, 60f)] [SerializeField] private float campoVision = 28f;
        [Tooltip("Distancia de la cámara al arma (m; el arma mide 1 m de largo en el set).")]
        [Min(0.5f)] [SerializeField] private float distancia = 2.9f;
        [Tooltip("Distancia mínima con la rueda (m).")]
        [Min(0.5f)] [SerializeField] private float distanciaMinima = 1.5f;
        [Tooltip("Distancia máxima con la rueda (m).")]
        [Min(0.5f)] [SerializeField] private float distanciaMaxima = 4f;
        [Tooltip("Metros por cada paso de la rueda (m por 120 unidades de scroll).")]
        [Min(0f)] [SerializeField] private float pasoRueda = 0.25f;
        [Tooltip("Giro al arrastrar con el ratón (° por píxel).")]
        [Min(0f)] [SerializeField] private float sensibilidad = 0.35f;
        [Tooltip("Inclinación máxima al arrastrar (°).")]
        [Range(0f, 89f)] [SerializeField] private float inclinacionMaxima = 55f;
        [Tooltip("Giro solo cuando no se arrastra (°/s).")]
        [Min(0f)] [SerializeField] private float giroSolo = 14f;
        [Tooltip("Segundos sin arrastrar antes de que vuelva a girar solo (s).")]
        [Min(0f)] [SerializeField] private float esperaGiroSolo = 2.5f;
        [Tooltip("Rapidez con que la pieza sale y entra (1/s).")]
        [Min(0.1f)] [SerializeField] private float rapidezPieza = 7f;
        [Tooltip("Duración del destello al comprar (s).")]
        [Min(0.05f)] [SerializeField] private float duracionDestello = 0.9f;
        [Tooltip("Duración de la animación de los números al comprar (s).")]
        [Min(0.05f)] [SerializeField] private float duracionNumeros = 0.7f;
        [Tooltip("Duración del letrero ARMA MEJORADA al salir (s).")]
        [Min(0.1f)] [SerializeField] private float duracionLetrero = 2.2f;

        [Header("Colores")]
        [SerializeField] private Color colorHolograma = new Color(0.15f, 0.85f, 1f, 1f);
        [SerializeField] private Color colorMejora = new Color(0.35f, 1f, 0.55f, 1f);
        [SerializeField] private Color colorPeor = new Color(1f, 0.3f, 0.3f, 1f);
        [Tooltip("Fondo de toda la pantalla (tapa la ciudad).")]
        [SerializeField] private Color colorFondo = new Color(0.012f, 0.02f, 0.04f, 1f);

        [Header("Textos")]
        [SerializeField] private string titulo = "BANCO DE TINO";
        [SerializeField] private string subtitulo = "MEJORA DE ARMAS";
        [SerializeField] private string encabezadoArmas = "TUS ARMAS";
        [SerializeField] private string encabezadoPiezas = "PIEZAS";
        [SerializeField] private string encabezadoEstadisticas = "ANTES  →  DESPUÉS";
        [Tooltip("{0} = nivel en romanos (I).")]
        [SerializeField] private string formatoNivel = "NIVEL {0}";
        [SerializeField] private string textoBase = "BASE";
        [SerializeField] private string textoInstalada = "INSTALADA";
        [Tooltip("{0} = la pieza que falta (CARGADOR).")]
        [SerializeField] private string formatoBloqueada = "PRIMERO EL {0}";
        [Tooltip("{0} = precio.")]
        [SerializeField] private string formatoMejorar = "ENTER · MEJORAR  {0}";
        [SerializeField] private string textoNoAlcanza = "NO TE ALCANZA";
        [SerializeField] private string textoAlMaximo = "ARMA AL MÁXIMO";
        [SerializeField] private string textoNoDisponible = "NO DISPONIBLE";
        [Tooltip("{0} = pieza, {1} = arma.")]
        [SerializeField] private string formatoComprado = "{0} INSTALADO EN TU {1}";
        [SerializeField] private string textoAyuda = "← → ARMA     ↑ ↓ PIEZA     ENTER MEJORAR     ESC SALIR";
        [SerializeField] private string textoAyudaRaton = "ARRASTRA PARA GIRAR · RUEDA PARA ACERCAR";
        [SerializeField] private string textoSinArmas = "NO TRAES ARMAS DE FUEGO";
        [SerializeField] private string textoLetrero = "ARMA MEJORADA";
        [Tooltip("{0} = arma y nivel (PISTOLA II).")]
        [SerializeField] private string formatoLetrero = "{0}";
        [SerializeField] private string textoAviso = "MEJORAR ARMAS";
        [SerializeField] private string[] nombresEstadisticas = { "DAÑO", "CARGADOR", "RECARGA", "CADENCIA", "DISPERSIÓN" };
        [Tooltip("Lo que mejora la pieza cuando su estadística no cambia en esa arma y se resalta otra; {0} = la estadística (RECARGA).")]
        [SerializeField] private string formatoMejoraReal = "MEJORA {0}";

        [Header("Sonidos (ids de GestorSonidos)")]
        [SerializeField] private string sonidoMover = "UI_MOVER";
        [SerializeField] private string sonidoAceptar = "UI_ACEPTAR";
        [SerializeField] private string sonidoCancelar = "UI_CANCELAR";
        [SerializeField] private string sonidoCompra = "ECO_COMPRA";
        [SerializeField] private string sonidoSinDinero = "ECO_SIN_DINERO";

        public IReadOnlyList<EntradaArmaMenu> Armas => armas;
        public IReadOnlyList<EntradaPiezaMenu> Piezas => piezas;
        public Material MaterialHolograma => materialHolograma;
        public Material MaterialAditivoUI => materialAditivoUI;
        public Vector3 PosicionSet => posicionSet;
        public int AnchoTextura => anchoTextura;
        public int AltoTextura => altoTextura;
        public float CampoVision => campoVision;
        public float Distancia => distancia;
        public float DistanciaMinima => distanciaMinima;
        public float DistanciaMaxima => distanciaMaxima;
        public float PasoRueda => pasoRueda;
        public float Sensibilidad => sensibilidad;
        public float InclinacionMaxima => inclinacionMaxima;
        public float GiroSolo => giroSolo;
        public float EsperaGiroSolo => esperaGiroSolo;
        public float RapidezPieza => rapidezPieza;
        public float DuracionDestello => duracionDestello;
        public float DuracionNumeros => duracionNumeros;
        public float DuracionLetrero => duracionLetrero;
        public Color ColorHolograma => colorHolograma;
        public Color ColorMejora => colorMejora;
        public Color ColorPeor => colorPeor;
        public Color ColorFondo => colorFondo;
        public string Titulo => titulo;
        public string Subtitulo => subtitulo;
        public string EncabezadoArmas => encabezadoArmas;
        public string EncabezadoPiezas => encabezadoPiezas;
        public string EncabezadoEstadisticas => encabezadoEstadisticas;
        public string FormatoNivel => formatoNivel;
        public string TextoBase => textoBase;
        public string TextoInstalada => textoInstalada;
        public string FormatoBloqueada => formatoBloqueada;
        public string FormatoMejorar => formatoMejorar;
        public string TextoNoAlcanza => textoNoAlcanza;
        public string TextoAlMaximo => textoAlMaximo;
        public string TextoNoDisponible => textoNoDisponible;
        public string FormatoComprado => formatoComprado;
        public string TextoAyuda => textoAyuda;
        public string TextoAyudaRaton => textoAyudaRaton;
        public string TextoSinArmas => textoSinArmas;
        public string TextoLetrero => textoLetrero;
        public string FormatoLetrero => formatoLetrero;
        public string TextoAviso => textoAviso;
        public string FormatoMejoraReal => formatoMejoraReal;
        public string SonidoMover => sonidoMover;
        public string SonidoAceptar => sonidoAceptar;
        public string SonidoCancelar => sonidoCancelar;
        public string SonidoCompra => sonidoCompra;
        public string SonidoSinDinero => sonidoSinDinero;

        /// <summary>El nombre de la estadística en la tabla (DAÑO).</summary>
        public string NombreEstadistica(EstadisticaArma estadistica)
        {
            int i = (int)estadistica;
            return nombresEstadisticas != null && i < nombresEstadisticas.Length ? nombresEstadisticas[i] : estadistica.ToString().ToUpperInvariant();
        }

        /// <summary>La entrada de esa arma, o null.</summary>
        public EntradaArmaMenu Entrada(ArmaAPie arma)
        {
            foreach (EntradaArmaMenu e in armas)
            {
                if (e != null && e.Arma == arma)
                {
                    return e;
                }
            }
            return null;
        }

        /// <summary>La presentación de la pieza; sin entrada en la lista, una por defecto.</summary>
        public EntradaPiezaMenu Pieza(PiezaArma pieza)
        {
            foreach (EntradaPiezaMenu p in piezas)
            {
                if (p != null && p.Pieza == pieza)
                {
                    return p;
                }
            }
            return EntradaPiezaMenu.Crear(pieza, pieza.ToString().ToUpperInvariant(), string.Empty, EstadisticaArma.Danio, 0.3f);
        }

        /// <summary>Llena la config en memoria (para el creador y las pruebas).</summary>
        public void Configurar(List<EntradaArmaMenu> nuevasArmas, List<EntradaPiezaMenu> nuevasPiezas, Material holograma, Material aditivo)
        {
            armas = nuevasArmas;
            piezas = nuevasPiezas;
            materialHolograma = holograma;
            materialAditivoUI = aditivo;
        }
    }
}

using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// La cola de avisos del centro (T-523): todo lo que antes salía a la vez en medio de la pantalla (objetivo cumplido, "SE UNE A TU PANDILLA", premios,
    /// subir de liga, reputación, jefe de zona, ayudas de la historia, cambio de estación) pasa por aquí, uno a la vez y por poco tiempo, con prioridad
    /// para los importantes. Se publica con <see cref="Publicar"/> desde donde ocurre el evento. Lo agrega <see cref="HudAPie"/>; la lógica está en
    /// <see cref="ReglaColaAvisos"/>. Con el juego en pausa o con otra pantalla encima se esconde y el tiempo no corre.
    /// </summary>
    public class ColaAvisos : MonoBehaviour
    {
        public const int OrdenCanvas = 14;

        /// <summary>T-594: el sonido sutil de un aviso nuevo.</summary>
        public const string SonidoTexto = "UI_TEXTO";

        [Header("Tiempos")]
        [Tooltip("Lo menos que se ve un aviso (s).")]
        [Min(0.5f)] [SerializeField] private float segundosMinimos = 2f;
        [Tooltip("Lo más que se ve un aviso (s), aunque quien lo publique pida más.")]
        [Min(0.5f)] [SerializeField] private float segundosMaximos = 3.5f;
        [Tooltip("Cuántos avisos esperan como máximo; si llegan más se descarta el menos importante y más viejo.")]
        [Min(1)] [SerializeField] private int maximoEnEspera = 5;

        [Header("Medidas (px a 1920x1080)")]
        [Tooltip("Altura sobre el centro de la pantalla (px).")]
        [SerializeField] private float alturaSobreCentro = 170f;
        [Tooltip("Alto de la franja oscura detrás del texto (px).")]
        [Min(10f)] [SerializeField] private float altoFranja = 60f;
        [Tooltip("Lo que sobra a cada lado del texto en la franja (px).")]
        [Min(0f)] [SerializeField] private float relleno = 44f;
        [Tooltip("Oscuridad de la franja (0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float opacidadFranja = 0.55f;

        private static ColaAvisos actual;
        private ReglaColaAvisos regla;
        private RectTransform franja;
        private Text texto;
        private string textoMostrado = string.Empty;

        /// <summary>La cola viva (null si no hay HUD).</summary>
        public static ColaAvisos Actual => actual;
        public bool Visible => franja != null && franja.gameObject.activeSelf;
        public string Texto => texto != null ? texto.text : string.Empty;
        public int EnEspera => regla != null ? regla.EnEspera : 0;
        public ReglaColaAvisos Regla => regla;

        /// <summary>Mete un aviso a la cola. Sin HUD vivo no hace nada. <paramref name="segundos"/> en 0 usa el mínimo.</summary>
        public static void Publicar(string texto, PrioridadAviso prioridad = PrioridadAviso.Normal, TipoAviso tipo = TipoAviso.Neutro, float segundos = 0f, string clave = null)
        {
            if (actual != null)
            {
                actual.regla.Publicar(texto, prioridad, tipo, segundos, clave);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void LimpiarEstatico() => actual = null;

        private void Awake()
        {
            regla = new ReglaColaAvisos(segundosMinimos, segundosMaximos, maximoEnEspera);
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasAvisos", OrdenCanvas);
            Image fondo = KitUI.CrearImagen(canvas.transform, "Franja", new Color(0.02f, 0.02f, 0.05f, opacidadFranja));
            franja = fondo.rectTransform;
            KitUI.Colocar(franja, new Vector2(0.5f, 0.5f), new Vector2(0f, alturaSobreCentro), new Vector2(relleno * 2f, altoFranja));
            franja.pivot = new Vector2(0.5f, 0.5f);
            texto = KitUI.CrearTexto(franja, "Texto", RolTexto.Encabezado, TextAnchor.MiddleCenter);
            KitUI.Estirar(texto.rectTransform);
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            franja.gameObject.SetActive(false);
        }

        private void OnEnable() => actual = this;

        private void OnDisable()
        {
            if (actual == this)
            {
                actual = null;
            }
        }

        private void Update()
        {
            bool puedeVerse = !Pausa.Activa && PilaPantallas.Vacia;
            regla.Avanzar(puedeVerse ? Time.unscaledDeltaTime : 0f);
            bool ver = puedeVerse && regla.HayActual;
            if (franja.gameObject.activeSelf != ver)
            {
                franja.gameObject.SetActive(ver);
            }
            if (!ver)
            {
                textoMostrado = string.Empty;
                return;
            }
            string nuevo = regla.Actual.Texto;
            texto.color = ColorDe(regla.Actual.Tipo);
            if (nuevo != textoMostrado)
            {
                textoMostrado = nuevo;
                texto.text = nuevo;
                Sonidos.Reproducir(SonidoTexto);
                float ancho = texto.preferredWidth + relleno * 2f;
                franja.sizeDelta = new Vector2(ancho, altoFranja);
            }
        }

        private static Color ColorDe(TipoAviso tipo)
        {
            EstiloCyberfix e = KitUI.Estilo;
            switch (tipo)
            {
                case TipoAviso.Bueno:
                    return e.Turquesa;
                case TipoAviso.Malo:
                    return e.Enemigo;
                case TipoAviso.Pandilla:
                    return MechaCyberpunk.UI.Mapa.FormatoTerritorio.ColorDe(IdPandilla.Jugador);
                case TipoAviso.Dinero:
                    return e.Naranja;
                default:
                    return e.Texto;
            }
        }
    }
}

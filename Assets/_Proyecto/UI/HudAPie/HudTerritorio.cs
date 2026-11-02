using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Mapa;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// Arriba a la izquierda dice en qué zona estás, de quién es y tu respeto, con el color del dueño (T-404). Avisa "¡EL JEFE DE &lt;ZONA&gt; TE BUSCA!" (T-428)
    /// cuando el jefe de una zona queda disponible y "&lt;ZONA&gt; ES DE &lt;TU PANDILLA&gt;" 3 s al conquistarla. Crea su propio canvas; no depende de HudAPie.
    /// </summary>
    public class HudTerritorio : MonoBehaviour
    {
        public const int OrdenCanvas = 10;
        private const float MaximoRespeto = 100f;
        private const float AnchoAviso = 1400f;

        [Tooltip("Segundos que dura el aviso de conquista (s).")]
        [Min(0.5f)] [SerializeField] private float segundosConquista = 3f;
        [Tooltip("Segundos que dura el aviso del jefe (s).")]
        [Min(0.5f)] [SerializeField] private float segundosJefe = 4f;

        private Text textoZona;
        private Text textoAviso;
        private RectTransform panelZona;
        private Image insignia;
        private Image rellenoRespeto;
        private ITerritorio territorio;
        private ServicioNegocios negocios;
        private string zonaPrevia;
        private float finAviso;
        private readonly System.Collections.Generic.HashSet<string> jefesAvisados = new System.Collections.Generic.HashSet<string>();

        public Text TextoZona => textoZona;
        public Text TextoAviso => textoAviso;

        private void Awake()
        {
            // T-408: el kit y las ranuras de DisposicionHud (zona bajo la radio y la renta; aviso bajo el objetivo).
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasTerritorio", OrdenCanvas);
            // T-521: la zona va en un panel arriba a la derecha: el texto de siempre, tu respeto en una barra y la insignia del dueño.
            Image marco = KitUI.CrearPanelMarco(canvas.transform, "PanelZona");
            panelZona = marco.rectTransform;
            KitUI.Colocar(panelZona, DisposicionHud.Zona, DisposicionHud.AnchoZona);
            // T-522: un fondo oscuro semitransparente detrás del texto, para que el color del dueño (magenta, verde) se lea sobre una pared del mismo color.
            Image oscuro = KitUI.CrearImagen(panelZona, "FondoOscuro", new Color(0.02f, 0.02f, 0.05f, 0.82f));
            KitUI.Estirar(oscuro.rectTransform);
            oscuro.rectTransform.offsetMin = new Vector2(14f, 12f);
            oscuro.rectTransform.offsetMax = new Vector2(-14f, -12f);
            insignia = KitUI.CrearImagenSprite(panelZona, "Insignia", null);
            KitUI.Colocar(insignia.rectTransform, new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(60f, 74f));
            textoZona = KitUI.CrearTexto(panelZona, "Zona", RolTexto.Hud, TextAnchor.MiddleLeft);
            textoZona.rectTransform.anchorMin = new Vector2(0f, 0.42f);
            textoZona.rectTransform.anchorMax = new Vector2(1f, 0.92f);
            textoZona.rectTransform.offsetMin = new Vector2(30f, 0f);
            textoZona.rectTransform.offsetMax = new Vector2(-88f, 0f);
            Image huecoRespeto = KitUI.CrearImagen(panelZona, "HuecoRespeto", new Color(0f, 0f, 0f, 0.45f));
            huecoRespeto.rectTransform.anchorMin = new Vector2(0f, 0.2f);
            huecoRespeto.rectTransform.anchorMax = new Vector2(1f, 0.34f);
            huecoRespeto.rectTransform.offsetMin = new Vector2(30f, 0f);
            huecoRespeto.rectTransform.offsetMax = new Vector2(-88f, 0f);
            rellenoRespeto = KitUI.CrearImagen(huecoRespeto.transform, "Respeto", Color.white);
            KitUI.Estirar(rellenoRespeto.rectTransform);
            panelZona.gameObject.SetActive(false);
            textoAviso = KitUI.CrearTexto(canvas.transform, "Aviso", RolTexto.Encabezado, TextAnchor.MiddleCenter);
            KitUI.Colocar(textoAviso.rectTransform, DisposicionHud.AvisoZona, AnchoAviso);
            textoZona.text = string.Empty;
            textoAviso.text = string.Empty;
            // T-523: los avisos de zona salen por la cola del centro; aquí solo queda el texto como dato, sin dibujarse.
            textoAviso.enabled = false;
        }

        private void OnDisable()
        {
            Desvincular();
            if (negocios != null)
            {
                negocios.Pagado -= AlPagarNegocios;
                negocios = null;
            }
        }

        // T-429: lo que pagaron tus negocios al cambiar el día.
        private void AlPagarNegocios(int total) =>
            Avisar(FormatoTerritorio.Cortar(ServicioNegocios.TextoPago(total)), FormatoTerritorio.ColorDe(IdPandilla.Jugador), segundosConquista, TipoAviso.Dinero);

        private void Desvincular()
        {
            if (territorio != null)
            {
                territorio.Cambio -= AlCambiar;
                territorio.Conquistada -= AlConquistar;
                territorio = null;
            }
        }

        private void Update()
        {
            ITerritorio actual = Registro<ITerritorio>.Actual;
            if (!ReferenceEquals(actual, territorio))
            {
                Desvincular();
                territorio = actual;
                if (territorio != null)
                {
                    territorio.Cambio += AlCambiar;
                    territorio.Conquistada += AlConquistar;
                }
            }
            ServicioNegocios servicioNegocios = Registro<ServicioNegocios>.Actual;
            if (!ReferenceEquals(servicioNegocios, negocios))
            {
                if (negocios != null)
                {
                    negocios.Pagado -= AlPagarNegocios;
                }
                negocios = servicioNegocios;
                if (negocios != null)
                {
                    negocios.Pagado += AlPagarNegocios;
                }
            }
            IJugador jugador = Registro<IJugador>.Actual;
            string zona = territorio != null && jugador != null && jugador.Raiz != null ? territorio.ZonaEn(jugador.Raiz.position) : string.Empty;
            if (zona != zonaPrevia)
            {
                // T-450: al cruzar a otra zona suena distinto si es tuya o de otra pandilla.
                if (zona.Length > 0 && zonaPrevia != null)
                {
                    Sonidos.Reproducir(territorio.Dueno(zona) == IdPandilla.Jugador ? "ZON_ENTRAR_PROPIA" : "ZON_ENTRAR_RIVAL");
                }
                zonaPrevia = zona;
            }
            if (zona.Length == 0)
            {
                textoZona.text = string.Empty;
                panelZona.gameObject.SetActive(false);
            }
            else
            {
                IdPandilla dueno = territorio.Dueno(zona);
                Color colorDueno = FormatoTerritorio.ColorDe(dueno);
                textoZona.text = FormatoTerritorio.Linea(territorio, zona);
                textoZona.color = colorDueno;
                panelZona.gameObject.SetActive(true);
                PiezasUI piezas = PiezasUI.Actual;
                insignia.sprite = piezas != null ? piezas.Insignia(dueno) : null;
                insignia.enabled = insignia.sprite != null;
                rellenoRespeto.color = colorDueno;
                RectTransform rr = rellenoRespeto.rectTransform;
                rr.anchorMax = new Vector2(Mathf.Clamp01(territorio.Respeto(zona) / MaximoRespeto), 1f);
            }
            if (textoAviso.text.Length > 0 && Time.unscaledTime >= finAviso)
            {
                textoAviso.text = string.Empty;
            }
        }

        private void AlCambiar(string zona)
        {
            if (!territorio.JefeDisponible(zona))
            {
                jefesAvisados.Remove(zona);
            }
            else if (jefesAvisados.Add(zona))
            {
                Avisar(FormatoTerritorio.Cortar("¡EL JEFE DE " + territorio.Nombre(zona) + " TE BUSCA!"), FormatoTerritorio.ColorDe(territorio.Dueno(zona)), segundosJefe, TipoAviso.Malo);
            }
        }

        private void AlConquistar(string zona, IdPandilla anterior)
        {
            Avisar(FormatoTerritorio.Cortar(territorio.Nombre(zona) + " ES DE " + FormatoTerritorio.NombreDe(IdPandilla.Jugador)), FormatoTerritorio.ColorDe(IdPandilla.Jugador), segundosConquista, TipoAviso.Pandilla);
        }

        private void Avisar(string texto, Color color, float segundos, TipoAviso tipo)
        {
            textoAviso.text = texto;
            textoAviso.color = color;
            finAviso = Time.unscaledTime + segundos;
            ColaAvisos.Publicar(texto, PrioridadAviso.Alta, tipo, segundos);
        }
    }
}

using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Mapa;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// Fila de aliados abajo a la derecha, sobre el arma (T-405; T-408 la movió para no encimarse con la vida y los Biits): con su nombre, nivel y barra de vida, y "¡CHATO SUBIÓ A NIVEL II!" 3 s al subir.
    /// Lee <see cref="IPandillaJugador"/>; crea su propio canvas.
    /// </summary>
    public class HudAliados : MonoBehaviour
    {
        private const int MaximoFilas = 5;
        private const float AnchoAviso = 1400f;
        private const float AltoFila = 34f;
        private const float AltoCabecera = 30f;
        private const float RellenoPanel = 12f;
        private const float AnchoBarra = 120f;
        private const float AltoBarra = 10f;
        private static readonly string[] Romanos = { "I", "II", "III" };

        [Tooltip("Segundos que dura el aviso de nivel (s).")]
        [Min(0.5f)] [SerializeField] private float segundosAviso = 3f;

        private Text textoFila;
        private Text textoAviso;
        private RectTransform panel;
        private Text[] nombres;
        private Image[] rellenos;
        private IPandillaJugador pandilla;
        private float finAviso;
        private int ultimaCantidad = -1;
        private readonly StringBuilder constructor = new StringBuilder();

        /// <summary>La cabecera del panel ("PANDILLA · TE SIGUEN (F)"); vacía si no hay aliados.</summary>
        public Text TextoFila => textoFila;
        public Text TextoAviso => textoAviso;
        /// <summary>Filas de aliados que se ven ahora (T-521).</summary>
        public int FilasVisibles { get; private set; }
        public RectTransform Panel => panel;

        private void Awake()
        {
            // T-521: aliados compactos junto al minimapa (abajo a la izquierda): un panel con la cabecera y una fila por aliado (nombre, nivel y barra de vida).
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasAliados", HudTerritorio.OrdenCanvas);
            Image marco = KitUI.CrearPanelMarco(canvas.transform, "PanelAliados", 0.7f);
            panel = marco.rectTransform;
            KitUI.Colocar(panel, DisposicionHud.Pandilla, DisposicionHud.AnchoAliados);
            textoFila = KitUI.CrearTexto(panel, "Cabecera", RolTexto.Ayuda, TextAnchor.MiddleLeft);
            nombres = new Text[MaximoFilas];
            rellenos = new Image[MaximoFilas];
            for (int i = 0; i < MaximoFilas; i++)
            {
                nombres[i] = KitUI.CrearTexto(panel, "Aliado" + i, RolTexto.Hud, TextAnchor.MiddleLeft);
                Image hueco = KitUI.CrearImagen(panel, "Vida" + i, new Color(0f, 0f, 0f, 0.5f));
                rellenos[i] = KitUI.CrearImagen(hueco.transform, "Relleno", KitUI.Estilo.Turquesa);
                KitUI.Estirar(rellenos[i].rectTransform);
                rellenos[i].rectTransform.pivot = new Vector2(0f, 0.5f);
                nombres[i].gameObject.SetActive(false);
                hueco.gameObject.SetActive(false);
            }
            textoAviso = KitUI.CrearTexto(canvas.transform, "AvisoNivel", RolTexto.Boton, TextAnchor.MiddleCenter);
            KitUI.Colocar(textoAviso.rectTransform, DisposicionHud.AvisoAliados, AnchoAviso);
            textoFila.text = string.Empty;
            textoAviso.text = string.Empty;
            panel.gameObject.SetActive(false);
            textoAviso.enabled = false; // T-523: el aviso sale por la cola del centro; aquí queda solo como dato
        }

        private void OnDisable() => Desvincular();

        private void Desvincular()
        {
            if (pandilla != null)
            {
                pandilla.SubioDeNivel -= AlSubir;
                pandilla = null;
            }
        }

        private void AlSubir(int indice)
        {
            string nombre = pandilla.Nombre(indice).Replace("EL ", string.Empty).Replace("LA ", string.Empty);
            textoAviso.text = "¡" + nombre + " SUBIÓ A NIVEL " + Romanos[pandilla.Nivel(indice) - 1] + "!";
            finAviso = Time.unscaledTime + segundosAviso;
            ColaAvisos.Publicar(textoAviso.text, PrioridadAviso.Normal, TipoAviso.Pandilla, segundosAviso);
        }

        private void Update()
        {
            IPandillaJugador actual = Registro<IPandillaJugador>.Actual;
            if (!ReferenceEquals(actual, pandilla))
            {
                Desvincular();
                pandilla = actual;
                ultimaCantidad = pandilla != null ? pandilla.Cantidad : -1;
                if (pandilla != null)
                {
                    pandilla.SubioDeNivel += AlSubir;
                }
            }
            Color colorPandilla = FormatoTerritorio.ColorDe(IdPandilla.Jugador);
            textoFila.color = colorPandilla;
            textoAviso.color = colorPandilla;
            if (pandilla != null && pandilla.Cantidad > ultimaCantidad && ultimaCantidad >= 0)
            {
                textoAviso.text = FormatoTerritorio.Cortar(pandilla.Nombre(pandilla.Cantidad - 1) + " SE UNE A " + FormatoTerritorio.NombreDe(IdPandilla.Jugador));
                finAviso = Time.unscaledTime + segundosAviso;
                ColaAvisos.Publicar(textoAviso.text, PrioridadAviso.Alta, TipoAviso.Pandilla, segundosAviso);
            }
            ultimaCantidad = pandilla != null ? pandilla.Cantidad : -1;
            int cantidad = pandilla != null ? Mathf.Min(pandilla.Cantidad, MaximoFilas) : 0;
            FilasVisibles = cantidad;
            panel.gameObject.SetActive(cantidad > 0);
            if (cantidad > 0)
            {
                textoFila.text = "PANDILLA · " + (pandilla.Siguiendo ? "TE SIGUEN" : "ESPERAN") + " (F)";
                textoFila.color = colorPandilla;
                float alto = RellenoPanel * 2f + AltoCabecera + cantidad * AltoFila;
                panel.sizeDelta = new Vector2(DisposicionHud.AnchoAliados, alto);
                RectTransform cab = textoFila.rectTransform;
                cab.anchorMin = new Vector2(0f, 1f);
                cab.anchorMax = new Vector2(1f, 1f);
                cab.pivot = new Vector2(0.5f, 1f);
                cab.offsetMin = new Vector2(RellenoPanel + 10f, -RellenoPanel - AltoCabecera);
                cab.offsetMax = new Vector2(-RellenoPanel, -RellenoPanel);
                for (int i = 0; i < MaximoFilas; i++)
                {
                    bool hay = i < cantidad;
                    nombres[i].gameObject.SetActive(hay);
                    rellenos[i].transform.parent.gameObject.SetActive(hay);
                    if (!hay)
                    {
                        continue;
                    }
                    float arriba = -RellenoPanel - AltoCabecera - i * AltoFila;
                    string nombre = pandilla.Nombre(i).Replace("EL ", string.Empty).Replace("LA ", string.Empty);
                    nombres[i].text = nombre + " " + Romanos[pandilla.Nivel(i) - 1];
                    nombres[i].color = KitUI.Estilo.Texto;
                    RectTransform rn = nombres[i].rectTransform;
                    rn.anchorMin = new Vector2(0f, 1f);
                    rn.anchorMax = new Vector2(1f, 1f);
                    rn.pivot = new Vector2(0.5f, 1f);
                    rn.offsetMin = new Vector2(RellenoPanel + 10f, arriba - AltoFila);
                    rn.offsetMax = new Vector2(-RellenoPanel - AnchoBarra - 10f, arriba);
                    RectTransform rb = (RectTransform)rellenos[i].transform.parent;
                    rb.anchorMin = rb.anchorMax = rb.pivot = new Vector2(1f, 1f);
                    rb.sizeDelta = new Vector2(AnchoBarra, AltoBarra);
                    rb.anchoredPosition = new Vector2(-RellenoPanel - 6f, arriba - (AltoFila - AltoBarra) * 0.5f);
                    float vida = Mathf.Clamp01(pandilla.Vida(i));
                    rellenos[i].rectTransform.anchorMax = new Vector2(vida, 1f);
                    rellenos[i].color = vida <= 0.3f ? KitUI.Estilo.Enemigo : KitUI.Estilo.Turquesa;
                }
            }
            else
            {
                textoFila.text = string.Empty;
            }
            if (textoAviso.text.Length > 0 && Time.unscaledTime >= finAviso)
            {
                textoAviso.text = string.Empty;
            }
        }
    }
}

using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Mapa;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// Fila de aliados abajo a la derecha, sobre el arma (T-405; T-408 la movió para no encimarse con la vida y los Biits): "CHATO II ▮▮▮▯" con su vida, y "¡CHATO SUBIÓ A NIVEL II!" 3 s al subir.
    /// Lee <see cref="IPandillaJugador"/>; crea su propio canvas.
    /// </summary>
    public class HudAliados : MonoBehaviour
    {
        private const int Barras = 4;
        private const float AnchoFila = 440f;
        private const float AnchoAviso = 1400f;
        private static readonly string[] Romanos = { "I", "II", "III" };

        [Tooltip("Segundos que dura el aviso de nivel (s).")]
        [Min(0.5f)] [SerializeField] private float segundosAviso = 3f;

        private Text textoFila;
        private Text textoAviso;
        private IPandillaJugador pandilla;
        private float finAviso;
        private int ultimaCantidad = -1;
        private readonly StringBuilder constructor = new StringBuilder();

        public Text TextoFila => textoFila;
        public Text TextoAviso => textoAviso;

        private void Awake()
        {
            // T-408: el kit y las ranuras de DisposicionHud: la pandilla abajo a la derecha sobre el arma (crece hacia arriba) y el aviso bajo el de zona.
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasAliados", HudTerritorio.OrdenCanvas);
            textoFila = KitUI.CrearTexto(canvas.transform, "Aliados", RolTexto.Hud, TextAnchor.LowerRight);
            KitUI.Colocar(textoFila.rectTransform, DisposicionHud.Pandilla, AnchoFila);
            textoAviso = KitUI.CrearTexto(canvas.transform, "AvisoNivel", RolTexto.Boton, TextAnchor.MiddleCenter);
            KitUI.Colocar(textoAviso.rectTransform, DisposicionHud.AvisoAliados, AnchoAviso);
            textoFila.text = string.Empty;
            textoAviso.text = string.Empty;
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
            }
            ultimaCantidad = pandilla != null ? pandilla.Cantidad : -1;
            constructor.Clear();
            if (pandilla != null && pandilla.Cantidad > 0)
            {
                constructor.Append("PANDILLA · ").Append(pandilla.Siguiendo ? "TE SIGUEN" : "ESPERAN").Append(" (F)\n");
                for (int i = 0; i < pandilla.Cantidad; i++)
                {
                    string nombre = pandilla.Nombre(i).Replace("EL ", string.Empty).Replace("LA ", string.Empty);
                    int llenas = Mathf.CeilToInt(pandilla.Vida(i) * Barras);
                    constructor.Append(nombre).Append(' ').Append(Romanos[pandilla.Nivel(i) - 1]).Append(' ');
                    constructor.Append('▮', llenas).Append('▯', Barras - llenas).Append('\n');
                }
            }
            textoFila.text = constructor.ToString().TrimEnd('\n');
            if (textoAviso.text.Length > 0 && Time.unscaledTime >= finAviso)
            {
                textoAviso.text = string.Empty;
            }
        }
    }
}

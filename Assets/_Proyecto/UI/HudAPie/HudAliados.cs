using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Mapa;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// Fila de aliados abajo a la izquierda (T-405): "CHATO II ▮▮▮▯" con su vida, y "¡CHATO SUBIÓ A NIVEL II!" 3 s al subir.
    /// Lee <see cref="IPandillaJugador"/>; crea su propio canvas.
    /// </summary>
    public class HudAliados : MonoBehaviour
    {
        private const int Barras = 4;
        private static readonly string[] Romanos = { "I", "II", "III" };

        [Tooltip("Segundos que dura el aviso de nivel (s).")]
        [Min(0.5f)] [SerializeField] private float segundosAviso = 3f;

        private Text textoFila;
        private Text textoAviso;
        private IPandillaJugador pandilla;
        private float finAviso;
        private readonly StringBuilder constructor = new StringBuilder();

        public Text TextoFila => textoFila;
        public Text TextoAviso => textoAviso;

        private void Awake()
        {
            Canvas canvas = FormatoTerritorio.CrearCanvas(transform, "CanvasAliados", 21);
            textoFila = FormatoTerritorio.CrearTexto(canvas.transform, "Aliados", 26, TextAnchor.LowerLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 20f), new Vector2(700f, 140f));
            textoAviso = FormatoTerritorio.CrearTexto(canvas.transform, "AvisoNivel", 40, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-450f, -190f), new Vector2(900f, 60f));
            textoFila.color = new Color(1f, 0.5f, 0.1f);
            textoAviso.color = new Color(1f, 0.5f, 0.1f);
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
                if (pandilla != null)
                {
                    pandilla.SubioDeNivel += AlSubir;
                }
            }
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
            textoFila.text = constructor.ToString();
            if (textoAviso.text.Length > 0 && Time.unscaledTime >= finAviso)
            {
                textoAviso.text = string.Empty;
            }
        }
    }
}

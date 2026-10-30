using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Mapa;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// Arriba a la izquierda dice en qué zona estás, de quién es y tu respeto, con el color del dueño (T-404). Avisa "¡EL JEFE TE BUSCA!"
    /// cuando el jefe de una zona queda disponible y "&lt;ZONA&gt; ES TUYO" 3 s al conquistarla. Crea su propio canvas; no depende de HudAPie.
    /// </summary>
    public class HudTerritorio : MonoBehaviour
    {
        // Debajo de la línea de la radio (arriba a la izquierda), para que no se encimen.
        private const float ArribaZona = 70f;

        [Tooltip("Segundos que dura el aviso de conquista (s).")]
        [Min(0.5f)] [SerializeField] private float segundosConquista = 3f;
        [Tooltip("Segundos que dura el aviso del jefe (s).")]
        [Min(0.5f)] [SerializeField] private float segundosJefe = 4f;

        private Text textoZona;
        private Text textoAviso;
        private ITerritorio territorio;
        private float finAviso;
        private readonly System.Collections.Generic.HashSet<string> jefesAvisados = new System.Collections.Generic.HashSet<string>();

        public Text TextoZona => textoZona;
        public Text TextoAviso => textoAviso;

        private void Awake()
        {
            Canvas canvas = FormatoTerritorio.CrearCanvas(transform, "CanvasTerritorio", 20);
            textoZona = FormatoTerritorio.CrearTexto(canvas.transform, "Zona", 28, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -ArribaZona), new Vector2(900f, 40f));
            textoAviso = FormatoTerritorio.CrearTexto(canvas.transform, "Aviso", 44, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-450f, -120f), new Vector2(900f, 60f));
            textoZona.text = string.Empty;
            textoAviso.text = string.Empty;
        }

        private void OnDisable() => Desvincular();

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
            IJugador jugador = Registro<IJugador>.Actual;
            string zona = territorio != null && jugador != null && jugador.Raiz != null ? territorio.ZonaEn(jugador.Raiz.position) : string.Empty;
            if (zona.Length == 0)
            {
                textoZona.text = string.Empty;
            }
            else
            {
                textoZona.text = FormatoTerritorio.Linea(territorio, zona);
                textoZona.color = FormatoTerritorio.ColorDe(territorio.Dueno(zona));
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
                Avisar("¡EL JEFE TE BUSCA!", Color.red, segundosJefe);
            }
        }

        private void AlConquistar(string zona, IdPandilla anterior)
        {
            Avisar(territorio.Nombre(zona) + " ES TUYO", FormatoTerritorio.ColorDe(IdPandilla.Jugador), segundosConquista);
        }

        private void Avisar(string texto, Color color, float segundos)
        {
            textoAviso.text = texto;
            textoAviso.color = color;
            finAviso = Time.unscaledTime + segundos;
        }
    }
}

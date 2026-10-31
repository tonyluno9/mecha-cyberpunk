using MechaCyberpunk.Core;
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
        private const float AnchoZona = 900f;
        private const float AnchoAviso = 1400f;

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
            // T-408: el kit y las ranuras de DisposicionHud (zona bajo la radio y la renta; aviso bajo el objetivo).
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasTerritorio", OrdenCanvas);
            textoZona = KitUI.CrearTexto(canvas.transform, "Zona", RolTexto.Hud, TextAnchor.MiddleLeft);
            KitUI.Colocar(textoZona.rectTransform, DisposicionHud.Zona, AnchoZona);
            textoAviso = KitUI.CrearTexto(canvas.transform, "Aviso", RolTexto.Encabezado, TextAnchor.MiddleCenter);
            KitUI.Colocar(textoAviso.rectTransform, DisposicionHud.AvisoZona, AnchoAviso);
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
                Avisar(FormatoTerritorio.Cortar("¡EL JEFE DE " + territorio.Nombre(zona) + " TE BUSCA!"), FormatoTerritorio.ColorDe(territorio.Dueno(zona)), segundosJefe);
            }
        }

        private void AlConquistar(string zona, IdPandilla anterior)
        {
            Avisar(FormatoTerritorio.Cortar(territorio.Nombre(zona) + " ES DE " + FormatoTerritorio.NombreDe(IdPandilla.Jugador)), FormatoTerritorio.ColorDe(IdPandilla.Jugador), segundosConquista);
        }

        private void Avisar(string texto, Color color, float segundos)
        {
            textoAviso.text = texto;
            textoAviso.color = color;
            finAviso = Time.unscaledTime + segundos;
        }
    }
}

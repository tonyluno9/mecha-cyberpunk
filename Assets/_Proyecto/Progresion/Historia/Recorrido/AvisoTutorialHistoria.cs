using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Rueda;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T-1662 (T44): un aviso de tutorial de la historia, abajo al centro (más arriba que el aviso ligero y el de prótesis para no encimarse).
    /// Se muestra con <see cref="Mostrar"/>; se quita solo a los N segundos (contados solo mientras se ve) o cuando la rueda de armas se abrió
    /// (si <c>hastaAbrirRueda</c>). No cuenta tiempo con pantallas abiertas ni en pausa. Lo crea el <see cref="DirectorRecorrido"/>.
    /// </summary>
    public class AvisoTutorialHistoria : MonoBehaviour
    {
        public const int OrdenCanvas = 13;
        private const float MargenAbajo = 230f;
        private static readonly Vector2 TamanoCuadro = new Vector2(1100f, 60f);

        public static AvisoTutorialHistoria Actual { get; private set; }

        private GameObject cuadro;
        private Text texto;
        private float restante;
        private bool hastaAbrirRueda;
        private int aperturasBase;

        public bool Visible => cuadro != null && cuadro.activeSelf;
        public string TextoActual => texto != null ? texto.text : string.Empty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => Actual = null;

        private void Awake()
        {
            Actual = this;
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasAvisoHistoria", OrdenCanvas);
            RectTransform panel = KitUI.CrearImagen(canvas.transform, "Aviso", new Color(0.02f, 0.02f, 0.05f, 0.7f)).rectTransform;
            KitUI.Colocar(panel, new Vector2(0.5f, 0f), new Vector2(0f, MargenAbajo), TamanoCuadro);
            KitUI.CrearBorde(panel, 1.5f, KitUI.Estilo.Turquesa);
            cuadro = panel.gameObject;
            texto = KitUI.CrearTexto(panel, "Texto", RolTexto.Ayuda, TextAnchor.MiddleCenter);
            KitUI.Estirar(texto.rectTransform);
            texto.color = KitUI.Estilo.Turquesa;
            texto.fontSize = 24;
            cuadro.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Actual == this)
            {
                Actual = null;
            }
        }

        /// <summary>Muestra el aviso <paramref name="segundos"/> s (solo cuentan mientras se ve). Con <paramref name="alAbrirRueda"/> se quita en cuanto se abre la rueda de armas.</summary>
        public void Mostrar(string aviso, float segundos, bool alAbrirRueda)
        {
            texto.text = aviso;
            restante = segundos;
            hastaAbrirRueda = alAbrirRueda;
            aperturasBase = RuedaArmasObjetos.Aperturas;
        }

        public void Quitar()
        {
            restante = 0f;
            if (cuadro != null)
            {
                cuadro.SetActive(false);
            }
        }

        private void Update()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            bool enJuego = restante > 0f && jugador != null && jugador.Raiz != null && !FlujoPartida.EnTitulo && !Pausa.Activa
                && PilaPantallas.Vacia && !CinematicaEnCurso.Activa;
            if (hastaAbrirRueda && RuedaArmasObjetos.Aperturas != aperturasBase)
            {
                Quitar();
                return;
            }
            if (cuadro.activeSelf != enJuego)
            {
                cuadro.SetActive(enJuego);
            }
            if (enJuego)
            {
                restante -= Time.unscaledDeltaTime;
            }
        }
    }
}

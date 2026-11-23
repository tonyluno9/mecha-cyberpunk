using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Habilidades;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.UI.Clinica;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Rueda;
using MechaCyberpunk.Combate.Golpe;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// T-1658: tutorial en pantalla de las prótesis, en pasos, al salir a la calle tras la primera visita al Doc (<see cref="PrimeraVisitaClinica.TutorialPendiente"/>):
    /// mantener TAB, activar PIERNAS y correr, activar BRAZOS y golpear. Cada paso avanza cuando el jugador lo hace (<see cref="ReglaTutorialProtesis"/>).
    /// Lo agrega <see cref="HudAPie"/>; construye su canvas, más arriba y más grande que el aviso del <see cref="TutorialLigero"/>.
    /// </summary>
    public class TutorialProtesis : MonoBehaviour
    {
        public const int OrdenCanvas = 14;
        private const float MargenAbajo = 150f;
        private static readonly Vector2 TamanoCuadro = new Vector2(1100f, 90f);
        private const float RapidezMinimaCorrer = 0.5f;

        private readonly ReglaTutorialProtesis regla = new ReglaTutorialProtesis();
        private GameObject cuadro;
        private Text texto;
        private Transform raiz;
        private ControladorAPie controlador;
        private GolpeAPie golpe;
        private HabilidadesProtesis habilidades;
        private int aperturasBase = -1;
        private bool activoAntes;

        public ReglaTutorialProtesis Regla => regla;
        public bool Visible => cuadro != null && cuadro.activeSelf;
        public string TextoActual => texto != null ? texto.text : string.Empty;

        private void Awake()
        {
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasTutorialProtesis", OrdenCanvas);
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

        private void Update()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            bool enJuego = PrimeraVisitaClinica.TutorialPendiente && jugador != null && jugador.Raiz != null && jugador.Modo == ModoJugador.APie
                && !FlujoPartida.EnTitulo && !Pausa.Activa && PilaPantallas.Vacia;
            if (!enJuego)
            {
                Ocultar();
                return;
            }
            if (jugador.Raiz != raiz)
            {
                raiz = jugador.Raiz;
                controlador = raiz.GetComponentInChildren<ControladorAPie>();
                golpe = raiz.GetComponentInChildren<GolpeAPie>();
                habilidades = raiz.GetComponentInChildren<HabilidadesProtesis>();
            }
            if (!activoAntes)
            {
                activoAntes = true;
                regla.Reiniciar();
                aperturasBase = RuedaHabilidadesExo.Aperturas;
            }
            bool piernas = habilidades != null && habilidades.Estado(HabilidadProtesis.Piernas) == EstadoHabilidad.Activa;
            bool brazos = habilidades != null && habilidades.Estado(HabilidadProtesis.Brazos) == EstadoHabilidad.Activa;
            bool abrio = RuedaHabilidadesExo.Aperturas != aperturasBase;
            bool corre = piernas && controlador != null && controlador.PidiendoCorrer && controlador.Velocidad.sqrMagnitude > RapidezMinimaCorrer * RapidezMinimaCorrer;
            bool golpea = brazos && golpe != null && golpe.Fase != FaseGolpe.Listo;
            bool tienePiernas = habilidades != null && habilidades.Nivel(HabilidadProtesis.Piernas) > 0;
            bool tieneBrazos = habilidades != null && habilidades.Nivel(HabilidadProtesis.Brazos) > 0;
            regla.Avanzar(abrio, piernas, corre, brazos, golpea, tienePiernas);
            PrimeraVisitaClinica.PasoTutorial = regla.Paso;
            if (regla.Paso == PasoTutorialProtesis.Hecho)
            {
                PrimeraVisitaClinica.TerminarTutorial();
                Ocultar();
                return;
            }
            string tecla = TeclasHud.Texto(TeclasHud.Buscar(ReglaTutorial.TeclaRuedaExo), ReglaTutorial.TeclaPorOmision(ReglaTutorial.TeclaRuedaExo)).ToUpperInvariant();
            string aviso = regla.Texto(tecla, tienePiernas, tieneBrazos);
            if (string.IsNullOrEmpty(aviso))
            {
                cuadro.SetActive(false);
                return;
            }
            texto.text = aviso;
            cuadro.SetActive(true);
        }

        private void Ocultar()
        {
            activoAntes = false;
            if (cuadro != null && cuadro.activeSelf)
            {
                cuadro.SetActive(false);
            }
        }

        /// <summary>Depuración (invocable por reflexión en Play): el paso del tutorial de prótesis y su texto.</summary>
        public static string Depurar()
        {
            TutorialProtesis t = FindAnyObjectByType<TutorialProtesis>();
            return PrimeraVisitaClinica.Depurar() + (t != null ? " visible=" + t.Visible + " texto=\"" + t.TextoActual + "\"" : " (sin TutorialProtesis en escena)");
        }
    }
}

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

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// T-1658 y T-62: tutorial en pantalla de las prótesis, en pasos, al salir a la calle tras la primera visita al Doc (<see cref="PrimeraVisitaClinica.TutorialPendiente"/>):
    /// mantener TAB, activar PIERNAS y correr, activar BRAZOS y golpear. Cada paso avanza cuando el jugador lo hace (<see cref="ReglaTutorialProtesis"/>).
    /// El aviso de cada paso sale UNA sola vez en una ventana al centro con ENTENDIDO (<see cref="VentanaAviso"/>); al completar el tutorial se marca
    /// <see cref="AvisosUnicos.Protesis"/> (se guarda en la partida) y ya no vuelve a salir. Lo agrega <see cref="HudAPie"/>.
    /// </summary>
    public class TutorialProtesis : MonoBehaviour
    {
        private const float RapidezMinimaCorrer = 0.5f;
        private const string PrefijoPaso = "protesis_paso_";

        private readonly ReglaTutorialProtesis regla = new ReglaTutorialProtesis();
        private string ultimoTexto = string.Empty;
        private Transform raiz;
        private ControladorAPie controlador;
        private GolpeAPie golpe;
        private HabilidadesProtesis habilidades;
        private int aperturasBase = -1;
        private bool activoAntes;

        public ReglaTutorialProtesis Regla => regla;
        public bool Visible => VentanaAviso.Abierta && VentanaAviso.TextoActual == ultimoTexto;
        public string TextoActual => ultimoTexto;

        private void Update()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (AvisosUnicos.YaVisto(AvisosUnicos.Protesis))
            {
                PrimeraVisitaClinica.TerminarTutorial();
            }
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
                AvisosUnicos.Marcar(AvisosUnicos.Protesis);
                PrimeraVisitaClinica.TerminarTutorial();
                Ocultar();
                return;
            }
            string tecla = TeclasHud.Texto(TeclasHud.Buscar(ReglaTutorial.TeclaRuedaExo), ReglaTutorial.TeclaPorOmision(ReglaTutorial.TeclaRuedaExo)).ToUpperInvariant();
            string aviso = regla.Texto(tecla, tienePiernas, tieneBrazos);
            if (string.IsNullOrEmpty(aviso))
            {
                return;
            }
            ultimoTexto = aviso;
            // T-62: cada paso se avisa una sola vez, en la ventana del centro.
            if (!VentanaAviso.Abierta && AvisosUnicos.Marcar(PrefijoPaso + (int)regla.Paso))
            {
                VentanaAviso.Mostrar(aviso);
            }
        }

        private void Ocultar()
        {
            activoAntes = false;
        }

        /// <summary>Depuración (invocable por reflexión en Play): el paso del tutorial de prótesis y su texto.</summary>
        public static string Depurar()
        {
            TutorialProtesis t = FindAnyObjectByType<TutorialProtesis>();
            return PrimeraVisitaClinica.Depurar() + (t != null ? " visible=" + t.Visible + " texto=\"" + t.TextoActual + "\"" : " (sin TutorialProtesis en escena)");
        }
    }
}

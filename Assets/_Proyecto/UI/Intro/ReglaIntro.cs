namespace MechaCyberpunk.UI.Intro
{
    /// <summary>
    /// Máquina de estados pura de la intro (sin MonoBehaviour, para probarla en EditMode): Inactiva, Video, Terminada. Termina cuando el
    /// video acaba, cuando se salta, cuando falla o cuando tarda demasiado en prepararse (para no dejar al jugador en negro).
    /// </summary>
    public class ReglaIntro
    {
        private readonly float segundosMaximoPreparar;
        private float esperando;

        public ReglaIntro(float segundosMaximoPreparar)
        {
            this.segundosMaximoPreparar = segundosMaximoPreparar;
        }

        public FaseIntro Fase { get; private set; } = FaseIntro.Inactiva;

        /// <summary>True si el jugador la saltó (Enter, clic o Escape).</summary>
        public bool Saltada { get; private set; }

        /// <summary>Con video empieza en <see cref="FaseIntro.Video"/>; sin video pasa directo a <see cref="FaseIntro.Terminada"/>.</summary>
        public void Iniciar(bool hayVideo)
        {
            esperando = 0f;
            Saltada = false;
            Fase = hayVideo ? FaseIntro.Video : FaseIntro.Terminada;
        }

        public void Saltar()
        {
            if (Fase == FaseIntro.Video)
            {
                Saltada = true;
                Fase = FaseIntro.Terminada;
            }
        }

        public void AlAcabarVideo()
        {
            if (Fase == FaseIntro.Video)
            {
                Fase = FaseIntro.Terminada;
            }
        }

        /// <summary>Avanza el reloj de espera: si el video no está preparado a tiempo, la intro termina.</summary>
        public void Actualizar(float dt, bool videoPreparado)
        {
            if (Fase != FaseIntro.Video || videoPreparado)
            {
                return;
            }
            esperando += dt;
            if (esperando >= segundosMaximoPreparar)
            {
                Fase = FaseIntro.Terminada;
            }
        }
    }
}

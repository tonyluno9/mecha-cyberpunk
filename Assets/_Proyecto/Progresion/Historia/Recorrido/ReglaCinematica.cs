using UnityEngine;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T-1625: lógica pura del tiempo de una cinemática. Fundido a negro (0.5 s), contenido (exactamente la duración), fundido de vuelta (0.5 s).
    /// ENTER salta el contenido (no el fundido de entrada). El tiempo entra como parámetro.
    /// </summary>
    public sealed class ReglaCinematica
    {
        public enum Fase { FundidoEntrada, Contenido, FundidoSalida, Terminada }

        public const float SegundosFundido = 0.5f;

        private readonly float duracion;
        private float enFase;

        public ReglaCinematica(float duracionContenido)
        {
            duracion = Mathf.Max(0f, duracionContenido);
        }

        public Fase Actual { get; private set; } = Fase.FundidoEntrada;
        public float Duracion => duracion;
        public float EnFase => enFase;

        /// <summary>Opacidad del negro: sube en la entrada, llena en el contenido y baja en la salida.</summary>
        public float Alfa
        {
            get
            {
                switch (Actual)
                {
                    case Fase.FundidoEntrada: return Mathf.Clamp01(enFase / SegundosFundido);
                    case Fase.Contenido: return 1f;
                    case Fase.FundidoSalida: return 1f - Mathf.Clamp01(enFase / SegundosFundido);
                    default: return 0f;
                }
            }
        }

        /// <summary>0 a 1 del contenido (la barra de progreso).</summary>
        public float Progreso
        {
            get
            {
                switch (Actual)
                {
                    case Fase.FundidoEntrada: return 0f;
                    case Fase.Contenido: return duracion <= 0f ? 1f : Mathf.Clamp01(enFase / duracion);
                    default: return 1f;
                }
            }
        }

        public bool PuedeSaltar => Actual == Fase.Contenido;

        /// <summary>Avanza <paramref name="dt"/> s. Devuelve la fase nueva si cambió (para avisar), o null si no.</summary>
        public Fase? Avanzar(float dt, bool saltar)
        {
            if (Actual == Fase.Terminada)
            {
                return null;
            }
            if (saltar && Actual == Fase.Contenido)
            {
                return Pasar(Fase.FundidoSalida);
            }
            enFase += Mathf.Max(0f, dt);
            switch (Actual)
            {
                case Fase.FundidoEntrada when enFase >= SegundosFundido:
                    return Pasar(Fase.Contenido, enFase - SegundosFundido);
                case Fase.Contenido when enFase >= duracion:
                    return Pasar(Fase.FundidoSalida, enFase - duracion);
                case Fase.FundidoSalida when enFase >= SegundosFundido:
                    return Pasar(Fase.Terminada);
            }
            return null;
        }

        private Fase Pasar(Fase nueva, float sobrante = 0f)
        {
            Actual = nueva;
            enFase = nueva == Fase.Contenido || nueva == Fase.FundidoSalida ? sobrante : 0f;
            if (nueva == Fase.Contenido && duracion <= 0f)
            {
                return Pasar(Fase.FundidoSalida, sobrante);
            }
            return Actual;
        }
    }
}

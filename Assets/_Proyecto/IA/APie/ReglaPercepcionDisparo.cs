using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1494: memoria de un guardia que oyó o recibió un disparo. Tranquilo; Investigando (va a la última posición conocida del jugador); Buscando (llegó y
    /// no lo ve: mira alrededor unos segundos); Viendo (lo tiene a la vista, actualiza su última posición). Lógica pura, sin cuerpo ni física.
    /// </summary>
    public class ReglaPercepcionDisparo
    {
        public enum Estado
        {
            Tranquilo,
            Investigando,
            Buscando,
            Viendo
        }

        /// <summary>Si en tanto tiempo (s) no llega a la última posición conocida, se rinde.</summary>
        public const float SegundosMaxInvestigar = 40f;

        private readonly float segundosBusqueda;
        private readonly float distanciaLlegada;
        private float temporizador;
        private float segundosInvestigando;

        public ReglaPercepcionDisparo(float segundosBusqueda, float distanciaLlegada)
        {
            this.segundosBusqueda = segundosBusqueda;
            this.distanciaLlegada = distanciaLlegada;
        }

        public Estado Actual { get; private set; } = Estado.Tranquilo;

        /// <summary>Última posición conocida del jugador.</summary>
        public Vector3 Ultima { get; private set; }

        /// <summary>True si está investigando, buscando o viendo (el guardia está en alerta).</summary>
        public bool Activa => Actual != Estado.Tranquilo;

        /// <summary>Un impacto o un disparo oído: va hacia <paramref name="punto"/>. Si ya lo está viendo, no pierde la vista.</summary>
        public void Avisar(Vector3 punto)
        {
            if (Actual == Estado.Viendo)
            {
                return;
            }
            Ultima = punto;
            Actual = Estado.Investigando;
            temporizador = 0f;
            segundosInvestigando = 0f;
        }

        public void Reiniciar()
        {
            Actual = Estado.Tranquilo;
            temporizador = 0f;
            segundosInvestigando = 0f;
        }

        /// <summary>Un paso: <paramref name="ve"/> si ahora ve al jugador (en <paramref name="posJugador"/>); <paramref name="posPropia"/> es la del guardia.</summary>
        public void Avanzar(float dt, bool ve, Vector3 posJugador, Vector3 posPropia)
        {
            if (Actual == Estado.Tranquilo)
            {
                return;
            }
            if (ve)
            {
                Ultima = posJugador;
                Actual = Estado.Viendo;
                temporizador = 0f;
                segundosInvestigando = 0f;
                return;
            }
            if (Actual == Estado.Viendo)
            {
                Actual = Estado.Investigando;
                segundosInvestigando = 0f;
            }
            if (Actual == Estado.Investigando)
            {
                segundosInvestigando += dt;
                if (DistanciaPlana(posPropia, Ultima) <= distanciaLlegada)
                {
                    Actual = Estado.Buscando;
                    temporizador = 0f;
                }
                else if (segundosInvestigando >= SegundosMaxInvestigar)
                {
                    Reiniciar();
                }
                return;
            }
            temporizador += dt; // Buscando
            if (temporizador >= segundosBusqueda)
            {
                Reiniciar();
            }
        }

        private static float DistanciaPlana(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return Mathf.Sqrt(x * x + z * z);
        }
    }
}

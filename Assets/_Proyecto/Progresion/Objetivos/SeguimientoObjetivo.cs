using System;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>
    /// Lógica pura que sigue al jugador respecto a la posición del objetivo: distancia horizontal, mejor distancia,
    /// si se aleja (con dos márgenes para que no parpadee) y si llegó. Avisa la llegada una sola vez por objetivo.
    /// </summary>
    public class SeguimientoObjetivo
    {
        private readonly float radioLlegada;
        private readonly float margenAlejarse;
        private readonly float margenVolver;
        private readonly float tiempoMinimoAlejandose;

        private Vector3 posicionObjetivo;
        private bool hayMejor;
        private float tiempoAlejandose;

        public SeguimientoObjetivo(float radioLlegada, float margenAlejarse, float margenVolver, float tiempoMinimoAlejandose)
        {
            this.radioLlegada = radioLlegada;
            this.margenAlejarse = margenAlejarse;
            this.margenVolver = margenVolver;
            this.tiempoMinimoAlejandose = tiempoMinimoAlejandose;
        }

        /// <summary>True si ya se conoce la posición del objetivo.</summary>
        public bool PosicionResuelta { get; private set; }

        /// <summary>Posición del objetivo (m, mundo). Solo vale si <see cref="PosicionResuelta"/>.</summary>
        public Vector3 Posicion => posicionObjetivo;

        /// <summary>Distancia horizontal actual al objetivo (m). 0 mientras no se haya medido.</summary>
        public float Distancia { get; private set; }

        /// <summary>Menor distancia horizontal alcanzada desde que se fijó el objetivo (m).</summary>
        public float MejorDistancia { get; private set; }

        public bool SeAleja { get; private set; }

        /// <summary>True desde que el jugador entró al radio o se avisó <see cref="MarcarLlegada"/>, hasta el próximo reinicio.</summary>
        public bool Llego { get; private set; }

        /// <summary>Avisa el nuevo valor de <see cref="SeAleja"/> cada vez que cambia.</summary>
        public event Action<bool> SeAlejaCambio;

        /// <summary>Avisa una sola vez por objetivo que el jugador llegó.</summary>
        public event Action Llegada;

        /// <summary>Olvida todo: posición, distancias, alejamiento y llegada. Se usa al cambiar de objetivo.</summary>
        public void Reiniciar()
        {
            bool estabaAlejando = SeAleja;
            PosicionResuelta = false;
            posicionObjetivo = Vector3.zero;
            Distancia = 0f;
            MejorDistancia = 0f;
            hayMejor = false;
            tiempoAlejandose = 0f;
            SeAleja = false;
            Llego = false;
            if (estabaAlejando) SeAlejaCambio?.Invoke(false);
        }

        /// <summary>Indica dónde está el objetivo. Si la posición no cambió no hace nada.</summary>
        public void FijarPosicion(Vector3 posicion)
        {
            if (PosicionResuelta && posicionObjetivo == posicion) return;
            posicionObjetivo = posicion;
            PosicionResuelta = true;
        }

        /// <summary>El objetivo ya no se puede ubicar (por ejemplo su lugar salió del registro). No avanza hasta que vuelva.</summary>
        public void PerderPosicion()
        {
            PosicionResuelta = false;
        }

        /// <summary>Cuenta como llegada por otra vía (por ejemplo, entró al lugar). Avisa una sola vez.</summary>
        public void MarcarLlegada()
        {
            if (Llego) return;
            Llego = true;
            Llegada?.Invoke();
        }

        /// <summary>Avanza el seguimiento. Sin posición resuelta no hace nada.</summary>
        public void Avanzar(float dt, Vector3 posicionJugador)
        {
            if (!PosicionResuelta) return;

            Vector3 diferencia = posicionJugador - posicionObjetivo;
            diferencia.y = 0f;
            Distancia = diferencia.magnitude;
            if (!hayMejor || Distancia < MejorDistancia)
            {
                MejorDistancia = Distancia;
                hayMejor = true;
            }

            if (!SeAleja)
            {
                if (Distancia > MejorDistancia + margenAlejarse) tiempoAlejandose += dt;
                else tiempoAlejandose = 0f;
                if (tiempoAlejandose >= tiempoMinimoAlejandose && Distancia > MejorDistancia + margenAlejarse)
                {
                    SeAleja = true;
                    SeAlejaCambio?.Invoke(true);
                }
            }
            else if (Distancia <= MejorDistancia + margenVolver)
            {
                SeAleja = false;
                tiempoAlejandose = 0f;
                SeAlejaCambio?.Invoke(false);
            }

            if (!Llego && Distancia <= radioLlegada)
            {
                MarcarLlegada();
            }
        }
    }
}

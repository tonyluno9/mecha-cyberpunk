using System;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.Pelea.Emboscada
{
    /// <summary>
    /// Lógica pura de la emboscada: no usa Unity. Recibe el tiempo y lo que pasa en el mundo en <see cref="Avanzar"/>.
    /// Esperando, Aparicion, EsperandoMecha (se salta si el jugador ya va en el mecha), Peleando, Pausada (jugador fuera del
    /// mecha a media pelea) y Superada.
    /// </summary>
    public class MaquinaEmboscada
    {
        private readonly float segundosAparicion;
        private float tiempoAparicion;

        public MaquinaEmboscada(float segundosAparicion)
        {
            this.segundosAparicion = Math.Max(segundosAparicion, 0f);
        }

        public EstadoEmboscada Estado { get; private set; } = EstadoEmboscada.Esperando;

        /// <summary>Avisa una vez, al pasar de Esperando a Aparicion.</summary>
        public event Action Iniciada;
        /// <summary>Avisa cada cambio de estado, con el estado nuevo.</summary>
        public event Action<EstadoEmboscada> EstadoCambio;
        /// <summary>Avisa una sola vez, al pasar a Superada.</summary>
        public event Action Terminada;

        /// <summary>Empieza la emboscada si sigue en Esperando. Devuelve si empezó.</summary>
        public bool Empezar()
        {
            if (Estado != EstadoEmboscada.Esperando)
            {
                return false;
            }
            tiempoAparicion = 0f;
            Iniciada?.Invoke();
            Cambiar(EstadoEmboscada.Aparicion);
            return true;
        }

        /// <param name="dt">Segundos desde la llamada anterior.</param>
        /// <param name="jugadorCerca">True si el jugador entró al radio y la emboscada se activa sola.</param>
        /// <param name="modoJugador">Modo actual del jugador.</param>
        /// <param name="rivalesEnPie">Rivales que ya aparecieron y no están en KO.</param>
        public void Avanzar(float dt, bool jugadorCerca, ModoJugador modoJugador, int rivalesEnPie)
        {
            bool enMecha = modoJugador == ModoJugador.Mecha;
            switch (Estado)
            {
                case EstadoEmboscada.Esperando:
                    if (jugadorCerca)
                    {
                        Empezar();
                    }
                    break;
                case EstadoEmboscada.Aparicion:
                    tiempoAparicion += Math.Max(dt, 0f);
                    if (tiempoAparicion >= segundosAparicion)
                    {
                        Cambiar(enMecha ? EstadoEmboscada.Peleando : EstadoEmboscada.EsperandoMecha);
                    }
                    break;
                case EstadoEmboscada.EsperandoMecha:
                    if (rivalesEnPie <= 0)
                    {
                        Superar();
                    }
                    else if (enMecha)
                    {
                        Cambiar(EstadoEmboscada.Peleando);
                    }
                    break;
                case EstadoEmboscada.Peleando:
                    if (rivalesEnPie <= 0)
                    {
                        Superar();
                    }
                    else if (!enMecha)
                    {
                        Cambiar(EstadoEmboscada.Pausada);
                    }
                    break;
                case EstadoEmboscada.Pausada:
                    if (rivalesEnPie <= 0)
                    {
                        Superar();
                    }
                    else if (enMecha)
                    {
                        Cambiar(EstadoEmboscada.Peleando);
                    }
                    break;
            }
        }

        private void Superar()
        {
            Cambiar(EstadoEmboscada.Superada);
            Terminada?.Invoke();
        }

        private void Cambiar(EstadoEmboscada nuevo)
        {
            Estado = nuevo;
            EstadoCambio?.Invoke(nuevo);
        }
    }
}

using System;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// Decisiones del pandillero a pie (T-195): esperar, perseguir, atacar y volver. Lógica pura: recibe el tiempo y lo que ve y
    /// devuelve a dónde ir y si su golpe conecta en este paso. No sabe de Unity.
    /// </summary>
    public class ReglaPandilleroAPie
    {
        /// <summary>A dónde se mueve en este paso.</summary>
        public enum Orden
        {
            Quieto,
            AlJugador,
            AlPunto
        }

        /// <summary>Lo que ve el pandillero en este paso.</summary>
        public struct Entrada
        {
            /// <summary>Distancia al jugador en el suelo (m).</summary>
            public float DistanciaJugador;
            /// <summary>Distancia del jugador a su punto (m).</summary>
            public float DistanciaJugadorAlPunto;
            /// <summary>Distancia propia a su punto (m).</summary>
            public float DistanciaAlPunto;
            /// <summary>False si el jugador está en KO, en carro, en mecha o no existe.</summary>
            public bool JugadorAtacable;
            /// <summary>True si la pandilla le dejó atacar (o ya atacaba).</summary>
            public bool TieneTurno;
        }

        /// <summary>Lo que decide en este paso.</summary>
        public struct Salida
        {
            public Orden Orden;
            /// <summary>True en el paso en que su golpe conecta (si el jugador sigue a su alcance).</summary>
            public bool Impacto;
        }

        private readonly float radioDeteccion;
        private readonly float correa;
        private readonly float alcance;
        private readonly float preparacion;
        private readonly float recarga;
        private readonly float margenLlegada;
        private float temporizador;

        public ReglaPandilleroAPie(float radioDeteccion, float correa, float alcance, float preparacion, float recarga, float margenLlegada)
        {
            this.radioDeteccion = radioDeteccion;
            this.correa = correa;
            this.alcance = alcance;
            this.preparacion = preparacion;
            this.recarga = recarga;
            this.margenLlegada = margenLlegada;
        }

        public EstadoPandillero Estado { get; private set; } = EstadoPandillero.Esperando;

        /// <summary>Avance del aviso del golpe, de 0 a 1 (0 fuera del aviso).</summary>
        public float ProgresoAviso => Estado == EstadoPandillero.Preparando ? Math.Min(1f, temporizador / preparacion) : 0f;

        /// <summary>Lo que falta de la recarga (s); 0 si no está recargando.</summary>
        public float RecargaRestante => Estado == EstadoPandillero.Recargando ? Math.Max(0f, recarga - temporizador) : 0f;

        /// <summary>True mientras avisa o recarga: ocupa un turno de la pandilla.</summary>
        public bool Atacando => Estado == EstadoPandillero.Preparando || Estado == EstadoPandillero.Recargando;

        /// <summary>True si está en alguna fase de pelea (persigue, avisa o recarga).</summary>
        public bool EnPelea => Estado == EstadoPandillero.Persiguiendo || Atacando;

        public void Reiniciar()
        {
            Estado = EstadoPandillero.Esperando;
            temporizador = 0f;
        }

        public Salida Avanzar(float dt, Entrada e)
        {
            Salida salida = new Salida { Orden = Orden.Quieto, Impacto = false };
            bool enZona = e.JugadorAtacable && e.DistanciaJugadorAlPunto <= correa && e.DistanciaAlPunto <= correa;
            bool alAlcance = e.DistanciaJugador <= alcance;

            if (Estado == EstadoPandillero.Esperando && enZona && e.DistanciaJugador <= radioDeteccion)
            {
                Estado = EstadoPandillero.Persiguiendo;
            }

            switch (Estado)
            {
                case EstadoPandillero.Persiguiendo:
                    if (!enZona)
                    {
                        Estado = EstadoPandillero.Volviendo;
                        salida.Orden = Orden.AlPunto;
                    }
                    else if (alAlcance && e.TieneTurno)
                    {
                        Estado = EstadoPandillero.Preparando;
                        temporizador = 0f;
                    }
                    else
                    {
                        salida.Orden = alAlcance ? Orden.Quieto : Orden.AlJugador;
                    }
                    break;
                case EstadoPandillero.Preparando:
                    if (!e.JugadorAtacable)
                    {
                        Estado = EstadoPandillero.Volviendo;
                        salida.Orden = Orden.AlPunto;
                        break;
                    }
                    temporizador += dt;
                    if (temporizador >= preparacion)
                    {
                        salida.Impacto = true;
                        Estado = EstadoPandillero.Recargando;
                        temporizador = 0f;
                    }
                    break;
                case EstadoPandillero.Recargando:
                    if (!enZona)
                    {
                        Estado = EstadoPandillero.Volviendo;
                        salida.Orden = Orden.AlPunto;
                        break;
                    }
                    temporizador += dt;
                    if (temporizador >= recarga)
                    {
                        Estado = EstadoPandillero.Persiguiendo;
                        temporizador = 0f;
                    }
                    salida.Orden = alAlcance ? Orden.Quieto : Orden.AlJugador;
                    break;
                case EstadoPandillero.Volviendo:
                    if (e.DistanciaAlPunto <= margenLlegada)
                    {
                        Estado = EstadoPandillero.Esperando;
                    }
                    else
                    {
                        salida.Orden = Orden.AlPunto;
                    }
                    break;
            }
            return salida;
        }
    }
}

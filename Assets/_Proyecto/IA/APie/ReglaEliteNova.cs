using System;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1553: el ciclo del elite de Nova (E18), sin arma (decisión de Tony 20-nov): vuela con el jetpack hacia el jugador (E18_Jetpack_Volar), aterriza cerca
    /// (E18_Caida_Aterrizaje) y pelea a golpes con los puños cerrados (E18_Golpe_Der). Se detiene para golpear. Lógica pura, sin escena.
    /// Patrulla -> (ve al jugador) Vuela si está lejos o Persigue si está cerca; Vuela -> Aterriza (baja) al llegar a <see cref="Parametros.DistanciaAterrizaje"/> ->
    /// Aterrizaje (clip, quieto) -> Persigue -> AvisoGolpe (quieto) -> Recupera (quieto) -> Persigue. Si el jugador se aleja más de <see cref="Parametros.DistanciaRelanza"/>
    /// vuelve a despegar. La conducta traduce las fases a movimiento, clips y daño.
    /// </summary>
    public sealed class ReglaEliteNova
    {
        public enum Fase { Patrulla, Vuela, Aterriza, Aterrizaje, Persigue, AvisoGolpe, Recupera }

        public readonly struct Parametros
        {
            public readonly float DistanciaVuelo;
            public readonly float DistanciaAterrizaje;
            public readonly float DistanciaRelanza;
            public readonly float AlturaVuelo;
            public readonly float VelocidadVuelo;
            public readonly float VelocidadVertical;
            public readonly float SegundosAterrizaje;
            public readonly float SegundosAlImpacto;
            public readonly float SegundosGolpe;
            public readonly float SegundosMinimosAire;

            public Parametros(float distanciaVuelo, float distanciaAterrizaje, float distanciaRelanza, float alturaVuelo, float velocidadVuelo, float velocidadVertical,
                float segundosAterrizaje, float segundosAlImpacto, float segundosGolpe, float segundosMinimosAire)
            {
                DistanciaVuelo = distanciaVuelo;
                DistanciaAterrizaje = distanciaAterrizaje;
                DistanciaRelanza = distanciaRelanza;
                AlturaVuelo = alturaVuelo;
                VelocidadVuelo = velocidadVuelo;
                VelocidadVertical = velocidadVertical;
                SegundosAterrizaje = segundosAterrizaje;
                SegundosAlImpacto = segundosAlImpacto;
                SegundosGolpe = segundosGolpe;
                SegundosMinimosAire = segundosMinimosAire;
            }

            /// <summary>Cuadros por segundo de los clips del E18.</summary>
            public const float CuadrosPorSegundo = 30f;
            /// <summary>E18_Caida_Aterrizaje dura 1.03 s (ConfigClipsNovaNpc).</summary>
            public const float SegundosClipAterrizaje = 1.03f;
            /// <summary>E18_Golpe_Der: 49 cuadros; el puño conecta hacia el cuadro 16 (estimado de la pose; se afina viendo el clip en Play).</summary>
            public const int CuadrosGolpe = 49;
            public const int CuadroImpactoGolpe = 16;

            /// <summary>
            /// Despega si el jugador está a más de 10 m, empieza a bajar a 5 m, relanza a más de 18 m; vuela a 3 m de altura a 9 m/s y sube o baja a 6 m/s; el aterrizaje dura
            /// el clip; el golpe conecta en su cuadro 16 y el clip dura 49 cuadros; al menos 0.4 s en el aire antes de aceptar que tocó el piso.
            /// </summary>
            public static Parametros Diseno => new Parametros(10f, 5f, 18f, 3f, 9f, 6f, SegundosClipAterrizaje, CuadroImpactoGolpe / CuadrosPorSegundo, CuadrosGolpe / CuadrosPorSegundo, 0.4f);
        }

        public struct Entrada
        {
            /// <summary>Distancia en el plano al jugador (m).</summary>
            public float Distancia;
            /// <summary>True si ve o sabe del jugador y se le puede pegar (atacable y detectado o en alerta).</summary>
            public bool Ve;
            /// <summary>True si el CharacterController toca el piso.</summary>
            public bool EnPiso;
            /// <summary>Alcance del puñetazo (m), de los datos del E18.</summary>
            public float AlcanceGolpe;
        }

        public struct Salida
        {
            /// <summary>Despega: empieza E18_Jetpack_Volar.</summary>
            public bool EmpezoVuelo;
            /// <summary>Toca el piso: empieza E18_Caida_Aterrizaje.</summary>
            public bool EmpezoAterrizaje;
            /// <summary>Empieza el golpe (E18_Golpe_Der): se detiene.</summary>
            public bool EmpezoGolpe;
            /// <summary>El puño conecta en este cuadro (la conducta comprueba el alcance).</summary>
            public bool Impacto;
        }

        private readonly Parametros p;
        private float tFase;

        public ReglaEliteNova(Parametros parametros)
        {
            p = parametros;
        }

        public Parametros Config => p;
        public Fase Actual { get; private set; }
        public int Golpes { get; private set; }
        public float SegundosEnFase => tFase;

        /// <summary>True si en esta fase el elite tiene que estar quieto (aterrizando, golpeando o recuperándose).</summary>
        public bool Quieto => Actual == Fase.Aterrizaje || Actual == Fase.AvisoGolpe || Actual == Fase.Recupera;

        /// <summary>True si está en el aire (vuela o baja).</summary>
        public bool EnElAire => Actual == Fase.Vuela || Actual == Fase.Aterriza;

        public void Reiniciar()
        {
            Actual = Fase.Patrulla;
            tFase = 0f;
            Golpes = 0;
        }

        /// <summary>Le pegaron o algo lo interrumpió a la mitad: corta un golpe o un aterrizaje y sigue persiguiendo.</summary>
        public void Interrumpir()
        {
            if (Actual == Fase.AvisoGolpe || Actual == Fase.Recupera || Actual == Fase.Aterrizaje)
            {
                Cambiar(Fase.Persigue);
            }
        }

        /// <summary>
        /// Velocidad vertical (m/s, positiva sube) para llegar a <paramref name="objetivo"/> m de altura desde <paramref name="altura"/>: proporcional al error y recortada a
        /// <paramref name="maxima"/>, así se asienta sin vibrar.
        /// </summary>
        public static float VelocidadVerticalHacia(float altura, float objetivo, float maxima)
        {
            float v = (objetivo - altura) * 2f;
            return Math.Max(-maxima, Math.Min(maxima, v));
        }

        private void Cambiar(Fase nueva)
        {
            Actual = nueva;
            tFase = 0f;
        }

        public Salida Avanzar(float dt, Entrada e)
        {
            Salida s = default;
            tFase += dt;
            switch (Actual)
            {
                case Fase.Patrulla:
                    if (e.Ve)
                    {
                        if (e.Distancia > p.DistanciaVuelo)
                        {
                            Cambiar(Fase.Vuela);
                            s.EmpezoVuelo = true;
                        }
                        else
                        {
                            Cambiar(Fase.Persigue);
                        }
                    }
                    break;
                case Fase.Vuela:
                    if (!e.Ve || e.Distancia <= p.DistanciaAterrizaje)
                    {
                        Cambiar(Fase.Aterriza);
                    }
                    break;
                case Fase.Aterriza:
                    if (e.EnPiso && tFase >= p.SegundosMinimosAire * 0.5f)
                    {
                        Cambiar(Fase.Aterrizaje);
                        s.EmpezoAterrizaje = true;
                    }
                    break;
                case Fase.Aterrizaje:
                    if (tFase >= p.SegundosAterrizaje)
                    {
                        Cambiar(e.Ve ? Fase.Persigue : Fase.Patrulla);
                    }
                    break;
                case Fase.Persigue:
                    if (!e.Ve)
                    {
                        Cambiar(Fase.Patrulla);
                    }
                    else if (e.Distancia > p.DistanciaRelanza)
                    {
                        Cambiar(Fase.Vuela);
                        s.EmpezoVuelo = true;
                    }
                    else if (e.Distancia <= e.AlcanceGolpe)
                    {
                        Cambiar(Fase.AvisoGolpe);
                        s.EmpezoGolpe = true;
                    }
                    break;
                case Fase.AvisoGolpe:
                    if (tFase >= p.SegundosAlImpacto)
                    {
                        Golpes++;
                        s.Impacto = true;
                        Actual = Fase.Recupera; // el tiempo de fase sigue corriendo: Recupera dura lo que falta del clip
                    }
                    break;
                case Fase.Recupera:
                    if (tFase >= p.SegundosGolpe)
                    {
                        Cambiar(e.Ve ? Fase.Persigue : Fase.Patrulla);
                    }
                    break;
            }
            return s;
        }
    }
}

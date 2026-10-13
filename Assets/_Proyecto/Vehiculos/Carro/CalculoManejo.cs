using System;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>Reglas de manejo del carro (motor, freno, dirección, agarre). Lógica pura: el tiempo entra como parámetro.</summary>
    public static class CalculoManejo
    {
        private const float MargenEstabilidad = 0.9f;
        private const float GradosARadianes = (float)(Math.PI / 180.0);
        private const float VelocidadCasiCero = 1e-4f;

        /// <summary>
        /// Fuerza (N) a lo largo del carro; positiva es hacia adelante.
        /// Acelerar empuja (se corta al llegar a la velocidad máxima); frenar frena si el carro va hacia adelante
        /// y da reversa solo con el carro casi detenido.
        /// </summary>
        public static float FuerzaLongitudinal(in ParametrosManejo p, float acelerar, float frenar, float frenoMano,
            float velocidadAdelante, float dt)
        {
            acelerar = Math.Clamp(acelerar, 0f, 1f);
            frenar = Math.Clamp(frenar, 0f, 1f);
            frenoMano = Math.Clamp(frenoMano, 0f, 1f);
            float fuerza;

            if (acelerar > 0f)
            {
                fuerza = velocidadAdelante < -p.UmbralReversa
                    ? FuerzaOpuesta(p.FuerzaFreno * acelerar, velocidadAdelante, p.Masa, dt)
                    : p.FuerzaMotor * acelerar * Math.Max(0f, 1f - Math.Max(0f, velocidadAdelante) / p.VelocidadMaxima);
            }
            else if (frenar > 0f)
            {
                if (velocidadAdelante > p.UmbralReversa)
                {
                    fuerza = FuerzaOpuesta(p.FuerzaFreno * frenar, velocidadAdelante, p.Masa, dt);
                }
                else
                {
                    float fraccion = Math.Max(0f, 1f - Math.Max(0f, -velocidadAdelante) / p.VelocidadReversa);
                    fuerza = -p.FuerzaReversa * frenar * fraccion;
                }
            }
            else
            {
                fuerza = FuerzaOpuesta(p.ResistenciaRodadura, velocidadAdelante, p.Masa, dt);
            }

            if (frenoMano > 0f)
            {
                fuerza += FuerzaOpuesta(p.FuerzaFrenoMano * frenoMano, velocidadAdelante, p.Masa, dt);
            }
            return fuerza;
        }

        /// <summary>Ángulo de las ruedas delanteras (grados, siempre positivo): máximo parado, mínimo a la velocidad máxima.</summary>
        public static float AnguloDireccion(in ParametrosManejo p, float rapidez)
        {
            float t = Math.Clamp(rapidez / p.VelocidadMaxima, 0f, 1f);
            float curva = (float)Math.Pow(t, p.ExponenteDireccion);
            return p.AnguloDireccionParado + (p.AnguloDireccionMaximo - p.AnguloDireccionParado) * curva;
        }

        /// <summary>
        /// Velocidad de giro (rad/s, positiva hacia la derecha) con el modelo de bicicleta. Es cero con el carro parado
        /// y no pide más aceleración lateral de la que el agarre aguanta.
        /// </summary>
        public static float VelocidadGiro(in ParametrosManejo p, float velocidadAdelante, float direccion)
        {
            direccion = Math.Clamp(direccion, -1f, 1f);
            float rapidez = Math.Abs(velocidadAdelante);
            if (rapidez < VelocidadCasiCero)
            {
                return 0f;
            }
            float anguloRad = AnguloDireccion(p, rapidez) * GradosARadianes * direccion;
            float giro = velocidadAdelante * (float)Math.Tan(anguloRad) / p.DistanciaEntreEjes;
            float tope = p.AceleracionLateralMaxima / rapidez;
            return Math.Clamp(giro, -tope, tope);
        }

        /// <summary>Fuerza lateral (N) que cancela el derrape. El freno de mano baja el agarre. Nunca pasa del agarre máximo.</summary>
        public static float FuerzaLateral(in ParametrosManejo p, float velocidadLateral, float frenoMano, float dt)
        {
            float factor = 1f - Math.Clamp(frenoMano, 0f, 1f) * (1f - p.FactorAgarreFrenoMano);
            float tasa = Math.Min(p.AgarreLateral * factor, MargenEstabilidad / dt);
            float fuerza = -p.Masa * velocidadLateral * tasa;
            float tope = p.Masa * p.AceleracionLateralMaxima * factor;
            return Math.Clamp(fuerza, -tope, tope);
        }

        /// <summary>Freno de estacionamiento (N): se opone a la velocidad (un eje) sin pasarla de largo.</summary>
        public static float FuerzaEstacionamiento(in ParametrosManejo p, float velocidad, float dt)
        {
            float tasa = Math.Min(p.GananciaEstacionamiento, MargenEstabilidad / dt);
            return -p.Masa * velocidad * tasa;
        }

        private static float FuerzaOpuesta(float magnitud, float velocidad, float masa, float dt)
        {
            if (Math.Abs(velocidad) < VelocidadCasiCero)
            {
                return 0f;
            }
            float maximo = masa * Math.Abs(velocidad) * MargenEstabilidad / dt;
            float fuerza = Math.Min(magnitud, maximo);
            return velocidad > 0f ? -fuerza : fuerza;
        }
    }
}

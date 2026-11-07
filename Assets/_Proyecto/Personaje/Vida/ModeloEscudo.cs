using System;

namespace MechaCyberpunk.Personaje.Vida
{
    /// <summary>
    /// Escudo en C# puro (T-684): absorbe daño antes que la vida y se recarga solo cuando pasan <c>espera</c> segundos
    /// sin recibir daño. Lógica pura: recibe el tiempo.
    /// </summary>
    public class ModeloEscudo
    {
        private readonly float maximoBase;
        private readonly float recargaBase;
        private readonly float espera;
        private float sinDanio;

        public float Actual { get; private set; }
        public float BonoMaximo { get; private set; }
        public float BonoRecarga { get; private set; }
        public float Maximo => maximoBase + BonoMaximo;
        public float RecargaPorSegundo => recargaBase + BonoRecarga;

        public event Action Cambio;
        public event Action<float> Absorbio;

        public ModeloEscudo(float maximo, float recargaPorSegundo, float segundosSinDanio)
        {
            maximoBase = Math.Max(0f, maximo);
            recargaBase = Math.Max(0f, recargaPorSegundo);
            espera = Math.Max(0f, segundosSinDanio);
            Actual = Maximo;
        }

        /// <summary>Absorbe lo que puede de <paramref name="cantidad"/> y devuelve el resto, que sigue a la vida.</summary>
        public float Absorber(float cantidad)
        {
            if (cantidad <= 0f)
            {
                return 0f;
            }
            sinDanio = 0f;
            if (Actual <= 0f)
            {
                return cantidad;
            }
            float absorbido = Math.Min(cantidad, Actual);
            Actual -= absorbido;
            Cambio?.Invoke();
            Absorbio?.Invoke(absorbido);
            return cantidad - absorbido;
        }

        /// <summary>Pasa el tiempo: tras la espera sin daño, el escudo sube hasta el máximo.</summary>
        public void Avanzar(float dt)
        {
            if (Actual >= Maximo)
            {
                return;
            }
            sinDanio += dt;
            if (sinDanio < espera || RecargaPorSegundo <= 0f)
            {
                return;
            }
            // Solo recarga el tiempo que sobra de la espera (un dt grande no regala recarga).
            float efectivo = Math.Min(dt, sinDanio - espera);
            Actual = Math.Min(Maximo, Actual + RecargaPorSegundo * efectivo);
            Cambio?.Invoke();
        }

        public void Recuperar(float puntos)
        {
            if (puntos <= 0f || Actual >= Maximo)
            {
                return;
            }
            Actual = Math.Min(Maximo, Actual + puntos);
            Cambio?.Invoke();
        }

        public void MejorarMaximo(float puntos)
        {
            if (puntos <= 0f)
            {
                return;
            }
            BonoMaximo += puntos;
            Actual = Math.Min(Maximo, Actual + puntos);
            Cambio?.Invoke();
        }

        public void MejorarRecarga(float puntosPorSegundo)
        {
            if (puntosPorSegundo <= 0f)
            {
                return;
            }
            BonoRecarga += puntosPorSegundo;
            Cambio?.Invoke();
        }

        /// <summary>Fija las mejoras guardadas (carga de partida) sin llenar el escudo.</summary>
        public void FijarBonos(float bonoMaximo, float bonoRecarga)
        {
            BonoMaximo = Math.Max(0f, bonoMaximo);
            BonoRecarga = Math.Max(0f, bonoRecarga);
            Actual = Math.Min(Actual, Maximo);
            Cambio?.Invoke();
        }

        public void Restaurar()
        {
            Actual = Maximo;
            sinDanio = 0f;
            Cambio?.Invoke();
        }
    }
}

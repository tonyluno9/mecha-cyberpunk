using System;

namespace MechaCyberpunk.Core
{
    /// <summary>Vida en C# puro: resta daño, avisa cambios y KO.</summary>
    public class ModeloVida : IVida
    {
        public float Actual { get; private set; }
        public float Maxima { get; }
        public bool EstaKO { get; private set; }

        public event Action Cambio;
        public event Action KO;

        public ModeloVida(float maxima)
        {
            if (maxima <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maxima), "La vida máxima debe ser mayor que 0.");

            Maxima = maxima;
            Actual = maxima;
        }

        /// <summary>Resta vida sin bajar de 0 y devuelve lo que de verdad restó.</summary>
        public float Aplicar(float cantidad)
        {
            if (EstaKO || cantidad <= 0f)
                return 0f;

            float restado = Math.Min(cantidad, Actual);
            Actual -= restado;
            Cambio?.Invoke();

            if (Actual <= 0f)
            {
                Actual = 0f;
                EstaKO = true;
                KO?.Invoke();
            }

            return restado;
        }

        /// <summary>Vuelve a la vida máxima y quita el KO.</summary>
        public void Restaurar()
        {
            Actual = Maxima;
            EstaKO = false;
            Cambio?.Invoke();
        }
    }
}

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// T-1533: la ráfaga de la ametralladora del dron. El archivo DRN_PES_AMETRALLADORA es un solo tiro (0.45 s): cada disparo del dron suena como
    /// <see cref="TirosPorRafaga"/> tiros seguidos, uno cada <see cref="SegundosEntreTiros"/> s. Lógica pura: el dron pide el tiro que toca con el tiempo.
    /// </summary>
    public sealed class ReglaRafagaDron
    {
        /// <summary>Tiros que suenan por cada disparo del dron.</summary>
        public const int TirosPorRafaga = 3;

        /// <summary>Segundos entre un tiro de la ráfaga y el siguiente (más que el mínimo de 0.05 s de la mezcla).</summary>
        public const float SegundosEntreTiros = 0.12f;

        private int restantes;
        private float proximo;

        /// <summary>Tiros que faltan por sonar de la ráfaga en curso.</summary>
        public int Restantes => restantes;

        /// <summary>Arranca una ráfaga: el primer tiro suena ya (lo pide quien llama) y los demás siguen con <see cref="Avanzar"/>.</summary>
        public void Iniciar(float ahora)
        {
            restantes = TirosPorRafaga - 1;
            proximo = ahora + SegundosEntreTiros;
        }

        /// <summary>True si ahora suena el siguiente tiro de la ráfaga (llámalo cada cuadro).</summary>
        public bool Avanzar(float ahora)
        {
            if (restantes <= 0 || ahora < proximo)
            {
                return false;
            }
            restantes--;
            proximo = ahora + SegundosEntreTiros;
            return true;
        }

        /// <summary>Corta lo que falte (el dron cayó o se fue).</summary>
        public void Cancelar() => restantes = 0;
    }
}

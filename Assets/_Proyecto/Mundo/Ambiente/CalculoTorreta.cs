using System;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// Lógica pura de una torreta de patrulla (sin Unity): alterna rojo y azul cada <c>periodo</c> segundos.
    /// La fase inicial desplaza el reloj propio de cada patrulla para que no parpadeen todas al mismo tiempo.
    /// </summary>
    public class CalculoTorreta
    {
        private readonly double periodo;
        private double tiempo;

        /// <param name="periodo">Segundos que dura cada color.</param>
        /// <param name="faseInicial">Segundos que la torreta ya lleva corriendo al empezar (puede ser negativa).</param>
        public CalculoTorreta(float periodo, float faseInicial)
        {
            if (periodo <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(periodo), "El periodo debe ser mayor que 0");
            }
            this.periodo = periodo;
            tiempo = faseInicial;
        }

        /// <summary>Color prendido ahora.</summary>
        public ColorTorreta ColorActual
        {
            get
            {
                long tramo = (long)Math.Floor(tiempo / periodo);
                return (tramo & 1L) == 0L ? ColorTorreta.Rojo : ColorTorreta.Azul;
            }
        }

        /// <summary>Avanza el reloj de la torreta <paramref name="dt"/> segundos.</summary>
        public void Avanzar(float dt)
        {
            if (dt > 0f)
            {
                tiempo += dt;
            }
        }
    }
}

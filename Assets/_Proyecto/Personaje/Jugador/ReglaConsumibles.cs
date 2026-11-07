using System;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>Curación repartida en el tiempo (la venda): entrega la vida de a poco, sin pasarse del total (C# puro, T-685).</summary>
    public class CuracionGradual
    {
        private float restante;
        private float porSegundo;

        public bool Activa => restante > 0f;
        public float Restante => restante;

        /// <summary>Empieza (o suma) una curación de <paramref name="total"/> puntos repartida en <paramref name="segundos"/>.</summary>
        public void Iniciar(float total, float segundos)
        {
            if (total <= 0f)
            {
                return;
            }
            restante += total;
            float nueva = segundos > 0f ? total / segundos : float.MaxValue;
            porSegundo = Math.Max(porSegundo, nueva);
        }

        /// <summary>Pasa el tiempo y devuelve cuánta vida toca dar en este paso.</summary>
        public float Avanzar(float dt)
        {
            if (restante <= 0f)
            {
                porSegundo = 0f;
                return 0f;
            }
            float cuanto = Math.Min(restante, porSegundo * dt);
            restante -= cuanto;
            if (restante <= 0f)
            {
                restante = 0f;
                porSegundo = 0f;
            }
            return cuanto;
        }
    }

    /// <summary>Reglas puras de la selección de consumibles (T-685).</summary>
    public static class ReglaConsumibles
    {
        /// <summary>Índice siguiente con vuelta al inicio. Con lista vacía devuelve -1.</summary>
        public static int Siguiente(int actual, int cantidad)
        {
            if (cantidad <= 0)
            {
                return -1;
            }
            return (actual + 1 + cantidad) % cantidad;
        }

        /// <summary>Deja el índice dentro de la lista (o -1 si no hay nada).</summary>
        public static int Acomodar(int actual, int cantidad)
        {
            if (cantidad <= 0)
            {
                return -1;
            }
            return Math.Max(0, Math.Min(actual, cantidad - 1));
        }
    }
}

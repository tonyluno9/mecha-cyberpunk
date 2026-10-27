using System;

namespace MechaCyberpunk.UI.Peleas
{
    /// <summary>
    /// La fila de montos de la pantalla de apuesta (T-145, C# puro): "SIN APUESTA" (0) y luego los montos de la pelea. Los montos que
    /// no se pueden pagar quedan apagados y <see cref="Mover"/> los salta. Empieza en "SIN APUESTA".
    /// </summary>
    public class SeleccionApuesta
    {
        private readonly int[] opciones;
        private readonly bool[] disponibles;

        public SeleccionApuesta(int[] montos, Func<int, bool> puedePagar)
        {
            int cantidad = montos != null ? montos.Length : 0;
            opciones = new int[cantidad + 1];
            disponibles = new bool[cantidad + 1];
            disponibles[0] = true;
            for (int i = 0; i < cantidad; i++)
            {
                opciones[i + 1] = montos[i];
                disponibles[i + 1] = montos[i] > 0 && puedePagar != null && puedePagar(montos[i]);
            }
            Indice = 0;
        }

        /// <summary>Opción elegida: 0 es "SIN APUESTA".</summary>
        public int Indice { get; private set; }

        public int Cantidad => opciones.Length;

        /// <summary>Monto elegido ($). 0 es sin apuesta.</summary>
        public int Monto => opciones[Indice];

        public int MontoDe(int indice) => opciones[indice];

        public bool Disponible(int indice) => disponibles[indice];

        /// <summary>Mueve la elección hacia el lado de <paramref name="paso"/> (-1 o 1) saltando los apagados, sin dar la vuelta. Devuelve si se movió.</summary>
        public bool Mover(int paso)
        {
            if (paso == 0)
            {
                return false;
            }
            int direccion = paso > 0 ? 1 : -1;
            for (int i = Indice + direccion; i >= 0 && i < opciones.Length; i += direccion)
            {
                if (disponibles[i])
                {
                    Indice = i;
                    return true;
                }
            }
            return false;
        }
    }
}

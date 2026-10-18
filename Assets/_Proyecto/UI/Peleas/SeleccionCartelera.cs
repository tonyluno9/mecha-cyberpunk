using System.Collections.Generic;

namespace MechaCyberpunk.UI.Peleas
{
    /// <summary>
    /// La elección de la pantalla de la cartelera (T-160, C# puro): ↑/↓ mueven sin dar la vuelta y saltan las filas que no se pueden
    /// elegir. Empieza en la primera fila elegible; si no hay ninguna, <see cref="Indice"/> es -1.
    /// </summary>
    public class SeleccionCartelera
    {
        private readonly bool[] elegibles;

        public SeleccionCartelera(IReadOnlyList<FilaCartelera> filas)
        {
            int cantidad = filas != null ? filas.Count : 0;
            elegibles = new bool[cantidad];
            Indice = -1;
            for (int i = 0; i < cantidad; i++)
            {
                elegibles[i] = filas[i].Elegible;
                if (Indice < 0 && elegibles[i])
                {
                    Indice = i;
                }
            }
        }

        /// <summary>Fila elegida; -1 si ninguna se puede elegir.</summary>
        public int Indice { get; private set; }

        public int Cantidad => elegibles.Length;

        public bool Elegible(int indice) => indice >= 0 && indice < elegibles.Length && elegibles[indice];

        /// <summary>Mueve la elección hacia el lado de <paramref name="paso"/> (-1 sube, 1 baja) saltando las bloqueadas, sin dar la vuelta. Devuelve si se movió.</summary>
        public bool Mover(int paso)
        {
            if (paso == 0 || Indice < 0)
            {
                return false;
            }
            int direccion = paso > 0 ? 1 : -1;
            for (int i = Indice + direccion; i >= 0 && i < elegibles.Length; i += direccion)
            {
                if (elegibles[i])
                {
                    Indice = i;
                    return true;
                }
            }
            return false;
        }
    }
}

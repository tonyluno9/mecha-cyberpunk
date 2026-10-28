using System.Collections.Generic;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// Los turnos de ataque de una pandilla (T-195): como máximo N pandilleros atacan a la vez. Quien ya tiene turno lo conserva hasta
    /// que lo suelta (al terminar su recarga o al rendirse).
    /// </summary>
    public class TurnosPandillaAPie
    {
        private readonly int maximo;
        private readonly HashSet<object> quienes = new HashSet<object>();

        public TurnosPandillaAPie(int maximo)
        {
            this.maximo = maximo;
        }

        public int Cantidad => quienes.Count;

        /// <summary>True si <paramref name="quien"/> tiene turno (lo toma si queda alguno libre).</summary>
        public bool Pedir(object quien)
        {
            if (quienes.Contains(quien))
            {
                return true;
            }
            if (quienes.Count >= maximo)
            {
                return false;
            }
            quienes.Add(quien);
            return true;
        }

        public void Soltar(object quien) => quienes.Remove(quien);

        public bool Tiene(object quien) => quienes.Contains(quien);
    }
}

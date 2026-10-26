using System;
using System.Collections.Generic;

namespace MechaCyberpunk.Progresion
{
    /// <summary>
    /// Qué misiones se cumplieron, cuántas veces y el último día (T-184). Lógica pura; la misión en curso no se guarda (D-tony-147).
    /// El guardado en texto lo hace <see cref="ServicioMisiones"/>.
    /// </summary>
    public class ModeloHistorialMisiones
    {
        private readonly Dictionary<string, int> veces = new Dictionary<string, int>();
        private readonly Dictionary<string, int> ultimoDia = new Dictionary<string, int>();

        /// <summary>Cuántas veces se cumplió la misión.</summary>
        public int Veces(string id) => id != null && veces.TryGetValue(id, out int n) ? n : 0;

        /// <summary>Último día en que se cumplió, o <see cref="ContextoMision.Nunca"/>.</summary>
        public int UltimoDia(string id) => id != null && ultimoDia.TryGetValue(id, out int d) ? d : ContextoMision.Nunca;

        /// <summary>Anota una misión cumplida en ese día.</summary>
        public void Anotar(string id, int dia)
        {
            if (string.IsNullOrEmpty(id)) return;
            veces[id] = Veces(id) + 1;
            ultimoDia[id] = dia;
        }

        /// <summary>Deja una misión con esos valores (al cargar). Veces menores o iguales a 0 la borran.</summary>
        public void Poner(string id, int cantidad, int dia)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (cantidad <= 0)
            {
                veces.Remove(id);
                ultimoDia.Remove(id);
                return;
            }
            veces[id] = cantidad;
            ultimoDia[id] = dia;
        }

        /// <summary>Olvida todo.</summary>
        public void Limpiar()
        {
            veces.Clear();
            ultimoDia.Clear();
        }

        /// <summary>Ids con al menos una vez cumplida.</summary>
        public IEnumerable<string> Ids => new List<string>(veces.Keys);
    }
}

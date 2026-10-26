using System;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Lógica pura del bolsillo de favores: cuántos hay de cada tipo, con un máximo.</summary>
    public class ModeloFavores
    {
        private readonly int[] cantidades = new int[Enum.GetValues(typeof(TipoFavor)).Length];
        private readonly int maximo;

        public ModeloFavores(int maximo)
        {
            if (maximo < 1) throw new ArgumentOutOfRangeException(nameof(maximo));
            this.maximo = maximo;
        }

        public event Action Cambio;

        public int Maximo => maximo;
        public int Cantidad(TipoFavor tipo) => cantidades[(int)tipo];

        /// <summary>Agrega un favor. False (sin cambios) si ya hay el máximo.</summary>
        public bool Dar(TipoFavor tipo)
        {
            if (cantidades[(int)tipo] >= maximo) return false;
            cantidades[(int)tipo]++;
            Cambio?.Invoke();
            return true;
        }

        /// <summary>Gasta un favor. False (sin cambios) si no hay.</summary>
        public bool Gastar(TipoFavor tipo)
        {
            if (cantidades[(int)tipo] <= 0) return false;
            cantidades[(int)tipo]--;
            Cambio?.Invoke();
            return true;
        }

        /// <summary>Fija la cantidad de un tipo (cargar), recortada a 0..máximo.</summary>
        public void Poner(TipoFavor tipo, int cantidad)
        {
            int nueva = Math.Max(0, Math.Min(maximo, cantidad));
            if (cantidades[(int)tipo] == nueva) return;
            cantidades[(int)tipo] = nueva;
            Cambio?.Invoke();
        }
    }
}

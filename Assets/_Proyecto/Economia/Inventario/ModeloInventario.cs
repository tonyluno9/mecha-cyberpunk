using System;
using System.Collections.Generic;

namespace MechaCyberpunk.Economia
{
    /// <summary>Cuántos objetos lleva el jugador de cada tipo, con tope por objeto (C# puro, T-142).</summary>
    public class ModeloInventario
    {
        private readonly Dictionary<DatosObjeto, int> cantidades = new Dictionary<DatosObjeto, int>();
        private readonly List<DatosObjeto> orden = new List<DatosObjeto>();

        /// <summary>Avisa una vez por cada cambio real de cantidades.</summary>
        public event Action Cambio;

        /// <summary>Objetos con cantidad mayor a 0, en orden de llegada.</summary>
        public IReadOnlyList<DatosObjeto> Todos => orden;

        public int Cantidad(DatosObjeto objeto) => objeto != null && cantidades.TryGetValue(objeto, out int n) ? n : 0;

        public bool PuedeAgregar(DatosObjeto objeto, int n) => objeto != null && n > 0 && Cantidad(objeto) + n <= objeto.Maximo;

        /// <summary>Agrega hasta el tope y devuelve cuántos entraron.</summary>
        public int Agregar(DatosObjeto objeto, int n)
        {
            if (objeto == null || n <= 0) return 0;
            int actual = Cantidad(objeto);
            int entran = Math.Min(n, Math.Max(0, objeto.Maximo - actual));
            if (entran == 0) return 0;
            if (actual == 0) orden.Add(objeto);
            cantidades[objeto] = actual + entran;
            Cambio?.Invoke();
            return entran;
        }

        /// <summary>Quita n; false (sin cambiar nada) si no hay suficientes o n no es positivo.</summary>
        public bool Quitar(DatosObjeto objeto, int n)
        {
            if (objeto == null || n <= 0) return false;
            int actual = Cantidad(objeto);
            if (actual < n) return false;
            if (actual == n)
            {
                cantidades.Remove(objeto);
                orden.Remove(objeto);
            }
            else
            {
                cantidades[objeto] = actual - n;
            }
            Cambio?.Invoke();
            return true;
        }

        /// <summary>Reemplaza todo el contenido (carga de partida); respeta el tope. Avisa una vez si algo cambió.</summary>
        public void Restablecer(IEnumerable<KeyValuePair<DatosObjeto, int>> nuevos)
        {
            var antes = new Dictionary<DatosObjeto, int>(cantidades);
            cantidades.Clear();
            orden.Clear();
            foreach (var par in nuevos)
            {
                if (par.Key == null || par.Value <= 0 || cantidades.ContainsKey(par.Key)) continue;
                cantidades[par.Key] = Math.Min(par.Value, par.Key.Maximo);
                orden.Add(par.Key);
            }
            bool igual = antes.Count == cantidades.Count;
            if (igual)
            {
                foreach (var par in cantidades)
                {
                    if (!antes.TryGetValue(par.Key, out int n) || n != par.Value) { igual = false; break; }
                }
            }
            if (!igual) Cambio?.Invoke();
        }
    }
}

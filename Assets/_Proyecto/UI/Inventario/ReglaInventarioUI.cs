using System.Collections.Generic;

namespace MechaCyberpunk.UI.Inventario
{
    /// <summary>Reglas puras de la pantalla de inventario (T-904): filtrar por categoría, contar, moverse por la cuadrícula y desplazarla.</summary>
    public static class ReglaInventarioUI
    {
        public const int Columnas = 4;
        public const int FilasVisibles = 4;

        /// <summary>El nombre de la categoría para el encabezado, en mayúsculas.</summary>
        public static string Nombre(CategoriaInventario categoria)
        {
            switch (categoria)
            {
                case CategoriaInventario.Armas: return "ARMAS";
                case CategoriaInventario.Municion: return "MUNICIÓN";
                case CategoriaInventario.Consumibles: return "CONSUMIBLES";
                case CategoriaInventario.Implantes: return "IMPLANTES";
                case CategoriaInventario.Otros: return "OTROS";
                default: return "TODO";
            }
        }

        /// <summary>El ícono de la categoría (textura de Resources/Menus), o null si no tiene.</summary>
        public static string IconoDeCategoria(CategoriaInventario categoria)
        {
            switch (categoria)
            {
                case CategoriaInventario.Armas: return "Cat_Pistola";
                case CategoriaInventario.Municion: return "Cat_Municion";
                case CategoriaInventario.Consumibles: return "Cat_Consumibles";
                case CategoriaInventario.Implantes: return "Cat_Implantes";
                default: return null;
            }
        }

        /// <summary>Las entradas de una categoría, en el mismo orden. Todo las devuelve todas.</summary>
        public static List<EntradaInventario> Filtrar(IReadOnlyList<EntradaInventario> entradas, CategoriaInventario categoria)
        {
            List<EntradaInventario> resultado = new List<EntradaInventario>();
            if (entradas == null)
            {
                return resultado;
            }
            for (int i = 0; i < entradas.Count; i++)
            {
                if (categoria == CategoriaInventario.Todo || entradas[i].Categoria == categoria)
                {
                    resultado.Add(entradas[i]);
                }
            }
            return resultado;
        }

        /// <summary>Cuántas entradas tiene una categoría.</summary>
        public static int Contar(IReadOnlyList<EntradaInventario> entradas, CategoriaInventario categoria) => Filtrar(entradas, categoria).Count;

        /// <summary>Mueve la casilla elegida <paramref name="dx"/> columnas y <paramref name="dy"/> filas sin salirse; si el destino no existe se queda donde estaba.</summary>
        public static int Mover(int indice, int dx, int dy, int cantidad, int columnas = Columnas)
        {
            if (cantidad <= 0)
            {
                return 0;
            }
            int columna = indice % columnas;
            int fila = indice / columnas;
            int nuevaColumna = columna + dx;
            int nuevaFila = fila + dy;
            if (nuevaColumna < 0 || nuevaColumna >= columnas || nuevaFila < 0)
            {
                return indice;
            }
            int destino = nuevaFila * columnas + nuevaColumna;
            if (destino >= cantidad)
            {
                // Bajar a una fila corta cae en la última casilla; un paso a un lado fuera de la lista no mueve.
                return dy > 0 && dx == 0 ? cantidad - 1 : indice;
            }
            return destino;
        }

        /// <summary>La primera fila que se ve para que la casilla elegida quede a la vista.</summary>
        public static int FilaInicial(int indice, int filaActual, int columnas = Columnas, int filasVisibles = FilasVisibles)
        {
            int fila = indice / columnas;
            if (fila < filaActual)
            {
                return fila;
            }
            if (fila >= filaActual + filasVisibles)
            {
                return fila - filasVisibles + 1;
            }
            return filaActual;
        }

        /// <summary>Pasa a la categoría siguiente o anterior, dando la vuelta.</summary>
        public static CategoriaInventario Siguiente(CategoriaInventario actual, int paso)
        {
            int total = System.Enum.GetValues(typeof(CategoriaInventario)).Length;
            return (CategoriaInventario)(((int)actual + paso % total + total) % total);
        }
    }
}

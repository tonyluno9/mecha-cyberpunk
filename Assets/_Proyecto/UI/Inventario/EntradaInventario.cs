using System.Collections.Generic;

namespace MechaCyberpunk.UI.Inventario
{
    /// <summary>
    /// Una casilla del inventario ya lista para dibujar (C# puro, T-904): lo que el jugador trae de verdad, leído de los servicios.
    /// <see cref="Rareza"/> va de 0 (base) a 3 y decide el filo de la casilla; <see cref="Icono"/> es el nombre de la textura en Resources/Menus
    /// o null si la categoría no tiene ícono.
    /// </summary>
    public class EntradaInventario
    {
        private readonly List<KeyValuePair<string, string>> datos = new List<KeyValuePair<string, string>>();

        public EntradaInventario(string id, string nombre, CategoriaInventario categoria, int cantidad, int maximo, int rareza, string icono, string descripcion)
        {
            Id = id;
            Nombre = nombre;
            Categoria = categoria;
            Cantidad = cantidad;
            Maximo = maximo;
            Rareza = rareza;
            Icono = icono;
            Descripcion = descripcion ?? string.Empty;
        }

        public string Id { get; }
        public string Nombre { get; }
        public CategoriaInventario Categoria { get; }
        public int Cantidad { get; }
        /// <summary>Lo máximo que se puede traer, o 0 si no hay tope que mostrar.</summary>
        public int Maximo { get; }
        public int Rareza { get; }
        public string Icono { get; }
        public string Descripcion { get; }
        /// <summary>Pares etiqueta, valor del detalle (daño, cargador, nivel...).</summary>
        public IReadOnlyList<KeyValuePair<string, string>> Datos => datos;

        public EntradaInventario ConDato(string etiqueta, string valor)
        {
            datos.Add(new KeyValuePair<string, string>(etiqueta, valor));
            return this;
        }
    }
}

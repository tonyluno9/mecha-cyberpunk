using MechaCyberpunk.Core;

namespace MechaCyberpunk.UI.Mapa
{
    /// <summary>Un lugar del catálogo del mapa (T-667): cómo se llama, qué símbolo lleva su icono y de dónde sale su posición.</summary>
    public sealed class DestinoMapa
    {
        public DestinoMapa(IdDestinoMapa id, string nombre, string simbolo, IdLugar lugar, TipoMarcaMapa? marca, params string[] nombresDeObjeto)
        {
            Id = id;
            Nombre = nombre;
            Simbolo = simbolo;
            Lugar = lugar;
            Marca = marca;
            NombresDeObjeto = nombresDeObjeto;
        }

        public IdDestinoMapa Id { get; }

        /// <summary>El nombre que se lee en el mapa grande (mayúsculas).</summary>
        public string Nombre { get; }

        /// <summary>Texto corto del icono (un carácter). Vacío si el icono es un dibujo (la casa).</summary>
        public string Simbolo { get; }

        /// <summary>El lugar de Core (<see cref="ILugar"/>) que es este destino, o <see cref="IdLugar.Ninguno"/>.</summary>
        public IdLugar Lugar { get; }

        /// <summary>El tipo de marca de lugar de T-409 que ya lo dibuja, si lo hay (para no dibujarlo dos veces).</summary>
        public TipoMarcaMapa? Marca { get; }

        /// <summary>Nombres de objetos de la escena donde está el lugar, por si no hay <see cref="ILugar"/> (se busca el primero activo).</summary>
        public string[] NombresDeObjeto { get; }

        /// <summary>True si el nombre va arriba del icono (para que no se encime con el de un lugar vecino).</summary>
        public bool EtiquetaArriba { get; private set; }

        public DestinoMapa ConEtiquetaArriba()
        {
            EtiquetaArriba = true;
            return this;
        }
    }
}

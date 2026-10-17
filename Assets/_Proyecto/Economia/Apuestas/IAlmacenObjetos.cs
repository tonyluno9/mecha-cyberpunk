namespace MechaCyberpunk.Economia
{
    /// <summary>Donde caen los objetos ganados en una apuesta: el inventario del jugador (T-144).</summary>
    public interface IAlmacenObjetos
    {
        bool PuedeAgregar(DatosObjeto objeto, int n);

        /// <summary>Agrega y devuelve cuántos entraron.</summary>
        int Agregar(DatosObjeto objeto, int n);
    }
}

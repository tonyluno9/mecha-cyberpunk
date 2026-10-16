namespace MechaCyberpunk.Mecha.Montaje
{
    /// <summary>Cómo empieza el mecha: con su piloto ya dentro (como en la arena) o vacío y estacionado, esperando a que alguien suba.</summary>
    public enum ArranqueMontaje
    {
        /// <summary>Sin piloto: cámara, oído y control propios desde el inicio. No se registra como montable.</summary>
        Directo,
        /// <summary>Vacío: cámara, oído y control apagados. Se registra como montable.</summary>
        Estacionado
    }
}

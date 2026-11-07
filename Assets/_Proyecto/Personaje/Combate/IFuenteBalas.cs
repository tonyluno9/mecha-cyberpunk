namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>De dónde salen las balas de una recarga (T-720). Un cargador sin fuente tiene balas infinitas (la pistola).</summary>
    public interface IFuenteBalas
    {
        /// <summary>Balas que quedan en la reserva.</summary>
        int Disponibles { get; }

        /// <summary>Saca hasta <paramref name="cantidad"/> balas de la reserva y devuelve las que sacó.</summary>
        int Tomar(int cantidad);
    }
}

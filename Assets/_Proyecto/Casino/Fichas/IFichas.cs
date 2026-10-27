namespace MechaCyberpunk.Casino.Fichas
{
    /// <summary>
    /// Las fichas del casino del jugador (T-302). Se busca en <c>Registro&lt;IFichas&gt;</c>; la registra <see cref="ServicioFichas"/>.
    /// Contrato propio del casino (solo lo usan cosas de Alan); el dinero sigue en <c>ICartera</c>.
    /// </summary>
    public interface IFichas
    {
        /// <summary>Fichas actuales. Nunca negativo.</summary>
        int Saldo { get; }

        /// <summary>True si <paramref name="cantidad"/> es mayor que 0 y el saldo alcanza. No cambia nada.</summary>
        bool PuedePagar(int cantidad);

        /// <summary>Quita fichas solo si alcanza. Con cantidad menor o igual a 0 devuelve false y no cambia nada.</summary>
        bool IntentarPagar(int cantidad);

        /// <summary>Suma fichas. Ignora cantidades menores o iguales a 0.</summary>
        void Abonar(int cantidad);
    }
}

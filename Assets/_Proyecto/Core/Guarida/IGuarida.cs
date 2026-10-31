namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Las mejoras de la guarida del jugador (T-448): armero, garaje y cama. La implementa ServicioGuarida (Progresion) y va en
    /// <c>Registro&lt;IGuarida&gt;</c>; la leen el arma a pie (daño), el garaje del carro (Vehiculos) y la cama (Progresion).
    /// Sin servicio registrado no hay mejoras.
    /// </summary>
    public interface IGuarida
    {
        /// <summary>Fracción extra de daño de las armas de fuego a pie (0.08 = 8 % más). Sin armero: 0.</summary>
        float BonoDanioArmas { get; }

        /// <summary>Nivel del garaje: 0 sin garaje, 1 el carro de la casa, 2 y 3 lo hacen más resistente.</summary>
        int NivelGaraje { get; }

        /// <summary>Cuántas veces su vida normal aguanta el carro del garaje (1 = normal).</summary>
        float FactorVidaCarro { get; }

        /// <summary>True si dormir en tu cama te deja con la vida llena.</summary>
        bool CamaCura { get; }

        /// <summary>Fracción del calor policial que se borra al dormir (0 nada, 1 todo).</summary>
        float CamaBajaCalor { get; }
    }
}

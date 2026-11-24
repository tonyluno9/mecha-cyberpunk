namespace MechaCyberpunk.VR.Manos
{
    /// <summary>VR40: qué pose base lleva una mano MN01 (<see cref="ReglaManosMN01VR.Elegir"/>).</summary>
    public enum PoseManoVR
    {
        /// <summary>Sin nada: el grip mezcla Abierta → Puño y el gatillo dobla el índice.</summary>
        Libre = 0,
        /// <summary>Agarre de pistola con el índice estirado; el gatillo lo dobla (Mano_Gatillo).</summary>
        Pistola = 1,
        /// <summary>Izquierda en el guardamanos de un arma larga.</summary>
        ArmaLargaApoyo = 2,
        /// <summary>Izquierda sosteniendo un cargador.</summary>
        Cargador = 3,
        /// <summary>Agarre redondo de la granada.</summary>
        Granada = 4,
        /// <summary>Índice estirado para menús y UI.</summary>
        Senalar = 5
    }
}

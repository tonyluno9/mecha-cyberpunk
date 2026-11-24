namespace MechaCyberpunk.VR.Menus
{
    /// <summary>Qué se hace en VR con un Canvas de pantalla (Overlay o Screen Space Camera).</summary>
    public enum TipoLienzoVR
    {
        /// <summary>Menús, tiendas, avisos: panel en el espacio frente a la cabeza; el láser lo usa.</summary>
        Frente,
        /// <summary>Fundidos, pantallas negras y de carga: pegado a la cabeza y más grande que la vista.</summary>
        PantallaCompleta,
        /// <summary>HUD de pantalla (mira, minimapa, barras sobre enemigos...): no se ve en VR; vida y dinero van en la muñeca.</summary>
        Oculto,
    }
}

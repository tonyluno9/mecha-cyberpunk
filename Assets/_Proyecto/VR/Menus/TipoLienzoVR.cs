namespace MechaCyberpunk.VR.Menus
{
    /// <summary>Qué se hace en VR con un Canvas de pantalla (Overlay o Screen Space Camera).</summary>
    public enum TipoLienzoVR
    {
        /// <summary>Menús, tiendas, avisos: panel en el espacio frente a la cabeza; el láser lo usa.</summary>
        Frente,
        /// <summary>Fundidos, pantallas negras y de carga: pegado a la cabeza y más grande que la vista.</summary>
        PantallaCompleta,
        /// <summary>Lo que en VR no se ve (la mira del francotirador, las barras sobre los enemigos, el negro de la intro).</summary>
        Oculto,
        /// <summary>
        /// VR14: el HUD de pantalla de PC (vida, dinero, minimapa, objetivo, arma, avisos, tutoriales, subtítulos...): Screen Space Camera
        /// sobre la cámara del HUD de casco (<see cref="HudCascoVR"/>), que lo dibuja en un panel curvo pegado a la cabeza.
        /// </summary>
        Casco,
    }
}

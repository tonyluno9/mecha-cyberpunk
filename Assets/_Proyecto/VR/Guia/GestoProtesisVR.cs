namespace MechaCyberpunk.VR.Guia
{
    /// <summary>VR20/VR31b: la pose de las dos manos que activa una prótesis.</summary>
    public enum GestoProtesisVR
    {
        Ninguno = 0,
        /// <summary>VR45: las dos manos abajo, de la cadera a la rodilla: prótesis de PIERNAS.</summary>
        Piernas = 1,
        /// <summary>Los dos brazos abiertos a los lados, en cruz (pose T aproximada): prótesis de BRAZOS.</summary>
        Brazos = 2,
        /// <summary>VR31b: las dos manos a los lados de la cabeza: prótesis de VISIÓN (habilidad CABEZA).</summary>
        Vision = 3
    }
}

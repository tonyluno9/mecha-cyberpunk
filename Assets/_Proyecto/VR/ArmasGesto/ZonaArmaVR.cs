namespace MechaCyberpunk.VR
{
    /// <summary>VR27: dónde está una mano respecto al cuerpo, para los gestos de sacar arma y tomar cargador (<see cref="ReglaGestosArmaVR"/>).</summary>
    public enum ZonaArmaVR
    {
        Ninguna,
        /// <summary>Mano derecha en la cadera derecha: sacar o guardar el arma corta.</summary>
        CaderaDerecha,
        /// <summary>Mano derecha sobre el hombro derecho (arriba y atrás): sacar o guardar el arma larga.</summary>
        HombroDerecho,
        /// <summary>Mano izquierda en la cadera izquierda: tomar un cargador nuevo.</summary>
        CaderaIzquierda
    }
}

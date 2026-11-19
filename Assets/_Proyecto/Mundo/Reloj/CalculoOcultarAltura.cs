namespace MechaCyberpunk.Mundo.Reloj
{
    /// <summary>Lógica pura (T-1462): decide si un fondo está oculto según la altura de la cámara, con histéresis para que no parpadee.</summary>
    public static class CalculoOcultarAltura
    {
        /// <summary>Se oculta al pasar de <paramref name="umbral"/> y reaparece al bajar de umbral menos histéresis (todo en m).</summary>
        public static bool Oculto(bool estabaOculto, float altura, float umbral, float histeresis)
        {
            return estabaOculto ? altura > umbral - histeresis : altura > umbral;
        }

        /// <summary>Al revés: está oculto mientras la cámara está por debajo de <paramref name="umbral"/> (solo se ve desde arriba); reaparece al pasar de umbral más histéresis.</summary>
        public static bool OcultoPorDebajo(bool estabaOculto, float altura, float umbral, float histeresis)
        {
            return estabaOculto ? altura < umbral + histeresis : altura < umbral;
        }
    }
}

namespace MechaCyberpunk.Mundo.Pruebas
{
    /// <summary>
    /// Un punto de vista de la medición de FPS (T-1366). x y z son del mundo; la altura se calcula con un rayo hacia abajo
    /// (empieza en <see cref="RayoDesdeY"/>) más <see cref="SobreSuelo"/>. La cámara mira a (MirarX, suelo + <see cref="MirarSobreSuelo"/>, MirarZ).
    /// </summary>
    public readonly struct VistaFpsT1366
    {
        public readonly string Zona;
        public readonly string Nombre;
        public readonly float X;
        public readonly float Z;
        public readonly float MirarX;
        public readonly float MirarZ;
        public readonly float SobreSuelo;
        public readonly float MirarSobreSuelo;
        public readonly float RayoDesdeY;

        public VistaFpsT1366(string zona, string nombre, float x, float z, float mirarX, float mirarZ,
            float sobreSuelo = 1.7f, float mirarSobreSuelo = 1.7f, float rayoDesdeY = 120f)
        {
            Zona = zona;
            Nombre = nombre;
            X = x;
            Z = z;
            MirarX = mirarX;
            MirarZ = mirarZ;
            SobreSuelo = sobreSuelo;
            MirarSobreSuelo = mirarSobreSuelo;
            RayoDesdeY = rayoDesdeY;
        }

        public string Etiqueta => Zona + "/" + Nombre;
    }
}

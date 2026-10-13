namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>Valores de manejo que usa la lógica pura (copia de ConfigCarro sin depender de Unity).</summary>
    public struct ParametrosManejo
    {
        public float Masa;
        public float VelocidadMaxima;
        public float VelocidadReversa;
        public float UmbralReversa;
        public float FuerzaMotor;
        public float FuerzaReversa;
        public float FuerzaFreno;
        public float FuerzaFrenoMano;
        public float ResistenciaRodadura;
        public float AnguloDireccionParado;
        public float AnguloDireccionMaximo;
        public float ExponenteDireccion;
        public float DistanciaEntreEjes;
        public float AceleracionLateralMaxima;
        public float AgarreLateral;
        public float FactorAgarreFrenoMano;
        public float GananciaEstacionamiento;
    }
}

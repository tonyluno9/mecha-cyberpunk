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
        /// <summary>Segundos que se queda parado el carro al cambiar de sentido (de adelante a atrás o al revés) antes de empujar (s). 0 desactiva la espera.</summary>
        public float EsperaCambioSentido;
        /// <summary>Segundos que tarda el motor en llegar a toda su fuerza al arrancar en reversa o tras cambiar de sentido (s). 0 desactiva la rampa.</summary>
        public float RampaMarcha;
    }
}

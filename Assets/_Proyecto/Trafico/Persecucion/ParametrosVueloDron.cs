namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>T-1008: los números del vuelo de un dron de policía (vienen de <c>ConfigPersecucion</c>); las pruebas los arman a mano.</summary>
    public struct ParametrosVueloDron
    {
        /// <summary>Velocidad horizontal máxima (m/s).</summary>
        public float VelocidadMaxima;
        /// <summary>Aceleración horizontal máxima al arrancar o girar (m/s2).</summary>
        public float AceleracionMaxima;
        /// <summary>Desaceleración que se planea al llegar; menor que la máxima para frenar suave (m/s2).</summary>
        public float AceleracionFrenado;
        /// <summary>Velocidad vertical máxima (m/s).</summary>
        public float VelocidadVertical;
        /// <summary>Aceleración vertical máxima (m/s2).</summary>
        public float AceleracionVertical;
        /// <summary>Qué tan rápido puede girar el rumbo (grados/s).</summary>
        public float GradosRumboPorSegundo;
        /// <summary>Inclinación máxima de cabeceo y alabeo (grados).</summary>
        public float InclinacionMaxima;
        /// <summary>Grados que se inclina por cada m/s2 de aceleración.</summary>
        public float GradosPorAceleracion;
        /// <summary>Rapidez con la que la inclinación alcanza su valor (1/s).</summary>
        public float SuavizadoInclinacion;
        /// <summary>Cuánto sube y baja flotando (m).</summary>
        public float AmplitudFlote;
        /// <summary>Cuántas veces por segundo sube y baja al flotar (Hz).</summary>
        public float FrecuenciaFlote;
        /// <summary>Balanceo leve de lado a lado en el aire (grados).</summary>
        public float AmplitudBalanceo;
        /// <summary>Cuántas veces por segundo se balancea (Hz).</summary>
        public float FrecuenciaBalanceo;
    }
}

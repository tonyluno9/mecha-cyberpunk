namespace MechaCyberpunk.Core
{
    /// <summary>Lo que pasó al aplicar un daño.</summary>
    public readonly struct ResultadoDanio
    {
        /// <summary>Puntos de vida que de verdad se restaron.</summary>
        public float DanioAplicado { get; }
        /// <summary>Cómo se defendió el que recibió el daño.</summary>
        public TipoBloqueo Bloqueo { get; }
        /// <summary>Si este daño dejó al peleador en KO.</summary>
        public bool CausoKO { get; }

        public ResultadoDanio(float danioAplicado, TipoBloqueo bloqueo, bool causoKO)
        {
            DanioAplicado = danioAplicado;
            Bloqueo = bloqueo;
            CausoKO = causoKO;
        }

        /// <summary>Resultado de un daño que no se tomó en cuenta.</summary>
        public static ResultadoDanio Ignorado => new ResultadoDanio(0f, TipoBloqueo.Ninguno, false);
    }
}

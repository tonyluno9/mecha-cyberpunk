namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>T-1013: en qué va el rifle de plasma.</summary>
    public enum EstadoPlasma
    {
        Reposo,
        Cargando,
        Rayo
    }

    /// <summary>
    /// T-1013: el rifle de plasma (lógica pura). Mantener el disparo carga durante <c>segundosCarga</c>; soltar antes cancela la carga. Al completarla
    /// sale el rayo, que dura <c>segundosRayo</c> aunque se suelte el botón, y luego vuelve al reposo (para otro rayo hay que soltar y volver a cargar).
    /// </summary>
    public sealed class ReglaRayoPlasma
    {
        private readonly float segundosCarga;
        private readonly float segundosRayo;
        private bool esperaSoltar;

        public ReglaRayoPlasma(float segundosCarga, float segundosRayo)
        {
            this.segundosCarga = segundosCarga < 0f ? 0f : segundosCarga;
            this.segundosRayo = segundosRayo < 0.01f ? 0.01f : segundosRayo;
        }

        public EstadoPlasma Estado { get; private set; } = EstadoPlasma.Reposo;

        /// <summary>De 0 a 1 mientras carga (1 al soltar el rayo).</summary>
        public float Carga { get; private set; }

        /// <summary>Segundos de rayo que quedan (0 si no hay rayo).</summary>
        public float RestanteRayo { get; private set; }

        /// <summary>
        /// Avanza <paramref name="dt"/> s con el botón <paramref name="pulsado"/>. <paramref name="puedeDisparar"/> dice si hay carga en el arma (se pide al
        /// terminar de cargar). Devuelve true en el cuadro en que empieza el rayo (para gastar la celda y avisar del disparo).
        /// </summary>
        public bool Avanzar(float dt, bool pulsado, System.Func<bool> puedeDisparar)
        {
            switch (Estado)
            {
                case EstadoPlasma.Reposo:
                    if (!pulsado)
                    {
                        esperaSoltar = false;
                    }
                    if (pulsado && !esperaSoltar)
                    {
                        Estado = EstadoPlasma.Cargando;
                        Carga = 0f;
                        return AvanzarCarga(dt, puedeDisparar);
                    }
                    return false;
                case EstadoPlasma.Cargando:
                    if (!pulsado)
                    {
                        Cancelar();
                        return false;
                    }
                    return AvanzarCarga(dt, puedeDisparar);
                default:
                    RestanteRayo -= dt;
                    if (RestanteRayo <= 0f)
                    {
                        RestanteRayo = 0f;
                        Carga = 0f;
                        Estado = EstadoPlasma.Reposo;
                        esperaSoltar = pulsado;
                    }
                    return false;
            }
        }

        private bool AvanzarCarga(float dt, System.Func<bool> puedeDisparar)
        {
            Carga = segundosCarga <= 0f ? 1f : Carga + dt / segundosCarga;
            if (Carga < 1f)
            {
                return false;
            }
            Carga = 1f;
            if (puedeDisparar != null && !puedeDisparar())
            {
                // Sin carga en el arma: se queda en reposo hasta que se suelte el botón.
                Cancelar();
                esperaSoltar = true;
                return false;
            }
            Estado = EstadoPlasma.Rayo;
            RestanteRayo = segundosRayo;
            return true;
        }

        /// <summary>Corta la carga o el rayo (cambiar de arma, morir, subir al carro).</summary>
        public void Cancelar()
        {
            Estado = EstadoPlasma.Reposo;
            Carga = 0f;
            RestanteRayo = 0f;
        }
    }
}

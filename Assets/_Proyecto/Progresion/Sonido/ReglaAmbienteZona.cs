using MechaCyberpunk.Core;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>Qué AMB_ suena según el lugar y la zona del jugador (T-470). Pura, para probarla sin escena.</summary>
    public static class ReglaAmbienteZona
    {
        public const string CalleNoche = "AMB_CALLE_NOCHE";
        public const string CalleDia = "AMB_CALLE_DIA";
        public const string InteriorCasa = "AMB_INTERIOR_CASA";
        public const string Mercado = "AMB_MERCADO";
        public const string Tianguis = "AMB_TIANGUIS";
        public const string NeonAlto = "AMB_NEON_ALTO";
        public const string Bar = "AMB_BAR";
        public const string Casino = "AMB_CASINO";
        public const string Clandestino = "AMB_CLANDESTINO";
        public const string Clinica = "AMB_CLINICA";

        /// <summary>
        /// Dentro de un lugar manda el lugar; en la calle manda la zona (tianguis, neón) y si no, calle de día o de noche.
        /// El callejón (AMB_CALLEJON) no tiene aún quien lo avise; I+D+I, El Fox y VivaCoin quedan dentro de Neón Alto.
        /// </summary>
        public static string Elegir(IdLugar lugar, string zona, bool esDeNoche)
        {
            switch (lugar)
            {
                case IdLugar.Casa: return InteriorCasa;
                case IdLugar.MercadoNegro: return Mercado;
                case IdLugar.Clinica: return Clinica;
                case IdLugar.Bar: return Bar;
                case IdLugar.Casino: return Casino;
                case IdLugar.Clandestino: return Clandestino;
                default:
                    if (zona == "tianguis") return Tianguis;
                    if (zona == "neon") return NeonAlto;
                    return esDeNoche ? CalleNoche : CalleDia;
            }
        }
    }
}

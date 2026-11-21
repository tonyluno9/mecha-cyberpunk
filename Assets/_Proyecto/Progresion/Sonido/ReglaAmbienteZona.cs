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

        // T-1535: los ambientes nuevos del 20-nov, uno por barrio (Deshuesadero, Avenida y Neón Alto no tienen propio: LEEME_SONIDOS).
        public const string Lomas = "AMB_LOMAS";
        public const string SuburbiosNoche = "AMB_SUBURBIOS_NOCHE";
        public const string AzoteaCiudad = "AMB_AZOTEA_CIUDAD";
        public const string Nova = "AMB_NOVA";

        /// <summary>Nombre de la zona de mundo (IZonaDelMundo, ZonaTrafico) de Las Lomas.</summary>
        public const string BarrioLomas = "LasLomas";
        /// <summary>Id de la zona de territorio de los Suburbios.</summary>
        public const string ZonaSuburbios = "fosa";
        /// <summary>Id de la zona de territorio de La Refinería, las calles de Nova.</summary>
        public const string ZonaNova = "refineria";

        /// <summary>Altura (m, y del jugador) desde la que se está en una azotea: las azoteas de la ciudad están a y = 26.</summary>
        public const float AlturaAzotea = 20f;
        /// <summary>Altura (m) por debajo de la cual se deja la azotea (histéresis para no parpadear en la orilla).</summary>
        public const float AlturaSalidaAzotea = 17f;
        /// <summary>Cruce (s) entre dos ambientes de calle al cambiar de barrio o subir a una azotea (de 2 a 4 s).</summary>
        public const float SegundosCruceBarrio = 3f;
        public const float MinimoCruceBarrio = 2f;
        public const float MaximoCruceBarrio = 4f;

        /// <summary>
        /// Dentro de un lugar manda el lugar; en la calle manda la zona (tianguis, neón) y si no, calle de día o de noche.
        /// El callejón (AMB_CALLEJON) no tiene aún quien lo avise; I+D+I, El Fox y VivaCoin quedan dentro de Neón Alto.
        /// </summary>
        public static string Elegir(IdLugar lugar, string zona, bool esDeNoche) => Elegir(lugar, zona, null, esDeNoche, false);

        /// <summary>
        /// T-1535: igual, con el barrio de mundo (Las Lomas) y si el jugador está en una azotea. En la calle manda, en orden: la azotea, Las Lomas,
        /// La Refinería (Nova), los Suburbios de noche, el tianguis y Neón Alto. Deshuesadero y Avenida no tienen ambiente propio: calle de día o de noche.
        /// </summary>
        public static string Elegir(IdLugar lugar, string zona, string barrio, bool esDeNoche, bool enAltura)
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
                    if (enAltura) return AzoteaCiudad;
                    if (barrio == BarrioLomas) return Lomas;
                    if (zona == ZonaNova) return Nova;
                    if (zona == ZonaSuburbios && esDeNoche) return SuburbiosNoche;
                    if (zona == "tianguis") return Tianguis;
                    if (zona == "neon") return NeonAlto;
                    return esDeNoche ? CalleNoche : CalleDia;
            }
        }

        /// <summary>Si el ID es un ambiente de calle (de un barrio, de una azotea o de la calle): entre dos de estos el cruce es largo.</summary>
        public static bool EsAmbienteDeCalle(string id)
        {
            switch (id)
            {
                case CalleNoche:
                case CalleDia:
                case Tianguis:
                case NeonAlto:
                case Lomas:
                case SuburbiosNoche:
                case AzoteaCiudad:
                case Nova:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Duración (s) del cruce: la del barrio si los dos ambientes son de calle, la corta (<paramref name="segundosLugar"/>) si entra o sale de un lugar.</summary>
        public static float SegundosCruce(string anterior, string nuevo, float segundosLugar, float segundosBarrio)
        {
            float barrio = segundosBarrio < MinimoCruceBarrio ? MinimoCruceBarrio : (segundosBarrio > MaximoCruceBarrio ? MaximoCruceBarrio : segundosBarrio);
            return EsAmbienteDeCalle(anterior) && EsAmbienteDeCalle(nuevo) ? barrio : segundosLugar;
        }

        /// <summary>Si el jugador está en una azotea, con histéresis: entra a <see cref="AlturaAzotea"/> y sale por debajo de <see cref="AlturaSalidaAzotea"/>.</summary>
        public static bool EnAzotea(float altura, bool estabaArriba) => estabaArriba ? altura >= AlturaSalidaAzotea : altura >= AlturaAzotea;
    }
}

using System.Collections.Generic;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>T-1535: qué bucles de clima suenan según la lluvia, la tormenta, el viento y dónde está el jugador. Pura, para probarla sin escena.</summary>
    public static class ReglaClimaSonido
    {
        public const string LluviaFuerte = "CLI_LLUVIA_FUERTE";
        public const string Tormenta = "CLI_TORMENTA_TRUENOS";
        public const string Viento = "CLI_VIENTO";
        /// <summary>Lo que baja la lluvia y el viento de afuera cuando el jugador va dentro de un carro (0 a 1). CLI_LLUVIA_DENTRO_CARRO lo maneja Vehiculos (T-1534).</summary>
        public const float FactorEnCarro = 0.35f;
        public const string LluviaSobreLamina = "CLI_LLUVIA_SOBRE_LAMINA";

        /// <summary>Viento mínimo (0 a 1) para que suene el bucle del viento.</summary>
        public const float VientoMinimo = 0.2f;

        /// <summary>
        /// Los bucles de clima 2D que suenan ahora. Con lluvia: la tormenta (con truenos) o la lluvia fuerte; en un carro suenan igual (SonidosClima los baja con <see cref="FactorEnCarro"/>);
        /// dentro de un lugar cerrado no se oye lluvia de calle. El viento suena a la intemperie. La lámina es 3D y la maneja <see cref="SonidosMundo"/>.
        /// </summary>
        public static List<string> Bucles(float lluvia, bool tormenta, float viento, bool enCarro, bool enLugarCerrado)
        {
            List<string> ids = new List<string>();
            if (lluvia > 0f && !enLugarCerrado)
            {
                ids.Add(tormenta ? Tormenta : LluviaFuerte);
            }
            if (viento >= VientoMinimo && !enLugarCerrado)
            {
                ids.Add(Viento);
            }
            return ids;
        }

        /// <summary>Si la lámina de los toldos debe sonar: llueve y el jugador está a la intemperie (no en un lugar cerrado ni en un carro).</summary>
        public static float Factor(bool enCarro) => enCarro ? FactorEnCarro : 1f;

        /// <summary>Si la lámina de los toldos debe sonar (ver arriba).</summary>
        public static bool SuenaLamina(float lluvia, bool enCarro, bool enLugarCerrado) => lluvia > 0f && !enCarro && !enLugarCerrado;
    }
}

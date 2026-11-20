using System;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>
    /// T-1488: el peso del carro en las cuentas. Transferencia de carga al frenar o acelerar (la carrocería cabecea), qué tan alta se aplica la fuerza
    /// de las llantas para provocarla, ayuda contra el vuelco a poca velocidad y amortiguación de la inclinación. Lógica pura.
    /// </summary>
    public static class CalculoPeso
    {
        /// <summary>Rapidez (m/s) a la que el antivuelco de baja velocidad ya no suma nada.</summary>
        public const float RapidezPlenaAntivuelco = 6f;

        /// <summary>
        /// Altura (m, sobre el origen del carro) a la que se aplica la fuerza de las llantas. Con <paramref name="factor"/> 0 va a la altura del
        /// centro de masa (no cabecea, como antes); con 1 va al piso (cabeceo físico completo).
        /// </summary>
        public static float AlturaFuerza(float alturaContacto, float alturaCentroMasa, float factor)
        {
            float f = Math.Clamp(factor, 0f, 1f);
            return alturaCentroMasa + (alturaContacto - alturaCentroMasa) * f;
        }

        /// <summary>Carga (N) que pasa del eje trasero al delantero al frenar (positivo) o al revés al acelerar, con aceleración <paramref name="aceleracion"/> (m/s²).</summary>
        public static float TransferenciaCarga(float masa, float aceleracion, float alturaCentroMasa, float distanciaEntreEjes)
        {
            return distanciaEntreEjes <= 0f ? 0f : masa * aceleracion * alturaCentroMasa / distanciaEntreEjes;
        }

        /// <summary>
        /// Cabeceo (°) estático que provoca una fuerza longitudinal constante de <paramref name="fuerza"/> N aplicada con <paramref name="factorCabeceo"/>:
        /// torque entre rigidez de cabeceo de los cuatro resortes (rigidez por (distancia entre ejes)²).
        /// </summary>
        public static float CabeceoEstimadoGrados(float fuerza, float alturaCentroMasa, float factorCabeceo, float rigidezPorRueda, float distanciaEntreEjes)
        {
            float rigidezCabeceo = rigidezPorRueda * distanciaEntreEjes * distanciaEntreEjes;
            if (rigidezCabeceo <= 0f)
            {
                return 0f;
            }
            float torque = fuerza * alturaCentroMasa * Math.Clamp(factorCabeceo, 0f, 1f);
            return (float)(Math.Atan(torque / rigidezCabeceo) * 180.0 / Math.PI);
        }

        /// <summary>
        /// Multiplicador del torque antivuelco: <paramref name="extraParado"/> con el vehículo detenido, baja a 1 al llegar a
        /// <see cref="RapidezPlenaAntivuelco"/>. Las motos lo usan para no caerse solas a baja velocidad. Nunca baja de 1.
        /// </summary>
        public static float FactorAntivuelco(float rapidez, float extraParado)
        {
            float extra = Math.Max(1f, extraParado);
            float t = Math.Clamp(Math.Abs(rapidez) / RapidezPlenaAntivuelco, 0f, 1f);
            return extra + (1f - extra) * t;
        }

        /// <summary>Torque (N·m) que frena una velocidad angular de cabeceo o balanceo (rad/s): se opone a ella.</summary>
        public static float TorqueAmortiguacion(float velocidadAngular, float coeficiente)
        {
            return -velocidadAngular * Math.Max(0f, coeficiente);
        }
    }
}

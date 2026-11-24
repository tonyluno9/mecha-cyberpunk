using System;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR38: lógica pura del giro SUAVE con el stick derecho (solo su eje X). Pasada la <see cref="ZonaMuerta"/>, la velocidad pedida es proporcional
    /// al stick hasta la VELOCIDAD DE GIRO de Opciones (60°/s por defecto); la velocidad real sube hacia ella en <see cref="SegundosAcelerar"/> s
    /// (aceleración suave, sin arranque de golpe) y baja en <see cref="SegundosFrenar"/> s al soltar (se detiene casi enseguida).
    /// </summary>
    public static class ReglaGiroSuaveVR
    {
        /// <summary>Fracción del stick (0..1) que no gira.</summary>
        public const float ZonaMuerta = 0.2f;
        /// <summary>Segundos de 0 a la velocidad máxima.</summary>
        public const float SegundosAcelerar = 0.15f;
        /// <summary>Segundos de la velocidad máxima a 0 (o al bajar).</summary>
        public const float SegundosFrenar = 0.06f;

        /// <summary>Qué fracción de la velocidad máxima pide el stick: 0 dentro de la zona muerta, 1 a fondo, lineal entre medio.</summary>
        public static float Proporcion(float fuerza)
        {
            float f = Math.Abs(fuerza);
            if (f <= ZonaMuerta)
            {
                return 0f;
            }
            return Math.Min(1f, (f - ZonaMuerta) / (1f - ZonaMuerta));
        }

        /// <summary>Velocidad pedida (°/s, con signo) para el eje X del stick y la velocidad máxima.</summary>
        public static float Objetivo(float x, float maxima) => Math.Sign(x) * Proporcion(x) * maxima;

        /// <summary>
        /// La velocidad (°/s, con signo) del cuadro siguiente: va de <paramref name="actual"/> hacia <paramref name="objetivo"/>, rápido al frenar o al
        /// cambiar de lado, suave al acelerar. Nunca pasa del objetivo.
        /// </summary>
        public static float Siguiente(float actual, float objetivo, float maxima, float dt)
        {
            if (dt <= 0f || maxima <= 0f)
            {
                return maxima <= 0f ? 0f : actual;
            }
            bool frena = Math.Abs(objetivo) < Math.Abs(actual) || (actual != 0f && Math.Sign(objetivo) != Math.Sign(actual));
            float paso = maxima / (frena ? SegundosFrenar : SegundosAcelerar) * dt;
            float diferencia = objetivo - actual;
            if (Math.Abs(diferencia) <= paso)
            {
                return objetivo;
            }
            return actual + Math.Sign(diferencia) * paso;
        }
    }
}

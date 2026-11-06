using System;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>T-658: el volante no salta de un lado al otro: sigue a la tecla a una velocidad limitada (más rápido al volver al centro).</summary>
    public static class CalculoDireccion
    {
        /// <summary>
        /// Acerca <paramref name="actual"/> (-1 a 1) a <paramref name="objetivo"/> (-1 a 1) sin pasar de <paramref name="tasaGiro"/> (1/s) al girar
        /// ni de <paramref name="tasaCentrado"/> (1/s) al volver hacia el centro. Una tasa de 0 o menos salta directo al objetivo.
        /// </summary>
        public static float Suavizar(float actual, float objetivo, float dt, float tasaGiro, float tasaCentrado)
        {
            objetivo = Math.Clamp(objetivo, -1f, 1f);
            bool haciaElCentro = Math.Abs(objetivo) < Math.Abs(actual) || (actual != 0f && Math.Sign(objetivo) != Math.Sign(actual));
            float tasa = haciaElCentro ? tasaCentrado : tasaGiro;
            if (tasa <= 0f)
            {
                return objetivo;
            }
            float paso = tasa * dt;
            if (Math.Abs(objetivo - actual) <= paso)
            {
                return objetivo;
            }
            return actual + Math.Sign(objetivo - actual) * paso;
        }
    }
}

using UnityEngine;

namespace MechaCyberpunk.Combate.Habilidades
{
    /// <summary>Lógica pura de los efectos de las habilidades (T-175): salva, cono, paso del proyectil y de la embestida.</summary>
    public static class ReglaEfectosHabilidad
    {
        /// <summary>Tope de vida de un proyectil (s).</summary>
        public const float VidaMaximaProyectil = 3f;

        // Tolerancia del borde del cono (grados), para que el redondeo no deje fuera lo que está justo en el borde.
        private const float MargenAngulo = 0.01f;

        /// <summary>Giros en Y (grados) de cada proyectil de una salva, repartidos de -dispersión a +dispersión. Uno solo sale recto.</summary>
        public static float[] AngulosSalva(int cantidad, float dispersion)
        {
            int n = Mathf.Max(1, cantidad);
            float[] angulos = new float[n];
            if (n == 1)
            {
                return angulos;
            }
            for (int i = 0; i < n; i++)
            {
                angulos[i] = Mathf.Lerp(-dispersion, dispersion, i / (float)(n - 1));
            }
            return angulos;
        }

        /// <summary>Si un punto cae dentro del cono, medido en el plano (sin altura). Con 360 grados cuenta todo el alrededor.</summary>
        public static bool EnCono(Vector3 origen, Vector3 frente, Vector3 punto, float alcance, float anguloTotal)
        {
            Vector3 hacia = punto - origen;
            hacia.y = 0f;
            frente.y = 0f;
            float distancia = hacia.magnitude;
            if (distancia > alcance)
            {
                return false;
            }
            if (anguloTotal >= 360f || distancia < 1e-4f || frente.sqrMagnitude < 1e-8f)
            {
                return true;
            }
            return Vector3.Angle(frente, hacia) <= anguloTotal * 0.5f + MargenAngulo;
        }

        /// <summary>Cuánto avanza un proyectil este cuadro sin pasar de su alcance.</summary>
        public static float AvanceProyectil(float velocidad, float dt, float recorrido, float alcance)
        {
            return Mathf.Clamp(velocidad * dt, 0f, Mathf.Max(0f, alcance - recorrido));
        }

        /// <summary>Cuánto avanza la embestida este cuadro: distancia/duración x dt, sin pasar de la distancia total.</summary>
        public static float PasoEmbestida(float distancia, float duracion, float recorrido, float dt)
        {
            if (duracion <= 0f)
            {
                return Mathf.Max(0f, distancia - recorrido);
            }
            return Mathf.Clamp(distancia / duracion * dt, 0f, Mathf.Max(0f, distancia - recorrido));
        }
    }
}

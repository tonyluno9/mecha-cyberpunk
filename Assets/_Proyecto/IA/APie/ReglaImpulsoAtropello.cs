using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>T25: lógica pura de cuando un carro arrolla a un NPC (peatón, pandillero, policía): si lo tira, con qué velocidad sale volando y cuánto daño recibe.</summary>
    public static class ReglaImpulsoAtropello
    {
        /// <summary>Rapidez del carro (m/s) desde la cual arrolla a un NPC.</summary>
        public const float RapidezMinima = 4f;
        /// <summary>Fracción de la velocidad horizontal del carro que se lleva el NPC.</summary>
        public const float FraccionHorizontal = 0.85f;
        /// <summary>Tope de la rapidez horizontal con la que sale volando (m/s).</summary>
        public const float RapidezHorizontalMaxima = 18f;
        /// <summary>Impulso vertical por cada m/s del carro, y sus topes (m/s).</summary>
        public const float VerticalPorRapidez = 0.45f;
        public const float VerticalMinimo = 3f;
        public const float VerticalMaximo = 7f;
        /// <summary>Daño por cada m/s del carro y su tope.</summary>
        public const float DanioPorRapidez = 4f;
        public const float DanioMaximo = 100f;

        /// <summary>True si el carro va rápido y el NPC está por delante de su avance.</summary>
        public static bool Arrolla(Vector3 velocidadCarro, Vector3 delCarroAlNpc)
        {
            Vector3 v = new Vector3(velocidadCarro.x, 0f, velocidadCarro.z);
            if (v.magnitude < RapidezMinima)
            {
                return false;
            }
            Vector3 h = new Vector3(delCarroAlNpc.x, 0f, delCarroAlNpc.z);
            return h.sqrMagnitude < 0.0001f || Vector3.Dot(v.normalized, h.normalized) > 0f;
        }

        /// <summary>Velocidad inicial del vuelo del NPC.</summary>
        public static Vector3 VelocidadDeVuelo(Vector3 velocidadCarro)
        {
            Vector3 plana = new Vector3(velocidadCarro.x, 0f, velocidadCarro.z);
            float rapidez = plana.magnitude;
            Vector3 salida = plana * FraccionHorizontal;
            if (salida.magnitude > RapidezHorizontalMaxima)
            {
                salida = salida.normalized * RapidezHorizontalMaxima;
            }
            salida.y = Mathf.Clamp(rapidez * VerticalPorRapidez, VerticalMinimo, VerticalMaximo);
            return salida;
        }

        /// <summary>Daño que recibe el NPC según la rapidez del carro.</summary>
        public static float Danio(Vector3 velocidadCarro)
        {
            float rapidez = new Vector3(velocidadCarro.x, 0f, velocidadCarro.z).magnitude;
            return Mathf.Min(rapidez * DanioPorRapidez, DanioMaximo);
        }
    }
}

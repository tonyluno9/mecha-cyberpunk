using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>Cuentas puras de la cámara del carro (sin MonoBehaviour), para probarlas en EditMode.</summary>
    public static class CalculoCamaraCarro
    {
        /// <summary>Ángulo (°) tras sumar <paramref name="delta"/>, sin pasar de ±<paramref name="limite"/>.</summary>
        public static float LimitarMirada(float actual, float delta, float limite)
        {
            return Mathf.Clamp(actual + delta, -limite, limite);
        }

        /// <summary>
        /// Ángulo (°) tras <paramref name="dt"/> segundos de regreso al frente. Solo regresa si pasaron al menos
        /// <paramref name="espera"/> segundos sin mover el ratón; entonces se acerca a 0 a <paramref name="velocidad"/> °/s.
        /// </summary>
        public static float Regreso(float actual, float segundosSinMover, float espera, float velocidad, float dt)
        {
            if (segundosSinMover < espera)
            {
                return actual;
            }
            return Mathf.MoveTowards(actual, 0f, velocidad * dt);
        }

        /// <summary>Posición ideal de la tercera persona: detrás del foco (según el frente plano) y arriba.</summary>
        public static Vector3 PosicionTercera(Vector3 foco, Vector3 frente, float atras, float arriba)
        {
            frente.y = 0f;
            if (frente.sqrMagnitude < 0.0001f)
            {
                frente = Vector3.forward;
            }
            frente.Normalize();
            return foco - frente * atras + Vector3.up * arriba;
        }

        /// <summary>
        /// Posición de la cámara en órbita: a <paramref name="distancia"/> del foco, girada <paramref name="yaw"/>° a los lados
        /// respecto a detrás del carro y elevada <paramref name="inclinacion"/>° sobre el horizonte.
        /// </summary>
        public static Vector3 PosicionOrbita(Vector3 foco, Vector3 frente, float distancia, float yaw, float inclinacion)
        {
            frente.y = 0f;
            if (frente.sqrMagnitude < 0.0001f)
            {
                frente = Vector3.forward;
            }
            frente.Normalize();
            Vector3 atras = Quaternion.Euler(0f, yaw, 0f) * -frente;
            float radianes = inclinacion * Mathf.Deg2Rad;
            return foco + (atras * Mathf.Cos(radianes) + Vector3.up * Mathf.Sin(radianes)) * distancia;
        }

        /// <summary>Distancia (m) de la órbita: la base más el extra proporcional a la rapidez (km/h), con tope.</summary>
        public static float DistanciaPorRapidez(float baseMetros, float extra, float kmh, float kmhMaximo)
        {
            return baseMetros + extra * Mathf.Clamp01(kmh / Mathf.Max(kmhMaximo, 0.01f));
        }

        /// <summary>
        /// Distancia (m) a la que poner la cámara sobre el rayo foco-ideal: si hay un muro a <paramref name="distanciaMuro"/>,
        /// se queda <paramref name="margen"/> antes (sin bajar de <paramref name="minima"/>); sin muro, toda la distancia.
        /// </summary>
        public static float DistanciaLibre(float distanciaIdeal, float distanciaMuro, float margen, float minima)
        {
            if (distanciaMuro >= distanciaIdeal)
            {
                return distanciaIdeal;
            }
            return Mathf.Min(distanciaIdeal, Mathf.Max(distanciaMuro - margen, minima));
        }
    }
}

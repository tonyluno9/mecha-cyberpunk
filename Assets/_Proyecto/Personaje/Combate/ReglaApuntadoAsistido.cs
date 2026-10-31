using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// Reglas puras del apuntado asistido (T-424): elegir el blanco más cercano al centro de la retícula y cuánto girar la cámara hacia él
    /// en un cuadro. Sin escena: la línea de vista se pasa como función.
    /// </summary>
    public static class ReglaApuntadoAsistido
    {
        /// <summary>
        /// Índice del enemigo con menor ángulo al rayo (origen, adelante) que esté dentro de <paramref name="radioGrados"/>, delante y a
        /// menos de <paramref name="distanciaMaxima"/>, y con línea de vista. -1 si no hay.
        /// </summary>
        public static int ElegirBlanco(Vector3 origen, Vector3 adelante, IReadOnlyList<Vector3> centros, float radioGrados,
            float distanciaMaxima, Func<int, bool> lineaDeVista)
        {
            int mejor = -1;
            float mejorAngulo = radioGrados;
            for (int i = 0; i < centros.Count; i++)
            {
                Vector3 hacia = centros[i] - origen;
                float distancia = hacia.magnitude;
                if (distancia < 0.01f || distancia > distanciaMaxima)
                {
                    continue;
                }
                float angulo = Vector3.Angle(adelante, hacia);
                if (angulo > mejorAngulo || (lineaDeVista != null && !lineaDeVista(i)))
                {
                    continue;
                }
                mejorAngulo = angulo;
                mejor = i;
            }
            return mejor;
        }

        /// <summary>
        /// Giro (°) de este cuadro para un error angular dado: se acerca en forma exponencial (fuerza en 1/s), con tope en °/s.
        /// Es una fracción del error, nunca lo cierra de golpe.
        /// </summary>
        public static float Correccion(float errorGrados, float fuerza, float dt, float maximoGradosPorSegundo)
        {
            float paso = errorGrados * (1f - Mathf.Exp(-Mathf.Max(0f, fuerza) * dt));
            float tope = maximoGradosPorSegundo * dt;
            return Mathf.Clamp(paso, -tope, tope);
        }

        /// <summary>Cuánto de la fuerza queda con el zoom del sniper: 1 sin zoom, <paramref name="factorConZoom"/> con el zoom completo.</summary>
        public static float FactorZoom(float mezclaZoom, float factorConZoom)
        {
            return Mathf.Lerp(1f, factorConZoom, Mathf.Clamp01(mezclaZoom));
        }
    }
}

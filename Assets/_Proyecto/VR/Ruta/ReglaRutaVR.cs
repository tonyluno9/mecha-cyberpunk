using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.VR.Ruta
{
    /// <summary>
    /// VR30: lógica pura de la ruta minimalista al objetivo. Reparte chevrones cada <see cref="MetrosEntrePuntos"/> sobre la ruta (las esquinas del
    /// NavMesh), solo los primeros <see cref="MetrosVisibles"/>, con un desfase que avanza con el tiempo (se mueven suave hacia adelante), y da el
    /// alfa de cada uno según su distancia (aparecen al frente de los pies y se desvanecen al fondo).
    /// </summary>
    public static class ReglaRutaVR
    {
        public const float MetrosEntrePuntos = 2f;
        public const float MetrosVisibles = 25f;
        /// <summary>Primer chevrón a esta distancia de los pies (no debajo del jugador).</summary>
        public const float MetrosInicio = 1.2f;
        /// <summary>A menos de esto (m, en el plano) del objetivo la ruta se apaga.</summary>
        public const float MetrosLlegada = 3f;
        /// <summary>Velocidad (m/s) a la que corren los chevrones hacia adelante.</summary>
        public const float RapidezAvance = 0.8f;
        /// <summary>Metros en que un chevrón aparece al principio de la ruta (alfa 0 a 1).</summary>
        public const float MetrosAparecer = 1f;
        /// <summary>Desde esta fracción de <see cref="MetrosVisibles"/> se empiezan a desvanecer.</summary>
        public const float FraccionDesvanecer = 0.45f;

        public static int MaximoPuntos => Mathf.CeilToInt((MetrosVisibles - MetrosInicio) / MetrosEntrePuntos) + 1;

        /// <summary>True si ya llegó (la ruta se apaga).</summary>
        public static bool Llego(Vector3 jugador, Vector3 objetivo)
        {
            jugador.y = 0f;
            objetivo.y = 0f;
            return Vector3.Distance(jugador, objetivo) < MetrosLlegada;
        }

        /// <summary>Desfase (0 a <see cref="MetrosEntrePuntos"/>) de los chevrones a los <paramref name="segundos"/> dados: crece y vuelve a empezar.</summary>
        public static float Desfase(float segundos) => Mathf.Repeat(segundos * RapidezAvance, MetrosEntrePuntos);

        /// <summary>Alfa (0 a 1) de un chevrón a <paramref name="metros"/> sobre la ruta.</summary>
        public static float Alfa(float metros)
        {
            if (metros < MetrosInicio || metros > MetrosVisibles)
            {
                return 0f;
            }
            float entrada = Mathf.Clamp01((metros - MetrosInicio) / MetrosAparecer);
            float inicioFin = MetrosVisibles * FraccionDesvanecer;
            float salida = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(inicioFin, MetrosVisibles, metros));
            return entrada * salida;
        }

        /// <summary>
        /// Llena <paramref name="puntos"/> (posición, dirección del tramo y metros desde el inicio) recorriendo <paramref name="esquinas"/>
        /// (las primeras <paramref name="cuenta"/>) a partir de <see cref="MetrosInicio"/> + <paramref name="desfase"/>, cada <see cref="MetrosEntrePuntos"/>,
        /// sin pasar de <see cref="MetrosVisibles"/> ni del final de la ruta. Devuelve cuántos puso.
        /// </summary>
        public static int Muestrear(IReadOnlyList<Vector3> esquinas, int cuenta, float desfase, List<PuntoRutaVR> puntos)
        {
            puntos.Clear();
            if (esquinas == null || cuenta < 2)
            {
                return 0;
            }
            cuenta = Mathf.Min(cuenta, esquinas.Count);
            float siguiente = MetrosInicio + Mathf.Repeat(desfase, MetrosEntrePuntos);
            float recorrido = 0f;
            for (int i = 1; i < cuenta && siguiente <= MetrosVisibles; i++)
            {
                Vector3 a = esquinas[i - 1];
                Vector3 b = esquinas[i];
                float largo = Vector3.Distance(a, b);
                if (largo < 0.0001f)
                {
                    continue;
                }
                Vector3 plano = b - a;
                plano.y = 0f;
                Vector3 dir = plano.sqrMagnitude > 0.0001f ? plano.normalized : Vector3.forward;
                while (siguiente <= recorrido + largo && siguiente <= MetrosVisibles)
                {
                    float t = (siguiente - recorrido) / largo;
                    puntos.Add(new PuntoRutaVR(Vector3.Lerp(a, b, t), dir, siguiente));
                    siguiente += MetrosEntrePuntos;
                }
                recorrido += largo;
            }
            return puntos.Count;
        }
    }
}

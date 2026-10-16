using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Mecha.Llamado
{
    /// <summary>Lógica pura del llamado: decide qué pasa cuando el jugador pulsa M.</summary>
    public static class ReglaLlamado
    {
        /// <summary>
        /// Decide el resultado. Alguien dentro del mecha manda sobre todo (Ocupado); si no, el estado: Llegando es YaViene,
        /// Regresando y EnArena son Nada, EnCalle es Regresa y EnTaller busca la zona. <paramref name="indice"/> es el punto elegido
        /// cuando el resultado es Llega, y -1 en los demás casos.
        /// </summary>
        public static ResultadoLlamado Decidir(Vector3 posicionJugador, IReadOnlyList<PuntoLlamado> puntos, EstadoLlamado estado,
            bool ocupado, out int indice)
        {
            indice = -1;
            if (ocupado)
            {
                return ResultadoLlamado.Ocupado;
            }
            switch (estado)
            {
                case EstadoLlamado.Llegando:
                    return ResultadoLlamado.YaViene;
                case EstadoLlamado.Regresando:
                case EstadoLlamado.EnArena:
                    return ResultadoLlamado.Nada;
                case EstadoLlamado.EnCalle:
                    return ResultadoLlamado.Regresa;
            }

            // La zona se mide en horizontal; gana la llegada más cercana al jugador entre los puntos habilitados que lo contienen.
            float mejor = float.MaxValue;
            if (puntos != null)
            {
                for (int i = 0; i < puntos.Count; i++)
                {
                    PuntoLlamado punto = puntos[i];
                    if (!punto.Habilitado)
                    {
                        continue;
                    }
                    float distancia = DistanciaHorizontal(posicionJugador, punto.Llegada.position);
                    if (distancia <= punto.Radio && distancia < mejor)
                    {
                        mejor = distancia;
                        indice = i;
                    }
                }
            }
            return indice >= 0 ? ResultadoLlamado.Llega : ResultadoLlamado.ZonaProhibida;
        }

        /// <summary>Distancia entre dos posiciones sin contar la altura (m).</summary>
        public static float DistanciaHorizontal(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}

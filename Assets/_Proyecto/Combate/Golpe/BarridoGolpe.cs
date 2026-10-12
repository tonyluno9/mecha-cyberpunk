using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Combate.Golpe
{
    /// <summary>
    /// El barrido de un golpe: una esfera hacia adelante que ignora al propio mecha (su <c>transform.root</c>) y devuelve
    /// al <see cref="IRecibeDanio"/> más cercano. Lo usan <see cref="GolpeMecha"/> y el rival.
    /// </summary>
    public static class BarridoGolpe
    {
        /// <summary>
        /// Busca al primer <see cref="IRecibeDanio"/> que no sea del propio mecha. El alcance se mide desde el origen
        /// hasta el borde de la esfera. Devuelve null si no hay nadie.
        /// </summary>
        public static IRecibeDanio Buscar(Transform propio, Vector3 origen, Vector3 direccion, float alcance,
            float radio, RaycastHit[] buffer, out Vector3 punto)
        {
            float distancia = Mathf.Max(0f, alcance - radio);
            int cantidad = Physics.SphereCastNonAlloc(origen, radio, direccion, buffer, distancia,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            float mejorDistancia = float.MaxValue;
            IRecibeDanio objetivo = null;
            punto = origen;
            for (int i = 0; i < cantidad; i++)
            {
                RaycastHit golpe = buffer[i];
                if (golpe.collider.transform.root == propio.root || golpe.distance >= mejorDistancia)
                {
                    continue;
                }
                IRecibeDanio candidato = golpe.collider.GetComponentInParent<IRecibeDanio>();
                if (candidato != null)
                {
                    mejorDistancia = golpe.distance;
                    objetivo = candidato;
                    punto = golpe.point;
                }
            }
            return objetivo;
        }
    }
}

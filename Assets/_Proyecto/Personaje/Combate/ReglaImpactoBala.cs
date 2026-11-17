using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>T-1324: busca el primer collider que para una bala (hitscan), sin buffer limitado y saltando al propio jugador y a su carro.</summary>
    public static class ReglaImpactoBala
    {
        private const int MaxSaltos = 24;
        private const float Avance = 0.02f;

        /// <summary>El primer collider no trigger del rayo que no sea de <paramref name="raizPropia"/> ni de <paramref name="raizExtra"/> (puede ser null).</summary>
        public static bool BuscarPrimero(Vector3 origen, Vector3 direccion, float alcance, Transform raizPropia, Transform raizExtra, out RaycastHit mejor)
        {
            mejor = default;
            float recorrido = 0f;
            Vector3 desde = origen;
            for (int i = 0; i < MaxSaltos && recorrido < alcance; i++)
            {
                if (!Physics.Raycast(desde, direccion, out RaycastHit impacto, alcance - recorrido, ~0, QueryTriggerInteraction.Ignore))
                {
                    return false;
                }
                Transform raiz = impacto.collider.transform.root;
                if (raiz != raizPropia && (raizExtra == null || raiz != raizExtra))
                {
                    impacto.distance += recorrido;
                    mejor = impacto;
                    return true;
                }
                float paso = impacto.distance + Avance;
                recorrido += paso;
                desde += direccion * paso;
            }
            return false;
        }
    }
}

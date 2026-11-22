using UnityEngine;

namespace MechaCyberpunk.IA.Barrio
{
    /// <summary>T-1643 (T23): mueve un transform a mano (sin CharacterController) solo si el sitio de llegada está libre de colliders sólidos; si no, lo acorta o no lo mueve.</summary>
    public static class DesplazamientoSeguro
    {
        private const float Radio = 0.25f;
        private const float Altura = 1.7f;
        private const float AlturaPaso = 0.35f;
        private const int Intentos = 4;
        private static readonly Collider[] Buffer = new Collider[32];

        /// <summary>Mueve <paramref name="objeto"/> <paramref name="distancia"/> m hacia <paramref name="direccion"/> (plano), acortándolo a la mitad hasta 4 veces si el destino está ocupado. Devuelve los metros que avanzó.</summary>
        public static float Mover(Transform objeto, Vector3 direccion, float distancia)
        {
            direccion.y = 0f;
            if (objeto == null || direccion.sqrMagnitude < 1e-6f)
            {
                return 0f;
            }
            direccion.Normalize();
            float d = distancia;
            for (int i = 0; i < Intentos; i++)
            {
                if (Libre(objeto.position + direccion * d, objeto))
                {
                    objeto.position += direccion * d;
                    return d;
                }
                d *= 0.5f;
            }
            return 0f;
        }

        /// <summary>True si la cápsula de un humano con los pies en <paramref name="pie"/> no toca nada sólido (ignora triggers, al propio objeto y a otros CharacterController).</summary>
        public static bool Libre(Vector3 pie, Transform propio)
        {
            int n = Physics.OverlapCapsuleNonAlloc(pie + Vector3.up * (AlturaPaso + Radio), pie + Vector3.up * (Altura - Radio), Radio, Buffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = Buffer[i];
                if (c != null && !c.isTrigger && !c.transform.IsChildOf(propio) && c.GetComponentInParent<CharacterController>() == null)
                {
                    return false;
                }
            }
            return true;
        }
    }
}

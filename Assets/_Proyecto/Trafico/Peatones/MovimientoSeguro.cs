using UnityEngine;

namespace MechaCyberpunk.Trafico.Peatones
{
    /// <summary>
    /// T-1643 (T23): pregunta barata de "¿cabe un peatón aquí?" contra los colliders sólidos del mundo (paredes, puestos, carros, rejas). La cápsula
    /// empieza <see cref="AlturaPaso"/> m sobre los pies, así que el piso y los bordillos no cuentan; ignora triggers, otros peatones y personajes con
    /// CharacterController. Sin asignaciones por llamada (buffer compartido).
    /// </summary>
    public static class MovimientoSeguro
    {
        /// <summary>Radio del cuerpo (m).</summary>
        public const float Radio = 0.25f;
        /// <summary>Altura de la cabeza sobre los pies (m).</summary>
        public const float Altura = 1.7f;
        /// <summary>Lo que se puede pisar sin que cuente como obstáculo (m).</summary>
        public const float AlturaPaso = 0.35f;
        private const int TamanoBuffer = 32;
        private static readonly Collider[] Buffer = new Collider[TamanoBuffer];

        /// <summary>True si la cápsula de un peatón con los pies en <paramref name="pie"/> no toca nada sólido; <paramref name="propio"/> se ignora con sus hijos.</summary>
        public static bool Libre(Vector3 pie, Transform propio)
        {
            Vector3 abajo = pie + Vector3.up * (AlturaPaso + Radio);
            Vector3 arriba = pie + Vector3.up * (Altura - Radio);
            int n = Physics.OverlapCapsuleNonAlloc(abajo, arriba, Radio, Buffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (EsObstaculo(Buffer[i], propio))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool EsObstaculo(Collider c, Transform propio)
        {
            if (c == null || c.isTrigger)
            {
                return false;
            }
            if (propio != null && c.transform.IsChildOf(propio))
            {
                return false;
            }
            return c.GetComponentInParent<PeatonMundo>() == null && c.GetComponentInParent<CharacterController>() == null;
        }
    }
}

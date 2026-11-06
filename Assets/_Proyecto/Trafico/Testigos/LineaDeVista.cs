using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Peatones;
using MechaCyberpunk.Trafico.Persecucion;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Testigos
{
    /// <summary>
    /// Línea de vista de los testigos (T-661): nada sólido entre un ojo y el jugador. No cuentan los triggers, los peatones, las patrullas ni el propio
    /// jugador, y se queda <see cref="MargenAlObjetivo"/> m antes de él (si va en un carro, el carro no lo tapa).
    /// </summary>
    public static class LineaDeVista
    {
        public const float MargenAlObjetivo = 3f;
        private const float AlturaObjetivo = 1.2f;
        private const float LargoMinimo = 0.5f;
        private const int Maximo = 24;
        private static readonly RaycastHit[] impactos = new RaycastHit[Maximo];

        public static bool Libre(Vector3 ojo, Vector3 objetivo)
        {
            objetivo.y += AlturaObjetivo;
            Vector3 hacia = objetivo - ojo;
            float largo = hacia.magnitude - MargenAlObjetivo;
            if (largo <= LargoMinimo)
            {
                return true;
            }
            Vector3 direccion = hacia.normalized;
            IJugador datos = Registro<IJugador>.Actual;
            Transform jugador = datos != null ? datos.Raiz : null;
            int cuantos = Physics.RaycastNonAlloc(ojo, direccion, impactos, largo, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < cuantos; i++)
            {
                Collider c = impactos[i].collider;
                if (c.GetComponentInParent<PeatonMundo>() != null || c.GetComponentInParent<PatrullaMundo>() != null
                    || (jugador != null && c.transform.IsChildOf(jugador)))
                {
                    continue;
                }
                return false;
            }
            return true;
        }
    }
}

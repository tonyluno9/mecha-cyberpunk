using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// T-986: las patrullas manejan con física, así que un cierre, una barda o un reten en la calle las detiene. Revisa cada tramo del grafo con dos
    /// barridos de esfera (uno por carril) y cierra los que tienen algo sólido en los dos carriles; un estorbo en uno solo se rodea y no cuenta.
    /// </summary>
    public static class BloqueosDeCalles
    {
        private const float AlturaDelBarrido = 1.2f;
        private const float RadioDelBarrido = 0.7f;
        private const float DesvioDeCarril = 1.6f;
        private const float MargenAlNodo = 1f;
        private const int MaximoDeImpactos = 16;
        private static readonly RaycastHit[] impactos = new RaycastHit[MaximoDeImpactos];

        /// <summary>Cierra en <paramref name="grafo"/> los tramos bloqueados. Devuelve cuántos cerró.</summary>
        public static int Marcar(GrafoCalles grafo)
        {
            Physics.SyncTransforms(); // los colliders recién creados (un barrio recién puesto) cuentan
            int cerrados = 0;
            for (int a = 0; a < grafo.Cantidad; a++)
            {
                foreach (int b in grafo.Vecinos(a))
                {
                    if (b > a && grafo.TramoLibre(a, b) && Bloqueado(grafo.Nodo(a), grafo.Nodo(b)))
                    {
                        grafo.BloquearTramo(a, b);
                        cerrados++;
                    }
                }
            }
            return cerrados;
        }

        /// <summary>True si algo sólido (que no sea un cuerpo suelto, como otro carro) cierra los dos carriles entre los dos puntos.</summary>
        public static bool Bloqueado(Vector3 desde, Vector3 hasta)
        {
            Vector3 tramo = hasta - desde;
            tramo.y = 0f;
            float largo = tramo.magnitude;
            if (largo <= MargenAlNodo * 2f)
            {
                return false;
            }
            Vector3 dir = tramo / largo;
            Vector3 derecha = Vector3.Cross(Vector3.up, dir);
            return Cerrado(desde, dir, derecha * DesvioDeCarril, largo) && Cerrado(desde, dir, -derecha * DesvioDeCarril, largo);
        }

        private static bool Cerrado(Vector3 desde, Vector3 dir, Vector3 lado, float largo)
        {
            Vector3 origen = desde + dir * MargenAlNodo + lado + Vector3.up * AlturaDelBarrido;
            int n = Physics.SphereCastNonAlloc(origen, RadioDelBarrido, dir, impactos, largo - MargenAlNodo * 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = impactos[i].collider;
                if (c != null && c.attachedRigidbody == null && c.GetComponentInParent<Peatones.PeatonMundo>() == null)
                {
                    return true;
                }
            }
            return false;
        }
    }
}

using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// T-986: las patrullas manejan con física, así que una barda o un cierre de lado a lado las detiene. Revisa cada tramo del grafo con barridos de esfera por
    /// cuatro carriles (los dos de la calle y los dos de más afuera, que la patrulla también usa para rodear) y cierra el tramo solo si en una misma franja de
    /// pocos metros hay algo sólido en los cuatro. Un puesto, un poste o un letrero en un carril se rodea; el piso, lo que queda bajo la suspensión y lo que cuelga
    /// por encima del techo del carro no cuentan.
    /// </summary>
    public static class BloqueosDeCalles
    {
        private const float AlturaDelBarrido = 1.2f;
        private const float RadioDelBarrido = 0.7f;
        private const float DesvioDeCarril = 1.6f;
        private const float DesvioExterior = 4.4f;
        private const float MargenAlNodo = 1f;
        private const float VentanaDeCierre = 3f;
        private const float AlturaLibreBajo = 0.5f;
        private const float AlturaLibreSobre = 1.9f;
        private const float NormalDePiso = 0.7f;
        private const int Carriles = 4;
        private const int MaximoDeImpactos = 48;
        private static readonly float[] Desvios = { DesvioDeCarril, -DesvioDeCarril, DesvioExterior, -DesvioExterior };
        private static readonly RaycastHit[] impactos = new RaycastHit[MaximoDeImpactos];
        private static readonly float[][] distancias = { new float[MaximoDeImpactos], new float[MaximoDeImpactos], new float[MaximoDeImpactos], new float[MaximoDeImpactos] };
        private static readonly Collider[][] colliders = { new Collider[MaximoDeImpactos], new Collider[MaximoDeImpactos], new Collider[MaximoDeImpactos], new Collider[MaximoDeImpactos] };
        private static readonly int[] cuantos = new int[Carriles];

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

        /// <summary>True si algo sólido (que no sea un cuerpo suelto, como otro carro) cierra la calle de lado a lado en una franja corta entre los dos puntos.</summary>
        public static bool Bloqueado(Vector3 desde, Vector3 hasta)
        {
            return Bloqueo(desde, hasta, out _, out _);
        }

        /// <summary>Como <see cref="Bloqueado"/>, y dice qué collider cierra el carril derecho y a qué distancia (m) del primer punto.</summary>
        public static bool Bloqueo(Vector3 desde, Vector3 hasta, out Collider cierre, out float distancia)
        {
            cierre = null;
            distancia = 0f;
            Vector3 tramo = hasta - desde;
            tramo.y = 0f;
            float largo = tramo.magnitude;
            if (largo <= MargenAlNodo * 2f)
            {
                return false;
            }
            Vector3 dir = tramo / largo;
            Vector3 derecha = Vector3.Cross(Vector3.up, dir);
            for (int c = 0; c < Carriles; c++)
            {
                cuantos[c] = Barrer(desde, dir, derecha * Desvios[c], largo, distancias[c], colliders[c]);
                if (cuantos[c] == 0)
                {
                    return false; // un carril libre de punta a punta: se puede pasar
                }
            }
            for (int i = 0; i < cuantos[0]; i++)
            {
                float d = distancias[0][i];
                bool todos = true;
                for (int c = 1; c < Carriles && todos; c++)
                {
                    bool hay = false;
                    for (int j = 0; j < cuantos[c] && !hay; j++)
                    {
                        hay = Mathf.Abs(distancias[c][j] - d) <= VentanaDeCierre;
                    }
                    todos = hay;
                }
                if (todos)
                {
                    cierre = colliders[0][i];
                    distancia = d + MargenAlNodo;
                    return true;
                }
            }
            return false;
        }

        // Los sólidos fijos que el carro no puede cruzar en ese carril, con su distancia desde el inicio del barrido.
        private static int Barrer(Vector3 desde, Vector3 dir, Vector3 lado, float largo, float[] salidaDistancias, Collider[] salidaColliders)
        {
            Vector3 origen = desde + dir * MargenAlNodo + lado + Vector3.up * AlturaDelBarrido;
            int n = Physics.SphereCastNonAlloc(origen, RadioDelBarrido, dir, impactos, largo - MargenAlNodo * 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            int guardados = 0;
            for (int i = 0; i < n; i++)
            {
                Collider c = impactos[i].collider;
                if (c == null || c.attachedRigidbody != null || c.GetComponentInParent<Peatones.PeatonMundo>() != null || impactos[i].normal.y > NormalDePiso)
                {
                    continue;
                }
                Bounds b = c.bounds;
                if (b.max.y < desde.y + AlturaLibreBajo || b.min.y > desde.y + AlturaLibreSobre)
                {
                    continue; // bajo la suspensión o por encima del techo del carro
                }
                salidaDistancias[guardados] = impactos[i].distance;
                salidaColliders[guardados] = c;
                guardados++;
            }
            return guardados;
        }

        /// <summary>El nombre del collider con su jerarquía (para diagnosticar qué cierra un tramo).</summary>
        public static string Ruta(Collider c)
        {
            if (c == null)
            {
                return "(nada)";
            }
            string ruta = c.name;
            Transform t = c.transform.parent;
            while (t != null)
            {
                ruta = t.name + "/" + ruta;
                t = t.parent;
            }
            return ruta;
        }
    }
}

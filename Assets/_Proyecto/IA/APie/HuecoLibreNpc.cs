using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T40 (punto 4): busca un hueco en el piso donde quepa un humano sin quedar encimado de otro cuerpo (NPC de pie, tirado, pandillero caído, robot) ni dentro de una pared.
    /// Los refuerzos de Nova que aparecen o caen lo usan para correrse a un lado en vez de aterrizar sobre un cuerpo.
    /// </summary>
    public static class HuecoLibreNpc
    {
        /// <summary>Radio del cuerpo que se reserva (m).</summary>
        public const float Radio = 0.55f;
        /// <summary>Altura que se revisa (m).</summary>
        public const float Altura = 1.9f;
        /// <summary>Distancia entre intentos en cada anillo (m).</summary>
        public const float Paso = 1.0f;
        public const int Anillos = 4;
        public const int DireccionesPorAnillo = 8;
        private const float AlturaPaso = 0.3f;
        private const float DistanciaCuerpoTirado = 1.3f;
        private const float AlcancePiso = 2.5f;
        private const int TamanoBuffer = 32;
        private static readonly Collider[] Buffer = new Collider[TamanoBuffer];

        /// <summary>Número de veces que se tuvo que correr un punto (para depurar).</summary>
        public static int Desplazados { get; private set; }

        /// <summary>
        /// Devuelve <paramref name="punto"/> (con los pies) si está libre; si no, el punto libre más cercano en anillos de <see cref="Paso"/> m alrededor.
        /// Si ninguno sirve devuelve <paramref name="punto"/> sin cambio.
        /// </summary>
        public static Vector3 Buscar(Vector3 punto, Transform ignorar = null)
        {
            if (Libre(punto, ignorar))
            {
                return punto;
            }
            for (int anillo = 1; anillo <= Anillos; anillo++)
            {
                for (int k = 0; k < DireccionesPorAnillo; k++)
                {
                    float a = (k + anillo * 0.5f) * Mathf.PI * 2f / DireccionesPorAnillo;
                    Vector3 candidato = punto + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (Paso * anillo);
                    if (!PonerEnPiso(punto, ref candidato, ignorar))
                    {
                        continue;
                    }
                    if (Libre(candidato, ignorar))
                    {
                        Desplazados++;
                        return candidato;
                    }
                }
            }
            return punto;
        }

        /// <summary>True si en <paramref name="pie"/> cabe un humano: sin colliders (de personajes o del mundo) y sin un cuerpo tirado a menos de 1.3 m.</summary>
        public static bool Libre(Vector3 pie, Transform ignorar = null)
        {
            Vector3 abajo = pie + Vector3.up * (AlturaPaso + Radio);
            Vector3 arriba = pie + Vector3.up * Mathf.Max(AlturaPaso + Radio, Altura - Radio);
            int n = Physics.OverlapCapsuleNonAlloc(abajo, arriba, Radio, Buffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = Buffer[i];
                if (c != null && !c.isTrigger && (ignorar == null || !c.transform.IsChildOf(ignorar)))
                {
                    return false;
                }
            }
            var cuerpos = PandilleroAPie.CuerposTirados;
            for (int i = 0; i < cuerpos.Count; i++)
            {
                PandilleroAPie p = cuerpos[i];
                if (p == null || (ignorar != null && p.transform.IsChildOf(ignorar)))
                {
                    continue;
                }
                Vector3 d = p.transform.position - pie;
                d.y = 0f;
                if (d.sqrMagnitude < DistanciaCuerpoTirado * DistanciaCuerpoTirado && Mathf.Abs(p.transform.position.y - pie.y) < 2f)
                {
                    return false;
                }
            }
            return true;
        }

        // Baja el candidato al piso que tenga debajo (el del origen más/menos AlcancePiso); false si no hay piso o es una pared.
        private static bool PonerEnPiso(Vector3 origen, ref Vector3 candidato, Transform ignorar)
        {
            Vector3 desde = candidato + Vector3.up * AlcancePiso;
            if (!Physics.Raycast(desde, Vector3.down, out RaycastHit golpe, AlcancePiso * 2f, ~0, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            if (golpe.normal.y < 0.7f || Mathf.Abs(golpe.point.y - origen.y) > AlcancePiso || (ignorar != null && golpe.collider.transform.IsChildOf(ignorar)))
            {
                return false;
            }
            candidato = golpe.point; // pies sobre el piso encontrado
            return true;
        }

        /// <summary>Depuración: cuántos puntos se han corrido por estar ocupados.</summary>
        public static string DepurarHuecos() => "HuecoLibreNpc: " + Desplazados + " puntos corridos por haber un cuerpo encima";
    }
}

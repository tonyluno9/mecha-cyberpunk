using System.Collections.Generic;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>
    /// T-1535: qué fuentes de sonido del mundo (interiores, transformadores, aires, cámaras...) deben sonar ahora. Pura, sin Unity.
    /// Cada candidato trae su distancia al jugador y el radio en que se oye; entre los que están dentro del radio se toman los más cercanos,
    /// con un tope por grupo (para no llenar las 12 voces del gestor) y un tope por ID (el gestor deja pocos bucles del mismo ID).
    /// </summary>
    public static class ReglaFuentesMundo
    {
        /// <summary>Un lugar del mundo que puede sonar.</summary>
        public struct Candidato
        {
            public int Grupo;
            public int Id;
            public float Distancia;
            public float Radio;
        }

        /// <summary>
        /// Marca en <paramref name="activos"/> (mismo largo que <paramref name="candidatos"/>) los que suenan: dentro de su radio, de menor a mayor distancia,
        /// hasta el tope de su grupo (<paramref name="topePorGrupo"/>, por índice de grupo) y el tope de su ID (<paramref name="topePorId"/>).
        /// Devuelve cuántos quedaron activos.
        /// </summary>
        public static int Elegir(IReadOnlyList<Candidato> candidatos, IReadOnlyList<int> topePorGrupo, int topePorId, bool[] activos)
        {
            int total = 0;
            List<int> orden = new List<int>();
            for (int i = 0; i < candidatos.Count; i++)
            {
                activos[i] = false;
                if (candidatos[i].Distancia <= candidatos[i].Radio) orden.Add(i);
            }
            orden.Sort((a, b) =>
            {
                int c = candidatos[a].Distancia.CompareTo(candidatos[b].Distancia);
                return c != 0 ? c : a.CompareTo(b);
            });
            Dictionary<int, int> porGrupo = new Dictionary<int, int>();
            Dictionary<int, int> porId = new Dictionary<int, int>();
            foreach (int i in orden)
            {
                Candidato c = candidatos[i];
                porGrupo.TryGetValue(c.Grupo, out int g);
                porId.TryGetValue(c.Id, out int n);
                int tope = c.Grupo >= 0 && c.Grupo < topePorGrupo.Count ? topePorGrupo[c.Grupo] : 0;
                if (g >= tope || n >= topePorId) continue;
                porGrupo[c.Grupo] = g + 1;
                porId[c.Id] = n + 1;
                activos[i] = true;
                total++;
            }
            return total;
        }

        /// <summary>Segundos al azar entre <paramref name="minimo"/> y <paramref name="maximo"/> (<paramref name="azar"/> en [0, 1)): lo que falta para el próximo golpe de un sonido que no es bucle.</summary>
        public static float SegundosHastaOtro(float minimo, float maximo, float azar)
        {
            if (maximo < minimo) maximo = minimo;
            return minimo + (maximo - minimo) * azar;
        }
    }
}

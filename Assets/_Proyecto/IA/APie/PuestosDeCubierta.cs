using System.Collections.Generic;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-711: orden en que un tirador prueba los puestos a lo largo de una cubierta (desvíos laterales, en metros, desde el centro). Un policía prueba primero
    /// las dos esquinas (la de su lado primero) y luego el centro; los demás, el centro y luego a los lados de <see cref="Separacion"/> en <see cref="Separacion"/>.
    /// Pura, sin Unity.
    /// </summary>
    public static class PuestosDeCubierta
    {
        /// <summary>Separación mínima (m) entre dos puestos de la misma cubierta.</summary>
        public const float Separacion = 1.3f;

        /// <param name="tope">Hasta dónde se puede desviar hacia cada lado (la media longitud de la cubierta menos el borde).</param>
        /// <param name="haciaMi">+1 o -1: el lado donde ya está el tirador (ese lado se prueba primero).</param>
        public static void Ordenar(List<float> desvios, float tope, float haciaMi, bool esquinas)
        {
            desvios.Clear();
            tope = tope < 0f ? 0f : tope;
            haciaMi = haciaMi < 0f ? -1f : 1f;
            if (esquinas && tope > 0.1f)
            {
                desvios.Add(tope * haciaMi);
                desvios.Add(-tope * haciaMi);
            }
            desvios.Add(0f);
            for (float o = Separacion; o <= tope; o += Separacion)
            {
                desvios.Add(o * haciaMi);
                desvios.Add(-o * haciaMi);
            }
        }

        /// <summary>True si <paramref name="punto"/> queda a menos de <see cref="Separacion"/> de algún puesto ya tomado (en el plano).</summary>
        public static bool Ocupado(float x, float z, IReadOnlyList<(float x, float z)> tomados)
        {
            for (int i = 0; i < tomados.Count; i++)
            {
                float dx = tomados[i].x - x;
                float dz = tomados[i].z - z;
                if (dx * dx + dz * dz < Separacion * Separacion)
                {
                    return true;
                }
            }
            return false;
        }
    }
}

using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>T-1498: reglas puras de la nave de Nova que baja soldados: dónde flota, cuántos baja y cómo bajan por la cuerda.</summary>
    public static class ReglaNaveNova
    {
        /// <summary>
        /// Altura (m sobre el piso) a la que flota la nave, o -1 si el punto no sirve. <paramref name="libreArriba"/> es lo que hay libre sobre el piso
        /// (infinito si no hay techo); la nave necesita <paramref name="deseada"/> más <paramref name="margen"/> de aire, o al menos <paramref name="minima"/> más margen.
        /// </summary>
        public static float AlturaDeFlote(float libreArriba, float deseada, float minima, float margen)
        {
            float disponible = libreArriba - margen;
            if (disponible < minima)
            {
                return -1f;
            }
            return Mathf.Min(deseada, disponible);
        }

        /// <summary>Cuántos soldados baja la nave: el mínimo con 4 estrellas, el máximo con 5 o más; 0 bajo 4.</summary>
        public static int Soldados(int estrellas, int conCuatro, int conCinco)
        {
            if (estrellas < 4)
            {
                return 0;
            }
            return Mathf.Max(0, estrellas >= 5 ? conCinco : conCuatro);
        }

        /// <summary>Posición de quien baja por la cuerda tras <paramref name="segundos"/>: lineal de <paramref name="desde"/> a <paramref name="hasta"/> a <paramref name="velocidad"/> m/s.</summary>
        public static Vector3 PosicionDescenso(Vector3 desde, Vector3 hasta, float segundos, float velocidad)
        {
            float largo = Vector3.Distance(desde, hasta);
            if (largo < 0.0001f || velocidad <= 0f)
            {
                return hasta;
            }
            return Vector3.Lerp(desde, hasta, Mathf.Clamp01(segundos * velocidad / largo));
        }

        /// <summary>Segundos que tarda en bajar <paramref name="altura"/> metros a <paramref name="velocidad"/> m/s.</summary>
        public static float SegundosDescenso(float altura, float velocidad) => velocidad <= 0f ? 0f : Mathf.Max(0f, altura) / velocidad;

        /// <summary>Punto de partida de la nave: lejos del lugar donde flota, en sentido contrario a <paramref name="haciaDonde"/> (rumbo plano), y más alto.</summary>
        public static Vector3 PuntoLejano(Vector3 flote, Vector3 rumboPlano, float distancia, float alturaExtra)
        {
            rumboPlano.y = 0f;
            Vector3 dir = rumboPlano.sqrMagnitude > 0.0001f ? rumboPlano.normalized : Vector3.forward;
            return flote + dir * distancia + Vector3.up * alturaExtra;
        }
    }
}

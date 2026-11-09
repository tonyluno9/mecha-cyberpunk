using UnityEngine;

namespace MechaCyberpunk.UI
{
    /// <summary>T-942: a qué pieza pertenece un triángulo de un arma de una sola malla.</summary>
    public enum ParteMalla
    {
        Resto = 0,
        Canon = 1,
        Cargador = 2
    }

    /// <summary>
    /// T-942: reparte los triángulos de una malla de arma en Cargador, Canon y resto según su posición. Cálculo puro (sin MonoBehaviour).
    /// El largo del arma es el eje horizontal más largo y el frente es el lado de la boca. Canon: el tramo del frente hacia la boca.
    /// Cargador: lo que cuelga por debajo de la zona central del arma, en el tramo medio.
    /// </summary>
    public static class CalculoPiezasArma
    {
        /// <summary>Desde qué fracción del largo (0 atrás, 1 la boca) empieza el cañón.</summary>
        public const float CanonDesde = 0.62f;
        /// <summary>Tramo del largo donde puede estar el cargador.</summary>
        public const float CargadorDesde = 0.3f;
        public const float CargadorHasta = 0.6f;
        /// <summary>Fracción de la altura, contada desde abajo, que cuenta como colgando.</summary>
        public const float CargadorAltura = 0.4f;

        /// <summary>Una parte por triángulo. Con una malla sin triángulos devuelve un arreglo vacío.</summary>
        public static ParteMalla[] Repartir(Vector3[] vertices, int[] triangulos, Vector3 boca)
        {
            int cantidad = triangulos.Length / 3;
            ParteMalla[] partes = new ParteMalla[cantidad];
            if (cantidad == 0 || vertices.Length == 0)
            {
                return partes;
            }
            Bounds b = new Bounds(vertices[0], Vector3.zero);
            for (int i = 1; i < vertices.Length; i++)
            {
                b.Encapsulate(vertices[i]);
            }
            bool largoEnX = b.size.x >= b.size.z;
            float centroLargo = largoEnX ? b.center.x : b.center.z;
            float bocaLargo = largoEnX ? boca.x : boca.z;
            float signo = bocaLargo >= centroLargo ? 1f : -1f;
            float largo = largoEnX ? b.size.x : b.size.z;
            float inicio = signo > 0f ? (largoEnX ? b.min.x : b.min.z) : (largoEnX ? b.max.x : b.max.z);
            float limiteAbajo = b.min.y + b.size.y * CargadorAltura;
            for (int t = 0; t < cantidad; t++)
            {
                Vector3 c = (vertices[triangulos[t * 3]] + vertices[triangulos[t * 3 + 1]] + vertices[triangulos[t * 3 + 2]]) / 3f;
                float s = largo > 0f ? ((largoEnX ? c.x : c.z) - inicio) * signo / largo : 0f;
                if (s >= CanonDesde)
                {
                    partes[t] = ParteMalla.Canon;
                }
                else if (c.y < limiteAbajo && s >= CargadorDesde && s <= CargadorHasta)
                {
                    partes[t] = ParteMalla.Cargador;
                }
            }
            return partes;
        }
    }
}

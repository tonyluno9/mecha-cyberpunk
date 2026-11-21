using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1541: cuentas puras (sin escena) de la entrada del mercado de armas: ancho del callejón medido con rayos, escala del portón EM01
    /// y las dos patas con collider que dejan libre el paso bajo el arco.
    /// </summary>
    public static class CalculoEntradaMercadoT1541
    {
        /// <summary>Lo que el portón se mete en cada muro para no dejar rendija (m, en total).</summary>
        public const float MargenMuros = 0.4f;
        public const float EscalaMinima = 0.5f;
        public const float EscalaMaxima = 1.3f;
        /// <summary>Cada pata del portón (collider) ocupa esta fracción del largo del arco.</summary>
        public const float FraccionPata = 0.16f;

        /// <summary>Mediana de las distancias medidas (las que no son positivas no cuentan); <paramref name="porOmision"/> si no hay ninguna.</summary>
        public static float Mediana(IEnumerable<float> distancias, float porOmision)
        {
            float[] validas = distancias.Where(d => d > 0f).OrderBy(d => d).ToArray();
            if (validas.Length == 0)
            {
                return porOmision;
            }
            int medio = validas.Length / 2;
            return validas.Length % 2 == 1 ? validas[medio] : (validas[medio - 1] + validas[medio]) * 0.5f;
        }

        /// <summary>Escala uniforme para que un portón de <paramref name="largoPrefab"/> m cubra el callejón de <paramref name="anchoCalle"/> m (con el margen en los muros).</summary>
        public static float EscalaPorton(float anchoCalle, float largoPrefab)
        {
            if (largoPrefab <= 0.0001f)
            {
                throw new ArgumentOutOfRangeException(nameof(largoPrefab));
            }
            return Mathf.Clamp((anchoCalle + MargenMuros) / largoPrefab, EscalaMinima, EscalaMaxima);
        }

        /// <summary>Centro y tamaño (en el eje del arco, <paramref name="largo"/>, y en el otro, <paramref name="fondo"/>) de las dos patas, respecto al centro de los límites.</summary>
        public static (Vector2 centroA, Vector2 centroB, Vector2 tamano) Patas(float largo, float fondo)
        {
            float ancho = largo * FraccionPata;
            float desplazamiento = largo * 0.5f - ancho * 0.5f;
            return (new Vector2(-desplazamiento, 0f), new Vector2(desplazamiento, 0f), new Vector2(ancho, fondo));
        }

        /// <summary>Abertura libre entre las patas (m) de un portón de <paramref name="largo"/> m.</summary>
        public static float AberturaLibre(float largo) => largo * (1f - 2f * FraccionPata);

        /// <summary>Si dos cajas del suelo (x, z: centro y mitades) se traslapan.</summary>
        public static bool Traslapan(Vector2 centroA, Vector2 mitadA, Vector2 centroB, Vector2 mitadB)
        {
            return Mathf.Abs(centroA.x - centroB.x) < mitadA.x + mitadB.x && Mathf.Abs(centroA.y - centroB.y) < mitadA.y + mitadB.y;
        }
    }
}

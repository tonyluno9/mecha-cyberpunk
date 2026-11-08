using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.UI.Mapa
{
    /// <summary>
    /// Anti-encimado de los nombres del mapa grande (T-721), sin Unity vivo: a cada icono se le busca un lugar para su nombre (debajo, arriba, a la
    /// derecha, a la izquierda y más lejos) donde no tape a otro nombre, a otro icono ni a un rectángulo fijo (los nombres de los barrios). Si ninguno
    /// queda libre se escoge el que tapa menos. Todo en píxeles de pantalla.
    /// </summary>
    public static class CalculoEtiquetasMapa
    {
        private const float Holgura = 3f;

        /// <summary>
        /// Centro de cada nombre relativo a su icono (px). <paramref name="iconos"/> son los centros, <paramref name="tamanos"/> lo que mide cada nombre,
        /// <paramref name="arriba"/> si prefiere ir arriba, <paramref name="visibles"/> cuáles se dibujan y <paramref name="fijos"/> lo que no se debe tapar.
        /// </summary>
        public static Vector2[] Colocar(IReadOnlyList<Vector2> iconos, IReadOnlyList<Vector2> tamanos, IReadOnlyList<bool> arriba, IReadOnlyList<bool> visibles,
            IReadOnlyList<Rect> fijos, float ladoIcono)
        {
            int n = iconos.Count;
            Vector2[] resultado = new Vector2[n];
            List<Rect> ocupados = new List<Rect>(fijos);
            for (int i = 0; i < n; i++)
            {
                if (visibles[i])
                {
                    ocupados.Add(Rect.MinMaxRect(iconos[i].x - ladoIcono * 0.5f, iconos[i].y - ladoIcono * 0.5f, iconos[i].x + ladoIcono * 0.5f, iconos[i].y + ladoIcono * 0.5f));
                }
            }
            int baseIconos = fijos.Count;
            List<Rect> puestos = new List<Rect>();
            for (int i = 0; i < n; i++)
            {
                if (!visibles[i])
                {
                    continue;
                }
                Vector2 mitad = tamanos[i] * 0.5f;
                float ladoY = ladoIcono * 0.5f + mitad.y + Holgura;
                float ladoX = ladoIcono * 0.5f + mitad.x + Holgura;
                float signo = arriba[i] ? 1f : -1f;
                Vector2[] candidatos =
                {
                    new Vector2(0f, signo * ladoY), new Vector2(0f, -signo * ladoY),
                    new Vector2(ladoX, 0f), new Vector2(-ladoX, 0f),
                    new Vector2(0f, signo * (ladoY + tamanos[i].y + Holgura)), new Vector2(0f, -signo * (ladoY + tamanos[i].y + Holgura)),
                    new Vector2(ladoX, signo * ladoY), new Vector2(-ladoX, signo * ladoY),
                };
                float mejorArea = float.MaxValue;
                Vector2 mejor = candidatos[0];
                for (int c = 0; c < candidatos.Length; c++)
                {
                    Rect r = Caja(iconos[i] + candidatos[c], tamanos[i]);
                    float area = 0f;
                    for (int o = 0; o < ocupados.Count; o++)
                    {
                        // El propio icono no cuenta (los iconos van tras los fijos, en el mismo orden).
                        if (o >= baseIconos && IndiceDeIcono(o - baseIconos, visibles) == i)
                        {
                            continue;
                        }
                        area += Traslape(r, ocupados[o]);
                    }
                    for (int p = 0; p < puestos.Count; p++)
                    {
                        area += Traslape(r, puestos[p]);
                    }
                    if (area < mejorArea)
                    {
                        mejorArea = area;
                        mejor = candidatos[c];
                    }
                    if (area <= 0f)
                    {
                        break;
                    }
                }
                resultado[i] = mejor;
                puestos.Add(Caja(iconos[i] + mejor, tamanos[i]));
            }
            return resultado;
        }

        /// <summary>El rectángulo de un nombre de <paramref name="tamano"/> con su centro en <paramref name="centro"/>.</summary>
        public static Rect Caja(Vector2 centro, Vector2 tamano) => new Rect(centro - tamano * 0.5f, tamano);

        /// <summary>Área en común de dos rectángulos (0 si no se tocan).</summary>
        public static float Traslape(Rect a, Rect b)
        {
            float ancho = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
            float alto = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            return ancho > 0f && alto > 0f ? ancho * alto : 0f;
        }

        // Los iconos visibles entran a la lista de ocupados en orden; esto devuelve el índice original del k-ésimo visible.
        private static int IndiceDeIcono(int k, IReadOnlyList<bool> visibles)
        {
            int cuenta = -1;
            for (int i = 0; i < visibles.Count; i++)
            {
                if (visibles[i] && ++cuenta == k)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}

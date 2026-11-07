using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-858 (playtest 6-nov tarde): mapa de paso a pie de la ciudad, para encontrar calles sin salida y lugares de los que no se sale. Cada
    /// celda de <see cref="Celda"/> m es transitable si hay piso cerca de la calle (rayo desde arriba) y cabe la cápsula del jugador. Se rellena
    /// desde la puerta de la casa; las zonas transitables que no se alcanzan son bolsas (si llegas ahí cayendo o en carro, no sales caminando).
    /// Salida: docs/capturas/s2t-mapa-paso.png (verde: se alcanza, rojo: bolsa, gris: no se pasa) y Logs/s2t-mapa-paso.txt.
    /// </summary>
    public static class MapaPasoS2t
    {
        public const float Celda = 1f;
        public static readonly Rect Zona = new Rect(-200f, -200f, 400f, 400f);
        public static readonly Vector3 Inicio = new Vector3(-52f, 0.2f, -77f);
        private const float AltoPisoMax = 0.7f;
        private const float AltoPisoMin = -0.6f;
        private const float RadioJugador = 0.3f;
        private const float AltoJugador = 1.8f;
        private const int CeldasBolsaMinima = 4;

        /// <summary>Calcula el mapa. Devuelve (transitable, alcanzado) por celda.</summary>
        public static (bool[,] paso, int[,] zona, int ancho, int largo) Calcular()
        {
            Physics.SyncTransforms();
            int ancho = Mathf.RoundToInt(Zona.width / Celda);
            int largo = Mathf.RoundToInt(Zona.height / Celda);
            bool[,] paso = new bool[ancho, largo];
            for (int i = 0; i < ancho; i++)
            {
                for (int j = 0; j < largo; j++)
                {
                    Vector3 c = Centro(i, j);
                    if (!Physics.Raycast(new Vector3(c.x, 30f, c.z), Vector3.down, out RaycastHit h, 40f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }
                    // Lo de arriba es techo o azotea si queda alto: se busca piso bajo, de abajo hacia arriba.
                    float piso = h.point.y;
                    if (piso > AltoPisoMax)
                    {
                        if (!Physics.Raycast(new Vector3(c.x, AltoPisoMax + 0.05f, c.z), Vector3.down, out RaycastHit h2, 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                        {
                            continue;
                        }
                        piso = h2.point.y;
                    }
                    if (piso < AltoPisoMin || piso > AltoPisoMax)
                    {
                        continue;
                    }
                    Vector3 pie = new Vector3(c.x, piso + RadioJugador + 0.05f, c.z);
                    paso[i, j] = !Physics.CheckCapsule(pie, pie + Vector3.up * (AltoJugador - 2f * RadioJugador), RadioJugador, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                }
            }
            int[,] zona = new int[ancho, largo];
            int siguiente = 1;
            (int ii, int jj) = Indice(Inicio);
            // La celda de inicio puede caer en el umbral: la transitable más cercana.
            if (!paso[ii, jj])
            {
                (ii, jj) = Enumerable.Range(0, 25).SelectMany(d => Enumerable.Range(-d, 2 * d + 1).SelectMany(dx => new[] { (ii + dx, jj - d), (ii + dx, jj + d), (ii - d, jj + dx), (ii + d, jj + dx) }))
                    .First(p => p.Item1 >= 0 && p.Item2 >= 0 && p.Item1 < ancho && p.Item2 < largo && paso[p.Item1, p.Item2]);
            }
            Rellenar(paso, zona, ii, jj, siguiente++);
            for (int i = 0; i < ancho; i++)
            {
                for (int j = 0; j < largo; j++)
                {
                    if (paso[i, j] && zona[i, j] == 0)
                    {
                        Rellenar(paso, zona, i, j, siguiente++);
                    }
                }
            }
            return (paso, zona, ancho, largo);
        }

        private static void Rellenar(bool[,] paso, int[,] zona, int i0, int j0, int id)
        {
            int ancho = paso.GetLength(0), largo = paso.GetLength(1);
            Queue<(int, int)> cola = new Queue<(int, int)>();
            cola.Enqueue((i0, j0));
            zona[i0, j0] = id;
            while (cola.Count > 0)
            {
                (int i, int j) = cola.Dequeue();
                foreach ((int a, int b) in new[] { (i + 1, j), (i - 1, j), (i, j + 1), (i, j - 1) })
                {
                    if (a >= 0 && b >= 0 && a < ancho && b < largo && paso[a, b] && zona[a, b] == 0)
                    {
                        zona[a, b] = id;
                        cola.Enqueue((a, b));
                    }
                }
            }
        }

        public static Vector3 Centro(int i, int j) => new Vector3(Zona.xMin + (i + 0.5f) * Celda, 0f, Zona.yMin + (j + 0.5f) * Celda);

        public static (int, int) Indice(Vector3 p) => (Mathf.Clamp(Mathf.FloorToInt((p.x - Zona.xMin) / Celda), 0, Mathf.RoundToInt(Zona.width / Celda) - 1),
            Mathf.Clamp(Mathf.FloorToInt((p.z - Zona.yMin) / Celda), 0, Mathf.RoundToInt(Zona.height / Celda) - 1));

        /// <summary>Las bolsas: zonas transitables que no se alcanzan desde la casa, con su tamaño (celdas) y su centro.</summary>
        public static List<(int id, int celdas, Vector3 centro)> Bolsas(int[,] zona)
        {
            var celdas = new Dictionary<int, List<Vector3>>();
            for (int i = 0; i < zona.GetLength(0); i++)
            {
                for (int j = 0; j < zona.GetLength(1); j++)
                {
                    int z = zona[i, j];
                    if (z > 1)
                    {
                        if (!celdas.TryGetValue(z, out var l))
                        {
                            celdas[z] = l = new List<Vector3>();
                        }
                        l.Add(Centro(i, j));
                    }
                }
            }
            return celdas.Where(k => k.Value.Count >= CeldasBolsaMinima)
                .Select(k => (k.Key, k.Value.Count, new Vector3(k.Value.Average(v => v.x), 0f, k.Value.Average(v => v.z)))).OrderByDescending(b => b.Item2).ToList();
        }

        [MenuItem("Mecha Cyberpunk/S2t/Mapa de paso a pie")]
        public static void Dibujar()
        {
            ColocarS2.Abrir();
            var (paso, zona, ancho, largo) = Calcular();
            Texture2D t = new Texture2D(ancho, largo, TextureFormat.RGB24, false);
            for (int i = 0; i < ancho; i++)
            {
                for (int j = 0; j < largo; j++)
                {
                    t.SetPixel(i, j, zona[i, j] == 1 ? new Color(0.2f, 0.75f, 0.3f) : zona[i, j] > 1 ? Color.red : paso[i, j] ? Color.yellow : new Color(0.15f, 0.15f, 0.18f));
                }
            }
            (int si, int sj) = Indice(Inicio);
            for (int d = -2; d <= 2; d++)
            {
                t.SetPixel(Mathf.Clamp(si + d, 0, ancho - 1), sj, Color.white);
                t.SetPixel(si, Mathf.Clamp(sj + d, 0, largo - 1), Color.white);
            }
            t.Apply();
            File.WriteAllBytes(ColocarS2t.CarpetaCapturas + "s2t-mapa-paso.png", t.EncodeToPNG());
            StringBuilder sb = new StringBuilder();
            int alcanzadas = 0;
            foreach (int z in zona)
            {
                alcanzadas += z == 1 ? 1 : 0;
            }
            sb.AppendLine("celdas alcanzadas desde la casa: " + alcanzadas);
            foreach (var b in Bolsas(zona))
            {
                sb.AppendLine("bolsa " + b.id + ": " + b.celdas + " celdas, centro " + b.centro.ToString("F0"));
            }
            File.WriteAllText("Logs/s2t-mapa-paso.txt", sb.ToString());
        }
    }
}

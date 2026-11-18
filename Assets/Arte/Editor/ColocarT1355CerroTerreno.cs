using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1355: la ladera del Cerro como malla de altura (rejilla de 4 m) y las mallas de calle y escalera. Sin escena: solo cuentas.
    /// La altura es una base que sube hacia el oeste (0 en x = -305, ~40 m en x = -455, roca detrás) mezclada con "fuentes" planas:
    /// calles (el piso queda a 12 cm bajo el asfalto), escaleras, la plaza y las plataformas de las casas.
    /// </summary>
    public static class CerroT1355Terreno
    {
        public const float X0 = -480f;
        public const float Z0 = -170f;
        public const float Celda = 4f;
        public const int CeldasX = 42;
        public const int CeldasZ = 97;
        public const float MetrosUv = 4f;
        private const float AltoLomaTunel = 20f;

        /// <summary>Una fuente de altura: puntos con su altura de piso, a qué distancia se queda plana y cómo se mezcla.</summary>
        public sealed class Fuente
        {
            public Vector3[] Puntos;
            public float Semiancho;
            public float Baja;
        }

        /// <summary>Plataforma rectangular (casa).</summary>
        public struct Plataforma
        {
            public float X0, X1, Z0, Z1, Y;
        }

        public static float Suave(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        /// <summary>Ladera sin calles: sube al oeste, se aplana al sur (por la unión U3), roca al fondo y loma sobre el túnel de la autopista.</summary>
        public static float Base(float x, float z)
        {
            float t = Mathf.Clamp01((CerroT1355Trazado.XBase - x) / 150f);
            float h = 30f * Suave(t);
            float oeste = Mathf.Max(0f, -450f - x);
            h += oeste * 0.5f;
            if (oeste > 0f)
            {
                h += 2.5f * Mathf.Sin(z * 0.07f + x * 0.11f) * Mathf.Min(1f, oeste / 10f);
            }
            h *= Suave(Mathf.InverseLerp(-150f, -100f, z));
            // Loma sobre el túnel: de la salida (-360, 185) hacia la boca (-330, 250); el terreno queda por encima.
            float d = DistanciaASegmento(new Vector2(x, z), new Vector2(-360f, 185f), new Vector2(-330f, 250f));
            float loma = AltoLomaTunel * (1f - Suave((d - 8f) / 20f)) * Suave(Mathf.InverseLerp(178f, 192f, z));
            return Mathf.Max(h, loma);
        }

        private static float DistanciaASegmento(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (p - (a + ab * t)).magnitude;
        }

        /// <summary>Altura final del terreno en (x, z).</summary>
        public static float Altura(float x, float z, IList<Fuente> fuentes, IList<Plataforma> plataformas)
        {
            float b = Base(x, z);
            float suma = 0f, pesos = 0f, mejor = 0f;
            foreach (Fuente f in fuentes)
            {
                float dm = float.MaxValue;
                float hm = 0f;
                foreach (Vector3 p in f.Puntos)
                {
                    float dx = p.x - x, dz = p.z - z;
                    float d2 = dx * dx + dz * dz;
                    if (d2 < dm)
                    {
                        dm = d2;
                        hm = p.y - f.Baja;
                    }
                }
                float d = Mathf.Sqrt(dm);
                float mezcla = Mathf.Clamp(Mathf.Abs(hm - b) * 1.6f, 8f, 30f);
                float a = 1f - Suave((d - f.Semiancho) / mezcla);
                if (a > 0f)
                {
                    suma += a * (hm - b);
                    pesos += a;
                    mejor = Mathf.Max(mejor, a);
                }
            }
            float h = pesos > 0f ? b + mejor * suma / pesos : b;
            // Las plataformas de las casas mandan sobre todo lo demás.
            float sumaP = 0f, pesosP = 0f, mejorP = 0f;
            foreach (Plataforma p in plataformas)
            {
                float dx = Mathf.Max(Mathf.Max(p.X0 - x, x - p.X1), 0f);
                float dz = Mathf.Max(Mathf.Max(p.Z0 - z, z - p.Z1), 0f);
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                float mezcla = Mathf.Clamp(Mathf.Abs(p.Y - h) * 1.2f, 2.5f, 8f);
                float a = 1f - Suave(d / mezcla);
                if (a > 0f)
                {
                    sumaP += a * p.Y;
                    pesosP += a;
                    mejorP = Mathf.Max(mejorP, a);
                }
            }
            return pesosP > 0f ? Mathf.Lerp(h, sumaP / pesosP, mejorP) : h;
        }

        /// <summary>
        /// La malla del terreno con UV de mundo en las tres direcciones (4 m por repetición): cada triángulo toma el plano
        /// (x,z), (z,y) o (x,y) según hacia dónde mira, así los taludes no estiran la textura. Normales suaves de la rejilla.
        /// </summary>
        public static Mesh Malla(IList<Fuente> fuentes, IList<Plataforma> plataformas)
        {
            int nx = CeldasX + 1, nz = CeldasZ + 1;
            float[,] h = new float[nx, nz];
            for (int j = 0; j < nz; j++)
            {
                for (int i = 0; i < nx; i++)
                {
                    h[i, j] = Altura(X0 + i * Celda, Z0 + j * Celda, fuentes, plataformas);
                }
            }
            Vector3[] normales = new Vector3[nx * nz];
            for (int j = 0; j < nz; j++)
            {
                for (int i = 0; i < nx; i++)
                {
                    float dx = h[Mathf.Max(i - 1, 0), j] - h[Mathf.Min(i + 1, nx - 1), j];
                    float dz = h[i, Mathf.Max(j - 1, 0)] - h[i, Mathf.Min(j + 1, nz - 1)];
                    normales[j * nx + i] = new Vector3(dx, 2f * Celda, dz).normalized;
                }
            }
            List<Vector3> v = new List<Vector3>();
            List<Vector3> n = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> t = new List<int>();
            for (int j = 0; j < CeldasZ; j++)
            {
                for (int i = 0; i < CeldasX; i++)
                {
                    int[] ci = { i, i, i + 1, i + 1 };
                    int[] cj = { j, j + 1, j, j + 1 };
                    // 0 = (i,j), 1 = (i,j+1), 2 = (i+1,j), 3 = (i+1,j+1): triángulos (0,1,2) y (2,1,3).
                    foreach (int[] tri in new[] { new[] { 0, 1, 2 }, new[] { 2, 1, 3 } })
                    {
                        Vector3 a = new Vector3(X0 + ci[tri[0]] * Celda, h[ci[tri[0]], cj[tri[0]]], Z0 + cj[tri[0]] * Celda);
                        Vector3 b = new Vector3(X0 + ci[tri[1]] * Celda, h[ci[tri[1]], cj[tri[1]]], Z0 + cj[tri[1]] * Celda);
                        Vector3 c = new Vector3(X0 + ci[tri[2]] * Celda, h[ci[tri[2]], cj[tri[2]]], Z0 + cj[tri[2]] * Celda);
                        Vector3 cara = Vector3.Cross(b - a, c - a);
                        Vector3 abs = new Vector3(Mathf.Abs(cara.x), Mathf.Abs(cara.y), Mathf.Abs(cara.z));
                        int eje = abs.y >= abs.x && abs.y >= abs.z ? 1 : (abs.x >= abs.z ? 0 : 2);
                        for (int k = 0; k < 3; k++)
                        {
                            Vector3 p = k == 0 ? a : (k == 1 ? b : c);
                            t.Add(v.Count);
                            v.Add(p);
                            n.Add(normales[cj[tri[k]] * nx + ci[tri[k]]]);
                            uv.Add(UvMundo(p, eje));
                        }
                    }
                }
            }
            Mesh m = new Mesh { name = "T1355_Terreno" };
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }

        /// <summary>UV de mundo de un punto: eje 1 (mira arriba) usa (x, z); eje 0 usa (z, y); eje 2 usa (x, y).</summary>
        public static Vector2 UvMundo(Vector3 p, int eje)
        {
            switch (eje)
            {
                case 0: return new Vector2(p.z, p.y) / MetrosUv;
                case 2: return new Vector2(p.x, p.y) / MetrosUv;
                default: return new Vector2(p.x, p.z) / MetrosUv;
            }
        }

        // ---------------------------------------------------------------- Cintas de calle

        /// <summary>Datos de malla que se van juntando.</summary>
        public sealed class Datos
        {
            public readonly List<Vector3> V = new List<Vector3>();
            public readonly List<Vector2> Uv = new List<Vector2>();
            public readonly List<int> T = new List<int>();

            public Mesh Crear(string nombre)
            {
                Mesh m = new Mesh { name = nombre, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                m.SetVertices(V);
                m.SetUVs(0, Uv);
                m.SetTriangles(T, 0);
                m.RecalculateNormals();
                m.RecalculateBounds();
                return m;
            }
        }

        private static Vector3 Izquierda(IList<Vector3> p, int i)
        {
            Vector3 a = p[Mathf.Max(0, i - 1)], b = p[Mathf.Min(p.Count - 1, i + 1)];
            Vector3 t = new Vector3(b.x - a.x, 0f, b.z - a.z).normalized;
            return new Vector3(-t.z, 0f, t.x);
        }

        /// <summary>Tira horizontal entre los desplazamientos d0 y d1 (d1 mayor, a la izquierda) sobre la línea, <paramref name="dy"/> arriba.</summary>
        public static void Cinta(Datos m, IList<Vector3> p, float d0, float d1, float dy, float metrosUv)
        {
            int baseIdx = m.V.Count;
            float recorrido = 0f;
            for (int i = 0; i < p.Count; i++)
            {
                if (i > 0)
                {
                    recorrido += Vector3.Distance(new Vector3(p[i].x, 0f, p[i].z), new Vector3(p[i - 1].x, 0f, p[i - 1].z));
                }
                Vector3 l = Izquierda(p, i);
                m.V.Add(p[i] + l * d0 + Vector3.up * dy);
                m.V.Add(p[i] + l * d1 + Vector3.up * dy);
                m.Uv.Add(new Vector2(0f, recorrido / metrosUv));
                m.Uv.Add(new Vector2((d1 - d0) / metrosUv, recorrido / metrosUv));
            }
            for (int i = 0; i < p.Count - 1; i++)
            {
                int a0 = baseIdx + i * 2, a1 = a0 + 1, b0 = a0 + 2, b1 = a0 + 3;
                m.T.AddRange(new[] { a0, a1, b0, a1, b1, b0 });
            }
        }

        /// <summary>Cara vertical (cordón) a distancia d de la línea, de y a y + alto. <paramref name="haciaIzquierda"/>: la cara mira a la izquierda.</summary>
        public static void Cordon(Datos m, IList<Vector3> p, float d, float alto, bool haciaIzquierda, float metrosUv)
        {
            int baseIdx = m.V.Count;
            float recorrido = 0f;
            for (int i = 0; i < p.Count; i++)
            {
                if (i > 0)
                {
                    recorrido += Vector3.Distance(new Vector3(p[i].x, 0f, p[i].z), new Vector3(p[i - 1].x, 0f, p[i - 1].z));
                }
                Vector3 pos = p[i] + Izquierda(p, i) * d;
                m.V.Add(pos);
                m.V.Add(pos + Vector3.up * alto);
                m.Uv.Add(new Vector2(0f, recorrido / metrosUv));
                m.Uv.Add(new Vector2(alto / metrosUv, recorrido / metrosUv));
            }
            for (int i = 0; i < p.Count - 1; i++)
            {
                int a0 = baseIdx + i * 2, a1 = a0 + 1, b0 = a0 + 2, b1 = a0 + 3;
                if (haciaIzquierda)
                {
                    m.T.AddRange(new[] { a0, b0, a1, a1, b0, b1 });
                }
                else
                {
                    m.T.AddRange(new[] { a0, a1, b0, a1, b1, b0 });
                }
            }
        }

        // ---------------------------------------------------------------- Escalera de concreto

        /// <summary>
        /// Escalera recta que baja hacia +x, de (xArriba, yArriba) a (xAbajo, yAbajo), con <paramref name="pasos"/> escalones, de z0 a z1.
        /// Lleva costados hasta 2.5 m bajo cada escalón para que no se vea el hueco.
        /// </summary>
        public static Mesh Escalera(string nombre, float xArriba, float yArriba, float xAbajo, float yAbajo, int pasos, float z0, float z1)
        {
            List<Vector2> perfil = new List<Vector2>();
            float huella = (xAbajo - xArriba) / pasos;
            float alza = (yArriba - yAbajo) / pasos;
            for (int k = 0; k < pasos; k++)
            {
                perfil.Add(new Vector2(xArriba + k * huella, yArriba - k * alza));
            }
            perfil.Add(new Vector2(xAbajo, yAbajo + alza));
            perfil.Add(new Vector2(xAbajo, yAbajo));
            Datos m = new Datos();
            for (int k = 0; k < perfil.Count - 1; k++)
            {
                Vector2 a = perfil[k], b = perfil[k + 1];
                int i0 = m.V.Count;
                m.V.Add(new Vector3(a.x, a.y, z0));
                m.V.Add(new Vector3(a.x, a.y, z1));
                m.V.Add(new Vector3(b.x, b.y, z0));
                m.V.Add(new Vector3(b.x, b.y, z1));
                for (int u = 0; u < 4; u++)
                {
                    m.Uv.Add(new Vector2(m.V[i0 + u].x / 2f, m.V[i0 + u].y / 2f));
                }
                m.T.AddRange(new[] { i0, i0 + 1, i0 + 2, i0 + 1, i0 + 3, i0 + 2 });
                if (Mathf.Abs(b.x - a.x) > 0.0001f)
                {
                    float fondo = Mathf.Min(a.y, b.y) - 2.5f;
                    for (int lado = 0; lado < 2; lado++)
                    {
                        float z = lado == 0 ? z0 : z1;
                        int s0 = m.V.Count;
                        m.V.Add(new Vector3(a.x, a.y, z));
                        m.V.Add(new Vector3(b.x, b.y, z));
                        m.V.Add(new Vector3(a.x, fondo, z));
                        m.V.Add(new Vector3(b.x, fondo, z));
                        for (int u = 0; u < 4; u++)
                        {
                            m.Uv.Add(new Vector2(m.V[s0 + u].x / 2f, m.V[s0 + u].y / 2f));
                        }
                        if (lado == 1)
                        {
                            m.T.AddRange(new[] { s0, s0 + 2, s0 + 1, s0 + 1, s0 + 2, s0 + 3 });
                        }
                        else
                        {
                            m.T.AddRange(new[] { s0, s0 + 1, s0 + 2, s0 + 1, s0 + 3, s0 + 2 });
                        }
                    }
                }
            }
            return m.Crear(nombre);
        }
    }
}

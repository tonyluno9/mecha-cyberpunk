using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1356 y T-1362: trazado puro (sin escena) de la autopista elevada del borde norte y sus uniones con la ciudad (plan-zonas-18nov.md).
    /// Cada lista es la línea del centro de una calle, en mundo, con la y real del tablero. Las uniones U6, U8 y U9 salen EXACTAS; la U7 (rampa norte)
    /// se reubicó en T-1362: la rampa sale de la calle baja y sube hacia el oeste, junto al túnel, como en K18.
    /// Las pruebas de EditMode (AutopistaT1356Pruebas) miden pendiente, radio, uniones, separación de columnas y de casas sobre estas listas.
    /// </summary>
    public static class ColocarT1356Trazado
    {
        public const float YTablero = 10f;
        public const float ZTablero = 250f;
        public const float AnchoCalle = 14f;
        public const float PendienteMaxima = 0.09f;
        public const float RadioMinimo = 40f;
        /// <summary>El túnel gira 90 grados en 30 m hacia el oeste y 65 m hacia el sur: con la boca y la salida fijas el radio máximo es 30 m (con 40 m la salida quedaría 10 m corrida).</summary>
        public const float RadioTunel = 30f;
        public const float RadioCurvaEste = 60f;
        public const float RadioRampaNorte = 40f;
        public const float SeparacionColumnas = 24f;
        public const float MaxSeparacionColumnas = 26f;
        public const float AlturaMinimaColumna = 2f;
        public const float PasoCurva = 15f;
        /// <summary>Metros antes de tocar la huella de la losa principal en los que la rampa norte ya va a la altura del tablero.</summary>
        public const float MargenIncorporacion = 15f;
        /// <summary>Línea del centro del tramo recto de la rampa norte (z, m).</summary>
        public const float ZRampaNorte = 232f;
        /// <summary>Calle baja de barrio (T-1362): z = 215 de x = -212 (donde llega la calle oeste) hasta el enlace de la rampa (-120,0,215).</summary>
        public const float ZCalleBaja = 215f;
        public const float XOesteCalleBaja = -212f;
        /// <summary>Separación mínima (m) en planta entre el borde del tablero y cualquier casa.</summary>
        public const float SeparacionCasasTablero = 8f;
        public const float MedioTableroPlanta = 7.2f;

        /// <summary>Huellas en planta (x0, x1, z0, z1) de las hileras de casitas que coloca ColocarT1356Autopista.Bajo.</summary>
        public static readonly (float x0, float x1, float z0, float z1)[] HuellasCasas =
        {
            (-195f, -135f, 199f, 205f), // sur de la calle baja, de cara al norte
            (40f, 225f, 204f, 210f),    // sur de la calle del lote este
            (-320f, 325f, 268f, 280f),  // detrás del tablero, de cara al sur
            (-258f, -252f, 80f, 170f),  // oeste de la calle oeste (x = -242), de cara al este
        };

        /// <summary>Distancia en planta (m) de un punto a una huella rectangular (0 si está dentro).</summary>
        public static float DistanciaAHuella(Vector3 punto, (float x0, float x1, float z0, float z1) h)
        {
            float dx = Mathf.Max(h.x0 - punto.x, 0f, punto.x - h.x1);
            float dz = Mathf.Max(h.z0 - punto.z, 0f, punto.z - h.z1);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>Puntos a lo largo de la línea cada <paramref name="paso"/> m (con y interpolada), incluidos los extremos.</summary>
        public static List<Vector3> Resamplear(IList<Vector3> p, float paso)
        {
            List<Vector3> r = new List<Vector3>();
            float largo = Longitud(p);
            int n = Mathf.Max(1, Mathf.CeilToInt(largo / paso));
            for (int k = 0; k <= n; k++)
            {
                PuntoEn(p, largo * k / n, out Vector3 punto, out _);
                r.Add(punto);
            }
            return r;
        }

        public static readonly Vector3 U6Boca = new Vector3(-330f, 10f, 250f);
        public static readonly Vector3 U6Salida = new Vector3(-360f, 10f, 185f);
        public static readonly Vector3 U7Inicio = new Vector3(-107f, 0f, 201f);
        public static readonly Vector3 U7Enlace = new Vector3(-120f, 0f, 215f);
        /// <summary>T-1362: la rampa norte sube hacia el OESTE (junto al túnel, como K18) y se une al tablero en (-270,10,250); antes era (30,10,250).</summary>
        public static readonly Vector3 U7Union = new Vector3(-270f, 10f, 250f);
        public static readonly Vector3 U8Curva = new Vector3(240f, 10f, 250f);
        public static readonly Vector3 U8Bajada = new Vector3(300f, 10f, 190f);
        public static readonly Vector3 U8Piso = new Vector3(300f, 0f, 60f);
        public static readonly Vector3 U9Cruce = new Vector3(300f, 0f, 0f);
        public static readonly Vector3 U9Calle = new Vector3(249f, 0f, 0f);
        public static readonly Vector3 PuertaNortePuerto = new Vector3(300f, 0f, -45f);

        /// <summary>Puntos de un arco en planta (x = cx + R cos t, z = cz + R sin t) con t de grados0 a grados1, sin el primero.</summary>
        public static List<Vector3> Arco(float cx, float cz, float radio, float grados0, float grados1, float pasoGrados, float y)
        {
            List<Vector3> lista = new List<Vector3>();
            int n = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(grados1 - grados0) / pasoGrados - 0.001f));
            for (int i = 1; i <= n; i++)
            {
                float t = Mathf.Lerp(grados0, grados1, i / (float)n) * Mathf.Deg2Rad;
                lista.Add(new Vector3(cx + radio * Mathf.Cos(t), y, cz + radio * Mathf.Sin(t)));
            }
            return lista;
        }

        /// <summary>Túnel: de la boca (-330,10,250) gira a la izquierda 90 grados (radio 30) y baja recto hasta la salida (-360,10,185) mirando al sur.</summary>
        public static List<Vector3> Tunel()
        {
            List<Vector3> p = new List<Vector3> { U6Boca };
            p.AddRange(Arco(-330f, 220f, RadioTunel, 90f, 180f, PasoCurva, YTablero));
            p[p.Count - 1] = new Vector3(-360f, YTablero, 220f);
            p.Add(new Vector3(-360f, YTablero, 202.5f));
            p.Add(U6Salida);
            return p;
        }

        /// <summary>Recta z = 250 de la boca a (240,10,250) cada 30 m (pasa por U7 en x = -270), curva de 90 grados de radio 60 y bajada recta por x = 300 hasta el piso en z = 60.</summary>
        public static List<Vector3> Principal()
        {
            List<Vector3> p = new List<Vector3>();
            for (int k = 0; k <= 19; k++)
            {
                p.Add(new Vector3(-330f + 30f * k, YTablero, ZTablero));
            }
            p.AddRange(Arco(240f, 190f, RadioCurvaEste, 90f, 0f, PasoCurva, YTablero));
            p[p.Count - 1] = U8Bajada;
            for (float z = 180f; z > 60f; z -= 10f)
            {
                p.Add(new Vector3(300f, YTablero * (z - 60f) / 130f, z));
            }
            p.Add(U8Piso);
            return p;
        }

        /// <summary>Límite de la recta plana: índices 0 a <see cref="IndiceFinRecta"/> de <see cref="Principal"/> (x -330 a 240).</summary>
        public const int IndiceFinRecta = 19;

        /// <summary>Calle baja con banquetas: del oeste al enlace de la rampa norte (U7Enlace), de donde sale la rampa.</summary>
        public static List<Vector3> CalleBaja()
        {
            return new List<Vector3>
            {
                new Vector3(XOesteCalleBaja, 0f, ZCalleBaja), new Vector3(-165f, 0f, ZCalleBaja), U7Enlace,
            };
        }

        /// <summary>Extremo norte (abierto) de la calle cruzada oeste de T530 (x = -242, z -60 a 60): ahí arranca la calle oeste de la autopista.</summary>
        public static readonly Vector3 CruzadaOesteNorte = new Vector3(-242f, 0f, 60f);
        public const float RadioCalleOeste = 30f;
        /// <summary>Ancho (m) de la calle oeste de la autopista.</summary>
        public const float AnchoCalleOeste = 14f;

        /// <summary>
        /// Calle oeste (T-1362): de la calle cruzada oeste de T530 (-242,0,60) recta al norte por x = -242 y una vuelta de radio 30 al este hasta la calle baja
        /// en (-212,0,215). Con la calle baja y la rampa une la calle oeste, el enlace (-120,0,215) y la calle cruzada norte (-107,0,201).
        /// </summary>
        public static List<Vector3> CalleOeste()
        {
            List<Vector3> p = new List<Vector3> { CruzadaOesteNorte };
            for (float z = 90f; z < 185f - 1f; z += 31.67f)
            {
                p.Add(new Vector3(-242f, 0f, z));
            }
            p.Add(new Vector3(-242f, 0f, 185f));
            p.AddRange(Arco(-242f + RadioCalleOeste, 185f, RadioCalleOeste, 180f, 90f, PasoCurva, 0f));
            p[p.Count - 1] = new Vector3(XOesteCalleBaja, 0f, ZCalleBaja);
            return p;
        }

        /// <summary>Puntos de la red que NO son de las zonas de la autopista pero a los que sus extremos se unen (calles de T530, cerro, puerto).</summary>
        public static readonly Vector3[] PuntosDeLaRedExterna =
        {
            CruzadaOesteNorte, U7Inicio, U6Salida, U9Calle, PuertaNortePuerto,
        };

        /// <summary>
        /// Rampa norte (T-1362): del borde de la calle cruzada norte (U7) y su enlace (-120,0,215) sobre la calle baja, curva a la izquierda (radio ~53)
        /// hasta ir al OESTE por z = 232, recta, y una S de radio 40 que la lleva a z = 250 y y = 10 en (-270,10,250), tangente a la autopista.
        /// Sube con pendiente pareja desde el enlace (~8.4 %).
        /// </summary>
        public static List<Vector3> RampaNorte()
        {
            const float r = RadioRampaNorte;
            Vector3 dir = Planta(U7Enlace - U7Inicio).normalized;
            float h = Mathf.Atan2(dir.z, dir.x);
            float r1 = (ZRampaNorte - U7Enlace.z) / (1f + Mathf.Cos(h));
            float cx = U7Enlace.x - r1 * Mathf.Sin(h);
            float cz = U7Enlace.z + r1 * Mathf.Cos(h);
            List<Vector3> p = new List<Vector3> { U7Inicio, U7Enlace };
            p.AddRange(Arco(cx, cz, r1, h * Mathf.Rad2Deg - 90f, 90f, PasoCurva, 0f));
            float d = U7Union.z - ZRampaNorte;
            float theta = Mathf.Acos(1f - d / (2f * r));
            float xS = U7Union.x + 2f * r * Mathf.Sin(theta);
            for (float x = Mathf.Floor((cx - 10f) / 20f) * 20f; x > xS + 10f; x -= 20f)
            {
                p.Add(new Vector3(x, 0f, ZRampaNorte));
            }
            p.Add(new Vector3(xS, 0f, ZRampaNorte));
            float tg = theta * Mathf.Rad2Deg;
            p.AddRange(Arco(xS, ZRampaNorte + r, r, -90f, -90f - tg, tg / 3f, 0f));
            p.AddRange(Arco(xS - 2f * r * Mathf.Sin(theta), U7Union.z - r, r, 90f - tg, 90f, tg / 3f, 0f));
            // La y sube pareja desde el enlace y llega a 10 m (la altura del tablero) MargenIncorporacion m antes de que el centro de la rampa entre
            // en la huella de la losa principal (z >= 250 - 7.2); desde ahí va a nivel y se une en plano.
            float zToca = ZTablero - MedioTableroPlanta;
            float sToca = -1f;
            float acumulado = 0f;
            for (int i = 2; i < p.Count && sToca < 0f; i++)
            {
                float l = Planta(p[i] - p[i - 1]).magnitude;
                if (p[i].z >= zToca)
                {
                    float f = Mathf.InverseLerp(p[i - 1].z, p[i].z, zToca);
                    sToca = acumulado + l * f;
                }
                acumulado += l;
            }
            float sLlana = sToca - MargenIncorporacion;
            float s = 0f;
            for (int i = 2; i < p.Count; i++)
            {
                s += Planta(p[i] - p[i - 1]).magnitude;
                p[i] = new Vector3(p[i].x, YTablero * Mathf.Min(1f, s / sLlana), p[i].z);
            }
            p[p.Count - 1] = U7Union;
            return p;
        }

        /// <summary>Calle de piso por x = 300: de la bajada (300,0,60) pasando por U9 (300,0,0) a la puerta norte del Puerto.</summary>
        public static List<Vector3> CalleEste()
        {
            return new List<Vector3> { U8Piso, new Vector3(300f, 0f, 30f), U9Cruce, new Vector3(300f, 0f, -22.5f), PuertaNortePuerto };
        }

        /// <summary>Conector U9 a la calle cruzada este.</summary>
        public static List<Vector3> ConectorEste()
        {
            return new List<Vector3> { U9Cruce, new Vector3(274.5f, 0f, 0f), U9Calle };
        }

        // ---------------------------------------------------------------- Medidas

        public static Vector3 Planta(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        public static float Longitud(IList<Vector3> p)
        {
            float l = 0f;
            for (int i = 1; i < p.Count; i++)
            {
                l += Vector3.Distance(p[i - 1], p[i]);
            }
            return l;
        }

        /// <summary>Pendiente máxima (subida entre distancia en planta) de los tramos.</summary>
        public static float PendienteMax(IList<Vector3> p)
        {
            float max = 0f;
            for (int i = 1; i < p.Count; i++)
            {
                float h = Planta(p[i] - p[i - 1]).magnitude;
                if (h > 0.001f)
                {
                    max = Mathf.Max(max, Mathf.Abs(p[i].y - p[i - 1].y) / h);
                }
            }
            return max;
        }

        /// <summary>Radio más chico (circunscrito de tres puntos seguidos, en planta) desde el punto <paramref name="desde"/>.</summary>
        public static float RadioMin(IList<Vector3> p, int desde = 0)
        {
            float min = float.MaxValue;
            for (int i = Mathf.Max(1, desde + 1); i < p.Count - 1; i++)
            {
                min = Mathf.Min(min, Radio(p[i - 1], p[i], p[i + 1]));
            }
            return min;
        }

        public static float Radio(Vector3 a, Vector3 b, Vector3 c)
        {
            float ab = Planta(b - a).magnitude;
            float bc = Planta(c - b).magnitude;
            float ca = Planta(a - c).magnitude;
            float area2 = Mathf.Abs((b.x - a.x) * (c.z - a.z) - (c.x - a.x) * (b.z - a.z));
            return area2 < 0.0001f ? float.MaxValue : ab * bc * ca / (2f * area2);
        }

        public static bool Pasa(IList<Vector3> p, Vector3 punto, float tolerancia = 0.01f)
        {
            for (int i = 0; i < p.Count; i++)
            {
                if (Vector3.Distance(p[i], punto) <= tolerancia)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Punto y tangente (en planta) a <paramref name="s"/> metros del inicio de la línea.</summary>
        public static void PuntoEn(IList<Vector3> p, float s, out Vector3 punto, out Vector3 tangente)
        {
            float resto = s;
            for (int i = 1; i < p.Count; i++)
            {
                float l = Vector3.Distance(p[i - 1], p[i]);
                if (resto <= l || i == p.Count - 1)
                {
                    float f = l < 0.0001f ? 0f : Mathf.Clamp01(resto / l);
                    punto = Vector3.Lerp(p[i - 1], p[i], f);
                    tangente = Planta(p[i] - p[i - 1]).normalized;
                    return;
                }
                resto -= l;
            }
            punto = p[0];
            tangente = Vector3.right;
        }

        /// <summary>Distancias a lo largo de la línea de cada columna: reparte el largo en tramos iguales de a lo más <paramref name="separacion"/> m (sin los extremos).</summary>
        public static List<float> Estaciones(IList<Vector3> p, float separacion)
        {
            List<float> e = new List<float>();
            float largo = Longitud(p);
            int n = Mathf.Max(1, Mathf.CeilToInt(largo / separacion));
            for (int k = 1; k < n; k++)
            {
                e.Add(largo * k / n);
            }
            return e;
        }

        /// <summary>Columnas de una línea elevada: puntos del centro del tablero donde la altura sobre el piso pasa de <see cref="AlturaMinimaColumna"/>.</summary>
        public static List<Vector3> Columnas(IList<Vector3> p, float separacion = SeparacionColumnas)
        {
            List<Vector3> lista = new List<Vector3>();
            foreach (float s in Estaciones(p, separacion))
            {
                PuntoEn(p, s, out Vector3 punto, out _);
                if (punto.y >= AlturaMinimaColumna)
                {
                    lista.Add(punto);
                }
            }
            return lista;
        }

        /// <summary>Puntos de las luminarias: cada <paramref name="separacion"/> m desde la mitad del primer paso, solo donde el tablero va elevado.</summary>
        public static List<(Vector3 punto, Vector3 tangente)> Luminarias(IList<Vector3> p, float separacion)
        {
            List<(Vector3, Vector3)> lista = new List<(Vector3, Vector3)>();
            float largo = Longitud(p);
            for (float s = separacion * 0.5f; s < largo; s += separacion)
            {
                PuntoEn(p, s, out Vector3 punto, out Vector3 tangente);
                if (punto.y >= AlturaMinimaColumna)
                {
                    lista.Add((punto, tangente));
                }
            }
            return lista;
        }
    }
}

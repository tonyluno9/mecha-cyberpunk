using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1354: las cuentas del Malecón sin Unity (se prueban en EditMode). La avenida costera va de U3 (-300, -205) a la puerta del Puerto U5 (160, -215)
    /// pasando por U1 (-47, -226) y U2 (47, -226); es plana (y = 0) y se abre en curva con "smootherstep" para que el radio mínimo quede muy arriba de 60 m.
    /// U3 sale de la avenida hacia la base del cerro con una curva Hermite que pasa por (-330, -150) y termina en (-305, -120).
    /// </summary>
    public static class ColocarT1354Curva
    {
        public static readonly Vector3 U1 = new Vector3(-47f, 0f, -226f);
        public static readonly Vector3 U2 = new Vector3(47f, 0f, -226f);
        public static readonly Vector3 U3Inicio = new Vector3(-300f, 0f, -205f);
        public static readonly Vector3 U3Medio = new Vector3(-330f, 0f, -150f);
        public static readonly Vector3 U3Fin = new Vector3(-305f, 0f, -120f);
        public static readonly Vector3 U5 = new Vector3(160f, 0f, -215f);

        /// <summary>Los corredores sur de la ciudad llegan a z = -201 y se alargan hasta la avenida.</summary>
        public const float ZCorredorSur = -201f;

        /// <summary>Tramo recto de la avenida (donde caen U1 y U2).</summary>
        public const float XPlanoDesde = -110f;
        public const float XPlanoHasta = 60f;
        public const float ZPlano = -226f;

        /// <summary>Separación en x entre los puntos de la avenida (m).</summary>
        public const float PasoX = 10f;
        public const float RadioMinimoAvenida = 60f;

        /// <summary>Dos carriles por sentido de 4 m: la avenida mide 16 m de banqueta a banqueta.</summary>
        public const float AnchoAvenida = 16f;

        /// <summary>El perfil de arriba (laterales: negativo hacia la ciudad, positivo hacia el mar).</summary>
        public const float LateralCiudad = -11f;
        public const float LateralBanquetaCiudad = -8f;
        public const float LateralBanquetaMar = 8f;
        public const float LateralMuro = 16f;

        /// <summary>y de la banqueta, del arroyo, del pie del muro, del agua y de la orilla (plan de zonas).</summary>
        public const float YBanqueta = 0.2f;
        public const float YArroyo = 0.02f;
        public const float YPiePlaya = -2.2f;
        public const float YAgua = -3f;
        public const float YFondoPlaya = -6f;

        /// <summary>El agua empieza en z = -262 (plan de zonas).</summary>
        public const float ZAgua = -262f;

        /// <summary>Suavizado de 0 a 1 con primera y segunda derivada en cero en los dos extremos.</summary>
        public static float Suave(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        /// <summary>z del centro de la avenida en la x dada.</summary>
        public static float ZCentro(float x)
        {
            if (x <= U3Inicio.x)
            {
                return U3Inicio.z;
            }
            if (x < XPlanoDesde)
            {
                return Mathf.Lerp(U3Inicio.z, ZPlano, Suave((x - U3Inicio.x) / (XPlanoDesde - U3Inicio.x)));
            }
            if (x <= XPlanoHasta)
            {
                return ZPlano;
            }
            if (x < U5.x)
            {
                return Mathf.Lerp(ZPlano, U5.z, Suave((x - XPlanoHasta) / (U5.x - XPlanoHasta)));
            }
            return U5.z;
        }

        /// <summary>Las x de la avenida: cada <see cref="PasoX"/> m y además los extremos del tramo recto, U1 y U2.</summary>
        public static List<float> XsAvenida()
        {
            SortedSet<float> xs = new SortedSet<float> { XPlanoDesde, XPlanoHasta, U1.x, U2.x };
            for (float x = U3Inicio.x; x <= U5.x + 0.001f; x += PasoX)
            {
                xs.Add(Mathf.Min(x, U5.x));
            }
            xs.Add(U5.x);
            return new List<float>(xs);
        }

        /// <summary>Línea del centro de la avenida (de oeste a este), con y = 0.</summary>
        public static List<Vector3> Avenida()
        {
            List<Vector3> linea = new List<Vector3>();
            foreach (float x in XsAvenida())
            {
                linea.Add(new Vector3(x, 0f, ZCentro(x)));
            }
            return linea;
        }

        /// <summary>La avenida alargada en línea recta hasta la orilla de la zona (muro, playa y banqueta del mar siguen hasta ahí).</summary>
        public static List<Vector3> BordeMar(float xOeste, float xEste)
        {
            List<Vector3> linea = Avenida();
            if (xOeste < linea[0].x)
            {
                linea.Insert(0, new Vector3(xOeste, 0f, linea[0].z));
            }
            if (xEste > linea[linea.Count - 1].x)
            {
                linea.Add(new Vector3(xEste, 0f, linea[linea.Count - 1].z));
            }
            return linea;
        }

        /// <summary>Puntos de la avenida entre x0 y x1 cada <paramref name="paso"/> m (incluye los dos extremos).</summary>
        public static List<Vector3> PuntosEn(float x0, float x1, float paso)
        {
            List<Vector3> linea = new List<Vector3>();
            float x = x0;
            while (x < x1 - 0.001f)
            {
                linea.Add(new Vector3(x, 0f, ZCentro(x)));
                x += paso;
            }
            linea.Add(new Vector3(x1, 0f, ZCentro(x1)));
            return linea;
        }

        /// <summary>U1 o U2: del fin del corredor a la avenida, recta.</summary>
        public static List<Vector3> Union(Vector3 enAvenida)
        {
            return new List<Vector3> { new Vector3(enAvenida.x, 0f, ZCorredorSur), new Vector3(enAvenida.x, 0f, (ZCorredorSur + enAvenida.z) * 0.5f), enAvenida };
        }

        /// <summary>U3: de la avenida (arranca hacia el oeste) por (-330, -150) a la calle base del cerro en (-305, -120).</summary>
        public static List<Vector3> U3()
        {
            List<Vector3> linea = Hermite(U3Inicio, new Vector3(-80f, 0f, 0f), U3Medio, new Vector3(0f, 0f, 60f), 12);
            List<Vector3> segundo = Hermite(U3Medio, new Vector3(0f, 0f, 60f), U3Fin, new Vector3(30f, 0f, 30f), 8);
            segundo.RemoveAt(0);
            linea.AddRange(segundo);
            return linea;
        }

        public static List<Vector3> Hermite(Vector3 p0, Vector3 t0, Vector3 p1, Vector3 t1, int pasos)
        {
            List<Vector3> linea = new List<Vector3>();
            for (int i = 0; i <= pasos; i++)
            {
                float s = (float)i / pasos;
                float s2 = s * s;
                float s3 = s2 * s;
                Vector3 p = (2f * s3 - 3f * s2 + 1f) * p0 + (s3 - 2f * s2 + s) * t0 + (-2f * s3 + 3f * s2) * p1 + (s3 - s2) * t1;
                p.y = 0f;
                linea.Add(p);
            }
            linea[0] = p0;
            linea[linea.Count - 1] = p1;
            return linea;
        }

        /// <summary>Radio (m, en planta) del círculo que pasa por tres puntos; infinito si están en línea.</summary>
        public static float Radio(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector2 pa = new Vector2(a.x, a.z), pb = new Vector2(b.x, b.z), pc = new Vector2(c.x, c.z);
            float ab = Vector2.Distance(pa, pb), bc = Vector2.Distance(pb, pc), ca = Vector2.Distance(pc, pa);
            float area2 = Mathf.Abs((pb.x - pa.x) * (pc.y - pa.y) - (pb.y - pa.y) * (pc.x - pa.x));
            return area2 < 1e-6f ? float.PositiveInfinity : ab * bc * ca / (2f * area2);
        }

        /// <summary>El radio más cerrado de una línea (de tres en tres puntos seguidos).</summary>
        public static float RadioMinimo(IList<Vector3> linea)
        {
            float minimo = float.PositiveInfinity;
            for (int i = 0; i + 2 < linea.Count; i++)
            {
                minimo = Mathf.Min(minimo, Radio(linea[i], linea[i + 1], linea[i + 2]));
            }
            return minimo;
        }

        public static float Longitud(IList<Vector3> linea)
        {
            float largo = 0f;
            for (int i = 0; i + 1 < linea.Count; i++)
            {
                largo += Vector3.Distance(linea[i], linea[i + 1]);
            }
            return largo;
        }

        /// <summary>Pendiente máxima (|dy| entre la distancia en planta) de una línea.</summary>
        public static float PendienteMaxima(IList<Vector3> linea)
        {
            float maxima = 0f;
            for (int i = 0; i + 1 < linea.Count; i++)
            {
                float plano = Vector2.Distance(new Vector2(linea[i].x, linea[i].z), new Vector2(linea[i + 1].x, linea[i + 1].z));
                if (plano > 1e-4f)
                {
                    maxima = Mathf.Max(maxima, Mathf.Abs(linea[i + 1].y - linea[i].y) / plano);
                }
            }
            return maxima;
        }

        /// <summary>¿La línea tiene un punto a menos de <paramref name="tolerancia"/> m (en planta) de <paramref name="punto"/>?</summary>
        public static bool Pasa(IList<Vector3> linea, Vector3 punto, float tolerancia)
        {
            for (int i = 0; i < linea.Count; i++)
            {
                if (Vector2.Distance(new Vector2(linea[i].x, linea[i].z), new Vector2(punto.x, punto.z)) <= tolerancia)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Punto a la distancia <paramref name="distancia"/> (m) sobre la línea y su tangente en planta (unitaria).</summary>
        public static Vector3 PuntoEn(IList<Vector3> linea, float distancia, out Vector3 tangente)
        {
            distancia = Mathf.Clamp(distancia, 0f, Longitud(linea));
            for (int i = 0; i + 1 < linea.Count; i++)
            {
                float tramo = Vector3.Distance(linea[i], linea[i + 1]);
                if (distancia <= tramo || i + 2 == linea.Count)
                {
                    Vector3 d = linea[i + 1] - linea[i];
                    d.y = 0f;
                    tangente = d.normalized;
                    return Vector3.Lerp(linea[i], linea[i + 1], tramo < 1e-5f ? 0f : Mathf.Clamp01(distancia / tramo));
                }
                distancia -= tramo;
            }
            tangente = Vector3.right;
            return linea[0];
        }

        /// <summary>Hacia el mar (la derecha de quien avanza hacia el este) para una tangente en planta.</summary>
        public static Vector3 HaciaElMar(Vector3 tangente)
        {
            return new Vector3(tangente.z, 0f, -tangente.x).normalized;
        }

        /// <summary>
        /// Palmeras y faroles alternados: cada <paramref name="paso"/> m va uno y el siguiente es del otro tipo
        /// (primero palmera). Devuelve la distancia sobre la línea y si es palmera.
        /// </summary>
        public static List<(float distancia, bool palmera)> Alternados(float largo, float paso, float inicio)
        {
            List<(float, bool)> lista = new List<(float, bool)>();
            int i = 0;
            for (float d = inicio; d <= largo + 0.001f; d += paso)
            {
                lista.Add((d, i % 2 == 0));
                i++;
            }
            return lista;
        }

        /// <summary>Distancias cada <paramref name="paso"/> m desde <paramref name="inicio"/> (escaleras, bancas, postes).</summary>
        public static List<float> Cada(float largo, float paso, float inicio)
        {
            List<float> lista = new List<float>();
            for (float d = inicio; d <= largo + 0.001f; d += paso)
            {
                lista.Add(d);
            }
            return lista;
        }

        /// <summary>Ancho de la playa seca (m) del pie del muro a la orilla.</summary>
        public const float AnchoPlaya = 30f;

        /// <summary>
        /// Lateral (m, hacia el mar) donde la playa toca el agua: <see cref="AnchoPlaya"/> m después del pie del muro, en cualquier z
        /// (el plano del mar llega por debajo de la arena hasta el muro, así la orilla es donde la arena baja de y = -3).
        /// </summary>
        public static float LateralOrilla(float zCentro)
        {
            return LateralMuro + AnchoPlaya;
        }

        // ================================================================ T-1363: luz local del Malecón y relleno de T530

        /// <summary>
        /// Volumen de posproceso local del Malecón (isGlobal = false): una caja que cubre la avenida, el paseo y la playa (x de -305 a 165, z de -300 a -205)
        /// con 15 m de mezcla. La mezcla llega a z = -190 como mucho: los barrios viejos (z &gt; -190) no cambian de luz.
        /// </summary>
        public const float VolumenXOeste = -305f;
        public const float VolumenXEste = 165f;
        public const float VolumenZSur = -300f;
        public const float VolumenZNorte = -205f;
        public const float VolumenYBajo = -10f;
        public const float VolumenYAlto = 40f;
        public const float VolumenMezcla = 15f;
        public const float VolumenPrioridad = 10f;

        /// <summary>Los barrios viejos empiezan en esta z hacia el norte: la luz del volumen no debe llegar ahí.</summary>
        public const float ZBarriosViejos = -190f;

        /// <summary>Distancia (m) de la avenida dentro de la cual el relleno liso de T530 se apaga.</summary>
        public const float AlcanceRellenoT530 = 60f;

        /// <summary>z del norte del tramo que las fachadas de las bocas de U1 y U2 cubren (los corredores sur terminan en z = -201).</summary>
        public const float ZInicioFachadaCorredor = -178f;

        /// <summary>La caja del collider del volumen (sin la mezcla).</summary>
        public static Bounds CajaVolumen()
        {
            Vector3 min = new Vector3(VolumenXOeste, VolumenYBajo, VolumenZSur);
            Vector3 max = new Vector3(VolumenXEste, VolumenYAlto, VolumenZNorte);
            return new Bounds((min + max) * 0.5f, max - min);
        }

        /// <summary>Peso del volumen en el punto (0 a 1): 1 dentro de la caja y cae a 0 a <see cref="VolumenMezcla"/> m de ella.</summary>
        public static float PesoVolumen(Vector3 punto)
        {
            Bounds caja = CajaVolumen();
            float distancia = Mathf.Sqrt(caja.SqrDistance(punto));
            return Mathf.Clamp01(1f - distancia / VolumenMezcla);
        }

        /// <summary>
        /// True si el edificio liso de relleno de T530 (por su centro) queda detrás de las fachadas del Malecón, a menos de
        /// <see cref="AlcanceRellenoT530"/> m de la avenida y en el último tramo de los corredores sur.
        /// </summary>
        public static bool RellenoT530Apagable(Vector3 centro)
        {
            float distancia = centro.z - ZCentro(centro.x);
            return distancia > 0f && distancia <= AlcanceRellenoT530 && centro.z < ZInicioFachadaCorredor;
        }
    }
}

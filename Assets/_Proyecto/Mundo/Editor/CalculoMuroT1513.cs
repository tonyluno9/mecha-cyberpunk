using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1513: cuentas puras (sin escena) del muro de la ciudad con los modelos MU. Parte de los segmentos del muro provisional
    /// (T530_Afueras/Orillas/Muro) y decide dónde van los tramos MU01, las torres MU02 (esquinas y a los lados de cada salida)
    /// y las puertas MU03 (en cada hueco del muro). Todo en el plano XZ.
    /// </summary>
    public static class CalculoMuroT1513
    {
        /// <summary>Cuánto se puede estirar o encoger el largo de un tramo para cerrar huecos (15 %).</summary>
        public const float AjusteMaximo = 0.15f;

        /// <summary>Dos extremos a menos de esta distancia son la misma esquina.</summary>
        public const float RadioEsquina = 3f;

        /// <summary>Segmento recto del muro viejo, de A a B.</summary>
        public struct Segmento
        {
            public Vector3 A;
            public Vector3 B;
        }

        /// <summary>Tramo de muro por cubrir con piezas MU01, de Inicio a Fin.</summary>
        public class Tramo
        {
            public Vector3 Inicio;
            public Vector3 Fin;
            public Vector3 Dir;
            public float Largo;
        }

        public class Torre
        {
            public Vector3 Centro;
            public bool Esquina;
        }

        public class Puerta
        {
            public Vector3 Centro;
            public Vector3 Dir;
            public float Ancho;
        }

        public class Plan
        {
            public List<Tramo> Tramos = new List<Tramo>();
            public List<Torre> Torres = new List<Torre>();
            public List<Puerta> Puertas = new List<Puerta>();
        }

        /// <summary>Un tramo ya repartido: cuántas piezas, cuál es _b y el factor de largo que cierra el tramo.</summary>
        public class Reparto
        {
            public List<bool> EsB = new List<bool>();
            public float Factor = 1f;
            public bool DentroDelAjuste;
        }

        /// <summary>
        /// Plan completo. tamTorre es el tamaño (x, z) de la torre en el suelo; las torres no se giran, así que su mitad a lo largo
        /// de un segmento sale de ahí.
        /// </summary>
        public static Plan Planear(IList<Segmento> segmentos, Vector2 tamTorre)
        {
            Plan plan = new Plan();
            int n = segmentos.Count;
            Vector3[] dir = new Vector3[n];
            bool[,] esquina = new bool[n, 2];
            for (int i = 0; i < n; i++)
            {
                Vector3 d = segmentos[i].B - segmentos[i].A;
                d.y = 0f;
                dir[i] = d.normalized;
            }
            for (int i = 0; i < n; i++)
            {
                for (int e = 0; e < 2; e++)
                {
                    Vector3 p = Extremo(segmentos[i], e);
                    for (int j = 0; j < n; j++)
                    {
                        if (j == i || Mathf.Abs(Vector3.Dot(dir[i], dir[j])) > 0.5f)
                        {
                            continue;
                        }
                        if (Distancia(p, segmentos[j].A) < RadioEsquina || Distancia(p, segmentos[j].B) < RadioEsquina)
                        {
                            esquina[i, e] = true;
                        }
                    }
                }
            }

            float[] desde = new float[n];
            float[] hasta = new float[n];
            for (int i = 0; i < n; i++)
            {
                float mitad = Mathf.Abs(dir[i].x) * tamTorre.x * 0.5f + Mathf.Abs(dir[i].z) * tamTorre.y * 0.5f;
                for (int e = 0; e < 2; e++)
                {
                    Vector3 p = Extremo(segmentos[i], e);
                    Vector3 adentro = e == 0 ? dir[i] : -dir[i];
                    float recorte;
                    if (esquina[i, e])
                    {
                        AgregarTorre(plan, p, true);
                        recorte = mitad;
                    }
                    else
                    {
                        AgregarTorre(plan, p + adentro * mitad, false);
                        recorte = mitad * 2f;
                    }
                    if (e == 0)
                    {
                        desde[i] = recorte;
                    }
                    else
                    {
                        hasta[i] = recorte;
                    }
                }
                float largo = Distancia(segmentos[i].A, segmentos[i].B);
                Tramo t = new Tramo();
                t.Dir = dir[i];
                t.Inicio = segmentos[i].A + dir[i] * desde[i];
                t.Fin = segmentos[i].B - dir[i] * hasta[i];
                t.Largo = Mathf.Max(0f, largo - desde[i] - hasta[i]);
                plan.Tramos.Add(t);
            }

            // Puertas: cada hueco entre dos extremos sueltos de segmentos alineados.
            for (int i = 0; i < n; i++)
            {
                for (int e = 0; e < 2; e++)
                {
                    if (esquina[i, e])
                    {
                        continue;
                    }
                    Vector3 p = Extremo(segmentos[i], e);
                    Vector3 afuera = e == 0 ? -dir[i] : dir[i];
                    int mejorJ = -1;
                    Vector3 mejorQ = Vector3.zero;
                    float mejorD = float.MaxValue;
                    for (int j = i + 1; j < n; j++)
                    {
                        if (Mathf.Abs(Vector3.Dot(dir[i], dir[j])) < 0.99f)
                        {
                            continue;
                        }
                        for (int f = 0; f < 2; f++)
                        {
                            if (esquina[j, f])
                            {
                                continue;
                            }
                            Vector3 q = Extremo(segmentos[j], f);
                            Vector3 v = q - p;
                            v.y = 0f;
                            float adelante = Vector3.Dot(v, afuera);
                            float lateral = Mathf.Abs(Vector3.Dot(v, new Vector3(-afuera.z, 0f, afuera.x)));
                            if (adelante > 0.5f && lateral < 0.5f && adelante < mejorD)
                            {
                                mejorD = adelante;
                                mejorJ = j;
                                mejorQ = q;
                            }
                        }
                    }
                    if (mejorJ >= 0)
                    {
                        Puerta pu = new Puerta();
                        pu.Centro = (p + mejorQ) * 0.5f;
                        pu.Dir = dir[i];
                        pu.Ancho = mejorD;
                        plan.Puertas.Add(pu);
                    }
                }
            }
            return plan;
        }

        /// <summary>
        /// Reparte un tramo en piezas alternando _a y _b (largos ya a la escala de altura). Elige el número de piezas cuyo factor
        /// de largo queda más cerca de 1; DentroDelAjuste dice si cabe en ±15 %.
        /// </summary>
        public static Reparto Repartir(float largo, float largoA, float largoB)
        {
            Reparto mejor = null;
            float mejorError = float.MaxValue;
            int max = Mathf.Max(1, Mathf.CeilToInt(largo / Mathf.Min(largoA, largoB)) + 1);
            for (int n = 1; n <= max; n++)
            {
                float suma = 0f;
                for (int k = 0; k < n; k++)
                {
                    suma += k % 2 == 0 ? largoA : largoB;
                }
                float factor = largo / suma;
                float error = Mathf.Abs(factor - 1f);
                if (error < mejorError)
                {
                    mejorError = error;
                    Reparto r = new Reparto();
                    for (int k = 0; k < n; k++)
                    {
                        r.EsB.Add(k % 2 == 1);
                    }
                    r.Factor = factor;
                    mejor = r;
                }
            }
            mejor.DentroDelAjuste = mejorError <= AjusteMaximo + 0.0001f;
            return mejor;
        }

        public static Vector3 Extremo(Segmento s, int e)
        {
            return e == 0 ? s.A : s.B;
        }

        private static float Distancia(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private static void AgregarTorre(Plan plan, Vector3 centro, bool esquina)
        {
            foreach (Torre t in plan.Torres)
            {
                if (Distancia(t.Centro, centro) < RadioEsquina)
                {
                    return;
                }
            }
            Torre nueva = new Torre();
            nueva.Centro = new Vector3(centro.x, 0f, centro.z);
            nueva.Esquina = esquina;
            plan.Torres.Add(nueva);
        }
    }
}

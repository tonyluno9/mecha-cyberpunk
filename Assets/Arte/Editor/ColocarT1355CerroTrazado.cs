using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1355: el trazado puro de la Colonia del Cerro (sin escena ni assets, para probarlo): calle base, zigzag, plaza de arriba, conexión U4,
    /// y el plan de alturas de las azoteas. x = este, z = norte, y = arriba.
    /// El zigzag sale de la calle base (-305, 0, 150), sube al norte, da media vuelta (radio 27.5 m) y sale en (-360, 10, 185) hacia el sur
    /// (la salida del túnel, U6), pasa por (-360, 10, 175), baja al sur 205 m, da otra media vuelta al oeste y sube al norte hasta una plaza redonda arriba.
    /// </summary>
    public static class CerroT1355Trazado
    {
        public const float XBase = -305f;
        public const float ZBaseSur = -120f;
        public const float ZBaseNorte = 150f;
        public const float RadioCurva = 27.5f;
        public const float RadioPlaza = 26f;
        public const float AnchoCalle = 9f;
        public const float AnchoBanqueta = 1.5f;
        /// <summary>Pendiente (m de subida por m de calle) del zigzag después de la salida del túnel.</summary>
        public const float PendienteSubida = 0.05f;
        public const float PendienteMaxima = 0.09f;
        public const float RadioMinimo = 25f;
        public const float PasoMuestra = 2f;

        public const float XPierna2 = -360f;
        public const float ZPierna2Norte = 185f;
        public const float ZPierna2Sur = -30f;
        public const float XPierna3 = XPierna2 - 2f * RadioCurva;
        public const float ZPierna3Norte = 40f;

        public static readonly Vector3 PuntoU4Corredor = new Vector3(-249f, 0f, 0f);
        public static readonly Vector3 PuntoU4Base = new Vector3(XBase, 0f, 0f);
        public static readonly Vector3 PuntoU6Calle = new Vector3(XPierna2, 10f, 175f);
        public static readonly Vector3 PuntoU6Salida = new Vector3(XPierna2, 10f, ZPierna2Norte);
        public static readonly Vector3 CentroPlaza = new Vector3(XPierna3 - RadioPlaza, 0f, ZPierna3Norte);

        /// <summary>Límites de azoteas que pide el parkour (TreparAPie: tope horizontal de 0.5 a 2.3 m sobre los pies).</summary>
        public const float EscalonMinimo = 0.5f;
        public const float EscalonMaximo = 2.3f;
        /// <summary>El plan de azoteas se queda dentro de esta banda (más estrecha que la del parkour, con margen).</summary>
        public const float EscalonPlanMinimo = 0.6f;
        public const float EscalonPlanMaximo = 2.2f;

        /// <summary>Calle base x = -305, de z = -120 a 150, plana (y = 0).</summary>
        public static List<Vector3> CalleBase()
        {
            List<Vector3> p = new List<Vector3>();
            Recta(p, new Vector3(XBase, 0f, ZBaseSur), new Vector3(XBase, 0f, ZBaseNorte));
            return p;
        }

        /// <summary>U4: del corredor oeste (-249, 0, 0) a la calle base (-305, 0, 0).</summary>
        public static List<Vector3> ConexionU4()
        {
            List<Vector3> p = new List<Vector3>();
            Recta(p, PuntoU4Corredor, PuntoU4Base);
            return p;
        }

        /// <summary>El zigzag completo con su altura, de la calle base a la plaza redonda de arriba (termina donde empieza la vuelta de la plaza).</summary>
        public static List<Vector3> Zigzag()
        {
            List<Vector3> p = new List<Vector3>();
            Recta(p, new Vector3(XBase, 0f, ZBaseNorte), new Vector3(XBase, 0f, ZPierna2Norte));
            Arco(p, new Vector3(XBase - RadioCurva, 0f, ZPierna2Norte), RadioCurva, 0f, Mathf.PI);
            int iSalida = p.Count - 1;
            Recta(p, new Vector3(XPierna2, 0f, ZPierna2Norte), PuntoU6Calle);
            int iPunto = p.Count - 1;
            Recta(p, PuntoU6Calle, new Vector3(XPierna2, 0f, ZPierna2Sur));
            Arco(p, new Vector3(XPierna2 - RadioCurva, 0f, ZPierna2Sur), RadioCurva, 0f, -Mathf.PI);
            Recta(p, new Vector3(XPierna3, 0f, ZPierna2Sur), new Vector3(XPierna3, 0f, ZPierna3Norte));
            int iPlaza = p.Count - 1;
            Arco(p, CentroPlaza, RadioPlaza, 0f, 2f * Mathf.PI);

            float[] largo = new float[p.Count];
            for (int i = 1; i < p.Count; i++)
            {
                Vector3 d = p[i] - p[i - 1];
                largo[i] = largo[i - 1] + new Vector2(d.x, d.z).magnitude;
            }
            float yPlaza = 0f;
            for (int i = 0; i < p.Count; i++)
            {
                float y;
                if (i <= iSalida)
                {
                    y = PuntoU6Salida.y * largo[i] / largo[iSalida];
                }
                else if (i <= iPunto)
                {
                    y = PuntoU6Calle.y;
                }
                else if (i <= iPlaza)
                {
                    y = PuntoU6Calle.y + PendienteSubida * (largo[i] - largo[iPunto]);
                    yPlaza = y;
                }
                else
                {
                    y = yPlaza;
                }
                p[i] = new Vector3(p[i].x, y, p[i].z);
            }
            return p;
        }

        /// <summary>Altura de la calle en la pierna 2 (x = -360) a la altura z (de 185 a -30).</summary>
        public static float AlturaPierna2(float z)
        {
            return PuntoU6Calle.y + PendienteSubida * Mathf.Max(0f, PuntoU6Calle.z - z);
        }

        /// <summary>Altura de la calle en la pierna 3 (x = -415) a la altura z (de -30 a 40).</summary>
        public static float AlturaPierna3(float z)
        {
            float arco = Mathf.PI * RadioCurva;
            return AlturaPierna2(ZPierna2Sur) + PendienteSubida * (arco + (z - ZPierna2Sur));
        }

        /// <summary>Altura de la plaza redonda de arriba.</summary>
        public static float AlturaPlaza()
        {
            return AlturaPierna3(ZPierna3Norte);
        }

        /// <summary>Pendiente máxima (valor absoluto, y sobre distancia en planta) entre muestras vecinas.</summary>
        public static float PendienteMaximaDe(IList<Vector3> p)
        {
            float peor = 0f;
            for (int i = 1; i < p.Count; i++)
            {
                Vector3 d = p[i] - p[i - 1];
                float h = new Vector2(d.x, d.z).magnitude;
                if (h > 0.0001f)
                {
                    peor = Mathf.Max(peor, Mathf.Abs(d.y) / h);
                }
            }
            return peor;
        }

        /// <summary>Radio de curva más chico (m): círculo por tres muestras con una de por medio (las rectas dan infinito).</summary>
        public static float RadioMinimoDe(IList<Vector3> p)
        {
            float menor = float.MaxValue;
            for (int i = 2; i < p.Count; i++)
            {
                Vector2 a = new Vector2(p[i - 2].x, p[i - 2].z);
                Vector2 b = new Vector2(p[i - 1].x, p[i - 1].z);
                Vector2 c = new Vector2(p[i].x, p[i].z);
                float lab = (b - a).magnitude, lbc = (c - b).magnitude, lca = (a - c).magnitude;
                float area2 = Mathf.Abs((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x));
                if (area2 > 0.0001f)
                {
                    menor = Mathf.Min(menor, lab * lbc * lca / (2f * area2));
                }
            }
            return menor;
        }

        /// <summary>Distancia en planta de <paramref name="punto"/> a la muestra más cercana, y su diferencia de altura.</summary>
        public static float DistanciaAPunto(IList<Vector3> p, Vector3 punto, out float difY)
        {
            float mejor = float.MaxValue;
            difY = 0f;
            foreach (Vector3 q in p)
            {
                float d = new Vector2(q.x - punto.x, q.z - punto.z).magnitude;
                if (d < mejor)
                {
                    mejor = d;
                    difY = q.y - punto.y;
                }
            }
            return mejor;
        }

        /// <summary>
        /// Plan de altura de una fila de casas pegadas (una cadena de azoteas): <paramref name="yCalle"/> es la altura de la calle frente a cada casa,
        /// <paramref name="alto"/> lo alto de cada casa. Devuelve cuánto se levanta cada casa sobre la calle (<c>e</c>) para que el escalón entre
        /// azoteas vecinas (calle + e + alto) quede en [<see cref="EscalonPlanMinimo"/>, <see cref="EscalonPlanMaximo"/>] y e dentro de [eMin, eMax].
        /// </summary>
        public static float[] PlanAzoteas(IList<float> yCalle, IList<float> alto, int semilla, float eMin, float eMax)
        {
            int n = yCalle.Count;
            float[] e = new float[n];
            System.Random azar = new System.Random(semilla);
            e[0] = eMin + (float)azar.NextDouble() * (eMax - eMin) * 0.5f;
            for (int i = 1; i < n; i++)
            {
                float cambio = (yCalle[i] + alto[i]) - (yCalle[i - 1] + alto[i - 1]);
                float signo = azar.NextDouble() < 0.5 ? 1f : -1f;
                float magnitud = EscalonPlanMinimo + (float)azar.NextDouble() * (EscalonPlanMaximo - EscalonPlanMinimo);
                bool puesto = false;
                for (int intento = 0; intento < 2 && !puesto; intento++)
                {
                    float candidato = e[i - 1] + signo * magnitud - cambio;
                    if (candidato >= eMin && candidato <= eMax)
                    {
                        e[i] = candidato;
                        puesto = true;
                    }
                    signo = -signo;
                }
                for (float m = EscalonPlanMinimo; m <= EscalonPlanMaximo + 0.001f && !puesto; m += 0.05f)
                {
                    foreach (float s in new[] { 1f, -1f })
                    {
                        float candidato = e[i - 1] + s * m - cambio;
                        if (!puesto && candidato >= eMin && candidato <= eMax)
                        {
                            e[i] = candidato;
                            puesto = true;
                        }
                    }
                }
                if (!puesto)
                {
                    // Sin escalón dentro del rango (la calle sube más de lo que cabe): se acerca lo más posible; esa casa no se trepa desde la vecina.
                    e[i] = Math.Max(eMin, Math.Min(eMax, e[i - 1] + signo * magnitud - cambio));
                }
            }
            return e;
        }

        /// <summary>Escalón entre cada azotea y la anterior (con signo).</summary>
        public static float[] Escalones(IList<float> yCalle, IList<float> alto, IList<float> e)
        {
            float[] r = new float[Mathf.Max(0, yCalle.Count - 1)];
            for (int i = 1; i < yCalle.Count; i++)
            {
                r[i - 1] = (yCalle[i] + e[i] + alto[i]) - (yCalle[i - 1] + e[i - 1] + alto[i - 1]);
            }
            return r;
        }

        // ---------------------------------------------------------------- Muestreo

        /// <summary>Agrega la recta de a a b cada <see cref="PasoMuestra"/> (incluye b; a solo si la lista está vacía).</summary>
        private static void Recta(List<Vector3> p, Vector3 a, Vector3 b)
        {
            if (p.Count == 0)
            {
                p.Add(a);
            }
            float largo = Vector3.Distance(a, b);
            int n = Mathf.Max(1, Mathf.CeilToInt(largo / PasoMuestra - 0.0001f));
            for (int k = 1; k <= n; k++)
            {
                p.Add(Vector3.Lerp(a, b, k / (float)n));
            }
        }

        /// <summary>Agrega un arco de centro, radio y ángulos (rad; crece = contra el reloj); no repite el primer punto.</summary>
        private static void Arco(List<Vector3> p, Vector3 centro, float radio, float desde, float delta)
        {
            int n = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(delta) * radio / PasoMuestra));
            for (int k = 1; k <= n; k++)
            {
                float a = desde + delta * k / n;
                p.Add(new Vector3(centro.x + radio * Mathf.Cos(a), 0f, centro.z + radio * Mathf.Sin(a)));
            }
        }
    }
}

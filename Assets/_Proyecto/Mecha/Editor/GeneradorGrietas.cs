using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Mecha.Editor
{
    /// <summary>
    /// Geometría de las grietas del cristal en coordenadas locales del quad (x e y entre -0.5 y 0.5). Pura y determinista:
    /// la misma semilla da siempre los mismos puntos. Ningún punto ni tramo entra en el rectángulo central.
    /// </summary>
    public static class GeneradorGrietas
    {
        /// <summary>Mitad del rectángulo central libre: 40 % del ancho y del alto del cristal.</summary>
        public const float MitadCentroLibre = 0.2f;

        private const int Semilla = 10500;
        private const float MargenGrieta = 0.06f;
        private const float MitadCristal = 0.5f;
        private const float PasoMin = 0.05f;
        private const float PasoMax = 0.09f;
        private const float DesvioMaximo = 0.35f;
        private const float AvanceMuestreo = 0.01f;
        private const int PuntosMaximos = 6;
        private const int RayosEstrella = 7;
        private const float RayoMin = 0.05f;
        private const float RayoMax = 0.13f;
        private const float JitterBorde = 0.04f;
        private static readonly Vector2 CentroEstrella = new Vector2(0.34f, 0.30f);

        // Puntos de entrada en las orillas y esquinas (x, y), nivel 1 y nivel 2.
        private static readonly Vector2[] EntradasNivel1 =
        {
            new Vector2(-0.5f, 0.12f), new Vector2(0.18f, 0.5f), new Vector2(0.5f, -0.22f), new Vector2(-0.3f, -0.5f)
        };

        private static readonly Vector2[] EntradasNivel2 =
        {
            new Vector2(-0.5f, 0.5f), new Vector2(0.5f, 0.2f), new Vector2(0.5f, -0.5f),
            new Vector2(-0.5f, -0.3f), new Vector2(0.1f, -0.5f), new Vector2(-0.2f, 0.5f)
        };

        /// <summary>True si el punto cae en el rectángulo central (con el margen extra que se pida).</summary>
        public static bool EnCentro(Vector2 punto, float margen)
        {
            float limite = MitadCentroLibre + margen;
            return Mathf.Abs(punto.x) < limite && Mathf.Abs(punto.y) < limite;
        }

        /// <summary>True si el segmento a-b toca el rectángulo central (se muestrea cada centímetro).</summary>
        public static bool SegmentoEnCentro(Vector2 a, Vector2 b)
        {
            int pasos = Mathf.CeilToInt((b - a).magnitude / AvanceMuestreo);
            for (int i = 0; i <= pasos; i++)
            {
                if (EnCentro(Vector2.Lerp(a, b, pasos == 0 ? 0f : (float)i / pasos), 0f))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Grietas leves: cada una es una polilínea que entra desde una orilla hacia el centro y se detiene antes de él.</summary>
        public static List<Vector2[]> Nivel1()
        {
            return Polilineas(EntradasNivel1, Semilla);
        }

        /// <summary>Grietas fuertes: más grietas desde otras orillas y una estrella de impacto en una esquina superior.</summary>
        public static List<Vector2[]> Nivel2()
        {
            List<Vector2[]> grietas = Polilineas(EntradasNivel2, Semilla + 1);
            grietas.AddRange(Estrella(new System.Random(Semilla + 2)));
            return grietas;
        }

        private static List<Vector2[]> Polilineas(Vector2[] entradas, int semilla)
        {
            System.Random azar = new System.Random(semilla);
            List<Vector2[]> resultado = new List<Vector2[]>();
            foreach (Vector2 entrada in entradas)
            {
                resultado.Add(Camino(entrada, azar));
            }
            return resultado;
        }

        private static Vector2[] Camino(Vector2 entrada, System.Random azar)
        {
            Vector2 inicio = JitterEnBorde(entrada, azar);
            Vector2 objetivo = new Vector2(Rango(azar, -0.05f, 0.05f), Rango(azar, -0.05f, 0.05f));
            Vector2 direccion = (objetivo - inicio).normalized;
            List<Vector2> puntos = new List<Vector2> { inicio };
            Vector2 actual = inicio;
            while (puntos.Count < PuntosMaximos)
            {
                float desvio = Rango(azar, -DesvioMaximo, DesvioMaximo);
                direccion = Rotar(direccion, desvio);
                Vector2 siguiente = actual + direccion * Rango(azar, PasoMin, PasoMax);
                if (EnCentro(siguiente, MargenGrieta) || SegmentoEnCentro(actual, siguiente)
                    || Mathf.Abs(siguiente.x) > MitadCristal || Mathf.Abs(siguiente.y) > MitadCristal)
                {
                    break;
                }
                puntos.Add(siguiente);
                actual = siguiente;
            }
            return puntos.ToArray();
        }

        private static List<Vector2[]> Estrella(System.Random azar)
        {
            List<Vector2[]> rayos = new List<Vector2[]>();
            float fase = Rango(azar, 0f, 360f / RayosEstrella);
            for (int i = 0; i < RayosEstrella; i++)
            {
                float angulo = fase + i * 360f / RayosEstrella + Rango(azar, -10f, 10f);
                Vector2 direccion = Rotar(Vector2.right, angulo * Mathf.Deg2Rad);
                float largo = Rango(azar, RayoMin, RayoMax);
                Vector2 fin = CentroEstrella + direccion * largo;
                while (largo > 0.01f && (EnCentro(fin, MargenGrieta) || SegmentoEnCentro(CentroEstrella, fin)
                    || Mathf.Abs(fin.x) > MitadCristal || Mathf.Abs(fin.y) > MitadCristal))
                {
                    largo *= 0.8f;
                    fin = CentroEstrella + direccion * largo;
                }
                rayos.Add(new[] { CentroEstrella, fin });
            }
            return rayos;
        }

        private static Vector2 JitterEnBorde(Vector2 entrada, System.Random azar)
        {
            Vector2 p = entrada;
            if (Mathf.Abs(p.x) >= MitadCristal && Mathf.Abs(p.y) < MitadCristal)
            {
                p.y = Mathf.Clamp(p.y + Rango(azar, -JitterBorde, JitterBorde), -MitadCristal, MitadCristal);
            }
            else if (Mathf.Abs(p.y) >= MitadCristal && Mathf.Abs(p.x) < MitadCristal)
            {
                p.x = Mathf.Clamp(p.x + Rango(azar, -JitterBorde, JitterBorde), -MitadCristal, MitadCristal);
            }
            return p;
        }

        private static Vector2 Rotar(Vector2 v, float radianes)
        {
            float c = Mathf.Cos(radianes);
            float s = Mathf.Sin(radianes);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        private static float Rango(System.Random azar, float min, float max)
        {
            return min + (float)azar.NextDouble() * (max - min);
        }
    }
}

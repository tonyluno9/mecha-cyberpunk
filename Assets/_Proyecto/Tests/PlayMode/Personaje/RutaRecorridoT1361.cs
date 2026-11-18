using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-1361: una línea de puntos en el mundo (el trazado de una calle, un pasillo, una escalera) con largo acumulado.
    /// Sirve para saber en qué parte del recorrido va el jugador o el carro y hacia dónde mirar. Solo mide en planta (x, z).
    /// </summary>
    public sealed class RutaRecorridoT1361
    {
        private readonly List<Vector3> puntos;
        private readonly float[] acumulado;

        public RutaRecorridoT1361(IList<Vector3> origen)
        {
            puntos = new List<Vector3>();
            for (int i = 0; i < origen.Count; i++)
            {
                // Se descartan puntos repetidos: un tramo de largo cero no tiene dirección.
                if (puntos.Count == 0 || Plano(puntos[puntos.Count - 1], origen[i]) > 0.01f)
                {
                    puntos.Add(origen[i]);
                }
            }
            acumulado = new float[puntos.Count];
            for (int i = 1; i < puntos.Count; i++)
            {
                acumulado[i] = acumulado[i - 1] + Plano(puntos[i - 1], puntos[i]);
            }
        }

        public IReadOnlyList<Vector3> Puntos => puntos;
        public float Largo => acumulado.Length == 0 ? 0f : acumulado[acumulado.Length - 1];
        public Vector3 Inicio => puntos[0];
        public Vector3 Final => puntos[puntos.Count - 1];

        public static float Plano(Vector3 a, Vector3 b)
        {
            return new Vector2(a.x - b.x, a.z - b.z).magnitude;
        }

        /// <summary>El punto a <paramref name="s"/> metros del inicio (se acota a 0..Largo).</summary>
        public Vector3 PuntoEn(float s)
        {
            if (puntos.Count == 1)
            {
                return puntos[0];
            }
            s = Mathf.Clamp(s, 0f, Largo);
            for (int i = 1; i < puntos.Count; i++)
            {
                if (s <= acumulado[i])
                {
                    float largo = acumulado[i] - acumulado[i - 1];
                    float t = largo > 0f ? (s - acumulado[i - 1]) / largo : 0f;
                    return Vector3.Lerp(puntos[i - 1], puntos[i], t);
                }
            }
            return Final;
        }

        /// <summary>Dirección horizontal del recorrido en <paramref name="s"/>.</summary>
        public Vector3 Direccion(float s)
        {
            Vector3 a = PuntoEn(s - 0.5f);
            Vector3 b = PuntoEn(s + 0.5f);
            Vector3 d = b - a;
            d.y = 0f;
            return d.sqrMagnitude > 0f ? d.normalized : Vector3.forward;
        }

        /// <summary>
        /// La s del punto de la ruta más cercano a <paramref name="posicion"/> (en planta) dentro de la ventana [desde, hasta].
        /// La ventana evita saltar a otro tramo que pasa cerca (las piernas del zigzag, el circuito del puerto).
        /// </summary>
        public float Proyectar(Vector3 posicion, float desde, float hasta, out float distancia)
        {
            float mejorS = Mathf.Clamp(desde, 0f, Largo);
            float mejor = float.MaxValue;
            for (int i = 1; i < puntos.Count; i++)
            {
                if (acumulado[i] < desde || acumulado[i - 1] > hasta)
                {
                    continue;
                }
                Vector2 a = new Vector2(puntos[i - 1].x, puntos[i - 1].z);
                Vector2 b = new Vector2(puntos[i].x, puntos[i].z);
                Vector2 p = new Vector2(posicion.x, posicion.z);
                Vector2 ab = b - a;
                float largo2 = ab.sqrMagnitude;
                float t = largo2 > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / largo2) : 0f;
                float s = acumulado[i - 1] + t * (acumulado[i] - acumulado[i - 1]);
                if (s < desde || s > hasta)
                {
                    s = Mathf.Clamp(s, desde, hasta);
                    Vector3 q = PuntoEn(s);
                    float d0 = Plano(q, posicion);
                    if (d0 < mejor)
                    {
                        mejor = d0;
                        mejorS = s;
                    }
                    continue;
                }
                float d = (a + ab * t - p).magnitude;
                if (d < mejor)
                {
                    mejor = d;
                    mejorS = s;
                }
            }
            distancia = mejor;
            return mejorS;
        }

        public float Proyectar(Vector3 posicion, float desde, float hasta)
        {
            return Proyectar(posicion, desde, hasta, out float _);
        }

        /// <summary>La ruta entre <paramref name="s0"/> y <paramref name="s1"/>, con los puntos intermedios originales.</summary>
        public RutaRecorridoT1361 Tramo(float s0, float s1)
        {
            List<Vector3> lista = new List<Vector3> { PuntoEn(s0) };
            for (int i = 0; i < puntos.Count; i++)
            {
                if (acumulado[i] > s0 + 0.01f && acumulado[i] < s1 - 0.01f)
                {
                    lista.Add(puntos[i]);
                }
            }
            lista.Add(PuntoEn(s1));
            return new RutaRecorridoT1361(lista);
        }

        /// <summary>La ruta partida en <paramref name="partes"/> tramos de igual largo.</summary>
        public List<RutaRecorridoT1361> Partir(int partes)
        {
            List<RutaRecorridoT1361> lista = new List<RutaRecorridoT1361>();
            for (int i = 0; i < partes; i++)
            {
                lista.Add(Tramo(Largo * i / partes, Largo * (i + 1) / partes));
            }
            return lista;
        }

        public RutaRecorridoT1361 Invertida()
        {
            List<Vector3> lista = new List<Vector3>(puntos);
            lista.Reverse();
            return new RutaRecorridoT1361(lista);
        }

        /// <summary>Une rutas en el orden dado, con un tramo recto entre el final de una y el inicio de la siguiente.</summary>
        public static RutaRecorridoT1361 Unir(IList<RutaRecorridoT1361> rutas)
        {
            List<Vector3> lista = new List<Vector3>();
            foreach (RutaRecorridoT1361 ruta in rutas)
            {
                lista.AddRange(ruta.puntos);
            }
            return new RutaRecorridoT1361(lista);
        }
    }
}

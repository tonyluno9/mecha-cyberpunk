using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1524: cuentas puras (sin escena) del perímetro del muro: los cuatro lados del rectángulo, las muestras cada metro
    /// y el agrupado de muestras sin collider en huecos continuos. Todo en el plano XZ.
    /// </summary>
    public static class CalculoMuroT1524
    {
        /// <summary>Separación entre muestras del perímetro (m).</summary>
        public const float Paso = 1f;

        /// <summary>Lo que se deja libre junto a cada esquina para que el rayo no nazca dentro del muro vecino (m).</summary>
        public const float MargenEsquina = 1.5f;

        /// <summary>Un lado del rectángulo del muro: Centro sobre la línea del muro, Eje a lo largo, Fuera hacia afuera.</summary>
        public struct Lado
        {
            public string Nombre;
            public Vector3 Centro;
            public Vector3 Eje;
            public Vector3 Fuera;
            public float Mitad;

            public Vector3 Punto(float t)
            {
                return Centro + Eje * t;
            }
        }

        /// <summary>Tramo continuo de muestras sin collider, de TIni a TFin a lo largo del eje del lado.</summary>
        public struct Hueco
        {
            public int Lado;
            public float TIni;
            public float TFin;

            public float Largo
            {
                get { return TFin - TIni; }
            }

            public float Medio
            {
                get { return (TIni + TFin) * 0.5f; }
            }
        }

        /// <summary>Los cuatro lados del rectángulo cuyas líneas centrales son x = minX, x = maxX, z = minZ, z = maxZ.</summary>
        public static Lado[] Lados(float minX, float maxX, float minZ, float maxZ)
        {
            float cx = (minX + maxX) * 0.5f;
            float cz = (minZ + maxZ) * 0.5f;
            float mx = (maxX - minX) * 0.5f;
            float mz = (maxZ - minZ) * 0.5f;
            return new[]
            {
                new Lado { Nombre = "Este", Centro = new Vector3(maxX, 0f, cz), Eje = Vector3.forward, Fuera = Vector3.right, Mitad = mz },
                new Lado { Nombre = "Oeste", Centro = new Vector3(minX, 0f, cz), Eje = Vector3.forward, Fuera = Vector3.left, Mitad = mz },
                new Lado { Nombre = "Norte", Centro = new Vector3(cx, 0f, maxZ), Eje = Vector3.right, Fuera = Vector3.forward, Mitad = mx },
                new Lado { Nombre = "Sur", Centro = new Vector3(cx, 0f, minZ), Eje = Vector3.right, Fuera = Vector3.back, Mitad = mx },
            };
        }

        /// <summary>Posiciones t de las muestras de un lado (de -Mitad+Margen a Mitad-Margen cada Paso).</summary>
        public static List<float> Muestras(Lado lado)
        {
            List<float> lista = new List<float>();
            float limite = lado.Mitad - MargenEsquina;
            for (float t = -limite; t <= limite + 0.0001f; t += Paso)
            {
                lista.Add(t);
            }
            return lista;
        }

        /// <summary>Agrupa las muestras sin cerrar en huecos continuos; cada hueco se ensancha medio paso por lado.</summary>
        public static List<Hueco> Huecos(int lado, IList<float> t, IList<bool> cerrado)
        {
            List<Hueco> huecos = new List<Hueco>();
            int i = 0;
            while (i < t.Count)
            {
                if (cerrado[i])
                {
                    i++;
                    continue;
                }
                int j = i;
                while (j + 1 < t.Count && !cerrado[j + 1])
                {
                    j++;
                }
                huecos.Add(new Hueco { Lado = lado, TIni = t[i] - Paso * 0.5f, TFin = t[j] + Paso * 0.5f });
                i = j + 1;
            }
            return huecos;
        }

        /// <summary>El hueco es de una puerta si su centro cae dentro del vano (ancho + holgura) alrededor de centroPuerta.</summary>
        public static bool EnPuerta(Vector3 centroHueco, Vector3 centroPuerta, float anchoPuerta, float holgura)
        {
            Vector3 d = centroHueco - centroPuerta;
            d.y = 0f;
            return d.magnitude <= anchoPuerta * 0.5f + holgura;
        }
    }
}

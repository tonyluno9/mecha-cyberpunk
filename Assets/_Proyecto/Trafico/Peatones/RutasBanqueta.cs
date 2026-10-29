using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Peatones
{
    /// <summary>Las rutas de los peatones por las banquetas (T-331): el contorno de una manzana cuadrada, a media banqueta.</summary>
    public static class RutasBanqueta
    {
        private const int PuntosDelContorno = 5;
        private const int LadosDeUnaManzana = 4;
        private const float Tolerancia = 0.01f;

        /// <summary>
        /// Una ruta por manzana. Las manzanas de los extremos no caminan por el lado que da al fin del barrio, donde están los cierres (vallas, retenes
        /// y pórticos): su ruta queda abierta y sus puntas se acortan <paramref name="recorteCierres"/> metros. Las demás dan la vuelta completa.
        /// </summary>
        public static List<Vector3[]> DeManzanas(IReadOnlyList<Vector3> centros, float ladoCentral, float alturaPie, float recorteCierres)
        {
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (Vector3 c in centros)
            {
                minX = Mathf.Min(minX, c.x);
                maxX = Mathf.Max(maxX, c.x);
                minZ = Mathf.Min(minZ, c.z);
                maxZ = Mathf.Max(maxZ, c.z);
            }
            List<Vector3[]> rutas = new List<Vector3[]>();
            foreach (Vector3 c in centros)
            {
                Vector3[] vuelta = Contorno(c, ladoCentral, alturaPie);
                // Lados en el orden de Contorno: oeste, norte, este, sur.
                bool[] exterior =
                {
                    Mathf.Abs(c.x - minX) < Tolerancia, Mathf.Abs(c.z - maxZ) < Tolerancia,
                    Mathf.Abs(c.x - maxX) < Tolerancia, Mathf.Abs(c.z - minZ) < Tolerancia
                };
                rutas.Add(Abrir(vuelta, exterior, recorteCierres));
            }
            return rutas;
        }

        // Quita los lados exteriores, se queda con la cadena más larga de lados seguidos y acorta sus dos puntas.
        private static Vector3[] Abrir(Vector3[] vuelta, bool[] exterior, float recorte)
        {
            int mejorInicio = -1;
            int mejorLargo = 0;
            for (int inicio = 0; inicio < LadosDeUnaManzana; inicio++)
            {
                if (exterior[(inicio + LadosDeUnaManzana - 1) % LadosDeUnaManzana] == false)
                {
                    continue;   // el lado anterior no es exterior: no es el comienzo de una cadena
                }
                int largo = 0;
                while (largo < LadosDeUnaManzana && !exterior[(inicio + largo) % LadosDeUnaManzana])
                {
                    largo++;
                }
                if (largo > mejorLargo)
                {
                    mejorLargo = largo;
                    mejorInicio = inicio;
                }
            }
            if (mejorInicio < 0)
            {
                return vuelta;
            }
            List<Vector3> puntos = new List<Vector3>();
            for (int i = 0; i <= mejorLargo; i++)
            {
                puntos.Add(vuelta[(mejorInicio + i) % LadosDeUnaManzana]);
            }
            puntos[0] = Vector3.MoveTowards(puntos[0], puntos[1], recorte);
            puntos[puntos.Count - 1] = Vector3.MoveTowards(puntos[puntos.Count - 1], puntos[puntos.Count - 2], recorte);
            return puntos.ToArray();
        }

        /// <summary>
        /// Cinco puntos que dan la vuelta a la manzana (la esquina de salida se repite al final), a <paramref name="ladoCentral"/> metros del
        /// centro de la manzana y a la altura <paramref name="alturaPie"/> (m). Cuatro tramos de un lado cada uno.
        /// </summary>
        public static Vector3[] Contorno(Vector3 centroManzana, float ladoCentral, float alturaPie)
        {
            float y = centroManzana.y + alturaPie;
            Vector3[] puntos = new Vector3[PuntosDelContorno];
            puntos[0] = new Vector3(centroManzana.x - ladoCentral, y, centroManzana.z - ladoCentral);
            puntos[1] = new Vector3(centroManzana.x - ladoCentral, y, centroManzana.z + ladoCentral);
            puntos[2] = new Vector3(centroManzana.x + ladoCentral, y, centroManzana.z + ladoCentral);
            puntos[3] = new Vector3(centroManzana.x + ladoCentral, y, centroManzana.z - ladoCentral);
            puntos[4] = puntos[0];
            return puntos;
        }

        /// <summary>Dónde está un peatón sobre la ruta: del punto de salida del tramo al de llegada, según su sentido y fracción.</summary>
        public static Vector3 Posicion(Vector3[] puntos, CalculoPeaton calculo)
        {
            int i = calculo.Segmento;
            Vector3 salida = calculo.Sentido > 0 ? puntos[i] : puntos[i + 1];
            Vector3 llegada = calculo.Sentido > 0 ? puntos[i + 1] : puntos[i];
            return Vector3.Lerp(salida, llegada, calculo.Fraccion);
        }

        /// <summary>Largo (m) de cada tramo entre puntos consecutivos.</summary>
        public static float[] Largos(Vector3[] puntos)
        {
            float[] largos = new float[puntos.Length - 1];
            for (int i = 0; i < largos.Length; i++)
            {
                largos[i] = Vector3.Distance(puntos[i], puntos[i + 1]);
            }
            return largos;
        }
    }
}

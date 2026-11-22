using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Calle
{
    /// <summary>
    /// T-1603: contadores del tráfico ordenado para leer en Play y en pruebas: choques entre carros del tráfico (sus cajas se traslapan) y carros atorados más de
    /// <see cref="SegundosAtorado"/> s. Con <see cref="RegistrarEnArchivo"/> en true cada evento también se anota en Logs/trafico-ordenado.txt.
    /// </summary>
    public static class EstadisticasTrafico
    {
        /// <summary>Un carro parado por obstáculo más de esto cuenta como atorado (s).</summary>
        public const float SegundosAtorado = 8f;
        /// <summary>Mitad del largo de la caja de un carro para detectar choques (m).</summary>
        public const float MitadLargo = 2.2f;
        /// <summary>Mitad del ancho de la caja de un carro para detectar choques (m).</summary>
        public const float MitadAncho = 0.9f;
        /// <summary>Cada cuántos segundos se revisan los traslapes.</summary>
        public const float SegundosEntreRevisiones = 0.2f;
        /// <summary>Dos carros a más de esta distancia (m) en planta no se revisan.</summary>
        public const float DistanciaMaximaDeRevision = 6f;
        public const string RutaRegistro = "Logs/trafico-ordenado.txt";

        private static readonly HashSet<long> parejasEnChoque = new HashSet<long>();
        private static readonly HashSet<long> parejasAhora = new HashSet<long>();
        private static float proximaRevision;

        /// <summary>Episodios de choque entre dos carros del tráfico desde que se reinició.</summary>
        public static int Choques { get; private set; }

        /// <summary>Carros que estuvieron atorados más de <see cref="SegundosAtorado"/> s (una vez por atorón).</summary>
        public static int Atorados { get; private set; }

        /// <summary>Si es true, cada choque y cada atorón se anota en <see cref="RutaRegistro"/>.</summary>
        public static bool RegistrarEnArchivo { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReiniciarAlArrancar() => Reiniciar();

        /// <summary>Pone los contadores en 0 y apaga el registro en archivo.</summary>
        public static void Reiniciar()
        {
            Choques = 0;
            Atorados = 0;
            RegistrarEnArchivo = false;
            proximaRevision = 0f;
            parejasEnChoque.Clear();
            parejasAhora.Clear();
        }

        /// <summary>Anota un carro atorado (lo llama el carro una vez por atorón).</summary>
        public static void AnotarAtorado(string carro, Vector3 donde, float segundos)
        {
            Atorados++;
            Escribir("atorado " + carro + " " + segundos.ToString("0.0") + "s en " + donde);
        }

        /// <summary>
        /// True si las cajas de dos carros (centro y dirección en planta (x, z), mitades de largo y ancho) se traslapan. Prueba de ejes separadores entre dos rectángulos orientados.
        /// </summary>
        public static bool Solapan(Vector2 centroA, Vector2 dirA, Vector2 centroB, Vector2 dirB, float mitadLargo, float mitadAncho)
        {
            if (dirA.sqrMagnitude < 0.0001f || dirB.sqrMagnitude < 0.0001f)
            {
                return (centroA - centroB).magnitude < mitadLargo;
            }
            dirA.Normalize();
            dirB.Normalize();
            Vector2 anchoA = new Vector2(-dirA.y, dirA.x);
            Vector2 anchoB = new Vector2(-dirB.y, dirB.x);
            Vector2 d = centroB - centroA;
            Vector2[] ejes = { dirA, anchoA, dirB, anchoB };
            foreach (Vector2 eje in ejes)
            {
                float radioA = mitadLargo * Mathf.Abs(Vector2.Dot(dirA, eje)) + mitadAncho * Mathf.Abs(Vector2.Dot(anchoA, eje));
                float radioB = mitadLargo * Mathf.Abs(Vector2.Dot(dirB, eje)) + mitadAncho * Mathf.Abs(Vector2.Dot(anchoB, eje));
                if (Mathf.Abs(Vector2.Dot(d, eje)) > radioA + radioB)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Revisa cada <see cref="SegundosEntreRevisiones"/> s los pares de carros activos: un par que empieza a traslaparse suma un choque (una sola vez hasta que se separen).
        /// No cuenta los robados ni los de refuerzo (los maneja otro sistema). Lo llama el administrador del tráfico; en pruebas se puede forzar la revisión.
        /// </summary>
        public static void RevisarChoques(IReadOnlyList<CarroTrafico> carros, float ahora, bool forzar = false)
        {
            if (!forzar && ahora < proximaRevision)
            {
                return;
            }
            proximaRevision = ahora + SegundosEntreRevisiones;
            parejasAhora.Clear();
            float maximoCuadrado = DistanciaMaximaDeRevision * DistanciaMaximaDeRevision;
            for (int i = 0; i < carros.Count; i++)
            {
                CarroTrafico a = carros[i];
                if (a == null || !Cuenta(a))
                {
                    continue;
                }
                for (int j = i + 1; j < carros.Count; j++)
                {
                    CarroTrafico b = carros[j];
                    if (b == null || !Cuenta(b))
                    {
                        continue;
                    }
                    Vector3 pa = a.transform.position;
                    Vector3 pb = b.transform.position;
                    if ((pa.x - pb.x) * (pa.x - pb.x) + (pa.z - pb.z) * (pa.z - pb.z) > maximoCuadrado)
                    {
                        continue;
                    }
                    Vector3 fa = a.transform.forward;
                    Vector3 fb = b.transform.forward;
                    if (!Solapan(new Vector2(pa.x, pa.z), new Vector2(fa.x, fa.z), new Vector2(pb.x, pb.z), new Vector2(fb.x, fb.z), MitadLargo, MitadAncho))
                    {
                        continue;
                    }
                    long llave = ((long)a.GetInstanceID() << 32) ^ (uint)b.GetInstanceID();
                    parejasAhora.Add(llave);
                    if (parejasEnChoque.Add(llave))
                    {
                        Choques++;
                        Escribir("choque " + a.name + " con " + b.name + " en " + pa);
                    }
                }
            }
            parejasEnChoque.RemoveWhere(llave => !parejasAhora.Contains(llave));
        }

        private static bool Cuenta(CarroTrafico carro) => carro.isActiveAndEnabled && !carro.Robado && !carro.EnRefuerzo;

        private static void Escribir(string linea)
        {
            if (!RegistrarEnArchivo)
            {
                return;
            }
            try
            {
                Directory.CreateDirectory("Logs");
                File.AppendAllText(RutaRegistro, "[" + Time.time.ToString("0.0") + "] " + linea + Environment.NewLine);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Trafico] T-1603: no pude escribir " + RutaRegistro + ": " + e.Message);
            }
        }
    }
}

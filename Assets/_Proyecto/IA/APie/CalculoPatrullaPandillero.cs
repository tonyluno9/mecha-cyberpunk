using System;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// La patrulla de un pandillero que está esperando en su zona (T-446), sin MonoBehaviour: elige un punto al azar cerca de su hogar, camina
    /// hacia él, espera un rato al llegar y elige otro. Si no avanza (una pared) cambia de punto. Trabaja en el plano del suelo; el azar viene de fuera.
    /// </summary>
    public sealed class CalculoPatrullaPandillero
    {
        private const float FraccionRadioMinimo = 0.35f;
        private const float GradosPorVuelta = 360f;
        private const float AvanceMinimoPorChequeo = 0.15f;

        private readonly float radio;
        private readonly float esperaMinima;
        private readonly float esperaMaxima;
        private readonly float margenLlegada;
        private readonly float segundosAtasco;
        private readonly Func<float> azar;

        private Vector3 destino;
        private bool hayDestino;
        private float espera;
        private float relojAtasco;
        private float distanciaAnterior = float.MaxValue;

        /// <param name="radio">Hasta qué distancia del hogar se aleja al patrullar (m); 0 o menos no patrulla.</param>
        /// <param name="esperaMinima">Espera mínima al llegar a un punto (s).</param>
        /// <param name="esperaMaxima">Espera máxima al llegar a un punto (s).</param>
        /// <param name="margenLlegada">A qué distancia del punto se considera que llegó (m).</param>
        /// <param name="segundosAtasco">Si en este tiempo no se acerca al punto, elige otro (s).</param>
        /// <param name="azar">Números al azar entre 0 y 1 (inyectado).</param>
        public CalculoPatrullaPandillero(float radio, float esperaMinima, float esperaMaxima, float margenLlegada, float segundosAtasco, Func<float> azar)
        {
            this.radio = Math.Max(0f, radio);
            this.esperaMinima = Math.Max(0f, esperaMinima);
            this.esperaMaxima = Math.Max(this.esperaMinima, esperaMaxima);
            this.margenLlegada = Math.Max(0.05f, margenLlegada);
            this.segundosAtasco = Math.Max(0.2f, segundosAtasco);
            this.azar = azar ?? throw new ArgumentNullException(nameof(azar));
        }

        public bool Activa => radio > 0f;

        /// <summary>True mientras espera quieto en un punto.</summary>
        public bool Esperando => espera > 0f;

        /// <summary>El punto al que camina (solo vale si ya eligió uno).</summary>
        public Vector3 Destino => destino;

        public bool HayDestino => hayDestino;

        /// <summary>Olvida el punto y la espera (al volver a empezar, por ejemplo tras una pelea).</summary>
        public void Reiniciar()
        {
            hayDestino = false;
            espera = 0f;
            relojAtasco = 0f;
            distanciaAnterior = float.MaxValue;
        }

        /// <summary>
        /// Avanza <paramref name="dt"/> segundos y dice hacia dónde caminar: un vector unitario en el suelo, o cero si espera, llegó o no patrulla.
        /// </summary>
        public Vector3 Avanzar(float dt, Vector3 posicion, Vector3 hogar)
        {
            if (!Activa)
            {
                return Vector3.zero;
            }
            if (espera > 0f)
            {
                espera -= dt;
                return Vector3.zero;
            }
            if (!hayDestino)
            {
                ElegirDestino(hogar);
            }
            Vector3 hacia = Plano(destino - posicion);
            float distancia = hacia.magnitude;
            if (distancia <= margenLlegada)
            {
                Esperar();
                return Vector3.zero;
            }
            if (RevisarAtasco(dt, distancia))
            {
                ElegirDestino(hogar);
                return Vector3.zero;
            }
            return hacia / distancia;
        }

        private void ElegirDestino(Vector3 hogar)
        {
            float angulo = azar() * GradosPorVuelta * Mathf.Deg2Rad;
            float distancia = radio * Mathf.Lerp(FraccionRadioMinimo, 1f, azar());
            destino = new Vector3(hogar.x + Mathf.Cos(angulo) * distancia, hogar.y, hogar.z + Mathf.Sin(angulo) * distancia);
            hayDestino = true;
            relojAtasco = 0f;
            distanciaAnterior = float.MaxValue;
        }

        private void Esperar()
        {
            espera = Mathf.Lerp(esperaMinima, esperaMaxima, azar());
            hayDestino = false;
            relojAtasco = 0f;
            distanciaAnterior = float.MaxValue;
        }

        // Cada segundosAtasco compara cuánto se acercó al punto; si casi nada, está trabado.
        private bool RevisarAtasco(float dt, float distancia)
        {
            relojAtasco += dt;
            if (relojAtasco < segundosAtasco)
            {
                return false;
            }
            relojAtasco = 0f;
            bool atascado = distanciaAnterior - distancia < AvanceMinimoPorChequeo;
            distanciaAnterior = distancia;
            return atascado;
        }

        private static Vector3 Plano(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}

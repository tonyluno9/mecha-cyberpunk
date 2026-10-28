using System;
using System.Collections.Generic;

namespace MechaCyberpunk.Trafico.Peatones
{
    /// <summary>
    /// Cómo camina un peatón por una banqueta (T-330), sin Unity: avanza por una ruta de puntos a su velocidad, espera un rato al llegar a cada punto
    /// intermedio (las esquinas) y da la vuelta en los finales. El azar viene de fuera. Trabaja con distancias: el MonoBehaviour las convierte en posiciones.
    /// </summary>
    public sealed class CalculoPeaton
    {
        private readonly float[] largosSegmentos;
        private readonly float velocidad;
        private readonly float esperaMinima;
        private readonly float esperaMaxima;
        private readonly Random azar;

        private int segmento;
        private int sentido = 1;
        private float recorridoEnSegmento;
        private float esperaRestante;

        /// <param name="largosSegmentos">Largo (m) de cada tramo entre puntos consecutivos de la ruta; al menos uno.</param>
        /// <param name="metrosPorSegundo">Velocidad al caminar (m/s).</param>
        /// <param name="segundosEsperaMinima">Espera mínima en una esquina (s).</param>
        /// <param name="segundosEsperaMaxima">Espera máxima en una esquina (s).</param>
        /// <param name="azarEspera">Azar de las esperas.</param>
        public CalculoPeaton(IReadOnlyList<float> largosSegmentos, float metrosPorSegundo, float segundosEsperaMinima, float segundosEsperaMaxima, Random azarEspera)
        {
            if (largosSegmentos == null || largosSegmentos.Count == 0)
            {
                throw new ArgumentException("La ruta necesita al menos un tramo.", nameof(largosSegmentos));
            }
            if (metrosPorSegundo <= 0f || segundosEsperaMinima < 0f || segundosEsperaMaxima < segundosEsperaMinima)
            {
                throw new ArgumentException("Velocidad mayor que 0 y esperas válidas.");
            }
            this.largosSegmentos = new float[largosSegmentos.Count];
            for (int i = 0; i < this.largosSegmentos.Length; i++)
            {
                if (largosSegmentos[i] <= 0f)
                {
                    throw new ArgumentException("Todos los tramos deben medir más de 0 m.", nameof(largosSegmentos));
                }
                this.largosSegmentos[i] = largosSegmentos[i];
            }
            velocidad = metrosPorSegundo;
            esperaMinima = segundosEsperaMinima;
            esperaMaxima = segundosEsperaMaxima;
            azar = azarEspera ?? throw new ArgumentNullException(nameof(azarEspera));
        }

        /// <summary>Tramo en el que va (0 es el primero de la ruta).</summary>
        public int Segmento => segmento;

        /// <summary>De 0 a 1 dentro del tramo; con <see cref="Sentido"/> -1 se recorre al revés, pero el valor sigue contando desde el punto de salida del tramo.</summary>
        public float Fraccion => recorridoEnSegmento / largosSegmentos[segmento];

        /// <summary>1 si va hacia el final de la ruta y -1 si regresa.</summary>
        public int Sentido => sentido;

        public bool Esperando => esperaRestante > 0f;

        /// <summary>Avanza <paramref name="dt"/> segundos (s): primero termina la espera y con lo que sobra camina.</summary>
        public void Avanzar(float dt)
        {
            if (dt <= 0f)
            {
                return;
            }
            while (dt > 0f)
            {
                if (esperaRestante > 0f)
                {
                    float usado = Math.Min(esperaRestante, dt);
                    esperaRestante -= usado;
                    dt -= usado;
                    continue;
                }
                float falta = largosSegmentos[segmento] - recorridoEnSegmento;
                float posible = velocidad * dt;
                if (posible < falta)
                {
                    recorridoEnSegmento += posible;
                    return;
                }
                dt -= falta / velocidad;
                LlegarAlPunto();
            }
        }

        // Al terminar un tramo: en un final da la vuelta sin esperar; en una esquina espera y sigue con el tramo siguiente.
        private void LlegarAlPunto()
        {
            int siguiente = segmento + sentido;
            if (siguiente < 0 || siguiente >= largosSegmentos.Length)
            {
                sentido = -sentido;
                recorridoEnSegmento = 0f;
                return;
            }
            segmento = siguiente;
            recorridoEnSegmento = 0f;
            esperaRestante = esperaMinima + (float)azar.NextDouble() * (esperaMaxima - esperaMinima);
        }
    }
}

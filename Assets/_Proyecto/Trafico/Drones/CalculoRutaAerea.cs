using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Drones
{
    /// <summary>T-1398: dónde está un dron en su ruta en un instante, hacia dónde mira y cuánto se inclina.</summary>
    public readonly struct MuestraRutaAerea
    {
        public MuestraRutaAerea(Vector3 posicion, Vector3 rumbo, float alabeo)
        {
            Posicion = posicion;
            Rumbo = rumbo;
            Alabeo = alabeo;
        }

        /// <summary>Posición (x, altura con la oscilación, z) en el espacio de la ruta.</summary>
        public Vector3 Posicion { get; }

        /// <summary>Dirección horizontal de avance (unitaria).</summary>
        public Vector3 Rumbo { get; }

        /// <summary>Inclinación lateral en grados, con la convención de Unity (rotación positiva sobre el eje de avance = inclinarse a la izquierda). Girar a la derecha da negativo.</summary>
        public float Alabeo { get; }
    }

    /// <summary>
    /// T-1398: lógica pura de una ruta aérea en bucle. Pasa por los puntos (x, z) con una curva suave (Catmull-Rom cerrada), a velocidad constante en m/s,
    /// a una altura con una oscilación senoidal arriba y abajo, y se inclina un poco en las curvas. Sin MonoBehaviour: el tiempo entra como parámetro.
    /// </summary>
    public sealed class CalculoRutaAerea
    {
        public const int MuestrasPorTramoPorDefecto = 12;
        private const float DosPi = 2f * Mathf.PI;
        private const float SegundosDeMirada = 0.4f;   // ventana para medir cuánto gira el rumbo
        private const float DistanciaMinimaMirada = 1f;

        private readonly Vector2[] polilinea;      // puntos de la curva suavizada; el último repite al primero
        private readonly float[] acumulada;        // distancia recorrida hasta cada punto de la polilínea
        private readonly float altura;
        private readonly float velocidad;
        private readonly float amplitud;
        private readonly float periodo;
        private readonly float alabeoMaximo;
        private readonly float alabeoPorGiro;

        /// <param name="puntos">Esquinas de la ruta (x, z), en orden; el bucle se cierra solo. Al menos 3.</param>
        /// <param name="altura">Altura media (m).</param>
        /// <param name="velocidad">Velocidad a lo largo de la ruta (m/s), mayor que 0.</param>
        /// <param name="amplitudOscilacion">Cuánto sube y baja (m). 0 = altura fija.</param>
        /// <param name="periodoOscilacion">Segundos de una oscilación completa, mayor que 0 si hay amplitud.</param>
        /// <param name="alabeoMaximo">Tope de la inclinación (°).</param>
        /// <param name="alabeoPorGiro">Grados de inclinación por cada °/s que gira el rumbo.</param>
        public CalculoRutaAerea(IReadOnlyList<Vector2> puntos, float altura, float velocidad, float amplitudOscilacion, float periodoOscilacion,
            float alabeoMaximo, float alabeoPorGiro, int muestrasPorTramo = MuestrasPorTramoPorDefecto)
        {
            if (puntos == null || puntos.Count < 3)
            {
                throw new ArgumentException("Una ruta aérea necesita al menos 3 puntos.", nameof(puntos));
            }
            if (velocidad <= 0f)
            {
                throw new ArgumentException("La velocidad debe ser mayor que 0.", nameof(velocidad));
            }
            if (amplitudOscilacion > 0f && periodoOscilacion <= 0f)
            {
                throw new ArgumentException("El periodo de oscilación debe ser mayor que 0.", nameof(periodoOscilacion));
            }
            this.altura = altura;
            this.velocidad = velocidad;
            amplitud = Mathf.Max(0f, amplitudOscilacion);
            periodo = periodoOscilacion;
            this.alabeoMaximo = Mathf.Max(0f, alabeoMaximo);
            this.alabeoPorGiro = alabeoPorGiro;

            int n = puntos.Count;
            int muestras = Mathf.Max(1, muestrasPorTramo);
            polilinea = new Vector2[n * muestras + 1];
            for (int i = 0; i < n; i++)
            {
                Vector2 p0 = puntos[(i + n - 1) % n], p1 = puntos[i], p2 = puntos[(i + 1) % n], p3 = puntos[(i + 2) % n];
                for (int k = 0; k < muestras; k++)
                {
                    polilinea[i * muestras + k] = CatmullRom(p0, p1, p2, p3, (float)k / muestras);
                }
            }
            polilinea[polilinea.Length - 1] = polilinea[0];
            acumulada = new float[polilinea.Length];
            for (int i = 1; i < polilinea.Length; i++)
            {
                acumulada[i] = acumulada[i - 1] + Vector2.Distance(polilinea[i - 1], polilinea[i]);
            }
        }

        /// <summary>Largo de una vuelta (m).</summary>
        public float Longitud => acumulada[acumulada.Length - 1];

        /// <summary>Segundos que tarda una vuelta.</summary>
        public float DuracionVuelta => Longitud / velocidad;

        public float Velocidad => velocidad;

        /// <summary>
        /// Estado del dron en el instante <paramref name="tiempo"/> (s). <paramref name="fase"/> (0 a 1) lo adelanta esa fracción de vuelta,
        /// y desfasa también la oscilación, para que varios drones en la misma ruta no vayan juntos.
        /// </summary>
        public MuestraRutaAerea Muestra(float tiempo, float fase = 0f)
        {
            float distancia = tiempo * velocidad + fase * Longitud;
            Vector2 p = PuntoEn(distancia);
            Vector2 rumbo = RumboEn(distancia);
            float giro = GradosPorSegundoDeGiro(distancia);
            float alabeo = Mathf.Clamp(-giro * alabeoPorGiro, -alabeoMaximo, alabeoMaximo);
            float vertical = amplitud > 0f ? amplitud * Mathf.Sin((tiempo / periodo + fase) * DosPi) : 0f;
            return new MuestraRutaAerea(new Vector3(p.x, altura + vertical, p.y), new Vector3(rumbo.x, 0f, rumbo.y), alabeo);
        }

        private Vector2 PuntoEn(float distancia)
        {
            float d = Envolver(distancia);
            int i = Segmento(d);
            float largo = acumulada[i + 1] - acumulada[i];
            float t = largo > 0f ? (d - acumulada[i]) / largo : 0f;
            return Vector2.Lerp(polilinea[i], polilinea[i + 1], t);
        }

        private Vector2 RumboEn(float distancia)
        {
            int i = Segmento(Envolver(distancia));
            int tramos = polilinea.Length - 1;
            // Primer segmento con largo desde este: el rumbo no queda en cero aunque dos puntos coincidan.
            for (int k = 0; k < tramos; k++)
            {
                int j = (i + k) % tramos;
                Vector2 v = polilinea[j + 1] - polilinea[j];
                if (v.sqrMagnitude > 0f)
                {
                    return v.normalized;
                }
            }
            return Vector2.up;
        }

        // °/s con que gira el rumbo en esa distancia (positivo = a la derecha, mirando desde arriba).
        private float GradosPorSegundoDeGiro(float distancia)
        {
            float paso = Mathf.Max(DistanciaMinimaMirada, velocidad * SegundosDeMirada);
            Vector2 antes = RumboEn(distancia - paso * 0.5f);
            Vector2 despues = RumboEn(distancia + paso * 0.5f);
            float yawAntes = Mathf.Atan2(antes.x, antes.y) * Mathf.Rad2Deg;
            float yawDespues = Mathf.Atan2(despues.x, despues.y) * Mathf.Rad2Deg;
            return Mathf.DeltaAngle(yawAntes, yawDespues) / (paso / velocidad);
        }

        private float Envolver(float distancia)
        {
            float largo = Longitud;
            float d = distancia % largo;
            return d < 0f ? d + largo : d;
        }

        private int Segmento(float d)
        {
            int bajo = 0, alto = acumulada.Length - 2;
            while (bajo < alto)
            {
                int medio = (bajo + alto + 1) / 2;
                if (acumulada[medio] <= d)
                {
                    bajo = medio;
                }
                else
                {
                    alto = medio - 1;
                }
            }
            return bajo;
        }

        private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }
    }
}

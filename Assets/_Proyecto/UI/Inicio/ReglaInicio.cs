using System;
using UnityEngine;

namespace MechaCyberpunk.UI.Inicio
{
    /// <summary>
    /// Reglas puras de la pantalla de inicio y de la de carga (sin MonoBehaviour, para probarlas en EditMode): el progreso real de una
    /// carga asíncrona, su suavizado visual, cuándo se puede activar la escena y los textos de los controles.
    /// </summary>
    public static class ReglaInicio
    {
        /// <summary>Hasta aquí llega <c>AsyncOperation.progress</c> mientras la escena espera su activación.</summary>
        public const float ProgresoAsincronoListo = 0.9f;

        private const float FactorSuavizado = 6f;
        private const float FraccionVelocidadMinima = 0.1f;
        private const float BarraLlena = 0.999f;
        private const float ConstanteExponencial = 3f;

        /// <summary>De 0 a 1: el progreso de Unity (0 a 0.9) llevado a la barra completa.</summary>
        public static float ProgresoDeCarga(float progresoAsincrono)
        {
            return Mathf.Clamp01(progresoAsincrono / ProgresoAsincronoListo);
        }

        /// <summary>
        /// La barra que se ve avanza hacia el valor real sin retroceder y sin pasarlo: más despacio al acercarse (suavizado), nunca más
        /// rápido que <paramref name="velocidad"/> (fracción por segundo) y nunca se detiene del todo, para que no salte aunque la carga sí.
        /// </summary>
        public static float Suavizar(float mostrado, float objetivo, float dt, float velocidad)
        {
            objetivo = Mathf.Clamp01(objetivo);
            if (mostrado >= objetivo)
            {
                return mostrado;
            }
            float maximo = velocidad * dt;
            float suave = (objetivo - mostrado) * (1f - Mathf.Exp(-FactorSuavizado * dt));
            float paso = Mathf.Clamp(suave, maximo * FraccionVelocidadMinima, maximo);
            return Mathf.Min(objetivo, mostrado + paso);
        }

        /// <summary>
        /// La escena nueva se activa cuando la carga ya llegó a 0.9, la barra que se ve ya está llena y pasó el tiempo mínimo de la
        /// pantalla (para que los consejos y la barra se alcancen a leer).
        /// </summary>
        public static bool PuedeActivar(float progresoAsincrono, float barraMostrada, float segundos, float segundosMinimos)
        {
            return progresoAsincrono >= ProgresoAsincronoListo && barraMostrada >= BarraLlena && segundos >= segundosMinimos;
        }

        /// <summary>Carga sin operación real (recarga de la ciudad): la barra se acerca a 0.9 con el tiempo y nunca lo pasa.</summary>
        public static float ProgresoSimulado(float segundos, float segundosParaCasiLleno)
        {
            if (segundosParaCasiLleno <= 0f)
            {
                return ProgresoAsincronoListo;
            }
            float t = 1f - Mathf.Exp(-ConstanteExponencial * Mathf.Max(0f, segundos) / segundosParaCasiLleno);
            return Mathf.Clamp(t * ProgresoAsincronoListo, 0f, ProgresoAsincronoListo);
        }

        public static string TextoPorcentaje(float progreso)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(progreso) * 100f) + "%";
        }

        public static string TextoResolucion(Vector2Int resolucion)
        {
            return resolucion.x + " x " + resolucion.y;
        }

        /// <summary>Puntos suspensivos animados: "CARGANDO", "CARGANDO.", "CARGANDO..", "CARGANDO..." según el tiempo.</summary>
        public static string TextoConPuntos(string texto, float segundos, float segundosPorPunto)
        {
            int puntos = segundosPorPunto > 0f ? (int)(segundos / segundosPorPunto) % 4 : 0;
            return texto + new string('.', puntos);
        }

        /// <summary>El siguiente consejo, dando la vuelta al llegar al final. Sin consejos devuelve 0.</summary>
        public static int SiguienteIndice(int actual, int total)
        {
            return total <= 0 ? 0 : (actual + 1 + total) % total;
        }

        /// <summary>Avanza o retrocede un índice de una lista de opciones; con <paramref name="dar"/> da la vuelta, si no se detiene en los extremos.</summary>
        public static int MoverIndice(int actual, int paso, int total, bool dar)
        {
            if (total <= 0)
            {
                return 0;
            }
            int nuevo = actual + paso;
            if (dar)
            {
                return ((nuevo % total) + total) % total;
            }
            return Mathf.Clamp(nuevo, 0, total - 1);
        }

        /// <summary>Valor de un deslizador tras mover <paramref name="pasos"/> pasos, recortado a [minimo, maximo] y sin errores de decimales.</summary>
        public static float MoverValor(float valor, int pasos, float paso, float minimo, float maximo)
        {
            float nuevo = Mathf.Clamp(valor + pasos * paso, minimo, maximo);
            return (float)Math.Round(nuevo, 4);
        }

        /// <summary>De la posición x del puntero dentro de una barra (0 a 1) al valor del deslizador, alineado a su paso.</summary>
        public static float ValorDeFraccion(float fraccion, float paso, float minimo, float maximo)
        {
            float crudo = Mathf.Lerp(minimo, maximo, Mathf.Clamp01(fraccion));
            if (paso <= 0f)
            {
                return crudo;
            }
            float alineado = minimo + Mathf.Round((crudo - minimo) / paso) * paso;
            return (float)Math.Round(Mathf.Clamp(alineado, minimo, maximo), 4);
        }

        public static string TextoVolumen(float volumen)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(volumen) * 100f) + "%";
        }
    }
}

using System;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>Qué viene dentro de la cápsula CP01 (de la tarea de cápsulas del 22-nov): decide la escala de la cápsula.</summary>
    public enum TipoCapsulaCaida
    {
        /// <summary>Robot pesado.</summary>
        E16,
        /// <summary>Élite con jetpack.</summary>
        E18,
        /// <summary>Centinela.</summary>
        E15,
        /// <summary>Perro robot.</summary>
        E17,
        /// <summary>Dron pesado (1.8 m).</summary>
        DR02a,
        /// <summary>Dron E13 (1.1 m).</summary>
        E13,
        /// <summary>Dron DR01 (0.8 m).</summary>
        DR01,
        /// <summary>Dron explorador (0.7 m).</summary>
        DR04a
    }

    /// <summary>
    /// T-1621: lógica pura de la cápsula de caída (sin Unity vivo): la línea de tiempo en cuadros a 24 fps, la escala por enemigo, la curva del holograma
    /// (Holo y Fade) y el vuelo de los drones. <see cref="CapsulaCaida"/> la lee y la aplica. Los cuadros se cuentan desde que empieza la caída (el cuadro 1 es el segundo 0).
    /// </summary>
    public static class CalculoCapsulaCaida
    {
        public const float Fps = 24f;

        /// <summary>La caja toca el piso: sacudida, chispas y sonido pesado; arranca el clip del robot.</summary>
        public const int FrameImpacto = 12;
        public const int FrameInicioClip = 12;
        /// <summary>La tapa sale disparada (suena el pistón).</summary>
        public const int FrameTapa = 36;
        /// <summary>El robot (o el dron) se hace visible dentro de la caja.</summary>
        public const int FrameVisible = 39;
        /// <summary>Cuadro de inicio del holograma de la tapa.</summary>
        public const int FrameHoloTapa = 46;
        /// <summary>Cuadro de inicio del holograma de la base y las paredes.</summary>
        public const int FrameHoloBase = 58;
        /// <summary>Cuadros que dura el holograma de una pieza: en el cuadro inicio + este valor se desactiva.</summary>
        public const int FramesHolo = 24;

        // Drones: quietos hasta el 52, suben a 1.6 m en el 64 y siguen con un vaivén hasta el 112, donde entra su IA normal.
        public const int FrameDronQuieto = 52;
        public const int FrameDronSube = 64;
        public const int FrameDronFin = 112;
        public const float AlturaDronAparece = 0.1f;
        public const float AlturaDronArriba = 1.6f;

        /// <summary>Altura (m) de la caída de la cápsula a escala 1 (la de la animación CP01_Abrir).</summary>
        public const float AlturaCaida = 30f;

        private const float ParpadeoMinimo = 0.15f;
        private const float ParpadeoMaximo = 0.65f;
        private const int Parpadeos = 6;
        private const float FrameParpadeoInicio = 5f;
        private const float FramesPorParpadeo = 2f;
        private const float FrameFade60 = 17f;
        private const float Fade60 = 0.6f;
        private const float FrameInvisible = 23f;
        private const float FrameHoloLleno = 3f;

        // Vaivén del dron: cuadro, altura (m) y giro en Y (grados).
        private static readonly float[] CuadrosVaiven = { 64f, 76f, 88f, 100f, 112f };
        private static readonly float[] AlturasVaiven = { 1.6f, 1.9f, 1.7f, 1.9f, 1.75f };
        private static readonly float[] GirosVaiven = { 0f, 15f, -15f, 20f, -10f };

        public static float SegundosDeFrame(float frame) => (frame - 1f) / Fps;

        public static float FrameDeSegundos(float segundos) => segundos * Fps + 1f;

        /// <summary>Escala (localScale) de la raíz de la cápsula para cada enemigo. Al escalar se escalan también la altura de caída y el rebote.</summary>
        public static float EscalaDe(TipoCapsulaCaida tipo)
        {
            switch (tipo)
            {
                case TipoCapsulaCaida.E16: return 1.00f;
                case TipoCapsulaCaida.E18: return 0.85f;
                case TipoCapsulaCaida.E15: return 0.66f;
                case TipoCapsulaCaida.E17: return 0.45f;
                case TipoCapsulaCaida.DR02a: return 0.70f;
                case TipoCapsulaCaida.E13: return 0.46f;
                case TipoCapsulaCaida.DR01: return 0.33f;
                default: return 0.30f; // DR04a
            }
        }

        public static bool EsDron(TipoCapsulaCaida tipo) =>
            tipo == TipoCapsulaCaida.DR02a || tipo == TipoCapsulaCaida.E13 || tipo == TipoCapsulaCaida.DR01 || tipo == TipoCapsulaCaida.DR04a;

        /// <summary>El dron que dice un nombre de prefab (Dron_DR04_a, Dron_DR01, Dron_E13, Dron_DR02_a). False si no es uno de los cuatro (NV01, NV03 y demás no llevan cápsula).</summary>
        public static bool TipoDronDeNombre(string nombre, out TipoCapsulaCaida tipo)
        {
            tipo = TipoCapsulaCaida.DR04a;
            if (string.IsNullOrEmpty(nombre))
            {
                return false;
            }
            if (Contiene(nombre, "DR04")) { tipo = TipoCapsulaCaida.DR04a; return true; }
            if (Contiene(nombre, "DR02")) { tipo = TipoCapsulaCaida.DR02a; return true; }
            if (Contiene(nombre, "DR01")) { tipo = TipoCapsulaCaida.DR01; return true; }
            if (Contiene(nombre, "Dron_E13") || Contiene(nombre, "DronE13")) { tipo = TipoCapsulaCaida.E13; return true; }
            return false;
        }

        private static bool Contiene(string texto, string parte) => texto.IndexOf(parte, StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>Altura (m sobre el piso) del dron en el cuadro dado: 0.1 al aparecer, quieto hasta el 52, sube a 1.6 en el 64 y sigue con el vaivén.</summary>
        public static float AlturaDron(float frame)
        {
            if (frame <= FrameDronQuieto)
            {
                return AlturaDronAparece;
            }
            if (frame < FrameDronSube)
            {
                return Mathf.Lerp(AlturaDronAparece, AlturaDronArriba, Suave((frame - FrameDronQuieto) / (FrameDronSube - FrameDronQuieto)));
            }
            return Vaiven(AlturasVaiven, frame);
        }

        /// <summary>Giro extra en Y (grados) del dron en el cuadro dado (cero hasta el 64, luego ±10 a 20 grados).</summary>
        public static float GiroDron(float frame) => frame <= CuadrosVaiven[0] ? 0f : Vaiven(GirosVaiven, frame);

        private static float Vaiven(float[] valores, float frame)
        {
            if (frame >= CuadrosVaiven[CuadrosVaiven.Length - 1])
            {
                return valores[valores.Length - 1];
            }
            for (int i = 1; i < CuadrosVaiven.Length; i++)
            {
                if (frame < CuadrosVaiven[i])
                {
                    float t = (frame - CuadrosVaiven[i - 1]) / (CuadrosVaiven[i] - CuadrosVaiven[i - 1]);
                    return Mathf.Lerp(valores[i - 1], valores[i], Suave(t));
                }
            }
            return valores[valores.Length - 1];
        }

        private static float Suave(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

        /// <summary>Lo que se le pone al material de una pieza en un cuadro.</summary>
        public struct EstadoHolo
        {
            public float Holo;
            public float Fade;
            /// <summary>True cuando ya pasó el cuadro de desactivar (inicio + 24): hay que apagar el GameObject de la pieza.</summary>
            public bool Desactivar;
        }

        /// <summary>
        /// La curva del holograma de una pieza, con s = su cuadro de inicio: s a s+3 Holo 0 a 1; s+5 a s+15, seis parpadeos de Fade (uno cada 2 cuadros, azar entre 0.15 y 0.65);
        /// s+17 Fade 0.6; s+23 Fade 1 (invisible); s+24 se desactiva. El azar entra por el constructor y los seis valores se sortean una sola vez.
        /// </summary>
        public sealed class CurvaHolo
        {
            private readonly float[] parpadeos = new float[Parpadeos];

            public CurvaHolo(Func<float> azar)
            {
                for (int i = 0; i < Parpadeos; i++)
                {
                    float r = azar != null ? Mathf.Clamp01(azar()) : 0.5f;
                    parpadeos[i] = Mathf.Lerp(ParpadeoMinimo, ParpadeoMaximo, r);
                }
            }

            /// <param name="framesDesdeInicio">Cuadros transcurridos desde el cuadro de inicio de la pieza (negativo = aún no empieza).</param>
            public EstadoHolo Evaluar(float framesDesdeInicio)
            {
                float d = framesDesdeInicio;
                EstadoHolo e = default;
                if (d < 0f)
                {
                    return e;
                }
                e.Holo = Mathf.Clamp01(d / FrameHoloLleno);
                if (d < FrameParpadeoInicio)
                {
                    e.Fade = 0f;
                }
                else if (d < FrameFade60)
                {
                    int indice = Mathf.Min(Parpadeos - 1, Mathf.FloorToInt((d - FrameParpadeoInicio) / FramesPorParpadeo));
                    e.Fade = parpadeos[indice];
                }
                else if (d < FrameInvisible)
                {
                    e.Fade = Mathf.Lerp(Fade60, 1f, (d - FrameFade60) / (FrameInvisible - FrameFade60));
                }
                else
                {
                    e.Fade = 1f;
                }
                e.Desactivar = d >= FramesHolo;
                return e;
            }

            /// <summary>El valor sorteado del parpadeo <paramref name="indice"/> (0 a 5), para las pruebas.</summary>
            public float Parpadeo(int indice) => parpadeos[indice];
        }
    }
}

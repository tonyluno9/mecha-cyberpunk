using System;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// T-1545: cuentas puras (sin escena) de <see cref="LucesCiudad"/>: qué luces de punto se dejan prendidas según su distancia a la cámara
    /// (con tope y histéresis) y cómo se reconoce una farola de sodio o una luz de neón por su color.
    /// </summary>
    public static class CalculoLucesCiudad
    {
        /// <summary>Saturación mínima (0 a 1) de un neón.</summary>
        public const float SaturacionNeon = 0.6f;

        /// <summary>Un neón es de luz chica: más alcance que esto (m) ya es un relleno, no un letrero.</summary>
        public const float AlcanceMaximoNeon = 15f;

        /// <summary>Tonos (0 a 1 del círculo de color) del neón: cian/turquesa y magenta/rosa.</summary>
        private const float TonoCianMin = 0.44f;
        private const float TonoCianMax = 0.55f;
        private const float TonoMagentaMin = 0.78f;
        private const float TonoMagentaMax = 0.95f;

        /// <summary>Tonos cálidos (0 a 1): del rojo anaranjado al ámbar.</summary>
        private const float TonoCalidoMax = 0.13f;
        private const float SaturacionCalidaMin = 0.4f;

        /// <summary>True si el color es turquesa o magenta saturado (neón de letrero).</summary>
        public static bool EsColorNeon(Color color)
        {
            Color.RGBToHSV(color, out float tono, out float saturacion, out float valor);
            if (saturacion < SaturacionNeon || valor < 0.5f)
            {
                return false;
            }
            return (tono >= TonoCianMin && tono <= TonoCianMax) || (tono >= TonoMagentaMin && tono <= TonoMagentaMax);
        }

        /// <summary>True si el color es cálido (amarillo ámbar a naranja): candidato a luz de farola.</summary>
        public static bool EsColorCalido(Color color)
        {
            Color.RGBToHSV(color, out float tono, out float saturacion, out float valor);
            return saturacion >= SaturacionCalidaMin && valor >= 0.5f && tono <= TonoCalidoMax;
        }

        /// <summary>Alcance (m) hasta el que la luz se deja prendida: su alcance más la distancia extra.</summary>
        public static float RadioPrendida(float alcance, float distanciaExtra)
        {
            return Mathf.Max(0f, alcance) + Mathf.Max(0f, distanciaExtra);
        }

        /// <summary>
        /// Decide cuáles de las <paramref name="cantidad"/> luces quedan prendidas: las que están dentro de su radio (más <paramref name="histeresis"/> si ya estaban prendidas)
        /// y, de ellas, las <paramref name="maximo"/> más cercanas. Escribe el resultado en <paramref name="prendida"/>. Usa <paramref name="memoria"/>
        /// (arreglo de al menos <paramref name="cantidad"/> flotantes) para ordenar sin crear basura. Devuelve cuántas quedan prendidas.
        /// </summary>
        public static int Seleccionar(Vector3 camara, Vector3[] posiciones, float[] radios, bool[] elegible, bool[] estabaPrendida,
            int cantidad, int maximo, float histeresis, bool[] prendida, float[] memoria)
        {
            int candidatas = 0;
            for (int i = 0; i < cantidad; i++)
            {
                prendida[i] = false;
                if (!elegible[i])
                {
                    continue;
                }
                float radio = radios[i] + (estabaPrendida[i] ? histeresis : 0f);
                float d2 = (posiciones[i] - camara).sqrMagnitude;
                if (d2 <= radio * radio)
                {
                    memoria[candidatas++] = d2;
                    prendida[i] = true;
                }
            }
            if (candidatas <= maximo)
            {
                return candidatas;
            }
            Array.Sort(memoria, 0, candidatas);
            float corte = memoria[Mathf.Max(0, maximo - 1)];
            int quedan = 0;
            for (int i = 0; i < cantidad; i++)
            {
                if (!prendida[i])
                {
                    continue;
                }
                if ((posiciones[i] - camara).sqrMagnitude <= corte && quedan < maximo)
                {
                    quedan++;
                }
                else
                {
                    prendida[i] = false;
                }
            }
            return quedan;
        }
    }
}

using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Opciones
{
    /// <summary>
    /// Reglas puras de las opciones (T-411): valores por defecto, límites y JSON de ida y vuelta. Sensibilidad de 0.25 a 3 en pasos de 0.05;
    /// volúmenes de 0 a 1 en pasos de 0.05 (5 %). Por defecto: sensibilidad 1, Y normal, volúmenes al 80 % salvo Ambiente y Música en 0
    /// (el silencio del plan de la noche, D-tony-264), calidad MEDIO (T-978), resolución nativa y pantalla completa.
    /// </summary>
    public static class ReglaOpciones
    {
        public const float SensibilidadMinima = 0.25f;
        public const float SensibilidadMaxima = 3f;
        public const float PasoSensibilidad = 0.05f;
        public const float PasoVolumen = 0.05f;
        public const float VolumenPorDefecto = 0.8f;
        public static readonly int CantidadSecciones = Enum.GetValues(typeof(SeccionSonido)).Length;

        // Calidad BAJA, MEDIA y ALTA (en el orden de CalidadGrafica).
        private static readonly float[] escalasRender = { 0.75f, 0.9f, 1f };
        private static readonly float[] distanciasSombra = { 40f, 70f, 120f };
        private static readonly int[] muestrasMsaa = { 1, 2, 4 };
        // T-978: distancia de dibujo (m; 0 = hasta el plano lejano de la cámara), reflejos de charcos y postproceso.
        private static readonly float[] distanciasDibujo = { 150f, 300f, 0f };
        private static readonly bool[] reflejosCharcos = { false, true, true };
        private static readonly bool[] postprocesos = { false, true, true };

        /// <summary>T-978: la calidad de una partida nueva (y la que queda si el archivo trae una que no existe).</summary>
        public const CalidadGrafica CalidadPorDefecto = CalidadGrafica.Media;

        public static ValoresOpciones PorDefecto()
        {
            ValoresOpciones v = new ValoresOpciones { volumenes = new float[CantidadSecciones] };
            for (int i = 0; i < CantidadSecciones; i++)
            {
                v.volumenes[i] = VolumenInicial((SeccionSonido)i);
            }
            return v;
        }

        public static float VolumenInicial(SeccionSonido seccion)
        {
            return seccion == SeccionSonido.Musica ? 0f : VolumenPorDefecto;
        }

        /// <summary>Deja cada valor en su rango y en su paso. Devuelve el mismo objeto.</summary>
        public static ValoresOpciones Recortar(ValoresOpciones v)
        {
            v.sensibilidad = RecortarSensibilidad(v.sensibilidad);
            float[] volumenes = new float[CantidadSecciones];
            for (int i = 0; i < CantidadSecciones; i++)
            {
                volumenes[i] = v.volumenes != null && i < v.volumenes.Length ? RecortarVolumen(v.volumenes[i]) : VolumenInicial((SeccionSonido)i);
            }
            v.volumenes = volumenes;
            if (!Enum.IsDefined(typeof(NivelApuntadoAsistido), v.apuntadoAsistido))
            {
                v.apuntadoAsistido = NivelApuntadoAsistido.Suave;
            }
            if (!Enum.IsDefined(typeof(CalidadGrafica), v.calidad))
            {
                v.calidad = CalidadPorDefecto;
            }
            if (v.tutorialVisto == null)
            {
                v.tutorialVisto = "";
            }
            if (v.resolucionAncho <= 0 || v.resolucionAlto <= 0)
            {
                v.resolucionAncho = 0;
                v.resolucionAlto = 0;
            }
            return v;
        }

        public static float RecortarSensibilidad(float valor)
        {
            if (float.IsNaN(valor))
            {
                return 1f;
            }
            return Mathf.Clamp(Mathf.Round(valor / PasoSensibilidad) * PasoSensibilidad, SensibilidadMinima, SensibilidadMaxima);
        }

        public static float RecortarVolumen(float valor)
        {
            if (float.IsNaN(valor))
            {
                return VolumenPorDefecto;
            }
            return Mathf.Clamp01(Mathf.Round(valor / PasoVolumen) * PasoVolumen);
        }

        public static string AJson(ValoresOpciones v) => JsonUtility.ToJson(v, true);

        /// <summary>Lee el JSON; si está vacío o roto devuelve false y los valores por defecto.</summary>
        public static bool DeJson(string texto, out ValoresOpciones valores)
        {
            valores = PorDefecto();
            if (string.IsNullOrWhiteSpace(texto))
            {
                return false;
            }
            try
            {
                ValoresOpciones leidos = JsonUtility.FromJson<ValoresOpciones>(texto);
                if (leidos == null)
                {
                    return false;
                }
                valores = Recortar(leidos);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public static float EscalaRender(CalidadGrafica calidad) => escalasRender[(int)calidad];
        public static float DistanciaSombras(CalidadGrafica calidad) => distanciasSombra[(int)calidad];
        public static int Msaa(CalidadGrafica calidad) => muestrasMsaa[(int)calidad];
        /// <summary>T-978: argumento de la línea de comandos que fija la calidad: "-calidad Bajo", "Medio" o "Alto" (también Baja/Media/Alta, sin importar mayúsculas).</summary>
        public const string ArgumentoCalidad = "-calidad";

        /// <summary>T-978: la calidad pedida con <see cref="ArgumentoCalidad"/>; false si no viene o no se entiende.</summary>
        public static bool CalidadDeArgumentos(string[] argumentos, out CalidadGrafica calidad)
        {
            calidad = CalidadPorDefecto;
            if (argumentos == null)
            {
                return false;
            }
            for (int i = 0; i < argumentos.Length - 1; i++)
            {
                if (!string.Equals(argumentos[i], ArgumentoCalidad, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                string valor = argumentos[i + 1].ToLowerInvariant();
                if (valor.StartsWith("baj"))
                {
                    calidad = CalidadGrafica.Baja;
                    return true;
                }
                if (valor.StartsWith("med"))
                {
                    calidad = CalidadGrafica.Media;
                    return true;
                }
                if (valor.StartsWith("alt"))
                {
                    calidad = CalidadGrafica.Alta;
                    return true;
                }
            }
            return false;
        }

        public static float DistanciaDibujo(CalidadGrafica calidad) => distanciasDibujo[(int)calidad];
        public static bool ReflejosCharcos(CalidadGrafica calidad) => reflejosCharcos[(int)calidad];
        public static bool Postproceso(CalidadGrafica calidad) => postprocesos[(int)calidad];
    }
}

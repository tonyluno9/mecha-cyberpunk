using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// V8–V10 (rama vr/quest3): las opciones de comodidad de VR. Las escribe la pantalla de OPCIONES (UI) y las lee el AdaptadorVR (VR)
    /// cada cuadro; viven en Core porque UI y VR no se referencian. Se guardan en PlayerPrefs al cambiar (como los subtítulos).
    /// Por defecto: giro SUAVE a 120°/s (VR18; POR PASOS queda como opción), viñeta SÍ y correr MANTENER.
    /// </summary>
    public static class OpcionesVR
    {
        public const string ClaveGiro = "opcion_vr_giro_v2";
        public const string ClaveVineta = "opcion_vr_vineta";
        public const string ClaveCorrer = "opcion_vr_correr";
        /// <summary>VR18: VELOCIDAD DE GIRO (°/s con el stick a fondo, giro SUAVE). Se guarda como entero.</summary>
        public const string ClaveVelocidadGiro = "opcion_vr_velocidad_giro";

        /// <summary>VR18: SUAVE por defecto (antes POR PASOS). La clave del giro cambió para que el valor viejo guardado no lo deje en pasos.</summary>
        public const ModoGiroVR GiroPorDefecto = ModoGiroVR.Suave;
        /// <summary>VR18: °/s del giro suave con el stick a fondo (proporcional al stick).</summary>
        public const float VelocidadGiroPorDefecto = 120f;
        public const float VelocidadGiroMinima = 30f;
        public const float VelocidadGiroMaxima = 180f;
        public const float PasoVelocidadGiro = 15f;
        public const bool VinetaPorDefecto = true;
        public const ModoCorrerVR CorrerPorDefecto = ModoCorrerVR.Mantener;

        private static bool cargadas;
        private static ModoGiroVR giro = GiroPorDefecto;
        private static bool vineta = VinetaPorDefecto;
        private static ModoCorrerVR correr = CorrerPorDefecto;
        private static float velocidadGiro = VelocidadGiroPorDefecto;

        /// <summary>Se dispara cuando cambia cualquiera de las opciones.</summary>
        public static event Action Cambio;

        /// <summary>GIRO: POR PASOS 45° o SUAVE.</summary>
        public static ModoGiroVR Giro
        {
            get
            {
                Cargar();
                return giro;
            }
        }

        /// <summary>VIÑETA: true = se cierra la vista al moverse y al girar.</summary>
        public static bool Vineta
        {
            get
            {
                Cargar();
                return vineta;
            }
        }

        /// <summary>CORRER: MANTENER o ALTERNAR.</summary>
        public static ModoCorrerVR Correr
        {
            get
            {
                Cargar();
                return correr;
            }
        }

        /// <summary>VR18: VELOCIDAD DE GIRO (°/s a fondo, entre <see cref="VelocidadGiroMinima"/> y <see cref="VelocidadGiroMaxima"/>).</summary>
        public static float VelocidadGiro
        {
            get
            {
                Cargar();
                return velocidadGiro;
            }
        }

        public static void FijarVelocidadGiro(float valor)
        {
            Cargar();
            valor = LimitarVelocidad(valor);
            PlayerPrefs.SetInt(ClaveVelocidadGiro, Mathf.RoundToInt(valor));
            PlayerPrefs.Save();
            if (!Mathf.Approximately(velocidadGiro, valor))
            {
                velocidadGiro = valor;
                Cambio?.Invoke();
            }
        }

        /// <summary>La velocidad dentro del rango y redondeada a grados enteros (NaN o fuera de rango quedan en el borde o por defecto).</summary>
        public static float LimitarVelocidad(float valor)
        {
            if (float.IsNaN(valor) || float.IsInfinity(valor))
            {
                return VelocidadGiroPorDefecto;
            }
            return Mathf.Round(Mathf.Clamp(valor, VelocidadGiroMinima, VelocidadGiroMaxima));
        }

        public static void FijarGiro(ModoGiroVR valor)
        {
            Cargar();
            valor = Enum.IsDefined(typeof(ModoGiroVR), valor) ? valor : GiroPorDefecto;
            PlayerPrefs.SetInt(ClaveGiro, (int)valor);
            PlayerPrefs.Save();
            if (giro != valor)
            {
                giro = valor;
                Cambio?.Invoke();
            }
        }

        public static void FijarVineta(bool valor)
        {
            Cargar();
            PlayerPrefs.SetInt(ClaveVineta, valor ? 1 : 0);
            PlayerPrefs.Save();
            if (vineta != valor)
            {
                vineta = valor;
                Cambio?.Invoke();
            }
        }

        public static void FijarCorrer(ModoCorrerVR valor)
        {
            Cargar();
            valor = Enum.IsDefined(typeof(ModoCorrerVR), valor) ? valor : CorrerPorDefecto;
            PlayerPrefs.SetInt(ClaveCorrer, (int)valor);
            PlayerPrefs.Save();
            if (correr != valor)
            {
                correr = valor;
                Cambio?.Invoke();
            }
        }

        /// <summary>Vuelve a leer lo guardado (lo que no esté guardado o no sea válido queda por defecto).</summary>
        public static void Recargar()
        {
            cargadas = false;
            Cargar();
        }

        private static void Cargar()
        {
            if (cargadas)
            {
                return;
            }
            cargadas = true;
            int g = PlayerPrefs.GetInt(ClaveGiro, (int)GiroPorDefecto);
            giro = Enum.IsDefined(typeof(ModoGiroVR), g) ? (ModoGiroVR)g : GiroPorDefecto;
            vineta = PlayerPrefs.GetInt(ClaveVineta, VinetaPorDefecto ? 1 : 0) != 0;
            int c = PlayerPrefs.GetInt(ClaveCorrer, (int)CorrerPorDefecto);
            correr = Enum.IsDefined(typeof(ModoCorrerVR), c) ? (ModoCorrerVR)c : CorrerPorDefecto;
            velocidadGiro = LimitarVelocidad(PlayerPrefs.GetInt(ClaveVelocidadGiro, Mathf.RoundToInt(VelocidadGiroPorDefecto)));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            cargadas = false;
            Cambio = null;
        }
    }
}

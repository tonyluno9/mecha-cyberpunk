using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// V8–V10 (rama vr/quest3): las opciones de comodidad de VR. Las escribe la pantalla de OPCIONES (UI) y las lee el AdaptadorVR (VR)
    /// cada cuadro; viven en Core porque UI y VR no se referencian. Se guardan en PlayerPrefs al cambiar (como los subtítulos).
    /// Por defecto: giro POR PASOS, viñeta SÍ y correr MANTENER.
    /// </summary>
    public static class OpcionesVR
    {
        public const string ClaveGiro = "opcion_vr_giro";
        public const string ClaveVineta = "opcion_vr_vineta";
        public const string ClaveCorrer = "opcion_vr_correr";

        public const ModoGiroVR GiroPorDefecto = ModoGiroVR.PorPasos;
        public const bool VinetaPorDefecto = true;
        public const ModoCorrerVR CorrerPorDefecto = ModoCorrerVR.Mantener;

        private static bool cargadas;
        private static ModoGiroVR giro = GiroPorDefecto;
        private static bool vineta = VinetaPorDefecto;
        private static ModoCorrerVR correr = CorrerPorDefecto;

        /// <summary>Se dispara cuando cambia cualquiera de las tres.</summary>
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
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            cargadas = false;
            Cambio = null;
        }
    }
}

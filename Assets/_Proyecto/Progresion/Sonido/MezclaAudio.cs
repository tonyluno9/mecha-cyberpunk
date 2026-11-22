using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>
    /// El AudioMixer del juego (Resources/MezclaCyberCity, lo crea CrearMezclaAudio): grupos Master, Musica, Efectos y Voces, cada uno
    /// con su volumen expuesto. Los deslizadores de opciones (0 a 1) se convierten a decibeles (0 = silencio de verdad, -80 dB).
    /// Ambiente e Interfaz van por Efectos.
    /// </summary>
    public static class MezclaAudio
    {
        public const string Recurso = "MezclaCyberCity";
        public const string GrupoMaster = "Master";
        public const string GrupoMusica = "Musica";
        public const string GrupoEfectos = "Efectos";
        public const string GrupoVoces = "Voces";
        public const string ParamGeneral = "VolGeneral";
        public const string ParamMusica = "VolMusica";
        public const string ParamEfectos = "VolEfectos";
        public const string ParamVoces = "VolVoces";
        /// <summary>El silencio del mezclador (dB).</summary>
        public const float Silencio = -80f;
        /// <summary>Por debajo de esto el deslizador cuenta como 0 (silencio).</summary>
        public const float MinimoAudible = 0.0001f;

        private static AudioMixer mezclador;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => mezclador = null;

        public static AudioMixer Mezclador
        {
            get
            {
                if (mezclador == null)
                {
                    mezclador = Resources.Load<AudioMixer>(Recurso);
                }
                return mezclador;
            }
        }

        /// <summary>Decibeles de un valor de deslizador de 0 a 1 (curva logarítmica; 0 es silencio).</summary>
        public static float Decibeles(float lineal)
        {
            return lineal <= MinimoAudible ? Silencio : Mathf.Max(Silencio, 20f * Mathf.Log10(Mathf.Min(lineal, 1f)));
        }

        /// <summary>El nombre del grupo del mezclador de una sección.</summary>
        public static string NombreGrupo(SeccionSonido seccion)
        {
            switch (seccion)
            {
                case SeccionSonido.General: return GrupoMaster;
                case SeccionSonido.Musica: return GrupoMusica;
                case SeccionSonido.Voces: return GrupoVoces;
                default: return GrupoEfectos;
            }
        }

        /// <summary>El parámetro expuesto que controla la sección (null para Ambiente e Interfaz: siguen a Efectos).</summary>
        public static string Parametro(SeccionSonido seccion)
        {
            switch (seccion)
            {
                case SeccionSonido.General: return ParamGeneral;
                case SeccionSonido.Musica: return ParamMusica;
                case SeccionSonido.Voces: return ParamVoces;
                case SeccionSonido.Efectos: return ParamEfectos;
                default: return null;
            }
        }

        public static AudioMixerGroup Grupo(SeccionSonido seccion)
        {
            AudioMixer m = Mezclador;
            if (m == null)
            {
                return null;
            }
            string nombre = NombreGrupo(seccion);
            AudioMixerGroup[] grupos = m.FindMatchingGroups(nombre);
            foreach (AudioMixerGroup g in grupos)
            {
                if (g.name == nombre)
                {
                    return g;
                }
            }
            return grupos.Length > 0 ? grupos[0] : null;
        }

        /// <summary>Aplica un deslizador al mezclador al instante. False si no hay mezclador o la sección no tiene parámetro propio.</summary>
        public static bool FijarVolumen(SeccionSonido seccion, float lineal)
        {
            string p = Parametro(seccion);
            return p != null && Mezclador != null && Mezclador.SetFloat(p, Decibeles(lineal));
        }
    }
}

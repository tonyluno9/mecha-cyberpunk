using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// T-1322: la radio de la policía suena cuando una patrulla llega y baja a sus policías, y cuando llegan los refuerzos de 5 estrellas. No hay sonido de radio en
    /// Assets/Arte/Audio: el ID vive en <see cref="ConfigPersecucion.IdSonidoRadio"/> y está vacío hasta que llegue el clip (vacío = no suena nada). Nunca más de
    /// una vez cada <see cref="SegundosEntreRadios"/> s, para que no sea una metralleta.
    /// </summary>
    public static class RadioPolicia
    {
        public const float SegundosEntreRadios = 15f;
        private static float proxima;

        /// <summary>Regla pura: suena si hay ID y ya pasó el enfriamiento.</summary>
        public static bool DebeSonar(string id, float ahora, float proximaPermitida) => !string.IsNullOrEmpty(id) && ahora >= proximaPermitida;

        /// <summary>Suena la radio en <paramref name="posicion"/> si toca. Devuelve true si la pidió al sistema de sonidos.</summary>
        public static bool Sonar(Vector3 posicion, ConfigPersecucion config)
        {
            string id = config != null ? config.IdSonidoRadio : null;
            if (!DebeSonar(id, Time.time, proxima))
            {
                return false;
            }
            proxima = Time.time + SegundosEntreRadios;
            Sonidos.ReproducirEn(id, posicion);
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => proxima = 0f;
    }
}

using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Avisos globales del montaje (T-444): un montable puede sacar al jugador de golpe (el carro que explota). Lo publica el montable
    /// (Vehiculos) después de permitirlo, y lo escucha el jugador a pie (Personaje), que baja si ese montable es el suyo.
    /// </summary>
    public static class EventosMontaje
    {
        /// <summary>El montable pide que quien lo ocupa baje ya, sin esperar a que frene.</summary>
        public static event Action<IMontable> SalidaForzada;

        public static void PublicarSalidaForzada(IMontable montable) => SalidaForzada?.Invoke(montable);

        /// <summary>Quita a todos los suscriptores. Para pruebas y para limpiar al entrar a Play.</summary>
        public static void LimpiarSuscriptores()
        {
            SalidaForzada = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => LimpiarSuscriptores();
    }
}

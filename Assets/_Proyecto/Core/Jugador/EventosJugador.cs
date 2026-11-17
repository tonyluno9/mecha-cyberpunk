using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// T-1328: avisos globales de la vida del jugador. <see cref="Desperto"/> lo publica el doctor (Progresion) cuando el jugador cae y despierta
    /// en la clínica; quien persigue o ataca al jugador (IA, Nova) lo escucha para soltarlo: al despertar todo vuelve a la normalidad.
    /// </summary>
    public static class EventosJugador
    {
        /// <summary>El jugador despertó con el doctor: se acabó la búsqueda y nadie lo persigue.</summary>
        public static event Action Desperto;

        public static void PublicarDesperto() => Desperto?.Invoke();

        /// <summary>Quita a todos los suscriptores. Para pruebas y para limpiar al entrar a Play.</summary>
        public static void LimpiarSuscriptores() => Desperto = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => LimpiarSuscriptores();
    }
}

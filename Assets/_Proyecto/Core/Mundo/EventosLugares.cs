using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Avisos globales de los lugares, para que las áreas se hablen sin referenciarse.
    /// Los publica Alan (las zonas de cada lugar, y cada <see cref="ILugar"/> al abrir o cerrar).
    /// Los escucha quien quiera (Tony: calor que baja en casa, objetivos, misiones).
    /// </summary>
    public static class EventosLugares
    {
        /// <summary>El jugador entró a un lugar.</summary>
        public static event Action<IdLugar> JugadorEntro;
        /// <summary>El jugador salió de un lugar.</summary>
        public static event Action<IdLugar> JugadorSalio;
        /// <summary>Un lugar abrió (true) o cerró (false).</summary>
        public static event Action<IdLugar, bool> EstadoLugarCambio;

        public static void PublicarEntro(IdLugar lugar) => JugadorEntro?.Invoke(lugar);
        public static void PublicarSalio(IdLugar lugar) => JugadorSalio?.Invoke(lugar);
        public static void PublicarEstadoLugar(IdLugar lugar, bool abierto) => EstadoLugarCambio?.Invoke(lugar, abierto);

        /// <summary>Quita a todos los suscriptores. Para pruebas y para limpiar al entrar a Play.</summary>
        public static void LimpiarSuscriptores()
        {
            JugadorEntro = null;
            JugadorSalio = null;
            EstadoLugarCambio = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => LimpiarSuscriptores();
    }
}

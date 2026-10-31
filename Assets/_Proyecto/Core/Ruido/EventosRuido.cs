using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Avisos globales de ruido (T-443): quien dispara publica dónde, y quien oye (peatones que huyen, tráfico que frena) decide qué hace
    /// según la distancia. Así las áreas se hablan sin referenciarse. Lo publican Personaje (la pistola del jugador), IA (pandilleros) y
    /// Vehiculos (disparar desde el carro); lo escuchan Trafico y quien lo necesite.
    /// </summary>
    public static class EventosRuido
    {
        /// <summary>Alguien disparó en esta posición del mundo.</summary>
        public static event Action<Vector3> Disparo;

        public static void PublicarDisparo(Vector3 posicion) => Disparo?.Invoke(posicion);

        /// <summary>Quita a todos los suscriptores. Para pruebas y para limpiar al entrar a Play.</summary>
        public static void LimpiarSuscriptores()
        {
            Disparo = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => LimpiarSuscriptores();
    }
}

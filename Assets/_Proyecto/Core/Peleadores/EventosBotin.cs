using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>Arma que suelta un enemigo al caer (T-686).</summary>
    public enum ArmaBotin
    {
        Ninguna,
        Pistola,
        Escopeta
    }

    /// <summary>Lo que suelta un enemigo al caer: dónde, cuánto dinero y qué arma (T-686).</summary>
    public readonly struct InfoBotin
    {
        public Vector3 Posicion { get; }
        public int Dinero { get; }
        public ArmaBotin Arma { get; }

        public InfoBotin(Vector3 posicion, int dinero, ArmaBotin arma)
        {
            Posicion = posicion;
            Dinero = dinero;
            Arma = arma;
        }
    }

    /// <summary>
    /// Aviso de que un enemigo cayó y suelta su botín (T-686). Lo publica quien maneja al enemigo (IA/) y lo atiende el jugador
    /// (Personaje/, <c>ColocadorBotin</c>), que pone el arma en el piso y el dinero para recoger. Así IA no depende de Personaje.
    /// </summary>
    public static class EventosBotin
    {
        /// <summary>Se dispara cuando un enemigo suelta su botín.</summary>
        public static event Action<InfoBotin> Soltado;

        public static void Publicar(InfoBotin info) => Soltado?.Invoke(info);

        /// <summary>Quita a todos los que escuchan (para pruebas).</summary>
        public static void LimpiarSuscriptores() => Soltado = null;
    }
}

using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// Puerta de un lugar. Lo implementa Alan (Mundo/) y la agrega a <c>Registro&lt;IPuerta&gt;</c>.
    /// Quien interactúa (el jugador de Tony en T-F3, o la cámara libre de Alan) llama a
    /// <see cref="IntentarAbrir"/>; la puerta decide según el lugar al que pertenece.
    /// Un lugar cerrado tiene la puerta cerrada y un letrero con el horario.
    /// </summary>
    public interface IPuerta
    {
        /// <summary>Lugar al que da esta puerta.</summary>
        IdLugar Lugar { get; }

        /// <summary>True si la hoja está abierta y se puede pasar.</summary>
        bool EstaAbierta { get; }

        /// <summary>Posición de la puerta en el mundo (m).</summary>
        Vector3 Posicion { get; }

        /// <summary>
        /// Intenta abrir. Orden de decisión: 1) si ya estaba abierta, <c>YaAbierta</c>; 2) si el jugador está dentro
        /// del lugar, abre siempre (para poder salir) y devuelve <c>Abierta</c>; 3) si el lugar está cerrado por algo
        /// que no depende de la hora (cerrado en la demo), <c>Bloqueada</c>; 4) si está fuera de horario,
        /// <c>CerradaPorHorario</c>; 5) si no, la abre y devuelve <c>Abierta</c>.
        /// Solo avisa <c>Cambio</c> cuando de verdad abre.
        /// </summary>
        ResultadoPuerta IntentarAbrir();

        /// <summary>Cierra la hoja. Si ya estaba cerrada no hace nada. También la cierra el propio lugar al cerrar.</summary>
        void Cerrar();

        /// <summary>Avisa al cambiar <c>EstaAbierta</c>: true al abrir, false al cerrar.</summary>
        event Action<bool> Cambio;
    }
}

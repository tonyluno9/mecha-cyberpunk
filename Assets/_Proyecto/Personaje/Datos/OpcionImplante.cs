using System;
using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>Opción de implante visible (la 0 es "Ninguno").</summary>
    [Serializable]
    public struct OpcionImplante
    {
        [Tooltip("Nombre corto que ve el jugador en la pantalla.")] public string nombre;
        [Tooltip("Qué implante se muestra.")] public ImplanteApariencia implante;
        [Tooltip("Color del implante.")] public Color color;
    }
}

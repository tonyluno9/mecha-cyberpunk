using System;
using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>Opción de cara: tono de piel y peinado.</summary>
    [Serializable]
    public struct OpcionCara
    {
        [Tooltip("Nombre corto que ve el jugador en la pantalla.")] public string nombre;
        [Tooltip("Color de la piel (cabeza y manos).")] public Color piel;
        [Tooltip("Pieza de peinado que se muestra.")] public PeinadoApariencia peinado;
        [Tooltip("Color del peinado.")] public Color colorPeinado;
    }
}

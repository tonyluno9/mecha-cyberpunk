using System;
using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>Opción de ropa: colores y una pieza distintiva.</summary>
    [Serializable]
    public struct OpcionRopa
    {
        [Tooltip("Nombre corto que ve el jugador en la pantalla.")] public string nombre;
        [Tooltip("Color del torso.")] public Color color;
        [Tooltip("Color de las piernas.")] public Color pantalon;
        [Tooltip("Si los brazos llevan el color de la ropa (mangas) o la piel.")] public bool conMangas;
        [Tooltip("Pieza distintiva que se muestra.")] public PiezaRopaApariencia pieza;
        [Tooltip("Color de la pieza distintiva.")] public Color colorPieza;
    }
}

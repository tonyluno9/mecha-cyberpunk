using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>
    /// Una zona del barrio (T-404, D-tony-217): id estable, nombre, dueño inicial, color y rectángulo en el suelo (X, Z).
    /// Assets: DatosZona_Fosa, _Tianguis y _Neon.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de zona", fileName = "DatosZona_Nueva")]
    public class DatosZona : ScriptableObject
    {
        [Tooltip("Id estable (minúsculas): fosa, tianguis, neon.")]
        [SerializeField] private string id = "zona";
        [SerializeField] private string nombre = "ZONA";
        [SerializeField] private IdPandilla duenoInicial = IdPandilla.Ninguna;
        [SerializeField] private Color color = Color.white;
        [Tooltip("Rectángulo en el suelo: x = X mínima, y = Z mínima, ancho en X, alto en Z (m).")]
        [SerializeField] private Rect rectangulo = new Rect(-100f, -100f, 200f, 200f);
        [Min(0)] [SerializeField] private int respetoInicial;

        public string Id => id;
        public string Nombre => nombre;
        public IdPandilla DuenoInicial => duenoInicial;
        public Color Color => color;
        public Rect Rectangulo => rectangulo;
        public int RespetoInicial => respetoInicial;
    }
}

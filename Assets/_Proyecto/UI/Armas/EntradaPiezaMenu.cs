using System;
using UnityEngine;

namespace MechaCyberpunk.UI.Armas
{
    /// <summary>Cómo se presenta una pieza en el banco de Tino (T-814): nombre, qué mejora y qué estadística resalta.</summary>
    [Serializable]
    public class EntradaPiezaMenu
    {
        [SerializeField] private PiezaArma pieza;
        [Tooltip("Nombre de la pieza (CARGADOR).")]
        [SerializeField] private string nombre = "CARGADOR";
        [Tooltip("Lo que mejora, corto (MÁS BALAS).")]
        [SerializeField] private string mejora = "MÁS BALAS";
        [Tooltip("La estadística que se resalta en la tabla antes/después.")]
        [SerializeField] private EstadisticaArma estadistica = EstadisticaArma.Cargador;
        [Tooltip("Cuánto se separa la pieza del arma en la vista explotada (fracción del largo del arma).")]
        [Range(0f, 1f)] [SerializeField] private float separacion = 0.32f;

        public PiezaArma Pieza => pieza;
        public string Nombre => nombre;
        public string Mejora => mejora;
        public EstadisticaArma Estadistica => estadistica;
        public float Separacion => separacion;

        public static EntradaPiezaMenu Crear(PiezaArma pieza, string nombre, string mejora, EstadisticaArma estadistica, float separacion)
        {
            return new EntradaPiezaMenu { pieza = pieza, nombre = nombre, mejora = mejora, estadistica = estadistica, separacion = separacion };
        }
    }
}

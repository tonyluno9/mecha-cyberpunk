using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1511: marca un prop como cobertura de combate (barrera de concreto, costales, cajas de metal, barricada de policía: CB01 a CB04).
    /// <see cref="PandilleroAPie"/> la acepta aunque sea más baja que un muro normal (un tirador agachado queda tapado) y la prefiere a otra cubierta cercana.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CoberturaCB : MonoBehaviour
    {
        /// <summary>Metros que se le descuentan a la distancia de una cobertura CB al compararla con otras: se prefiere aunque quede un poco más lejos.</summary>
        public const float BonoPreferencia = 6f;

        /// <summary>Altura de cobertura a la que se escaló el prop, en metros (informativa, la fija el creador).</summary>
        [SerializeField] private float altura = 1f;

        public float Altura
        {
            get => altura;
            set => altura = value;
        }
    }
}

using UnityEngine;

namespace MechaCyberpunk.Economia
{
    /// <summary>Los números de la economía de la demo en un solo lugar (valores provisionales, D-tony-44).</summary>
    [CreateAssetMenu(fileName = "DatosEconomia", menuName = "Mecha Cyberpunk/Datos de economía")]
    public class DatosEconomia : ScriptableObject
    {
        [Tooltip("Dinero con el que empieza el jugador ($).")]
        [SerializeField] private int dineroInicial = 500;
        [Tooltip("Costo de reparar cada punto de vida perdido ($/punto).")]
        [Min(0)] [SerializeField] private int costoReparacionPorPunto = 10;
        [Tooltip("Costo mínimo de una reparación, si hay algún daño ($).")]
        [Min(0)] [SerializeField] private int costoReparacionMinimo = 50;
        [Tooltip("Lo que cuesta cada renta ($).")]
        [Min(0)] [SerializeField] private int renta = 500;
        [Tooltip("Días de juego entre una renta y la siguiente (días).")]
        [Min(1)] [SerializeField] private int diasEntreRentas = 7;

        public int DineroInicial => dineroInicial;
        public int CostoReparacionPorPunto => costoReparacionPorPunto;
        public int CostoReparacionMinimo => costoReparacionMinimo;
        public int Renta => renta;
        public int DiasEntreRentas => diasEntreRentas;
    }
}

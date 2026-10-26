using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Números de las dos reputaciones (D-tony-145): rango, umbrales de nivel, nombres y efecto en el calor.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de reputación", fileName = "DatosReputacion_Nuevo")]
    public class DatosReputacion : ScriptableObject
    {
        [Tooltip("Valor mínimo de una reputación.")]
        [SerializeField] private int minimo = -100;

        [Tooltip("Valor máximo de una reputación.")]
        [SerializeField] private int maximo = 100;

        [Tooltip("Valor absoluto desde el que se está en el nivel 1 y en el nivel 2 (simétrico para los negativos), en orden.")]
        [SerializeField] private int[] umbrales = { 15, 50 };

        [Tooltip("Nombres de los 5 niveles del barrio, de -2 a 2.")]
        [SerializeField] private string[] nombresBarrio = { "TRAIDOR", "MAL VISTO", "DESCONOCIDO", "CONOCIDO", "JUSTICIERO" };

        [Tooltip("Nombres de los 5 niveles de las corporaciones, de -2 a 2.")]
        [SerializeField] private string[] nombresCorporaciones = { "ENEMIGO", "FICHADO", "SIN FICHA", "CONTRATISTA", "SOCIO" };

        [Tooltip("Multiplicador del calor que se suma según el nivel de corporaciones, de -2 a 2.")]
        [SerializeField] private float[] multiplicadorCalor = { 1.3f, 1.15f, 1f, 1f, 1f };

        public int Minimo => minimo;
        public int Maximo => maximo;
        public int[] Umbrales => umbrales;
        public string[] NombresBarrio => nombresBarrio;
        public string[] NombresCorporaciones => nombresCorporaciones;
        public float[] MultiplicadorCalor => multiplicadorCalor;

        private void OnValidate()
        {
            if (minimo > 0) minimo = 0;
            if (maximo < 0) maximo = 0;
            if (umbrales == null || umbrales.Length != 2) umbrales = new[] { 15, 50 };
            umbrales[0] = Mathf.Max(1, umbrales[0]);
            umbrales[1] = Mathf.Max(umbrales[0], umbrales[1]);
            if (nombresBarrio == null || nombresBarrio.Length != 5) nombresBarrio = new[] { "TRAIDOR", "MAL VISTO", "DESCONOCIDO", "CONOCIDO", "JUSTICIERO" };
            if (nombresCorporaciones == null || nombresCorporaciones.Length != 5) nombresCorporaciones = new[] { "ENEMIGO", "FICHADO", "SIN FICHA", "CONTRATISTA", "SOCIO" };
            if (multiplicadorCalor == null || multiplicadorCalor.Length != 5) multiplicadorCalor = new[] { 1.3f, 1.15f, 1f, 1f, 1f };
        }
    }
}

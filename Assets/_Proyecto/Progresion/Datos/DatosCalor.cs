using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Números del calor policial (D-tony-8).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de calor", fileName = "DatosCalor_Nuevo")]
    public class DatosCalor : ScriptableObject
    {
        [Tooltip("Calor máximo (puntos).")]
        [Min(1f)] [SerializeField] private float maximo = 100f;

        [Tooltip("Valores desde los que se está en el nivel 1, 2 y 3 (puntos, en orden).")]
        [SerializeField] private float[] umbralesNivel = { 25f, 50f, 75f };

        [Tooltip("Segundos sin sumar calor antes de que empiece a bajar (s).")]
        [Min(0f)] [SerializeField] private float retrasoEnfriamiento = 10f;

        [Tooltip("Cuánto baja el calor por segundo una vez que empieza a enfriarse (puntos/s).")]
        [Min(0f)] [SerializeField] private float enfriamientoPorSegundo = 5f;

        public float Maximo => maximo;
        public float[] UmbralesNivel => umbralesNivel;
        public float RetrasoEnfriamiento => retrasoEnfriamiento;
        public float EnfriamientoPorSegundo => enfriamientoPorSegundo;

        private void OnValidate()
        {
            if (maximo < 1f) maximo = 1f;
            if (umbralesNivel == null || umbralesNivel.Length != 3) umbralesNivel = new[] { 25f, 50f, 75f };
            float anterior = 0f;
            for (int i = 0; i < umbralesNivel.Length; i++)
            {
                umbralesNivel[i] = Mathf.Clamp(umbralesNivel[i], anterior, maximo);
                anterior = umbralesNivel[i];
            }
        }
    }
}

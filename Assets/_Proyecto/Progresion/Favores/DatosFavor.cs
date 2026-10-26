using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Un tipo de favor (D-tony-146): nombre, descripción, cuántos caben en el bolsillo y su valor.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de favor", fileName = "DatosFavor_Nuevo")]
    public class DatosFavor : ScriptableObject
    {
        [Tooltip("Identificador estable del favor.")]
        [SerializeField] private string id = "descuento";

        [Tooltip("Nombre corto para pantallas.")]
        [SerializeField] private string nombre = "DESCUENTO";

        [Tooltip("Descripción corta.")]
        [SerializeField] private string descripcion = "";

        [Tooltip("Tipo de favor.")]
        [SerializeField] private TipoFavor tipo = TipoFavor.Descuento;

        [Tooltip("Cuántos favores de este tipo caben en el bolsillo.")]
        [Min(1)] [SerializeField] private int maximo = 3;

        [Tooltip("Descuento: fracción de rebaja (0.3 = 30 %). Los otros tipos no lo usan.")]
        [Range(0f, 1f)] [SerializeField] private float valor = 0.3f;

        public string Id => id;
        public string Nombre => nombre;
        public string Descripcion => descripcion;
        public TipoFavor Tipo => tipo;
        public int Maximo => maximo;
        public float Valor => valor;

        private void OnValidate()
        {
            if (maximo < 1) maximo = 1;
            valor = Mathf.Clamp01(valor);
        }
    }
}

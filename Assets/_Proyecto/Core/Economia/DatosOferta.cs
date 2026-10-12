using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>Una cosa que se vende: nombre, descripción y precio.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de oferta", fileName = "DatosOferta_Nueva")]
    public class DatosOferta : ScriptableObject
    {
        [Tooltip("Identificador único (texto).")]
        [SerializeField] private string id = "oferta";

        [Tooltip("Nombre que ve el jugador (texto).")]
        [SerializeField] private string nombre = "Oferta";

        [Tooltip("Descripción que ve el jugador (texto).")]
        [SerializeField] private string descripcion = "";

        [Tooltip("Precio (pesos, entero).")]
        [Min(0)] [SerializeField] private int precio;

        [Tooltip("Clase de oferta.")]
        [SerializeField] private TipoOferta tipo;

        public string Id => id;
        public string Nombre => nombre;
        public string Descripcion => descripcion;
        public int Precio => precio;
        public TipoOferta Tipo => tipo;
    }
}

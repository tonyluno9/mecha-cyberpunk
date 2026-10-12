using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>Lo que se vende en un lugar.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de tienda", fileName = "DatosTienda_Nueva")]
    public class DatosTienda : ScriptableObject
    {
        [Tooltip("Lugar donde está esta tienda.")]
        [SerializeField] private IdLugar lugar;

        [Tooltip("Nombre de la tienda (texto).")]
        [SerializeField] private string nombre = "Tienda";

        [Tooltip("Ofertas a la venta.")]
        [SerializeField] private List<DatosOferta> ofertas = new List<DatosOferta>();

        public IdLugar Lugar => lugar;
        public string Nombre => nombre;
        public IReadOnlyList<DatosOferta> Ofertas => ofertas;

        private void OnValidate()
        {
            var vistos = new HashSet<string>();
            foreach (var oferta in ofertas)
            {
                if (oferta == null)
                {
                    Debug.LogWarning($"[Economia] {name}: hay una oferta vacía en la lista.", this);
                    continue;
                }
                if (!vistos.Add(oferta.Id))
                    Debug.LogWarning($"[Economia] {name}: el id de oferta '{oferta.Id}' está repetido.", this);
            }
        }
    }
}

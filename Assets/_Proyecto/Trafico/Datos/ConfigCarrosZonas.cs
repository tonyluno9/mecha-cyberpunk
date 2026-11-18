using System;
using System.Collections.Generic;
using MechaCyberpunk.Trafico.Calle;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Datos
{
    /// <summary>T-1345: qué carros salen en cada zona de la ciudad, y los que se suman a la lista general en todas partes. Vive en Resources/ConfigCarrosZonas.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Trafico/ConfigCarrosZonas", fileName = "ConfigCarrosZonas")]
    public class ConfigCarrosZonas : ScriptableObject
    {
        [Serializable]
        public class Zona
        {
            [Tooltip("Debe casar con el nombre de la ZonaTrafico.")]
            public string nombre;
            public List<CarroTrafico> carros = new List<CarroTrafico>();
            [Tooltip("Probabilidad (0 a 1) de sacar un carro de esta zona en vez de la lista general.")]
            [Range(0f, 1f)] public float mezcla = 0.5f;
        }

        [Tooltip("Carros que se suman a la lista general en toda la ciudad.")]
        [SerializeField] private List<CarroTrafico> extrasGenerales = new List<CarroTrafico>();
        [SerializeField] private List<Zona> zonas = new List<Zona>();

        public IReadOnlyList<CarroTrafico> ExtrasGenerales => extrasGenerales;
        public IReadOnlyList<Zona> Zonas => zonas;

        /// <summary>La zona con ese nombre, o null.</summary>
        public Zona Buscar(string nombreZona)
        {
            foreach (Zona z in zonas)
            {
                if (z != null && z.nombre == nombreZona)
                {
                    return z;
                }
            }
            return null;
        }
    }
}

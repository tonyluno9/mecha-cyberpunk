using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-1409: qué visuales de peatón salen en cada zona de la ciudad y con qué peso. Los nombres casan con ZonaTrafico (Malecon, Puerto, Cerro, Autopista).
    /// Vive en Personaje.Visual (y no en Personaje) porque PeatonVariado, que la lee, está en este ensamblado. El asset va en Personaje/Datos/.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Personaje/ConfigPeatonesZona", fileName = "ConfigPeatonesZona")]
    public class ConfigPeatonesZona : ScriptableObject
    {
        [Serializable]
        public class Entrada
        {
            [Tooltip("Visual_NPxx_* que sale.")]
            public GameObject visual;
            [Tooltip("Peso relativo (puntos) frente a los demás de la zona.")]
            [Min(0f)] public float peso = 1f;
        }

        [Serializable]
        public class Zona
        {
            [Tooltip("Debe casar con el nombre de la ZonaTrafico.")]
            public string nombre;
            [Tooltip("Peso (puntos) de la persona de color de siempre en esta zona.")]
            [Min(0f)] public float pesoPersona = 2f;
            public List<Entrada> entradas = new List<Entrada>();
        }

        [SerializeField] private List<Zona> zonas = new List<Zona>();

        public IReadOnlyList<Zona> Zonas => zonas;

        /// <summary>Lo usa el creador de editor.</summary>
        public void Definir(List<Zona> nuevas)
        {
            zonas = nuevas ?? new List<Zona>();
        }

        /// <summary>La zona con ese nombre, o null.</summary>
        public Zona Buscar(string nombreZona)
        {
            string[] nombres = new string[zonas.Count];
            for (int i = 0; i < zonas.Count; i++)
            {
                nombres[i] = zonas[i] != null ? zonas[i].nombre : null;
            }
            int indice = SelectorPeatonZona.BuscarZona(nombres, nombreZona);
            return indice >= 0 ? zonas[indice] : null;
        }

        /// <summary>
        /// Elige el visual para <paramref name="nombreZona"/> con la <paramref name="tirada"/> (0 a 1). <paramref name="hayZona"/> es false si la zona
        /// no está en la tabla (el llamador usa entonces la lista general). Devuelve null si toca la persona de siempre.
        /// </summary>
        public GameObject Elegir(string nombreZona, float tirada, out bool hayZona)
        {
            Zona zona = Buscar(nombreZona);
            hayZona = zona != null;
            if (zona == null)
            {
                return null;
            }
            float[] pesos = new float[zona.entradas.Count];
            for (int i = 0; i < pesos.Length; i++)
            {
                Entrada e = zona.entradas[i];
                pesos[i] = e != null && e.visual != null ? e.peso : 0f;
            }
            int indice = SelectorPeatonZona.Elegir(pesos, zona.pesoPersona, tirada);
            return indice == SelectorPeatonZona.Persona ? null : zona.entradas[indice].visual;
        }
    }
}

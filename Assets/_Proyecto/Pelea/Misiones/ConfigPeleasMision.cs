using System;
using System.Collections.Generic;
using MechaCyberpunk.Pelea.Datos;
using UnityEngine;

namespace MechaCyberpunk.Pelea.Misiones
{
    /// <summary>
    /// Qué emboscada le toca a cada misión con un paso Pelear (T-185): una tabla id de misión a <see cref="DatosEmboscada"/>.
    /// Los datos de misión no fijan objetivos: la misión pone los suyos.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Pelea/Config peleas de misión", fileName = "ConfigPeleasMision")]
    public class ConfigPeleasMision : ScriptableObject
    {
        /// <summary>Una fila de la tabla.</summary>
        [Serializable]
        public struct Entrada
        {
            [Tooltip("Id de la misión (por ejemplo barrio_callejon).")]
            public string idMision;
            [Tooltip("La pelea que se arma en el punto de la misión.")]
            public DatosEmboscada datos;
        }

        [Tooltip("Una fila por cada misión con paso Pelear.")]
        [SerializeField] private Entrada[] entradas = new Entrada[0];

        public IReadOnlyList<Entrada> Entradas => entradas;

        /// <summary>Los datos de la pelea de esa misión, o null si no tiene.</summary>
        public DatosEmboscada Buscar(string idMision)
        {
            foreach (Entrada entrada in entradas)
            {
                if (entrada.idMision == idMision) return entrada.datos;
            }
            return null;
        }
    }
}

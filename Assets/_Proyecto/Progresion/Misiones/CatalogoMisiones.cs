using System;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Lista de todas las misiones del juego, en orden (T-183).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Catálogo de misiones", fileName = "CatalogoMisiones_Nuevo")]
    public class CatalogoMisiones : ScriptableObject
    {
        [Tooltip("Las misiones, en orden.")]
        [SerializeField] private DatosMision[] misiones = Array.Empty<DatosMision>();

        public DatosMision[] Misiones => misiones ?? Array.Empty<DatosMision>();

        /// <summary>La misión con ese id, o null.</summary>
        public DatosMision Buscar(string id)
        {
            foreach (DatosMision m in Misiones)
            {
                if (m != null && m.Id == id) return m;
            }
            return null;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Economia
{
    /// <summary>Lista de todos los objetos del juego; busca por id (T-142).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Catálogo de objetos", fileName = "CatalogoObjetos_Nuevo")]
    public class CatalogoObjetos : ScriptableObject
    {
        [Tooltip("Todos los objetos (cada uno con id único).")]
        [SerializeField] private List<DatosObjeto> objetos = new List<DatosObjeto>();

        public IReadOnlyList<DatosObjeto> Objetos => objetos;

        public DatosObjeto Buscar(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (DatosObjeto o in objetos)
            {
                if (o != null && o.Id == id) return o;
            }
            return null;
        }

        /// <summary>Crea un catálogo en memoria (para pruebas).</summary>
        public static CatalogoObjetos Crear(params DatosObjeto[] lista)
        {
            var c = CreateInstance<CatalogoObjetos>();
            c.objetos = new List<DatosObjeto>(lista);
            return c;
        }
    }
}

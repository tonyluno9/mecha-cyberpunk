using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Lista de todos los implantes del juego (T-191).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Catálogo de implantes", fileName = "CatalogoImplantes_Nuevo")]
    public class CatalogoImplantes : ScriptableObject
    {
        [Tooltip("Los implantes, en orden.")]
        [SerializeField] private DatosImplante[] implantes = Array.Empty<DatosImplante>();

        public DatosImplante[] Implantes => implantes ?? Array.Empty<DatosImplante>();

        /// <summary>El implante con ese id, o null.</summary>
        public DatosImplante Buscar(string id)
        {
            foreach (DatosImplante d in Implantes)
            {
                if (d != null && d.Id == id) return d;
            }
            return null;
        }

        /// <summary>El implante de ese espacio y nivel, o null.</summary>
        public DatosImplante Buscar(EspacioImplante espacio, int nivel)
        {
            foreach (DatosImplante d in Implantes)
            {
                if (d != null && d.Espacio == espacio && d.Nivel == nivel) return d;
            }
            return null;
        }

        /// <summary>Para las pruebas.</summary>
        public static CatalogoImplantes Crear(params DatosImplante[] lista)
        {
            var c = CreateInstance<CatalogoImplantes>();
            c.implantes = lista;
            return c;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Mecha
{
    /// <summary>Lista de todas las piezas del mecha (T-166).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Catálogo de piezas", fileName = "CatalogoPiezas_Nuevo")]
    public class CatalogoPiezas : ScriptableObject
    {
        [Tooltip("Todas las piezas (cada una con id único).")]
        [SerializeField] private List<DatosPieza> piezas = new List<DatosPieza>();

        public IReadOnlyList<DatosPieza> Piezas => piezas;

        /// <summary>La pieza de serie de la ranura, o null.</summary>
        public DatosPieza DeSerie(RanuraPieza ranura)
        {
            foreach (DatosPieza p in piezas)
            {
                if (p != null && p.Ranura == ranura && p.Rama == RamaPieza.DeSerie) return p;
            }
            return null;
        }

        /// <summary>Todas las piezas de la ranura (de serie incluida).</summary>
        public List<DatosPieza> De(RanuraPieza ranura)
        {
            var lista = new List<DatosPieza>();
            foreach (DatosPieza p in piezas)
            {
                if (p != null && p.Ranura == ranura) lista.Add(p);
            }
            return lista;
        }

        public DatosPieza Buscar(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (DatosPieza p in piezas)
            {
                if (p != null && p.Id == id) return p;
            }
            return null;
        }

        /// <summary>Crea un catálogo en memoria (para pruebas).</summary>
        public static CatalogoPiezas Crear(params DatosPieza[] lista)
        {
            var c = CreateInstance<CatalogoPiezas>();
            c.piezas = new List<DatosPieza>(lista);
            return c;
        }
    }
}

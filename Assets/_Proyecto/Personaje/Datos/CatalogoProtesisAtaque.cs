using System.Collections.Generic;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Habilidades;
using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>
    /// T-1017: lo que vende el Doc en "PRÓTESIS DE ATAQUE" (la pestaña de la clínica que se abre con Tab): las prótesis y la tienda con sus ofertas.
    /// Vive en Personaje/Datos/Resources para cargarlo sin referencias de escena.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Catálogo de prótesis de ataque", fileName = "CatalogoProtesisAtaque")]
    public class CatalogoProtesisAtaque : ScriptableObject
    {
        public const string NombreRecurso = "CatalogoProtesisAtaque";
        private static CatalogoProtesisAtaque actual;

        [SerializeField] private List<DatosProtesisAtaque> protesis = new List<DatosProtesisAtaque>();
        [Tooltip("La tienda del Doc con las ofertas de las prótesis.")]
        [SerializeField] private MechaCyberpunk.Core.DatosTienda tienda;

        public IReadOnlyList<DatosProtesisAtaque> Protesis => protesis;
        public MechaCyberpunk.Core.DatosTienda Tienda => tienda;

        public static CatalogoProtesisAtaque Actual => actual != null ? actual : actual = Resources.Load<CatalogoProtesisAtaque>(NombreRecurso);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => actual = null;

        /// <summary>La prótesis que da esa habilidad, o null.</summary>
        public DatosProtesisAtaque De(HabilidadProtesis habilidad)
        {
            foreach (DatosProtesisAtaque p in protesis)
            {
                if (p != null && p.Habilidad == habilidad)
                {
                    return p;
                }
            }
            return null;
        }

        /// <summary>True si el inventario tiene la prótesis de esa habilidad.</summary>
        public bool Tiene(HabilidadProtesis habilidad, ServicioInventario inventario)
        {
            DatosProtesisAtaque p = De(habilidad);
            return p != null && inventario != null && inventario.Cantidad(p) > 0;
        }
    }
}

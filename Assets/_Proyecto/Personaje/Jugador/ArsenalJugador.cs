using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// Qué armas tiene el jugador a pie y en qué nivel (T-403). El nivel de un arma es el más alto de sus <see cref="DatosArma"/> en el
    /// <see cref="ServicioInventario"/>. Los puños son nivel 0 desde el inicio; la pistola también si <c>pistolaAlInicio</c> (en T-406 se
    /// consigue en la historia). Comprar el nivel n pide tener el n-1. Se registra como <c>Registro&lt;ArsenalJugador&gt;</c>.
    /// </summary>
    public class ArsenalJugador : MonoBehaviour
    {
        [SerializeField] private CatalogoArmas catalogo;
        [Tooltip("True: la pistola se tiene desde el inicio. T-406 la pone en false y la consigue en la historia.")]
        [SerializeField] private bool pistolaAlInicio = true;

        public CatalogoArmas Catalogo => catalogo;
        public bool PistolaAlInicio => pistolaAlInicio;

        private void OnEnable() => Registro<ArsenalJugador>.Agregar(this);

        private void OnDisable() => Registro<ArsenalJugador>.Quitar(this);

        /// <summary>Nivel del arma (0 a 3) o -1 si no la tiene.</summary>
        public int Nivel(ArmaAPie arma)
        {
            int nivel = arma == ArmaAPie.Punos || (arma == ArmaAPie.Pistola && pistolaAlInicio) ? 0 : -1;
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            if (inventario == null)
            {
                return nivel;
            }
            foreach (DatosObjeto objeto in inventario.Todos)
            {
                if (objeto is DatosArma datos && datos.Arma == arma && datos.Nivel > nivel)
                {
                    nivel = datos.Nivel;
                }
            }
            return nivel;
        }

        public bool Tiene(ArmaAPie arma) => Nivel(arma) >= 0;

        /// <summary>Los datos del arma en su nivel actual, o null si no la tiene.</summary>
        public DatosArma Datos(ArmaAPie arma)
        {
            int nivel = Nivel(arma);
            return nivel >= 0 && catalogo != null ? catalogo.Buscar(arma, nivel) : null;
        }

        /// <summary>True si este nivel es el que sigue (el n-1 ya se tiene): la tienda solo muestra esa mejora de cada arma.</summary>
        public bool EsSiguiente(DatosArma oferta) => oferta != null && oferta.Nivel == Nivel(oferta.Arma) + 1;

        /// <summary>
        /// Regala el nivel que sigue del arma, al menos el I (T-418: EL INVENTOR mejora la pistola). Lo mete al inventario sin cobrar.
        /// Devuelve sus datos, o null si ya está al máximo o falta el catálogo o el inventario.
        /// </summary>
        public DatosArma DarMejora(ArmaAPie arma)
        {
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            DatosArma siguiente = catalogo != null ? catalogo.Buscar(arma, Mathf.Max(1, Nivel(arma) + 1)) : null;
            if (siguiente == null || inventario == null || inventario.Agregar(siguiente, 1) <= 0)
            {
                return null;
            }
            return siguiente;
        }
    }
}

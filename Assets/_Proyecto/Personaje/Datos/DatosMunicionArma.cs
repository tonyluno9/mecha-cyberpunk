using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>
    /// Balas de un arma a pie (T-720). Es un objeto de tienda: cada compra suma <see cref="BalasPorPaquete"/> a la reserva del arma
    /// (<c>ReservaBalas</c>) en vez de ir al inventario. <see cref="DatosObjeto.Maximo"/> es el tope de balas de reserva.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de munición de arma", fileName = "DatosMunicionArma_Nueva")]
    public class DatosMunicionArma : DatosObjeto
    {
        [Tooltip("Arma que usa estas balas.")]
        [SerializeField] private ArmaAPie arma = ArmaAPie.Escopeta;

        [Tooltip("Balas que da cada compra (balas).")]
        [Min(1)] [SerializeField] private int balasPorPaquete = 10;

        [Tooltip("Balas de reserva que se reciben una vez al tener el arma por primera vez (balas).")]
        [Min(0)] [SerializeField] private int balasIniciales = 10;

        public ArmaAPie Arma => arma;
        public int BalasPorPaquete => balasPorPaquete;
        public int BalasIniciales => balasIniciales;

        /// <summary>Crea unas balas en memoria (para pruebas y herramientas).</summary>
        public static DatosMunicionArma Crear(string id, string nombre, ArmaAPie arma, int balasPorPaquete, int balasIniciales, int maximo,
            MechaCyberpunk.Core.DatosOferta oferta = null)
        {
            var o = CreateInstance<DatosMunicionArma>();
            o.Configurar(id, nombre, TipoObjeto.Municion, 0, maximo);
            o.arma = arma;
            o.balasPorPaquete = Mathf.Max(1, balasPorPaquete);
            o.balasIniciales = Mathf.Max(0, balasIniciales);
            o.FijarOferta(oferta);
            return o;
        }
    }
}

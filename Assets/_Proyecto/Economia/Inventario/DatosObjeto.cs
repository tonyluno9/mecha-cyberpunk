using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Economia
{
    /// <summary>Un objeto que el jugador puede llevar: pieza, chatarra o kit (T-142).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de objeto", fileName = "DatosObjeto_Nuevo")]
    public class DatosObjeto : ScriptableObject
    {
        [Tooltip("Identificador único (texto). Es la clave del guardado: no lo cambies después.")]
        [SerializeField] private string id = "objeto";

        [Tooltip("Nombre que ve el jugador (texto).")]
        [SerializeField] private string nombre = "Objeto";

        [Tooltip("Clase de objeto.")]
        [SerializeField] private TipoObjeto tipo;

        [Tooltip("Cuánto paga el mercado negro por uno ($, entero). 0 = no se vende.")]
        [Min(0)] [SerializeField] private int valorVenta;

        [Tooltip("Cuántos puede llevar el jugador (entero, al menos 1).")]
        [Min(1)] [SerializeField] private int maximo = 1;

        [Tooltip("Opcional: la oferta de tienda que entrega este objeto al comprarse.")]
        [SerializeField] private DatosOferta oferta;

        public string Id => id;
        public string Nombre => nombre;
        public TipoObjeto Tipo => tipo;
        public int ValorVenta => valorVenta;
        public int Maximo => maximo;
        public DatosOferta Oferta => oferta;

        /// <summary>Crea un objeto en memoria (para pruebas y herramientas).</summary>
        public static DatosObjeto Crear(string id, string nombre, TipoObjeto tipo, int valorVenta, int maximo, DatosOferta oferta = null)
        {
            var o = CreateInstance<DatosObjeto>();
            o.id = id;
            o.nombre = nombre;
            o.tipo = tipo;
            o.valorVenta = Mathf.Max(0, valorVenta);
            o.maximo = Mathf.Max(1, maximo);
            o.oferta = oferta;
            return o;
        }
    }
}

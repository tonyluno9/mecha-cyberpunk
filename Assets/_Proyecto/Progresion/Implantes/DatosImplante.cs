using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Un implante como datos (T-191, D-tony-165): espacio, nivel, precio, liga mínima y efectos.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de implante", fileName = "DatosImplante_Nuevo")]
    public class DatosImplante : ScriptableObject
    {
        [Tooltip("Identificador estable: implante_<espacio>_<nivel> (texto). Es lo que se guarda.")]
        [SerializeField] private string id = "implante_ojos_1";

        [Tooltip("Nombre para pantallas, en mayúsculas (texto).")]
        [SerializeField] private string nombre = "IMPLANTE";

        [Tooltip("Espacio del cuerpo donde se instala.")]
        [SerializeField] private EspacioImplante espacio = EspacioImplante.Ojos;

        [Tooltip("Nivel del implante (1 o 2).")]
        [Range(1, 2)] [SerializeField] private int nivel = 1;

        [Tooltip("Precio completo en la clínica ($, entero).")]
        [Min(0)] [SerializeField] private int precio = 1000;

        [Tooltip("Liga mínima que lo abre (de 1 a 4).")]
        [Range(1, 4)] [SerializeField] private int ligaMinima = 1;

        [Tooltip("Frase corta para la clínica (texto).")]
        [SerializeField] private string descripcion = "";

        [Tooltip("Lo que suma este implante. Neutro = no cambia nada.")]
        [SerializeField] private EfectosImplantes efectos = EfectosImplantes.Ninguno;

        public string Id => id;
        public string Nombre => nombre;
        public EspacioImplante Espacio => espacio;
        public int Nivel => nivel;
        public int Precio => precio;
        public int LigaMinima => ligaMinima;
        public string Descripcion => descripcion;
        public EfectosImplantes Efectos => efectos;

        /// <summary>Para el creador de assets y las pruebas.</summary>
        public static DatosImplante Crear(string id, string nombre, EspacioImplante espacio, int nivel, int precio, int ligaMinima,
            string descripcion, EfectosImplantes efectos)
        {
            var d = CreateInstance<DatosImplante>();
            d.id = id;
            d.nombre = nombre;
            d.espacio = espacio;
            d.nivel = nivel;
            d.precio = precio;
            d.ligaMinima = ligaMinima;
            d.descripcion = descripcion;
            d.efectos = efectos;
            return d;
        }
    }
}

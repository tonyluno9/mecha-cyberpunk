using UnityEngine;

namespace MechaCyberpunk.Progresion.Apoyo
{
    /// <summary>Un personaje de apoyo (T-417): cómo se llama, qué hace y qué dice.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de personaje de apoyo", fileName = "DatosPersonajeApoyo_Nuevo")]
    public class DatosPersonajeApoyo : ScriptableObject
    {
        [SerializeField] private string id = "inventor";
        [Tooltip("Lo que se lee primero en el letrero: EL INVENTOR.")]
        [SerializeField] private string titulo = "EL INVENTOR";
        [Tooltip("El nombre, con el color del personaje: TINO.")]
        [SerializeField] private string nombre = "TINO";
        [SerializeField] private string funcion = "MEJORA TUS ARMAS";
        [SerializeField] private Color color = Color.white;
        [SerializeField] private ServicioApoyo servicio = ServicioApoyo.Platica;
        [Min(0)] [SerializeField] private int costo;
        [SerializeField] private string[] frases = new string[0];

        public string Id => id;
        public string Titulo => titulo;
        public string Nombre => nombre;
        public string Funcion => funcion;
        public Color Color => color;
        public ServicioApoyo Servicio => servicio;
        public int Costo => costo;
        public string[] Frases => frases;
    }
}

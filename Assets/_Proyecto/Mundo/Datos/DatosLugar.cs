using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Datos
{
    /// <summary>Datos de un local del barrio: qué lugar es, cómo se llama su letrero, su horario y si la demo lo cierra.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos lugar", fileName = "DatosLugar_Nuevo")]
    public class DatosLugar : ScriptableObject
    {
        [SerializeField] private IdLugar id = IdLugar.Ninguno;

        [Tooltip("Nombre del letrero. Es un nombre de trabajo: los definitivos están sin confirmar.")]
        [SerializeField] private string nombre = "Local";

        [SerializeField] private HorarioLugar horario = HorarioLugar.Siempre;

        [Tooltip("Si está marcado, el lugar está cerrado en la demo sin importar la hora.")]
        [SerializeField] private bool cerradoEnDemo;

        public IdLugar Id => id;
        public string Nombre => nombre;
        public HorarioLugar Horario => horario;
        public bool CerradoEnDemo => cerradoEnDemo;

        /// <summary>Llena los datos desde código de editor (para el creador de assets).</summary>
        public void Configurar(IdLugar nuevoId, string nuevoNombre, HorarioLugar nuevoHorario, bool nuevoCerradoEnDemo)
        {
            id = nuevoId;
            nombre = nuevoNombre;
            horario = nuevoHorario;
            cerradoEnDemo = nuevoCerradoEnDemo;
        }
    }
}

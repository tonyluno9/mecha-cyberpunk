using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Habilidades;
using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>
    /// T-1017: una prótesis de ataque que vende el Doc (brazo láser, hombrera de misiles). Es un objeto del inventario (tipo Protesis, máximo 1): tenerla
    /// enciende su habilidad en la barra (<see cref="HabilidadesProtesis"/>). Lo que hace en combate está en <see cref="ConfigHabilidadesProtesis"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de prótesis de ataque", fileName = "DatosProtesisAtaque_Nueva")]
    public class DatosProtesisAtaque : DatosObjeto
    {
        [Tooltip("La habilidad de la barra que da (Laser o Misiles).")]
        [SerializeField] private HabilidadProtesis habilidad = HabilidadProtesis.Laser;
        [Tooltip("Modelo que se ve en el cuerpo al tenerla (opcional).")]
        [SerializeField] private GameObject modelo;

        public HabilidadProtesis Habilidad => habilidad;
        public GameObject Modelo => modelo;
    }
}

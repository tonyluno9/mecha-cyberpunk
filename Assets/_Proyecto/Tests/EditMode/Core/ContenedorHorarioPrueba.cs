using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Tests.Core
{
    /// <summary>ScriptableObject de prueba con un campo HorarioLugar, para probar su serialización.</summary>
    public class ContenedorHorarioPrueba : ScriptableObject
    {
        [SerializeField] private HorarioLugar horario;

        public HorarioLugar Horario => horario;
    }
}

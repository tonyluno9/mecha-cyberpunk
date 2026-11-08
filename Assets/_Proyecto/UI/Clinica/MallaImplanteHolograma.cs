using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.UI.Clinica
{
    /// <summary>T-896: la malla horneada de un implante de PJ01 para el holograma de la clínica, por espacio y nivel.</summary>
    [Serializable]
    public struct MallaImplanteHolograma
    {
        public EspacioImplante Espacio;
        public int Nivel;
        public Mesh Malla;
    }
}

using System;
using System.Collections.Generic;

namespace MechaCyberpunk.IA.Adaptacion
{
    /// <summary>Envoltorio de las memorias de los rivales para guardarlas con JsonUtility (T-199).</summary>
    [Serializable]
    public class ListaMemoriasRival
    {
        public List<MemoriaRival> memorias = new List<MemoriaRival>();
    }
}

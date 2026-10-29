using System;

namespace MechaCyberpunk.IA.Adaptacion
{
    /// <summary>Lo que un rival recuerda del jugador (T-199, D-tony-194). Se guarda con JsonUtility; la llave es el nombre del asset DatosRival.</summary>
    [Serializable]
    public class MemoriaRival
    {
        /// <summary>Nombre del asset DatosRival.</summary>
        public string idRival;
        /// <summary>Evidencia de cada hábito, indexada por TipoAdaptacion.</summary>
        public int[] evidencias = new int[3];
        /// <summary>Peleas terminadas contra este rival.</summary>
        public int peleas;
        /// <summary>Derrotas del jugador seguidas contra este rival.</summary>
        public int derrotasSeguidas;
    }
}

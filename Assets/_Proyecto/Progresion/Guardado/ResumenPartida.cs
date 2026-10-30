using System;

namespace MechaCyberpunk.Progresion.Guardado
{
    /// <summary>
    /// Lo que la pantalla de ranuras muestra de una partida sin cargarla (T-412, D-tony-244): fecha local, zona donde estaba el jugador,
    /// dinero y texto del objetivo. Va dentro del archivo como campo opcional; los archivos de la versión 1 sin él se leen igual.
    /// </summary>
    [Serializable]
    public class ResumenPartida
    {
        /// <summary>Fecha local al guardar, "yyyy-MM-ddTHH:mm:ss".</summary>
        public string fecha;
        public string zona;
        public int dinero;
        public string objetivo;
    }
}

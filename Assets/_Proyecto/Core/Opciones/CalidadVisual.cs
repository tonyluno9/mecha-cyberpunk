using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// T-978: lo de la calidad gráfica que tienen que respetar otras áreas. Lo fija <c>ServicioOpciones</c> (Progresion) según la CALIDAD de Opciones;
    /// lo lee <c>ReflejoCharcos</c> (Mundo): sin reflejos no pinta la segunda cámara de los charcos. Sin servicio queda todo prendido (como antes).
    /// </summary>
    public static class CalidadVisual
    {
        /// <summary>True: los charcos reflejan la calle (una cámara más por cuadro).</summary>
        public static bool ReflejosCharcos { get; private set; } = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            ReflejosCharcos = true;
        }

        public static void FijarReflejosCharcos(bool prendidos)
        {
            ReflejosCharcos = prendidos;
        }
    }
}

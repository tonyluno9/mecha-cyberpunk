using UnityEngine;

namespace MechaCyberpunk.UI.Mapa
{
    /// <summary>
    /// El punto que marcas con un clic en el mapa grande (T-409); el minimapa lo dibuja con su distancia. Uno a la vez. Se borra al entrar a Play.
    /// </summary>
    public static class PuntoMarcadoMapa
    {
        public static bool Hay { get; private set; }

        /// <summary>Dónde está (m, mundo, altura 0).</summary>
        public static Vector3 Posicion { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Quitar()
        {
            Hay = false;
            Posicion = Vector3.zero;
        }

        public static void Marcar(Vector3 posicion)
        {
            Posicion = new Vector3(posicion.x, 0f, posicion.z);
            Hay = true;
        }
    }
}

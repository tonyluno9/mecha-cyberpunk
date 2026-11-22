using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// T-1631 (T17): avisos y ganchos del director de respuesta para que las áreas se hablen sin referenciarse.
    /// <see cref="NovaInterviene"/>: lo publica el director al llegar al máximo de estrellas; el HUD (UI) pone el texto "NOVA INTERVIENE".
    /// <see cref="CapsulaDron"/>: lo pone IA (RespuestaNova) y lo usa Trafico (GestorPersecucion) para que los drones de Nova lleguen en cápsula.
    /// </summary>
    public static class EventosRespuesta
    {
        /// <summary>Texto del aviso en pantalla.</summary>
        public const string TextoNovaInterviene = "NOVA INTERVIENE";

        /// <summary>Lanza una cápsula de caída con este dron dentro: (dron en escena, nombre del modelo/variante, punto en el piso, rotación, al terminar). True si la lanzó.</summary>
        public delegate bool LanzadorCapsulaDron(GameObject dron, string nombre, Vector3 punto, Quaternion rotacion, Action<GameObject> alTerminar);

        /// <summary>Avisa que llegó el máximo de estrellas y Nova toma el lugar de la policía.</summary>
        public static event Action NovaInterviene;

        /// <summary>Quien sepa lanzar cápsulas de dron lo pone aquí (null si no hay: el dron llega volando como antes).</summary>
        public static LanzadorCapsulaDron CapsulaDron;

        public static void PublicarNovaInterviene() => NovaInterviene?.Invoke();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            NovaInterviene = null;
            CapsulaDron = null;
        }
    }
}

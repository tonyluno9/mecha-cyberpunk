using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Recorrido;
using UnityEngine;

namespace MechaCyberpunk.VR.Cinematicas
{
    /// <summary>
    /// V12: en VR la cinemática pendiente (que con teclado se salta con ENTER) se salta con B o con el gatillo derecho.
    /// Vive en un objeto DontDestroyOnLoad y solo actúa con <see cref="ModoVR.Activo"/>.
    /// </summary>
    public class SaltarCinematicaVR : MonoBehaviour
    {
        /// <summary>Veces que se saltó con el control (para las pruebas).</summary>
        public static int Saltadas { get; private set; }

        private static SaltarCinematicaVR instancia;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Crear()
        {
            if (instancia != null)
            {
                return;
            }
            var objeto = new GameObject("SaltarCinematicaVR");
            DontDestroyOnLoad(objeto);
            instancia = objeto.AddComponent<SaltarCinematicaVR>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            instancia = null;
            Saltadas = 0;
        }

        private void Update()
        {
            if (!ModoVR.Activo || !CinematicaPendiente.EnCurso)
            {
                return;
            }
            if ((EntradaVR.Pulso(EntradaVR.BotonB) || EntradaVR.Pulso(EntradaVR.GatilloDerecho)) && CinematicaPendiente.SaltarActual())
            {
                Saltadas++;
            }
        }
    }
}

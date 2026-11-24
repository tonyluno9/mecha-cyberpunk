using MechaCyberpunk.Personaje.Combate;
using UnityEngine;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR2: cuando el rig VR queda armado en el jugador a pie, le agrega la pistola en la mano derecha (<see cref="PistolaVR"/>), las armas especiales
    /// (<see cref="ArmasEspecialesVR"/>, VR5), el cambio de arma con el stick (<see cref="CambioArmaStickVR"/>, VR5) y los golpes
    /// con las manos (<see cref="GolpesManosVR"/>). Sin VR no hace nada (el adaptador no se arma).
    /// </summary>
    public static class ArranqueCombateVR
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            AdaptadorVR.Listo -= AlArmar;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Suscribir()
        {
            AdaptadorVR.Listo -= AlArmar;
            AdaptadorVR.Listo += AlArmar;
            if (AdaptadorVR.Actual != null && AdaptadorVR.Actual.Armado)
            {
                AlArmar(AdaptadorVR.Actual);
            }
        }

        /// <summary>Agrega el combate VR al jugador del adaptador (no duplica). Público para las pruebas.</summary>
        public static void AlArmar(AdaptadorVR adaptador)
        {
            if (adaptador == null)
            {
                return;
            }
            GameObject jugador = adaptador.gameObject;
            if (jugador.GetComponent<PistolaAPie>() != null && jugador.GetComponent<ArmasAPie>() != null && jugador.GetComponent<PistolaVR>() == null)
            {
                jugador.AddComponent<PistolaVR>();
            }
            // VR5: lanzagranadas y plasma con el gatillo desde el cañón en la mano, la granada con el grip, y el cambio de arma con el stick.
            if (jugador.GetComponent<PistolaVR>() != null && jugador.GetComponent<ArmasEspecialesAPie>() != null && jugador.GetComponent<ArmasEspecialesVR>() == null)
            {
                jugador.AddComponent<ArmasEspecialesVR>();
            }
            if (jugador.GetComponent<ArmasAPie>() != null && jugador.GetComponent<CambioArmaStickVR>() == null)
            {
                jugador.AddComponent<CambioArmaStickVR>();
            }
            if (jugador.GetComponent<GolpeAPie>() != null && jugador.GetComponent<GolpesManosVR>() == null)
            {
                jugador.AddComponent<GolpesManosVR>();
            }
        }
    }
}

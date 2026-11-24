using MechaCyberpunk.Personaje.Combate;
using UnityEngine;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR2: cuando el rig VR queda armado en el jugador a pie, le agrega la pistola en la mano derecha (<see cref="PistolaVR"/>), las armas especiales
    /// (<see cref="ArmasEspecialesVR"/>, VR5), el recentrar con el stick derecho (<see cref="CambioArmaStickVR"/>; desde VR19 ya no cambia de arma) y los golpes
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
            // VR5: lanzagranadas y plasma con el gatillo desde el cañón en la mano, la granada con el grip, y recentrar con el stick derecho (VR19).
            if (jugador.GetComponent<PistolaVR>() != null && jugador.GetComponent<ArmasEspecialesAPie>() != null && jugador.GetComponent<ArmasEspecialesVR>() == null)
            {
                jugador.AddComponent<ArmasEspecialesVR>();
            }
            if (jugador.GetComponent<ArmasAPie>() != null && jugador.GetComponent<CambioArmaStickVR>() == null)
            {
                jugador.AddComponent<CambioArmaStickVR>();
            }
            // VR27: sacar y guardar armas con gestos, y la recarga a mano (A suelta el cargador).
            if (jugador.GetComponent<ArmasAPie>() != null && jugador.GetComponent<GestosArmaVR>() == null)
            {
                jugador.AddComponent<GestosArmaVR>();
            }
            if (jugador.GetComponent<PistolaVR>() != null && jugador.GetComponent<RecargaManualVR>() == null)
            {
                jugador.AddComponent<RecargaManualVR>();
            }
            // VR52: el aviso "A: RECARGAR" con el cargador vacío (en VR no se recarga solo).
            if (jugador.GetComponent<PistolaVR>() != null && jugador.GetComponent<AvisoRecargaVR>() == null)
            {
                jugador.AddComponent<AvisoRecargaVR>();
            }
            // VR46: la rueda de armas se elige con el grip izquierdo y la mano. VR51: la mira con el gatillo izquierdo (después del stick derecho).
            if (jugador.GetComponent<RuedaArmasVR>() == null)
            {
                jugador.AddComponent<RuedaArmasVR>();
            }
            if (jugador.GetComponent<PistolaVR>() != null && jugador.GetComponent<MiraArmaVR>() == null)
            {
                jugador.AddComponent<MiraArmaVR>();
            }
            if (jugador.GetComponent<GolpeAPie>() != null && jugador.GetComponent<GolpesManosVR>() == null)
            {
                jugador.AddComponent<GolpesManosVR>();
            }
            // VR35: los puños de PJ01 en las manos, en lugar de los modelos de los controles.
            if (jugador.GetComponent<Manos.PunosVisiblesVR>() == null)
            {
                jugador.AddComponent<Manos.PunosVisiblesVR>();
            }
        }
    }
}

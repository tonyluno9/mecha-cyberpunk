using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Titulo;
using UnityEngine;

namespace MechaCyberpunk.VR.Guia
{
    /// <summary>
    /// VR20: la primera vez que se juega en VR, la guía de controles (<see cref="GuiaControlesVR"/>) sale sola al empezar la partida: cuando el
    /// jugador ya tiene el rig armado y control, sin carga, título, pausa, menús ni avisos, tras <see cref="SegundosEspera"/> s de calma.
    /// Se guarda en PlayerPrefs (<see cref="GuiaControlesVR.ClaveVista"/>) al mostrarla y ya no vuelve a salir sola (sí desde Pausa → CONTROLES).
    /// Con el simulador de XR no sale sola salvo <see cref="GuiaControlesVR.MostrarEnSimulador"/> (para no tapar las pruebas de VR).
    /// Se agrega solo al jugador cuando el rig VR está armado y se quita al terminar.
    /// </summary>
    public class ArranqueGuiaVR : MonoBehaviour
    {
        /// <summary>Segundos seguidos de calma antes de mostrarla (s).</summary>
        public const float SegundosEspera = 1f;

        private AdaptadorVR adaptador;
        private float calma;

        /// <summary>True si la guía ya salió sola alguna vez en esta máquina.</summary>
        public static bool YaVista => PlayerPrefs.GetInt(GuiaControlesVR.ClaveVista, 0) == 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Suscribir()
        {
            AdaptadorVR.Listo -= AlListo;
            AdaptadorVR.Listo += AlListo;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RevisarActual()
        {
            if (AdaptadorVR.Actual != null && AdaptadorVR.Actual.Armado)
            {
                AlListo(AdaptadorVR.Actual);
            }
        }

        private static void AlListo(AdaptadorVR rig)
        {
            if (rig != null && !YaVista && rig.GetComponent<ArranqueGuiaVR>() == null)
            {
                rig.gameObject.AddComponent<ArranqueGuiaVR>();
            }
        }

        private void Awake()
        {
            adaptador = GetComponent<AdaptadorVR>();
        }

        private void Update()
        {
            if (YaVista || !ModoVR.Activo)
            {
                Destroy(this);
                return;
            }
            if (!EnCalma())
            {
                calma = 0f;
                return;
            }
            calma += Time.unscaledDeltaTime;
            if (calma < SegundosEspera)
            {
                return;
            }
            if (GuiaControlesVR.Abrir() != null)
            {
                PlayerPrefs.SetInt(GuiaControlesVR.ClaveVista, 1);
                PlayerPrefs.Save();
                Debug.Log("[VR] Guía de controles: primera vez en VR, se muestra sola.");
            }
            Destroy(this);
        }

        private bool EnCalma()
        {
            if (ModoVR.Simulado && !GuiaControlesVR.MostrarEnSimulador)
            {
                return false;
            }
            return adaptador != null && adaptador.Armado && adaptador.Controlador != null && adaptador.Controlador.ControlActivo
                && !FlujoPartida.EnTitulo && !PantallaCargando.Visible && !Pausa.Activa && PilaPantallas.Vacia
                && MenuCyberfix.Abiertos == 0 && !VentanaAviso.Abierta && !GuiaControlesVR.Abierta
                && !Menus.MenusVR.MenuAbierto;
        }
    }
}

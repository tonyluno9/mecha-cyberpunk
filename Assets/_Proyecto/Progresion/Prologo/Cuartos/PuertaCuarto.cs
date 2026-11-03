using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.UI.Historia;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Cuartos
{
    /// <summary>
    /// T-550: puerta de un cuarto de interior aparte. Es un trigger. La de afuera (<see cref="EsSalida"/> false) va justo detrás de la hoja de la puerta de
    /// la calle: si el lugar está abierto (la hoja solo abre así), al cruzarla hay un fundido a negro y el jugador aparece dentro del cuarto
    /// junto a su puerta interior. La interior (<see cref="EsSalida"/> true) hace el fundido y lo deja afuera, frente a la puerta de la calle, mirando a la calle.
    /// Dentro del cuarto, <c>LugarMundo.JugadorDentro</c> lo marca la <c>ZonaInterior</c> del cuarto, que cubre todo el cuarto.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class PuertaCuarto : MonoBehaviour
    {
        private const float SegundosEntreCruces = 0.75f;
        private const int CuadrosDeFisica = 2;

        [SerializeField] private CuartoInterior cuarto;
        [Tooltip("False = puerta de la calle (entra al cuarto). True = puerta interior (sale a la calle).")]
        [SerializeField] private bool esSalida;
        [Tooltip("PantallasHistoria.prefab: se instancia una vez si la escena no trae pantalla de fundido.")]
        [SerializeField] private GameObject prefabPantallas;
        [Tooltip("Tiempo que se queda en negro con el jugador ya movido (s).")]
        [Min(0f)] [SerializeField] private float segundosEnNegro = 0.15f;

        private static float proximoCruce;
        private static bool cruzando;
        private ControladorAPie controlCongelado;

        public bool EsSalida => esSalida;
        public CuartoInterior Cuarto => cuarto;

        /// <summary>True mientras hay un cruce (fundido incluido) en curso en cualquier puerta de cuarto.</summary>
        public static bool HayCruce => cruzando;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            cruzando = false;
            proximoCruce = 0f;
        }

        private void Awake()
        {
            if (cuarto == null)
            {
                Debug.LogError("[Progresion] PuertaCuarto en '" + name + "' necesita su CuartoInterior.", this);
                enabled = false;
                return;
            }
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider otro)
        {
            if (cruzando || Time.unscaledTime < proximoCruce)
            {
                return;
            }
            IJugador jugador = otro.GetComponentInParent<IJugador>();
            if (jugador == null || jugador != Registro<IJugador>.Actual || jugador.Modo != ModoJugador.APie)
            {
                return;
            }
            if (!esSalida && !LugarAbierto())
            {
                return; // cerrado: la hoja tampoco deja pasar; esto es el seguro por si alguien llega sin pasar por ella
            }
            StartCoroutine(Cruzar(jugador, esSalida ? cuarto.PoseExterior : cuarto.PoseInterior));
        }

        private void OnDisable()
        {
            if (cruzando && controlCongelado != null)
            {
                controlCongelado.ActivarControl(true);
                controlCongelado = null;
                cruzando = false;
            }
        }

        private bool LugarAbierto()
        {
            foreach (ILugar lugar in Registro<ILugar>.Todos)
            {
                if (lugar.Id == cuarto.Lugar)
                {
                    return lugar.EstaAbierto || lugar.JugadorDentro;
                }
            }
            return false;
        }

        private IEnumerator Cruzar(IJugador jugador, Pose destino)
        {
            cruzando = true;
            JugadorAPie aPie = jugador as JugadorAPie;
            ControladorAPie controlador = aPie != null ? aPie.GetComponent<ControladorAPie>() : null;
            if (controlador != null && controlador.ControlActivo)
            {
                controlador.ActivarControl(false);
                controlCongelado = controlador;
            }
            PantallaFundido fundido = ObtenerFundido();
            if (fundido != null)
            {
                float salida = fundido.Config != null ? fundido.Config.SegundosSalida : 0f;
                yield return fundido.Fundir(1f, salida);
            }
            jugador.Colocar(destino);
            for (int i = 0; i < CuadrosDeFisica; i++)
            {
                yield return new WaitForFixedUpdate();
            }
            yield return new WaitForSecondsRealtime(segundosEnNegro);
            if (fundido != null)
            {
                float entrada = fundido.Config != null ? fundido.Config.SegundosEntrada : 0f;
                yield return fundido.Fundir(0f, entrada);
            }
            if (controlCongelado != null)
            {
                controlCongelado.ActivarControl(true);
                controlCongelado = null;
            }
            proximoCruce = Time.unscaledTime + SegundosEntreCruces;
            cruzando = false;
        }

        private PantallaFundido ObtenerFundido()
        {
            if (PantallaFundido.Actual != null)
            {
                return PantallaFundido.Actual;
            }
            if (prefabPantallas == null)
            {
                return null;
            }
            GameObject nuevas = Instantiate(prefabPantallas);
            nuevas.name = prefabPantallas.name;
            return nuevas.GetComponentInChildren<PantallaFundido>(true);
        }
    }
}

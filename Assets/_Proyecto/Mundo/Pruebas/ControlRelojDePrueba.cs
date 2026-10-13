using MechaCyberpunk.Mundo.Reloj;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Mundo.Pruebas
{
    /// <summary>
    /// Herramienta de prueba de Alan para mover el reloj del mundo: N salta a las 22:00, M a las 12:00 y T alterna la
    /// velocidad normal con la velocidad rápida. Escribe en la consola la hora nueva.
    /// </summary>
    public class ControlRelojDePrueba : MonoBehaviour
    {
        private const string FormatoHora = "00";

        [SerializeField] private RelojMundo reloj;

        [Tooltip("Asset ControlesCamaraLibre con las acciones SaltarANoche, SaltarAMediodia y AlternarVelocidad.")]
        [SerializeField] private InputActionAsset controles;

        [Tooltip("Hora a la que salta la acción de noche (h).")]
        [SerializeField, Range(0f, 23.99f)] private float horaNoche = 22f;

        [Tooltip("Hora a la que salta la acción de mediodía (h).")]
        [SerializeField, Range(0f, 23.99f)] private float horaMediodia = 12f;

        [Tooltip("Multiplicador de velocidad al alternar con T.")]
        [SerializeField, Min(1f)] private float escalaRapida = 30f;

        private InputAction saltarANoche;
        private InputAction saltarAMediodia;
        private InputAction alternarVelocidad;

        private void Awake()
        {
            if (reloj == null || controles == null)
            {
                Debug.LogError("[Mundo] ControlRelojDePrueba en '" + name + "' necesita reloj y controles.", this);
                enabled = false;
                return;
            }
            saltarANoche = controles.FindAction("SaltarANoche", true);
            saltarAMediodia = controles.FindAction("SaltarAMediodia", true);
            alternarVelocidad = controles.FindAction("AlternarVelocidad", true);
        }

        private void OnEnable()
        {
            if (saltarANoche == null)
            {
                return;
            }
            saltarANoche.Enable();
            saltarAMediodia.Enable();
            alternarVelocidad.Enable();
        }

        private void OnDisable()
        {
            if (saltarANoche == null)
            {
                return;
            }
            saltarANoche.Disable();
            saltarAMediodia.Disable();
            alternarVelocidad.Disable();
        }

        private void Update()
        {
            if (saltarANoche.WasPressedThisFrame())
            {
                reloj.AvanzarHasta(horaNoche);
                Informar();
            }
            if (saltarAMediodia.WasPressedThisFrame())
            {
                reloj.AvanzarHasta(horaMediodia);
                Informar();
            }
            if (alternarVelocidad.WasPressedThisFrame())
            {
                reloj.EscalaTiempo = reloj.EscalaTiempo > 1f ? 1f : escalaRapida;
                Debug.Log("[Mundo] Reloj de prueba: velocidad x" + reloj.EscalaTiempo);
            }
        }

        private void Informar()
        {
            int horas = Mathf.FloorToInt(reloj.Hora);
            int minutos = Mathf.FloorToInt((reloj.Hora - horas) * 60f);
            Debug.Log("[Mundo] Reloj de prueba: " + horas.ToString(FormatoHora) + ":" + minutos.ToString(FormatoHora) + " día " + reloj.Dia);
        }
    }
}

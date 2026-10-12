using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Combate.Pruebas
{
    /// <summary>Herramienta de prueba: con una tecla aplica un daño fijo a un objetivo con IRecibeDanio. Solo va en Pruebas_Tony.</summary>
    public class DaniadorDePrueba : MonoBehaviour
    {
        [Tooltip("Objeto con un IRecibeDanio (el mecha del jugador).")]
        [SerializeField] private GameObject objetivo;
        [Tooltip("Daño de cada aplicación (puntos).")]
        [Min(0f)] [SerializeField] private float danio = 10f;
        [Tooltip("Acción que dispara el daño (tecla H).")]
        [SerializeField] private InputAction aplicar;

        private IRecibeDanio receptor;

        private void Awake()
        {
            if (objetivo != null)
            {
                receptor = objetivo.GetComponent<IRecibeDanio>();
            }
        }

        private void OnEnable()
        {
            aplicar.performed += AlPulsar;
            aplicar.Enable();
        }

        private void OnDisable()
        {
            aplicar.performed -= AlPulsar;
            aplicar.Disable();
        }

        /// <summary>Para pruebas: configura sin pasar por el inspector. Llámalo antes de activar el objeto.</summary>
        public void Configurar(GameObject nuevoObjetivo, float nuevoDanio, InputAction accion)
        {
            objetivo = nuevoObjetivo;
            danio = nuevoDanio;
            aplicar = accion;
        }

        private void AlPulsar(InputAction.CallbackContext contexto)
        {
            if (receptor == null && objetivo != null)
            {
                receptor = objetivo.GetComponent<IRecibeDanio>();
            }
            if (receptor == null)
            {
                Debug.LogError("[Combate] DaniadorDePrueba sin objetivo con IRecibeDanio", this);
                return;
            }
            receptor.RecibirDanio(new InfoDanio(danio, TipoDanio.Golpe, objetivo.transform.position,
                objetivo.transform.forward, null));
        }
    }
}

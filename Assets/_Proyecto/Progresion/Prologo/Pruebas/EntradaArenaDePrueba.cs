using MechaCyberpunk.Pelea.Flujo;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Progresion.Prologo.Pruebas
{
    /// <summary>Herramienta de prueba: con una tecla (T) entra a la pelea tutorial con el mecha de la historia. Solo en escenas de pruebas.</summary>
    public class EntradaArenaDePrueba : MonoBehaviour
    {
        [Tooltip("Acción que entra a la arena (tecla T).")]
        [SerializeField] private InputAction entrar;
        [SerializeField] private EntradaArena entrada;
        [Tooltip("Flujo de la pelea tutorial.")]
        [SerializeField] private DirectorPelea director;

        private void Awake()
        {
            if (entrada == null || director == null)
            {
                Debug.LogError("[Progresion] EntradaArenaDePrueba necesita EntradaArena y DirectorPelea en " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            entrar.Enable();
        }

        private void OnDisable()
        {
            entrar.Disable();
        }

        private void Update()
        {
            if (entrar.WasPressedThisFrame())
            {
                entrada.Empezar(director);
            }
        }
    }
}

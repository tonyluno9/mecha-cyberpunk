using MechaCyberpunk.Pelea.Flujo;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Pelea.Pruebas
{
    /// <summary>Herramienta de prueba: con una tecla (P) empieza la pelea del director. Solo va en Pruebas_Tony.</summary>
    public class IniciadorPeleaDePrueba : MonoBehaviour
    {
        [Tooltip("Acción que empieza la pelea (tecla P).")]
        [SerializeField] private InputAction iniciar;
        [Tooltip("Director de la pelea que se inicia.")]
        [SerializeField] private DirectorPelea director;

        private void Awake()
        {
            if (director == null)
            {
                Debug.LogError("[Pelea] IniciadorPeleaDePrueba necesita un DirectorPelea en " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            iniciar.Enable();
        }

        private void OnDisable()
        {
            iniciar.Disable();
        }

        private void Update()
        {
            if (iniciar.WasPressedThisFrame())
            {
                director.EmpezarPelea();
            }
        }
    }
}

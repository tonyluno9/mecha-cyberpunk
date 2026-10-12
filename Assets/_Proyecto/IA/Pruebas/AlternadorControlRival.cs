using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.IA.Pruebas
{
    /// <summary>
    /// Herramienta de prueba: con una tecla (C) activa o apaga el control del rival para ver si camina y ataca.
    /// Solo va en Pruebas_Tony.
    /// </summary>
    public class AlternadorControlRival : MonoBehaviour
    {
        [Tooltip("Acción que alterna el control (tecla C).")]
        [SerializeField] private InputAction alternar;
        [Tooltip("Objeto del rival (con un IPeleador).")]
        [SerializeField] private GameObject rival;

        private IPeleador peleador;

        private void Awake()
        {
            if (rival != null)
            {
                peleador = rival.GetComponent<IPeleador>();
            }
            if (peleador == null)
            {
                Debug.LogError("[IA] AlternadorControlRival necesita un rival con IPeleador en " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            alternar.Enable();
        }

        private void OnDisable()
        {
            alternar.Disable();
        }

        private void Update()
        {
            if (alternar.WasPressedThisFrame())
            {
                peleador.ActivarControl(!peleador.ControlActivo);
                Debug.Log("[IA] Control del rival: " + (peleador.ControlActivo ? "activo" : "apagado"));
            }
        }
    }
}

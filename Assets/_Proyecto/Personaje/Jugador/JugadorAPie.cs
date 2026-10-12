using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// El jugador a pie en el mundo: implementa IJugador en la raíz (que tiene el CharacterController)
    /// y se registra en Registro&lt;IJugador&gt; mientras está habilitado.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class JugadorAPie : MonoBehaviour, IJugador
    {
        [Tooltip("Cámara hija del jugador.")]
        [SerializeField] private Camera camara;

        private CharacterController controlador;
        private ControladorAPie controladorAPie;
        private bool avisoDuplicadoDado;

        public Transform Raiz => transform;
        public ModoJugador Modo => ModoJugador.APie;
        public Camera Camara => camara;

        /// <summary>Se dispara al cambiar de modo. Todavía no se dispara: el cambio llega con el carro (T-058).</summary>
#pragma warning disable CS0067 // se dispara desde T-058
        public event Action<ModoJugador> ModoCambio;
#pragma warning restore CS0067

        private void Awake()
        {
            controlador = GetComponent<CharacterController>();
            controladorAPie = GetComponent<ControladorAPie>();
            if (camara == null)
            {
                Debug.LogError("[Personaje] Falta la cámara en JugadorAPie de " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (Registro<IJugador>.Cantidad > 0 && !avisoDuplicadoDado)
            {
                avisoDuplicadoDado = true;
                Debug.LogWarning("[Personaje] Ya hay un IJugador registrado; solo debe haber uno (" + name + ").", this);
            }
            Registro<IJugador>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IJugador>.Quitar(this);
        }

        public void Colocar(Pose pose)
        {
            controlador.enabled = false;
            transform.SetPositionAndRotation(pose.position, pose.rotation);
            controlador.enabled = true;
            if (controladorAPie != null)
            {
                controladorAPie.ReiniciarMovimiento();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Combate.Peleador
{
    /// <summary>El mecha como participante de la pelea: lado, nombre, vida y control activable.</summary>
    [RequireComponent(typeof(VidaMecha))]
    public class PeleadorMecha : MonoBehaviour, IPeleador
    {
        [SerializeField] private LadoPelea lado = LadoPelea.Jugador;
        [Tooltip("Componentes que se apagan cuando el mecha pierde el control (movimiento, golpe, bloqueo).")]
        [SerializeField] private List<Behaviour> controlados = new List<Behaviour>();

        private VidaMecha vida;
        private bool permisoPelea = true;
        private bool permisoPiloto = true;
        private bool controlActivo = true;

        public string Nombre => vida != null && vida.Datos != null ? vida.Datos.Nombre : name;
        public LadoPelea Lado => lado;
        public Transform Raiz => transform;
        public IVida Vida => vida;
        public bool ControlActivo => controlActivo;

        /// <summary>Segundo permiso de control: lo da quien monta el mecha (false = nadie lo maneja). Por defecto true.</summary>
        public bool PermisoPiloto => permisoPiloto;

        /// <summary>Avisa al final de <see cref="PrepararPelea"/>, cuando el mecha ya quedó de pie y sin control.</summary>
        public event Action PeleaPreparada;

        private void Awake()
        {
            vida = GetComponent<VidaMecha>();
        }

        private void OnEnable()
        {
            vida.KO += AlLlegarAKO;
        }

        private void OnDisable()
        {
            vida.KO -= AlLlegarAKO;
        }

        public void ActivarControl(bool activo)
        {
            permisoPelea = activo;
            AplicarControl();
        }

        /// <summary>Da o quita el permiso de piloto sin tocar el permiso de pelea del director.</summary>
        public void FijarPermisoPiloto(bool permiso)
        {
            permisoPiloto = permiso;
            AplicarControl();
        }

        private void AplicarControl()
        {
            if (vida == null)
            {
                vida = GetComponent<VidaMecha>();
            }
            // Un mecha en KO no recupera el control hasta que PrepararPelea le devuelva la vida.
            controlActivo = ReglaControlMecha.Calcular(permisoPelea, permisoPiloto, vida.EstaKO);
            foreach (Behaviour componente in controlados)
            {
                if (componente != null)
                {
                    componente.enabled = controlActivo;
                }
            }
        }

        public void PrepararPelea(Pose inicio)
        {
            vida.Restaurar();

            CharacterController controlador = GetComponent<CharacterController>();
            bool teniaControlador = controlador != null && controlador.enabled;
            if (teniaControlador)
            {
                controlador.enabled = false;
            }
            transform.SetPositionAndRotation(inicio.position, inicio.rotation);
            if (teniaControlador)
            {
                controlador.enabled = true;
            }

            ActivarControl(false);
            PeleaPreparada?.Invoke();
        }

        private void AlLlegarAKO() => ActivarControl(false);
    }
}

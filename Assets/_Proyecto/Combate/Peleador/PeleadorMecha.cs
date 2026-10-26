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
        private float finAturdimiento;
        private bool aturdido;

        private string nombreVisible;

        public string Nombre => !string.IsNullOrEmpty(nombreVisible) ? nombreVisible
            : vida != null && vida.Datos != null ? vida.Datos.Nombre : name;
        public LadoPelea Lado => lado;
        public Transform Raiz => transform;
        public IVida Vida => vida;
        public bool ControlActivo => controlActivo;
        /// <summary>Si está aturdido: sus <c>controlados</c> están apagados aunque <see cref="ControlActivo"/> siga en true.</summary>
        public bool EstaAturdido => aturdido;

        /// <summary>Fija el nombre para mostrar (el alias del rival). Vacío o null vuelve al nombre de su DatosMecha.</summary>
        public void FijarNombre(string nombre)
        {
            nombreVisible = nombre;
        }

        /// <summary>Segundo permiso de control: lo da quien monta el mecha (false = nadie lo maneja). Por defecto true.</summary>
        public bool PermisoPiloto => permisoPiloto;

        /// <summary>
        /// Si es true, <see cref="PrepararPelea"/> no restaura la vida: el mecha empieza con la que traía (daño persistente).
        /// Por defecto false. Lo activa quien guarda el daño (MechaPersistente).
        /// </summary>
        public bool ConservarVidaAlPreparar { get; set; }

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

        /// <summary>
        /// Apaga los controlados <paramref name="segundos"/> (golpe, guardia, movimiento, cerebro del rival) y luego los vuelve a
        /// encender si <see cref="ControlActivo"/> sigue en true. Sin control no hace nada. No se suma: queda el que termina más tarde.
        /// </summary>
        public void Aturdir(float segundos)
        {
            if (!controlActivo || segundos <= 0f)
            {
                return;
            }
            finAturdimiento = ReglaAturdimiento.NuevoFin(aturdido ? finAturdimiento : 0f, Time.time, segundos);
            aturdido = true;
            AplicarControl();
        }

        private void Update()
        {
            if (aturdido && Time.time >= finAturdimiento)
            {
                aturdido = false;
                AplicarControl();
            }
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
            if (!controlActivo)
            {
                aturdido = false;
            }
            bool encendido = controlActivo && !aturdido;
            foreach (Behaviour componente in controlados)
            {
                if (componente != null)
                {
                    componente.enabled = encendido;
                }
            }
        }

        public void PrepararPelea(Pose inicio)
        {
            if (!ConservarVidaAlPreparar)
            {
                vida.Restaurar();
            }

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

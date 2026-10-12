using System;
using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// Arma activa del jugador a pie: 1 puños, 2 pistola. Mantiene a <see cref="GolpeAPie"/> al día y muestra solo el visual
    /// del arma en la mano. No se cambia de arma mientras se prepara o se recupera un golpe. El HUD lee el arma, las balas
    /// y el cargador desde aquí.
    /// </summary>
    [RequireComponent(typeof(GolpeAPie))]
    [RequireComponent(typeof(PistolaAPie))]
    [RequireComponent(typeof(ControladorAPie))]
    [RequireComponent(typeof(VidaAPie))]
    public class ArmasAPie : MonoBehaviour
    {
        [SerializeField] private InputActionReference armaPunos;
        [SerializeField] private InputActionReference armaPistola;
        [Tooltip("Visual del puño (hijo de Manos). Se oculta con la pistola en la mano.")]
        [SerializeField] private GameObject visualPunos;
        [Tooltip("Visual de la pistola (hijo de Manos). Se oculta con los puños en la mano.")]
        [SerializeField] private GameObject visualPistola;

        private GolpeAPie golpe;
        private PistolaAPie pistola;
        private ControladorAPie controlador;
        private VidaAPie vida;
        private ArmaAPie armaActiva = ArmaAPie.Punos;

        public ArmaAPie ArmaActiva => armaActiva;
        public int Balas => pistola != null ? pistola.Balas : 0;
        public int TamanoCargador => pistola != null ? pistola.TamanoCargador : 0;
        public bool Recargando => pistola != null && pistola.Recargando;

        /// <summary>Se dispara al cambiar de arma y cuando cambian las balas o la recarga de la pistola.</summary>
        public event Action Cambio;

        private void Awake()
        {
            golpe = GetComponent<GolpeAPie>();
            pistola = GetComponent<PistolaAPie>();
            controlador = GetComponent<ControladorAPie>();
            vida = GetComponent<VidaAPie>();
            if (armaPunos == null || armaPistola == null)
            {
                Debug.LogError("[Personaje] Faltan referencias en ArmasAPie de " + name, this);
                enabled = false;
                return;
            }
            Aplicar();
        }

        private void OnEnable()
        {
            armaPunos.action.Enable();
            armaPistola.action.Enable();
            pistola.Cambio += AlCambiarPistola;
        }

        private void OnDisable()
        {
            armaPunos.action.Disable();
            armaPistola.action.Disable();
            pistola.Cambio -= AlCambiarPistola;
        }

        private void Update()
        {
            if (vida.EstaKO || !controlador.ControlActivo || golpe.Fase != FaseGolpe.Listo)
            {
                return;
            }
            if (armaPunos.action.WasPressedThisFrame())
            {
                Cambiar(ArmaAPie.Punos);
            }
            else if (armaPistola.action.WasPressedThisFrame())
            {
                Cambiar(ArmaAPie.Pistola);
            }
        }

        private void Cambiar(ArmaAPie arma)
        {
            if (arma == armaActiva)
            {
                return;
            }
            armaActiva = arma;
            Aplicar();
            Cambio?.Invoke();
        }

        private void Aplicar()
        {
            golpe.ArmaActiva = armaActiva;
            if (visualPunos != null)
            {
                visualPunos.SetActive(armaActiva == ArmaAPie.Punos);
            }
            if (visualPistola != null)
            {
                visualPistola.SetActive(armaActiva == ArmaAPie.Pistola);
            }
        }

        private void AlCambiarPistola() => Cambio?.Invoke();
    }
}

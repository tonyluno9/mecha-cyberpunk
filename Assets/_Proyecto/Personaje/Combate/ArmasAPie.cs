using System;
using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// Arma activa del jugador a pie: 1 puños, 2 pistola, 3 escopeta, 4 subfusil y la rueda del ratón (T-403). Un arma que no se tiene
    /// (<see cref="ArsenalJugador"/>) no se elige. Mantiene a <see cref="GolpeAPie"/> al día y muestra solo el visual del arma en la mano.
    /// No se cambia de arma mientras se prepara o se recupera un golpe. El HUD lee el arma, el nivel, las balas y el cargador desde aquí.
    /// </summary>
    [RequireComponent(typeof(GolpeAPie))]
    [RequireComponent(typeof(PistolaAPie))]
    [RequireComponent(typeof(ControladorAPie))]
    [RequireComponent(typeof(VidaAPie))]
    public class ArmasAPie : MonoBehaviour
    {
        [SerializeField] private InputActionReference armaPunos;
        [SerializeField] private InputActionReference armaPistola;
        [SerializeField] private InputActionReference armaEscopeta;
        [SerializeField] private InputActionReference armaSubfusil;
        [Tooltip("Rueda del ratón: arriba la siguiente arma, abajo la anterior.")]
        [SerializeField] private InputActionReference cambiarArma;
        [Tooltip("Visual del puño (hijo de Manos). Se oculta con un arma de fuego en la mano.")]
        [SerializeField] private GameObject visualPunos;
        [SerializeField] private GameObject visualPistola;
        [SerializeField] private GameObject visualEscopeta;
        [SerializeField] private GameObject visualSubfusil;

        private GolpeAPie golpe;
        private PistolaAPie pistola;
        private ControladorAPie controlador;
        private VidaAPie vida;
        private ArsenalJugador arsenal;
        private ArmaAPie armaActiva = ArmaAPie.Punos;

        public ArmaAPie ArmaActiva => armaActiva;
        /// <summary>Nivel del arma en la mano (0 a 3; 0 si no hay arsenal).</summary>
        public int Nivel
        {
            get
            {
                arsenal = arsenal != null ? arsenal : Registro<ArsenalJugador>.Actual;
                return arsenal != null ? Mathf.Max(0, arsenal.Nivel(armaActiva)) : 0;
            }
        }
        public int Balas => pistola != null ? pistola.Balas : 0;
        public int TamanoCargador => pistola != null ? pistola.TamanoCargador : 0;
        public bool Recargando => pistola != null && pistola.Recargando;

        /// <summary>Se dispara al cambiar de arma y cuando cambian las balas o la recarga del arma de fuego.</summary>
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
            armaEscopeta?.action.Enable();
            armaSubfusil?.action.Enable();
            cambiarArma?.action.Enable();
            pistola.Cambio += AlCambiarPistola;
        }

        private void OnDisable()
        {
            armaPunos.action.Disable();
            armaPistola.action.Disable();
            armaEscopeta?.action.Disable();
            armaSubfusil?.action.Disable();
            cambiarArma?.action.Disable();
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
            else if (armaEscopeta != null && armaEscopeta.action.WasPressedThisFrame())
            {
                Cambiar(ArmaAPie.Escopeta);
            }
            else if (armaSubfusil != null && armaSubfusil.action.WasPressedThisFrame())
            {
                Cambiar(ArmaAPie.Subfusil);
            }
            else if (cambiarArma != null && cambiarArma.action.WasPerformedThisFrame())
            {
                float rueda = cambiarArma.action.ReadValue<float>();
                if (rueda != 0f)
                {
                    Rotar(rueda > 0f ? 1 : -1);
                }
            }
        }

        private bool Tiene(ArmaAPie arma)
        {
            arsenal = arsenal != null ? arsenal : Registro<ArsenalJugador>.Actual;
            if (arsenal == null)
            {
                return arma == ArmaAPie.Punos || arma == ArmaAPie.Pistola;
            }
            return arsenal.Tiene(arma);
        }

        // Pasa a la siguiente (o anterior) arma que se tiene, dando la vuelta.
        private void Rotar(int paso)
        {
            int total = Enum.GetValues(typeof(ArmaAPie)).Length;
            for (int i = 1; i < total; i++)
            {
                ArmaAPie candidata = (ArmaAPie)(((int)armaActiva + paso * i % total + total) % total);
                if (Tiene(candidata))
                {
                    Cambiar(candidata);
                    return;
                }
            }
        }

        private void Cambiar(ArmaAPie arma)
        {
            if (arma == armaActiva || !Tiene(arma))
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
            Mostrar(visualPunos, ArmaAPie.Punos);
            Mostrar(visualPistola, ArmaAPie.Pistola);
            Mostrar(visualEscopeta, ArmaAPie.Escopeta);
            Mostrar(visualSubfusil, ArmaAPie.Subfusil);
        }

        private void Mostrar(GameObject visual, ArmaAPie arma)
        {
            if (visual != null)
            {
                visual.SetActive(armaActiva == arma);
            }
        }

        private void AlCambiarPistola() => Cambio?.Invoke();
    }
}

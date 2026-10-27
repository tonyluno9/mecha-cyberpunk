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
    /// Golpe simple del jugador a pie. Con Atacar y los puños en la mano arranca un <see cref="CicloGolpe"/>; en el impacto
    /// lanza una esfera desde la cámara hacia donde mira (<see cref="BarridoGolpe"/>) y daña al primer
    /// <see cref="IRecibeDanio"/> que no sea el propio jugador. En KO no ataca. El atacante es null: la persona no es IPeleador.
    /// </summary>
    [RequireComponent(typeof(ControladorAPie))]
    [RequireComponent(typeof(VidaAPie))]
    public class GolpeAPie : MonoBehaviour
    {
        private const int MaxImpactosPorGolpe = 16;

        [SerializeField] private DatosPersonaje datos;
        [SerializeField] private InputActionReference atacar;
        [Tooltip("Cubo que hace de puño, hijo de Manos. Avanza durante la preparación y regresa en la recarga.")]
        [SerializeField] private Transform puno;
        [Tooltip("Cuánto avanza el puño hacia adelante en el impacto (m).")]
        [Min(0f)] [SerializeField] private float avancePuno = 0.6f;

        private ControladorAPie controlador;
        private VidaAPie vida;
        private CicloGolpe ciclo;
        private readonly SeguidorImplantes implantes = new SeguidorImplantes();
        private Vector3 posicionReposo;
        private readonly RaycastHit[] impactos = new RaycastHit[MaxImpactosPorGolpe];

        public DatosPersonaje Datos => datos;
        public FaseGolpe Fase => ciclo != null ? ciclo.Fase : FaseGolpe.Listo;
        public float Progreso => ciclo != null ? ciclo.Progreso : 0f;
        public Transform Puno => puno;
        /// <summary>Arma en la mano. T-053 la alterna entre puños y pistola; solo los puños golpean.</summary>
        public ArmaAPie ArmaActiva { get; set; } = ArmaAPie.Punos;
        /// <summary>Se dispara en cada impacto, haya dado a alguien o no.</summary>
        public event Action Impacto;

        private void Awake()
        {
            controlador = GetComponent<ControladorAPie>();
            vida = GetComponent<VidaAPie>();
            if (datos == null || atacar == null)
            {
                Debug.LogError("[Personaje] Faltan referencias en GolpeAPie de " + name, this);
                enabled = false;
                return;
            }
            ciclo = new CicloGolpe(datos.PreparacionGolpe, datos.RecargaGolpe);
            if (puno != null)
            {
                posicionReposo = puno.localPosition;
            }
        }

        private void OnEnable()
        {
            atacar.action.Enable();
            implantes.Activar();
        }

        private void OnDisable()
        {
            implantes.Desactivar();
            ciclo?.Reiniciar();
            MoverPuno();
        }

        private void Update()
        {
            if (vida.EstaKO || !controlador.ControlActivo)
            {
                ciclo.Reiniciar();
                MoverPuno();
                return;
            }
            if (atacar.action.WasPressedThisFrame() && ArmaActiva == ArmaAPie.Punos)
            {
                ciclo.IntentarGolpear();
            }
            if (ciclo.Avanzar(Time.deltaTime))
            {
                Impactar();
            }
            MoverPuno();
        }

        private void Impactar()
        {
            Transform camara = controlador.Camara;
            Vector3 origen = camara.position;
            Vector3 direccion = camara.forward;
            IRecibeDanio objetivo = BarridoGolpe.Buscar(transform, origen, direccion, datos.AlcanceGolpe,
                datos.RadioGolpe, impactos, out Vector3 punto);
            if (objetivo != null)
            {
                objetivo.RecibirDanio(new InfoDanio(EfectosAPie.DanioGolpe(datos, implantes.Efectos), TipoDanio.Golpe, punto, direccion, null));
            }
            Impacto?.Invoke();
        }

        // Preparando: el puño va de reposo al máximo avance; Recuperando: regresa.
        private void MoverPuno()
        {
            if (puno == null)
            {
                return;
            }
            float avance = 0f;
            if (ciclo != null)
            {
                if (ciclo.Fase == FaseGolpe.Preparando)
                {
                    avance = ciclo.Progreso;
                }
                else if (ciclo.Fase == FaseGolpe.Recuperando)
                {
                    avance = 1f - ciclo.Progreso;
                }
            }
            puno.localPosition = posicionReposo + Vector3.forward * (avancePuno * avance);
        }
    }
}

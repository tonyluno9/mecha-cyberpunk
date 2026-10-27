using System;
using MechaCyberpunk.Combate.Bloqueo;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Combate.Golpe
{
    /// <summary>
    /// El golpe del brazo derecho. Con Golpear arranca un <see cref="CicloGolpe"/>; en el impacto lanza una esfera hacia
    /// adelante hasta <c>AlcanceGolpe</c>, ignora al propio mecha y daña al primer <see cref="IRecibeDanio"/>.
    /// No golpea con la guardia levantada.
    /// </summary>
    [RequireComponent(typeof(VidaMecha))]
    public class GolpeMecha : MonoBehaviour
    {
        private const int MaxImpactosPorGolpe = 16;

        [SerializeField] private InputActionReference golpear;
        [Tooltip("Radio de la esfera que busca al rival (m). El alcance llega hasta el borde de la esfera.")]
        [Min(0.01f)] [SerializeField] private float radioBusqueda = 0.25f;

        private VidaMecha vida;
        private IPeleador peleador;
        private GuardiaMecha guardia;
        private CharacterController controlador;
        private CicloGolpe ciclo;
        private readonly RaycastHit[] impactos = new RaycastHit[MaxImpactosPorGolpe];

        public FaseGolpe Fase => ciclo != null ? ciclo.Fase : FaseGolpe.Listo;
        public float Progreso => ciclo != null ? ciclo.Progreso : 0f;
        /// <summary>Se dispara en cada impacto, haya dado a alguien o no.</summary>
        public event Action Impacto;

        private void Awake()
        {
            vida = GetComponent<VidaMecha>();
            peleador = GetComponent<IPeleador>();
            guardia = GetComponent<GuardiaMecha>();
            controlador = GetComponent<CharacterController>();
            if (golpear == null)
            {
                Debug.LogError("[Combate] Falta la acción Golpear en GolpeMecha de " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            golpear.action.Enable();
            if (vida.Datos == null)
            {
                Debug.LogError("[Combate] GolpeMecha sin DatosMecha en " + name, this);
                enabled = false;
                return;
            }
            vida.DatosCambiados += AlCambiarDatos;
            ciclo = new CicloGolpe(vida.Datos.PreparacionGolpe, vida.Datos.RecargaGolpe);
        }

        // Un golpe a medias se cancela y el ciclo se rehace con la nueva preparación y recarga (D-tony-119).
        private void AlCambiarDatos()
        {
            if (vida.Datos != null)
            {
                ciclo = new CicloGolpe(vida.Datos.PreparacionGolpe, vida.Datos.RecargaGolpe);
            }
        }

        private void OnDisable()
        {
            vida.DatosCambiados -= AlCambiarDatos;
            // La acción Golpear la comparte ControladorMecha, así que no se apaga aquí.
            ciclo?.Reiniciar();
        }

        private void Update()
        {
            bool guardiaLevantada = guardia != null && guardia.Levantada;
            if (golpear.action.WasPressedThisFrame() && !guardiaLevantada)
            {
                ciclo.IntentarGolpear();
            }
            if (ciclo.Avanzar(Time.deltaTime))
            {
                Impactar();
            }
        }

        private void Impactar()
        {
            DatosMecha datos = vida.Datos;
            Vector3 origen = controlador != null ? transform.TransformPoint(controlador.center) : transform.position;
            Vector3 direccion = transform.forward;
            IRecibeDanio objetivo = BarridoGolpe.Buscar(transform, origen, direccion, datos.AlcanceGolpe,
                radioBusqueda, impactos, out Vector3 punto);

            if (objetivo != null)
            {
                objetivo.RecibirDanio(new InfoDanio(datos.DanioGolpe, TipoDanio.Golpe, punto, direccion, peleador));
            }
            Impacto?.Invoke();
        }
    }
}

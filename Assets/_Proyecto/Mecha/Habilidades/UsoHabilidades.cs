using System;
using MechaCyberpunk.Combate.Bloqueo;
using MechaCyberpunk.Combate.Habilidades;
using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Mecha
{
    /// <summary>
    /// Usa las habilidades de las piezas del mecha en plena pelea (T-176, D-tony-135 y D-tony-138): tecla 1 la del brazo izquierdo y
    /// tecla 2 la del torso. Cada uso gasta una munición del inventario y arranca el enfriamiento (más corto con piezas tech).
    /// Sin <see cref="EquipoMecha"/> o con las piezas de serie no hay habilidad. El efecto lo ejecuta <see cref="LanzadorHabilidades"/>.
    /// </summary>
    [RequireComponent(typeof(VidaMecha))]
    [RequireComponent(typeof(PeleadorMecha))]
    [RequireComponent(typeof(LanzadorHabilidades))]
    public class UsoHabilidades : MonoBehaviour
    {
        private const int NumeroRanuras = 2;

        [SerializeField] private InputActionReference habilidad1;
        [SerializeField] private InputActionReference habilidad2;

        private VidaMecha vida;
        private PeleadorMecha peleador;
        private LanzadorHabilidades lanzador;
        private EquipoMecha equipo;
        private GuardiaMecha guardia;
        private bool enPelea;
        private readonly float[] finEnfriamiento = new float[NumeroRanuras];
        private readonly float[] totalEnfriamiento = new float[NumeroRanuras];
        private readonly string[] huella = new string[NumeroRanuras];

        public bool EnPelea => enPelea;

        /// <summary>Avisa al lanzar y cuando cambia la habilidad, el nivel, la munición, el motivo (incluye acabar el enfriamiento) o la pelea.</summary>
        public event Action Cambio;

        private void Awake()
        {
            vida = GetComponent<VidaMecha>();
            peleador = GetComponent<PeleadorMecha>();
            lanzador = GetComponent<LanzadorHabilidades>();
            equipo = GetComponent<EquipoMecha>();
            guardia = GetComponent<GuardiaMecha>();
            if (habilidad1 == null || habilidad2 == null)
            {
                Debug.LogError("[Mecha] Faltan referencias en UsoHabilidades de " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            EventosPelea.PeleaIniciada += AlIniciarPelea;
            EventosPelea.PeleaTerminada += AlTerminarPelea;
            habilidad1.action.performed += AlUsar1;
            habilidad2.action.performed += AlUsar2;
            habilidad1.action.Enable();
            habilidad2.action.Enable();
        }

        private void OnDisable()
        {
            EventosPelea.PeleaIniciada -= AlIniciarPelea;
            EventosPelea.PeleaTerminada -= AlTerminarPelea;
            habilidad1.action.performed -= AlUsar1;
            habilidad2.action.performed -= AlUsar2;
            habilidad1.action.Disable();
            habilidad2.action.Disable();
        }

        private void AlUsar1(InputAction.CallbackContext contexto) => IntentarUsar(RanuraPieza.BrazoIzquierdo);

        private void AlUsar2(InputAction.CallbackContext contexto) => IntentarUsar(RanuraPieza.Torso);

        private void AlIniciarPelea()
        {
            enPelea = true;
            for (int i = 0; i < NumeroRanuras; i++)
            {
                finEnfriamiento[i] = 0f;
                totalEnfriamiento[i] = 0f;
            }
            Cambio?.Invoke();
        }

        private void AlTerminarPelea(ResultadoPelea resultado)
        {
            enPelea = false;
            Cambio?.Invoke();
        }

        private static int Indice(RanuraPieza ranura)
        {
            switch (ranura)
            {
                case RanuraPieza.BrazoIzquierdo: return 0;
                case RanuraPieza.Torso: return 1;
                default: return -1;
            }
        }

        private DatosPieza Pieza(RanuraPieza ranura) => Indice(ranura) < 0 || equipo == null ? null : equipo.Puesta(ranura);

        /// <summary>La habilidad de esa ranura (solo BrazoIzquierdo y Torso), o null si no hay.</summary>
        public DatosHabilidad Habilidad(RanuraPieza ranura)
        {
            DatosPieza pieza = Pieza(ranura);
            return pieza != null ? pieza.Habilidad : null;
        }

        /// <summary>Nivel de la pieza puesta (0 si no hay habilidad).</summary>
        public int Nivel(RanuraPieza ranura)
        {
            DatosPieza pieza = Pieza(ranura);
            return pieza != null && pieza.Habilidad != null ? pieza.Nivel : 0;
        }

        /// <summary>Cuántas municiones de esa ranura hay en el inventario, o -1 si no hay inventario o la ranura no usa munición.</summary>
        public int Municion(RanuraPieza ranura)
        {
            DatosPieza pieza = Pieza(ranura);
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            if (pieza == null || pieza.Municion == null || inventario == null)
            {
                return -1;
            }
            return inventario.Cantidad(pieza.Municion);
        }

        public float EnfriamientoRestante(RanuraPieza ranura)
        {
            int i = Indice(ranura);
            return i < 0 ? 0f : Mathf.Max(0f, finEnfriamiento[i] - Time.time);
        }

        public float EnfriamientoTotal(RanuraPieza ranura)
        {
            int i = Indice(ranura);
            return i < 0 ? 0f : totalEnfriamiento[i];
        }

        /// <summary>Por qué se puede o no usar la habilidad de esa ranura ahora.</summary>
        public MotivoHabilidad Motivo(RanuraPieza ranura)
        {
            bool tiene = Habilidad(ranura) != null;
            bool control = peleador.PermisoPiloto && peleador.ControlActivo && !vida.EstaKO;
            bool guardiaArriba = ranura == RanuraPieza.BrazoIzquierdo && guardia != null && guardia.Levantada;
            return ReglaHabilidad.Decidir(tiene, enPelea, control, peleador.EstaAturdido, guardiaArriba,
                EnfriamientoRestante(ranura), Mathf.Max(0, Municion(ranura)));
        }

        /// <summary>Intenta usar la habilidad de la ranura. Devuelve true si se lanzó (y gastó una munición).</summary>
        public bool IntentarUsar(RanuraPieza ranura)
        {
            if (Motivo(ranura) != MotivoHabilidad.Puede)
            {
                return false;
            }
            DatosPieza pieza = Pieza(ranura);
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            if (!lanzador.Lanzar(pieza.Habilidad, pieza.Nivel, ranura == RanuraPieza.Torso))
            {
                return false;
            }
            inventario.Quitar(pieza.Municion, 1);
            int i = Indice(ranura);
            float total = ReglaHabilidad.Enfriamiento(pieza.Habilidad.Enfriamiento, ReglaEstadisticasPiezas.ReduccionEnfriamiento(equipo.Puestas));
            totalEnfriamiento[i] = total;
            finEnfriamiento[i] = Time.time + total;
            Cambio?.Invoke();
            return true;
        }

        private void Update()
        {
            if (equipo == null)
            {
                return;
            }
            // Una huella por ranura: si cambia habilidad, nivel, munición o motivo, avisa una sola vez (no en cada cuadro).
            bool cambio = false;
            for (int i = 0; i < NumeroRanuras; i++)
            {
                RanuraPieza ranura = i == 0 ? RanuraPieza.BrazoIzquierdo : RanuraPieza.Torso;
                DatosHabilidad habilidad = Habilidad(ranura);
                string nueva = (habilidad != null ? habilidad.Id : "-") + "|" + Nivel(ranura) + "|" + Municion(ranura) + "|" + Motivo(ranura);
                if (nueva != huella[i])
                {
                    huella[i] = nueva;
                    cambio = true;
                }
            }
            if (cambio)
            {
                Cambio?.Invoke();
            }
        }
    }
}

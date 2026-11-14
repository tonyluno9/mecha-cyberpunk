using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Personaje.Habilidades
{
    /// <summary>
    /// Habilidades de prótesis del jugador a pie (T-992): [1] PIERNAS, [2] BRAZOS, [3] CABEZA. Cada una necesita el implante de su espacio
    /// (<see cref="IImplantes"/>), dura según su nivel y luego recarga. Solo se activan a pie, con control y vivo; en el carro los tiempos siguen
    /// corriendo. Al morir se cortan y quedan en recarga. Los multiplicadores (<see cref="FactorCorrer"/> y demás) valen 1 si no están activas y los
    /// aplican los componentes de movimiento y combate encima de los efectos pasivos de los implantes. La agrega <see cref="Jugador.JugadorAPie"/> al nacer.
    /// </summary>
    public class HabilidadesProtesis : MonoBehaviour
    {
        private const int Cantidad = 3;

        private static bool cabezaActiva;

        private readonly ReglaHabilidadProtesis[] reglas = new ReglaHabilidadProtesis[Cantidad];
        private readonly InputAction[] acciones = new InputAction[Cantidad];
        private readonly Action<InputAction.CallbackContext>[] manejadores = new Action<InputAction.CallbackContext>[Cantidad];
        private readonly Jugador.SeguidorImplantes implantes = new Jugador.SeguidorImplantes();

        private ConfigHabilidadesProtesis config;
        private Jugador.JugadorAPie jugador;
        private ControladorAPie controlador;
        private VidaAPie vida;

        /// <summary>True mientras la habilidad CABEZA está activa: el visor de rayos X solo marca enemigos entonces.</summary>
        public static bool CabezaActiva => cabezaActiva;

        /// <summary>Se avisa cuando CABEZA se activa o se acaba.</summary>
        public static event Action CabezaCambio;

        /// <summary>El componente del jugador (null si no hay).</summary>
        public static HabilidadesProtesis Actual { get; private set; }

        /// <summary>Se avisa al activar una habilidad.</summary>
        public event Action<HabilidadProtesis> Activada;

        /// <summary>Se avisa al pulsar una habilidad sin tener su implante.</summary>
        public event Action<HabilidadProtesis> SinImplante;

        public ConfigHabilidadesProtesis Config => config;

        public EstadoHabilidad Estado(HabilidadProtesis h) => reglas[(int)h] != null ? reglas[(int)h].Estado : EstadoHabilidad.Lista;

        /// <summary>El medidor de 0 a 1 (lleno si está lista).</summary>
        public float Medidor(HabilidadProtesis h) => reglas[(int)h] != null ? reglas[(int)h].Medidor : 1f;

        public float Restante(HabilidadProtesis h) => reglas[(int)h] != null ? reglas[(int)h].Restante : 0f;

        /// <summary>El nivel del implante que da esa habilidad (0 = sin implante).</summary>
        public int Nivel(HabilidadProtesis h) => implantes.Nivel(Espacio(h));

        public static EspacioImplante Espacio(HabilidadProtesis h)
        {
            switch (h)
            {
                case HabilidadProtesis.Piernas: return EspacioImplante.Piernas;
                case HabilidadProtesis.Brazos: return EspacioImplante.Brazos;
                default: return EspacioImplante.Ojos;
            }
        }

        private bool Activa(HabilidadProtesis h) => Estado(h) == EstadoHabilidad.Activa;

        public float FactorCorrer => Activa(HabilidadProtesis.Piernas) ? config.FactorCorrer : 1f;
        public float FactorCaminar => Activa(HabilidadProtesis.Piernas) ? config.FactorCaminar : 1f;
        public float FactorSalto => Activa(HabilidadProtesis.Piernas) ? config.FactorSalto : 1f;
        public float FactorGolpe => Activa(HabilidadProtesis.Brazos) ? config.FactorGolpe : 1f;
        public float EmpujeGolpe => Activa(HabilidadProtesis.Brazos) ? config.EmpujeGolpe : 0f;
        public float FactorTiempoRecarga => Activa(HabilidadProtesis.Brazos) ? config.FactorTiempoRecarga : 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar()
        {
            cabezaActiva = false;
            CabezaCambio = null;
            Actual = null;
        }

        /// <summary>Solo para pruebas del visor: fija si CABEZA cuenta como activa sin necesitar al jugador.</summary>
        public static void PonerCabezaActiva(bool valor)
        {
            if (cabezaActiva == valor)
            {
                return;
            }
            cabezaActiva = valor;
            CabezaCambio?.Invoke();
        }

        private void Awake()
        {
            jugador = GetComponent<Jugador.JugadorAPie>();
            controlador = GetComponent<ControladorAPie>();
            vida = GetComponent<VidaAPie>();
            config = Resources.Load<ConfigHabilidadesProtesis>(ConfigHabilidadesProtesis.NombreRecurso);
            if (config == null)
            {
                Debug.LogError("[Personaje] Falta Resources/" + ConfigHabilidadesProtesis.NombreRecurso + " (ConfigHabilidadesProtesis) en " + name, this);
                enabled = false;
                return;
            }
            for (int i = 0; i < Cantidad; i++)
            {
                TiemposHabilidad t = config.Tiempos((HabilidadProtesis)i);
                reglas[i] = new ReglaHabilidadProtesis(t.duracionNivel1, t.duracionNivel2, t.recarga);
            }
        }

        private void OnEnable()
        {
            if (config == null)
            {
                return;
            }
            Actual = this;
            implantes.Activar();
            InputActionAsset controles = config.Controles;
            for (int i = 0; i < Cantidad; i++)
            {
                int indice = i;
                acciones[i] = controles != null ? controles.FindAction("APie/" + ConfigHabilidadesProtesis.NombreAccion((HabilidadProtesis)i), false) : null;
                if (acciones[i] == null)
                {
                    continue;
                }
                manejadores[i] = _ => Pulsar((HabilidadProtesis)indice);
                acciones[i].performed += manejadores[i];
                acciones[i].Enable();
            }
        }

        private void OnDisable()
        {
            if (Actual == this)
            {
                Actual = null;
                PonerCabezaActiva(false);
            }
            implantes.Desactivar();
            for (int i = 0; i < Cantidad; i++)
            {
                if (acciones[i] != null && manejadores[i] != null)
                {
                    acciones[i].performed -= manejadores[i];
                }
                acciones[i] = null;
                manejadores[i] = null;
            }
        }

        private bool PuedeActuar()
        {
            return jugador != null && jugador.Modo == ModoJugador.APie && controlador != null && controlador.ControlActivo
                && vida != null && !vida.EstaKO;
        }

        /// <summary>Intenta activar una habilidad (lo que hace su tecla). False si no se pudo.</summary>
        public bool Pulsar(HabilidadProtesis h)
        {
            if (!PuedeActuar())
            {
                return false;
            }
            int nivel = Nivel(h);
            if (nivel <= 0)
            {
                SinImplante?.Invoke(h);
                return false;
            }
            if (!reglas[(int)h].Activar(nivel))
            {
                return false;
            }
            Activada?.Invoke(h);
            Sincronizar();
            return true;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            bool muerto = vida != null && vida.EstaKO;
            for (int i = 0; i < Cantidad; i++)
            {
                if (muerto || Nivel((HabilidadProtesis)i) <= 0)
                {
                    reglas[i].Cortar();
                }
                reglas[i].Avanzar(dt);
            }
            Sincronizar();
        }

        private void Sincronizar() => PonerCabezaActiva(Activa(HabilidadProtesis.Cabeza));
    }
}

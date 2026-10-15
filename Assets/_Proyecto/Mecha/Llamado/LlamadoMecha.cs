using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Montaje;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Mecha.Llamado
{
    /// <summary>
    /// Llamado del mecha desde el taller (T-066). A pie, M llama al mecha: si el jugador está en una zona permitida
    /// (<see cref="IPuntoLlamadoMecha"/>) el mecha desaparece del taller y baja del cielo en la llegada; con el mecha vacío en la
    /// calle, M lo devuelve al taller, y regresa solo si el jugador se aleja. Solo actúa con un mecha Estacionado.
    /// </summary>
    [RequireComponent(typeof(MontajeMecha))]
    [RequireComponent(typeof(CharacterController))]
    public class LlamadoMecha : MonoBehaviour
    {
        [SerializeField] private ConfigLlamadoMecha config;
        [SerializeField] private InputActionReference llamar;
        [Tooltip("Material de neón de la marca que se ve en el piso de la llegada mientras el mecha viene.")]
        [SerializeField] private Material materialMarca;

        private MontajeMecha montaje;
        private CharacterController controlador;
        private EstadoLlamado estado = EstadoLlamado.EnTaller;
        private Pose poseInicial;
        private Pose destino;
        private Vector3 origenRegreso;
        private float tiempo;
        private GameObject marca;
        private Renderer[] renderizadores = Array.Empty<Renderer>();
        private bool[] renderizadorPrendido = Array.Empty<bool>();
        private Canvas[] lienzos = Array.Empty<Canvas>();
        private bool[] lienzoPrendido = Array.Empty<bool>();
        private Collider[] colisionadores = Array.Empty<Collider>();
        private bool[] colisionadorPrendido = Array.Empty<bool>();
        private bool visibleDuranteLlegada;
        private readonly List<PuntoLlamado> puntos = new List<PuntoLlamado>();

        public EstadoLlamado Estado => estado;
        public ConfigLlamadoMecha Config => config;

        /// <summary>Segundos que faltan para que el mecha llegue; 0 si no viene en camino.</summary>
        public float SegundosParaLlegar => estado == EstadoLlamado.Llegando ? Mathf.Max(0f, config.SegundosLlegada - tiempo) : 0f;

        /// <summary>false mientras el mecha viene en camino o regresa: no se puede subir.</summary>
        public bool PermiteSubir => estado == EstadoLlamado.EnTaller || estado == EstadoLlamado.EnCalle;

        /// <summary>Se dispara cada vez que el jugador intenta llamar, con el resultado.</summary>
        public event Action<ResultadoLlamado> LlamadoIntentado;

        /// <summary>Se dispara cuando cambia el estado, con el estado nuevo.</summary>
        public event Action<EstadoLlamado> EstadoCambio;

        private void Awake()
        {
            montaje = GetComponent<MontajeMecha>();
            controlador = GetComponent<CharacterController>();
            poseInicial = new Pose(transform.position, transform.rotation);
            if (config == null || llamar == null || materialMarca == null)
            {
                Debug.LogError("[Mecha] Faltan referencias en LlamadoMecha de " + name, this);
                enabled = false;
                return;
            }
            // Un mecha con piloto desde el inicio (arena, pruebas) no se llama.
            if (!montaje.Estacionado)
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            Registro<LlamadoMecha>.Agregar(this);
            llamar.action.Enable();
            montaje.OcupadoCambio += AlCambiarOcupado;
        }

        private void OnDisable()
        {
            Registro<LlamadoMecha>.Quitar(this);
            if (llamar != null)
            {
                llamar.action.Disable();
            }
            if (montaje != null)
            {
                montaje.OcupadoCambio -= AlCambiarOcupado;
            }
        }

        private void OnDestroy()
        {
            QuitarMarca();
        }

        // El taller se registra en su OnEnable: se busca aquí, no en Awake.
        private void Start()
        {
            if (!enabled)
            {
                return;
            }
            Colocar(PoseCasa());
        }

        private void Update()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador != null && jugador.Modo == ModoJugador.APie && llamar.action.WasPressedThisFrame())
            {
                IntentarLlamar();
            }

            switch (estado)
            {
                case EstadoLlamado.Llegando:
                    AvanzarLlegada(Time.deltaTime);
                    break;
                case EstadoLlamado.Regresando:
                    AvanzarRegreso(Time.deltaTime);
                    break;
                case EstadoLlamado.EnCalle:
                    ComprobarLejania(jugador);
                    break;
            }
        }

        /// <summary>Lo que hace la tecla M: llama al mecha o lo manda de vuelta. Devuelve lo que decidió la regla.</summary>
        public ResultadoLlamado IntentarLlamar()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador == null || jugador.Raiz == null)
            {
                return ResultadoLlamado.Nada;
            }

            IReadOnlyList<IPuntoLlamadoMecha> zonas = Registro<IPuntoLlamadoMecha>.Todos;
            puntos.Clear();
            for (int i = 0; i < zonas.Count; i++)
            {
                IPuntoLlamadoMecha zona = zonas[i];
                puntos.Add(zona != null ? new PuntoLlamado(zona.Llegada, zona.Radio, zona.Habilitado)
                    : new PuntoLlamado(Pose.identity, 0f, false));
            }

            ResultadoLlamado resultado = ReglaLlamado.Decidir(jugador.Raiz.position, puntos, estado, montaje.Ocupado, out int indice);
            switch (resultado)
            {
                case ResultadoLlamado.Llega:
                    IniciarLlegada(puntos[indice].Llegada);
                    break;
                case ResultadoLlamado.Regresa:
                    IniciarRegreso();
                    break;
            }
            LlamadoIntentado?.Invoke(resultado);
            return resultado;
        }

        // Si el jugador sube al mecha estando en el taller, el mecha ya no descansa ahí.
        private void AlCambiarOcupado(bool ocupado)
        {
            if (ocupado && estado == EstadoLlamado.EnTaller)
            {
                CambiarEstado(EstadoLlamado.EnCalle);
            }
        }

        private void CambiarEstado(EstadoLlamado nuevo)
        {
            estado = nuevo;
            tiempo = 0f;
            EstadoCambio?.Invoke(nuevo);
        }

        // ---- Llegada ----

        private void IniciarLlegada(Pose llegada)
        {
            destino = new Pose(llegada.position + Vector3.up * config.SeparacionPiso, llegada.rotation);
            GuardarYApagar();
            visibleDuranteLlegada = false;
            Colocar(new Pose(destino.position + Vector3.up * config.AlturaDescenso, destino.rotation));
            CrearMarca(llegada.position);
            CambiarEstado(EstadoLlamado.Llegando);
        }

        private void AvanzarLlegada(float dt)
        {
            tiempo += dt;
            float inicioDescenso = config.SegundosLlegada - config.SegundosDescenso;
            if (tiempo >= config.SegundosLlegada)
            {
                Colocar(destino);
                RestaurarVisuales();
                RestaurarColisionadores();
                QuitarMarca();
                CambiarEstado(EstadoLlamado.EnCalle);
                return;
            }
            if (tiempo < inicioDescenso)
            {
                return;
            }
            if (!visibleDuranteLlegada)
            {
                RestaurarVisuales();
                visibleDuranteLlegada = true;
            }
            // Cae acelerando: lento al empezar a verse, rápido al tocar el piso.
            float avance = Mathf.Clamp01((tiempo - inicioDescenso) / config.SegundosDescenso);
            float altura = config.AlturaDescenso * (1f - avance * avance);
            Colocar(new Pose(destino.position + Vector3.up * altura, destino.rotation));
        }

        // ---- Regreso ----

        private void IniciarRegreso()
        {
            GuardarColisionadores();
            origenRegreso = transform.position;
            CambiarEstado(EstadoLlamado.Regresando);
        }

        private void AvanzarRegreso(float dt)
        {
            tiempo += dt;
            if (tiempo >= config.SegundosRegreso)
            {
                Colocar(PoseCasa());
                RestaurarColisionadores();
                CambiarEstado(EstadoLlamado.EnTaller);
                return;
            }
            // Sube acelerando hasta perderse en el cielo.
            float avance = tiempo / config.SegundosRegreso;
            Colocar(new Pose(origenRegreso + Vector3.up * (config.AlturaDescenso * avance * avance), transform.rotation));
        }

        private void ComprobarLejania(IJugador jugador)
        {
            if (montaje.Ocupado || jugador == null || jugador.Raiz == null)
            {
                return;
            }
            if (ReglaLlamado.DistanciaHorizontal(jugador.Raiz.position, transform.position) > config.DistanciaRegresoAutomatico)
            {
                IniciarRegreso();
            }
        }

        // ---- Casa ----

        // Con taller registrado la casa es su estacionamiento; si no, la pose donde arrancó el mecha.
        private Pose PoseCasa()
        {
            ITallerMecha taller = Registro<ITallerMecha>.Actual;
            if (taller == null)
            {
                return poseInicial;
            }
            Pose estacionamiento = taller.Estacionamiento;
            return new Pose(estacionamiento.position + Vector3.up * config.SeparacionPiso, estacionamiento.rotation);
        }

        // Teletransporte con el CharacterController apagado un instante.
        private void Colocar(Pose pose)
        {
            bool prendido = controlador.enabled;
            controlador.enabled = false;
            transform.SetPositionAndRotation(pose.position, pose.rotation);
            controlador.enabled = prendido;
            Physics.SyncTransforms();
        }

        // ---- Visibilidad y colisiones ----
        // Se guarda lo que estaba prendido para devolverlo igual: el encendido de la cabina apaga luces por su cuenta.

        private void GuardarYApagar()
        {
            renderizadores = GetComponentsInChildren<Renderer>(true);
            renderizadorPrendido = new bool[renderizadores.Length];
            for (int i = 0; i < renderizadores.Length; i++)
            {
                renderizadorPrendido[i] = renderizadores[i].enabled;
                renderizadores[i].enabled = false;
            }
            lienzos = GetComponentsInChildren<Canvas>(true);
            lienzoPrendido = new bool[lienzos.Length];
            for (int i = 0; i < lienzos.Length; i++)
            {
                lienzoPrendido[i] = lienzos[i].enabled;
                lienzos[i].enabled = false;
            }
            GuardarColisionadores();
        }

        private void GuardarColisionadores()
        {
            colisionadores = GetComponentsInChildren<Collider>(true);
            colisionadorPrendido = new bool[colisionadores.Length];
            for (int i = 0; i < colisionadores.Length; i++)
            {
                colisionadorPrendido[i] = colisionadores[i].enabled;
                colisionadores[i].enabled = false;
            }
        }

        private void RestaurarVisuales()
        {
            for (int i = 0; i < renderizadores.Length; i++)
            {
                if (renderizadores[i] != null)
                {
                    renderizadores[i].enabled = renderizadorPrendido[i];
                }
            }
            for (int i = 0; i < lienzos.Length; i++)
            {
                if (lienzos[i] != null)
                {
                    lienzos[i].enabled = lienzoPrendido[i];
                }
            }
        }

        private void RestaurarColisionadores()
        {
            for (int i = 0; i < colisionadores.Length; i++)
            {
                if (colisionadores[i] != null)
                {
                    colisionadores[i].enabled = colisionadorPrendido[i];
                }
            }
            Physics.SyncTransforms();
        }

        // ---- Marca de llegada ----

        private void CrearMarca(Vector3 centro)
        {
            QuitarMarca();
            marca = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marca.name = "MarcaLlamadoMecha";
            Destroy(marca.GetComponent<Collider>());
            marca.transform.position = centro + Vector3.up * 0.03f;
            marca.transform.localScale = new Vector3(config.RadioMarca * 2f, 0.01f, config.RadioMarca * 2f);
            Renderer render = marca.GetComponent<Renderer>();
            render.sharedMaterial = materialMarca;
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            render.receiveShadows = false;
        }

        private void QuitarMarca()
        {
            if (marca != null)
            {
                Destroy(marca);
                marca = null;
            }
        }
    }
}

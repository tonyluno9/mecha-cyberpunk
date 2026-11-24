using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR21: manejar carros y motos en VR. Se agrega solo al jugador cuando el rig VR está armado (<see cref="AdaptadorVR.Listo"/>); sin VR no
    /// existe y en PC nada cambia.
    /// <list type="bullet">
    /// <item>Se sube y se baja con B (la Interactuar de siempre, en InteraccionAPie).</item>
    /// <item>Vista desde el asiento: cada LateUpdate (después del adaptador y de que el jugador se pega al asiento) el rig se coloca para que la
    /// cabeza quede en los ojos del conductor (<see cref="ReglaAsientoVR"/>), mirando al frente del vehículo, solo con su yaw y sin retraso.
    /// La cámara del carro de PC (CamaraCarro: sacudidas, FOV, tercera persona) se apaga y no va al visor; el oído va en la cabeza.</item>
    /// <item>Gatillo derecho acelera y el izquierdo frena y da reversa (analógicos), el stick izquierdo (X) dirige y A es el freno de mano: son
    /// bindings extra (grupo VR) en las acciones del mapa Carro de ese vehículo, que se quitan al bajar.</item>
    /// <item>Dentro no se camina ni gira con los sticks (el ControladorAPie está apagado y sin control), y la pistola, las armas especiales,
    /// el cambio de arma del stick derecho y el agarre se pausan (el gatillo derecho es el acelerador). El clic del stick derecho recentra la
    /// cabeza en el asiento.</item>
    /// <item>Viñeta suave al girar o acelerar fuerte (<see cref="ReglaVinetaManejoVR"/>), salvo con VIÑETA: NO. Fundido corto al subir y bajar.</item>
    /// <item>VR48: en carros (no en motos) la carrocería y el interior no se dibujan para la cámara mientras se maneja (sus MeshRenderer y
    /// SkinnedMeshRenderer pasan a solo-sombra): se ve limpio, como desde el parabrisas. Los colliders no se tocan; al bajar vuelven.</item>
    /// <item>Al bajar todo vuelve como estaba y el rig se recentra a pie (ojos a 1.72 m, mirando hacia donde quedó el cuerpo).</item>
    /// </list>
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class ManejoVR : MonoBehaviour, ITunnelingVignetteProvider
    {
        private const string GrupoVR = "VR";
        private const float SegundosFundido = 0.35f;
        private const float SuavidadAceleracion = 8f;
        private const float MinimoDt = 1e-4f;
        /// <summary>Los componentes VR del jugador que se pausan dentro del vehículo (por nombre: son de otras tareas).</summary>
        private static readonly string[] Pausados = { "PistolaVR", "ArmasEspecialesVR", "CambioArmaStickVR", "AgarreImplanteVR" };
        public const string RutaAcelerar = EntradaVR.Der + "/{Trigger}";
        public const string RutaFrenar = EntradaVR.Izq + "/{Trigger}";
        public const string RutaGirar = EntradaVR.Izq + "/{Primary2DAxis}/x";
        public const string RutaFrenoMano = EntradaVR.Der + "/{PrimaryButton}";
        private const string RutaRecentrar = EntradaVR.Der + "/{Primary2DAxisClick}";
        private const string ProcesadorGirar = "axisDeadzone(min=0.15,max=0.95)";

        private static readonly (string accion, string ruta, string procesador)[] Bindings =
        {
            ("Acelerar", RutaAcelerar, null),
            ("Frenar", RutaFrenar, null),
            ("Girar", RutaGirar, ProcesadorGirar),
            ("FrenoMano", RutaFrenoMano, null),
        };

        private AdaptadorVR adaptador;
        private JugadorAPie jugador;
        private InputAction recentrar;
        private bool manejando;
        private IMontable montable;
        private Transform vehiculo;
        private CarroViejo carro;
        private Rigidbody cuerpo;
        private RigidbodyInterpolation interpolacionPrevia;
        private Camera camaraVehiculo;
        private bool camaraVehiculoPrendida;
        private AudioListener oyenteVehiculo;
        private bool oyenteVehiculoPrendido;
        private AudioListener oyenteJugador;
        private Vector3 ojosLocal;
        private Vector3 camaraRef;
        private float yawCabezaRef;
        private float yawVehiculo;
        private TunnelingVignetteController vineta;
        private readonly VignetteParameters parametros = new VignetteParameters
        {
            apertureSize = 0.75f,
            featheringEffect = 0.25f,
            easeInTime = 0.3f,
            easeOutTime = 0.4f,
            vignetteColor = Color.black,
        };
        private bool vinetaPuesta;
        private float segundosVineta;
        private float velocidadPrevia;
        private float aceleracionSuave;
        private readonly List<(InputAction accion, string ruta)> agregados = new List<(InputAction, string)>();
        private readonly List<(Behaviour componente, bool prendido)> pausados = new List<(Behaviour, bool)>();
        private readonly List<(Renderer renderer, ShadowCastingMode sombra, bool forzado)> ocultos = new List<(Renderer, ShadowCastingMode, bool)>();
        private bool esMoto;

        public VignetteParameters vignetteParameters => parametros;

        /// <summary>True mientras el jugador va manejando en VR.</summary>
        public bool Manejando => manejando;

        /// <summary>El vehículo que maneja (null a pie).</summary>
        public IMontable Vehiculo => manejando ? montable : null;

        /// <summary>Dónde van los ojos del conductor ahora (mundo).</summary>
        public Vector3 OjosMundo => vehiculo != null ? vehiculo.TransformPoint(ojosLocal) : Vector3.zero;

        /// <summary>Los ojos en el espacio local del vehículo.</summary>
        public Vector3 OjosLocal => ojosLocal;

        /// <summary>True si los ojos se estimaron con los bounds (el vehículo no tenía asiento o su altura no era creíble).</summary>
        public bool AsientoEstimado { get; private set; }

        /// <summary>True mientras la viñeta de manejo está pedida.</summary>
        public bool VinetaPuesta => vinetaPuesta;

        /// <summary>VR48: cuántos renderers del carro están ocultos para la cámara ahora (0 a pie y en moto).</summary>
        public int RenderersOcultos => ocultos.Count;

        /// <summary>Cuántos bindings VR van puestos en el mapa del vehículo.</summary>
        public int BindingsPuestos => agregados.Count;

        // ---- Arranque ----

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            AdaptadorVR.Listo -= AlListo;
            AdaptadorVR.Listo += AlListo;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RevisarActual()
        {
            AdaptadorVR.Listo -= AlListo;
            AdaptadorVR.Listo += AlListo;
            if (AdaptadorVR.Actual != null && AdaptadorVR.Actual.Armado)
            {
                AlListo(AdaptadorVR.Actual);
            }
        }

        /// <summary>Agrega el manejo VR al jugador del rig (no duplica). Público para las pruebas.</summary>
        public static void AlListo(AdaptadorVR rig)
        {
            if (rig != null && rig.Jugador != null && rig.GetComponent<ManejoVR>() == null)
            {
                rig.gameObject.AddComponent<ManejoVR>();
            }
        }

        private void Awake()
        {
            adaptador = GetComponent<AdaptadorVR>();
            jugador = GetComponent<JugadorAPie>();
            if (adaptador == null || jugador == null)
            {
                Debug.LogError("[VR] ManejoVR sin AdaptadorVR o JugadorAPie en " + name, this);
                enabled = false;
                return;
            }
            recentrar = new InputAction("VR_RecentrarAsiento", InputActionType.Button, RutaRecentrar);
        }

        private void OnEnable()
        {
            if (jugador == null)
            {
                return;
            }
            jugador.ModoCambio += AlCambiarModo;
            if (jugador.Modo == ModoJugador.Carro && !manejando)
            {
                Entrar(jugador.MontableActual);
            }
        }

        private void OnDisable()
        {
            if (jugador != null)
            {
                jugador.ModoCambio -= AlCambiarModo;
            }
            if (manejando)
            {
                Salir(false);
            }
        }

        private void OnDestroy()
        {
            recentrar?.Dispose();
            recentrar = null;
        }

        private void AlCambiarModo(ModoJugador modo)
        {
            if (modo == ModoJugador.Carro)
            {
                if (!manejando)
                {
                    Entrar(jugador.MontableActual);
                }
            }
            else if (manejando)
            {
                Salir(true);
            }
        }

        // ---- Subir y bajar ----

        private void Entrar(IMontable nuevo)
        {
            if (nuevo == null || nuevo.Modo != ModoJugador.Carro || adaptador == null || !adaptador.Armado || adaptador.Camara == null)
            {
                return;
            }
            vehiculo = nuevo is Component c ? c.transform : (nuevo.Asiento != null ? nuevo.Asiento.root : null);
            if (vehiculo == null)
            {
                return;
            }
            montable = nuevo;
            manejando = true;
            carro = vehiculo.GetComponent<CarroViejo>();
            CalcularOjos();

            // La cabeza sigue siendo la cámara del visor; la del carro de PC (con sus sacudidas) se apaga y no va al visor.
            Camera cabeza = adaptador.Camara;
            cabeza.enabled = true;
            SistemaVR.PermitirXR(cabeza, true);
            SistemaVR.CamaraVR = cabeza;
            oyenteJugador = cabeza.GetComponent<AudioListener>();
            camaraVehiculo = montable.Camara;
            oyenteVehiculo = camaraVehiculo != null ? camaraVehiculo.GetComponent<AudioListener>() : null;
            oyenteVehiculoPrendido = oyenteVehiculo != null && oyenteVehiculo.enabled;
            camaraVehiculoPrendida = camaraVehiculo != null && camaraVehiculo.enabled;
            if (camaraVehiculo != null)
            {
                SistemaVR.PermitirXR(camaraVehiculo, false);
                camaraVehiculo.enabled = false;
            }
            // Se oye desde la cabeza (un solo AudioListener prendido).
            if (oyenteVehiculo != null)
            {
                oyenteVehiculo.enabled = false;
            }
            if (oyenteJugador != null)
            {
                oyenteJugador.enabled = true;
            }

            // Sin interpolación el asiento avanza a saltos de física: en el visor se ve temblar todo.
            cuerpo = vehiculo.GetComponent<Rigidbody>();
            if (cuerpo != null)
            {
                interpolacionPrevia = cuerpo.interpolation;
                cuerpo.interpolation = RigidbodyInterpolation.Interpolate;
                velocidadPrevia = Vector3.Dot(cuerpo.linearVelocity, vehiculo.forward);
            }
            aceleracionSuave = 0f;
            segundosVineta = 0f;

            PonerBindings();
            Pausar();
            OcultarCarroceria();
            Recentrar();
            vineta = cabeza.GetComponentInChildren<TunnelingVignetteController>(true);
            recentrar?.Enable();
            adaptador.Fundir(SegundosFundido);
            ColocarCabeza();
            Debug.Log("[VR] Maneja " + montable.Nombre + ": ojos en " + ojosLocal.ToString("F2") + " (local" + (AsientoEstimado ? ", estimados" : "") + ").");
        }

        private void Salir(bool recentrarAPie)
        {
            manejando = false;
            recentrar?.Disable();
            QuitarBindings();
            Reanudar();
            MostrarCarroceria();
            PonerVineta(false);
            if (cuerpo != null)
            {
                cuerpo.interpolation = interpolacionPrevia;
            }
            bool sigueDentro = jugador != null && jugador.Modo == ModoJugador.Carro;
            if (sigueDentro)
            {
                // Se quitó el manejo VR sin bajar (salir de VR): todo como lo espera PC dentro del carro.
                if (camaraVehiculo != null)
                {
                    camaraVehiculo.enabled = camaraVehiculoPrendida;
                }
                if (oyenteJugador != null)
                {
                    oyenteJugador.enabled = false;
                }
                if (oyenteVehiculo != null)
                {
                    oyenteVehiculo.enabled = oyenteVehiculoPrendido;
                }
            }
            if (recentrarAPie && adaptador != null && adaptador.Armado)
            {
                adaptador.Recentrar();
                adaptador.Fundir(SegundosFundido);
            }
            Debug.Log("[VR] Deja de manejar" + (montable != null ? " " + montable.Nombre : "") + ".");
            montable = null;
            vehiculo = null;
            carro = null;
            cuerpo = null;
            camaraVehiculo = null;
            oyenteVehiculo = null;
            vineta = null;
        }

        /// <summary>
        /// Recentra la cabeza en el asiento: la pose del visor en el cuarto de ahora queda en los ojos del conductor, mirando al frente.
        /// Se hace al subir y con el clic del stick derecho.
        /// </summary>
        public void Recentrar()
        {
            if (!manejando || adaptador == null || adaptador.Camara == null)
            {
                return;
            }
            Transform cam = adaptador.Camara.transform;
            camaraRef = cam.localPosition;
            yawCabezaRef = ReglaAsientoVR.Yaw(cam.localRotation * Vector3.forward, 0f);
        }

        // Los ojos del conductor en el espacio del vehículo (se calculan una vez por subida).
        private void CalcularOjos()
        {
            bool moto = vehiculo.GetComponent<ManejoMoto>() != null || vehiculo.name.ToLowerInvariant().Contains("moto");
            esMoto = moto;
            float escala = vehiculo.lossyScale.y;
            Bounds caja = BoundsLocales(vehiculo);
            Transform asiento = montable.Asiento;
            AsientoEstimado = true;
            if (asiento != null)
            {
                float alturaOjos = moto ? ReglaAsientoVR.AlturaOjosMoto : ReglaAsientoVR.AlturaOjosCarro;
                CamaraCarro camaraCarro = montable.Camara != null ? montable.Camara.GetComponent<CamaraCarro>() : null;
                if (camaraCarro != null && camaraCarro.Config != null)
                {
                    alturaOjos = camaraCarro.Config.AlturaOjos; // la misma primera persona que en PC
                }
                ojosLocal = vehiculo.InverseTransformPoint(ReglaAsientoVR.OjosDesdeAsiento(asiento.position, vehiculo.up, alturaOjos));
                AsientoEstimado = caja.size != Vector3.zero && !ReglaAsientoVR.AlturaValida(ojosLocal, caja, escala);
            }
            if (AsientoEstimado)
            {
                ojosLocal = ReglaAsientoVR.OjosEstimadosLocal(caja, moto, escala);
            }
            if (!moto && caja.size != Vector3.zero)
            {
                ojosLocal = ReglaAsientoVR.BajoElTecho(ojosLocal, caja, escala);
            }
            yawVehiculo = ReglaAsientoVR.Yaw(vehiculo.forward, 0f);
        }

        // Bounds de los renderers del vehículo en su espacio local.
        private static Bounds BoundsLocales(Transform raiz)
        {
            bool hay = false;
            Bounds caja = new Bounds(Vector3.zero, Vector3.zero);
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer)
                {
                    continue;
                }
                Bounds local = r.localBounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 esquina = local.center + Vector3.Scale(local.extents,
                        new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    Vector3 punto = raiz.InverseTransformPoint(r.transform.TransformPoint(esquina));
                    if (!hay)
                    {
                        caja = new Bounds(punto, Vector3.zero);
                        hay = true;
                    }
                    else
                    {
                        caja.Encapsulate(punto);
                    }
                }
            }
            return caja;
        }

        // ---- Cada cuadro ----

        private void Update()
        {
            if (!manejando)
            {
                return;
            }
            if (adaptador == null || !adaptador.Armado)
            {
                Salir(false);
                return;
            }
            if (recentrar != null && recentrar.WasPressedThisFrame())
            {
                Recentrar();
            }
        }

        // Después del AdaptadorVR (-150) y del JugadorAPie (0), que pega la raíz al asiento: la cabeza va en los ojos, sin retraso.
        private void LateUpdate()
        {
            if (!manejando)
            {
                return;
            }
            if (vehiculo == null)
            {
                return;
            }
            ColocarCabeza();
            ActualizarVineta(Time.deltaTime);
        }

        private void ColocarCabeza()
        {
            Transform ajuste = adaptador.Ajuste;
            Transform rig = adaptador.Rig;
            if (ajuste == null || rig == null)
            {
                return;
            }
            yawVehiculo = ReglaAsientoVR.Yaw(vehiculo.forward, yawVehiculo);
            // La cabeza de referencia en el espacio del rig, con la altura que tenga el ajuste ahora (el offset y el ajuste no giran).
            Vector3 cabezaRef = ajuste.parent.localPosition + ajuste.localPosition + camaraRef;
            Pose pose = ReglaAsientoVR.PoseRig(OjosMundo, yawVehiculo, cabezaRef, yawCabezaRef);
            rig.SetPositionAndRotation(pose.position, pose.rotation);
        }

        private void ActualizarVineta(float dt)
        {
            if (dt <= MinimoDt)
            {
                return;
            }
            float giro = 0f;
            float aceleracion = 0f;
            if (cuerpo != null)
            {
                giro = Vector3.Dot(cuerpo.angularVelocity, vehiculo.up) * Mathf.Rad2Deg;
                float velocidad = Vector3.Dot(cuerpo.linearVelocity, vehiculo.forward);
                aceleracionSuave = Mathf.Lerp(aceleracionSuave, (velocidad - velocidadPrevia) / dt, 1f - Mathf.Exp(-SuavidadAceleracion * dt));
                velocidadPrevia = velocidad;
                aceleracion = aceleracionSuave;
            }
            segundosVineta = ReglaVinetaManejoVR.Avanzar(segundosVineta, giro, aceleracion, OpcionesVR.Vineta, dt);
            PonerVineta(segundosVineta > 0f);
        }

        private void PonerVineta(bool poner)
        {
            if (poner == vinetaPuesta)
            {
                return;
            }
            vinetaPuesta = poner;
            if (vineta == null)
            {
                return;
            }
            if (poner)
            {
                vineta.BeginTunnelingVignette(this);
            }
            else
            {
                vineta.EndTunnelingVignette(this);
            }
        }

        // ---- Entrada del vehículo ----

        private void PonerBindings()
        {
            InputActionMap mapa = carro != null ? carro.MapaControles : null;
            if (mapa == null)
            {
                Debug.LogWarning("[VR] El vehículo " + vehiculo.name + " no tiene mapa Carro: no se maneja con los controles.", this);
                return;
            }
            bool prendido = mapa.enabled;
            mapa.Disable();
            foreach (var (nombre, ruta, procesador) in Bindings)
            {
                InputAction accion = mapa.FindAction(nombre);
                if (accion == null)
                {
                    continue;
                }
                accion.AddBinding(ruta, processors: procesador, groups: GrupoVR);
                agregados.Add((accion, ruta));
            }
            if (prendido)
            {
                mapa.Enable();
            }
        }

        private void QuitarBindings()
        {
            var mapas = new HashSet<InputActionMap>();
            var prendidos = new HashSet<InputActionMap>();
            foreach (var (accion, _) in agregados)
            {
                InputActionMap mapa = accion?.actionMap;
                if (mapa != null && mapas.Add(mapa) && mapa.enabled)
                {
                    prendidos.Add(mapa);
                }
            }
            foreach (InputActionMap mapa in mapas)
            {
                mapa.Disable();
            }
            foreach (var (accion, ruta) in agregados)
            {
                if (accion == null)
                {
                    continue;
                }
                for (int i = accion.bindings.Count - 1; i >= 0; i--)
                {
                    if (accion.bindings[i].path == ruta && accion.bindings[i].groups == GrupoVR)
                    {
                        accion.ChangeBinding(i).Erase();
                    }
                }
            }
            foreach (InputActionMap mapa in prendidos)
            {
                mapa.Enable();
            }
            agregados.Clear();
        }

        // ---- VR48: la carrocería no se ve desde dentro ----

        // Los renderers de malla del carro pasan a solo-sombra (la cámara no los dibuja, la sombra en el piso queda); los que no daban sombra se
        // apagan con forceRenderingOff. Partículas, estelas y líneas se quedan. Nada del jugador ni marcado con IgnorarOcultarVR.
        private void OcultarCarroceria()
        {
            MostrarCarroceria();
            if (vehiculo == null || !ReglaAsientoVR.OcultarCarroceria(esMoto))
            {
                return;
            }
            foreach (Renderer r in vehiculo.GetComponentsInChildren<Renderer>())
            {
                if (r == null || !(r is MeshRenderer || r is SkinnedMeshRenderer) || r.transform.IsChildOf(transform)
                    || r.GetComponentInParent<IgnorarOcultarVR>() != null)
                {
                    continue;
                }
                ShadowCastingMode sombra = r.shadowCastingMode;
                bool forzado = r.forceRenderingOff;
                if (sombra == ShadowCastingMode.Off)
                {
                    r.forceRenderingOff = true;
                }
                else
                {
                    r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
                ocultos.Add((r, sombra, forzado));
            }
        }

        private void MostrarCarroceria()
        {
            foreach (var (r, sombra, forzado) in ocultos)
            {
                if (r != null)
                {
                    r.shadowCastingMode = sombra;
                    r.forceRenderingOff = forzado;
                }
            }
            ocultos.Clear();
        }

        // ---- Lo que no va dentro del vehículo ----

        private void Pausar()
        {
            pausados.Clear();
            foreach (Behaviour b in GetComponents<Behaviour>())
            {
                if (b == null || b == this || System.Array.IndexOf(Pausados, b.GetType().Name) < 0)
                {
                    continue;
                }
                pausados.Add((b, b.enabled));
                b.enabled = false;
            }
        }

        private void Reanudar()
        {
            foreach (var (componente, prendido) in pausados)
            {
                if (componente != null)
                {
                    componente.enabled = prendido;
                }
            }
            pausados.Clear();
        }
    }
}

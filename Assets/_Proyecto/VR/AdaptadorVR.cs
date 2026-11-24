using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using Unity.Cinemachine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TrackedPoseDriver = UnityEngine.InputSystem.XR.TrackedPoseDriver;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR1: pone el rig VR (XR Origin) en el jugador a pie. La cámara del jugador pasa a ser la cabeza (TrackedPoseDriver del visor),
    /// a la altura de los ojos de PJ01 (1.72 m de una persona de 1.84 m) sea cual sea la estatura real. El cuerpo de PJ01 solo
    /// da sombra; se ven los controles de los Starter Assets como manos.
    /// Caminar: stick izquierdo (lo mueve el mismo ControladorAPie, con sus velocidades 1.62 / 4.53, clic del stick = correr) hacia
    /// donde mira la cabeza. Giro por pasos de 45° con el stick derecho. Viñeta al moverse y al girar. Caminar en el cuarto empuja
    /// la cápsula (con colisión) para que la cabeza no atraviese paredes.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public class AdaptadorVR : MonoBehaviour, ITunnelingVignetteProvider
    {
        public const float AlturaOjos = 1.72f;
        public const float GradosGiro = 45f;
        private const float UmbralGiro = 0.75f;
        private const float RearmeGiro = 0.35f;
        private const float VelocidadVineta = 0.4f;
        private const string GrupoVR = "VR";

        /// <summary>El adaptador del jugador actual (null sin VR).</summary>
        public static AdaptadorVR Actual { get; private set; }

        /// <summary>Se dispara cuando el rig está armado (manos y cámara listas).</summary>
        public static event Action<AdaptadorVR> Listo;

        public Transform Rig { get; private set; }
        /// <summary>Nivel que ajusta la altura (hijo del offset del XR Origin): padre de la cámara y las manos.</summary>
        public Transform Ajuste { get; private set; }
        public Camera Camara { get; private set; }
        public ManoVR ManoIzquierda { get; private set; }
        public ManoVR ManoDerecha { get; private set; }
        public XROrigin Origen { get; private set; }
        public JugadorAPie Jugador { get; private set; }
        public ControladorAPie Controlador { get; private set; }
        public bool Armado { get; private set; }

        /// <summary>Giro del rig en el mundo (°): solo cambia con el stick derecho.</summary>
        public float GiroRig => giroRig;

        /// <summary>Hacia dónde mira la cabeza, en el plano (normalizado).</summary>
        public Vector3 FrenteCabeza
        {
            get
            {
                Vector3 f = Camara != null ? Camara.transform.forward : transform.forward;
                f.y = 0f;
                return f.sqrMagnitude > 1e-4f ? f.normalized : transform.forward;
            }
        }

        private CharacterController capsula;
        private float giroRig;
        private bool giroArmado = true;
        private Vector3 cabezaPrevia;
        private bool hayCabezaPrevia;
        private bool calibrado;
        private float segundosSinCalibrar;
        private TunnelingVignetteController vineta;
        private bool vinetaPuesta;
        private float segundosVinetaGiro;
        private readonly VignetteParameters parametros = new VignetteParameters();
        private readonly ProveedorFundido fundido = new ProveedorFundido();
        private float segundosFundido;
        private readonly Dictionary<Renderer, ShadowCastingMode> sombrasOriginales = new Dictionary<Renderer, ShadowCastingMode>();
        private float segundosOcultar;
        private VidaAPie vida;
        private CamaraHombro camaraHombro;
        private CinemachineBrain brain;
        private Transform padreCamaraOriginal;
        private Pose poseCamaraOriginal;
        private static readonly List<(InputAction accion, string ruta)> bindingsAgregados = new List<(InputAction, string)>();

        public VignetteParameters vignetteParameters => parametros;

        private void Start()
        {
            if (!ModoVR.Activo)
            {
                Destroy(this);
                return;
            }
            Construir();
        }

        private void Construir()
        {
            Jugador = GetComponent<JugadorAPie>();
            Controlador = GetComponent<ControladorAPie>();
            capsula = GetComponent<CharacterController>();
            vida = GetComponent<VidaAPie>();
            Camara = Jugador != null ? Jugador.Camara : GetComponentInChildren<Camera>(true);
            if (Camara == null)
            {
                Debug.LogError("[VR] El jugador no tiene cámara: no se arma el rig.", this);
                enabled = false;
                return;
            }
            Actual = this;

            // La cámara de hombro (Cinemachine) ya no mueve la cámara: la mueve el visor.
            camaraHombro = Camara.GetComponent<CamaraHombro>();
            if (camaraHombro != null)
            {
                if (camaraHombro.Vcam != null)
                {
                    camaraHombro.Vcam.gameObject.SetActive(false);
                }
                camaraHombro.enabled = false;
            }
            brain = Camara.GetComponent<CinemachineBrain>();
            if (brain != null)
            {
                brain.enabled = false;
            }

            giroRig = transform.eulerAngles.y;
            var rigObjeto = new GameObject("RigVR");
            rigObjeto.SetActive(false);
            Rig = rigObjeto.transform;
            Rig.SetParent(transform, false);
            Rig.SetPositionAndRotation(transform.position, Quaternion.Euler(0f, giroRig, 0f));
            var offset = new GameObject("CameraOffset").transform;
            offset.SetParent(Rig, false);
            Ajuste = new GameObject("AjusteAltura").transform;
            Ajuste.SetParent(offset, false);

            padreCamaraOriginal = Camara.transform.parent;
            poseCamaraOriginal = new Pose(Camara.transform.localPosition, Camara.transform.localRotation);
            Camara.transform.SetParent(Ajuste, false);
            Camara.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            Camara.transform.localRotation = Quaternion.identity;
            Camara.nearClipPlane = 0.05f;
            TrackedPoseDriver cabeza = Camara.GetComponent<TrackedPoseDriver>();
            if (cabeza == null)
            {
                cabeza = Camara.gameObject.AddComponent<TrackedPoseDriver>();
            }
            ConfigurarPose(cabeza, "<XRHMD>/centerEyePosition", "<XRHMD>/centerEyeRotation");
            UniversalAdditionalCameraData datosCamara = Camara.GetUniversalAdditionalCameraData();
            if (datosCamara != null)
            {
                // Postproceso sí (el neón es el juego), pero sin el antialias de pantalla: el MSAA del URP lo hace en VR.
                datosCamara.antialiasing = AntialiasingMode.None;
            }

            Origen = rigObjeto.AddComponent<XROrigin>();
            Origen.Origin = rigObjeto;
            Origen.CameraFloorOffsetObject = offset.gameObject;
            Origen.Camera = Camara;
            Origen.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

            RecursosVR recursos = RecursosVR.Cargar();
            ManoIzquierda = CrearMano(false, recursos != null ? recursos.controlIzquierdo : null);
            ManoDerecha = CrearMano(true, recursos != null ? recursos.controlDerecho : null);
            if (recursos != null && recursos.accionesXRI != null)
            {
                // Los modelos de los controles animan sus botones con las acciones de XRI.
                recursos.accionesXRI.Enable();
            }
            if (recursos != null && recursos.vinetaTunel != null)
            {
                GameObject v = Instantiate(recursos.vinetaTunel, Camara.transform, false);
                v.name = "VinetaTunel";
                vineta = v.GetComponent<TunnelingVignetteController>();
            }
            parametros.apertureSize = 0.7f;
            parametros.featheringEffect = 0.2f;
            parametros.easeInTime = 0.2f;
            parametros.easeOutTime = 0.3f;
            parametros.vignetteColor = Color.black;
            rigObjeto.SetActive(true);

            AgregarBindingsBase();
            if (vida != null)
            {
                vida.KO += AlKO;
            }
            OcultarCuerpo();
            Armado = true;
            SistemaVR.CamaraVR = Camara;
            Debug.Log("[VR] Rig VR armado en " + name + ".");
            Listo?.Invoke(this);
        }

        private ManoVR CrearMano(bool derecha, GameObject modelo)
        {
            var objeto = new GameObject(derecha ? "ManoDerecha" : "ManoIzquierda");
            objeto.transform.SetParent(Ajuste, false);
            objeto.transform.localPosition = new Vector3(derecha ? 0.2f : -0.2f, 1.1f, 0.3f);
            string mano = derecha ? EntradaVR.Der : EntradaVR.Izq;
            TrackedPoseDriver pose = objeto.AddComponent<TrackedPoseDriver>();
            ConfigurarPose(pose, mano + "/devicePosition", mano + "/deviceRotation");
            ManoVR m = objeto.AddComponent<ManoVR>();
            m.Configurar(derecha, Rig);
            if (modelo != null)
            {
                GameObject visual = Instantiate(modelo, objeto.transform, false);
                visual.name = "Modelo";
            }
            return m;
        }

        private static void ConfigurarPose(TrackedPoseDriver tpd, string posicion, string rotacion)
        {
            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            tpd.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            tpd.ignoreTrackingState = true;
            tpd.positionInput = new InputActionProperty(new InputAction(posicion, InputActionType.Value, posicion, expectedControlType: "Vector3"));
            tpd.rotationInput = new InputActionProperty(new InputAction(rotacion, InputActionType.Value, rotacion, expectedControlType: "Quaternion"));
        }

        /// <summary>
        /// Agrega a una acción del mapa del jugador a pie (ControlesAPie, mapa APie) un binding de los controles VR.
        /// Se quita al salir. Úsalo para que una acción existente (Recargar, Rueda...) también responda a un botón del control.
        /// </summary>
        public static bool AgregarBinding(string nombreAccion, string ruta)
        {
            InputAction accion = BuscarAccionAPie(nombreAccion);
            if (accion == null)
            {
                Debug.LogWarning("[VR] No encontré la acción " + nombreAccion + " del mapa APie.");
                return false;
            }
            foreach (InputBinding b in accion.bindings)
            {
                if (b.path == ruta)
                {
                    return true;
                }
            }
            InputActionMap mapa = accion.actionMap;
            var encendidas = new List<InputAction>();
            if (mapa != null)
            {
                foreach (InputAction a in mapa.actions)
                {
                    if (a.enabled)
                    {
                        encendidas.Add(a);
                    }
                }
                mapa.Disable();
            }
            else
            {
                accion.Disable();
            }
            accion.AddBinding(ruta, groups: GrupoVR);
            bindingsAgregados.Add((accion, ruta));
            foreach (InputAction a in encendidas)
            {
                a.Enable();
            }
            if (mapa == null)
            {
                accion.Enable();
            }
            return true;
        }

        /// <summary>La acción <paramref name="nombre"/> del mapa APie (la instancia que usa el juego), o null.</summary>
        public static InputAction BuscarAccionAPie(string nombre)
        {
            var lista = new List<InputAction>();
            InputSystem.ListEnabledActions(lista);
            foreach (InputAction a in lista)
            {
                if (a.actionMap != null && a.actionMap.name == "APie")
                {
                    InputAction encontrada = a.actionMap.FindAction(nombre);
                    if (encontrada != null)
                    {
                        return encontrada;
                    }
                }
            }
            return null;
        }

        private static void QuitarBindings()
        {
            foreach (var (accion, ruta) in bindingsAgregados)
            {
                if (accion == null)
                {
                    continue;
                }
                InputActionMap mapa = accion.actionMap;
                var encendidas = new List<InputAction>();
                if (mapa != null)
                {
                    foreach (InputAction a in mapa.actions)
                    {
                        if (a.enabled)
                        {
                            encendidas.Add(a);
                        }
                    }
                    mapa.Disable();
                }
                for (int i = accion.bindings.Count - 1; i >= 0; i--)
                {
                    if (accion.bindings[i].path == ruta && accion.bindings[i].groups == GrupoVR)
                    {
                        accion.ChangeBinding(i).Erase();
                    }
                }
                foreach (InputAction a in encendidas)
                {
                    a.Enable();
                }
            }
            bindingsAgregados.Clear();
        }

        // Caminar, correr, saltar, interactuar y recargar con los controles (lo demás lo ponen VR2–VR4).
        private static void AgregarBindingsBase()
        {
            AgregarBinding("Mover", EntradaVR.Izq + "/{Primary2DAxis}");
            AgregarBinding("Correr", EntradaVR.Izq + "/{Primary2DAxisClick}");
            AgregarBinding("Saltar", EntradaVR.Izq + "/{PrimaryButton}");
            AgregarBinding("Interactuar", EntradaVR.Der + "/{SecondaryButton}");
            AgregarBinding("Recargar", EntradaVR.Der + "/{PrimaryButton}");
        }

        private void Update()
        {
            if (!Armado)
            {
                return;
            }
            // El movimiento del ControladorAPie es relativo a su giro de cámara: aquí es el de la cabeza.
            if (Controlador != null)
            {
                Vector3 f = FrenteCabeza;
                float yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
                Controlador.Empujar(Mathf.DeltaAngle(Controlador.GiroCamara, yaw), 0f);
            }
            float x = EntradaVR.Eje(EntradaVR.StickDerecho).x;
            if (giroArmado && Mathf.Abs(x) > UmbralGiro && (Controlador == null || Controlador.ControlActivo))
            {
                giroArmado = false;
                giroRig = Mathf.Repeat(giroRig + Mathf.Sign(x) * GradosGiro, 360f);
                segundosVinetaGiro = 0.25f;
            }
            else if (Mathf.Abs(x) < RearmeGiro)
            {
                giroArmado = true;
            }
        }

        private void LateUpdate()
        {
            if (!Armado)
            {
                return;
            }
            float dt = Time.deltaTime;
            // Cabeza en el espacio del rig (el rig no escala ni las capas intermedias giran).
            Transform offset = Ajuste.parent;
            Vector3 cabeza = offset.localPosition + Ajuste.localPosition + Camara.transform.localPosition;
            Calibrar(dt);

            // Caminar en el cuarto: la cápsula sigue a la cabeza, chocando con lo que haya.
            if (hayCabezaPrevia && capsula != null && capsula.enabled && Jugador != null && Jugador.Modo == ModoJugador.APie)
            {
                Vector3 delta = cabeza - cabezaPrevia;
                delta.y = 0f;
                if (delta.sqrMagnitude > 1e-8f && delta.sqrMagnitude < 1f)
                {
                    capsula.Move(Rig.rotation * delta);
                }
            }
            cabezaPrevia = cabeza;
            hayCabezaPrevia = true;

            // El rig se queda con su giro (no gira con el cuerpo) y pone la cabeza justo sobre la cápsula.
            Quaternion giro = Quaternion.Euler(0f, giroRig, 0f);
            Vector3 horizontal = new Vector3(cabeza.x, 0f, cabeza.z);
            Rig.SetPositionAndRotation(transform.position - giro * horizontal, giro);

            ActualizarVineta(dt);
            segundosOcultar -= dt;
            if (segundosOcultar <= 0f)
            {
                segundosOcultar = 0.5f;
                OcultarCuerpo();
            }
        }

        // La altura de los ojos queda en la de PJ01 sea cual sea la estatura de quien juega.
        private void Calibrar(float dt)
        {
            if (calibrado)
            {
                return;
            }
            segundosSinCalibrar += dt;
            float alturaVisor = Camara.transform.localPosition.y;
            // Con el visor puesto da su altura al primer cuadro con pose; en el simulador puede quedarse en 0.
            if (segundosSinCalibrar > 0.5f && (Mathf.Abs(alturaVisor - 1.6f) > 1e-3f || segundosSinCalibrar > 2f))
            {
                Recalibrar();
            }
        }

        /// <summary>Vuelve a poner los ojos a la altura de PJ01 con la altura actual del visor.</summary>
        public void Recalibrar()
        {
            calibrado = true;
            float alturaVisor = Camara.transform.localPosition.y;
            Ajuste.localPosition = new Vector3(0f, AlturaOjos - alturaVisor, 0f);
            Debug.Log("[VR] Altura calibrada: visor " + alturaVisor.ToString("0.00") + " m, ajuste " + Ajuste.localPosition.y.ToString("0.00") + " m.");
        }

        private void ActualizarVineta(float dt)
        {
            segundosVinetaGiro = Mathf.Max(0f, segundosVinetaGiro - dt);
            Vector3 v = capsula != null ? capsula.velocity : Vector3.zero;
            v.y = 0f;
            bool moviendo = v.magnitude > VelocidadVineta || segundosVinetaGiro > 0f;
            if (vineta != null && moviendo != vinetaPuesta)
            {
                vinetaPuesta = moviendo;
                if (moviendo)
                {
                    vineta.BeginTunnelingVignette(this);
                }
                else
                {
                    vineta.EndTunnelingVignette(this);
                }
            }
            if (segundosFundido > 0f)
            {
                segundosFundido -= dt;
                if (segundosFundido <= 0f && vineta != null)
                {
                    vineta.EndTunnelingVignette(fundido);
                }
            }
        }

        /// <summary>Fundido a negro corto (en lugar de una sacudida, un derribo o un cambio de cámara).</summary>
        public void Fundir(float segundos)
        {
            if (vineta == null)
            {
                return;
            }
            segundosFundido = Mathf.Max(segundosFundido, segundos);
            vineta.BeginTunnelingVignette(fundido);
        }

        private void AlKO()
        {
            Fundir(1.5f);
        }

        // El cuerpo de PJ01 no se ve desde la cabeza: solo su sombra. Se repite porque el modelo cambia (ropa, implantes, armas).
        private void OcultarCuerpo()
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || (Rig != null && r.transform.IsChildOf(Rig)) || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer)
                {
                    continue;
                }
                if (r.GetComponentInParent<IgnorarOcultarVR>(true) != null)
                {
                    continue;
                }
                if (!sombrasOriginales.ContainsKey(r))
                {
                    sombrasOriginales[r] = r.shadowCastingMode;
                }
                if (r.shadowCastingMode != ShadowCastingMode.ShadowsOnly)
                {
                    r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
            }
        }

        private void OnDestroy()
        {
            if (vida != null)
            {
                vida.KO -= AlKO;
            }
            if (Actual == this)
            {
                Actual = null;
                QuitarBindings();
            }
            foreach (var par in sombrasOriginales)
            {
                if (par.Key != null)
                {
                    par.Key.shadowCastingMode = par.Value;
                }
            }
            if (Camara != null && padreCamaraOriginal != null)
            {
                Camara.transform.SetParent(padreCamaraOriginal, false);
                Camara.transform.SetLocalPositionAndRotation(poseCamaraOriginal.position, poseCamaraOriginal.rotation);
                TrackedPoseDriver tpd = Camara.GetComponent<TrackedPoseDriver>();
                if (tpd != null)
                {
                    Destroy(tpd);
                }
            }
            if (camaraHombro != null)
            {
                camaraHombro.enabled = true;
                if (camaraHombro.Vcam != null)
                {
                    camaraHombro.Vcam.gameObject.SetActive(true);
                }
            }
            if (brain != null)
            {
                brain.enabled = true;
            }
            if (Rig != null)
            {
                Destroy(Rig.gameObject);
            }
        }

        private sealed class ProveedorFundido : ITunnelingVignetteProvider
        {
            private readonly VignetteParameters p = new VignetteParameters
            {
                apertureSize = 0f,
                featheringEffect = 0f,
                easeInTime = 0.15f,
                easeOutTime = 0.35f,
                vignetteColor = Color.black,
            };

            public VignetteParameters vignetteParameters => p;
        }
    }
}

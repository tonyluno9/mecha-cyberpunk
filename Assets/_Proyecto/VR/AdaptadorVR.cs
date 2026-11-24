using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using Unity.Cinemachine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
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
    /// V8–V10: <see cref="Recentrar"/> (altura y frente), y las opciones de <see cref="OpcionesVR"/>: giro por pasos o suave,
    /// viñeta sí o no, y correr manteniendo o alternando el clic del stick (alternar se apaga solo al soltar el stick de mover).
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
        /// <summary>V10: giro suave con el stick derecho a fondo (°/s).</summary>
        public const float VelocidadGiroSuave = 90f;
        private const float ZonaMuertaGiroSuave = 0.15f;
        private const float SegundosVinetaGiroSuave = 0.1f;
        /// <summary>V9: con CORRER ALTERNAR, el stick de mover por debajo de esto (0..1) durante <see cref="SegundosApagarCorrer"/> apaga correr.</summary>
        public const float ZonaReposoCorrer = 0.2f;
        public const float SegundosApagarCorrer = 0.3f;
        private const string AccionCorrer = "Correr";
        private const string LayoutCorrerVirtual = "CorrerVirtualVR";
        private static readonly string RutaClicCorrer = EntradaVR.Izq + "/{Primary2DAxisClick}";
        private static readonly string RutaCorrerVirtual = "<" + LayoutCorrerVirtual + ">/correr";

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

        /// <summary>True mientras la viñeta de movimiento o de giro está pedida (con VIÑETA: NO nunca).</summary>
        public bool VinetaPuesta => vinetaPuesta;

        /// <summary>V9: true mientras CORRER ALTERNAR tiene correr encendido.</summary>
        public bool CorrerAlternado => correrAlternado;

        /// <summary>V9: el modo de correr que está aplicado a los bindings ahora.</summary>
        public ModoCorrerVR ModoCorrer => modoCorrer;

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
        private InputAction accionCorrer;
        private DispositivoCorrerVR dispositivoCorrer;
        private ModoCorrerVR modoCorrer = ModoCorrerVR.Mantener;
        private bool correrAlternado;
        private float segundosStickQuieto;
        private bool clicCorrerPrevio;
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
            PrepararCorrer();
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
            AgregarBinding(accion, ruta);
            return true;
        }

        private static void AgregarBinding(InputAction accion, string ruta)
        {
            foreach (InputBinding b in accion.bindings)
            {
                if (b.path == ruta)
                {
                    return;
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
                BorrarBinding(accion, ruta);
            }
            bindingsAgregados.Clear();
        }

        // Quita un binding agregado por el adaptador (grupo VR) y lo saca de la lista.
        private static void QuitarBinding(InputAction accion, string ruta)
        {
            BorrarBinding(accion, ruta);
            bindingsAgregados.RemoveAll(par => par.accion == accion && par.ruta == ruta);
        }

        private static void BorrarBinding(InputAction accion, string ruta)
        {
            if (accion == null)
            {
                return;
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

        // Caminar, correr, saltar, interactuar y recargar con los controles (lo demás lo ponen VR2–VR4).
        private static void AgregarBindingsBase()
        {
            AgregarBinding("Mover", EntradaVR.Izq + "/{Primary2DAxis}");
            AgregarBinding(AccionCorrer, RutaClicCorrer);
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
            float dt = Time.deltaTime;
            ActualizarGiro(dt);
            ActualizarCorrer(dt);
        }

        // V10: por pasos de 45° (hay que soltar el stick para el siguiente) o suave, proporcional al stick, con viñeta mientras gira.
        private void ActualizarGiro(float dt)
        {
            float x = EntradaVR.Eje(EntradaVR.StickDerecho).x;
            bool puedeGirar = Controlador == null || Controlador.ControlActivo;
            if (OpcionesVR.Giro == ModoGiroVR.Suave)
            {
                float fuerza = Mathf.Abs(x);
                if (puedeGirar && fuerza > ZonaMuertaGiroSuave)
                {
                    float proporcion = Mathf.Clamp01((fuerza - ZonaMuertaGiroSuave) / (1f - ZonaMuertaGiroSuave));
                    giroRig = Mathf.Repeat(giroRig + Mathf.Sign(x) * proporcion * VelocidadGiroSuave * dt, 360f);
                    segundosVinetaGiro = Mathf.Max(segundosVinetaGiro, SegundosVinetaGiroSuave);
                }
                // Si se cambia a pasos con el stick ladeado, no salta un paso hasta soltarlo.
                giroArmado = fuerza < RearmeGiro;
                return;
            }
            if (giroArmado && Mathf.Abs(x) > UmbralGiro && puedeGirar)
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

        // V9: la acción Correr del mapa APie siempre tiene, además, el botón de un dispositivo virtual. Con MANTENER, Correr también va
        // al clic del stick izquierdo (como antes). Con ALTERNAR ese binding se quita y el adaptador prende y apaga el botón virtual.
        private void PrepararCorrer()
        {
            accionCorrer = BuscarAccionAPie(AccionCorrer);
            if (accionCorrer == null)
            {
                Debug.LogWarning("[VR] Sin la acción Correr: CORRER ALTERNAR no se puede usar.", this);
                return;
            }
            try
            {
                InputSystem.RegisterLayout<DispositivoCorrerVR>(LayoutCorrerVirtual);
                dispositivoCorrer = InputSystem.AddDevice<DispositivoCorrerVR>(LayoutCorrerVirtual);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[VR] No se pudo crear el botón virtual de correr: " + e.Message, this);
                dispositivoCorrer = null;
                return;
            }
            AgregarBinding(accionCorrer, RutaCorrerVirtual);
            AplicarModoCorrer(OpcionesVR.Correr);
        }

        private void AplicarModoCorrer(ModoCorrerVR modo)
        {
            modoCorrer = modo;
            correrAlternado = false;
            segundosStickQuieto = 0f;
            if (accionCorrer == null)
            {
                return;
            }
            if (modo == ModoCorrerVR.Alternar)
            {
                QuitarBinding(accionCorrer, RutaClicCorrer);
            }
            else
            {
                AgregarBinding(accionCorrer, RutaClicCorrer);
            }
            FijarBotonCorrer(false);
        }

        private void ActualizarCorrer(float dt)
        {
            if (dispositivoCorrer == null || accionCorrer == null)
            {
                return;
            }
            ModoCorrerVR modo = OpcionesVR.Correr;
            if (modo != modoCorrer)
            {
                AplicarModoCorrer(modo);
            }
            if (modo != ModoCorrerVR.Alternar)
            {
                return;
            }
            bool puedeMover = Controlador == null || Controlador.ControlActivo;
            // El flanco se mide aquí (no con WasPressedThisFrame) para no perder un clic corto entre vueltas del Input System.
            bool clic = EntradaVR.Sostenido(EntradaVR.ClicStickIzquierdo);
            bool clicNuevo = clic && !clicCorrerPrevio;
            clicCorrerPrevio = clic;
            if (!puedeMover)
            {
                correrAlternado = false;
            }
            else if (clicNuevo)
            {
                correrAlternado = !correrAlternado;
                segundosStickQuieto = 0f;
            }
            if (correrAlternado)
            {
                // Se apaga solo al soltar el stick de mover (un instante, para no cortarse al pasar por el centro).
                if (EntradaVR.Eje(EntradaVR.StickIzquierdo).magnitude < ZonaReposoCorrer)
                {
                    segundosStickQuieto += dt;
                    if (segundosStickQuieto >= SegundosApagarCorrer)
                    {
                        correrAlternado = false;
                    }
                }
                else
                {
                    segundosStickQuieto = 0f;
                }
            }
            FijarBotonCorrer(correrAlternado);
        }

        // Manda el estado al botón virtual (se aplica en la siguiente vuelta del Input System, un cuadro después).
        private void FijarBotonCorrer(bool pulsado)
        {
            if (dispositivoCorrer == null || !dispositivoCorrer.added || dispositivoCorrer.Correr == null || dispositivoCorrer.Correr.isPressed == pulsado)
            {
                return;
            }
            InputSystem.QueueStateEvent(dispositivoCorrer, new EstadoCorrerVR { correr = pulsado ? 1f : 0f });
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
            ColocarRig(cabeza);

            ActualizarVineta(dt);
            segundosOcultar -= dt;
            if (segundosOcultar <= 0f)
            {
                segundosOcultar = 0.5f;
                OcultarCuerpo();
            }
        }

        // El rig se queda con su giro (no gira con el cuerpo) y pone la cabeza justo sobre la cápsula.
        private void ColocarRig(Vector3 cabeza)
        {
            Quaternion giro = Quaternion.Euler(0f, giroRig, 0f);
            Vector3 horizontal = new Vector3(cabeza.x, 0f, cabeza.z);
            Rig.SetPositionAndRotation(transform.position - giro * horizontal, giro);
        }

        /// <summary>
        /// V8: recentrar. Pone los ojos a 1.72 m (como <see cref="Recalibrar"/>) y gira el rig para que la cabeza, tal como está en el
        /// cuarto, quede mirando al frente del cuerpo del jugador (el giro del ControladorAPie). Lo llama el gesto del stick derecho.
        /// </summary>
        public void Recentrar()
        {
            if (!Armado || Camara == null || Ajuste == null)
            {
                return;
            }
            Recalibrar();
            // El yaw de la cabeza dentro del cuarto (el offset y el ajuste no giran).
            Vector3 f = Camara.transform.localRotation * Vector3.forward;
            f.y = 0f;
            float yawCabeza = f.sqrMagnitude > 1e-4f ? Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg : 0f;
            float frente = transform.eulerAngles.y;
            giroRig = Mathf.Repeat(frente - yawCabeza, 360f);
            // Un giro por pasos pendiente no se suma justo después.
            giroArmado = false;
            ColocarRig(Ajuste.parent.localPosition + Ajuste.localPosition + Camara.transform.localPosition);
            if (Controlador != null)
            {
                Controlador.Empujar(Mathf.DeltaAngle(Controlador.GiroCamara, frente), 0f);
            }
            Debug.Log("[VR] Recentrado: frente " + frente.ToString("0") + "°, cabeza en el cuarto " + yawCabeza.ToString("0") + "°.");
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
            // V10: con VIÑETA: NO no se cierra la vista al moverse ni al girar (el fundido del KO sí sale).
            bool moviendo = OpcionesVR.Vineta && (v.magnitude > VelocidadVineta || segundosVinetaGiro > 0f);
            if (moviendo != vinetaPuesta)
            {
                vinetaPuesta = moviendo;
                if (vineta != null)
                {
                    if (moviendo)
                    {
                        vineta.BeginTunnelingVignette(this);
                    }
                    else
                    {
                        vineta.EndTunnelingVignette(this);
                    }
                }
            }
            if (segundosFundido > 0f)
            {
                segundosFundido -= Time.unscaledDeltaTime; // también en pausa (timeScale 0)
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

        private bool destruyendoJugador = true;

        /// <summary>Quita el rig y deja al jugador como en el modo normal (tercera persona).</summary>
        public void Desarmar()
        {
            destruyendoJugador = false;
            Destroy(this);
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
            if (dispositivoCorrer != null)
            {
                if (dispositivoCorrer.added && InputSystem.GetDeviceById(dispositivoCorrer.deviceId) == dispositivoCorrer)
                {
                    InputSystem.RemoveDevice(dispositivoCorrer);
                }
                dispositivoCorrer = null;
            }
            foreach (var par in sombrasOriginales)
            {
                if (par.Key != null)
                {
                    par.Key.shadowCastingMode = par.Value;
                }
            }
            // Si se quita el adaptador con el jugador vivo (salir de VR), la cámara vuelve a la tercera persona.
            if (Camara != null && padreCamaraOriginal != null && !destruyendoJugador)
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

        // V9: estado del botón virtual de correr (CORRER ALTERNAR).
        [StructLayout(LayoutKind.Explicit, Size = 4)]
        private struct EstadoCorrerVR : IInputStateTypeInfo
        {
            [InputControl(name = "correr", layout = "Button", format = "FLT")]
            [FieldOffset(0)] public float correr;

            public FourCC format => new FourCC('C', 'R', 'V', 'R');
        }

        // V9: dispositivo virtual con un solo botón; la acción Correr lo escucha y el adaptador lo prende con CORRER ALTERNAR.
        [UnityEngine.Scripting.Preserve]
        [InputControlLayout(stateType = typeof(EstadoCorrerVR), displayName = "Correr VR")]
        private sealed class DispositivoCorrerVR : InputDevice
        {
            public ButtonControl Correr { get; private set; }

            [UnityEngine.Scripting.Preserve]
            public DispositivoCorrerVR()
            {
            }

            protected override void FinishSetup()
            {
                base.FinishSetup();
                Correr = GetChildControl<ButtonControl>("correr");
            }
        }
    }
}

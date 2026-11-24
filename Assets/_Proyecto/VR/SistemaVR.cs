using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR1: arranca el modo VR. Al iniciar el juego intenta encender OpenXR (Quest Link / Air Link). Si hay visor,
    /// <see cref="ModoVR.Activo"/> pasa a true y al jugador a pie se le pone <see cref="AdaptadorVR"/>; si no hay visor
    /// el juego sigue normal. Argumentos: <c>-novr</c> no intenta XR; <c>-vrsim</c> usa el simulador de XR sin visor.
    /// Mientras VR está activo: solo la cámara del visor se ve en el visor (las de cinemáticas van a la ventana del PC),
    /// la calidad baja para 72–90 fps y las acciones de los controles están encendidas.
    /// </summary>
    [DefaultExecutionOrder(-300)]
    public class SistemaVR : MonoBehaviour
    {
        /// <summary>Las pruebas lo ponen en true antes de cargar la escena: activa VR con el simulador (sin visor).</summary>
        public static bool PedirSimulador { get; set; }

        /// <summary>Las pruebas lo ponen en true para no intentar OpenXR.</summary>
        public static bool NoIntentarXR { get; set; }

        public static SistemaVR Instancia { get; private set; }

        /// <summary>La cámara que va en el visor (la del jugador, movida por la cabeza). La pone AdaptadorVR.</summary>
        public static Camera CamaraVR { get; set; }

        /// <summary>True si OpenXR arrancó con un visor.</summary>
        public bool XRIniciado { get; private set; }

        private bool calidadAplicada;
        private float shadowDistanciaPrevia;
        private int cascadasPrevias;
        private float escalaPrevia;
        private int msaaPrevio;
        private UniversalRenderPipelineAsset urp;
        private GameObject simulador;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Instancia = null;
            CamaraVR = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Arrancar()
        {
            if (Instancia != null)
            {
                return;
            }
            var objeto = new GameObject("SistemaVR");
            DontDestroyOnLoad(objeto);
            objeto.AddComponent<SistemaVR>();
        }

        private static bool TieneArgumento(string arg)
        {
            foreach (string a in Environment.GetCommandLineArgs())
            {
                if (string.Equals(a, arg, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;
            RenderPipelineManager.beginContextRendering += AntesDeDibujar;
            // VR22: pausa si el visor se quita o se desconecta (solo actúa con ModoVR.Activo).
            if (GetComponent<DesconexionVR>() == null)
            {
                gameObject.AddComponent<DesconexionVR>();
            }
            // VR30: ruta de chevrones al objetivo (solo se ve con ModoVR.Activo y OpcionesVR.Ruta).
            if (GetComponent<Ruta.RutaObjetivoVR>() == null)
            {
                gameObject.AddComponent<Ruta.RutaObjetivoVR>();
            }
        }

        // V12: justo antes de dibujar el cuadro: una cámara que se prendió este cuadro (cinemática, muerte, Doc) no llega al visor ni un cuadro.
        private static void AntesDeDibujar(ScriptableRenderContext contexto, System.Collections.Generic.List<Camera> camaras)
        {
            if (ModoVR.Activo)
            {
                GuardarCamaras();
            }
        }

        private IEnumerator Start()
        {
            if (TieneArgumento("-novr"))
            {
                Debug.Log("[VR] -novr: modo normal.");
                yield break;
            }
            bool sim = PedirSimulador || TieneArgumento("-vrsim");
            if (!sim && !NoIntentarXR && !Application.isBatchMode)
            {
                yield return IniciarXR();
            }
            if (XRIniciado)
            {
                Debug.Log("[VR] Visor encontrado (" + XRSettings.loadedDeviceName + "): modo VR.");
                ModoVR.Fijar(true);
            }
            else if (sim)
            {
                CrearSimulador();
                Debug.Log("[VR] Simulador de XR: modo VR sin visor.");
                ModoVR.Fijar(true, true);
            }
            else
            {
                Debug.Log("[VR] Sin visor: modo normal.");
            }
            if (ModoVR.Activo)
            {
                EntradaVR.Encender();
                AplicarCalidad();
            }
        }

        private IEnumerator IniciarXR()
        {
            XRManagerSettings manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            if (manager == null)
            {
                Debug.LogWarning("[VR] No hay XR Management configurado (Mecha Cyberpunk/VR/Configurar VR).");
                yield break;
            }
            if (manager.activeLoader == null)
            {
                yield return manager.InitializeLoader();
            }
            if (manager.activeLoader == null)
            {
                Debug.Log("[VR] OpenXR no arrancó (¿Quest Link apagado?).");
                yield break;
            }
            manager.StartSubsystems();
            // El visor tarda unos cuadros en dar pose; con el loader activo ya cuenta como VR.
            XRIniciado = true;
        }

        private void CrearSimulador()
        {
            RecursosVR recursos = RecursosVR.Cargar();
            if (recursos != null && recursos.simuladorXR != null && simulador == null)
            {
                simulador = Instantiate(recursos.simuladorXR);
                simulador.name = "SimuladorXR";
                DontDestroyOnLoad(simulador);
            }
        }

        private void Update()
        {
            if (!ModoVR.Activo)
            {
                return;
            }
            AsegurarAdaptador();
            GuardarCamaras();
            segundosRevision -= Time.unscaledDeltaTime;
            if (segundosRevision <= 0f)
            {
                segundosRevision = 2f;
                QuitarPostprocesoQueMarea();
                ApagarReflejos();
            }
        }

        private float segundosRevision;
        private readonly System.Collections.Generic.HashSet<Volume> volumenesRevisados = new System.Collections.Generic.HashSet<Volume>();

        // En VR marean o pesan mucho por ojo: desenfoque, motion blur, aberración, distorsión de lente, grano y Panini. Bloom y color se quedan (el neón).
        private void QuitarPostprocesoQueMarea()
        {
            foreach (Volume v in FindObjectsByType<Volume>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (v == null || !volumenesRevisados.Add(v) || v.sharedProfile == null)
                {
                    continue;
                }
                VolumeProfile perfil = v.profile; // copia propia: no cambia el asset
                Apagar<DepthOfField>(perfil);
                Apagar<MotionBlur>(perfil);
                Apagar<ChromaticAberration>(perfil);
                Apagar<LensDistortion>(perfil);
                Apagar<FilmGrain>(perfil);
                Apagar<PaniniProjection>(perfil);
            }
        }

        private static void Apagar<T>(VolumeProfile perfil) where T : VolumeComponent
        {
            if (perfil.TryGet(out T c) && c.active)
            {
                c.active = false;
            }
        }

        // Los reflejos planos (espejos, charcos) renderizan otra vez la escena: en VR cuestan el doble y se ven mal por ojo.
        private static void ApagarReflejos()
        {
            foreach (var e in FindObjectsByType<MechaCyberpunk.Mundo.Ambiente.EspejoReflejo>(FindObjectsSortMode.None))
            {
                if (e.enabled)
                {
                    e.enabled = false;
                }
            }
            foreach (var r in FindObjectsByType<MechaCyberpunk.Mundo.Ambiente.ReflejoCharcos>(FindObjectsSortMode.None))
            {
                if (r.enabled)
                {
                    r.enabled = false;
                }
            }
        }

        // El jugador a pie de la escena actual lleva el rig VR.
        private static void AsegurarAdaptador()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador is JugadorAPie aPie && aPie != null && aPie.GetComponent<AdaptadorVR>() == null)
            {
                aPie.gameObject.AddComponent<AdaptadorVR>();
            }
        }

        // Solo la cámara del visor va al visor: las de cinemáticas o de menús se ven en la ventana del PC (no mueven la cabeza).
        private static void GuardarCamaras()
        {
            Camera vr = CamaraVR;
            if (vr == null)
            {
                // La cámara del visor se perdió (el rig de menús se quitó después de que el jugador se armara, o cambió la escena):
                // si hay jugador con rig, manda la suya, para que el visor nunca se quede sin cámara (en negro).
                AdaptadorVR a = AdaptadorVR.Actual;
                if (a != null && a.Armado && a.Camara != null)
                {
                    vr = a.Camara;
                    CamaraVR = vr;
                }
            }
            foreach (Camera c in Camera.allCameras)
            {
                if (c == vr || c.targetTexture != null)
                {
                    continue;
                }
                // V12: en URP stereoTargetEye no hace nada (y avisaba cada cuadro): la que manda es allowXRRendering.
                // Sin esto la cámara de la escena (el recorrido de Menu_Calles, cinemáticas) se dibujaba en el visor y movía la vista.
                PermitirXR(c, false);
            }
            if (vr != null)
            {
                PermitirXR(vr, true);
                if (!vr.enabled && vr.gameObject.activeInHierarchy)
                {
                    vr.enabled = true;
                }
            }
        }

        /// <summary>V12: true si la cámara se dibuja en el visor (URP: allowXRRendering y sin targetTexture).</summary>
        public static bool VaAlVisor(Camera c)
        {
            if (c == null || c.targetTexture != null)
            {
                return false;
            }
            UniversalAdditionalCameraData datos = c.GetUniversalAdditionalCameraData();
            return datos == null || datos.allowXRRendering;
        }

        /// <summary>V12: si la cámara se dibuja en el visor (URP). Solo la cabeza del jugador o del rig de menús lo hace.</summary>
        public static void PermitirXR(Camera c, bool permitir)
        {
            if (c == null)
            {
                return;
            }
            UniversalAdditionalCameraData datos = c.GetUniversalAdditionalCameraData();
            if (datos != null && datos.allowXRRendering != permitir)
            {
                datos.allowXRRendering = permitir;
            }
        }

        // VR1: calidad para 72–90 fps en la RTX 5070 Laptop: menos sombra, sin MSAA alto, escala 1 del visor. Se deshace al salir.
        private void AplicarCalidad()
        {
            if (calidadAplicada)
            {
                return;
            }
            urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                calidadAplicada = true;
                shadowDistanciaPrevia = urp.shadowDistance;
                cascadasPrevias = urp.shadowCascadeCount;
                escalaPrevia = urp.renderScale;
                msaaPrevio = urp.msaaSampleCount;
                urp.shadowDistance = Mathf.Min(urp.shadowDistance, 30f);
                urp.shadowCascadeCount = 1;
                urp.renderScale = 1f;
                urp.msaaSampleCount = 2;
            }
            XRSettings.eyeTextureResolutionScale = 1f;
            QualitySettings.vSyncCount = 0;
            QualitySettings.lodBias = Mathf.Min(QualitySettings.lodBias, 1f);
        }

        private void DeshacerCalidad()
        {
            if (!calidadAplicada || urp == null)
            {
                return;
            }
            calidadAplicada = false;
            urp.shadowDistance = shadowDistanciaPrevia;
            urp.shadowCascadeCount = cascadasPrevias;
            urp.renderScale = escalaPrevia;
            urp.msaaSampleCount = msaaPrevio;
        }

        private void OnDestroy()
        {
            if (Instancia != this)
            {
                return;
            }
            RenderPipelineManager.beginContextRendering -= AntesDeDibujar;
            DeshacerCalidad();
            EntradaVR.Apagar();
            XRManagerSettings manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            if (XRIniciado && manager != null && manager.activeLoader != null)
            {
                manager.StopSubsystems();
                manager.DeinitializeLoader();
            }
            ModoVR.Fijar(false);
            Instancia = null;
        }
    }
}

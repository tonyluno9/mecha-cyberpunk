using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.UI.MenuPausa;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR22: si el visor se quita, la sesión OpenXR pierde el foco o se cae (Link desconectado), el juego se pausa (tiempo a 0, audio en pausa,
    /// control del jugador congelado, <see cref="Pausa.Activa"/>) y sale el aviso "VISOR DESCONECTADO" en la ventana del PC y en el visor.
    /// Cuando el visor vuelve: "pulsa A (o gatillo) para seguir": recentra (<see cref="AdaptadorVR.Recentrar"/>), reenciende <see cref="EntradaVR"/>
    /// y quita la pausa. Si OpenXR quiere cerrar el juego porque la sesión terminó, se cancela (el juego sigue en la ventana del PC, pausado) y al volver
    /// el foco (o con ENTER en el PC) intenta una vez reiniciar XR. Solo actúa con <see cref="ModoVR.Activo"/>; la detección real solo con un visor
    /// de verdad (<see cref="SistemaVR.XRIniciado"/>); las pruebas fuerzan el estado con <see cref="ForzarVisor"/>.
    /// Lo agrega <see cref="SistemaVR"/> en su mismo objeto (DontDestroyOnLoad).
    /// </summary>
    [DefaultExecutionOrder(1000)] // tarde: el A que quita la pausa no lo usa también el juego en el mismo cuadro (los demás ya leyeron Pausa.Activa)
    public class DesconexionVR : MonoBehaviour
    {
        public const string TextoDesconectado = "VISOR DESCONECTADO";
        public const string TextoPonte = "Ponte el visor y pulsa A para seguir";
        public const string TextoDeVuelta = "VISOR DE VUELTA";
        public const string TextoPulsa = "Pulsa A o el gatillo para seguir";
        public const string TextoSesion = "Se cerró la conexión con el visor. Conecta Quest Link y vuelve a esta ventana (o pulsa ENTER) para reintentar.";

        /// <summary>Segundos seguidos sin visor antes de pausar (un parpadeo de la sesión no pausa).</summary>
        public const float SegundosParaPerder = 0.75f;

        /// <summary>Segundos seguidos con el visor bien antes de ofrecer seguir.</summary>
        public const float SegundosParaVolver = 0.3f;

        /// <summary>Tras volver el visor, un botón pulsado antes de esto no cuenta (el que se puso el visor con el dedo en el gatillo).</summary>
        public const float SegundosAntesDelBoton = 0.25f;

        public static DesconexionVR Actual { get; private set; }

        /// <summary>True mientras el juego está pausado por el visor.</summary>
        public bool Pausado { get; private set; }

        /// <summary>True si el visor ya volvió y se espera A o gatillo.</summary>
        public bool EsperandoBoton { get; private set; }

        /// <summary>True si la sesión XR terminó (el loader se apagó) mientras estaba pausado.</summary>
        public bool SesionTerminada { get; private set; }

        /// <summary>Veces que se recentró al seguir (pruebas).</summary>
        public int Recentrados { get; private set; }

        /// <summary>Intentos de reiniciar XR (pruebas y registro).</summary>
        public int Reintentos { get; private set; }

        /// <summary>El texto del aviso que se ve ahora (vacío si no hay).</summary>
        public string TextoAviso => textoPC != null && avisoPC != null && avisoPC.activeSelf ? textoPC.text : string.Empty;

        /// <summary>Se dispara con true al pausar y false al seguir.</summary>
        public event Action<bool> CambioPausa;

        private bool? forzado;
        private bool vistoBien;
        private float segundosMal;
        private float segundosBien;
        private float desdeVuelta;
        private bool audioPrevio;
        private ControladorAPie congelado;
        private bool reintentando;
        private bool reintentoPorFoco;
        private bool nativoRoto;

        private GameObject avisoPC;
        private Text textoPC;
        private GameObject avisoVisor;
        private Text textoVisor;

        /// <summary>Lo crea si no existe (pruebas sin <see cref="SistemaVR"/>).</summary>
        public static DesconexionVR Asegurar()
        {
            if (Actual != null)
            {
                return Actual;
            }
            GameObject g = SistemaVR.Instancia != null ? SistemaVR.Instancia.gameObject : new GameObject("DesconexionVR");
            if (SistemaVR.Instancia == null)
            {
                DontDestroyOnLoad(g);
            }
            DesconexionVR d = g.GetComponent<DesconexionVR>();
            return d != null ? d : g.AddComponent<DesconexionVR>();
        }

        /// <summary>
        /// Pruebas: true = visor puesto, false = visor quitado/desconectado, null = lo que diga el visor real. Con un valor forzado no hay espera
        /// (<see cref="SegundosParaPerder"/>).
        /// </summary>
        public void ForzarVisor(bool? puesto) => forzado = puesto;

        /// <summary>Pruebas: quita la pausa y el estado forzado sin recentrar.</summary>
        public void Restablecer()
        {
            forzado = null;
            SesionTerminada = false;
            if (Pausado)
            {
                Seguir(false);
            }
        }

        private void Awake()
        {
            if (Actual != null && Actual != this)
            {
                Destroy(this);
                return;
            }
            Actual = this;
            OpenXRRuntime.wantsToQuit += NoCerrarElJuego;
            Application.focusChanged += AlCambiarFoco;
            ModoVR.Cambio += AlCambiarModo;
        }

        private void OnDestroy()
        {
            if (Actual != this)
            {
                return;
            }
            OpenXRRuntime.wantsToQuit -= NoCerrarElJuego;
            Application.focusChanged -= AlCambiarFoco;
            ModoVR.Cambio -= AlCambiarModo;
            if (Pausado)
            {
                Seguir(false);
            }
            Actual = null;
        }

        // OpenXR cierra el juego cuando la sesión termina (Link desconectado, salir desde el menú del visor). En VR mejor seguir en la ventana del PC.
        private bool NoCerrarElJuego()
        {
            if (!ModoVR.Activo)
            {
                return true;
            }
            Debug.LogWarning("[VR] OpenXR terminó la sesión y quería cerrar el juego: se queda abierto en pausa (VR22).");
            SesionTerminada = true;
            Pausar();
            return false;
        }

        private void AlCambiarModo(bool activo)
        {
            if (!activo && Pausado)
            {
                Seguir(false);
            }
        }

        private void AlCambiarFoco(bool foco)
        {
            if (!ModoVR.Activo)
            {
                return;
            }
            if (!foco)
            {
                reintentoPorFoco = false;
                return;
            }
            // Las acciones de los controles pueden quedar apagadas tras perder el foco: se vuelven a encender (no hace nada si ya lo están).
            EntradaVR.Encender();
            if (SesionTerminada && !reintentoPorFoco)
            {
                reintentoPorFoco = true;
                ReintentarXR();
            }
        }

        private void Update()
        {
            if (!ModoVR.Activo)
            {
                return;
            }
            float dt = Time.unscaledDeltaTime;
            bool? puesto = forzado ?? LeerVisorReal();
            if (puesto.HasValue)
            {
                Vigilar(puesto.Value, dt, forzado.HasValue);
            }
            if (!Pausado)
            {
                return;
            }
            // Otra pantalla (la pausa del PC) pudo soltar la pausa o el control mientras el visor no está.
            Pausa.Fijar(true);
            if (!ControlTiempo.Pide(this))
            {
                ControlTiempo.Pedir(this, 0f);
            }
            CongelarControl();
            Keyboard teclado = Keyboard.current;
            bool enter = teclado != null && (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame);
            if (SesionTerminada && enter)
            {
                ReintentarXR();
            }
            if (EsperandoBoton)
            {
                desdeVuelta += dt;
                if (desdeVuelta >= SegundosAntesDelBoton && (EntradaVR.Pulso(EntradaVR.BotonA) || EntradaVR.Pulso(EntradaVR.GatilloDerecho) || enter))
                {
                    Seguir(true);
                    return;
                }
            }
            MantenerAvisoEnLaCara();
        }

        private void Vigilar(bool puesto, float dt, bool inmediato)
        {
            if (puesto)
            {
                vistoBien = true;
                segundosMal = 0f;
                segundosBien += dt;
                if (Pausado && !EsperandoBoton && (inmediato || segundosBien >= SegundosParaVolver))
                {
                    SesionTerminada = false;
                    EsperandoBoton = true;
                    desdeVuelta = 0f;
                    EntradaVR.Encender();
                    Debug.Log("[VR] Visor de vuelta: pulsa A para seguir.");
                    MostrarAviso();
                }
                return;
            }
            segundosBien = 0f;
            // Al arrancar la sesión tarda en tener foco: sin haber visto el visor bien una vez no se pausa (salvo que la sesión ya terminó).
            if (!vistoBien && !SesionTerminada && !inmediato)
            {
                return;
            }
            segundosMal += dt;
            if (EsperandoBoton)
            {
                // Se lo volvió a quitar antes de pulsar A.
                EsperandoBoton = false;
                MostrarAviso();
            }
            if (!Pausado && (inmediato || segundosMal >= SegundosParaPerder))
            {
                Pausar();
            }
        }

        // null = no hay visor real que vigilar (simulador, pruebas, sin XR).
        private bool? LeerVisorReal()
        {
            SistemaVR sistema = SistemaVR.Instancia;
            if (sistema == null || !sistema.XRIniciado)
            {
                return null;
            }
            XRManagerSettings manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            XRLoader loader = manager != null ? manager.activeLoader : null;
            if (loader == null)
            {
                if (!reintentando)
                {
                    SesionTerminada = true;
                }
                return false;
            }
            XRDisplaySubsystem pantalla = loader.GetLoadedSubsystem<XRDisplaySubsystem>();
            if (pantalla == null || !pantalla.running)
            {
                return false;
            }
            if (nativoRoto)
            {
                return true;
            }
            try
            {
                // Sin foco: visor quitado (sensor de proximidad), menú del sistema o Link en pausa. IsUserPresent da true si el runtime no lo sabe.
                return OpenXRUtility.IsSessionFocused && OpenXRUtility.IsUserPresent;
            }
            catch (Exception e)
            {
                nativoRoto = true; // una sola vez: sin la librería nativa solo se vigila la pantalla
                Debug.LogWarning("[VR] No se pudo leer el estado de la sesión OpenXR: " + e.Message);
                return true;
            }
        }

        private void Pausar()
        {
            if (Pausado)
            {
                MostrarAviso();
                return;
            }
            Pausado = true;
            EsperandoBoton = false;
            audioPrevio = AudioListener.pause;
            ControlTiempo.Pedir(this, 0f);
            AudioListener.pause = true;
            Pausa.Fijar(true);
            CongelarControl();
            Debug.Log("[VR] Visor desconectado o quitado: juego en pausa (VR22).");
            MostrarAviso();
            CambioPausa?.Invoke(true);
        }

        private void Seguir(bool recentrar)
        {
            Pausado = false;
            EsperandoBoton = false;
            segundosMal = 0f;
            ControlTiempo.Soltar(this);
            AudioListener.pause = audioPrevio;
            PantallaPausa pausaPC = FindFirstObjectByType<PantallaPausa>();
            if (pausaPC == null || !pausaPC.Abierta)
            {
                Pausa.Fijar(false);
            }
            if (congelado != null)
            {
                congelado.ActivarControl(true);
                congelado = null;
            }
            OcultarAviso();
            if (recentrar)
            {
                EntradaVR.Encender();
                AdaptadorVR adaptador = AdaptadorVR.Actual;
                if (adaptador != null)
                {
                    adaptador.Recentrar();
                }
                Recentrados++;
                Debug.Log("[VR] Visor de vuelta: recentrado y el juego sigue (VR22).");
            }
            CambioPausa?.Invoke(false);
        }

        private void CongelarControl()
        {
            if (congelado != null)
            {
                return;
            }
            IJugador jugador = Registro<IJugador>.Actual;
            ControladorAPie c = jugador != null && jugador.Raiz != null ? jugador.Raiz.GetComponentInChildren<ControladorAPie>() : null;
            if (c != null && c.ControlActivo)
            {
                c.ActivarControl(false);
                congelado = c;
            }
        }

        // ---------- Reintentar XR ----------

        private void ReintentarXR()
        {
            if (reintentando || !isActiveAndEnabled)
            {
                return;
            }
            XRManagerSettings manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            if (manager == null || manager.activeLoader != null)
            {
                return;
            }
            StartCoroutine(Reiniciar(manager));
        }

        private IEnumerator Reiniciar(XRManagerSettings manager)
        {
            reintentando = true;
            Reintentos++;
            Debug.Log("[VR] Intento de volver a encender XR (" + Reintentos + ").");
            // Un momento por si OpenXR ya se está reiniciando solo (sesión perdida).
            float hasta = Time.realtimeSinceStartup + 1f;
            while (Time.realtimeSinceStartup < hasta)
            {
                yield return null;
            }
            if (manager.activeLoader == null)
            {
                yield return manager.InitializeLoader();
                if (manager.activeLoader != null)
                {
                    manager.StartSubsystems();
                }
            }
            reintentando = false;
            if (manager.activeLoader != null)
            {
                Debug.Log("[VR] XR encendido otra vez: ponte el visor y pulsa A.");
                SesionTerminada = false;
                EntradaVR.Encender();
            }
            else
            {
                Debug.Log("[VR] XR no volvió: el juego sigue en pausa en la ventana del PC.");
            }
            MostrarAviso();
        }

        // ---------- Aviso ----------

        private string Texto()
        {
            if (EsperandoBoton)
            {
                return TextoDeVuelta + "\n" + TextoPulsa;
            }
            return SesionTerminada ? TextoDesconectado + "\n" + TextoSesion : TextoDesconectado + "\n" + TextoPonte;
        }

        private void MostrarAviso()
        {
            ArmarAviso();
            string t = Texto();
            textoPC.text = t;
            textoVisor.text = t;
            avisoPC.SetActive(true);
            avisoVisor.SetActive(true);
            MantenerAvisoEnLaCara();
        }

        private void OcultarAviso()
        {
            if (avisoPC != null)
            {
                avisoPC.SetActive(false);
            }
            if (avisoVisor != null)
            {
                avisoVisor.SetActive(false);
            }
        }

        // El aviso del visor cuelga de la cámara del visor (la que haya ahora), 1.2 m al frente.
        private void MantenerAvisoEnLaCara()
        {
            if (avisoVisor == null)
            {
                if (Pausado && avisoPC != null)
                {
                    MostrarAviso(); // la cámara (y el aviso que colgaba de ella) se destruyó: se vuelve a armar
                }
                return;
            }
            Camera cam = SistemaVR.CamaraVR;
            Transform padre = cam != null ? cam.transform : null;
            if (padre == null)
            {
                return;
            }
            if (avisoVisor.transform.parent != padre)
            {
                avisoVisor.transform.SetParent(padre, false);
                avisoVisor.transform.localPosition = new Vector3(0f, 0f, 1.2f);
                avisoVisor.transform.localRotation = Quaternion.identity;
                avisoVisor.transform.localScale = Vector3.one * 0.0012f;
            }
        }

        private void ArmarAviso()
        {
            Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (avisoPC == null)
            {
                avisoPC = new GameObject("AvisoDesconexionVR_PC");
                avisoPC.transform.SetParent(transform, false);
                Canvas lienzo = avisoPC.AddComponent<Canvas>();
                lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
                lienzo.sortingOrder = 500;
                CanvasScaler escala = avisoPC.AddComponent<CanvasScaler>();
                escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                escala.referenceResolution = new Vector2(1920f, 1080f);
                textoPC = Panel(avisoPC.transform, fuente, new Vector2(1400f, 360f), 56);
                ((RectTransform)textoPC.transform.parent).anchoredPosition = new Vector2(0f, 330f); // arriba: al centro va el espejo del visor
                avisoPC.SetActive(false);
            }
            if (avisoVisor == null)
            {
                // Cuelga de la cámara del visor: si la cámara se destruye (cambio de escena) se vuelve a armar aquí.
                avisoVisor = new GameObject("AvisoDesconexionVR_Visor", typeof(RectTransform));
                avisoVisor.transform.SetParent(transform, false);
                Canvas lienzoVisor = avisoVisor.AddComponent<Canvas>();
                lienzoVisor.renderMode = RenderMode.WorldSpace;
                lienzoVisor.sortingOrder = 500;
                ((RectTransform)avisoVisor.transform).sizeDelta = new Vector2(1000f, 400f);
                textoVisor = Panel(avisoVisor.transform, fuente, new Vector2(1000f, 400f), 60);
                avisoVisor.SetActive(false);
            }
        }

        private static Text Panel(Transform padre, Font fuente, Vector2 tamano, int tamanoLetra)
        {
            var fondo = new GameObject("Fondo", typeof(RectTransform), typeof(Image));
            fondo.transform.SetParent(padre, false);
            RectTransform rf = (RectTransform)fondo.transform;
            rf.sizeDelta = tamano;
            Image img = fondo.GetComponent<Image>();
            img.color = new Color(0.02f, 0.02f, 0.05f, 0.88f);
            img.raycastTarget = false;

            var texto = new GameObject("Texto", typeof(RectTransform), typeof(Text));
            texto.transform.SetParent(fondo.transform, false);
            RectTransform rt = (RectTransform)texto.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(30f, 20f);
            rt.offsetMax = new Vector2(-30f, -20f);
            Text t = texto.GetComponent<Text>();
            t.font = fuente;
            t.fontSize = tamanoLetra;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.2f, 1f, 0.95f);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }
    }
}

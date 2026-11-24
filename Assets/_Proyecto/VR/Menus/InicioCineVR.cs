using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Inicio;
using MechaCyberpunk.UI.Intro;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.VR.Menus
{
    /// <summary>
    /// V12: Inicio en VR sin mareo. Va en la raíz del rig de menús (<see cref="RigMenusVR"/>) y vive lo que vive el rig.
    /// <list type="bullet">
    /// <item>La cámara de la escena (la del recorrido de Menu_Calles, <see cref="RecorridoCallesMenu"/>, o <c>Camera.main</c> de Inicio) se sigue
    /// animando, pero se dibuja en una textura que se ve en la pantalla de cine (<see cref="PantallaCineVR"/>). Al quitarse se le devuelve su
    /// targetTexture.</item>
    /// <item>El video del logo (<see cref="IntroInicio"/>) se ve en la misma pantalla, antes que las calles; B o el gatillo derecho lo saltan.
    /// Mientras suena, el menú (CanvasInicio) está apagado; al acabar, la pantalla y el menú se ponen frente a donde mira la cabeza.</item>
    /// <item>Los adornos a pantalla completa del menú (fotos de las calles, sombra, viñetas, velo negro) no se ven en VR: taparían la pantalla.</item>
    /// <item>Si la cabeza empieza a dar pose (el visor arrancó después del rig) o mira lejos de la pantalla un rato, pantalla y menú se recolocan.</item>
    /// </list>
    /// </summary>
    public class InicioCineVR : MonoBehaviour
    {
        /// <summary>Tamaño de la textura de la cámara de la escena (px): la pantalla mide ~64° de la vista, unos 1300 px del visor.</summary>
        public const int AnchoTextura = 1280;
        public const int AltoTextura = 720;
        /// <summary>El video del logo gana a las calles en la pantalla.</summary>
        public const int PrioridadIntro = 10;
        private const int BitsProfundidad = 24;
        private const float SegundosBuscar = 0.25f;
        // Lo que tiene que moverse la cabeza (m) desde que se armó el rig para decir que el visor ya da pose.
        private const float UmbralPoseReal = 0.01f;
        // Hijos de CanvasInicio que en VR se apagan: cubren toda la pantalla y taparían la pantalla de cine.
        private static readonly string[] Adornos = { "FondoCalles", "SombraMenu", "VinetaArriba", "VinetaAbajo", "Entrada" };

        /// <summary>El del rig de menús (null sin rig).</summary>
        public static InicioCineVR Actual { get; private set; }

        private readonly List<Graphic> apagados = new List<Graphic>();
        private RenderTexture textura;
        private Camera camaraEscena;
        private RenderTexture texturaOriginal;
        private MenuInicio menu;
        private IntroInicio intro;
        private bool menuApagado;
        private bool introPrevia;
        private Camera cabeza;
        private Vector3 poseInicial;
        private bool poseReal;
        private float fuera;
        private float buscar;

        /// <summary>La textura donde se dibuja la cámara de la escena.</summary>
        public RenderTexture Textura => textura;
        /// <summary>La cámara de la escena que va a la pantalla (null si no hay).</summary>
        public Camera CamaraEscena => camaraEscena;
        /// <summary>True si el video del logo está en la pantalla.</summary>
        public bool IntroEnPantalla => intro != null && PantallaCineVR.Tiene(intro);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => Actual = null;

        private void Awake()
        {
            textura = new RenderTexture(AnchoTextura, AltoTextura, BitsProfundidad) { name = "InicioCineVR", antiAliasing = 1 };
            textura.Create();
        }

        private void OnEnable()
        {
            Actual = this;
            buscar = 0f;
        }

        private void OnDisable()
        {
            SoltarCamara();
            PantallaCineVR.Ocultar(this);
            if (intro != null)
            {
                PantallaCineVR.Ocultar(intro);
            }
            RestaurarMenu();
            intro = null;
            menu = null;
            introPrevia = false;
            if (Actual == this)
            {
                Actual = null;
            }
        }

        private void OnDestroy()
        {
            if (textura != null)
            {
                textura.Release();
                Destroy(textura);
                textura = null;
            }
        }

        private void Update()
        {
            if (!ModoVR.Activo)
            {
                return;
            }
            float dt = Time.unscaledDeltaTime;
            buscar -= dt;
            if (buscar <= 0f)
            {
                buscar = SegundosBuscar;
                BuscarMenu();
            }
            ElegirCamara();
            ActualizarIntro();
            ActualizarMenu();
            VigilarCabeza(dt);
        }

        // ---- Cámara de la escena → pantalla ----

        private void ElegirCamara()
        {
            Camera deseada = CamaraDeEscena();
            if (deseada != camaraEscena)
            {
                SoltarCamara();
                if (deseada != null)
                {
                    camaraEscena = deseada;
                    texturaOriginal = deseada.targetTexture;
                    Debug.Log("[VR] La cámara " + deseada.name + " va a la pantalla de cine.");
                }
            }
            if (camaraEscena == null)
            {
                if (PantallaCineVR.Tiene(this))
                {
                    PantallaCineVR.Ocultar(this);
                }
                return;
            }
            if (camaraEscena.targetTexture != textura)
            {
                camaraEscena.targetTexture = textura;
            }
            SistemaVR.PermitirXR(camaraEscena, false);
            if (!PantallaCineVR.Tiene(this))
            {
                PantallaCineVR.Mostrar(this, textura, (float)AnchoTextura / AltoTextura);
            }
        }

        // La del recorrido de Menu_Calles si está; si no, la principal de la escena (nunca la cabeza ni una que ya dibuja en otra textura).
        private Camera CamaraDeEscena()
        {
            RecorridoCallesMenu recorrido = RecorridoCallesMenu.Actual;
            if (recorrido != null && recorrido.isActiveAndEnabled && Usable(recorrido.Camara))
            {
                return recorrido.Camara;
            }
            Camera principal = Camera.main;
            return Usable(principal) ? principal : null;
        }

        private bool Usable(Camera c)
        {
            return c != null && c.isActiveAndEnabled && c != SistemaVR.CamaraVR
                && (c.targetTexture == null || c.targetTexture == textura);
        }

        private void SoltarCamara()
        {
            if (camaraEscena != null && camaraEscena.targetTexture == textura)
            {
                camaraEscena.targetTexture = texturaOriginal;
                camaraEscena.ResetAspect();
            }
            camaraEscena = null;
            texturaOriginal = null;
        }

        // ---- Intro y menú ----

        private void BuscarMenu()
        {
            if (menu != null)
            {
                return;
            }
            menu = FindFirstObjectByType<MenuInicio>();
            if (menu == null)
            {
                return;
            }
            intro = menu.GetComponent<IntroInicio>();
            ApagarAdornos();
        }

        private void ActualizarIntro()
        {
            bool enVideo = intro != null && intro.Fase == FaseIntro.Video;
            RenderTexture video = enVideo ? intro.Textura : null;
            if (video != null)
            {
                if (!PantallaCineVR.Tiene(intro))
                {
                    PantallaCineVR.Mostrar(intro, video, video.height > 0 ? (float)video.width / video.height : 0f, PrioridadIntro);
                }
            }
            else if (intro != null && PantallaCineVR.Tiene(intro))
            {
                PantallaCineVR.Ocultar(intro);
            }
            if (enVideo && (EntradaVR.Pulso(EntradaVR.BotonB) || EntradaVR.Pulso(EntradaVR.GatilloDerecho)))
            {
                // La misma vía que Enter, clic o Escape en PC.
                intro.Saltar();
                Debug.Log("[VR] Intro saltada con el control.");
            }
            if (introPrevia && !enVideo)
            {
                // Acabó el logo: la pantalla (ahora con las calles) y el menú, frente a donde mira.
                Recentrar();
            }
            introPrevia = enVideo;
        }

        // Mientras suena la intro el menú no se ve (en PC lo tapa el negro de la intro, que en VR va oculto).
        private void ActualizarMenu()
        {
            if (menu == null || menu.Canvas == null)
            {
                return;
            }
            Canvas lienzo = menu.Canvas;
            if (menu.IntroEnCurso)
            {
                if (lienzo.enabled)
                {
                    lienzo.enabled = false;
                    menuApagado = true;
                }
            }
            else if (menuApagado)
            {
                lienzo.enabled = true;
                menuApagado = false;
            }
        }

        private void ApagarAdornos()
        {
            if (menu == null || menu.Canvas == null)
            {
                return;
            }
            Transform raiz = menu.Canvas.transform;
            foreach (string nombre in Adornos)
            {
                Transform t = raiz.Find(nombre);
                if (t == null)
                {
                    continue;
                }
                foreach (Graphic g in t.GetComponentsInChildren<Graphic>(true))
                {
                    if (g.enabled)
                    {
                        g.enabled = false;
                        apagados.Add(g);
                    }
                }
            }
        }

        private void RestaurarMenu()
        {
            foreach (Graphic g in apagados)
            {
                if (g != null)
                {
                    g.enabled = true;
                }
            }
            apagados.Clear();
            if (menuApagado && menu != null && menu.Canvas != null)
            {
                menu.Canvas.enabled = true;
            }
            menuApagado = false;
        }

        // ---- Cabeza ----

        private void VigilarCabeza(float dt)
        {
            Camera c = SistemaVR.CamaraVR;
            if (c == null)
            {
                return;
            }
            Transform t = c.transform;
            if (c != cabeza)
            {
                cabeza = c;
                poseInicial = t.localPosition;
                poseReal = false;
                fuera = 0f;
            }
            // El visor empezó a dar pose después de que se colocaron la pantalla y el menú: se ponen frente a la cabeza de verdad.
            if (!poseReal && t.localPosition.sqrMagnitude > UmbralPoseReal * UmbralPoseReal
                && (t.localPosition - poseInicial).sqrMagnitude > UmbralPoseReal * UmbralPoseReal)
            {
                poseReal = true;
                Recentrar();
                return;
            }
            if (!PantallaCineVR.Visible)
            {
                fuera = 0f;
                return;
            }
            Vector3 frente = ReglaLienzosVR.FrentePlano(t.forward, t.up);
            float angulo = ReglaLienzosVR.AnguloHacia(t.position, frente, PantallaCineVR.Centro);
            fuera = angulo > ReglaLienzosVR.AnguloFueraDeVista ? fuera + dt : 0f;
            if (fuera > ReglaLienzosVR.SegundosFueraDeVista)
            {
                Recentrar();
            }
        }

        private void Recentrar()
        {
            fuera = 0f;
            PantallaCineVR.Recolocar();
            MenusVR menus = MenusVR.Actual;
            if (menus != null)
            {
                menus.ColocarAncla();
            }
        }
    }
}

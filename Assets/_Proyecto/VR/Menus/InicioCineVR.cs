using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Inicio;
using MechaCyberpunk.UI.Intro;
using MechaCyberpunk.UI.Titulo;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace MechaCyberpunk.VR.Menus
{
    /// <summary>
    /// V12 / VR15: Inicio en VR en UNA sola pantalla grande curva (<see cref="PantallaCineVR"/>), compuesta como en PC.
    /// Vive en DontDestroyOnLoad (lo arma <see cref="RigMenusVR"/>) y está activa mientras hay rig de menús (escenas sin jugador a pie) y,
    /// al cargar la partida, hasta que la pantalla de carga se quita (aunque ya mande el rig del jugador).
    /// <list type="bullet">
    /// <item>Fondo: la cámara de la escena (la del recorrido de Menu_Calles, <see cref="RecorridoCallesMenu"/>, o <c>Camera.main</c>) se sigue
    /// animando, pero dibuja en <see cref="Textura"/>; sin cámara de escena (al cambiar de escena) el fondo es negro. Al soltarla se le
    /// devuelve su targetTexture.</item>
    /// <item>Encima: todos los Canvas de pantalla de Inicio (menú, video del logo, opciones, creación de personaje, carga...) pasan a
    /// Screen Space Camera sobre <see cref="CamaraUI"/>, que los dibuja en <see cref="TexturaUI"/> (fondo transparente) con su orden de PC.
    /// La pantalla grande compone esa capa sobre las calles: el menú se ve sobre la ciudad igual que en PC, el logo lo tapa todo mientras
    /// suena y la carga también. Ninguno va como panel en el mundo (MenusVR no los toca mientras son de aquí, ver <see cref="MenusVR.Reclamado"/>).
    /// Al soltarlos vuelven a como estaban (y MenusVR los vuelve a tomar).</item>
    /// <item>El láser del control derecho se cruza con la curva (<see cref="Lanzar"/>): el punto en la pantalla se pasa a píxeles de la
    /// textura y se busca el gráfico de UI de esos Canvas que está ahí, como el GraphicRaycaster con el ratón en PC. El gatillo hace clic.</item>
    /// <item>B o el gatillo derecho saltan el video del logo (la misma vía que Enter en PC).</item>
    /// <item>Si la cabeza empieza a dar pose (el visor arrancó después del rig) o mira lejos de la pantalla un rato, la pantalla se recoloca.</item>
    /// </list>
    /// </summary>
    [DefaultExecutionOrder(-110)]
    public class InicioCineVR : MonoBehaviour
    {
        /// <summary>Tamaño de las texturas (px): la de los Canvas es la pantalla de referencia del juego; la pantalla mide ~105° de la vista.</summary>
        public const int AnchoTextura = 1920;
        public const int AltoTextura = 1080;
        /// <summary>A qué distancia de <see cref="CamaraUI"/> van los Canvas en Screen Space Camera (m).</summary>
        public const float DistanciaPlano = 1f;
        /// <summary>Dónde vive la cámara de los Canvas: sola, lejos de la ciudad y del cuarto oscuro (no ve nada más).</summary>
        public static readonly Vector3 PosicionCamaraUI = new Vector3(0f, -1500f, 0f);
        private const float CercaCamaraUI = 0.1f;
        private const float LejosCamaraUI = 3f;
        // Antes de la cabeza del rig (100): la pantalla muestra lo de este cuadro.
        private const float OrdenCamaraUI = 90f;
        private const int BitsProfundidad = 24;
        private const float SegundosBuscar = 0.25f;
        // Lo que tiene que moverse la cabeza (m) desde que se armó el rig para decir que el visor ya da pose.
        private const float UmbralPoseReal = 0.01f;
        private const string NombreLienzoIntro = "IntroInicio_Pantalla";

        private sealed class LienzoReclamado
        {
            public Canvas Canvas;
            public RenderMode Modo;
            public Camera Camara;
            public float Plano;
            public readonly List<GraphicRaycaster> Apagados = new List<GraphicRaycaster>();
        }

        /// <summary>El de la sesión (null hasta que se arma el primer rig de menús).</summary>
        public static InicioCineVR Actual { get; private set; }

        private readonly List<LienzoReclamado> reclamados = new List<LienzoReclamado>();
        private readonly HashSet<Canvas> mios = new HashSet<Canvas>();
        private readonly List<Canvas> temporalLienzos = new List<Canvas>();
        private readonly List<GraphicRaycaster> temporalRayos = new List<GraphicRaycaster>();
        private System.Func<Canvas, bool> predicado;
        private RenderTexture textura;
        private RenderTexture texturaUI;
        private Camera camaraUI;
        private RaycasterLaserVR modulo;
        private Camera camaraEscena;
        private RenderTexture texturaOriginal;
        private MenuInicio menu;
        private IntroInicio intro;
        private bool introPrevia;
        private Camera cabeza;
        private Vector3 poseInicial;
        private bool poseReal;
        private float fuera;
        private float buscar;
        private bool activa;
        private bool cargaPrevia;

        /// <summary>La textura donde se dibuja la cámara de la escena (el fondo de la pantalla grande).</summary>
        public RenderTexture Textura => textura;
        /// <summary>VR15: la textura donde se dibujan los Canvas de Inicio (la capa de encima, con alfa).</summary>
        public RenderTexture TexturaUI => texturaUI;
        /// <summary>VR15: la cámara que dibuja los Canvas de Inicio en <see cref="TexturaUI"/>.</summary>
        public Camera CamaraUI => camaraUI;
        /// <summary>La cámara de la escena que va a la pantalla (null si no hay).</summary>
        public Camera CamaraEscena => camaraEscena;
        /// <summary>VR15: true mientras Inicio (o su carga) va en la pantalla grande.</summary>
        public bool Activa => activa;
        /// <summary>VR15: cuántos Canvas van ahora en la pantalla grande.</summary>
        public int Reclamados => reclamados.Count;

        /// <summary>True si el video del logo se ve en la pantalla grande (su Canvas va en ella mientras suena).</summary>
        public bool IntroEnPantalla
        {
            get
            {
                if (!activa || intro == null || intro.Fase != FaseIntro.Video)
                {
                    return false;
                }
                foreach (Canvas c in intro.GetComponentsInChildren<Canvas>(true))
                {
                    if (c.name == NombreLienzoIntro && EnPantalla(c))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>VR15: true si este Canvas (raíz) se dibuja en la pantalla grande.</summary>
        public bool EnPantalla(Canvas canvas)
        {
            return activa && canvas != null && mios.Contains(canvas) && canvas.renderMode == RenderMode.ScreenSpaceCamera
                && canvas.worldCamera == camaraUI;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => Actual = null;

        /// <summary>Crea el componente (DontDestroyOnLoad) si no hay.</summary>
        public static InicioCineVR Asegurar()
        {
            if (Actual != null)
            {
                return Actual;
            }
            var objeto = new GameObject("InicioCineVR");
            DontDestroyOnLoad(objeto);
            return objeto.AddComponent<InicioCineVR>();
        }

        private void Awake()
        {
            if (Actual != null && Actual != this)
            {
                Destroy(gameObject);
                return;
            }
            Actual = this;
            predicado = EsDeLaPantalla;
            textura = new RenderTexture(AnchoTextura, AltoTextura, BitsProfundidad) { name = "InicioCineVR_Escena", antiAliasing = 1 };
            textura.Create();
            texturaUI = new RenderTexture(AnchoTextura, AltoTextura, BitsProfundidad, RenderTextureFormat.ARGB32)
            {
                name = "InicioCineVR_UI",
                antiAliasing = 1,
            };
            texturaUI.Create();
            CrearCamaraUI();
        }

        private void CrearCamaraUI()
        {
            var objeto = new GameObject("CamaraUIInicioVR");
            objeto.transform.SetParent(transform, false);
            objeto.transform.SetPositionAndRotation(PosicionCamaraUI, Quaternion.identity);
            camaraUI = objeto.AddComponent<Camera>();
            camaraUI.clearFlags = CameraClearFlags.SolidColor;
            camaraUI.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camaraUI.cullingMask = ~0;
            camaraUI.nearClipPlane = CercaCamaraUI;
            camaraUI.farClipPlane = LejosCamaraUI;
            camaraUI.depth = OrdenCamaraUI;
            camaraUI.allowHDR = false;
            camaraUI.allowMSAA = false;
            camaraUI.useOcclusionCulling = false;
            camaraUI.targetTexture = texturaUI;
            camaraUI.ResetAspect();
            UniversalAdditionalCameraData datos = camaraUI.GetUniversalAdditionalCameraData();
            if (datos != null)
            {
                datos.allowXRRendering = false;
                datos.renderPostProcessing = false;
                datos.renderShadows = false;
                datos.antialiasing = AntialiasingMode.None;
                datos.requiresColorOption = CameraOverrideOption.Off;
                datos.requiresDepthOption = CameraOverrideOption.Off;
            }
            camaraUI.enabled = false;
            // El "módulo" de los resultados del láser: su cámara de eventos es la de los Canvas (deslizadores y listas convierten bien).
            modulo = gameObject.AddComponent<RaycasterLaserVR>();
            modulo.Configurar(camaraUI);
        }

        private void OnEnable()
        {
            if (Actual == this)
            {
                MenusVR.Reclamado = predicado;
            }
        }

        private void OnDisable()
        {
            if (activa)
            {
                Desactivar();
            }
            if (MenusVR.Reclamado == predicado)
            {
                MenusVR.Reclamado = null;
            }
        }

        private void OnDestroy()
        {
            if (Actual == this)
            {
                Actual = null;
            }
            Liberar(ref textura);
            Liberar(ref texturaUI);
        }

        private static void Liberar(ref RenderTexture rt)
        {
            if (rt != null)
            {
                rt.Release();
                Destroy(rt);
                rt = null;
            }
        }

        private void Update()
        {
            if (!ModoVR.Activo)
            {
                if (activa)
                {
                    Desactivar();
                }
                return;
            }
            MenusVR menus = MenusVR.Actual;
            bool rig = menus != null && menus.RigMenusArmado;
            bool carga = PantallaCargando.Visible;
            // La carga hacia la partida sigue en la pantalla grande aunque ya mande el rig del jugador; al quitarse, la pantalla se va.
            bool quiere = rig || (activa && carga);
            if (quiere && !activa)
            {
                Activar();
            }
            else if (!quiere && activa)
            {
                Desactivar();
            }
            if (!activa)
            {
                return;
            }
            float dt = Time.unscaledDeltaTime;
            buscar -= dt;
            if (buscar <= 0f || carga != cargaPrevia)
            {
                buscar = SegundosBuscar;
                Barrer(rig);
                BuscarMenu();
            }
            cargaPrevia = carga;
            MantenerLienzos();
            ElegirCamara();
            ActualizarIntro();
            VigilarCabeza(dt);
        }

        private void Activar()
        {
            activa = true;
            buscar = 0f;
            cabeza = null;
            cargaPrevia = PantallaCargando.Visible;
            camaraUI.enabled = true;
            PantallaCineVR.MostrarEncima(this, texturaUI);
            Debug.Log("[VR] Inicio en la pantalla grande (una sola pantalla curva).");
        }

        private void Desactivar()
        {
            activa = false;
            SoltarCamara();
            SoltarLienzos();
            PantallaCineVR.Ocultar(this);
            PantallaCineVR.OcultarEncima(this);
            if (camaraUI != null)
            {
                camaraUI.enabled = false;
            }
            menu = null;
            intro = null;
            introPrevia = false;
            Debug.Log("[VR] La pantalla grande de Inicio se quita.");
        }

        // ---- Canvas → pantalla grande ----

        // Lo que MenusVR pregunta en su barrido: los Canvas de aquí (o que van a serlo) no los pasa a World Space.
        private bool EsDeLaPantalla(Canvas c)
        {
            if (!activa || c == null)
            {
                return false;
            }
            if (mios.Contains(c))
            {
                return true;
            }
            MenusVR menus = MenusVR.Actual;
            return PuedeReclamar(c, menus != null && menus.RigMenusArmado);
        }

        // Un Canvas raíz de pantalla (o uno que MenusVR ya pasó a World Space), que no es de MenusVR ni de aquí.
        // Sin rig de menús (la carga ya con el jugador) solo los de la pantalla de carga: el HUD de la ciudad lo maneja MenusVR.
        private bool PuedeReclamar(Canvas c, bool nuevos)
        {
            if (c == null)
            {
                return false;
            }
            Transform padre = c.transform.parent;
            if (padre != null && padre.GetComponentInParent<Canvas>(true) != null)
            {
                return false;
            }
            MenusVR menus = MenusVR.Actual;
            if (c.transform.IsChildOf(transform) || (menus != null && c.transform.IsChildOf(menus.transform)))
            {
                return false;
            }
            bool convertido = menus != null && menus.TipoDe(c) != null;
            if (c.renderMode == RenderMode.WorldSpace && !convertido)
            {
                return false; // letreros y pantallas del mundo, la muñeca, los avisos de VR
            }
            return nuevos || c.GetComponentInParent<PantallaCargando>(true) != null;
        }

        private void Barrer(bool nuevos)
        {
            foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (c == null || mios.Contains(c) || !PuedeReclamar(c, nuevos))
                {
                    continue;
                }
                Reclamar(c);
            }
            // GraphicRaycaster nuevos (un panel que se armó después): el ratón del PC no debe pegarle a lo que se ve en el visor.
            foreach (LienzoReclamado r in reclamados)
            {
                if (r.Canvas != null)
                {
                    ApagarRayos(r);
                }
            }
        }

        private void Reclamar(Canvas c)
        {
            MenusVR menus = MenusVR.Actual;
            if (menus != null)
            {
                menus.SoltarLienzo(c); // si MenusVR ya lo había pasado a panel, vuelve a como estaba
            }
            var r = new LienzoReclamado { Canvas = c, Modo = c.renderMode, Camara = c.worldCamera, Plano = c.planeDistance };
            reclamados.Add(r);
            mios.Add(c);
            AplicarModo(r);
            ApagarRayos(r);
            Debug.Log("[VR] Canvas " + c.name + " a la pantalla grande.");
        }

        private void AplicarModo(LienzoReclamado r)
        {
            Canvas c = r.Canvas;
            if (c.renderMode != RenderMode.ScreenSpaceCamera)
            {
                c.renderMode = RenderMode.ScreenSpaceCamera;
            }
            if (c.worldCamera != camaraUI)
            {
                c.worldCamera = camaraUI;
            }
            if (!Mathf.Approximately(c.planeDistance, DistanciaPlano))
            {
                c.planeDistance = DistanciaPlano;
            }
        }

        private void ApagarRayos(LienzoReclamado r)
        {
            r.Canvas.GetComponentsInChildren(true, temporalRayos);
            foreach (GraphicRaycaster g in temporalRayos)
            {
                if (g != null && g.enabled)
                {
                    g.enabled = false;
                    r.Apagados.Add(g);
                }
            }
        }

        // Cada cuadro: quita los destruidos y, si el juego cambió el modo de alguno, lo regresa a la pantalla grande.
        private void MantenerLienzos()
        {
            for (int i = reclamados.Count - 1; i >= 0; i--)
            {
                LienzoReclamado r = reclamados[i];
                if (r.Canvas == null)
                {
                    reclamados.RemoveAt(i);
                    continue;
                }
                AplicarModo(r);
            }
            mios.RemoveWhere(c => c == null);
        }

        private void SoltarLienzos()
        {
            foreach (LienzoReclamado r in reclamados)
            {
                foreach (GraphicRaycaster g in r.Apagados)
                {
                    if (g != null)
                    {
                        g.enabled = true;
                    }
                }
                if (r.Canvas == null)
                {
                    continue;
                }
                r.Canvas.renderMode = r.Modo;
                r.Canvas.worldCamera = r.Camara;
                r.Canvas.planeDistance = r.Plano;
            }
            reclamados.Clear();
            mios.Clear();
        }

        // ---- Láser ----

        /// <summary>
        /// VR15: lo que toca el láser en la pantalla grande: el gráfico de más arriba de los Canvas de aquí (como el GraphicRaycaster en PC,
        /// con CanvasGroup, máscaras y orden de dibujo). Si toca la pantalla pero ningún gráfico, también da true (el láser llega a la
        /// pantalla) con gameObject null.
        /// </summary>
        public bool Lanzar(Ray rayo, float distanciaMaxima, out RaycastResult resultado)
        {
            resultado = default;
            if (!activa || camaraUI == null || !PantallaCineVR.TieneEncima(this))
            {
                return false;
            }
            if (!PantallaCineVR.Tocar(rayo, out Vector2 uv, out Vector3 punto, out float distancia) || distancia > distanciaMaxima)
            {
                return false;
            }
            var pantalla = new Vector2(uv.x * texturaUI.width, uv.y * texturaUI.height);
            Transform raiz = PantallaCineVR.Raiz;
            Vector3 normal = raiz != null ? raiz.position - punto : -rayo.direction;
            normal.y = 0f;
            normal = normal.sqrMagnitude > 1e-6f ? normal.normalized : -rayo.direction;
            resultado = new RaycastResult
            {
                gameObject = null,
                module = modulo,
                distance = distancia,
                worldPosition = punto,
                worldNormal = normal,
                screenPosition = pantalla,
                index = 0,
                depth = int.MinValue,
                sortingLayer = 0,
                sortingOrder = int.MinValue,
            };
            bool hay = false;
            int mejorCapa = int.MinValue;
            int mejorOrden = int.MinValue;
            int mejorProfundidad = int.MinValue;
            foreach (LienzoReclamado r in reclamados)
            {
                Canvas lienzoRaiz = r.Canvas;
                if (lienzoRaiz == null || !lienzoRaiz.isActiveAndEnabled)
                {
                    continue;
                }
                lienzoRaiz.GetComponentsInChildren(false, temporalLienzos);
                foreach (Canvas c in temporalLienzos)
                {
                    if (c == null || !c.isActiveAndEnabled)
                    {
                        continue;
                    }
                    bool propio = c == lienzoRaiz || c.overrideSorting;
                    int idCapa = propio ? c.sortingLayerID : lienzoRaiz.sortingLayerID;
                    int capa = SortingLayer.GetLayerValueFromID(idCapa);
                    int orden = propio ? c.sortingOrder : lienzoRaiz.sortingOrder;
                    IList<Graphic> graficos = GraphicRegistry.GetRaycastableGraphicsForCanvas(c);
                    for (int i = 0; i < graficos.Count; i++)
                    {
                        Graphic g = graficos[i];
                        if (!Toca(g, pantalla))
                        {
                            continue;
                        }
                        bool mejor = !hay || capa > mejorCapa || (capa == mejorCapa && (orden > mejorOrden
                            || (orden == mejorOrden && g.depth > mejorProfundidad)));
                        if (!mejor)
                        {
                            continue;
                        }
                        hay = true;
                        mejorCapa = capa;
                        mejorOrden = orden;
                        mejorProfundidad = g.depth;
                        resultado.gameObject = g.gameObject;
                        resultado.depth = g.depth;
                        resultado.sortingLayer = idCapa;
                        resultado.sortingOrder = orden;
                    }
                }
            }
            return true;
        }

        private bool Toca(Graphic g, Vector2 pantalla)
        {
            if (g == null || !g.raycastTarget || !g.isActiveAndEnabled || g.depth == -1 || g.canvasRenderer.cull)
            {
                return false;
            }
            if (!RectTransformUtility.RectangleContainsScreenPoint(g.rectTransform, pantalla, camaraUI))
            {
                return false;
            }
            // Filtros del juego: CanvasGroup (blocksRaycasts), máscaras, alfa de la imagen.
            return g.Raycast(pantalla, camaraUI);
        }

        /// <summary>
        /// VR15: el punto de la pantalla grande (en el mundo) donde se ve el centro de este elemento de UI de un Canvas de aquí.
        /// False si no va en la pantalla grande o cae fuera de ella. Sirve para apuntar el láser (pruebas, ayudas).
        /// </summary>
        public bool PuntoEnPantalla(RectTransform elemento, out Vector3 punto)
        {
            punto = Vector3.zero;
            if (!activa || elemento == null || camaraUI == null)
            {
                return false;
            }
            Vector3 centro = elemento.TransformPoint(elemento.rect.center);
            Vector2 pantalla = RectTransformUtility.WorldToScreenPoint(camaraUI, centro);
            var uv = new Vector2(pantalla.x / texturaUI.width, pantalla.y / texturaUI.height);
            if (uv.x < 0f || uv.x > 1f || uv.y < 0f || uv.y > 1f)
            {
                return false;
            }
            punto = PantallaCineVR.PuntoEnUV(uv);
            return true;
        }

        // ---- Cámara de la escena → fondo ----

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
                    Debug.Log("[VR] La cámara " + deseada.name + " va al fondo de la pantalla grande.");
                }
            }
            if (camaraEscena == null)
            {
                // Sin cámara de escena (cambio de escena, carga): fondo negro y la capa de los Canvas encima.
                if (PantallaCineVR.Tiene(this))
                {
                    PantallaCineVR.Ocultar(this);
                }
                return;
            }
            if (camaraEscena.targetTexture != textura)
            {
                camaraEscena.targetTexture = textura;
                camaraEscena.ResetAspect();
            }
            SistemaVR.PermitirXR(camaraEscena, false);
            if (!PantallaCineVR.Tiene(this))
            {
                PantallaCineVR.Mostrar(this, textura, (float)AnchoTextura / AltoTextura);
            }
        }

        // La del recorrido de Menu_Calles si está; si no, la principal de la escena. Nunca una cabeza (ni la del jugador, que en la carga
        // ya es Camera.main), ni la de los Canvas, ni una que ya dibuja en otra textura.
        private Camera CamaraDeEscena()
        {
            AdaptadorVR a = AdaptadorVR.Actual;
            if (a != null && a.Armado)
            {
                return null;
            }
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
            MenusVR menus = MenusVR.Actual;
            return c != null && c.isActiveAndEnabled && c != SistemaVR.CamaraVR && c != camaraUI
                && (menus == null || c != menus.CamaraRigMenus)
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

        // ---- Intro ----

        private void BuscarMenu()
        {
            if (menu != null)
            {
                return;
            }
            menu = FindFirstObjectByType<MenuInicio>();
            intro = menu != null ? menu.GetComponent<IntroInicio>() : null;
        }

        // El video del logo se ve en la pantalla grande con su Canvas (negro y video encima de todo, como en PC). B o el gatillo lo saltan.
        private void ActualizarIntro()
        {
            bool enVideo = intro != null && intro.Fase == FaseIntro.Video;
            if (enVideo && (EntradaVR.Pulso(EntradaVR.BotonB) || EntradaVR.Pulso(EntradaVR.GatilloDerecho)))
            {
                // La misma vía que Enter, clic o Escape en PC.
                intro.Saltar();
                Debug.Log("[VR] Intro saltada con el control.");
            }
            if (introPrevia && !enVideo)
            {
                // Acabó el logo: la pantalla (ahora con el menú sobre las calles) frente a donde mira.
                Recentrar();
            }
            introPrevia = enVideo;
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
            // El visor empezó a dar pose después de que se colocó la pantalla: se pone frente a la cabeza de verdad.
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

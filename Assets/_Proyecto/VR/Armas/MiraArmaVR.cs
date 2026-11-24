using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.VR.Menus;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// T101 (antes VR51/T91): apuntar con el gatillo IZQUIERDO las armas de bala (pistola, revólver, subfusil, fusil, escopeta y francotirador).
    /// Mientras está apretado TODO se pone negro salvo un círculo de <see cref="ReglaMiraVR.AnguloCirculo"/>° con borde difuminado, pegado a la vista
    /// (un Canvas World Space hijo de la cámara XR, a <see cref="DistanciaMira"/> m, que ven igual los dos ojos) y alineado con el cañón: su centro va
    /// sobre la línea ojo-punto de impacto (<see cref="ReglaMiraVR.PosicionReticula"/>; el rayo de la boca del arma, como dispara
    /// <see cref="PistolaAPie"/> en VR). Dentro del círculo se ve la imagen de una cámara de mira en la boca del arma que dibuja a una RenderTexture
    /// (<see cref="ResolucionMira"/>²), con el aumento de <see cref="ReglaMiraVR"/>: el zoom va SOLO ahí, la cámara del visor no cambia. Encima, la
    /// retícula propia (<see cref="SpritesMiraVR"/>): cruz negra delgada con marcas de distancia, anillo rojo abierto, punto central y arcos cian.
    /// <list type="bullet">
    /// <item>Francotirador: zoom de 2× a 8× con el stick derecho (adelante acerca, atrás aleja); ese rato el stick derecho no gira ni recentra
    ///   (<see cref="BloqueaGiro"/>, <see cref="CambioArmaStickVR.ZoomActivo"/>).</item>
    /// <item>Las demás: <see cref="ReglaMiraVR.AumentoFijo"/>× fijo.</item>
    /// </list>
    /// Al apretar y al soltar hay un fundido de <see cref="ReglaMiraVR.SegundosFundido"/> s. La cámara de mira solo dibuja mientras se ve la mira.
    /// El lanzagranadas (arco) y el plasma (carga) con el gatillo izquierdo los hace <see cref="ArmasEspecialesVR"/>. El Canvas va en la capa UI, que
    /// la cámara de mira no dibuja, y con un material que se dibuja encima de todo (el arma y las manos no tapan la mira). Corre después de
    /// <see cref="PistolaVR"/>, que coloca el arma. Lo agrega <see cref="ArranqueCombateVR"/> (solo en VR: en PC nada cambia).
    /// </summary>
    [DefaultExecutionOrder(125)]
    public class MiraArmaVR : MonoBehaviour
    {
        /// <summary>Qué mira lleva un arma en VR (<see cref="ReglaMiraVR.MiraDe"/>).</summary>
        public enum TipoMira
        {
            Ninguna,
            /// <summary>Aumento fijo (<see cref="ReglaMiraVR.AumentoFijo"/>).</summary>
            Fija,
            /// <summary>Zoom con el stick derecho (francotirador).</summary>
            Zoom
        }

        /// <summary>Hasta dónde (m) se busca el punto de impacto desde la boca.</summary>
        public const float AlcanceReticula = 150f;
        /// <summary>Para <see cref="ReglaMiraVR.PosicionReticula"/>: el punto de impacto más cerca que esto (m) cuenta a esta distancia.</summary>
        public const float DistanciaMinima = 0.3f;
        /// <summary>Para <see cref="ReglaMiraVR.PosicionReticula"/>: ni más lejos que esto (m).</summary>
        public const float DistanciaMaxima = 60f;
        /// <summary>T101: el círculo de la mira va a esta distancia del ojo (m): cómodo para los dos ojos y delante del arma.</summary>
        public const float DistanciaMira = 1f;
        /// <summary>Unidades del Canvas a lo ancho del círculo.</summary>
        public const float Unidades = 100f;
        /// <summary>T101: lado de la textura de la cámara de mira (px): moderado para el Quest.</summary>
        public const int ResolucionMira = 1024;
        private const int ProfundidadMira = 24;
        private const float CercaMira = 0.05f;
        private const float BordeNegro = 2000f;
        private const int ColaEncima = 4000;
        private const int OrdenLienzo = 50;
        private const int CapaUIPorDefecto = 5;
        private const bool SombrasEnMira = false; // rendimiento en el Quest: la cámara de mira no dibuja sombras
        // Tamaños de la retícula en unidades del lienzo (100 = el círculo entero).
        private const float LadoAnilloRojo = 11f;
        private const float LadoPuntoCentral = 6f;

        private static readonly Color ColorCruz = new Color(0.03f, 0.03f, 0.04f, 0.95f);
        private static readonly Color ColorRojo = new Color(1f, 0.16f, 0.12f, 0.95f);
        private static readonly Color ColorCian = new Color(0.30f, 0.92f, 0.90f, 0.45f);

        private static bool bloqueaGiro;

        private readonly ReglaMiraVR regla = new ReglaMiraVR();
        private AdaptadorVR adaptador;
        private ArmasAPie armas;
        private PistolaVR pistolaVR;
        private CambioArmaStickVR stick;

        private Material materialEncima;
        private Canvas lienzo;
        private CanvasGroup grupo;
        private Camera camaraMira;
        private RenderTexture texturaMira;

        /// <summary>
        /// True mientras se apunta con el francotirador: el stick derecho en vertical es el zoom. El giro (<see cref="AdaptadorVR"/>) no gira
        /// mientras sea true.
        /// </summary>
        public static bool BloqueaGiro => bloqueaGiro;

        /// <summary>La mira que se usa ahora (Ninguna si no se apunta; al soltar pasa a Ninguna aunque siga el fundido).</summary>
        public TipoMira Activa => regla.Activa;

        /// <summary>True si la mira (el negro con el círculo) se ve delante de los ojos, aunque sea en el fundido.</summary>
        public bool ReticulaVisible => lienzo != null && lienzo.gameObject.activeInHierarchy;

        /// <summary>Opacidad de la mira (0 vista normal, 1 todo negro salvo el círculo).</summary>
        public float Alfa => regla.Alfa;

        /// <summary>El punto (mundo) donde pega la bala según el último cuadro con mira.</summary>
        public Vector3 PuntoImpacto { get; private set; }

        /// <summary>El aumento de la mira (×).</summary>
        public float Aumento => regla.Aumento;

        /// <summary>El campo de visión de la cámara de mira (°).</summary>
        public float Fov => regla.Fov;

        /// <summary>La cámara de mira (null si no se creó).</summary>
        public Camera CamaraMira => camaraMira;

        /// <summary>La textura donde dibuja la cámara de mira (null si no se creó).</summary>
        public RenderTexture TexturaMira => texturaMira;

        /// <summary>El Canvas de la mira (null si no se creó).</summary>
        public Canvas Lienzo => lienzo;

        /// <summary>La mira de cada arma en VR.</summary>
        public static TipoMira MiraDe(ArmaAPie arma) => ReglaMiraVR.MiraDe(arma);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            bloqueaGiro = false;
        }

        private void Awake()
        {
            adaptador = GetComponent<AdaptadorVR>();
            armas = GetComponent<ArmasAPie>();
            pistolaVR = GetComponent<PistolaVR>();
            stick = GetComponent<CambioArmaStickVR>();
            if (adaptador == null || armas == null || pistolaVR == null)
            {
                Debug.LogWarning("[VR] MiraArmaVR sin AdaptadorVR, ArmasAPie o PistolaVR en " + name + ".", this);
                enabled = false;
            }
        }

        private void Start()
        {
            // Las texturas de la retícula se dibujan aquí y no al apuntar la primera vez (sin tirón).
            _ = SpritesMiraVR.CruzMarcas;
            _ = SpritesMiraVR.Vineta;
            _ = SpritesMiraVR.AnilloAbierto;
            _ = SpritesMiraVR.AcentoCian;
            _ = SpritesMiraVR.Punto;
        }

        private void OnDisable()
        {
            Cortar();
        }

        private void OnDestroy()
        {
            bloqueaGiro = false;
            if (lienzo != null)
            {
                Destroy(lienzo.gameObject);
            }
            if (camaraMira != null)
            {
                Destroy(camaraMira.gameObject);
            }
            if (texturaMira != null)
            {
                texturaMira.Release();
                Destroy(texturaMira);
            }
            if (materialEncima != null)
            {
                Destroy(materialEncima);
            }
        }

        private void LateUpdate()
        {
            if (adaptador == null)
            {
                Destroy(this);
                return;
            }
            Camera cabeza = adaptador.Camara;
            bool bloqueado = MenusVR.MenuAbierto || Pausa.Activa || (adaptador.Jugador != null && adaptador.Jugador.Modo != ModoJugador.APie);
            if (!adaptador.Armado || bloqueado || cabeza == null)
            {
                Cortar();
                return;
            }
            Transform boca = pistolaVR.Boca;
            bool conArma = pistolaVR.ArmaVisible && pistolaVR.Arma != null && boca != null;
            TipoMira tipo = conArma ? MiraDe(armas.ArmaActiva) : TipoMira.Ninguna;
            bool gatillo = EntradaVR.Sostenido(EntradaVR.GatilloIzquierdo);
            float ejeY = tipo == TipoMira.Zoom && gatillo ? EntradaVR.Eje(EntradaVR.StickDerecho).y : 0f;
            regla.Avanzar(tipo, gatillo, ejeY, Time.unscaledDeltaTime);
            FijarBloqueo(regla.BloqueaGiro);
            if (!regla.Visible)
            {
                Ocultar();
                return;
            }

            bool empieza = !ReticulaVisible;
            Asegurar(cabeza.transform);
            if (empieza)
            {
                CopiarCamara(cabeza);
            }
            Activar(lienzo, true);
            grupo.alpha = regla.Alfa;
            if (conArma)
            {
                // Mientras se desvanece sin arma (la guardó) se queda donde estaba, pegada a la cabeza.
                Colocar(cabeza.transform, boca);
            }
            camaraMira.fieldOfView = regla.Fov;
            if (!camaraMira.enabled)
            {
                camaraMira.enabled = true;
            }
        }

        // El círculo sobre la línea ojo-impacto (mirando al ojo, sin girar con la muñeca) y la cámara de mira en la boca, mirando por el cañón.
        private void Colocar(Transform ojo, Transform boca)
        {
            PuntoImpacto = ReglaImpactoBala.BuscarPrimero(boca.position, boca.forward, AlcanceReticula, transform.root, null, out RaycastHit impacto)
                ? impacto.point
                : boca.position + boca.forward * AlcanceReticula;
            Vector3 enCabeza = Quaternion.Inverse(ojo.rotation) * (PuntoImpacto - ojo.position);
            bool ve = ReglaMiraVR.PosicionReticula(enCabeza, DistanciaMinima, DistanciaMaxima, out Vector3 posicion);
            Vector3 direccion = ve ? posicion.normalized : Vector3.forward;

            Transform t = lienzo.transform;
            Vector3 mundo = ojo.position + ojo.rotation * (direccion * DistanciaMira);
            t.SetPositionAndRotation(mundo, Quaternion.LookRotation(mundo - ojo.position, ojo.up));
            float escalaPadre = Mathf.Max(1e-4f, Mathf.Abs(ojo.lossyScale.x));
            t.localScale = Vector3.one * (ReglaMiraVR.LadoAngular(DistanciaMira, ReglaMiraVR.AnguloCirculo) / Unidades / escalaPadre);

            // La imagen no rueda con la muñeca: arriba es el arriba de la cabeza (si el cañón apunta justo arriba o abajo, el de la boca).
            Vector3 adelante = boca.forward;
            Quaternion rotacion = Mathf.Abs(Vector3.Dot(adelante.normalized, ojo.up)) < 0.999f ? Quaternion.LookRotation(adelante, ojo.up) : boca.rotation;
            camaraMira.transform.SetPositionAndRotation(boca.position, rotacion);
        }

        // Lo mismo que dibuja la cámara del visor (menos la capa UI, donde va la mira): misma máscara, distancias de corte y fondo.
        private void CopiarCamara(Camera cabeza)
        {
            camaraMira.cullingMask = cabeza.cullingMask & ~(1 << CapaUI);
            camaraMira.farClipPlane = cabeza.farClipPlane;
            camaraMira.layerCullDistances = cabeza.layerCullDistances;
            camaraMira.layerCullSpherical = cabeza.layerCullSpherical;
            camaraMira.useOcclusionCulling = cabeza.useOcclusionCulling;
            camaraMira.clearFlags = cabeza.clearFlags;
            Color fondo = cabeza.backgroundColor;
            fondo.a = 1f; // el cielo sin alfa no deja ver la vista normal por el círculo
            camaraMira.backgroundColor = fondo;
        }

        private void FijarBloqueo(bool bloquea)
        {
            bloqueaGiro = bloquea;
            if (stick != null)
            {
                stick.ZoomActivo = bloquea;
            }
        }

        private static void Activar(Canvas c, bool activo)
        {
            if (c != null && c.gameObject.activeSelf != activo)
            {
                c.gameObject.SetActive(activo);
            }
        }

        private void Ocultar()
        {
            Activar(lienzo, false);
            if (camaraMira != null && camaraMira.enabled)
            {
                camaraMira.enabled = false;
            }
        }

        // Quita la mira de golpe (menú, pausa, sin rig, componente apagado).
        private void Cortar()
        {
            regla.Cortar();
            FijarBloqueo(false);
            Ocultar();
        }

        private static int CapaUI
        {
            get
            {
                int capa = LayerMask.NameToLayer("UI");
                return capa >= 0 ? capa : CapaUIPorDefecto;
            }
        }

        // Crea lo que falte. El Canvas cuelga de la cámara XR (sigue a la cabeza aun si el casco se mueve después del LateUpdate).
        private void Asegurar(Transform cabeza)
        {
            if (materialEncima == null)
            {
                materialEncima = new Material(Canvas.GetDefaultCanvasMaterial()) { name = "MiraEncimaVR", renderQueue = ColaEncima };
                materialEncima.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always); // el arma, las manos y las paredes cercanas no tapan la mira
            }
            if (texturaMira == null)
            {
                texturaMira = new RenderTexture(ResolucionMira, ResolucionMira, ProfundidadMira, RenderTextureFormat.ARGB32)
                {
                    name = "MiraVR",
                    antiAliasing = 1,
                    useMipMap = false,
                };
                texturaMira.Create();
            }
            if (camaraMira == null)
            {
                var objetoCamara = new GameObject("CamaraMiraVR");
                camaraMira = objetoCamara.AddComponent<Camera>();
                camaraMira.stereoTargetEye = StereoTargetEyeMask.None; // no va al visor: solo dibuja en la textura
                camaraMira.targetTexture = texturaMira;
                camaraMira.nearClipPlane = CercaMira;
                camaraMira.fieldOfView = regla.Fov;
                camaraMira.allowHDR = false;
                camaraMira.allowMSAA = false;
                camaraMira.enabled = false;
                UniversalAdditionalCameraData datos = camaraMira.GetUniversalAdditionalCameraData();
                datos.renderShadows = SombrasEnMira;
                datos.renderPostProcessing = false;
                datos.antialiasing = AntialiasingMode.None;
                datos.requiresDepthTexture = false;
                datos.requiresColorTexture = false;
                CopiarCamara(adaptador.Camara);
            }
            if (lienzo == null)
            {
                var raiz = new GameObject("MiraVR", typeof(RectTransform));
                raiz.SetActive(false);
                raiz.layer = CapaUI;
                raiz.AddComponent<IgnorarOcultarVR>();
                raiz.transform.SetParent(cabeza, false);
                lienzo = raiz.AddComponent<Canvas>();
                lienzo.renderMode = RenderMode.WorldSpace;
                lienzo.sortingOrder = OrdenLienzo;
                grupo = raiz.AddComponent<CanvasGroup>();
                grupo.interactable = false;
                grupo.blocksRaycasts = false;
                var rt = (RectTransform)raiz.transform;
                rt.sizeDelta = new Vector2(Unidades, Unidades);
                rt.pivot = new Vector2(0.5f, 0.5f);

                // De atrás para adelante: el mundo por la mira, la cruz, el anillo rojo y el punto, el negro difuminado, los arcos cian sobre el borde
                // y cuatro paneles negros que tapan el resto de la vista.
                RectTransform cuadro = Hijo(rt, "Mundo", Unidades);
                RawImage imagenMira = cuadro.gameObject.AddComponent<RawImage>();
                imagenMira.texture = texturaMira;
                imagenMira.material = materialEncima;
                imagenMira.raycastTarget = false;
                Imagen(rt, "Cruz", SpritesMiraVR.CruzMarcas, ColorCruz, Unidades);
                Imagen(rt, "AnilloRojo", SpritesMiraVR.AnilloAbierto, ColorRojo, LadoAnilloRojo);
                Imagen(rt, "Punto", SpritesMiraVR.Punto, ColorRojo, LadoPuntoCentral);
                Imagen(rt, "Vineta", SpritesMiraVR.Vineta, Color.black, Unidades);
                Imagen(rt, "AcentoCian", SpritesMiraVR.AcentoCian, ColorCian, Unidades);
                float mitad = Unidades * 0.5f;
                Panel(rt, "Arriba", new Vector2(0f, mitad + BordeNegro * 0.5f), new Vector2(Unidades + 2f * BordeNegro, BordeNegro));
                Panel(rt, "Abajo", new Vector2(0f, -(mitad + BordeNegro * 0.5f)), new Vector2(Unidades + 2f * BordeNegro, BordeNegro));
                Panel(rt, "Izquierda", new Vector2(-(mitad + BordeNegro * 0.5f), 0f), new Vector2(BordeNegro, Unidades));
                Panel(rt, "Derecha", new Vector2(mitad + BordeNegro * 0.5f, 0f), new Vector2(BordeNegro, Unidades));
            }
        }

        private static RectTransform Hijo(Transform padre, string nombre, float lado)
        {
            var g = new GameObject(nombre, typeof(RectTransform));
            g.layer = padre.gameObject.layer;
            g.transform.SetParent(padre, false);
            var r = (RectTransform)g.transform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(lado, lado);
            r.anchoredPosition = Vector2.zero;
            return r;
        }

        private RectTransform Imagen(Transform padre, string nombre, Sprite sprite, Color color, float lado)
        {
            RectTransform r = Hijo(padre, nombre, lado);
            Image i = r.gameObject.AddComponent<Image>();
            i.sprite = sprite;
            i.color = color;
            i.material = materialEncima;
            i.raycastTarget = false;
            return r;
        }

        private void Panel(Transform padre, string nombre, Vector2 posicion, Vector2 tamano)
        {
            RectTransform r = Imagen(padre, nombre, null, Color.black, 1f);
            r.sizeDelta = tamano;
            r.anchoredPosition = posicion;
        }
    }
}

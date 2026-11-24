using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.VR.Menus;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// T91 (antes VR51): apuntar con el gatillo IZQUIERDO las armas de bala. Mientras está apretado sale una retícula a vista completa delante de
    /// los ojos (un Canvas World Space hijo de la cámara XR, pegado a la cabeza), alineada con el cañón: cada cuadro se busca dónde pega la bala
    /// (el rayo de la boca del arma, como dispara <see cref="PistolaAPie"/> en VR) y la retícula se pone sobre la línea ojo-impacto
    /// (<see cref="ReglaMiraVR.PosicionReticula"/>), con tamaño angular fijo. Al soltar el gatillo izquierdo se quita. Ya no hay mira en el arma.
    /// <list type="bullet">
    /// <item>Pistola y revólver: cruz simple con punto (<see cref="TipoMira.Cruz"/>).</item>
    /// <item>Subfusil: punto rojo (<see cref="TipoMira.PuntoRojo"/>).</item>
    /// <item>Fusil y escopeta: retícula de mira, anillo con trazos y punto; la de la escopeta es más ancha (<see cref="TipoMira.Mira"/>).</item>
    /// <item>Francotirador: visor a vista completa con bordes negros; una cámara en el cañón dibuja el círculo. El stick derecho en vertical
    ///   hace zoom (<see cref="ReglaMiraVR.AvanzarZoom"/>); ese rato el stick derecho no recentra y <see cref="BloqueaGiro"/> avisa que no debe girar.</item>
    /// </list>
    /// El lanzagranadas (arco) y el plasma (carga) con el gatillo izquierdo los hace <see cref="ArmasEspecialesVR"/>. Los Canvas van en la capa UI,
    /// que la cámara del visor no dibuja, y con un material que se dibuja encima de todo (el arma no tapa la retícula). Corre después de
    /// <see cref="PistolaVR"/>, que coloca el arma. Lo agrega <see cref="ArranqueCombateVR"/>.
    /// </summary>
    [DefaultExecutionOrder(125)]
    public class MiraArmaVR : MonoBehaviour
    {
        /// <summary>Qué retícula lleva un arma en VR.</summary>
        public enum TipoMira
        {
            Ninguna,
            Cruz,
            PuntoRojo,
            Mira,
            Visor
        }

        /// <summary>Hasta dónde (m) se busca el punto de impacto desde la boca.</summary>
        public const float AlcanceReticula = 150f;
        /// <summary>La retícula nunca va más cerca del ojo que esto (m).</summary>
        public const float DistanciaMinima = 0.3f;
        /// <summary>Ni más lejos que esto (m): más allá la diferencia entre los ojos ya no se nota.</summary>
        public const float DistanciaMaxima = 60f;
        /// <summary>El visor del francotirador va a esta distancia del ojo (m).</summary>
        public const float DistanciaVisor = 0.5f;
        /// <summary>Lo que abarca el lienzo de la retícula (°): <see cref="Unidades"/> unidades del Canvas.</summary>
        public const float AnguloReticula = 24f;
        /// <summary>Lo que abarca el círculo del visor (°): <see cref="Unidades"/> unidades del Canvas.</summary>
        public const float AnguloVisor = 40f;
        /// <summary>Unidades del Canvas a lo ancho del lienzo.</summary>
        public const float Unidades = 100f;
        /// <summary>Lado de la textura del visor (px).</summary>
        public const int ResolucionVisor = 512;
        /// <summary>La retícula de la escopeta es esto más ancha (el cono de perdigones).</summary>
        public const float EscalaEscopeta = 1.6f;
        private const int ProfundidadVisor = 16;
        private const float CercaVisor = 0.05f;
        private const float LejosVisor = 1500f;
        private const float BordeNegro = 2000f;
        private const int ColaEncima = 4000;
        private const int OrdenLienzo = 50;
        private const int CapaUIPorDefecto = 5;

        private static readonly Color ColorCian = new Color(0.30f, 0.92f, 0.90f, 0.95f);
        private static readonly Color ColorRojo = new Color(1f, 0.22f, 0.18f, 0.95f);
        private static readonly Color ColorHalo = new Color(1f, 0.22f, 0.18f, 0.25f);
        private static readonly Color ColorCruzVisor = new Color(0.05f, 0.05f, 0.05f, 0.95f);

        private static bool bloqueaGiro;

        private readonly ReglaMiraVR regla = new ReglaMiraVR();
        private AdaptadorVR adaptador;
        private ArmasAPie armas;
        private PistolaVR pistolaVR;
        private CambioArmaStickVR stick;

        private Material materialEncima;
        private Canvas lienzo;
        private Canvas lienzoVisor;
        private GameObject cruz;
        private GameObject puntoRojo;
        private RectTransform mira;
        private RawImage imagenVisor;
        private Camera camaraVisor;
        private RenderTexture texturaVisor;

        /// <summary>
        /// True mientras el visor del francotirador está activo: el stick derecho en vertical es el zoom. El giro (AdaptadorVR) debería no girar
        /// mientras sea true.
        /// </summary>
        public static bool BloqueaGiro => bloqueaGiro;

        /// <summary>La retícula que se ve ahora (Ninguna si no se apunta).</summary>
        public TipoMira Activa { get; private set; }

        /// <summary>True si la retícula (o el visor) se ve delante de los ojos.</summary>
        public bool ReticulaVisible => (lienzo != null && lienzo.gameObject.activeInHierarchy) || (lienzoVisor != null && lienzoVisor.gameObject.activeInHierarchy);

        /// <summary>El punto (mundo) donde pega la bala según el último cuadro con retícula.</summary>
        public Vector3 PuntoImpacto { get; private set; }

        /// <summary>El campo de visión del visor del francotirador (°).</summary>
        public float Fov => regla.Fov;

        /// <summary>La cámara del visor (null si no se creó).</summary>
        public Camera CamaraVisor => camaraVisor;

        /// <summary>El Canvas de la retícula (null si no se creó).</summary>
        public Canvas Lienzo => lienzo;

        /// <summary>El Canvas del visor del francotirador (null si no se creó).</summary>
        public Canvas LienzoVisor => lienzoVisor;

        /// <summary>La retícula de cada arma en VR.</summary>
        public static TipoMira MiraDe(ArmaAPie arma)
        {
            switch (arma)
            {
                case ArmaAPie.Pistola:
                case ArmaAPie.Revolver:
                    return TipoMira.Cruz;
                case ArmaAPie.Subfusil:
                    return TipoMira.PuntoRojo;
                case ArmaAPie.Fusil:
                case ArmaAPie.Escopeta:
                    return TipoMira.Mira;
                case ArmaAPie.Sniper:
                    return TipoMira.Visor;
                default:
                    return TipoMira.Ninguna;
            }
        }

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

        private void OnDisable()
        {
            Ocultar();
        }

        private void OnDestroy()
        {
            bloqueaGiro = false;
            if (lienzo != null)
            {
                Destroy(lienzo.gameObject);
            }
            if (lienzoVisor != null)
            {
                Destroy(lienzoVisor.gameObject);
            }
            if (camaraVisor != null)
            {
                Destroy(camaraVisor.gameObject);
            }
            if (texturaVisor != null)
            {
                texturaVisor.Release();
                Destroy(texturaVisor);
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
            if (!adaptador.Armado)
            {
                Ocultar();
                return;
            }
            ArmaAPie activa = armas.ArmaActiva;
            TipoMira tipo = MiraDe(activa);
            bool bloqueado = MenusVR.MenuAbierto || Pausa.Activa || (adaptador.Jugador != null && adaptador.Jugador.Modo != ModoJugador.APie);
            Transform boca = pistolaVR.Boca;
            Camera cabeza = adaptador.Camara;
            bool apunta = !bloqueado && tipo != TipoMira.Ninguna && pistolaVR.ArmaVisible && pistolaVR.Arma != null && boca != null && cabeza != null
                && EntradaVR.Sostenido(EntradaVR.GatilloIzquierdo);
            if (!apunta)
            {
                Ocultar();
                return;
            }
            Asegurar(cabeza.transform);
            Activa = tipo;
            bloqueaGiro = tipo == TipoMira.Visor;
            if (stick != null)
            {
                stick.ZoomActivo = bloqueaGiro;
            }
            PuntoImpacto = ReglaImpactoBala.BuscarPrimero(boca.position, boca.forward, AlcanceReticula, transform.root, null, out RaycastHit impacto)
                ? impacto.point
                : boca.position + boca.forward * AlcanceReticula;

            Transform ojo = cabeza.transform;
            Vector3 enCabeza = Quaternion.Inverse(ojo.rotation) * (PuntoImpacto - ojo.position);
            bool ve = ReglaMiraVR.PosicionReticula(enCabeza, DistanciaMinima, DistanciaMaxima, out Vector3 posicion);
            if (tipo == TipoMira.Visor)
            {
                Activar(lienzo, false);
                Activar(lienzoVisor, true);
                Vector3 direccion = ve ? posicion.normalized : Vector3.forward;
                Colocar(lienzoVisor, ojo, direccion * DistanciaVisor, AnguloVisor);
                regla.AvanzarZoom(EntradaVR.Eje(EntradaVR.StickDerecho).y, Time.unscaledDeltaTime);
                camaraVisor.fieldOfView = regla.Fov;
                camaraVisor.transform.SetPositionAndRotation(boca.position, boca.rotation); // sigue al cañón (aquí, después de que PistolaVR lo coloca)
                camaraVisor.enabled = true;
                return;
            }
            Activar(lienzoVisor, false);
            if (camaraVisor.enabled)
            {
                camaraVisor.enabled = false;
            }
            Activar(lienzo, ve);
            if (!ve)
            {
                return;
            }
            Colocar(lienzo, ojo, posicion, AnguloReticula);
            cruz.SetActive(tipo == TipoMira.Cruz);
            puntoRojo.SetActive(tipo == TipoMira.PuntoRojo);
            mira.gameObject.SetActive(tipo == TipoMira.Mira);
            mira.localScale = Vector3.one * (activa == ArmaAPie.Escopeta ? EscalaEscopeta : 1f);
        }

        // Pone el lienzo en la posición (ejes de la cabeza, m) mirando al ojo, con el ancho que abarca esos grados de la vista.
        private static void Colocar(Canvas c, Transform ojo, Vector3 posicion, float grados)
        {
            Transform t = c.transform;
            Vector3 mundo = ojo.position + ojo.rotation * posicion;
            t.SetPositionAndRotation(mundo, Quaternion.LookRotation(mundo - ojo.position, ojo.up));
            float escalaPadre = Mathf.Max(1e-4f, Mathf.Abs(ojo.lossyScale.x));
            t.localScale = Vector3.one * (ReglaMiraVR.LadoAngular(posicion.magnitude, grados) / Unidades / escalaPadre);
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
            Activa = TipoMira.Ninguna;
            bloqueaGiro = false;
            if (stick != null)
            {
                stick.ZoomActivo = false;
            }
            Activar(lienzo, false);
            Activar(lienzoVisor, false);
            if (camaraVisor != null && camaraVisor.enabled)
            {
                camaraVisor.enabled = false;
            }
        }

        private static int CapaUI
        {
            get
            {
                int capa = LayerMask.NameToLayer("UI");
                return capa >= 0 ? capa : CapaUIPorDefecto;
            }
        }

        // Crea lo que falte. Los Canvas cuelgan de la cámara XR (siguen a la cabeza aun si el casco se mueve después del LateUpdate).
        private void Asegurar(Transform cabeza)
        {
            if (materialEncima == null)
            {
                materialEncima = new Material(Canvas.GetDefaultCanvasMaterial()) { name = "ReticulaEncimaVR", renderQueue = ColaEncima };
                materialEncima.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always); // el arma y las paredes cercanas no tapan la retícula
            }
            if (texturaVisor == null)
            {
                texturaVisor = new RenderTexture(ResolucionVisor, ResolucionVisor, ProfundidadVisor, RenderTextureFormat.ARGB32) { name = "VisorSniperVR" };
                texturaVisor.Create();
            }
            if (camaraVisor == null)
            {
                var objetoCamara = new GameObject("CamaraVisorVR");
                camaraVisor = objetoCamara.AddComponent<Camera>();
                camaraVisor.stereoTargetEye = StereoTargetEyeMask.None; // no va al visor: solo dibuja en la textura
                camaraVisor.targetTexture = texturaVisor;
                camaraVisor.nearClipPlane = CercaVisor;
                camaraVisor.farClipPlane = LejosVisor;
                camaraVisor.fieldOfView = regla.Fov;
                camaraVisor.cullingMask = ~(1 << CapaUI); // no se ve a sí misma ni la retícula
                camaraVisor.enabled = false;
            }
            if (lienzo == null)
            {
                lienzo = NuevoLienzo("ReticulaVR", cabeza);
                var rt = (RectTransform)lienzo.transform;
                // Pistola y revólver: cruz simple con punto.
                cruz = Hijo(rt, "Cruz", Unidades).gameObject;
                Imagen(cruz.transform, "Trazos", SpritesMiraVR.CruzSimple, ColorCian, 40f);
                Imagen(cruz.transform, "Punto", SpritesMiraVR.Punto, ColorCian, 6f);
                // Subfusil: punto rojo con un halo tenue.
                puntoRojo = Hijo(rt, "PuntoRojo", Unidades).gameObject;
                Imagen(puntoRojo.transform, "Halo", SpritesMiraVR.Punto, ColorHalo, 22f);
                Imagen(puntoRojo.transform, "Punto", SpritesMiraVR.Punto, ColorRojo, 11f);
                // Fusil y escopeta: anillo, trazos y punto rojo.
                mira = Hijo(rt, "Mira", Unidades);
                Imagen(mira, "Anillo", SpritesMiraVR.Anillo, ColorCian, 60f);
                Imagen(mira, "Trazos", SpritesMiraVR.CruzSimple, ColorCian, 48f);
                Imagen(mira, "Punto", SpritesMiraVR.Punto, ColorRojo, 5f);
            }
            if (lienzoVisor == null)
            {
                lienzoVisor = NuevoLienzo("VisorFrancotiradorVR", cabeza);
                var rt = (RectTransform)lienzoVisor.transform;
                // La imagen de la cámara del cañón; encima, el agujero negro la recorta en círculo, y cuatro paneles negros tapan el resto de la vista.
                RectTransform cuadro = Hijo(rt, "Imagen", Unidades);
                imagenVisor = cuadro.gameObject.AddComponent<RawImage>();
                imagenVisor.texture = texturaVisor;
                imagenVisor.material = materialEncima;
                imagenVisor.raycastTarget = false;
                Imagen(rt, "Cruz", SpritesMiraVR.Cruz, ColorCruzVisor, Unidades);
                Imagen(rt, "Centro", SpritesMiraVR.Punto, ColorRojo, 2.5f);
                Imagen(rt, "Agujero", SpritesMiraVR.Agujero, Color.black, Unidades);
                float mitad = Unidades * 0.5f;
                Panel(rt, "Arriba", new Vector2(0f, mitad + BordeNegro * 0.5f), new Vector2(Unidades + 2f * BordeNegro, BordeNegro));
                Panel(rt, "Abajo", new Vector2(0f, -(mitad + BordeNegro * 0.5f)), new Vector2(Unidades + 2f * BordeNegro, BordeNegro));
                Panel(rt, "Izquierda", new Vector2(-(mitad + BordeNegro * 0.5f), 0f), new Vector2(BordeNegro, Unidades));
                Panel(rt, "Derecha", new Vector2(mitad + BordeNegro * 0.5f, 0f), new Vector2(BordeNegro, Unidades));
            }
        }

        private Canvas NuevoLienzo(string nombre, Transform cabeza)
        {
            var raiz = new GameObject(nombre, typeof(RectTransform));
            raiz.SetActive(false);
            raiz.layer = CapaUI;
            raiz.AddComponent<IgnorarOcultarVR>();
            raiz.transform.SetParent(cabeza, false);
            Canvas c = raiz.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.sortingOrder = OrdenLienzo;
            var rt = (RectTransform)raiz.transform;
            rt.sizeDelta = new Vector2(Unidades, Unidades);
            rt.pivot = new Vector2(0.5f, 0.5f);
            return c;
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

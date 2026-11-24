using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.VR.Menus;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR51: apuntar con el gatillo IZQUIERDO las armas largas de bala. Pistola y revólver se quedan a una mano, sin mira (no cambian).
    /// <list type="bullet">
    /// <item>Subfusil, fusil y escopeta: con el gatillo izquierdo apretado sale una mira holográfica EN el arma (un vidrio sobre el cañón, no en
    ///   pantalla), sin zoom. La retícula está colimada como una de verdad: se ve donde la línea paralela al cañón que sale del ojo cruza el vidrio
    ///   (<see cref="ReglaMiraVR.PuntoColimado"/>), así que al alinear el ojo con el arma marca a dónde va la bala. El gatillo derecho dispara.</item>
    /// <item>Francotirador: igual, pero la mira es un visor con aumento (una cámara en el cañón dibuja en el visor). El stick derecho en vertical
    ///   hace zoom mientras el visor está activo (<see cref="ReglaMiraVR.Zoom"/>); ese rato el stick derecho no recentra y
    ///   <see cref="BloqueaGiro"/> avisa que no debe girar.</item>
    /// </list>
    /// El lanzagranadas (arco) y el plasma (carga) con el gatillo izquierdo los hace <see cref="ArmasEspecialesVR"/>. Todo con UI (sprites de
    /// <see cref="SpritesMiraVR"/>) en un Canvas World Space colgado del arma en la mano. Corre después de <see cref="PistolaVR"/>, que coloca el
    /// arma. Lo agrega <see cref="ArranqueCombateVR"/>.
    /// </summary>
    [DefaultExecutionOrder(125)]
    public class MiraArmaVR : MonoBehaviour
    {
        /// <summary>Qué mira lleva un arma en VR.</summary>
        public enum TipoMira
        {
            Ninguna,
            Holografica,
            Visor
        }

        /// <summary>El vidrio de la mira va esto (m) por encima del cañón.</summary>
        public const float AlturaMira = 0.055f;
        /// <summary>Dónde va el vidrio entre la empuñadura (0) y la boca (1).</summary>
        public const float FraccionMira = 0.35f;
        /// <summary>Ancho del vidrio holográfico (m).</summary>
        public const float AnchoVidrio = 0.055f;
        /// <summary>Ancho del visor del francotirador (m).</summary>
        public const float AnchoVisor = 0.05f;
        /// <summary>Unidades del Canvas a lo ancho del vidrio.</summary>
        public const float UnidadesVidrio = 100f;
        /// <summary>Lado de la textura del visor (px).</summary>
        public const int ResolucionVisor = 512;
        private const int ProfundidadVisor = 16;
        private const float CercaVisor = 0.05f;
        private const float LejosVisor = 1500f;
        private const float TamanoReticula = 16f;
        private const float TamanoPunto = 4f;

        private static readonly Color ColorReticula = new Color(1f, 0.22f, 0.18f, 0.95f);
        private static readonly Color ColorVidrio = new Color(0.30f, 0.92f, 0.90f, 0.35f);
        private static readonly Color ColorCruz = new Color(0.05f, 0.05f, 0.05f, 0.95f);

        private static bool bloqueaGiro;

        private readonly ReglaMiraVR regla = new ReglaMiraVR();
        private AdaptadorVR adaptador;
        private ArmasAPie armas;
        private PistolaVR pistolaVR;
        private CambioArmaStickVR stick;

        private Canvas lienzo;
        private RectTransform reticula;
        private GameObject holografica;
        private GameObject visor;
        private RawImage imagenVisor;
        private Camera camaraVisor;
        private RenderTexture texturaVisor;

        /// <summary>
        /// True mientras el visor del francotirador está activo: el stick derecho en vertical es el zoom. El giro (AdaptadorVR) debería no girar
        /// mientras sea true.
        /// </summary>
        public static bool BloqueaGiro => bloqueaGiro;

        /// <summary>La mira que se ve ahora (Ninguna si no se apunta).</summary>
        public TipoMira Activa { get; private set; }

        /// <summary>True si la retícula holográfica se ve en el vidrio (el ojo está alineado con el arma).</summary>
        public bool ReticulaVisible => reticula != null && reticula.gameObject.activeInHierarchy;

        /// <summary>El campo de visión del visor del francotirador (°).</summary>
        public float Fov => regla.Fov;

        /// <summary>La cámara del visor (null si no se creó).</summary>
        public Camera CamaraVisor => camaraVisor;

        /// <summary>El Canvas de la mira (null si no se creó).</summary>
        public Canvas Lienzo => lienzo;

        /// <summary>La mira de cada arma en VR.</summary>
        public static TipoMira MiraDe(ArmaAPie arma)
        {
            switch (arma)
            {
                case ArmaAPie.Subfusil:
                case ArmaAPie.Fusil:
                case ArmaAPie.Escopeta:
                    return TipoMira.Holografica;
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
            if (camaraVisor != null)
            {
                Destroy(camaraVisor.gameObject);
            }
            if (texturaVisor != null)
            {
                texturaVisor.Release();
                Destroy(texturaVisor);
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
            Transform arma = pistolaVR.Arma;
            Transform boca = pistolaVR.Boca;
            bool apunta = !bloqueado && tipo != TipoMira.Ninguna && pistolaVR.ArmaVisible && arma != null && boca != null
                && EntradaVR.Sostenido(EntradaVR.GatilloIzquierdo);
            if (!apunta)
            {
                Ocultar();
                return;
            }
            Mostrar(tipo, arma, boca);
            if (tipo == TipoMira.Visor)
            {
                regla.AvanzarZoom(EntradaVR.Eje(EntradaVR.StickDerecho).y, Time.unscaledDeltaTime);
                camaraVisor.fieldOfView = regla.Fov;
            }
            else
            {
                MoverReticula(boca);
            }
        }

        private void Mostrar(TipoMira tipo, Transform arma, Transform boca)
        {
            Asegurar();
            Activa = tipo;
            bloqueaGiro = tipo == TipoMira.Visor;
            if (stick != null)
            {
                stick.ZoomActivo = bloqueaGiro;
            }
            if (lienzo.transform.parent != arma)
            {
                lienzo.transform.SetParent(arma, false);
            }
            Vector3 bocaLocal = arma.InverseTransformPoint(boca.position);
            lienzo.transform.localPosition = new Vector3(bocaLocal.x, bocaLocal.y + AlturaMira, bocaLocal.z * FraccionMira);
            lienzo.transform.localRotation = Quaternion.Inverse(arma.rotation) * boca.rotation; // de frente al cañón
            float ancho = tipo == TipoMira.Visor ? AnchoVisor : AnchoVidrio;
            lienzo.transform.localScale = Vector3.one * (ancho / UnidadesVidrio);
            if (!lienzo.gameObject.activeSelf)
            {
                lienzo.gameObject.SetActive(true);
            }
            holografica.SetActive(tipo == TipoMira.Holografica);
            visor.SetActive(tipo == TipoMira.Visor);
            if (tipo == TipoMira.Visor)
            {
                camaraVisor.transform.SetPositionAndRotation(boca.position, boca.rotation); // sigue al cañón (aquí, después de que PistolaVR lo coloca)
                camaraVisor.enabled = true;
            }
            else if (camaraVisor.enabled)
            {
                camaraVisor.enabled = false;
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
            if (lienzo != null && lienzo.gameObject.activeSelf)
            {
                lienzo.gameObject.SetActive(false);
            }
            if (camaraVisor != null && camaraVisor.enabled)
            {
                camaraVisor.enabled = false;
            }
        }

        // La retícula colimada: donde la línea paralela al cañón que sale del ojo cruza el vidrio; fuera del vidrio no se ve.
        private void MoverReticula(Transform boca)
        {
            Camera cabeza = adaptador.Camara;
            if (cabeza == null)
            {
                reticula.gameObject.SetActive(false);
                return;
            }
            Transform plano = lienzo.transform;
            bool ve = ReglaMiraVR.PuntoColimado(cabeza.transform.position, boca.forward, plano.position, plano.forward, out Vector3 punto);
            Vector3 local = plano.InverseTransformPoint(punto);
            Vector2 enVidrio = new Vector2(local.x, local.y);
            ve &= ReglaMiraVR.DentroDelVidrio(enVidrio, UnidadesVidrio * 0.5f - TamanoReticula * 0.5f);
            reticula.gameObject.SetActive(ve);
            if (ve)
            {
                reticula.anchoredPosition = enVidrio;
            }
        }

        // Crea lo que falte. El Canvas cuelga del arma: si la copia del arma se destruye, se vuelve a armar (la textura y la cámara se reusan).
        private void Asegurar()
        {
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
                camaraVisor.enabled = false;
            }
            if (lienzo != null)
            {
                return;
            }
            var raiz = new GameObject("MiraArmaVR", typeof(RectTransform));
            raiz.SetActive(false);
            raiz.AddComponent<IgnorarOcultarVR>();
            lienzo = raiz.AddComponent<Canvas>();
            lienzo.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)raiz.transform;
            rt.sizeDelta = new Vector2(UnidadesVidrio, UnidadesVidrio);
            rt.pivot = new Vector2(0.5f, 0.5f);

            // Holográfica: el vidrio (un anillo tenue) y la retícula roja (anillo con punto) que se mueve dentro.
            holografica = Hijo(rt, "Holografica", UnidadesVidrio).gameObject;
            Imagen(holografica.transform, "Vidrio", SpritesMiraVR.Anillo, ColorVidrio, UnidadesVidrio);
            reticula = Imagen(holografica.transform, "Reticula", SpritesMiraVR.Anillo, ColorReticula, TamanoReticula);
            Imagen(reticula, "Punto", SpritesMiraVR.Punto, ColorReticula, TamanoPunto);

            // Visor: la imagen de la cámara del cañón, recortada en círculo, con la cruz y un borde.
            visor = Hijo(rt, "Visor", UnidadesVidrio).gameObject;
            RectTransform mascara = Imagen(visor.transform, "Mascara", SpritesMiraVR.Circulo, Color.white, UnidadesVidrio);
            mascara.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            RectTransform cuadro = Hijo(mascara, "Imagen", UnidadesVidrio);
            imagenVisor = cuadro.gameObject.AddComponent<RawImage>();
            imagenVisor.texture = texturaVisor;
            imagenVisor.raycastTarget = false;
            Imagen(mascara, "Cruz", SpritesMiraVR.Cruz, ColorCruz, UnidadesVidrio);
            Imagen(mascara, "Centro", SpritesMiraVR.Punto, ColorReticula, TamanoPunto);
            Imagen(visor.transform, "Borde", SpritesMiraVR.Anillo, ColorVidrio, UnidadesVidrio);
        }

        private static RectTransform Hijo(Transform padre, string nombre, float lado)
        {
            var g = new GameObject(nombre, typeof(RectTransform));
            g.transform.SetParent(padre, false);
            var r = (RectTransform)g.transform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(lado, lado);
            r.anchoredPosition = Vector2.zero;
            return r;
        }

        private static RectTransform Imagen(Transform padre, string nombre, Sprite sprite, Color color, float lado)
        {
            RectTransform r = Hijo(padre, nombre, lado);
            Image i = r.gameObject.AddComponent<Image>();
            i.sprite = sprite;
            i.color = color;
            i.raycastTarget = false;
            return r;
        }
    }
}

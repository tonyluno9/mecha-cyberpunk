using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.MenuPausa;
using MechaCyberpunk.VR.Menus;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.VR.Guia
{
    /// <summary>
    /// VR20/VR31: la guía de controles de VR. Un panel del mundo a <see cref="Distancia"/> m frente a la cabeza, quieto, con el estilo H10 del juego
    /// (TrazoH10: fondo oscuro, línea turquesa, letra Rajdhani) y páginas con las imágenes de Assets/Arte/UI/ControlesVR (<see cref="TextosGuiaVR.Paginas"/>):
    /// botones → sacar pistola → arma larga → recargar → prótesis piernas → prótesis brazos. En la de botones cada etiqueta va junto a su punto cian
    /// con una línea fina; en las de gestos, una frase corta abajo. ANTERIOR / SIGUIENTE / CERRAR con el láser y el gatillo derecho; el stick derecho
    /// a los lados cambia de página; B, Y o Menú/Esc la cierran.
    /// Mientras está abierta va en <see cref="PilaPantallas"/> (el láser se ve, el gatillo no dispara y Menú no abre la pausa). Abierta sola
    /// (primera vez, <see cref="ArranqueGuiaVR"/>) detiene el tiempo y el control del jugador; abierta desde la pausa (botón CONTROLES que se
    /// agrega a la pausa solo en VR) esconde la pausa y al cerrarse vuelve a ella.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class GuiaControlesVR : MonoBehaviour
    {
        /// <summary>PlayerPrefs: 1 cuando la guía ya salió sola una vez.</summary>
        public const string ClaveVista = "vr_guia_vista";
        /// <summary>Distancia del panel a la cabeza (m).</summary>
        public const float Distancia = 1f;
        /// <summary>El panel queda un poco abajo de los ojos (m).</summary>
        public const float Bajada = 0.12f;
        public const float AnchoPx = 1600f;
        public const float AltoPx = 900f;
        /// <summary>Tamaño de un px del panel en el mundo (m): 1600 px = 1.12 m.</summary>
        public const float MetrosPorPx = 0.0007f;
        /// <summary>La imagen de cada página (16:9, px del panel).</summary>
        public const float AnchoImagenPx = 1200f;
        public const float AltoImagenPx = 675f;

        private const float CentroImagenYPx = 30f;
        private const float AnchoEtiquetaIzqPx = 380f;
        private const float AnchoEtiquetaDerPx = 440f;
        private const float AltoEtiquetaPx = 100f;
        private const float SeparacionLineaPx = 8f;
        private const float YFrasePx = -338f;
        private const float YBotonesPx = -405f;
        private const float AnchoBotonPx = 300f;
        private const float AltoBotonPx = 56f;
        private const float MargenPared = 0.1f;
        private const float DistanciaMinima = 0.5f;
        private const float AlfaFondo = 0.94f;
        private const float AlcanceRayo = 5f;
        private const float UmbralStick = 0.7f;
        private const float RearmeStick = 0.3f;
        private static readonly Vector2 GrosorContorno = new Vector2(1.5f, -1.5f);

        private readonly List<Text> etiquetas = new List<Text>();
        private readonly List<Sprite> creados = new List<Sprite>();
        private readonly RaycastHit[] golpes = new RaycastHit[16];
        private Canvas canvas;
        private Image imagen;
        private Text tituloPagina;
        private Text frase;
        private GameObject grupoEtiquetas;
        private BotonCyberfix cerrar;
        private BotonCyberfix siguiente;
        private BotonCyberfix anterior;
        private PantallaPausa pausa;
        private BotonCyberfix botonPausa;
        private CanvasGroup grupoPausa;
        private Personaje.Movimiento.ControladorAPie congelado;
        private Transform manoLaser;
        private LineRenderer laserMenus;
        private int cuadroAbierta;
        private int pagina;
        private bool stickArmado = true;
        private bool cerrando;

        /// <summary>La guía abierta (null si no hay).</summary>
        public static GuiaControlesVR Actual { get; private set; }

        public static bool Abierta => Actual != null;

        /// <summary>Con el simulador de XR la guía no sale sola (las pruebas lo encienden para probarla).</summary>
        public static bool MostrarEnSimulador { get; set; }

        public Canvas Canvas => canvas;
        public BotonCyberfix BotonCerrar => cerrar;
        public BotonCyberfix BotonSiguiente => siguiente;
        public BotonCyberfix BotonAnterior => anterior;
        /// <summary>Las etiquetas de la página de botones.</summary>
        public IReadOnlyList<Text> Etiquetas => etiquetas;
        /// <summary>La imagen de la página actual.</summary>
        public Image Imagen => imagen;
        /// <summary>La frase de la página actual (vacía en la de botones).</summary>
        public string Frase => frase != null ? frase.text : string.Empty;
        /// <summary>La página actual (0 = botones).</summary>
        public int Pagina => pagina;
        public static int Paginas => TextosGuiaVR.Paginas.Length;
        public bool DesdePausa => pausa != null;
        /// <summary>True si el rayo del control derecho apunta a CERRAR este cuadro.</summary>
        public bool CerrarApuntado { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Actual = null;
            MostrarEnSimulador = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Suscribir()
        {
            Pausa.Cambio -= AlCambiarPausa;
            Pausa.Cambio += AlCambiarPausa;
        }

        // La pausa se abre: en VR lleva el botón CONTROLES (antes de SALIR AL TÍTULO); fuera de VR se quita si quedó.
        private static void AlCambiarPausa(bool activa)
        {
            if (!activa)
            {
                return;
            }
            foreach (PantallaPausa p in FindObjectsByType<PantallaPausa>(FindObjectsSortMode.None))
            {
                if (ModoVR.Activo)
                {
                    PantallaPausa esta = p;
                    BotonCyberfix boton = null;
                    boton = p.AgregarBoton(TextosGuiaVR.BotonPausa, () => Abrir(esta, boton));
                }
                else
                {
                    p.QuitarBoton(TextosGuiaVR.BotonPausa);
                }
            }
        }

        /// <summary>Abre la guía frente a la cabeza (o devuelve la que ya está). Desde la pausa, la esconde mientras tanto. Null sin VR.</summary>
        public static GuiaControlesVR Abrir(PantallaPausa desdePausa = null, BotonCyberfix boton = null)
        {
            if (!ModoVR.Activo)
            {
                return null;
            }
            if (Actual != null)
            {
                return Actual;
            }
            var objeto = new GameObject("GuiaControlesVR");
            GuiaControlesVR guia = objeto.AddComponent<GuiaControlesVR>();
            guia.Construir(desdePausa, boton);
            return guia;
        }

        /// <summary>Cierra la guía (si es la de la pausa, vuelve a la pausa).</summary>
        public void Cerrar()
        {
            if (cerrando)
            {
                return;
            }
            cerrando = true;
            Soltar();
            Sonidos.Reproducir("UI_MENU_CERRAR");
            Destroy(gameObject);
        }

        /// <summary>Pasa a la página siguiente (da la vuelta).</summary>
        public void IrSiguiente() => IrA(pagina + 1);

        /// <summary>Pasa a la página anterior (da la vuelta).</summary>
        public void IrAnterior() => IrA(pagina - 1);

        /// <summary>Muestra la página <paramref name="indice"/> (da la vuelta en los dos sentidos).</summary>
        public void IrA(int indice)
        {
            int total = Paginas;
            if (total == 0)
            {
                return;
            }
            int nueva = ((indice % total) + total) % total;
            bool cambio = nueva != pagina;
            pagina = nueva;
            MostrarPagina();
            if (cambio)
            {
                Sonidos.Reproducir(SonidoPagina);
            }
        }

        private const string SonidoPagina = "UI_MOVER";

        private void Construir(PantallaPausa desdePausa, BotonCyberfix boton)
        {
            Actual = this;
            cuadroAbierta = Time.frameCount;
            Transform cabeza = Cabeza();
            Colocar(cabeza);
            ArmarPanel();
            MostrarPagina();

            PilaPantallas.Abrir(this);
            if (desdePausa != null && desdePausa.Abierta)
            {
                pausa = desdePausa;
                botonPausa = boton;
                pausa.AbrirEncima(boton);
                Canvas lienzoPausa = LienzoDe(pausa);
                if (lienzoPausa != null)
                {
                    grupoPausa = lienzoPausa.GetComponent<CanvasGroup>();
                    if (grupoPausa == null)
                    {
                        grupoPausa = lienzoPausa.gameObject.AddComponent<CanvasGroup>();
                    }
                    grupoPausa.alpha = 0f;
                    grupoPausa.blocksRaycasts = false;
                    grupoPausa.interactable = false;
                }
            }
            else
            {
                // Sola (primera vez): el juego se detiene como en la pausa.
                ControlTiempo.Pedir(this, 0f);
                AdaptadorVR a = AdaptadorVR.Actual;
                if (a != null && a.Controlador != null && a.Controlador.ControlActivo)
                {
                    congelado = a.Controlador;
                    congelado.ActivarControl(false);
                }
            }
            Sonidos.Reproducir("UI_MENU_ABRIR");
            Debug.Log("[VR] Guía de controles abierta" + (pausa != null ? " desde la pausa" : string.Empty) + ": " + Paginas + " páginas, "
                + etiquetas.Count + " etiquetas" + (imagen != null && imagen.sprite != null ? "." : " (falta la imagen: corre ImportarGuiaVR31.Aplicar)."));
        }

        private static Canvas LienzoDe(PantallaPausa p)
        {
            Canvas primero = null;
            foreach (Canvas c in p.GetComponentsInChildren<Canvas>(true))
            {
                if (c.name == "CanvasPausa")
                {
                    return c;
                }
                if (primero == null)
                {
                    primero = c;
                }
            }
            return primero;
        }

        private static Transform Cabeza()
        {
            AdaptadorVR a = AdaptadorVR.Actual;
            if (a != null && a.Armado && a.Camara != null)
            {
                return a.Camara.transform;
            }
            if (MenusVR.Actual != null && MenusVR.Actual.Cabeza != null)
            {
                return MenusVR.Actual.Cabeza;
            }
            Camera principal = Camera.main;
            return principal != null ? principal.transform : null;
        }

        private static Transform ManoDerecha()
        {
            if (MenusVR.Actual != null && MenusVR.Actual.ManoLaser != null)
            {
                return MenusVR.Actual.ManoLaser;
            }
            AdaptadorVR a = AdaptadorVR.Actual;
            return a != null && a.ManoDerecha != null ? a.ManoDerecha.transform : null;
        }

        // Frente a la cabeza, en el plano (sin inclinar), un poco abajo; más cerca si hay pared.
        private void Colocar(Transform cabeza)
        {
            if (cabeza == null)
            {
                return;
            }
            Vector3 frente = cabeza.forward;
            frente.y = 0f;
            if (frente.sqrMagnitude < 1e-4f)
            {
                frente = cabeza.up;
                frente.y = 0f;
            }
            frente = frente.sqrMagnitude > 1e-6f ? frente.normalized : Vector3.forward;
            float distancia = Distancia;
            Transform raizJugador = AdaptadorVR.Actual != null ? AdaptadorVR.Actual.transform.root : null;
            int n = Physics.RaycastNonAlloc(cabeza.position, frente, golpes, Distancia + MargenPared, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (raizJugador != null && golpes[i].collider.transform.root == raizJugador)
                {
                    continue;
                }
                distancia = Mathf.Min(distancia, Mathf.Max(DistanciaMinima, golpes[i].distance - MargenPared));
            }
            transform.SetPositionAndRotation(cabeza.position + frente * distancia + Vector3.down * Bajada, Quaternion.LookRotation(frente, Vector3.up));
        }

        // ---- Panel ----

        private void ArmarPanel()
        {
            var objeto = new GameObject("CanvasGuiaVR", typeof(RectTransform));
            objeto.transform.SetParent(transform, false);
            canvas = objeto.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 70;
            CanvasScaler escala = objeto.AddComponent<CanvasScaler>();
            escala.dynamicPixelsPerUnit = 2f;
            var rect = (RectTransform)objeto.transform;
            rect.sizeDelta = new Vector2(AnchoPx, AltoPx);
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * MetrosPorPx;

            Image fondo = TrazoH10.Panel(rect, "Fondo", TrazoH10.TurquesaTenue, TrazoH10.ConAlfa(TrazoH10.FondoPantalla, AlfaFondo));
            TrazoH10.Estirar(fondo.rectTransform);

            float mitadX = AnchoPx * 0.5f;
            float mitadY = AltoPx * 0.5f;
            float margen = TrazoH10.MargenPantalla;
            tituloPagina = Texto("Titulo", TextosGuiaVR.Titulo, TrazoH10.TamanoSeccion, TrazoH10.Texto, new Vector2(-mitadX + margen, mitadY - 40f),
                new Vector2(0f, 0.5f), new Vector2(900f, 50f), TextAnchor.MiddleLeft);
            Texto("Ayuda", TextosGuiaVR.AyudaCerrar, TrazoH10.TamanoMicro, TrazoH10.TextoTenue, new Vector2(mitadX - margen, mitadY - 40f),
                new Vector2(1f, 0.5f), new Vector2(700f, 40f), TextAnchor.MiddleRight);

            // La imagen de la página, con un marco fino.
            imagen = TrazoH10.Imagen(rect, "ImagenPagina", Color.white);
            imagen.preserveAspect = true;
            imagen.raycastTarget = false;
            TrazoH10.Colocar(imagen.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, CentroImagenYPx), new Vector2(AnchoImagenPx, AltoImagenPx));
            Image marco = TrazoH10.Panel(rect, "MarcoImagen", TrazoH10.TurquesaTenue, Color.clear);
            marco.raycastTarget = false;
            TrazoH10.Colocar(marco.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, CentroImagenYPx), new Vector2(AnchoImagenPx + 4f, AltoImagenPx + 4f));

            // Etiquetas de la página de botones.
            var grupo = new GameObject("EtiquetasBotones", typeof(RectTransform));
            grupo.transform.SetParent(rect, false);
            TrazoH10.Estirar((RectTransform)grupo.transform);
            grupoEtiquetas = grupo;
            CrearEtiquetas((RectTransform)grupo.transform);

            // La frase de los gestos, abajo de la imagen.
            frase = Texto("Frase", string.Empty, TrazoH10.TamanoFila, TrazoH10.Turquesa, new Vector2(0f, YFrasePx), new Vector2(0.5f, 0.5f),
                new Vector2(AnchoPx - 2f * margen, 40f), TextAnchor.MiddleCenter);
            Contorno(frase);

            anterior = CrearBoton(rect, TextosGuiaVR.Anterior, IrAnterior, -370f);
            siguiente = CrearBoton(rect, TextosGuiaVR.Siguiente, IrSiguiente, 0f);
            cerrar = CrearBoton(rect, TextosGuiaVR.Cerrar, Cerrar, 370f);
            VestidoH10.Fuente(rect);
        }

        private BotonCyberfix CrearBoton(RectTransform padre, string texto, System.Action accion, float x)
        {
            BotonCyberfix boton = KitUI.CrearBoton(padre, texto, accion);
            boton.Texto.alignment = TextAnchor.MiddleCenter;
            VestidoH10.Boton(boton);
            TrazoH10.Punto((RectTransform)boton.transform, new Vector2(0.5f, 0.5f), new Vector2(x, YBotonesPx), new Vector2(AnchoBotonPx, AltoBotonPx));
            return boton;
        }

        // Px del panel (centro = 0,0) de un punto normalizado de la imagen.
        private static Vector2 EnPanel(Vector2 normalizado)
        {
            return new Vector2((normalizado.x - 0.5f) * AnchoImagenPx, CentroImagenYPx + (normalizado.y - 0.5f) * AltoImagenPx);
        }

        private void CrearEtiquetas(RectTransform padre)
        {
            Vector2 esquina = new Vector2(AnchoPx * 0.5f, AltoPx * 0.5f); // de centro a abajo-izquierda (TrazoH10.Unir)
            for (int i = 0; i < TextosGuiaVR.Etiquetas.Length; i++)
            {
                var e = TextosGuiaVR.Etiquetas[i];
                Vector2 posicion = EnPanel(e.etiqueta);
                float ancho = e.derecha ? AnchoEtiquetaDerPx : AnchoEtiquetaIzqPx;
                Text t = TrazoH10.Etiqueta(padre, "Etiqueta_" + i, e.texto, TrazoH10.TamanoDato, TrazoH10.Turquesa,
                    e.derecha ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
                TrazoH10.Colocar(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(e.derecha ? 0f : 1f, 0.5f), posicion,
                    new Vector2(ancho, AltoEtiquetaPx));
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                t.raycastTarget = false;
                Contorno(t);
                etiquetas.Add(t);
                if (e.punto.x < 0f)
                {
                    continue;
                }
                Vector2 desde = posicion + new Vector2(e.derecha ? -SeparacionLineaPx : SeparacionLineaPx, 0f);
                Image linea = TrazoH10.Imagen(padre, "Linea_" + i, TrazoH10.ConAlfa(TrazoH10.Turquesa, 0.8f));
                linea.raycastTarget = false;
                TrazoH10.Unir(linea.rectTransform, desde + esquina, EnPanel(e.punto) + esquina);
            }
        }

        private static void Contorno(Text t)
        {
            Outline contorno = t.gameObject.AddComponent<Outline>();
            contorno.effectColor = new Color(0f, 0f, 0f, 0.85f);
            contorno.effectDistance = GrosorContorno;
        }

        private Text Texto(string nombre, string contenido, int tamano, Color color, Vector2 posicion, Vector2 pivote, Vector2 tamanoRect, TextAnchor ancla)
        {
            Text t = TrazoH10.Etiqueta(canvas.transform, nombre, contenido, tamano, color, ancla);
            TrazoH10.Colocar(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pivote, posicion, tamanoRect);
            t.raycastTarget = false;
            return t;
        }

        private void MostrarPagina()
        {
            var p = TextosGuiaVR.Paginas[pagina];
            if (imagen != null)
            {
                imagen.sprite = CargarImagen(p.imagen);
                imagen.color = imagen.sprite != null ? Color.white : TrazoH10.TurquesaFondo;
            }
            if (tituloPagina != null)
            {
                tituloPagina.text = TextosGuiaVR.Titulo + " · " + p.titulo + "   " + (pagina + 1) + "/" + Paginas;
            }
            if (frase != null)
            {
                frase.text = p.frase;
            }
            if (grupoEtiquetas != null)
            {
                grupoEtiquetas.SetActive(pagina == 0);
            }
        }

        // La imagen importada como Sprite (ImportarGuiaVR31); si aún es textura, se hace el sprite aquí.
        private Sprite CargarImagen(string nombre)
        {
            string ruta = TextosGuiaVR.CarpetaImagenes + "/" + nombre;
            Sprite sprite = Resources.Load<Sprite>(ruta);
            if (sprite != null)
            {
                return sprite;
            }
            Texture2D textura = Resources.Load<Texture2D>(ruta);
            if (textura == null)
            {
                Debug.LogWarning("[VR] Guía: falta la imagen " + ruta + " en Resources (Assets/Arte/UI/ControlesVR/Resources).");
                return null;
            }
            sprite = Sprite.Create(textura, new Rect(0f, 0f, textura.width, textura.height), new Vector2(0.5f, 0.5f));
            creados.Add(sprite);
            return sprite;
        }

        // ---- Cada cuadro ----

        private void Update()
        {
            if (!ModoVR.Activo)
            {
                Cerrar();
                return;
            }
            if (Time.frameCount == cuadroAbierta)
            {
                return;
            }
            BotonCyberfix apuntado = BotonApuntado();
            CerrarApuntado = apuntado == cerrar;
            if (apuntado != null && !apuntado.EstaElegido)
            {
                apuntado.Elegir();
            }
            if (apuntado != null && EntradaVR.Pulso(EntradaVR.GatilloDerecho))
            {
                apuntado.Pulsar(); // CERRAR llama a Cerrar; ANTERIOR y SIGUIENTE cambian de página
                return;
            }
            Keyboard teclado = Keyboard.current;
            bool esc = teclado != null && teclado.escapeKey.wasPressedThisFrame; // Menú del control izquierdo = Esc (MenusVR)
            if (EntradaVR.Pulso(EntradaVR.BotonB) || EntradaVR.Pulso(EntradaVR.BotonY) || esc)
            {
                Cerrar();
                return;
            }
            // El stick derecho a los lados cambia de página (y las flechas del teclado, para probar sin visor).
            float x = EntradaVR.Eje(EntradaVR.StickDerecho).x;
            if (stickArmado && Mathf.Abs(x) > UmbralStick)
            {
                stickArmado = false;
                IrA(pagina + (x > 0f ? 1 : -1));
            }
            else if (Mathf.Abs(x) < RearmeStick)
            {
                stickArmado = true;
            }
            if (teclado != null && teclado.rightArrowKey.wasPressedThisFrame)
            {
                IrSiguiente();
            }
            else if (teclado != null && teclado.leftArrowKey.wasPressedThisFrame)
            {
                IrAnterior();
            }
        }

        private void LateUpdate()
        {
            // El láser de los menús no conoce este panel (es del mundo): se recorta aquí, donde toca el panel.
            if (ApuntaAlPanel(out float distancia))
            {
                LineRenderer laser = LaserMenus();
                if (laser != null && laser.enabled && !laser.useWorldSpace)
                {
                    laser.SetPosition(1, Vector3.forward * distancia);
                }
            }
        }

        private LineRenderer LaserMenus()
        {
            Transform mano = ManoDerecha();
            if (mano != manoLaser || (laserMenus != null && laserMenus.transform.parent != mano))
            {
                manoLaser = mano;
                Transform t = mano != null ? mano.Find("LaserVR") : null;
                laserMenus = t != null ? t.GetComponent<LineRenderer>() : null;
            }
            return laserMenus;
        }

        // El rayo del control derecho contra el plano del panel: dónde lo toca (px del panel) y a qué distancia.
        private bool RayoAlPanel(out Vector2 px, out float distancia)
        {
            px = Vector2.zero;
            distancia = 0f;
            Transform mano = ManoDerecha();
            if (mano == null || canvas == null)
            {
                return false;
            }
            Transform lienzo = canvas.transform;
            var plano = new Plane(-lienzo.forward, lienzo.position);
            var rayo = new Ray(mano.position, mano.forward);
            if (!plano.Raycast(rayo, out distancia) || distancia > AlcanceRayo)
            {
                return false;
            }
            Vector3 local = lienzo.InverseTransformPoint(rayo.GetPoint(distancia));
            px = new Vector2(local.x, local.y);
            return Mathf.Abs(px.x) <= AnchoPx * 0.5f && Mathf.Abs(px.y) <= AltoPx * 0.5f;
        }

        private bool ApuntaAlPanel(out float distancia) => RayoAlPanel(out _, out distancia);

        /// <summary>True si el rayo del control derecho toca el botón CERRAR.</summary>
        public bool ApuntaACerrar(out float distancia) => ApuntaA(cerrar, out distancia);

        /// <summary>True si el rayo del control derecho toca el botón dado.</summary>
        public bool ApuntaA(BotonCyberfix boton, out float distancia)
        {
            if (boton == null || !RayoAlPanel(out Vector2 px, out distancia))
            {
                distancia = 0f;
                return false;
            }
            var rect = (RectTransform)boton.transform;
            Vector3 enBoton = rect.InverseTransformPoint(canvas.transform.TransformPoint(new Vector3(px.x, px.y, 0f)));
            return rect.rect.Contains(new Vector2(enBoton.x, enBoton.y));
        }

        // El botón (ANTERIOR, SIGUIENTE o CERRAR) que toca el rayo; null si ninguno.
        private BotonCyberfix BotonApuntado()
        {
            if (ApuntaA(cerrar, out _))
            {
                return cerrar;
            }
            if (ApuntaA(siguiente, out _))
            {
                return siguiente;
            }
            return ApuntaA(anterior, out _) ? anterior : null;
        }

        // Devuelve el tiempo, el control y la pausa como estaban.
        private void Soltar()
        {
            ControlTiempo.Soltar(this);
            PilaPantallas.Cerrar(this);
            if (congelado != null)
            {
                congelado.ActivarControl(true);
                congelado = null;
            }
            if (grupoPausa != null)
            {
                grupoPausa.alpha = 1f;
                grupoPausa.blocksRaycasts = true;
                grupoPausa.interactable = true;
                grupoPausa = null;
            }
            if (pausa != null)
            {
                pausa.VolverDeEncima();
                if (botonPausa != null)
                {
                    botonPausa.Elegir();
                }
                pausa = null;
            }
            if (Actual == this)
            {
                Actual = null;
            }
        }

        private void OnDestroy()
        {
            Soltar();
            foreach (Sprite s in creados)
            {
                if (s != null)
                {
                    Destroy(s);
                }
            }
            creados.Clear();
        }
    }
}

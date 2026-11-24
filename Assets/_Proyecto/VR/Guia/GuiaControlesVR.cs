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
    /// VR20: la guía de controles de VR. Un panel del mundo a <see cref="Distancia"/> m frente a la cabeza, quieto, con el estilo H10 del juego
    /// (TrazoH10: fondo oscuro, línea turquesa, letra Rajdhani) y, delante, los modelos 3D REALES de los dos controles (los prefabs de los
    /// Starter Assets de XRI que usa el rig, <see cref="RecursosVR"/>; sin sus scripts: solo transforms, mallas y renderers) con un vaivén lento.
    /// Cada botón lleva una etiqueta (<see cref="TextosGuiaVR"/>, el reparto de VR19) y una línea (LineRenderer) hasta la pieza del modelo; abajo,
    /// los gestos de las prótesis. Se cierra con B, con el gatillo derecho apuntando a CERRAR o con Menú/Esc.
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
        /// <summary>El lado mayor de cada control en la guía (m).</summary>
        public const float TamanoModelo = 0.26f;
        /// <summary>Cuánto se adelantan los modelos hacia la cabeza desde el panel (m).</summary>
        public const float AdelantoModelos = 0.1f;

        private const float CentroModeloPx = 250f;
        private const float AlturaModeloPx = 30f;
        private const float ColumnaEtiquetasPx = 430f;
        private const float AnchoEtiquetaPx = 360f;
        private const float AltoEtiquetaPx = 64f;
        private const float PrimeraEtiquetaPx = 250f;
        private const float PasoEtiquetaPx = 76f;
        private const float GradosInclinacion = 20f;
        private const float GradosVaiven = 12f;
        private const float SegundosVaiven = 7f;
        private const float GrosorLinea = 0.0012f;
        private const float MargenPared = 0.1f;
        private const float DistanciaMinima = 0.5f;
        private const float AlfaFondo = 0.92f;
        private const float AlcanceRayo = 5f;
        private static readonly Color ColorLinea = new Color(0.361f, 0.890f, 0.839f, 0.9f);

        private sealed class LineaGuia
        {
            public LineRenderer Linea;
            public Transform Ancla;
            public Vector3 Local;
            public Vector2 OrigenPx;
            public bool Encontrada;
        }

        private readonly List<Text> etiquetas = new List<Text>();
        private readonly List<GameObject> modelos = new List<GameObject>();
        private readonly List<Renderer> renderers = new List<Renderer>();
        private readonly List<LineaGuia> lineas = new List<LineaGuia>();
        private readonly List<Transform> soportes = new List<Transform>();
        private readonly RaycastHit[] golpes = new RaycastHit[16];
        private Canvas canvas;
        private BotonCyberfix cerrar;
        private Material materialLinea;
        private PantallaPausa pausa;
        private BotonCyberfix botonPausa;
        private CanvasGroup grupoPausa;
        private Personaje.Movimiento.ControladorAPie congelado;
        private Transform manoLaser;
        private LineRenderer laserMenus;
        private int cuadroAbierta;
        private float inicio;
        private bool cerrando;

        /// <summary>La guía abierta (null si no hay).</summary>
        public static GuiaControlesVR Actual { get; private set; }

        public static bool Abierta => Actual != null;

        /// <summary>Con el simulador de XR la guía no sale sola (las pruebas lo encienden para probarla).</summary>
        public static bool MostrarEnSimulador { get; set; }

        public Canvas Canvas => canvas;
        public BotonCyberfix BotonCerrar => cerrar;
        public IReadOnlyList<Text> Etiquetas => etiquetas;
        /// <summary>Los dos modelos de los controles (izquierdo y derecho), instancias de los prefabs reales.</summary>
        public IReadOnlyList<GameObject> Modelos => modelos;
        public IReadOnlyList<Renderer> RenderersModelos => renderers;
        public int Lineas => lineas.Count;
        /// <summary>Cuántas líneas van a una pieza con nombre del modelo (el resto usa el punto de respaldo).</summary>
        public int LineasConPieza { get; private set; }
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

        private void Construir(PantallaPausa desdePausa, BotonCyberfix boton)
        {
            Actual = this;
            cuadroAbierta = Time.frameCount;
            inicio = Time.unscaledTime;
            Transform cabeza = Cabeza();
            Colocar(cabeza);
            ArmarPanel();
            RecursosVR recursos = RecursosVR.Cargar();
            CrearModelo(false, recursos != null ? recursos.controlIzquierdo : null);
            CrearModelo(true, recursos != null ? recursos.controlDerecho : null);
            AplicarVaiven(0f);
            CrearEtiquetas(false);
            CrearEtiquetas(true);
            ActualizarLineas();

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
            Debug.Log("[VR] Guía de controles abierta" + (pausa != null ? " desde la pausa" : string.Empty) + ": " + modelos.Count
                + " modelos, " + etiquetas.Count + " etiquetas, " + LineasConPieza + "/" + lineas.Count + " líneas a piezas con nombre.");
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
            Texto("Titulo", TextosGuiaVR.Titulo, TrazoH10.TamanoTitulo, TrazoH10.Texto, new Vector2(-mitadX + margen, mitadY - 50f), new Vector2(0f, 0.5f),
                new Vector2(800f, 60f), TextAnchor.MiddleLeft);
            Texto("Subtitulo", TextosGuiaVR.Subtitulo, TrazoH10.TamanoDato, TrazoH10.Turquesa, new Vector2(-mitadX + margen + 2f, mitadY - 96f), new Vector2(0f, 0.5f),
                new Vector2(800f, 30f), TextAnchor.MiddleLeft);
            TrazoH10.Recta(rect, "TiraCabecera", TrazoH10.TurquesaTenue, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -118f), new Vector2(-2f * margen, TrazoH10.GrosorLinea));
            Texto("Izquierdo", TextosGuiaVR.Izquierdo, TrazoH10.TamanoDato, TrazoH10.Turquesa, new Vector2(-CentroModeloPx, mitadY - 150f), new Vector2(0.5f, 0.5f),
                new Vector2(320f, 30f), TextAnchor.MiddleCenter);
            Texto("Derecho", TextosGuiaVR.Derecho, TrazoH10.TamanoDato, TrazoH10.Turquesa, new Vector2(CentroModeloPx, mitadY - 150f), new Vector2(0.5f, 0.5f),
                new Vector2(320f, 30f), TextAnchor.MiddleCenter);

            // Gestos de las prótesis, abajo.
            float yGestos = -mitadY + 210f;
            TrazoH10.Recta(rect, "TiraGestos", TrazoH10.TurquesaTenue, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, yGestos + mitadY + 26f), new Vector2(-2f * margen, TrazoH10.GrosorLinea));
            Texto("TituloGestos", TextosGuiaVR.TituloGestos, TrazoH10.TamanoDato, TrazoH10.Turquesa, new Vector2(-mitadX + margen, yGestos), new Vector2(0f, 0.5f),
                new Vector2(AnchoPx - 2f * margen, 32f), TextAnchor.MiddleLeft);
            Texto("GestoPiernas", TextosGuiaVR.GestoPiernas, TrazoH10.TamanoFila, TrazoH10.Amarillo, new Vector2(-mitadX + margen, yGestos - 42f), new Vector2(0f, 0.5f),
                new Vector2(AnchoPx - 2f * margen, 36f), TextAnchor.MiddleLeft);
            Texto("GestoBrazos", TextosGuiaVR.GestoBrazos, TrazoH10.TamanoFila, TrazoH10.Amarillo, new Vector2(-mitadX + margen, yGestos - 82f), new Vector2(0f, 0.5f),
                new Vector2(AnchoPx - 2f * margen, 36f), TextAnchor.MiddleLeft);

            // CERRAR y su ayuda.
            cerrar = KitUI.CrearBoton(rect, TextosGuiaVR.Cerrar, Cerrar);
            cerrar.Texto.alignment = TextAnchor.MiddleCenter;
            VestidoH10.Boton(cerrar);
            TrazoH10.Punto((RectTransform)cerrar.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -mitadY + 52f), new Vector2(360f, 64f));
            Texto("AyudaCerrar", TextosGuiaVR.AyudaCerrar, TrazoH10.TamanoDato, TrazoH10.TextoTenue, new Vector2(200f, -mitadY + 52f), new Vector2(0f, 0.5f),
                new Vector2(500f, 32f), TextAnchor.MiddleLeft);
            VestidoH10.Fuente(rect);
        }

        private Text Texto(string nombre, string contenido, int tamano, Color color, Vector2 posicion, Vector2 pivote, Vector2 tamanoRect, TextAnchor ancla)
        {
            Text t = TrazoH10.Etiqueta(canvas.transform, nombre, contenido, tamano, color, ancla);
            TrazoH10.Colocar(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pivote, posicion, tamanoRect);
            return t;
        }

        // ---- Modelos ----

        // Instancia el prefab real del control sin dejar que despierte ningún script (bajo un padre apagado) y le quita todo lo que no es malla.
        private void CrearModelo(bool derecha, GameObject prefab)
        {
            var soporte = new GameObject(derecha ? "SoporteDerecho" : "SoporteIzquierdo").transform;
            soporte.SetParent(transform, false);
            soportes.Add(soporte);
            if (prefab == null)
            {
                Debug.LogWarning("[VR] Guía: falta el modelo del control " + (derecha ? "derecho" : "izquierdo") + " en RecursosVR.");
                modelos.Add(null);
                return;
            }
            var deposito = new GameObject("Deposito");
            deposito.SetActive(false);
            GameObject modelo = Instantiate(prefab, deposito.transform, false);
            modelo.name = derecha ? "ModeloControlDerecho" : "ModeloControlIzquierdo";
            QuitarScripts(modelo);
            var centro = new GameObject("Centro").transform;
            centro.SetParent(soporte, false);
            modelo.transform.SetParent(centro, false);
            Destroy(deposito);

            // Centro y tamaño medidos en el marco del soporte (aún en el origen, sin girar ni escalar).
            soporte.localPosition = Vector3.zero;
            soporte.localRotation = Quaternion.identity;
            soporte.localScale = Vector3.one;
            Bounds caja = new Bounds();
            bool hay = false;
            foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                renderers.Add(r);
                Bounds b = LocalEn(soporte, r.bounds);
                if (!hay)
                {
                    caja = b;
                    hay = true;
                }
                else
                {
                    caja.Encapsulate(b);
                }
            }
            float lado = hay ? Mathf.Max(caja.size.x, Mathf.Max(caja.size.y, caja.size.z)) : 0f;
            centro.localPosition = hay ? -caja.center : Vector3.zero;
            soporte.localScale = Vector3.one * (lado > 1e-4f ? TamanoModelo / lado : 1f);
            soporte.localPosition = new Vector3((derecha ? CentroModeloPx : -CentroModeloPx) * MetrosPorPx, AlturaModeloPx * MetrosPorPx, -AdelantoModelos);
            modelo.SetActive(true);
            modelos.Add(modelo);
        }

        private static Bounds LocalEn(Transform marco, Bounds mundo)
        {
            Vector3 c = marco.InverseTransformPoint(mundo.center);
            Vector3 e = mundo.extents;
            var b = new Bounds(c, Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                Vector3 esquina = mundo.center + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                b.Encapsulate(marco.InverseTransformPoint(esquina));
            }
            return b;
        }

        // Solo quedan transforms, mallas y renderers: fuera TrackedPoseDriver, los scripts de XRI que animan botones, colliders, etc.
        private static void QuitarScripts(GameObject modelo)
        {
            foreach (MonoBehaviour m in modelo.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (m != null)
                {
                    DestroyImmediate(m);
                }
            }
            foreach (Component c in modelo.GetComponentsInChildren<Component>(true))
            {
                if (c == null || c is Transform || c is MeshFilter || c is Renderer)
                {
                    continue;
                }
                DestroyImmediate(c);
            }
        }

        // El soporte de cada control: de cara a la cabeza (la cara de los botones mirando hacia ella, la punta hacia arriba), inclinado y con un vaivén lento.
        private void AplicarVaiven(float segundos)
        {
            float vaiven = Mathf.Sin(segundos * (2f * Mathf.PI / SegundosVaiven)) * GradosVaiven;
            Quaternion baseGiro = Quaternion.LookRotation(Vector3.up, Vector3.back);
            for (int i = 0; i < soportes.Count; i++)
            {
                if (soportes[i] == null)
                {
                    continue;
                }
                soportes[i].localRotation = Quaternion.AngleAxis(vaiven, Vector3.up) * Quaternion.AngleAxis(GradosInclinacion, Vector3.right) * baseGiro;
            }
        }

        // ---- Etiquetas y líneas ----

        private void CrearEtiquetas(bool derecha)
        {
            int indice = derecha ? 1 : 0;
            GameObject modelo = indice < modelos.Count ? modelos[indice] : null;
            Transform soporte = indice < soportes.Count ? soportes[indice] : null;
            var propias = new List<(LineaGuia linea, string texto, float y)>();
            foreach (var e in TextosGuiaVR.Etiquetas)
            {
                if (e.derecha != derecha)
                {
                    continue;
                }
                var l = new LineaGuia();
                Transform pieza = modelo != null ? Buscar(modelo.transform, TextosGuiaVR.Nombres(e.boton, derecha)) : null;
                if (pieza != null)
                {
                    l.Ancla = pieza;
                    l.Local = Vector3.zero;
                    l.Encontrada = true;
                    LineasConPieza++;
                }
                else
                {
                    // Sin la pieza: un punto fijo en el marco del control (el padre del modelo, que hace de mano).
                    l.Ancla = modelo != null ? modelo.transform.parent : soporte;
                    l.Local = TextosGuiaVR.Respaldo(e.boton, derecha);
                }
                Vector3 mundo = l.Ancla != null ? l.Ancla.TransformPoint(l.Local) : transform.position;
                float y = transform.InverseTransformPoint(mundo).y / MetrosPorPx;
                propias.Add((l, e.texto, y));
            }
            // De arriba abajo en el orden de las piezas, para que las líneas no se crucen.
            propias.Sort((a, b) => b.y.CompareTo(a.y));
            for (int i = 0; i < propias.Count; i++)
            {
                float y = PrimeraEtiquetaPx - i * PasoEtiquetaPx;
                float x = derecha ? ColumnaEtiquetasPx : -ColumnaEtiquetasPx;
                Text t = Texto("Etiqueta_" + (derecha ? "Der_" : "Izq_") + i, propias[i].texto, TrazoH10.TamanoFila, TrazoH10.Texto,
                    new Vector2(x, y), new Vector2(derecha ? 0f : 1f, 0.5f), new Vector2(AnchoEtiquetaPx, AltoEtiquetaPx),
                    derecha ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                etiquetas.Add(t);
                LineaGuia l = propias[i].linea;
                l.OrigenPx = new Vector2(derecha ? x - 10f : x + 10f, y);
                l.Linea = CrearLinea(t.transform);
                lineas.Add(l);
            }
        }

        private static Transform Buscar(Transform raiz, string[] nombres)
        {
            foreach (string nombre in nombres)
            {
                Transform t = BuscarUno(raiz, nombre);
                if (t != null)
                {
                    return t;
                }
            }
            return null;
        }

        private static Transform BuscarUno(Transform t, string nombre)
        {
            if (string.Equals(t.name, nombre, System.StringComparison.OrdinalIgnoreCase))
            {
                return t;
            }
            for (int i = 0; i < t.childCount; i++)
            {
                Transform r = BuscarUno(t.GetChild(i), nombre);
                if (r != null)
                {
                    return r;
                }
            }
            return null;
        }

        private LineRenderer CrearLinea(Transform padre)
        {
            if (materialLinea == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                materialLinea = shader != null ? new Material(shader) : new Material(Canvas.GetDefaultCanvasMaterial());
            }
            var objeto = new GameObject("Linea");
            objeto.transform.SetParent(transform, false);
            LineRenderer linea = objeto.AddComponent<LineRenderer>();
            linea.useWorldSpace = true;
            linea.positionCount = 2;
            linea.widthMultiplier = GrosorLinea;
            linea.numCapVertices = 2;
            linea.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            linea.receiveShadows = false;
            linea.sharedMaterial = materialLinea;
            linea.startColor = ColorLinea;
            linea.endColor = ColorLinea;
            return linea;
        }

        private void ActualizarLineas()
        {
            Transform lienzo = canvas != null ? canvas.transform : transform;
            foreach (LineaGuia l in lineas)
            {
                if (l.Linea == null)
                {
                    continue;
                }
                Vector3 desde = lienzo.TransformPoint(new Vector3(l.OrigenPx.x, l.OrigenPx.y, 0f));
                Vector3 hasta = l.Ancla != null ? l.Ancla.TransformPoint(l.Local) : desde;
                l.Linea.SetPosition(0, desde);
                l.Linea.SetPosition(1, hasta);
            }
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
            CerrarApuntado = ApuntaACerrar(out _);
            if (CerrarApuntado && !cerrar.EstaElegido)
            {
                cerrar.Elegir();
            }
            Keyboard teclado = Keyboard.current;
            bool esc = teclado != null && teclado.escapeKey.wasPressedThisFrame; // Menú del control izquierdo = Esc (MenusVR)
            bool gatillo = CerrarApuntado && EntradaVR.Pulso(EntradaVR.GatilloDerecho);
            if (gatillo)
            {
                cerrar.Pulsar(); // CERRAR llama a Cerrar
                return;
            }
            if (EntradaVR.Pulso(EntradaVR.BotonB) || esc)
            {
                Cerrar();
            }
        }

        private void LateUpdate()
        {
            AplicarVaiven(Time.unscaledTime - inicio);
            ActualizarLineas();
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
        public bool ApuntaACerrar(out float distancia)
        {
            if (cerrar == null || !RayoAlPanel(out Vector2 px, out distancia))
            {
                distancia = 0f;
                return false;
            }
            var rect = (RectTransform)cerrar.transform;
            Vector3 enBoton = rect.InverseTransformPoint(canvas.transform.TransformPoint(new Vector3(px.x, px.y, 0f)));
            return rect.rect.Contains(new Vector2(enBoton.x, enBoton.y));
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
            if (materialLinea != null)
            {
                Destroy(materialLinea);
            }
        }
    }
}

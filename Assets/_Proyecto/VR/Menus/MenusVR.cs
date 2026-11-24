using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.MenuPausa;
using MechaCyberpunk.UI.Rueda;
using MechaCyberpunk.UI.Titulo;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.VR.Menus
{
    /// <summary>
    /// VR4: los menús en VR, sin pulir. Se crea solo cuando <see cref="ModoVR.Activo"/> (y sobrevive a los cambios de escena).
    /// <list type="bullet">
    /// <item>Cada Canvas de pantalla (Overlay o Screen Space Camera) pasa a World Space: los menús, tiendas y avisos como un panel a 1.5 m frente a la
    /// cabeza (1920 px = 1.6 m) que se coloca al abrirse un menú y se queda quieto; sin menú sigue suave a la cabeza. Los fundidos y pantallas
    /// negras van pegados a la cabeza. El HUD de pantalla (mira, minimapa, HUD H10...) no se ve: vida y dinero van en la muñeca izquierda.
    /// Al salir de VR todo vuelve como estaba.</item>
    /// <item>Láser del control derecho: se ve con un menú abierto o al apuntar a algo de la UI que se pueda usar; el gatillo derecho hace clic.
    /// Mientras <see cref="BloqueaDisparo"/> la pistola (VR2) no debe disparar.</item>
    /// <item>Botón Menú del control izquierdo = Esc (abre y cierra la pausa, cierra el mapa...). Y = la rueda de armas (se elige apuntando con el láser).</item>
    /// <item>Los carros y motos no se manejan en VR: al intentar subir sale "NO DISPONIBLE EN VR" 2 s.</item>
    /// <item>En escenas sin jugador a pie (Inicio) arma un rig mínimo (cabeza y control derecho) para poder elegir con el láser.</item>
    /// </list>
    /// </summary>
    [DefaultExecutionOrder(-120)]
    public class MenusVR : MonoBehaviour
    {
        public const string TextoNoDisponible = "NO DISPONIBLE EN VR";
        public const float SegundosAviso = 2f;
        public const string AccionRueda = "Rueda";
        /// <summary>Y del control izquierdo: la rueda de armas (Tab / botón medio).</summary>
        public const string RutaRueda = EntradaVR.Izq + "/{SecondaryButton}";

        private const string GrupoVR = "VR";
        private const float SegundosBarrido = 0.25f;
        private const float SegundosSinJugadorParaRig = 0.5f;
        /// <summary>Hasta dónde busca UI el láser (m).</summary>
        private const float AlcanceLaser = 10f;
        /// <summary>Largo del láser cuando no toca nada (m).</summary>
        private const float LargoLaser = 3f;
        private const float GrosorLaser = 0.004f;
        private const float DistanciaAviso = 1.2f;
        private const float SegundosBajarCarro = 3f;
        private const float EscalaOculto = 1e-5f;
        private const float BajadaOculto = 1000f;
        private const float ToleranciaSeguir = 3f;
        private const float DiferenciaAlturaSeguir = 0.3f;
        private const int CuadrosEsc = 2;
        private static readonly Color ColorLaser = new Color(0f, 0.9f, 1f, 0.85f);
        private static readonly Color ColorLaserFin = new Color(0f, 0.9f, 1f, 0.15f);

        /// <summary>El de la sesión (null sin VR).</summary>
        public static MenusVR Actual { get; private set; }

        /// <summary>True con un menú, tienda, pausa, rueda o ventana abiertos (en VR).</summary>
        public static bool MenuAbierto { get; private set; }

        /// <summary>True si el láser apunta este cuadro a algo de la UI que se puede usar (botón, deslizador, lista...).</summary>
        public static bool UIApuntada { get; private set; }

        /// <summary>Lo que VR2 consulta: con un menú abierto o apuntando a la UI el gatillo derecho es clic, no disparo.</summary>
        public static bool BloqueaDisparo => MenuAbierto || UIApuntada;

        private readonly List<LienzoConvertidoVR> lienzos = new List<LienzoConvertidoVR>();
        private readonly HashSet<Canvas> vistos = new HashSet<Canvas>();
        private readonly PunteroUIVR puntero = new PunteroUIVR();
        private readonly HudMunecaVR hud = new HudMunecaVR();
        private readonly PanelAvisoVR aviso = new PanelAvisoVR();
        private readonly RigMenusVR rig = new RigMenusVR();
        private readonly RaycastHit[] golpes = new RaycastHit[16];
        private static readonly Predicate<Canvas> EsNulo = c => c == null;

        private Camera camaraLaser;
        private RaycasterLaserVR raycaster;
        private LineRenderer linea;
        private Material materialLinea;
        private Transform cabeza;
        private Transform mano;
        private Transform raizJugador;
        private AdaptadorVR adaptador;
        private JugadorAPie jugador;
        private IVida vida;
        private float barrido;
        private float sinJugador;
        private Pose ancla;
        private bool anclaPuesta;
        private float distanciaAncla = ReglaLienzosVR.DistanciaFrente;
        private bool siguiendo;
        private float segundosFuera;
        private bool laserVisible;
        private float largoLaser = LargoLaser;
        private int escPendiente;
        private Canvas lienzoRueda;
        private Object ruedaDelLienzo;
        private bool cargaPrevia;
        private bool duplicado;

        // Lo de antes, para ver qué se abrió.
        private int cantidadPrevia;
        private Object arribaPrevia;
        private bool pausaPrevia;
        private bool ruedaPrevia;
        private int abiertosPrevios;
        private bool abiertoPrevio;

        /// <summary>Cuántos Canvas de pantalla van pasados a World Space.</summary>
        public int Convertidos => lienzos.Count;
        public bool LaserVisible => laserVisible;
        public bool AvisoVisible => aviso.Visible;
        public string TextoAviso => aviso.Texto;
        public Pose Ancla => ancla;
        public HudMunecaVR Hud => hud;
        public bool RigMenusArmado => rig.Existe;
        /// <summary>La raíz del rig de menús (null si no hay).</summary>
        public GameObject RaizRigMenus => rig.Raiz;
        /// <summary>La cámara del rig de menús (null si no hay).</summary>
        public Camera CamaraRigMenus => rig.Camara;
        /// <summary>De dónde sale la cabeza este cuadro: la cámara del jugador, la del rig de menús o la de la escena.</summary>
        public Transform Cabeza => cabeza;
        /// <summary>El control del que sale el láser (null si no hay).</summary>
        public Transform ManoLaser => mano;
        /// <summary>True si el láser se dibujó el último cuadro.</summary>
        public bool LaserDibujado => linea != null && linea.enabled;

        /// <summary>El tipo con el que se convirtió este Canvas, o null si no es uno de los convertidos.</summary>
        public TipoLienzoVR? TipoDe(Canvas canvas)
        {
            foreach (LienzoConvertidoVR l in lienzos)
            {
                if (l.Canvas == canvas)
                {
                    return l.Tipo;
                }
            }
            return null;
        }

        // ---- Arranque ----

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Actual = null;
            MenuAbierto = false;
            UIApuntada = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Arrancar()
        {
            ModoVR.Cambio -= AlCambiarModoVR;
            ModoVR.Cambio += AlCambiarModoVR;
            AdaptadorVR.Listo -= AlAdaptadorListo;
            AdaptadorVR.Listo += AlAdaptadorListo;
            if (ModoVR.Activo || AdaptadorVR.Actual != null)
            {
                Asegurar();
            }
        }

        private static void AlCambiarModoVR(bool activo)
        {
            if (activo)
            {
                Asegurar();
            }
            else if (Actual != null)
            {
                Actual.Apagar();
            }
        }

        private static void AlAdaptadorListo(AdaptadorVR a)
        {
            MenusVR menus = Asegurar();
            if (menus != null)
            {
                menus.AlArmarseJugador();
            }
        }

        // El rig del jugador ya está: el de menús se quita en el acto (no al siguiente Update), para que nunca haya dos XR Origin
        // ni dos cámaras de cabeza a la vez.
        private void AlArmarseJugador()
        {
            sinJugador = 0f;
            if (rig.Existe)
            {
                QuitarRig();
            }
        }

        /// <summary>Crea el componente si VR está activo y no hay uno.</summary>
        public static MenusVR Asegurar()
        {
            if (Actual != null || !ModoVR.Activo)
            {
                return Actual;
            }
            var objeto = new GameObject("MenusVR");
            DontDestroyOnLoad(objeto);
            return objeto.AddComponent<MenusVR>();
        }

        private void Awake()
        {
            if (Actual != null && Actual != this)
            {
                // Uno de más: se va sin tocar nada del bueno (los bindings agregados son estáticos y compartidos).
                duplicado = true;
                Destroy(gameObject);
                return;
            }
            Actual = this;
            var objetoCamara = new GameObject("CamaraLaserVR");
            objetoCamara.transform.SetParent(transform, false);
            camaraLaser = objetoCamara.AddComponent<Camera>();
            camaraLaser.enabled = false;
            camaraLaser.stereoTargetEye = StereoTargetEyeMask.None;
            camaraLaser.nearClipPlane = 0.01f;
            camaraLaser.farClipPlane = AlcanceLaser * 2f;
            camaraLaser.fieldOfView = 10f;
            camaraLaser.targetTexture = new RenderTexture(16, 16, 0) { name = "LaserVR" };
            raycaster = gameObject.AddComponent<RaycasterLaserVR>();
            raycaster.Configurar(camaraLaser);
            CrearLinea();
            Debug.Log("[VR] Menús VR listos.");
        }

        private void OnEnable()
        {
            CandadosMundo.Negado += AlNegado;
            SceneManager.sceneLoaded += AlCargarEscena;
        }

        private void OnDisable()
        {
            CandadosMundo.Negado -= AlNegado;
            SceneManager.sceneLoaded -= AlCargarEscena;
        }

        /// <summary>Sale de VR: devuelve los Canvas como estaban y se quita.</summary>
        public void Apagar()
        {
            RestaurarTodo();
            if (Actual == this)
            {
                Actual = null;
                MenuAbierto = false;
                UIApuntada = false;
            }
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (duplicado)
            {
                return;
            }
            RestaurarTodo();
            if (Actual == this)
            {
                Actual = null;
                MenuAbierto = false;
                UIApuntada = false;
            }
            if (camaraLaser != null && camaraLaser.targetTexture != null)
            {
                RenderTexture rt = camaraLaser.targetTexture;
                camaraLaser.targetTexture = null;
                Destroy(rt);
            }
            if (linea != null && !linea.transform.IsChildOf(transform))
            {
                // El láser colgaba del control del jugador, que sigue vivo: se va con los menús.
                Destroy(linea.gameObject);
            }
            if (materialLinea != null)
            {
                Destroy(materialLinea);
            }
        }

        private void RestaurarTodo()
        {
            puntero.Liberar();
            foreach (LienzoConvertidoVR l in lienzos)
            {
                l.Restaurar();
            }
            lienzos.Clear();
            vistos.Clear();
            hud.Destruir();
            aviso.Destruir();
            QuitarRig(false);
            CambiarJugador(null);
            QuitarBindingsPropios();
            anclaPuesta = false;
        }

        private void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            // La cámara de la escena nueva manda dónde queda el rig de menús.
            rig.ColocarEnEscena();
            anclaPuesta = false;
            barrido = 0f;
        }

        // ---- Cada cuadro ----

        private void Update()
        {
            if (!ModoVR.Activo)
            {
                Apagar();
                return;
            }
            float dt = Time.unscaledDeltaTime;
            ElegirFuente(dt);
            bool carga = PantallaCargando.Visible;
            if (carga && !cargaPrevia)
            {
                // La pantalla de carga es un Overlay, que en el visor no se ve: se pasa a panel ya (sin esperar al barrido)
                // y frente a donde mira la cabeza ahora.
                barrido = 0f;
                ColocarAncla();
            }
            cargaPrevia = carga;
            barrido -= dt;
            if (barrido <= 0f)
            {
                barrido = SegundosBarrido;
                Barrer();
                AsegurarBindingRueda();
            }
            ActualizarEstadoMenus();
            ActualizarLaser();
            ActualizarRueda();
            ActualizarPausa();
            hud.Actualizar(dt, vida, Registro<ICartera>.Actual);
            aviso.Actualizar(dt);
        }

        private void LateUpdate()
        {
            if (!ModoVR.Activo || cabeza == null)
            {
                return;
            }
            SeguirCabeza(Time.unscaledDeltaTime);
            Vector3 frente = cabeza.forward;
            for (int i = 0; i < lienzos.Count; i++)
            {
                LienzoConvertidoVR l = lienzos[i];
                if (l.Canvas == null)
                {
                    continue;
                }
                switch (l.Tipo)
                {
                    case TipoLienzoVR.Frente:
                        float factor = distanciaAncla / ReglaLienzosVR.DistanciaFrente;
                        Vector3 posicion = ancla.position - ancla.forward * ReglaLienzosVR.Adelanto(l.Canvas.sortingOrder, factor);
                        l.Colocar(posicion, ancla.rotation, ReglaLienzosVR.EscalaFrente * factor);
                        break;
                    case TipoLienzoVR.PantallaCompleta:
                        int orden = l.Canvas.sortingOrder - ReglaLienzosVR.OrdenPantallaCompleta;
                        Vector3 cerca = cabeza.position + frente * (ReglaLienzosVR.DistanciaCompleta - ReglaLienzosVR.Adelanto(orden, 1f));
                        l.Colocar(cerca, cabeza.rotation, ReglaLienzosVR.EscalaCompleta);
                        break;
                    default:
                        l.Colocar(cabeza.position + Vector3.down * BajadaOculto, Quaternion.identity, EscalaOculto);
                        break;
                }
            }
            DibujarLaser();
        }

        // La cabeza y el control derecho: los del jugador a pie si hay, si no los del rig de menús.
        private void ElegirFuente(float dt)
        {
            Transform previa = cabeza;
            ElegirCabezaYMano(dt);
            if (cabeza != null && cabeza != previa)
            {
                // Cambió de dónde sale la cabeza (rig de menús ↔ rig del jugador): los paneles se vuelven a poner frente a la cabeza nueva,
                // si no se quedarían donde estaba la vieja (al cargar la partida, la carga quedaría lejos o detrás).
                anclaPuesta = false;
            }
        }

        private void ElegirCabezaYMano(float dt)
        {
            AdaptadorVR a = AdaptadorVR.Actual;
            if (a != null && a.Armado && a.Camara != null)
            {
                sinJugador = 0f;
                if (rig.Existe)
                {
                    QuitarRig();
                }
                cabeza = a.Camara.transform;
                mano = a.ManoDerecha != null ? a.ManoDerecha.transform : null;
                if (a != adaptador)
                {
                    CambiarJugador(a);
                }
                if (!hud.Existe && a.ManoIzquierda != null)
                {
                    hud.Crear(a.ManoIzquierda.transform);
                }
                return;
            }
            if (adaptador != null || jugador != null)
            {
                CambiarJugador(null);
            }
            if (hud.Existe && hud.Mano == null)
            {
                hud.Destruir();
            }
            sinJugador += dt;
            if (!rig.Existe && sinJugador > SegundosSinJugadorParaRig)
            {
                rig.Crear(transform);
            }
            if (rig.Existe)
            {
                cabeza = rig.Camara.transform;
                mano = rig.ManoDerecha;
            }
            else
            {
                Camera principal = Camera.main;
                cabeza = principal != null ? principal.transform : null;
                mano = null;
            }
        }

        // Quita el rig de menús. El láser cuelga de su control: se rescata antes, si no se destruiría con él y ya no se vería
        // (al apagar los menús no: este objeto también se va y no se le pueden colgar hijos mientras se destruye).
        private void QuitarRig(bool rescatarLaser = true)
        {
            if (!rig.Existe)
            {
                return;
            }
            Transform manoRig = rig.ManoDerecha;
            if (rescatarLaser && linea != null && manoRig != null && linea.transform.IsChildOf(manoRig))
            {
                linea.enabled = false;
                linea.transform.SetParent(transform, false);
            }
            if (mano == manoRig)
            {
                mano = null;
            }
            if (rig.Camara != null && cabeza == rig.Camara.transform)
            {
                cabeza = null;
            }
            rig.Destruir();
        }

        private void CambiarJugador(AdaptadorVR nuevo)
        {
            if (jugador != null)
            {
                jugador.ModoCambio -= AlCambiarModoJugador;
            }
            hud.Destruir();
            adaptador = nuevo;
            jugador = nuevo != null ? nuevo.Jugador : null;
            raizJugador = nuevo != null ? nuevo.transform.root : null;
            VidaAPie v = nuevo != null ? nuevo.GetComponent<VidaAPie>() : null;
            vida = v != null ? v : null;
            if (jugador != null)
            {
                jugador.ModoCambio += AlCambiarModoJugador;
            }
        }

        // ---- Canvas ----

        // Busca los Canvas de pantalla nuevos (también los apagados, para que ya estén en su lugar al abrirse) y los pasa a World Space.
        private void Barrer()
        {
            for (int i = lienzos.Count - 1; i >= 0; i--)
            {
                if (lienzos[i].Canvas == null)
                {
                    lienzos.RemoveAt(i);
                }
            }
            vistos.RemoveWhere(EsNulo);
            Canvas[] todos = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Canvas c in todos)
            {
                if (c == null || vistos.Contains(c))
                {
                    continue;
                }
                if (c == hud.Canvas || c == aviso.Canvas)
                {
                    vistos.Add(c);
                    continue;
                }
                Transform padre = c.transform.parent;
                bool anidado = padre != null && padre.GetComponentInParent<Canvas>(true) != null;
                if (anidado || c.renderMode == RenderMode.WorldSpace)
                {
                    // Los anidados siguen a su raíz; los del mundo (letreros, terminales) no se tocan.
                    vistos.Add(c);
                    continue;
                }
                TipoLienzoVR tipo = ReglaLienzosVR.Clasificar(c.name, c.sortingOrder);
                var l = new LienzoConvertidoVR(c, tipo);
                l.Convertir();
                lienzos.Add(l);
                vistos.Add(c);
                if (tipo == TipoLienzoVR.Frente && !anclaPuesta)
                {
                    ColocarAncla();
                }
                Debug.Log("[VR] Canvas " + c.name + " a World Space (" + tipo + ").");
            }
            foreach (LienzoConvertidoVR l in lienzos)
            {
                // Si el juego lo volvió a poner en pantalla, se convierte otra vez.
                if (l.Canvas.renderMode != RenderMode.WorldSpace)
                {
                    l.Convertir();
                }
                else
                {
                    l.Refrescar();
                }
            }
        }

        // ---- Panel frente a la cabeza ----

        private void ActualizarEstadoMenus()
        {
            int cantidad = PilaPantallas.Cantidad;
            Object arriba = PilaPantallas.Arriba;
            bool rueda = RuedaArmasObjetos.Abierta || RuedaHabilidadesExo.Abierta;
            bool pausa = Pausa.Activa;
            int abiertos = MenuCyberfix.Abiertos;
            // Sin jugador (Inicio y la carga hacia la partida) el panel es el menú: queda quieto y el láser siempre se ve.
            bool sinJugadorConRig = rig.Existe;
            bool abierto = cantidad > 0 || pausa || abiertos > 0 || rueda || VentanaAviso.Abierta || sinJugadorConRig;
            bool algoNuevo = cantidad > cantidadPrevia
                || (arriba != null && arriba != arribaPrevia && cantidad >= cantidadPrevia)
                || (rueda && !ruedaPrevia)
                || (pausa && !pausaPrevia)
                || abiertos > abiertosPrevios
                || (abierto && !abiertoPrevio);
            if (algoNuevo)
            {
                ColocarAncla();
            }
            cantidadPrevia = cantidad;
            arribaPrevia = arriba;
            ruedaPrevia = rueda;
            pausaPrevia = pausa;
            abiertosPrevios = abiertos;
            abiertoPrevio = abierto;
            if (abierto != MenuAbierto)
            {
                Debug.Log("[VR] Menú abierto: " + abierto + " (pila " + cantidad + ", pausa " + pausa + ", cyberfix " + abiertos + ", rueda " + rueda + ", aviso " + VentanaAviso.Abierta + ", rig de menús " + sinJugadorConRig + ")");
            }
            MenuAbierto = abierto;
        }

        /// <summary>Pone el panel de menús frente a la cabeza (más cerca si hay pared).</summary>
        public void ColocarAncla()
        {
            if (cabeza == null)
            {
                return;
            }
            Vector3 frente = ReglaLienzosVR.FrentePlano(cabeza.forward, cabeza.up);
            distanciaAncla = ReglaLienzosVR.Distancia(DistanciaPared(cabeza.position, frente));
            ancla = ReglaLienzosVR.PoseFrente(cabeza.position, frente, distanciaAncla);
            anclaPuesta = true;
            siguiendo = false;
            segundosFuera = 0f;
        }

        // Con menú: quieto, salvo que quede fuera de la vista un rato. Sin menú (avisos, tutorial): sigue suave a la cabeza.
        private void SeguirCabeza(float dt)
        {
            if (!anclaPuesta)
            {
                ColocarAncla();
                return;
            }
            Vector3 frente = ReglaLienzosVR.FrentePlano(cabeza.forward, cabeza.up);
            float angulo = ReglaLienzosVR.AnguloHacia(cabeza.position, frente, ancla.position);
            Vector3 plano = ancla.position - cabeza.position;
            float alto = plano.y + ReglaLienzosVR.BajadaFrente;
            plano.y = 0f;
            bool lejos = plano.magnitude > ReglaLienzosVR.DistanciaSeguir || Mathf.Abs(alto) > DiferenciaAlturaSeguir;
            if (MenuAbierto)
            {
                siguiendo = false;
                segundosFuera = angulo > ReglaLienzosVR.AnguloFueraDeVista || lejos ? segundosFuera + dt : 0f;
                if (segundosFuera > ReglaLienzosVR.SegundosFueraDeVista)
                {
                    ColocarAncla();
                }
                return;
            }
            if (!siguiendo && (angulo > ReglaLienzosVR.AnguloSeguir || lejos))
            {
                siguiendo = true;
            }
            if (!siguiendo)
            {
                return;
            }
            float distancia = ReglaLienzosVR.Distancia(DistanciaPared(cabeza.position, frente));
            Pose objetivo = ReglaLienzosVR.PoseFrente(cabeza.position, frente, distancia);
            float k = 1f - Mathf.Exp(-ReglaLienzosVR.RapidezSeguir * dt);
            ancla.position = Vector3.Lerp(ancla.position, objetivo.position, k);
            ancla.rotation = Quaternion.Slerp(ancla.rotation, objetivo.rotation, k);
            distanciaAncla = Mathf.Lerp(distanciaAncla, distancia, k);
            if (angulo < ToleranciaSeguir && !lejos)
            {
                siguiendo = false;
            }
        }

        // Distancia a la pared más cercana enfrente (m), sin contar al jugador; -1 si no hay.
        private float DistanciaPared(Vector3 desde, Vector3 frente)
        {
            int n = Physics.RaycastNonAlloc(desde, frente, golpes, ReglaLienzosVR.DistanciaFrente + ReglaLienzosVR.MargenPared,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float minima = -1f;
            for (int i = 0; i < n; i++)
            {
                if (raizJugador != null && golpes[i].collider.transform.root == raizJugador)
                {
                    continue;
                }
                if (minima < 0f || golpes[i].distance < minima)
                {
                    minima = golpes[i].distance;
                }
            }
            return minima;
        }

        // ---- Láser ----

        private void CrearLinea()
        {
            var objeto = new GameObject("LaserVR");
            objeto.transform.SetParent(transform, false);
            linea = objeto.AddComponent<LineRenderer>();
            linea.useWorldSpace = false;
            linea.positionCount = 2;
            linea.widthMultiplier = GrosorLaser;
            linea.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            linea.receiveShadows = false;
            if (materialLinea == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                materialLinea = shader != null ? new Material(shader) : new Material(Canvas.GetDefaultCanvasMaterial());
            }
            linea.sharedMaterial = materialLinea;
            linea.startColor = ColorLaser;
            linea.endColor = ColorLaserFin;
            linea.enabled = false;
        }

        private void ActualizarLaser()
        {
            UIApuntada = false;
            if (mano == null)
            {
                puntero.Liberar();
                laserVisible = false;
                return;
            }
            camaraLaser.transform.SetPositionAndRotation(mano.position, mano.rotation);
            var rayo = new Ray(mano.position, mano.forward);
            bool hay = raycaster.Lanzar(rayo, AlcanceLaser, lienzos, out RaycastResult resultado);
            bool interactivo = hay && PunteroUIVR.EsInteractivo(resultado.gameObject);
            if (!MenuAbierto && !interactivo && !puntero.Pulsado)
            {
                puntero.Liberar();
                laserVisible = false;
                return;
            }
            bool presiono = EntradaVR.Pulso(EntradaVR.GatilloDerecho);
            bool solto = EntradaVR.Solto(EntradaVR.GatilloDerecho);
            puntero.Procesar(hay, resultado, presiono, solto);
            UIApuntada = interactivo;
            laserVisible = true;
            largoLaser = hay ? resultado.distance : LargoLaser;
        }

        private void DibujarLaser()
        {
            if (linea == null)
            {
                // Se fue con el control que lo llevaba (el jugador se destruyó al cambiar de escena): se vuelve a crear.
                CrearLinea();
            }
            if (!laserVisible || mano == null)
            {
                linea.enabled = false;
                return;
            }
            // Hijo del control: sigue a la pose aunque el TrackedPoseDriver la cambie antes de dibujar.
            if (linea.transform.parent != mano)
            {
                linea.transform.SetParent(mano, false);
                linea.transform.localPosition = Vector3.zero;
                linea.transform.localRotation = Quaternion.identity;
                linea.transform.localScale = Vector3.one;
            }
            linea.SetPosition(0, Vector3.zero);
            linea.SetPosition(1, Vector3.forward * largoLaser);
            linea.enabled = true;
        }

        // ---- Rueda ----

        // Y abre la rueda: el binding va en la acción "Rueda" que lee la rueda (la del asset de ControlesHud) y en la del mapa APie del jugador.
        private void AsegurarBindingRueda()
        {
            if (adaptador == null)
            {
                return;
            }
            ControlesHud controles = ControlesHud.Actual;
            InputAction accion = controles != null ? controles.AccionAPie(AccionRueda) : null;
            if (accion != null)
            {
                AgregarBinding(accion, RutaRueda);
            }
            InputAction delMapa = AdaptadorVR.BuscarAccionAPie(AccionRueda);
            if (delMapa != null && delMapa != accion)
            {
                AdaptadorVR.AgregarBinding(AccionRueda, RutaRueda);
            }
        }

        private static readonly List<(InputAction accion, string ruta)> bindingsPropios = new List<(InputAction, string)>();

        private static void AgregarBinding(InputAction accion, string ruta)
        {
            foreach (InputBinding b in accion.bindings)
            {
                if (b.path == ruta)
                {
                    return;
                }
            }
            bool encendida = accion.enabled;
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
            bindingsPropios.Add((accion, ruta));
            foreach (InputAction a in encendidas)
            {
                a.Enable();
            }
            if (mapa == null && encendida)
            {
                accion.Enable();
            }
        }

        private static void QuitarBindingsPropios()
        {
            foreach (var (accion, ruta) in bindingsPropios)
            {
                if (accion == null)
                {
                    continue;
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
            bindingsPropios.Clear();
        }

        // Con la rueda abierta, el punto del láser sobre la rueda hace de ratón: se manda la diferencia como movimiento del ratón,
        // que es lo que la rueda ya lee (sin tocar su código).
        private void ActualizarRueda()
        {
            Mouse raton = Mouse.current;
            if (mano == null || raton == null)
            {
                return;
            }
            MonoBehaviour rueda = null;
            Vector2 cursor = Vector2.zero;
            Vector2 limite = Vector2.zero;
            bool circular = true;
            if (RuedaArmasObjetos.Abierta && RuedaArmasObjetos.Actual != null)
            {
                rueda = RuedaArmasObjetos.Actual;
                cursor = RuedaArmasObjetos.Actual.Cursor;
                limite = Vector2.one * ReglaRuedaArmas.RadioMaximo;
            }
            else if (RuedaHabilidadesExo.Abierta && RuedaHabilidadesExo.Actual != null)
            {
                rueda = RuedaHabilidadesExo.Actual;
                cursor = RuedaHabilidadesExo.Actual.Cursor;
                limite = RuedaHabilidadesExo.LimiteCursor;
                circular = false;
            }
            if (rueda == null)
            {
                return;
            }
            if (ruedaDelLienzo != rueda || lienzoRueda == null)
            {
                ruedaDelLienzo = rueda;
                lienzoRueda = rueda.GetComponentInChildren<Canvas>(true);
            }
            if (lienzoRueda == null)
            {
                return;
            }
            var rt = (RectTransform)lienzoRueda.transform;
            Vector3 normal = rt.forward;
            float denominador = Vector3.Dot(normal, mano.forward);
            if (denominador <= 1e-5f)
            {
                return;
            }
            float t = Vector3.Dot(rt.position - mano.position, normal) / denominador;
            if (t < 0f || t > AlcanceLaser)
            {
                return;
            }
            Vector3 local = rt.InverseTransformPoint(mano.position + mano.forward * t);
            Vector2 objetivo = new Vector2(local.x, local.y) - rt.rect.center;
            objetivo = circular ? Vector2.ClampMagnitude(objetivo, limite.x)
                : new Vector2(Mathf.Clamp(objetivo.x, -limite.x, limite.x), Mathf.Clamp(objetivo.y, -limite.y, limite.y));
            Vector2 delta = objetivo - cursor;
            if (delta.sqrMagnitude > 0.25f)
            {
                InputSystem.QueueDeltaStateEvent(raton.delta, delta);
            }
        }

        // ---- Pausa ----

        // El botón Menú del control izquierdo hace lo que hoy hace Esc: se simula un Esc del teclado (abre la pausa si no hay nada abierto,
        // cierra la de arriba si la hay). Sin teclado se llama a la pausa directo.
        private void ActualizarPausa()
        {
            if (escPendiente > 0)
            {
                escPendiente--;
                if (escPendiente == 0 && Keyboard.current != null)
                {
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                }
            }
            if (!EntradaVR.Pulso(EntradaVR.Menu) || escPendiente > 0)
            {
                return;
            }
            Keyboard teclado = Keyboard.current;
            if (teclado != null)
            {
                InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.Escape));
                escPendiente = CuadrosEsc;
                return;
            }
            PantallaPausa pausa = FindFirstObjectByType<PantallaPausa>(FindObjectsInactive.Include);
            if (pausa == null)
            {
                return;
            }
            if (pausa.Abierta && PilaPantallas.EsArriba(pausa))
            {
                pausa.Cerrar();
            }
            else if (!pausa.Abierta && PilaPantallas.Vacia && MenuCyberfix.Abiertos == 0)
            {
                pausa.Abrir();
            }
        }

        // ---- Carros ----

        private void AlNegado(string texto)
        {
            if (texto == TextoNoDisponible)
            {
                MostrarAviso(texto);
            }
        }

        /// <summary>Muestra un aviso corto frente a la cabeza.</summary>
        public void MostrarAviso(string texto)
        {
            if (cabeza == null)
            {
                return;
            }
            Vector3 frente = ReglaLienzosVR.FrentePlano(cabeza.forward, cabeza.up);
            float distancia = Mathf.Min(DistanciaAviso, ReglaLienzosVR.Distancia(DistanciaPared(cabeza.position, frente)));
            Pose pose = ReglaLienzosVR.PoseFrente(cabeza.position, frente, distancia);
            aviso.Mostrar(texto, pose, ReglaLienzosVR.EscalaFrente * distancia / ReglaLienzosVR.DistanciaFrente, SegundosAviso);
        }

        // Respaldo por si falta el parche de InteraccionAPie: si de todos modos subió a un carro, se baja en cuanto pueda.
        private void AlCambiarModoJugador(ModoJugador modo)
        {
            if (modo != ModoJugador.Carro || !ModoVR.Activo || jugador == null)
            {
                return;
            }
            Debug.LogWarning("[VR] El jugador subió a un carro en VR (¿falta el parche de InteraccionAPie?): se baja.");
            MostrarAviso(TextoNoDisponible);
            StartCoroutine(BajarDelCarro(jugador));
        }

        private IEnumerator BajarDelCarro(JugadorAPie quien)
        {
            float fin = Time.unscaledTime + SegundosBajarCarro;
            while (quien != null && quien.Modo == ModoJugador.Carro && Time.unscaledTime < fin)
            {
                if (quien.Bajar())
                {
                    yield break;
                }
                yield return null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void LimpiarBindings()
        {
            bindingsPropios.Clear();
        }
    }
}

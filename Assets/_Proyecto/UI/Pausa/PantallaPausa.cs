using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.UI.Guardado;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.MenuPausa
{
    /// <summary>
    /// La pausa (T-410, D-tony-242), hecha con el kit en un canvas de orden 60: fondo negro a pantalla completa y un panel centrado con
    /// "PAUSA" y CONTINUAR, GUARDAR, CARGAR, OPCIONES y SALIR AL TÍTULO. Esc la abre solo si no hay otra pantalla en
    /// <see cref="PilaPantallas"/>. Abierta: <c>Time.timeScale</c> en 0 (tiempo, IA y disparos), <c>AudioListener.pause</c> (los sonidos de
    /// Interfaz siguen), cursor visible, jugador congelado y <see cref="Pausa"/> activa. CONTINUAR o Esc dejan todo como estaba.
    /// GUARDAR y CARGAR abren <see cref="PantallaRanuras"/> encima; SALIR AL TÍTULO pide confirmar y llama <see cref="FlujoPartida.IrAlTitulo"/>.
    /// </summary>
    public class PantallaPausa : MonoBehaviour
    {
        public const int OrdenCanvas = 60;
        public const string TextoTitulo = "PAUSA";
        public const string TextoContinuar = "CONTINUAR";
        public const string TextoGuardar = "GUARDAR";
        public const string TextoCargar = "CARGAR";
        public const string TextoOpciones = "OPCIONES";
        public const string TextoSalir = "SALIR AL TÍTULO";
        public const string TextoPregunta = "¿SALIR?";
        public const string TextoAviso = "LO QUE NO GUARDASTE SE PIERDE";
        public const string TextoSi = "SÍ";
        public const string TextoNo = "NO";
        public const string TextoAyuda = "↑ ↓ ELEGIR · ENTER ACEPTAR · ESC CONTINUAR";
        private const int CantidadBotones = 5;

        [Header("Medidas (px a 1920x1080)")]
        [Tooltip("Ancho del panel; los botones son los del kit (440x64).")]
        [SerializeField] private float anchoPanel = 600f;
        [SerializeField] private Vector2 tamanoConfirmar = new Vector2(720f, 300f);
        [Tooltip("Opacidad del fondo de los paneles (0 a 1), como en las ranuras, para que el HUD de atrás no se lea a través.")]
        [Range(0f, 1f)] [SerializeField] private float alfaPanel = 0.96f;

        [Header("Piezas (las arma el creador con el kit)")]
        [SerializeField] private GameObject contenido;
        [SerializeField] private MenuCyberfix menu;
        [SerializeField] private BotonCyberfix continuar;
        [SerializeField] private BotonCyberfix guardar;
        [SerializeField] private BotonCyberfix cargar;
        [SerializeField] private BotonCyberfix opciones;
        [SerializeField] private BotonCyberfix salir;
        [SerializeField] private GameObject confirmar;
        [SerializeField] private MenuCyberfix menuConfirmar;
        [SerializeField] private BotonCyberfix si;
        [SerializeField] private BotonCyberfix no;

        private PantallaRanuras ranuras;
        private BotonCyberfix volverA;
        private ControladorAPie controlCongelado;
        private float escalaPrevia = 1f;
        private bool audioPrevio;

        public bool Abierta => contenido != null && contenido.activeSelf;
        public bool ConfirmacionAbierta => confirmar != null && confirmar.activeSelf;
        public MenuCyberfix Menu => menu;
        public MenuCyberfix MenuConfirmar => menuConfirmar;
        public BotonCyberfix Continuar => continuar;
        public BotonCyberfix Guardar => guardar;
        public BotonCyberfix Cargar => cargar;
        public BotonCyberfix Opciones => opciones;
        public BotonCyberfix Salir => salir;
        public BotonCyberfix Si => si;
        public BotonCyberfix No => no;

        /// <summary>La pantalla de ranuras que abren GUARDAR y CARGAR (la busca en la escena la primera vez).</summary>
        public PantallaRanuras Ranuras
        {
            get
            {
                if (ranuras == null)
                {
                    ranuras = FindFirstObjectByType<PantallaRanuras>(FindObjectsInactive.Include);
                    if (ranuras != null)
                    {
                        ranuras.Cerrada += AlCerrarRanuras;
                    }
                }
                return ranuras;
            }
        }

        /// <summary>Arma todas las piezas con el kit (lo llama el creador del prefab, o Awake si faltan).</summary>
        public void Armar()
        {
            EstiloCyberfix e = KitUI.Estilo;
            float relleno = e.Relleno;
            float separacion = e.SeparacionBotones;
            Vector2 boton = e.TamanoBotonMenu;
            float altoTitulo = e.Tamano(RolTexto.Encabezado) + relleno;
            float altoAyuda = e.AltoFilaHud;
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasPausa", OrdenCanvas);
            contenido = canvas.gameObject;
            KitUI.CrearFondoPantalla(canvas.transform, "FondoPantalla");

            float altoBotones = CantidadBotones * boton.y + (CantidadBotones - 1) * separacion;
            Vector2 tamanoPanel = new Vector2(anchoPanel,
                relleno + altoTitulo + e.SeparacionBloques + altoBotones + e.SeparacionBloques + altoAyuda + relleno);
            RectTransform panel = KitUI.CrearPanel(canvas.transform, "Panel", tamanoPanel);
            menu = panel.gameObject.AddComponent<MenuCyberfix>();
            Opacar(panel);

            Text titulo = KitUI.CrearTexto(panel, "Titulo", RolTexto.Encabezado, TextAnchor.MiddleCenter);
            KitUI.Colocar(titulo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -relleno), new Vector2(tamanoPanel.x, altoTitulo));
            titulo.text = TextoTitulo;
            titulo.color = e.Naranja;

            float y = -(relleno + altoTitulo + e.SeparacionBloques);
            continuar = Boton(panel, TextoContinuar, ref y);
            guardar = Boton(panel, TextoGuardar, ref y);
            cargar = Boton(panel, TextoCargar, ref y);
            opciones = Boton(panel, TextoOpciones, ref y);
            salir = Boton(panel, TextoSalir, ref y);
            opciones.interactable = false;

            Text ayuda = KitUI.CrearTexto(panel, "Ayuda", RolTexto.Ayuda, TextAnchor.MiddleCenter);
            KitUI.Colocar(ayuda.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, relleno), new Vector2(tamanoPanel.x, altoAyuda));
            ayuda.text = TextoAyuda;
            ayuda.color = e.Turquesa;

            // ¿SALIR? LO QUE NO GUARDASTE SE PIERDE · SÍ / NO, encima de todo, en magenta.
            RectTransform caja = KitUI.CrearPanel(canvas.transform, "Confirmar", tamanoConfirmar);
            confirmar = caja.gameObject;
            Opacar(caja);
            menuConfirmar = confirmar.AddComponent<MenuCyberfix>();
            Text pregunta = KitUI.CrearTexto(caja, "Pregunta", RolTexto.Encabezado, TextAnchor.MiddleCenter);
            KitUI.Colocar(pregunta.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -relleno), new Vector2(tamanoConfirmar.x, altoTitulo));
            pregunta.text = TextoPregunta;
            pregunta.color = e.Magenta;
            float altoAviso = e.Tamano(RolTexto.Texto) + relleno;
            Text aviso = KitUI.CrearTexto(caja, "Aviso", RolTexto.Texto, TextAnchor.MiddleCenter);
            KitUI.Colocar(aviso.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -(relleno + altoTitulo)), new Vector2(tamanoConfirmar.x, altoAviso));
            aviso.text = TextoAviso;
            aviso.color = e.Magenta;
            float yBoton = -(relleno + altoTitulo + altoAviso + relleno);
            si = KitUI.CrearBoton(caja, TextoSi, null);
            si.Texto.alignment = TextAnchor.MiddleCenter;
            KitUI.Colocar((RectTransform)si.transform, new Vector2(0.5f, 1f), new Vector2(0f, yBoton), boton);
            no = KitUI.CrearBoton(caja, TextoNo, null);
            no.Texto.alignment = TextAnchor.MiddleCenter;
            KitUI.Colocar((RectTransform)no.transform, new Vector2(0.5f, 1f), new Vector2(0f, yBoton - boton.y - separacion), boton);
            confirmar.SetActive(false);
            contenido.SetActive(false);
        }

        private static BotonCyberfix Boton(RectTransform panel, string texto, ref float y)
        {
            EstiloCyberfix e = KitUI.Estilo;
            BotonCyberfix b = KitUI.CrearBoton(panel, texto, null);
            b.Texto.alignment = TextAnchor.MiddleCenter;
            KitUI.Colocar((RectTransform)b.transform, new Vector2(0.5f, 1f), new Vector2(0f, y), e.TamanoBotonMenu);
            y -= e.TamanoBotonMenu.y + e.SeparacionBotones;
            return b;
        }

        private void Opacar(RectTransform panel)
        {
            Image fondo = panel.GetComponent<Image>();
            if (fondo != null)
            {
                Color color = fondo.color;
                color.a = Mathf.Max(color.a, alfaPanel);
                fondo.color = color;
            }
        }

        private void Awake()
        {
            if (contenido == null || menu == null || continuar == null || confirmar == null)
            {
                Armar();
            }
            continuar.AlPulsar.AddListener(Cerrar);
            guardar.AlPulsar.AddListener(() => AbrirRanuras(ModoRanuras.Guardar, guardar));
            cargar.AlPulsar.AddListener(() => AbrirRanuras(ModoRanuras.Cargar, cargar));
            salir.AlPulsar.AddListener(AbrirConfirmacion);
            si.AlPulsar.AddListener(SalirAlTitulo);
            no.AlPulsar.AddListener(CancelarConfirmacion);
            menu.Regresar += Cerrar;
            menuConfirmar.Regresar += CancelarConfirmacion;
            confirmar.SetActive(false);
            contenido.SetActive(false);
        }

        // Un cambio de escena (cargar o salir al título) no puede dejar el tiempo parado.
        private void OnDisable() => Cerrar();

        private void OnDestroy()
        {
            if (menu != null)
            {
                menu.Regresar -= Cerrar;
            }
            if (menuConfirmar != null)
            {
                menuConfirmar.Regresar -= CancelarConfirmacion;
            }
            if (ranuras != null)
            {
                ranuras.Cerrada -= AlCerrarRanuras;
            }
        }

        private void Update()
        {
            if (Abierta)
            {
                return;
            }
            Keyboard teclado = Keyboard.current;
            if (teclado == null || !teclado.escapeKey.wasPressedThisFrame)
            {
                return;
            }
            // Si otra pantalla atiende (o atendió en este cuadro) el Esc, no se pausa.
            if (!PilaPantallas.Vacia || PilaPantallas.CambioEsteCuadro || MenuCyberfix.Abiertos > 0)
            {
                return;
            }
            Abrir();
        }

        public void Abrir()
        {
            if (Abierta)
            {
                return;
            }
            escalaPrevia = Time.timeScale;
            audioPrevio = AudioListener.pause;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            Pausa.Fijar(true);
            IJugador jugador = Registro<IJugador>.Actual;
            controlCongelado = jugador != null && jugador.Raiz != null ? jugador.Raiz.GetComponentInChildren<ControladorAPie>() : null;
            if (controlCongelado != null && controlCongelado.ControlActivo)
            {
                controlCongelado.ActivarControl(false);
            }
            else
            {
                controlCongelado = null;
            }
            PilaPantallas.Abrir(this);
            confirmar.SetActive(false);
            contenido.SetActive(true);
            menu.enabled = true;
            menu.Recargar();
            Debug.Log("[UI] Pausa abierta.");
        }

        /// <summary>CONTINUAR o Esc: el tiempo, el audio y el control vuelven como estaban.</summary>
        public void Cerrar()
        {
            if (!Abierta)
            {
                return;
            }
            volverA = null;
            if (ranuras != null && ranuras.Abierta)
            {
                ranuras.Cerrar();
            }
            confirmar.SetActive(false);
            contenido.SetActive(false);
            Time.timeScale = escalaPrevia > 0f ? escalaPrevia : 1f;
            AudioListener.pause = audioPrevio;
            Pausa.Fijar(false);
            if (controlCongelado != null)
            {
                controlCongelado.ActivarControl(true);
                controlCongelado = null;
            }
            PilaPantallas.Cerrar(this);
            Debug.Log("[UI] Pausa cerrada.");
        }

        private void AbrirRanuras(ModoRanuras modo, BotonCyberfix desde)
        {
            PantallaRanuras pantalla = Ranuras;
            if (pantalla == null)
            {
                Debug.LogWarning("[UI] PantallaPausa sin PantallaRanuras en la escena.", this);
                return;
            }
            volverA = desde;
            // Primero se abren las ranuras y luego se apaga este menú, para que el cursor no se bloquee entre medio.
            pantalla.Abrir(modo);
            menu.enabled = false;
        }

        // Las ranuras se cerraron (VOLVER, Esc o guardar): la pausa vuelve a su menú, con el botón que las abrió elegido.
        private void AlCerrarRanuras()
        {
            if (!Abierta || menu.enabled)
            {
                return;
            }
            menu.enabled = true;
            if (volverA != null)
            {
                volverA.Elegir();
                volverA = null;
            }
        }

        private void AbrirConfirmacion()
        {
            confirmar.SetActive(true);
            menu.enabled = false;
            no.Elegir();
        }

        private void CancelarConfirmacion()
        {
            if (!ConfirmacionAbierta)
            {
                return;
            }
            menu.enabled = true;
            confirmar.SetActive(false);
            salir.Elegir();
        }

        private void SalirAlTitulo()
        {
            Cerrar();
            FlujoPartida.IrAlTitulo();
        }
    }
}

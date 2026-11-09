using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.UI.Guardado;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Mapa;
using MechaCyberpunk.UI.Opciones;
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
    /// GUARDAR y CARGAR abren <see cref="PantallaRanuras"/> encima y OPCIONES abre <see cref="PantallaOpciones"/> (T-411; gris si no hay); SALIR AL TÍTULO pide confirmar y llama <see cref="FlujoPartida.IrAlTitulo"/>.
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

        // T-902: la composición H10 (cabecera, panel de opciones a la izquierda y de datos a la derecha).
        private const float AnchoPanelOpciones = 520f;
        private const float AnchoPanelDatos = 600f;
        private const float AltoPaneles = 448f;
        private const float RellenoPanel = 18f;
        private const float AltoBoton = 64f;
        private const float SeparacionFilas = 12f;
        private const float InicioFilas = 62f;
        private const float AnchoEtiquetaDato = 170f;
        public const string TextoSituacion = "SITUACIÓN";
        public const string TextoMenu = "MENÚ";
        public const string TextoSinZona = "FUERA DE ZONA";
        public const string TextoSinObjetivo = "SIN OBJETIVO";
        private const float AlfaVelo = 0.93f;
        private bool vestidaH10;
        private Text textoDinero;
        private Text textoZona;
        private Text textoDueno;
        private Text textoObjetivo;
        private Text textoHora;
        private Text textoDia;
        private RectTransform panelOpciones;
        private RectTransform panelDatos;

        public Text TextoDinero => textoDinero;
        public Text TextoZona => textoZona;
        public Text TextoObjetivo => textoObjetivo;
        public Text TextoHora => textoHora;

        private PantallaRanuras ranuras;
        private PantallaOpciones pantallaOpciones;
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
                        ranuras.Cerrada += AlCerrarEncima;
                    }
                }
                return ranuras;
            }
        }

        /// <summary>La pantalla de opciones que abre OPCIONES (la busca en la escena la primera vez).</summary>
        public PantallaOpciones PantallaOpciones
        {
            get
            {
                if (pantallaOpciones == null)
                {
                    pantallaOpciones = FindFirstObjectByType<PantallaOpciones>(FindObjectsInactive.Include);
                    if (pantallaOpciones != null)
                    {
                        pantallaOpciones.Cerrada += AlCerrarEncima;
                    }
                }
                return pantallaOpciones;
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

        // T-902: viste la pausa con la guía H10 sin tocar el prefab: velo, cabecera, panel de opciones a la izquierda, panel de datos a la derecha.
        private void VestirH10()
        {
            if (vestidaH10)
            {
                return;
            }
            vestidaH10 = true;
            Transform raiz = contenido.transform;
            float margen = TrazoH10.MargenPantalla;
            Transform velo = raiz.Find("FondoPantalla");
            if (velo != null)
            {
                velo.GetComponent<Image>().color = TrazoH10.ConAlfa(TrazoH10.FondoPantalla, AlfaVelo);
            }
            foreach (string vieja in new[] { "Titulo", "Ayuda" })
            {
                Transform t = menu.transform.Find(vieja);
                if (t != null)
                {
                    t.gameObject.SetActive(false);
                }
            }
            textoDinero = VestidoH10.Cabecera(raiz, TextoTitulo, null, "TU DINERO", out _);
            VestidoH10.Pie(raiz, TextoAyuda);

            // Panel de opciones, a la izquierda.
            panelOpciones = (RectTransform)menu.transform;
            VestidoH10.Panel(panelOpciones.GetComponent<Image>());
            float arriba = VestidoH10.AltoCabecera + 24f;
            TrazoH10.Colocar(panelOpciones, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(margen, -arriba), new Vector2(AnchoPanelOpciones, AltoPaneles));
            TrazoH10.Seccion(panelOpciones, "Seccion", TextoMenu, new Vector2(RellenoPanel, -12f), AnchoPanelOpciones - 2f * RellenoPanel);
            BotonCyberfix[] botones = { continuar, guardar, cargar, opciones, salir };
            for (int i = 0; i < botones.Length; i++)
            {
                VestidoH10.Boton(botones[i]);
                TrazoH10.Colocar((RectTransform)botones[i].transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -(InicioFilas + i * (AltoBoton + SeparacionFilas))), new Vector2(AnchoPanelOpciones - 2f * RellenoPanel, AltoBoton));
            }

            // Panel de datos reales, a la derecha.
            panelDatos = TrazoH10.Panel(raiz, "PanelDatos", TrazoH10.TurquesaTenue).rectTransform;
            TrazoH10.Colocar(panelDatos, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-margen, -arriba), new Vector2(AnchoPanelDatos, AltoPaneles));
            TrazoH10.Seccion(panelDatos, "Seccion", TextoSituacion, new Vector2(RellenoPanel, -12f), AnchoPanelDatos - 2f * RellenoPanel);
            float y = InicioFilas;
            textoZona = FilaDato("ZONA", ref y, 44f);
            textoDueno = FilaDato("DUEÑO", ref y, 44f);
            textoObjetivo = FilaDato("OBJETIVO", ref y, 92f);
            textoHora = FilaDato("HORA", ref y, 44f);
            textoDia = FilaDato("DÍA", ref y, 44f);

            // Confirmar: el mismo panel H10, con el aviso en coral (es una alerta).
            VestidoH10.Panel(confirmar.GetComponent<Image>());
            foreach (BotonCyberfix b in new[] { si, no })
            {
                VestidoH10.Boton(b);
            }
            foreach (string nombre in new[] { "Pregunta", "Aviso" })
            {
                Transform t = confirmar.transform.Find(nombre);
                if (t != null)
                {
                    t.GetComponent<Text>().color = TrazoH10.Coral;
                }
            }
            confirmar.transform.SetAsLastSibling();
            VestidoH10.Fuente(raiz);
        }

        private Text FilaDato(string etiqueta, ref float y, float alto)
        {
            Text nombre = TrazoH10.Etiqueta(panelDatos, "Etiqueta" + etiqueta, etiqueta, TrazoH10.TamanoDato, TrazoH10.TextoTenue, TextAnchor.UpperLeft);
            TrazoH10.Punto(nombre.rectTransform, new Vector2(0f, 1f), new Vector2(RellenoPanel, -y), new Vector2(AnchoEtiquetaDato, 32f));
            Text valor = TrazoH10.Etiqueta(panelDatos, "Valor" + etiqueta, string.Empty, TrazoH10.TamanoFila, TrazoH10.Texto, TextAnchor.UpperLeft);
            valor.horizontalOverflow = HorizontalWrapMode.Wrap;
            float ancho = AnchoPanelDatos - 2f * RellenoPanel - AnchoEtiquetaDato;
            TrazoH10.Punto(valor.rectTransform, new Vector2(0f, 1f), new Vector2(RellenoPanel + AnchoEtiquetaDato, -y), new Vector2(ancho, alto));
            y += alto + SeparacionFilas;
            return valor;
        }

        // Datos reales de ahora: dinero, zona y dueño, objetivo y hora del juego. Sin el servicio, la fila dice que no hay.
        private void RefrescarDatos()
        {
            ICartera cartera = Registro<ICartera>.Actual;
            textoDinero.text = Moneda.Texto(cartera != null ? cartera.Dinero : 0);
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            IJugador jugador = Registro<IJugador>.Actual;
            string zona = territorio != null && jugador != null && jugador.Raiz != null ? territorio.ZonaEn(jugador.Raiz.position) : string.Empty;
            bool hayZona = !string.IsNullOrEmpty(zona);
            textoZona.text = hayZona ? territorio.Nombre(zona).ToUpperInvariant() : TextoSinZona;
            textoDueno.text = hayZona ? FormatoTerritorio.NombreDe(territorio.Dueno(zona)) : "-";
            textoDueno.color = hayZona ? FormatoTerritorio.ColorDe(territorio.Dueno(zona)) : TrazoH10.TextoTenue;
            IObjetivos objetivos = Registro<IObjetivos>.Actual;
            bool hayObjetivo = objetivos != null && objetivos.HayObjetivo;
            textoObjetivo.text = hayObjetivo ? objetivos.Actual.Texto : TextoSinObjetivo;
            textoObjetivo.color = hayObjetivo ? TrazoH10.Texto : TrazoH10.TextoTenue;
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            textoHora.text = reloj != null ? HorarioLugar.TextoHora(reloj.Hora) : "-";
            textoDia.text = reloj != null ? reloj.Dia.ToString() : "-";
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
            KitUI.VestirTurquesa(contenido.transform); // T-897: también la pausa ya guardada en el prefab
            VestirH10();
            continuar.AlPulsar.AddListener(Cerrar);
            guardar.AlPulsar.AddListener(() => AbrirRanuras(ModoRanuras.Guardar, guardar));
            cargar.AlPulsar.AddListener(() => AbrirRanuras(ModoRanuras.Cargar, cargar));
            opciones.AlPulsar.AddListener(AbrirOpciones);
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
                ranuras.Cerrada -= AlCerrarEncima;
            }
            if (pantallaOpciones != null)
            {
                pantallaOpciones.Cerrada -= AlCerrarEncima;
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
            opciones.interactable = PantallaOpciones != null;
            contenido.SetActive(true);
            RefrescarDatos();
            AnimH10.EntrarPanel(panelOpciones, new Vector2(-AnimH10.DesplazamientoEntrada, 0f));
            AnimH10.EntrarPanel(panelDatos, new Vector2(AnimH10.DesplazamientoEntrada, 0f), AnimH10.SegundosEscalon);
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
            if (pantallaOpciones != null && pantallaOpciones.Abierta)
            {
                pantallaOpciones.Cerrar();
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

        private void AbrirOpciones()
        {
            PantallaOpciones pantalla = PantallaOpciones;
            if (pantalla == null)
            {
                return;
            }
            volverA = opciones;
            pantalla.Abrir();
            menu.enabled = false;
        }

        // Las ranuras o las opciones se cerraron (VOLVER, Esc o guardar): la pausa vuelve a su menú, con el botón que las abrió elegido.
        private void AlCerrarEncima()
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
            MechaCyberpunk.UI.Titulo.PantallaCargando.Mostrar();
            FlujoPartida.IrAlTitulo();
        }
    }
}

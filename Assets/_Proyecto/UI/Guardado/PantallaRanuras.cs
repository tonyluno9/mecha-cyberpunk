using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Mapa;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Guardado
{
    /// <summary>
    /// La pantalla de ranuras (T-412, D-tony-244), hecha con el kit en un canvas de orden 70: AUTOGUARDADO arriba y las ranuras 1 a 3, cada
    /// una con "RANURA 2 · 29 OCT 21:14 · EL TIANGUIS · Ƀ340" y el objetivo abajo en turquesa, o "VACÍA". En GUARDAR se eligen las 3 manuales
    /// (sobrescribir pide "¿SOBRESCRIBIR?" SÍ/NO en magenta) y AUTO queda desactivada; en CARGAR se eligen las 4 llenas y cargar recarga la
    /// escena con <see cref="FlujoPartida.IrACargar"/>. Esc o VOLVER la cierran. Mientras está abierta el jugador no se mueve ni dispara.
    /// Hasta que llegue la pausa (T-410), F5 la abre en GUARDAR y F9 en CARGAR, solo fuera de DEMO_FINAL.
    /// </summary>
    public class PantallaRanuras : MonoBehaviour
    {
        public const int OrdenCanvas = 70;
        public const int CantidadFilas = ServicioGuardado.UltimaRanura + 1;
        public const string TituloGuardar = "GUARDAR PARTIDA";
        public const string TituloCargar = "CARGAR PARTIDA";
        public const string TextoVolver = "VOLVER";
        public const string TextoSobrescribir = "¿SOBRESCRIBIR?";
        public const string TextoSi = "SÍ";
        public const string TextoNo = "NO";
        public const string TextoAyuda = "↑ ↓ ELEGIR · ENTER ACEPTAR · ESC VOLVER";

        [Header("Teclas de prueba (hasta la pausa, T-410)")]
        [SerializeField] private InputAction abrirGuardar = new InputAction("AbrirGuardar", InputActionType.Button, "<Keyboard>/f5");
        [SerializeField] private InputAction abrirCargar = new InputAction("AbrirCargar", InputActionType.Button, "<Keyboard>/f9");

        [Header("Medidas (px a 1920x1080)")]
        [SerializeField] private Vector2 tamanoFila = new Vector2(1080f, 96f);
        [SerializeField] private Vector2 tamanoConfirmar = new Vector2(560f, 260f);

        [Header("Piezas (las arma el creador con el kit)")]
        [SerializeField] private GameObject contenido;
        [SerializeField] private MenuCyberfix menu;
        [SerializeField] private Text titulo;
        [SerializeField] private BotonCyberfix[] filas;
        [SerializeField] private Text[] objetivos;
        [SerializeField] private BotonCyberfix volver;
        [SerializeField] private GameObject confirmar;
        [SerializeField] private MenuCyberfix menuConfirmar;
        [SerializeField] private BotonCyberfix si;
        [SerializeField] private BotonCyberfix no;

        private ModoRanuras modo;
        private int ranuraConfirmar = -1;
        private ControladorAPie controlCongelado;

        public bool Abierta => contenido != null && contenido.activeSelf;
        public bool ConfirmacionAbierta => confirmar != null && confirmar.activeSelf;
        public ModoRanuras Modo => modo;
        public MenuCyberfix Menu => menu;
        public MenuCyberfix MenuConfirmar => menuConfirmar;
        public Text Titulo => titulo;
        public BotonCyberfix Fila(int ranura) => filas[ranura];
        public Text Objetivo(int ranura) => objetivos[ranura];
        public BotonCyberfix Volver => volver;

        /// <summary>Arma todas las piezas con el kit (lo llama el creador del prefab, o Awake si faltan).</summary>
        public void Armar()
        {
            EstiloCyberfix e = KitUI.Estilo;
            float relleno = e.Relleno;
            float separacion = e.SeparacionBotones;
            float altoTitulo = e.Tamano(RolTexto.Encabezado) + relleno;
            float altoAyuda = e.AltoFilaHud;
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasRanuras", OrdenCanvas);
            contenido = canvas.gameObject;
            KitUI.CrearFondoPantalla(canvas.transform, "FondoPantalla");

            float altoFilas = CantidadFilas * tamanoFila.y + (CantidadFilas - 1) * separacion;
            Vector2 tamanoPanel = new Vector2(tamanoFila.x + 2f * relleno,
                relleno + altoTitulo + relleno + altoFilas + e.SeparacionBloques + e.TamanoBotonMenu.y + relleno + altoAyuda + relleno);
            RectTransform panel = KitUI.CrearPanel(canvas.transform, "Panel", tamanoPanel);
            menu = panel.gameObject.AddComponent<MenuCyberfix>();

            titulo = KitUI.CrearTexto(panel, "Titulo", RolTexto.Encabezado, TextAnchor.MiddleCenter);
            KitUI.Colocar(titulo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -relleno), new Vector2(tamanoPanel.x, altoTitulo));
            titulo.color = e.Naranja;

            filas = new BotonCyberfix[CantidadFilas];
            objetivos = new Text[CantidadFilas];
            float y = -(relleno + altoTitulo + relleno);
            for (int i = 0; i < CantidadFilas; i++)
            {
                BotonCyberfix fila = KitUI.CrearBoton(panel, FormatoRanura.Nombre(i), null);
                fila.name = "Fila_" + FormatoRanura.Nombre(i).Replace(' ', '_');
                KitUI.Colocar((RectTransform)fila.transform, new Vector2(0.5f, 1f), new Vector2(0f, y), tamanoFila);
                // La línea arriba y el objetivo abajo, en turquesa.
                RectTransform etiqueta = fila.Texto.rectTransform;
                etiqueta.anchorMin = new Vector2(0f, 0.42f);
                etiqueta.anchorMax = Vector2.one;
                etiqueta.offsetMin = new Vector2(relleno, 0f);
                etiqueta.offsetMax = new Vector2(-relleno, 0f);
                Text objetivo = KitUI.CrearTexto(fila.transform, "Objetivo", RolTexto.Ayuda, TextAnchor.MiddleLeft);
                RectTransform rectObjetivo = objetivo.rectTransform;
                rectObjetivo.anchorMin = Vector2.zero;
                rectObjetivo.anchorMax = new Vector2(1f, 0.42f);
                rectObjetivo.offsetMin = new Vector2(relleno * 2.5f, relleno * 0.5f);
                rectObjetivo.offsetMax = new Vector2(-relleno, 0f);
                objetivo.color = e.Turquesa;
                filas[i] = fila;
                objetivos[i] = objetivo;
                y -= tamanoFila.y + separacion;
            }
            y += separacion - e.SeparacionBloques;
            volver = KitUI.CrearBoton(panel, TextoVolver, null);
            KitUI.Colocar((RectTransform)volver.transform, new Vector2(0.5f, 1f), new Vector2(0f, y), e.TamanoBotonMenu);

            Text ayuda = KitUI.CrearTexto(panel, "Ayuda", RolTexto.Ayuda, TextAnchor.MiddleCenter);
            KitUI.Colocar(ayuda.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, relleno), new Vector2(tamanoPanel.x, altoAyuda));
            ayuda.text = TextoAyuda;
            ayuda.color = e.Turquesa;

            // ¿SOBRESCRIBIR? SÍ / NO, encima de todo.
            RectTransform caja = KitUI.CrearPanel(canvas.transform, "Confirmar", tamanoConfirmar);
            confirmar = caja.gameObject;
            menuConfirmar = confirmar.AddComponent<MenuCyberfix>();
            Text pregunta = KitUI.CrearTexto(caja, "Pregunta", RolTexto.Encabezado, TextAnchor.MiddleCenter);
            KitUI.Colocar(pregunta.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -relleno), new Vector2(tamanoConfirmar.x, altoTitulo));
            pregunta.text = TextoSobrescribir;
            pregunta.color = e.Magenta;
            float yBoton = -(relleno + altoTitulo + relleno);
            si = KitUI.CrearBoton(caja, TextoSi, null);
            KitUI.Colocar((RectTransform)si.transform, new Vector2(0.5f, 1f), new Vector2(0f, yBoton), e.TamanoBotonMenu);
            no = KitUI.CrearBoton(caja, TextoNo, null);
            KitUI.Colocar((RectTransform)no.transform, new Vector2(0.5f, 1f), new Vector2(0f, yBoton - e.TamanoBotonMenu.y - separacion), e.TamanoBotonMenu);
            confirmar.SetActive(false);
            contenido.SetActive(false);
        }

        private void Awake()
        {
            if (contenido == null || menu == null || filas == null || filas.Length != CantidadFilas)
            {
                Armar();
            }
            for (int i = 0; i < filas.Length; i++)
            {
                int ranura = i;
                filas[i].AlPulsar.AddListener(() => AlPulsarRanura(ranura));
            }
            volver.AlPulsar.AddListener(Cerrar);
            si.AlPulsar.AddListener(AlConfirmar);
            no.AlPulsar.AddListener(CancelarConfirmacion);
            menu.Regresar += Cerrar;
            menuConfirmar.Regresar += CancelarConfirmacion;
            confirmar.SetActive(false);
            contenido.SetActive(false);
        }

        private void OnEnable()
        {
#if !DEMO_FINAL
            abrirGuardar.Enable();
            abrirCargar.Enable();
#endif
        }

        private void OnDisable()
        {
            abrirGuardar.Disable();
            abrirCargar.Disable();
            Cerrar();
        }

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
        }

        private void Update()
        {
            if (Abierta || MenuCyberfix.Abiertos > 0 || PantallaMapa.HayAbierto)
            {
                return;
            }
            if (abrirGuardar.WasPressedThisFrame())
            {
                Abrir(ModoRanuras.Guardar);
            }
            else if (abrirCargar.WasPressedThisFrame())
            {
                Abrir(ModoRanuras.Cargar);
            }
        }

        public void Abrir(ModoRanuras nuevoModo)
        {
            if (Abierta)
            {
                return;
            }
            modo = nuevoModo;
            titulo.text = modo == ModoRanuras.Guardar ? TituloGuardar : TituloCargar;
            Refrescar();
            confirmar.SetActive(false);
            contenido.SetActive(true);
            menu.enabled = true;
            menu.Recargar();
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
        }

        public void Cerrar()
        {
            if (!Abierta)
            {
                return;
            }
            ranuraConfirmar = -1;
            confirmar.SetActive(false);
            contenido.SetActive(false);
            if (controlCongelado != null)
            {
                controlCongelado.ActivarControl(true);
                controlCongelado = null;
            }
        }

        // Lee el resumen de cada ranura sin cargarla y decide cuáles se pueden elegir.
        private void Refrescar()
        {
            ServicioGuardado servicio = ServicioGuardado.Actual;
            EstiloCyberfix e = KitUI.Estilo;
            for (int i = 0; i < filas.Length; i++)
            {
                ResumenRanura resumen = servicio != null ? servicio.ResumenDe(i) : ResumenRanura.SinPartida(i);
                filas[i].CambiarEtiqueta(FormatoRanura.Linea(resumen));
                bool activa = modo == ModoRanuras.Guardar ? i != ServicioGuardado.RanuraAuto : !resumen.Vacia;
                filas[i].interactable = activa;
                objetivos[i].text = resumen.Vacia ? string.Empty : resumen.Objetivo;
                objetivos[i].color = activa ? e.Turquesa : e.Desactivado;
            }
        }

        private void AlPulsarRanura(int ranura)
        {
            ServicioGuardado servicio = ServicioGuardado.Actual;
            if (servicio == null)
            {
                Debug.LogWarning("[UI] PantallaRanuras sin ServicioGuardado en la escena.", this);
                return;
            }
            if (modo == ModoRanuras.Cargar)
            {
                Cerrar();
                FlujoPartida.IrACargar(ranura);
                return;
            }
            if (!servicio.ResumenDe(ranura).Vacia)
            {
                AbrirConfirmacion(ranura);
                return;
            }
            GuardarEn(ranura);
        }

        private void GuardarEn(int ranura)
        {
            ServicioGuardado servicio = ServicioGuardado.Actual;
            bool ok = servicio != null && servicio.Guardar(ranura);
            Debug.Log("[UI] Guardar en " + FormatoRanura.Nombre(ranura) + ": " + (ok ? "guardada" : "falló"));
            Cerrar();
        }

        private void AbrirConfirmacion(int ranura)
        {
            ranuraConfirmar = ranura;
            // Primero se abre la confirmación y luego se apaga el menú, para que el cursor no se bloquee entre medio.
            confirmar.SetActive(true);
            menu.enabled = false;
            no.Elegir();
        }

        private void AlConfirmar()
        {
            int ranura = ranuraConfirmar;
            CancelarConfirmacion();
            if (ranura >= 0)
            {
                GuardarEn(ranura);
            }
        }

        private void CancelarConfirmacion()
        {
            if (!ConfirmacionAbierta)
            {
                return;
            }
            int ranura = ranuraConfirmar;
            ranuraConfirmar = -1;
            menu.enabled = true;
            confirmar.SetActive(false);
            if (ranura >= 0 && ranura < filas.Length)
            {
                filas[ranura].Elegir();
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Tiendas;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Armas
{
    /// <summary>
    /// T-59: el banco de armas de Tino. Al hablar con él se abre un solo banco con dos pestañas arriba al centro: COMPRAR (todas las armas y las balas,
    /// la pantalla del mostrador de armas) y MEJORAR (las armas que tengas, <see cref="PantallaMejoraArmas"/>). Se cambia de pestaña con clic o con TAB;
    /// Esc o SALIR cierran todo. Congela el control a pie mientras está abierto y lo devuelve un cuadro después de cerrar. El mostrador de armas y el
    /// banco de trabajo dejan de ofrecerse por su cuenta (<see cref="QuitarInteractuables"/>): ahora solo se llega a ellos hablando con Tino.
    /// </summary>
    public class PestanasBancoTino : MonoBehaviour
    {
        public const int OrdenCanvas = 120;
        public const string TextoComprar = "COMPRAR";
        public const string TextoMejorar = "MEJORAR";
        private static readonly Vector2 TamanoPestana = new Vector2(240f, 52f);
        private const float SeparacionPestanas = 10f;
        private const float MargenArriba = 14f;

        private static PestanasBancoTino actual;
        private static readonly List<BotonExtra> botonesExtra = new List<BotonExtra>();

        /// <summary>T-78: un botón que otra función agrega a la barra de arriba del banco (por ejemplo CAMPO DE TIRO, T-79).</summary>
        public sealed class BotonExtra
        {
            public string Etiqueta;
            public Action Accion;
            public Func<bool> Visible;
            internal Text Texto;
            internal GameObject Caja;
        }

        /// <summary>
        /// T-78: PUNTO DE EXTENSIÓN para T-79. Registra un botón extra en la barra de pestañas del banco (a la derecha de MEJORAR). Se crea la próxima vez que
        /// se abre el banco; <paramref name="visible"/> (opcional) decide si se ve cada vez que se abre. Ejemplo: <c>PestanasBancoTino.RegistrarBotonExtra("CAMPO DE TIRO", abrirCampo)</c>.
        /// </summary>
        public static BotonExtra RegistrarBotonExtra(string etiqueta, Action accion, Func<bool> visible = null)
        {
            BotonExtra boton = new BotonExtra { Etiqueta = etiqueta, Accion = accion, Visible = visible };
            botonesExtra.Add(boton);
            return boton;
        }

        /// <summary>Quita un botón registrado con <see cref="RegistrarBotonExtra"/> (y su objeto, si ya se creó).</summary>
        public static void QuitarBotonExtra(BotonExtra boton)
        {
            botonesExtra.Remove(boton);
            if (boton != null && boton.Caja != null)
            {
                Destroy(boton.Caja);
            }
        }

        /// <summary>
        /// T-78: mientras devuelva un texto no vacío el banco NO se puede cerrar (Esc ni SALIR) y ese texto sale bajo las pestañas ("COMPRA UN ARMA").
        /// La pone la historia en el paso de Tino; null = el banco cierra libre.
        /// </summary>
        public static Func<string> BloqueoSalida { get; set; }

        /// <summary>
        /// T-84: tutorial guiado de la primera visita. La historia lo pone en true en el paso Armas y en false al salir de él. Con él encendido todo el
        /// banco queda oscuro y bloqueado salvo el control del paso actual (<see cref="PasoTutorial"/>), y las teclas no se saltan el guion.
        /// </summary>
        public static bool TutorialGuiado { get; set; }

        /// <summary>T-84: el paso actual del tutorial (Ninguno si está apagado o el banco está cerrado).</summary>
        public static PasoTutorialTino PasoTutorial => actual != null && actual.abierto && TutorialGuiado ? actual.paso : PasoTutorialTino.Ninguno;

        /// <summary>T-84: el número de paso mostrado (1 a 5), 0 si no hay tutorial.</summary>
        public static int NumeroPasoTutorial => ReglaTutorialTino.Numero(PasoTutorial);

        /// <summary>T-84: la línea que se ve ("PASO 2 DE 5: AHORA PULSA COMPRAR"), vacía si no hay tutorial.</summary>
        public static string TextoTutorial => ReglaTutorialTino.Linea(PasoTutorial);

        /// <summary>T-84: cuántas celdas oscuras bloquean la pantalla ahora (pruebas); 0 si la guía no está bloqueando.</summary>
        public static int CeldasBloqueando => actual != null && actual.guia != null ? actual.guia.CeldasActivas : 0;

        /// <summary>T-84: el control que está resaltado ahora (nombres de GameObject), para que las pruebas hagan clic ahí.</summary>
        public static IReadOnlyList<RectTransform> ObjetivosResaltados => actual != null ? actual.objetivos : (IReadOnlyList<RectTransform>)new RectTransform[0];

        /// <summary>T-79: true mientras el jugador está en el campo de tiro (salió del banco por el botón y regresará a él). La historia lo trata como banco abierto.</summary>
        public static bool EnCampoDeTiro { get; set; }

        /// <summary>T-79: el banco está abierto o el jugador está en el campo de tiro (que vuelve al banco).</summary>
        public static bool AbiertoOEnCampo => Abierto || EnCampoDeTiro;

        /// <summary>La línea de lo que falta para poder salir (pruebas); vacía si no hay bloqueo.</summary>
        public static string LineaFalta => actual != null && actual.cajaFalta != null && actual.cajaFalta.activeSelf ? actual.textoFalta.text : string.Empty;

        private MostradorTienda mostrador;
        private BancoArmas banco;
        private Action alCerrar;
        private bool abierto;
        private bool enMejorar;
        private ControladorAPie controlCongelado;
        private InputAction cambiar;
        private GameObject raiz;
        private Image lineaComprar;
        private Image lineaMejorar;
        private Text textoComprar;
        private Text textoMejorar;
        private Text textoFalta;
        private GameObject cajaFalta;
        private RectTransform barraPestanas;
        private RectTransform botonSalir;
        private GuiaTutorialTino guia;
        private PantallaTienda pantallaTienda;
        private PasoTutorialTino paso;
        private string nombrePistola = "PISTOLA";
        private float proximaBusqueda;
        private float proximaPantalla;
        private bool guiaEncendida;
        private PasoTutorialTino pasoBuscado;
        private readonly List<RectTransform> objetivos = new List<RectTransform>();
        private const float SegundosEntreBusquedas = 0.2f;
        private const float AnchoSalir = 180f;
        private const string TextoSalir = "SALIR";

        /// <summary>True mientras el banco de Tino está abierto.</summary>
        public static bool Abierto => actual != null && actual.abierto;

        /// <summary>True si la pestaña elegida es MEJORAR (pruebas).</summary>
        public static bool EnMejorar => actual != null && actual.enMejorar;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            actual = null;
            BloqueoSalida = null;
            TutorialGuiado = false;
            EnCampoDeTiro = false;
            botonesExtra.Clear();
        }

        /// <summary>El mostrador de armas de la ciudad (el que vende armas) o null.</summary>
        public static MostradorTienda BuscarMostradorArmas()
        {
            // T-1755: el que vende armas BASE y balas (el del Mercado Negro, que ya no se usa suelto); el del CIC solo trae niveles de mejora.
            MostradorTienda mejor = null;
            int puntos = 0;
            foreach (MostradorTienda m in FindObjectsByType<MostradorTienda>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                int esta = 0;
                foreach (DatosObjeto objeto in m.EnVenta)
                {
                    if (objeto is DatosArma arma)
                    {
                        esta += arma.Nivel == 0 ? PuntosArmaBase : 1;
                    }
                    else if (objeto is DatosMunicionArma)
                    {
                        esta += PuntosArmaBase;
                    }
                }
                if (esta > puntos)
                {
                    puntos = esta;
                    mejor = m;
                }
            }
            return mejor;
        }

        private const int PuntosArmaBase = 10;

        /// <summary>
        /// Quita de las interacciones sueltas el mostrador de armas y el banco de trabajo (solo se llega a ellos hablando con Tino).
        /// Devuelve true si encontró los dos.
        /// </summary>
        public static bool QuitarInteractuables()
        {
            MostradorTienda armas = BuscarMostradorArmas();
            BancoArmas trabajo = FindFirstObjectByType<BancoArmas>(FindObjectsInactive.Exclude);
            if (armas != null)
            {
                Registro<IInteractuable>.Quitar(armas);
            }
            if (trabajo != null)
            {
                Registro<IInteractuable>.Quitar(trabajo);
            }
            return armas != null && trabajo != null;
        }

        /// <summary>Abre el banco en COMPRAR. Devuelve false si no hay ni mostrador de armas ni banco de trabajo, o ya está abierto.</summary>
        public static bool Abrir(Action alCerrar = null)
        {
            if (Abierto)
            {
                return false;
            }
            MostradorTienda armas = BuscarMostradorArmas();
            BancoArmas trabajo = FindFirstObjectByType<BancoArmas>(FindObjectsInactive.Exclude);
            if ((armas == null && trabajo == null) || !(Registro<IJugador>.Actual is JugadorAPie jugador) || jugador.Modo != ModoJugador.APie)
            {
                return false;
            }
            if (actual == null)
            {
                actual = new GameObject("PestanasBancoTino").AddComponent<PestanasBancoTino>();
            }
            actual.Iniciar(armas, trabajo, alCerrar);
            return true;
        }

        /// <summary>Cierra el banco (Esc, SALIR o pruebas).</summary>
        public static void Cerrar()
        {
            if (Abierto)
            {
                actual.Salir();
            }
        }

        /// <summary>Elige la pestaña (clic o pruebas).</summary>
        public static void Elegir(bool mejorar)
        {
            if (Abierto)
            {
                actual.CambiarA(mejorar);
            }
        }

        private void Awake()
        {
            cambiar = new InputAction("BancoTinoPestana", InputActionType.Button, "<Keyboard>/tab");
            Construir();
            raiz.SetActive(false);
        }

        private void OnDestroy()
        {
            cambiar?.Dispose();
            if (actual == this)
            {
                actual = null;
            }
        }

        private void Construir()
        {
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasPestanasBanco", OrdenCanvas);
            raiz = canvas.gameObject;
            float ancho = TamanoPestana.x * 2f + SeparacionPestanas;
            RectTransform barra = TrazoH10.Nodo(canvas.transform, "Pestanas");
            TrazoH10.Colocar(barra, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -MargenArriba), new Vector2(ancho, TamanoPestana.y));
            CrearPestana(barra, "Comprar", TextoComprar, 0f, false, out textoComprar, out lineaComprar);
            CrearPestana(barra, "Mejorar", TextoMejorar, TamanoPestana.x + SeparacionPestanas, true, out textoMejorar, out lineaMejorar);
            barraPestanas = barra;
            // Abajo al centro, con fondo y marco propios: arriba quedaba detrás del letrero del CIC y no se leía.
            RectTransform falta = TrazoH10.Nodo(canvas.transform, "LineaFalta");
            TrazoH10.Colocar(falta, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(760f, 64f));
            Image fondoFalta = TrazoH10.Imagen(falta, "Fondo", TrazoH10.ConAlfa(TrazoH10.FondoPantalla, 0.95f), TrazoH10.Relleno);
            TrazoH10.Estirar(fondoFalta.rectTransform);
            Image marcoFalta = TrazoH10.Imagen(falta, "Marco", TrazoH10.Coral, TrazoH10.Linea);
            TrazoH10.Estirar(marcoFalta.rectTransform);
            textoFalta = TrazoH10.Etiqueta(falta, "Texto", string.Empty, TrazoH10.TamanoSeccion, TrazoH10.Coral, TextAnchor.MiddleCenter);
            TrazoH10.Estirar(textoFalta.rectTransform);
            VestidoH10.Fuente(falta);
            cajaFalta = falta.gameObject;
            cajaFalta.SetActive(false);
            ConstruirBotonSalir(barra);
            // La guía va al final: queda sobre las pestañas, el botón SALIR y los botones extra.
            guia = new GuiaTutorialTino(canvas.transform);
        }

        // T-84: SALIR propio del banco, en la barra de arriba (este lienzo va sobre los paneles de COMPRAR y MEJORAR): se ve en todas las pestañas.
        private void ConstruirBotonSalir(RectTransform barra)
        {
            RectTransform caja = TrazoH10.Nodo(barra, "BotonSalirBanco");
            botonSalir = caja;
            TrazoH10.Colocar(caja, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2((TamanoPestana.x + SeparacionPestanas) * 2f, 0f), new Vector2(AnchoSalir, TamanoPestana.y));
            Image fondo = TrazoH10.Imagen(caja, "Fondo", TrazoH10.ConAlfa(TrazoH10.FondoPanel, 0.97f), TrazoH10.Relleno);
            TrazoH10.Estirar(fondo.rectTransform);
            fondo.raycastTarget = true;
            Image linea = TrazoH10.Imagen(caja, "Linea", TrazoH10.Coral, TrazoH10.Linea);
            TrazoH10.Estirar(linea.rectTransform);
            Text texto = TrazoH10.Etiqueta(caja, "Texto", TextoSalir, TrazoH10.TamanoFila, TrazoH10.Coral, TextAnchor.MiddleCenter);
            TrazoH10.Estirar(texto.rectTransform);
            EventTrigger disparador = caja.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry clic = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            clic.callback.AddListener(_ =>
            {
                Sonidos.Reproducir("UI_CANCELAR");
                Salir();
            });
            disparador.triggers.Add(clic);
            VestidoH10.Fuente(caja);
        }

        // T-78: crea los botones extra que se registraron y aún no tienen objeto (a la derecha de MEJORAR) y muestra solo los visibles.
        private void PrepararBotonesExtra()
        {
            float x = (TamanoPestana.x + SeparacionPestanas) * 2f;
            for (int i = 0; i < botonesExtra.Count; i++)
            {
                BotonExtra boton = botonesExtra[i];
                if (boton.Caja == null)
                {
                    RectTransform caja = TrazoH10.Nodo(barraPestanas, "Extra_" + boton.Etiqueta);
                    TrazoH10.Colocar(caja, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, 0f), TamanoPestana);
                    Image fondo = TrazoH10.Imagen(caja, "Fondo", TrazoH10.ConAlfa(TrazoH10.FondoPanel, 0.9f), TrazoH10.Relleno);
                    TrazoH10.Estirar(fondo.rectTransform);
                    fondo.raycastTarget = true;
                    Image linea = TrazoH10.Imagen(caja, "Linea", TrazoH10.TurquesaTenue, TrazoH10.Linea);
                    TrazoH10.Estirar(linea.rectTransform);
                    boton.Texto = TrazoH10.Etiqueta(caja, "Texto", boton.Etiqueta, TrazoH10.TamanoFila, TrazoH10.Turquesa, TextAnchor.MiddleCenter);
                    TrazoH10.Estirar(boton.Texto.rectTransform);
                    EventTrigger disparador = caja.gameObject.AddComponent<EventTrigger>();
                    EventTrigger.Entry clic = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                    BotonExtra capturado = boton;
                    clic.callback.AddListener(_ => capturado.Accion?.Invoke());
                    disparador.triggers.Add(clic);
                    VestidoH10.Fuente(caja);
                    boton.Caja = caja.gameObject;
                }
                boton.Caja.SetActive(boton.Visible == null || boton.Visible());
                x += TamanoPestana.x + SeparacionPestanas;
            }
            if (botonSalir != null)
            {
                // SALIR siempre al final de la barra y al frente de sus hermanos.
                botonSalir.anchoredPosition = new Vector2(x, 0f);
                botonSalir.SetAsLastSibling();
            }
        }

        private void CrearPestana(Transform padre, string nombre, string etiqueta, float x, bool mejorar, out Text texto, out Image linea)
        {
            RectTransform caja = TrazoH10.Nodo(padre, "Pestana_" + nombre);
            TrazoH10.Colocar(caja, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, 0f), TamanoPestana);
            Image fondo = TrazoH10.Imagen(caja, "Fondo", TrazoH10.ConAlfa(TrazoH10.FondoPanel, 0.9f), TrazoH10.Relleno);
            TrazoH10.Estirar(fondo.rectTransform);
            fondo.raycastTarget = true;
            linea = TrazoH10.Imagen(caja, "Linea", TrazoH10.TurquesaTenue, TrazoH10.Linea);
            TrazoH10.Estirar(linea.rectTransform);
            texto = TrazoH10.Etiqueta(caja, "Texto", etiqueta, TrazoH10.TamanoFila, TrazoH10.Turquesa, TextAnchor.MiddleCenter);
            TrazoH10.Estirar(texto.rectTransform);
            EventTrigger disparador = caja.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry clic = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            clic.callback.AddListener(_ => CambiarA(mejorar));
            disparador.triggers.Add(clic);
            VestidoH10.Fuente(caja);
        }

        private void Iniciar(MostradorTienda armas, BancoArmas trabajo, Action alCerrarBanco)
        {
            mostrador = armas;
            banco = trabajo;
            alCerrar = alCerrarBanco;
            abierto = true;
            proximaPantalla = 0f;
            proximaBusqueda = 0f;
            pasoBuscado = PasoTutorialTino.Ninguno;
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            ControladorAPie controlador = jugador != null ? jugador.GetComponent<ControladorAPie>() : null;
            if (controlador != null && controlador.ControlActivo)
            {
                controlador.ActivarControl(false);
                controlCongelado = controlador;
            }
            KitUI.AsegurarEventSystem();
            raiz.SetActive(true);
            PrepararBotonesExtra();
            cambiar.Enable();
            // Con el mostrador vacío (no hay armas en venta) se arranca en MEJORAR, y al revés.
            enMejorar = mostrador == null;
            AbrirPestana();
        }

        private void AbrirPestana()
        {
            if (enMejorar)
            {
                if (banco != null && banco.Pantalla != null)
                {
                    banco.Pantalla.Mostrar(Salir);
                }
            }
            else if (mostrador != null)
            {
                mostrador.MostrarPantalla(Salir, string.Empty); // T-1755: sin "EL MERCADO" arriba: ahí van las pestañas
            }
            Pintar();
        }

        private void CerrarPestana()
        {
            if (enMejorar)
            {
                if (banco != null && banco.Pantalla != null)
                {
                    banco.Pantalla.Ocultar();
                }
            }
            else if (mostrador != null)
            {
                mostrador.OcultarPantalla();
            }
        }

        private void CambiarA(bool mejorar)
        {
            if (!abierto || mejorar == enMejorar || (mejorar && (banco == null || banco.Pantalla == null)) || (!mejorar && mostrador == null))
            {
                return;
            }
            CerrarPestana();
            enMejorar = mejorar;
            AbrirPestana();
            Sonidos.Reproducir("UI_MOVER");
        }

        private void Update()
        {
            // Una ventana de aviso encima atiende las teclas.
            if (abierto && cambiar.WasPressedThisFrame() && !VentanaAviso.Abierta && !TutorialGuiado)
            {
                CambiarA(!enMejorar); // T-84: con el tutorial, TAB no se salta el guion
            }
            if (abierto && textoFalta != null)
            {
                string falta = TextoFalta();
                textoFalta.text = falta.Length > 0 ? "PARA SALIR: " + falta : string.Empty;
                cajaFalta.SetActive(falta.Length > 0 && !TutorialGuiado);
            }
            ActualizarTutorial();
        }

        // T-84: calcula el paso del estado real y dibuja la guía.
        private void ActualizarTutorial()
        {
            if (!abierto || !TutorialGuiado)
            {
                paso = PasoTutorialTino.Ninguno;
                objetivos.Clear();
                if (guia != null && guiaEncendida)
                {
                    guia.Apagar();
                    guiaEncendida = false;
                }
                return;
            }
            guiaEncendida = true;
            paso = CalcularPaso();
            if (paso != pasoBuscado || Time.unscaledTime >= proximaBusqueda || HayObjetivoPerdido())
            {
                pasoBuscado = paso;
                proximaBusqueda = Time.unscaledTime + SegundosEntreBusquedas;
                BuscarObjetivos();
            }
            guia.Actualizar(objetivos, ReglaTutorialTino.Linea(paso), Time.unscaledTime);
        }

        private bool HayObjetivoPerdido()
        {
            foreach (RectTransform r in objetivos)
            {
                if (r == null || !r.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }
            return false;
        }

        private PasoTutorialTino CalcularPaso()
        {
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            bool tienePistola = arsenal != null && arsenal.Tiene(ArmaAPie.Pistola);
            bool pistolaEnCompra = false;
            if (!enMejorar)
            {
                BuscarPantallaTienda();
                pistolaEnCompra = pantallaTienda != null && pantallaTienda.Mercado != null && pantallaTienda.Mercado.NombreMostrado == NombrePistolaEnLista();
            }
            bool pistolaEnMejorar = enMejorar && banco != null && banco.Pantalla != null && banco.Pantalla.ArmaElegida != null
                && banco.Pantalla.ArmaElegida.Arma == ArmaAPie.Pistola;
            return ReglaTutorialTino.Calcular(true, TextoFalta().Length == 0, tienePistola, enMejorar, pistolaEnCompra, pistolaEnMejorar);
        }

        // El nombre de la pistola base como lo muestra la lista de COMPRAR (en mayúsculas).
        private string NombrePistolaEnLista()
        {
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            if (arsenal != null && arsenal.Catalogo != null)
            {
                var datos = arsenal.Catalogo.Buscar(ArmaAPie.Pistola, 0);
                if (datos != null)
                {
                    nombrePistola = ReglaVistaMercado.Nombre(datos).ToUpperInvariant();
                }
            }
            return nombrePistola;
        }

        private void BuscarPantallaTienda()
        {
            if (pantallaTienda != null && pantallaTienda.Visible)
            {
                return;
            }
            if (Time.unscaledTime < proximaPantalla)
            {
                return;
            }
            pantallaTienda = null;
            proximaPantalla = Time.unscaledTime + SegundosEntreBusquedas;
            foreach (PantallaTienda p in FindObjectsByType<PantallaTienda>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (p.Visible)
                {
                    pantallaTienda = p;
                    return;
                }
            }
        }

        private void BuscarObjetivos()
        {
            objetivos.Clear();
            if (ReglaTutorialTino.ResaltaFilaPistolaCompra(paso))
            {
                RectTransform fila = BuscarFilaPistola();
                if (fila != null)
                {
                    objetivos.Add(fila);
                }
                return;
            }
            foreach (string nombre in ReglaTutorialTino.Objetivos(paso))
            {
                RectTransform r = BuscarPorNombre(barraPestanas, nombre);
                if (r == null && pantallaTienda != null)
                {
                    r = BuscarPorNombre(pantallaTienda.transform, nombre);
                }
                if (r == null && banco != null && banco.Pantalla != null && banco.Pantalla.Canvas != null)
                {
                    r = BuscarPorNombre(banco.Pantalla.Canvas.transform, nombre);
                }
                if (r != null)
                {
                    objetivos.Add(r);
                }
            }
        }

        private RectTransform BuscarFilaPistola()
        {
            if (pantallaTienda == null)
            {
                return null;
            }
            string buscada = NombrePistolaEnLista();
            foreach (RectTransform r in pantallaTienda.GetComponentsInChildren<RectTransform>(false))
            {
                if (!r.name.StartsWith("Fila_") || r.name == "Fila_vacia")
                {
                    continue;
                }
                Transform nombre = r.Find("Nombre");
                Text t = nombre != null ? nombre.GetComponent<Text>() : null;
                if (t != null && t.text.Trim().ToUpperInvariant() == buscada)
                {
                    return r;
                }
            }
            return null;
        }

        private static RectTransform BuscarPorNombre(Transform raizBusqueda, string nombre)
        {
            if (raizBusqueda == null)
            {
                return null;
            }
            foreach (RectTransform r in raizBusqueda.GetComponentsInChildren<RectTransform>(false))
            {
                if (r.name == nombre)
                {
                    return r;
                }
            }
            return null;
        }

        /// <summary>T-84: el GameObject de la fila de la pistola en la lista de COMPRAR, o null.</summary>
        public static GameObject FilaPistolaCompra()
        {
            if (actual == null || !actual.abierto || actual.enMejorar)
            {
                return null;
            }
            actual.BuscarPantallaTienda();
            RectTransform fila = actual.BuscarFilaPistola();
            return fila != null ? fila.gameObject : null;
        }

        private static string TextoFalta()
        {
            string falta = BloqueoSalida != null ? BloqueoSalida() : string.Empty;
            return falta ?? string.Empty;
        }

        private void Salir()
        {
            if (!abierto)
            {
                return;
            }
            if (TextoFalta().Length > 0)
            {
                Sonidos.Reproducir("UI_CANCELAR"); // T-78: en el paso de Tino no se sale sin arma y mejora; la línea de arriba dice qué falta
                return;
            }
            if (TutorialGuiado && !ReglaTutorialTino.PermiteSalir(paso))
            {
                Sonidos.Reproducir("UI_CANCELAR"); // T-84: el guion no deja salir antes del paso 5
                return;
            }
            CerrarPestana();
            abierto = false;
            raiz.SetActive(false);
            cambiar.Disable();
            Action callback = alCerrar;
            alCerrar = null;
            StartCoroutine(DevolverControlDespues());
            callback?.Invoke();
        }

        // La tecla que cierra (Esc) no debe reabrir nada: el control vuelve un cuadro después, como en los demás mostradores.
        private IEnumerator DevolverControlDespues()
        {
            yield return null;
            if (controlCongelado != null)
            {
                controlCongelado.ActivarControl(true);
                controlCongelado = null;
            }
        }

        private void Pintar()
        {
            PintarPestana(textoComprar, lineaComprar, !enMejorar);
            PintarPestana(textoMejorar, lineaMejorar, enMejorar);
        }

        private static void PintarPestana(Text texto, Image linea, bool elegida)
        {
            texto.color = elegida ? TrazoH10.Amarillo : TrazoH10.Turquesa;
            linea.color = elegida ? TrazoH10.Amarillo : TrazoH10.TurquesaTenue;
        }

        private void OnDisable()
        {
            if (abierto)
            {
                CerrarPestana();
                abierto = false;
                if (controlCongelado != null)
                {
                    controlCongelado.ActivarControl(true);
                    controlCongelado = null;
                }
            }
        }
    }
}

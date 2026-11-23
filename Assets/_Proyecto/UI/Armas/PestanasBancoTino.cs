using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje;
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

        /// <summary>True mientras el banco de Tino está abierto.</summary>
        public static bool Abierto => actual != null && actual.abierto;

        /// <summary>True si la pestaña elegida es MEJORAR (pruebas).</summary>
        public static bool EnMejorar => actual != null && actual.enMejorar;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            actual = null;
            BloqueoSalida = null;
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
            if (abierto && cambiar.WasPressedThisFrame() && !VentanaAviso.Abierta)
            {
                CambiarA(!enMejorar);
            }
            if (abierto && textoFalta != null)
            {
                string falta = TextoFalta();
                textoFalta.text = falta.Length > 0 ? "PARA SALIR: " + falta : string.Empty;
                cajaFalta.SetActive(falta.Length > 0);
            }
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

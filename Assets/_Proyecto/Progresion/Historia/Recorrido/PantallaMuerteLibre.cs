using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Titulo;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T-historia-libre-23nov: la pantalla HAS MUERTO del modo libre, con las estadísticas (eliminados de cada bando, tiempo, estrellas máximas,
    /// dinero, récord) y dos opciones: REINTENTAR (justo después del campo de tiro) y MENÚ PRINCIPAL.
    /// T93: con el estilo de la UI nueva (guía H10, la de la pausa, las máquinas y el HUD): velo oscuro, cabecera con el título y el tiempo,
    /// panel de ESTADÍSTICAS a la izquierda y panel de opciones a la derecha, letra Rajdhani, filas con línea fina que se enciende
    /// (<see cref="VestidoH10.Boton"/>) y navegación con ↑ ↓ y Enter (<see cref="MenuCyberfix"/>). Su Canvas empieza con "CanvasPantallaAviso"
    /// para que en VR vaya como panel frente a la cabeza, donde el láser usa los botones (ReglaLienzosVR). Usa tiempo sin escala.
    /// T96: los dos botones muestran la pantalla de carga antes de cambiar de escena; la carga se quita sola al llegar el jugador o el menú.
    /// </summary>
    public class PantallaMuerteLibre : MonoBehaviour
    {
        public const string NombreCanvas = "CanvasPantallaAvisoMuerteLibre";
        public const string TextoTitulo = "HAS MUERTO";
        public const string TextoReintentar = "REINTENTAR";
        public const string TextoMenu = "MENÚ PRINCIPAL";
        public const string TextoSubtitulo = "MODO LIBRE · FIN DE LA PARTIDA";
        public const string TextoEtiquetaTiempo = "TIEMPO VIVO";
        public const string TextoSeccionEstadisticas = "ESTADÍSTICAS";
        public const string TextoSeccionOpciones = "¿QUÉ SIGUE?";
        public const string TextoNuevoRecord = "¡NUEVO RÉCORD!";
        public const string TextoAyuda = "↑ ↓ ELEGIR · ENTER ACEPTAR";
        public const string EtiquetaRecord = "RÉCORD";
        private const int OrdenDibujo = 70;
        private const float AlfaVelo = 0.93f;
        private const float AnchoPanelDatos = 640f;
        private const float AnchoPanelOpciones = 520f;
        private const float AltoPaneles = 448f;
        private const float SeparacionPaneles = 24f;
        private const float RellenoPanel = 18f;
        private const float InicioFilas = 62f;
        private const float AltoFila = 40f;
        private const float AnchoEtiqueta = 330f;
        private const float AltoBoton = 64f;
        private const float SeparacionBotones = 12f;
        private const float SegundosEntrada = 0.35f;

        public static PantallaMuerteLibre Actual { get; private set; }

        private string textoEstadisticas = string.Empty;
        private readonly List<Text> valores = new List<Text>();
        private CanvasGroup grupo;
        private MenuCyberfix menu;
        private BotonCyberfix reintentar;
        private BotonCyberfix botonMenu;
        private Text titulo;
        private float entrada;
        private bool elegido;

        /// <summary>El texto de estadísticas que se ve, una línea por dato (pruebas).</summary>
        public string Estadisticas => textoEstadisticas;
        public BotonCyberfix Reintentar => reintentar;
        public BotonCyberfix Menu => botonMenu;
        public MenuCyberfix Navegacion => menu;
        public Text Titulo => titulo;
        /// <summary>Los textos de valor de cada fila de estadísticas, en el orden de <see cref="FilasEstadisticas(DirectorModoLibre)"/> (pruebas).</summary>
        public IReadOnlyList<Text> Valores => valores;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => Actual = null;

        public static PantallaMuerteLibre Abrir(DirectorModoLibre director)
        {
            if (Actual != null)
            {
                return Actual;
            }
            PantallaMuerteLibre p = new GameObject("PantallaMuerteLibre").AddComponent<PantallaMuerteLibre>();
            p.Armar(director);
            return p;
        }

        private void Armar(DirectorModoLibre d)
        {
            Actual = this;
            KitUI.AsegurarEventSystem();
            Canvas canvas = KitUI.CrearCanvas(transform, NombreCanvas, OrdenDibujo);
            Transform raiz = canvas.transform;
            grupo = canvas.gameObject.AddComponent<CanvasGroup>();
            grupo.alpha = 0f;

            Image velo = KitUI.CrearImagen(raiz, "Fondo", TrazoH10.ConAlfa(TrazoH10.FondoPantalla, AlfaVelo));
            KitUI.Estirar(velo.rectTransform);
            velo.raycastTarget = true; // el clic no pasa al juego

            // Cabecera H10: HAS MUERTO en coral (es una alerta, como la confirmación de la pausa) y el tiempo vivo a la derecha.
            Text tiempo = VestidoH10.Cabecera(raiz, TextoTitulo, TextoSubtitulo, TextoEtiquetaTiempo, out titulo);
            titulo.color = TrazoH10.Coral;
            tiempo.text = d != null ? MarcadorModoLibre.FormatoTiempo(d.SegundosVivo) : string.Empty;
            VestidoH10.Pie(raiz, TextoAyuda);

            float anchoTotal = AnchoPanelDatos + SeparacionPaneles + AnchoPanelOpciones;
            float izquierda = -anchoTotal * 0.5f;
            Vector2 centro = new Vector2(0.5f, 0.5f);

            // Panel de estadísticas, a la izquierda.
            RectTransform datos = TrazoH10.Panel(raiz, "PanelEstadisticas", TrazoH10.TurquesaTenue).rectTransform;
            TrazoH10.Colocar(datos, centro, centro, new Vector2(0f, 0.5f), new Vector2(izquierda, 0f), new Vector2(AnchoPanelDatos, AltoPaneles));
            TrazoH10.Seccion(datos, "Seccion", TextoSeccionEstadisticas, new Vector2(RellenoPanel, -12f), AnchoPanelDatos - 2f * RellenoPanel);
            KeyValuePair<string, string>[] filas = FilasEstadisticas(d);
            float y = InicioFilas;
            for (int i = 0; i < filas.Length; i++)
            {
                bool record = d != null && d.NuevoRecord && filas[i].Key == EtiquetaRecord;
                valores.Add(Fila(datos, filas[i].Key, filas[i].Value, record ? TrazoH10.Amarillo : TrazoH10.Texto, y));
                y += AltoFila + SeparacionBotones * 0.5f;
            }
            textoEstadisticas = TextoEstadisticas(d);

            // Panel de opciones, a la derecha: REINTENTAR y MENÚ PRINCIPAL como las filas de la pausa.
            RectTransform opciones = TrazoH10.Panel(raiz, "PanelOpciones", TrazoH10.TurquesaTenue).rectTransform;
            TrazoH10.Colocar(opciones, centro, centro, new Vector2(1f, 0.5f), new Vector2(-izquierda, 0f), new Vector2(AnchoPanelOpciones, AltoPaneles));
            TrazoH10.Seccion(opciones, "Seccion", TextoSeccionOpciones, new Vector2(RellenoPanel, -12f), AnchoPanelOpciones - 2f * RellenoPanel);
            reintentar = KitUI.CrearBoton(opciones, TextoReintentar, AlReintentar);
            botonMenu = KitUI.CrearBoton(opciones, TextoMenu, AlMenu);
            BotonCyberfix[] botones = { reintentar, botonMenu };
            for (int i = 0; i < botones.Length; i++)
            {
                VestidoH10.Boton(botones[i]);
                TrazoH10.Colocar((RectTransform)botones[i].transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -(InicioFilas + i * (AltoBoton + SeparacionBotones))), new Vector2(AnchoPanelOpciones - 2f * RellenoPanel, AltoBoton));
            }
            // La navegación del kit (↑ ↓, Enter, ratón y láser eligen el mismo botón); empieza en REINTENTAR.
            menu = opciones.gameObject.AddComponent<MenuCyberfix>();
            menu.Recargar();
            VestidoH10.Fuente(raiz);

            LiberarCursor();
            Sonidos.Reproducir(MechaCyberpunk.Progresion.Sonido.ReglaSonidoEventos.Muerte);
        }

        // Una fila de dato: etiqueta tenue a la izquierda y valor a la derecha, como el panel SITUACIÓN de la pausa.
        private static Text Fila(RectTransform panel, string etiqueta, string valor, Color color, float y)
        {
            float ancho = AnchoPanelDatos - 2f * RellenoPanel;
            Text nombre = TrazoH10.Etiqueta(panel, "Etiqueta" + etiqueta, etiqueta, TrazoH10.TamanoDato, TrazoH10.TextoTenue, TextAnchor.MiddleLeft);
            TrazoH10.Punto(nombre.rectTransform, new Vector2(0f, 1f), new Vector2(RellenoPanel, -y), new Vector2(AnchoEtiqueta, AltoFila));
            Text texto = TrazoH10.Etiqueta(panel, "Valor" + etiqueta, valor, TrazoH10.TamanoFila, color, TextAnchor.MiddleRight);
            TrazoH10.Punto(texto.rectTransform, new Vector2(0f, 1f), new Vector2(RellenoPanel + AnchoEtiqueta, -y), new Vector2(ancho - AnchoEtiqueta, AltoFila));
            return texto;
        }

        /// <summary>Las filas de estadísticas (etiqueta, valor) del director: eliminados de cada bando, tiempo, estrellas máximas, dinero y récord.</summary>
        public static KeyValuePair<string, string>[] FilasEstadisticas(DirectorModoLibre d)
        {
            if (d == null)
            {
                return new KeyValuePair<string, string>[0];
            }
            ICartera cartera = Registro<ICartera>.Actual;
            return FilasEstadisticas(d.SkullsMuertos, d.PoliciasMuertos, d.NovaMuertos, d.SegundosVivo, d.EstrellasMaximas,
                cartera != null ? cartera.Dinero : 0, d.NuevoRecord, DirectorModoLibre.MejorTiempo);
        }

        /// <summary>Regla pura de las filas (para probarla sin director).</summary>
        public static KeyValuePair<string, string>[] FilasEstadisticas(int skulls, int policias, int nova, float segundosVivo, int estrellasMaximas,
            int dinero, bool nuevoRecord, float mejorTiempo)
        {
            return new[]
            {
                new KeyValuePair<string, string>("SKULLS ELIMINADOS", skulls.ToString()),
                new KeyValuePair<string, string>("POLICÍAS ELIMINADOS", policias.ToString()),
                new KeyValuePair<string, string>("NOVA ELIMINADOS", nova.ToString()),
                new KeyValuePair<string, string>("TIEMPO", MarcadorModoLibre.FormatoTiempo(segundosVivo)),
                new KeyValuePair<string, string>("ESTRELLAS MÁXIMAS", estrellasMaximas.ToString()),
                new KeyValuePair<string, string>("DINERO", FormatoDinero.Texto(dinero)),
                new KeyValuePair<string, string>(EtiquetaRecord, nuevoRecord ? TextoNuevoRecord : MarcadorModoLibre.FormatoTiempo(mejorTiempo)),
            };
        }

        /// <summary>Las líneas de estadísticas en un solo texto ("ETIQUETA  valor" por línea; el récord nuevo solo dice ¡NUEVO RÉCORD!).</summary>
        public static string TextoEstadisticas(DirectorModoLibre d)
        {
            KeyValuePair<string, string>[] filas = FilasEstadisticas(d);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < filas.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append('\n');
                }
                bool recordNuevo = filas[i].Key == EtiquetaRecord && filas[i].Value == TextoNuevoRecord;
                sb.Append(recordNuevo ? TextoNuevoRecord : filas[i].Key + "  " + filas[i].Value);
            }
            return sb.ToString();
        }

        private void Update()
        {
            if (grupo != null && entrada < SegundosEntrada)
            {
                entrada += Time.unscaledDeltaTime;
                grupo.alpha = Mathf.Clamp01(entrada / SegundosEntrada);
            }
            LiberarCursor(); // otro sistema puede volver a bloquearlo
        }

        private static void LiberarCursor()
        {
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            Cursor.visible = true;
        }

        private void AlReintentar()
        {
            if (elegido)
            {
                return;
            }
            elegido = true;
            BloquearBotones();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            // T96: la misma carga que CARGAR y la pausa; se quita sola cuando llega el jugador de la escena nueva.
            PantallaCargando.Mostrar();
            DirectorModoLibre.Reintentar();
        }

        private void AlMenu()
        {
            if (elegido)
            {
                return;
            }
            elegido = true;
            BloquearBotones();
            // T96: se quita sola al cargar el menú principal (Inicio, o el título de la ciudad en el editor), aunque no haya jugador.
            PantallaCargando.Mostrar();
            DirectorModoLibre.IrAlMenu();
        }

        private void BloquearBotones()
        {
            if (grupo != null)
            {
                grupo.interactable = false;
            }
        }

        private void OnDestroy()
        {
            if (Actual == this)
            {
                Actual = null;
            }
        }
    }
}

using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Opciones;
using MechaCyberpunk.UI.Hud;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Inicio
{
    public enum PestanaOpciones
    {
        Graficos,
        Audio,
        Controles,
    }

    /// <summary>
    /// Las opciones de la pantalla de inicio, en tres pestañas (GRÁFICOS, AUDIO y CONTROLES) sobre un panel de cristal. Cada fila lee del
    /// <see cref="ServicioOpciones"/> al abrirse y le escribe al instante lo que el jugador cambia (el servicio lo aplica y lo guarda).
    /// Teclado, control y ratón: ↑ ↓ eligen, ← → cambian, Q y E (o LB y RB) cambian de pestaña, Esc o B regresan.
    /// V8–V10 (VR): con <see cref="ModoVR.Activo"/>, CONTROLES muestra GIRO, VELOCIDAD DE GIRO (VR18), VIÑETA y CORRER (se guardan en
    /// <see cref="OpcionesVR"/>) en lugar de la sensibilidad del ratón y la lista de teclas, que en VR no aplican. Sin VR esas filas no se ven.
    /// </summary>
    public class PanelOpcionesInicio : MonoBehaviour
    {
        public const string TextoTitulo = "OPCIONES";
        public const string TextoVolver = "VOLVER";
        public const string TextoAyuda = "↑ ↓ ELEGIR  ·  ← → CAMBIAR  ·  Q E PESTAÑA  ·  ESC VOLVER";
        /// <summary>T-726: las filas de controles con las teclas reales de las acciones del Input System (no texto fijo).</summary>
        public static string TextoControles => ReglaControlesInicio.Texto(TeclaReal);
        public static readonly string[] TextosPestanas = { "GRÁFICOS", "AUDIO", "CONTROLES" };
        /// <summary>Pedido de Tony (volumen): cuatro deslizadores por el AudioMixer, en este orden. Ambiente e Interfaz van por Efectos.</summary>
        public static readonly string[] TextosVolumen = { "VOLUMEN GENERAL", "MÚSICA", "EFECTOS", "VOCES" };
        /// <summary>La sección de cada deslizador de <see cref="TextosVolumen"/>.</summary>
        public static readonly SeccionSonido[] SeccionesVolumen = { SeccionSonido.General, SeccionSonido.Musica, SeccionSonido.Efectos, SeccionSonido.Voces };
        public static readonly string[] TextosCalidad = { "BAJO", "MEDIO", "ALTO" };
        public static readonly string[] TextosApuntado = { "APAGADO", "SUAVE", "FUERTE" };
        /// <summary>T68: en el orden de <see cref="MomentoDelDia"/>.</summary>
        public static readonly string[] TextosMomentoDia = { "NOCHE", "DÍA" };
        public const string TextoMomentoDia = "MOMENTO DEL DÍA";
        public const string TextoGiroVR = "GIRO";
        public const string TextoVinetaVR = "VIÑETA";
        /// <summary>VR26: en el orden de <see cref="NivelVinetaVR"/>.</summary>
        public static readonly string[] TextosVinetaVR = { "NO", "BAJA", "MEDIA" };
        /// <summary>RUTA: SÍ/NO (la ruta guía en VR).</summary>
        public const string TextoRutaVR = "RUTA";
        /// <summary>VR18: °/s del giro SUAVE con el stick a fondo.</summary>
        public const string TextoVelocidadGiroVR = "VELOCIDAD DE GIRO";
        public const string TextoCorrerVR = "CORRER";
        /// <summary>En el orden de <see cref="ModoGiroVR"/>.</summary>
        public static readonly string[] TextosGiroVR = { "POR PASOS 45°", "SUAVE" };
        /// <summary>En el orden de <see cref="ModoCorrerVR"/>.</summary>
        public static readonly string[] TextosCorrerVR = { "MANTENER", "ALTERNAR" };

        private const float AnchoPanel = 1000f;
        private const float AltoPanel = 860f;
        private const float AltoFila = 64f;
        private const float SeparacionFilas = 8f;
        private const float AnchoFila = 900f;
        private const float AnchoPestana = 280f;
        private const float AltoPestana = 66f;
        private const float YPestanas = -140f;
        private const float YFilas = -236f;

        [SerializeField] private BotonInicio[] pestanas = new BotonInicio[0];
        [SerializeField] private RectTransform[] contenidos = new RectTransform[0];
        [SerializeField] private FilaOpcion calidad;
        [SerializeField] private FilaOpcion resolucion;
        [SerializeField] private FilaOpcion pantallaCompleta;
        [SerializeField] private FilaOpcion momentoDia;
        [SerializeField] private FilaOpcion[] volumenes = new FilaOpcion[0];
        [SerializeField] private FilaOpcion sensibilidad;
        [SerializeField] private FilaOpcion invertirY;
        [SerializeField] private FilaOpcion apuntado;
        [SerializeField] private FilaOpcion tutorial;
        [SerializeField] private FilaOpcion giroVR;
        [SerializeField] private FilaOpcion velocidadGiroVR;
        [SerializeField] private FilaOpcion vinetaVR;
        [SerializeField] private FilaOpcion correrVR;
        [SerializeField] private FilaOpcion rutaVR;
        [SerializeField] private BotonInicio volver;

        [SerializeField] private Text controles;
        [SerializeField] private Image rayaControles;

        private ServicioOpciones servicio;
        private PestanaOpciones actual;
        private bool refrescando;
        private bool filasVR;

        public PestanaOpciones Pestana => actual;
        public BotonInicio Volver => volver;
        /// <summary>El texto de Controles, con las teclas reales.</summary>
        public Text Controles => controles;
        public BotonInicio[] Pestanas => pestanas;
        public FilaOpcion Calidad => calidad;
        public FilaOpcion Resolucion => resolucion;
        public FilaOpcion PantallaCompleta => pantallaCompleta;
        /// <summary>T68: MOMENTO DEL DÍA (NOCHE / DÍA).</summary>
        public FilaOpcion MomentoDia => momentoDia;
        public FilaOpcion[] Volumenes => volumenes;
        public FilaOpcion Sensibilidad => sensibilidad;
        public FilaOpcion InvertirY => invertirY;
        public FilaOpcion Apuntado => apuntado;
        public FilaOpcion Tutorial => tutorial;
        /// <summary>V10: GIRO (POR PASOS 45° / SUAVE). Solo se ve en VR.</summary>
        public FilaOpcion GiroVR => giroVR;
        /// <summary>VR18: VELOCIDAD DE GIRO (30–180 °/s, 120 por defecto). Solo se ve en VR.</summary>
        public FilaOpcion VelocidadGiroVR => velocidadGiroVR;
        /// <summary>VR26: VIÑETA (NO / BAJA / MEDIA). Solo se ve en VR.</summary>
        public FilaOpcion VinetaVR => vinetaVR;
        /// <summary>V9: CORRER (MANTENER / ALTERNAR). Solo se ve en VR.</summary>
        public FilaOpcion CorrerVR => correrVR;
        /// <summary>RUTA (SÍ / NO). Solo se ve en VR.</summary>
        public FilaOpcion RutaVR => rutaVR;
        /// <summary>True si las filas de VR están a la vista (lo que tenía <see cref="ModoVR.Activo"/> al refrescar).</summary>
        public bool FilasVRVisibles => filasVR;

        /// <summary>El primer elemento a elegir al abrir (la pestaña activa).</summary>
        public BotonInicio PrimerElegido => pestanas.Length > (int)actual ? pestanas[(int)actual] : volver;

        /// <summary>Se dispara al pulsar VOLVER (el menú decide qué mostrar).</summary>
        public event Action PidioVolver;

        // El nombre visible de la tecla de una acción: de la acción real del jugador (o del asset real si aún no hay jugador); el mapa y la pausa
        // son teclas del teclado que el juego lee directo, y salen del dispositivo.
        private static string TeclaReal(string accion)
        {
            switch (accion)
            {
                case ReglaControlesInicio.Mover:
                    return TeclasHud.TextoMover(TeclasHud.Buscar(accion));
                case ReglaControlesInicio.Mapa:
                    return Keyboard.current != null ? TeclasHud.Nombre(Keyboard.current.mKey.displayName) : string.Empty;
                case ReglaControlesInicio.Pausa:
                    return Keyboard.current != null ? TeclasHud.Nombre(Keyboard.current.escapeKey.displayName) : string.Empty;
                default:
                    return TeclasHud.Texto(TeclasHud.Buscar(accion));
            }
        }

        public static PanelOpcionesInicio Crear(Transform padre)
        {
            RectTransform raiz = UIInicio.Nodo(padre, "PanelOpciones");
            UIInicio.Punto(raiz, new Vector2(0.5f, 0.5f), new Vector2(420f, 0f), new Vector2(AnchoPanel, AltoPanel));
            PanelOpcionesInicio panel = raiz.gameObject.AddComponent<PanelOpcionesInicio>();

            Image cristal = UIInicio.Imagen(raiz, "Cristal", UIInicio.PanelFondo, SpritesInicio.Panel);
            cristal.type = Image.Type.Sliced;
            UIInicio.Estirar(cristal.rectTransform);
            Image borde = UIInicio.Imagen(raiz, "Borde", UIInicio.ConAlfa(UIInicio.Cian, 0.75f), SpritesInicio.PanelBorde, true);
            borde.type = Image.Type.Sliced;
            UIInicio.Estirar(borde.rectTransform);

            Text titulo = UIInicio.Texto(raiz, "Titulo", TextoTitulo, UIInicio.FuenteTitulos, 78, UIInicio.Blanco, TextAnchor.MiddleLeft);
            UIInicio.Punto(titulo.rectTransform, new Vector2(0f, 1f), new Vector2(54f, -52f), new Vector2(600f, 90f));
            UIInicio.Halo(titulo, UIInicio.Cian, 0.5f, 3f);
            Image rayaTitulo = UIInicio.Imagen(raiz, "RayaTitulo", UIInicio.ConAlfa(UIInicio.Cian, 0.8f), SpritesInicio.DegradadoH, true);
            UIInicio.Punto(rayaTitulo.rectTransform, new Vector2(0f, 1f), new Vector2(54f, -100f), new Vector2(AnchoPanel - 108f, 3f));

            int cantidad = TextosPestanas.Length;
            panel.pestanas = new BotonInicio[cantidad];
            panel.contenidos = new RectTransform[cantidad];
            for (int i = 0; i < cantidad; i++)
            {
                BotonInicio pestana = BotonInicio.Crear(raiz, "Pestana" + i, TextosPestanas[i], new Vector2(AnchoPestana, AltoPestana), 46, TextAnchor.MiddleLeft, UIInicio.Cian);
                UIInicio.Punto(pestana.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(54f + i * (AnchoPestana + 14f), YPestanas), new Vector2(AnchoPestana, AltoPestana));
                PestanaOpciones cual = (PestanaOpciones)i;
                pestana.Elegido += _ => panel.Mostrar(cual);
                pestana.AlPulsar.AddListener(() => panel.Mostrar(cual));
                panel.pestanas[i] = pestana;
                RectTransform contenido = UIInicio.Nodo(raiz, "Contenido" + TextosPestanas[i]);
                UIInicio.Punto(contenido, new Vector2(0.5f, 1f), new Vector2(0f, YFilas), new Vector2(AnchoFila, 520f));
                contenido.pivot = new Vector2(0.5f, 1f);
                panel.contenidos[i] = contenido;
            }

            float y = 0f;
            panel.calidad = Fila(panel.contenidos[0], ref y, f => FilaOpcion.CrearSelector(f, "Calidad", "CALIDAD", TextosCalidad, (int)ReglaOpciones.CalidadPorDefecto, false));
            panel.resolucion = Fila(panel.contenidos[0], ref y, f => FilaOpcion.CrearSelector(f, "Resolucion", "RESOLUCIÓN", new[] { "1920 x 1080" }, 0, false));
            panel.pantallaCompleta = Fila(panel.contenidos[0], ref y, f => FilaOpcion.CrearConmutador(f, "PantallaCompleta", "PANTALLA COMPLETA", true));
            panel.momentoDia = Fila(panel.contenidos[0], ref y, f => FilaOpcion.CrearSelector(f, "MomentoDia", TextoMomentoDia, TextosMomentoDia, (int)ReglaMomentoDia.PorDefecto, true));

            y = 0f;
            panel.volumenes = new FilaOpcion[TextosVolumen.Length];
            for (int i = 0; i < TextosVolumen.Length; i++)
            {
                string texto = TextosVolumen[i];
                panel.volumenes[i] = Fila(panel.contenidos[1], ref y, f => FilaOpcion.CrearDeslizador(f, "Volumen" + texto, texto, 0f, 1f, ReglaOpciones.PasoVolumen,
                    ReglaOpciones.VolumenPorDefecto, ReglaInicio.TextoVolumen));
            }

            y = 0f;
            panel.sensibilidad = Fila(panel.contenidos[2], ref y, f => FilaOpcion.CrearDeslizador(f, "Sensibilidad", "SENSIBILIDAD DEL RATÓN", ReglaOpciones.SensibilidadMinima,
                ReglaOpciones.SensibilidadMaxima, ReglaOpciones.PasoSensibilidad, 1f, v => v.ToString("0.00") + "x"));
            panel.invertirY = Fila(panel.contenidos[2], ref y, f => FilaOpcion.CrearConmutador(f, "InvertirY", "INVERTIR EJE Y", false));
            panel.apuntado = Fila(panel.contenidos[2], ref y, f => FilaOpcion.CrearSelector(f, "Apuntado", "APUNTADO ASISTIDO", TextosApuntado, (int)NivelApuntadoAsistido.Suave, true));
            panel.tutorial = Fila(panel.contenidos[2], ref y, f => FilaOpcion.CrearConmutador(f, "Tutorial", "AVISOS DE TUTORIAL", true));
            // V8–V10: las de VR se acomodan en AplicarFilasVR (ocupan el lugar de la sensibilidad y de la lista de teclas).
            float yVR = y;
            panel.giroVR = Fila(panel.contenidos[2], ref yVR, f => FilaOpcion.CrearSelector(f, "GiroVR", TextoGiroVR, TextosGiroVR, (int)OpcionesVR.GiroPorDefecto, true));
            panel.velocidadGiroVR = Fila(panel.contenidos[2], ref yVR, f => FilaOpcion.CrearDeslizador(f, "VelocidadGiroVR", TextoVelocidadGiroVR,
                OpcionesVR.VelocidadGiroMinima, OpcionesVR.VelocidadGiroMaxima, OpcionesVR.PasoVelocidadGiro, OpcionesVR.VelocidadGiroPorDefecto, TextoVelocidadGiro));
            panel.vinetaVR = Fila(panel.contenidos[2], ref yVR, f => FilaOpcion.CrearSelector(f, "VinetaVR", TextoVinetaVR, TextosVinetaVR, (int)OpcionesVR.NivelVinetaPorDefecto, true));
            panel.correrVR = Fila(panel.contenidos[2], ref yVR, f => FilaOpcion.CrearSelector(f, "CorrerVR", TextoCorrerVR, TextosCorrerVR, (int)OpcionesVR.CorrerPorDefecto, true));
            panel.rutaVR = Fila(panel.contenidos[2], ref yVR, f => FilaOpcion.CrearConmutador(f, "RutaVR", TextoRutaVR, OpcionesVR.RutaPorDefecto));
            Text controles = UIInicio.Texto(panel.contenidos[2], "Controles", TextoControles, UIInicio.FuenteTexto, 27, UIInicio.TextoSuave, TextAnchor.UpperLeft);
            panel.controles = controles;
            UIInicio.Punto(controles.rectTransform, new Vector2(0f, 1f), new Vector2(34f, y - 22f), new Vector2(AnchoFila - 40f, 130f));
            controles.lineSpacing = 1.35f;
            Image rayaControles = UIInicio.Imagen(panel.contenidos[2], "RayaControles", UIInicio.ConAlfa(UIInicio.Naranja, 0.6f), SpritesInicio.DegradadoH, true);
            panel.rayaControles = rayaControles;
            UIInicio.Punto(rayaControles.rectTransform, new Vector2(0f, 1f), new Vector2(34f, y - 10f), new Vector2(AnchoFila - 120f, 2f));

            panel.volver = BotonInicio.Crear(raiz, "Volver", TextoVolver, new Vector2(340f, 70f), 52, TextAnchor.MiddleLeft, UIInicio.Naranja);
            UIInicio.Punto(panel.volver.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(54f, 64f), new Vector2(340f, 70f));
            panel.volver.AlPulsar.AddListener(() => panel.PidioVolver?.Invoke());
            Text ayuda = UIInicio.Texto(raiz, "Ayuda", TextoAyuda, UIInicio.FuenteTexto, 24, UIInicio.TextoApagado, TextAnchor.MiddleRight);
            UIInicio.Punto(ayuda.rectTransform, new Vector2(1f, 0f), new Vector2(-54f, 40f), new Vector2(660f, 36f));
            panel.Conectar();
            panel.AplicarFilasVR();
            panel.Mostrar(PestanaOpciones.Graficos);
            return panel;
        }

        /// <summary>VR18: el texto de la VELOCIDAD DE GIRO ("120°/S").</summary>
        public static string TextoVelocidadGiro(float gradosPorSegundo) => Mathf.RoundToInt(gradosPorSegundo) + "°/S";

        private static FilaOpcion Fila(RectTransform padre, ref float y, Func<Transform, FilaOpcion> crear)
        {
            FilaOpcion fila = crear(padre);
            UIInicio.Punto(fila.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, y - AltoFila * 0.5f), new Vector2(AnchoFila, AltoFila));
            y -= AltoFila + SeparacionFilas;
            return fila;
        }

        private ServicioOpciones Servicio
        {
            get
            {
                if (servicio == null)
                {
                    servicio = Registro<IOpciones>.Actual as ServicioOpciones;
                }
                return servicio;
            }
        }

        private void Conectar()
        {
            calidad.Cambio += f => { if (!refrescando) Servicio?.FijarCalidad((CalidadGrafica)f.Indice); };
            resolucion.Cambio += AlCambiarResolucion;
            pantallaCompleta.Cambio += f => { if (!refrescando) Servicio?.FijarPantallaCompleta(f.Activo); };
            momentoDia.Cambio += f => { if (!refrescando) Servicio?.FijarMomentoDia((MomentoDelDia)f.Indice); };
            for (int i = 0; i < volumenes.Length; i++)
            {
                SeccionSonido seccion = SeccionesVolumen[i];
                volumenes[i].Cambio += f => { if (!refrescando) Servicio?.FijarVolumen(seccion, f.Valor); };
            }
            sensibilidad.Cambio += f => { if (!refrescando) Servicio?.FijarSensibilidad(f.Valor); };
            invertirY.Cambio += f => { if (!refrescando) Servicio?.FijarInvertirY(f.Activo); };
            apuntado.Cambio += f => { if (!refrescando) Servicio?.FijarApuntadoAsistido((NivelApuntadoAsistido)f.Indice); };
            tutorial.Cambio += f => { if (!refrescando) Servicio?.FijarTutorial(f.Activo); };
            // V8–V10: las de VR no pasan por el servicio: se guardan en OpcionesVR (Core) y el AdaptadorVR las lee.
            if (giroVR != null)
            {
                giroVR.Cambio += f => { if (!refrescando) OpcionesVR.FijarGiro((ModoGiroVR)f.Indice); };
            }
            if (velocidadGiroVR != null)
            {
                velocidadGiroVR.Cambio += f => { if (!refrescando) OpcionesVR.FijarVelocidadGiro(f.Valor); };
            }
            if (vinetaVR != null)
            {
                vinetaVR.Cambio += f => { if (!refrescando) OpcionesVR.FijarNivelVineta((NivelVinetaVR)f.Indice); };
            }
            if (rutaVR != null)
            {
                rutaVR.Cambio += f => { if (!refrescando) OpcionesVR.FijarRuta(f.Activo); };
            }
            if (correrVR != null)
            {
                correrVR.Cambio += f => { if (!refrescando) OpcionesVR.FijarCorrer((ModoCorrerVR)f.Indice); };
            }
        }

        // Las filas de CONTROLES en orden; en VR van sin la sensibilidad del ratón ni INVERTIR EJE Y (no aplican) y con GIRO, VELOCIDAD DE GIRO,
        // VIÑETA, CORRER y RUTA.
        private FilaOpcion[] FilasControles()
        {
            if (filasVR && giroVR != null && vinetaVR != null && correrVR != null)
            {
                var lista = new System.Collections.Generic.List<FilaOpcion> { apuntado, tutorial, giroVR };
                if (velocidadGiroVR != null)
                {
                    lista.Add(velocidadGiroVR);
                }
                lista.Add(vinetaVR);
                lista.Add(correrVR);
                if (rutaVR != null)
                {
                    lista.Add(rutaVR);
                }
                return lista.ToArray();
            }
            return new[] { sensibilidad, invertirY, apuntado, tutorial };
        }

        /// <summary>Muestra u oculta las filas de VR según <see cref="ModoVR.Activo"/>, acomoda CONTROLES y vuelve a atar la navegación.</summary>
        public void AplicarFilasVR()
        {
            filasVR = ModoVR.Activo && giroVR != null && vinetaVR != null && correrVR != null;
            if (giroVR != null)
            {
                giroVR.gameObject.SetActive(filasVR);
            }
            if (velocidadGiroVR != null)
            {
                velocidadGiroVR.gameObject.SetActive(filasVR);
            }
            if (vinetaVR != null)
            {
                vinetaVR.gameObject.SetActive(filasVR);
            }
            if (correrVR != null)
            {
                correrVR.gameObject.SetActive(filasVR);
            }
            if (rutaVR != null)
            {
                rutaVR.gameObject.SetActive(filasVR);
            }
            if (sensibilidad != null)
            {
                sensibilidad.gameObject.SetActive(!filasVR);
            }
            if (invertirY != null)
            {
                invertirY.gameObject.SetActive(!filasVR);
            }
            if (controles != null)
            {
                controles.gameObject.SetActive(!filasVR);
            }
            if (rayaControles != null)
            {
                rayaControles.gameObject.SetActive(!filasVR);
            }
            float y = 0f;
            foreach (FilaOpcion fila in FilasControles())
            {
                if (fila != null)
                {
                    UIInicio.Punto(fila.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, y - AltoFila * 0.5f), new Vector2(AnchoFila, AltoFila));
                }
                y -= AltoFila + SeparacionFilas;
            }
            ConectarNavegacion();
            Mostrar(actual);
        }

        private void OnEnable()
        {
            Refrescar();
        }

        private void Update()
        {
            int paso = 0;
            Keyboard teclado = Keyboard.current;
            Gamepad control = Gamepad.current;
            if ((teclado != null && teclado.eKey.wasPressedThisFrame) || (control != null && control.rightShoulder.wasPressedThisFrame))
            {
                paso = 1;
            }
            else if ((teclado != null && teclado.qKey.wasPressedThisFrame) || (control != null && control.leftShoulder.wasPressedThisFrame))
            {
                paso = -1;
            }
            if (paso != 0)
            {
                CambiarPestana(paso);
            }
        }

        /// <summary>Pasa a la pestaña siguiente (1) o anterior (-1), dando la vuelta.</summary>
        public void CambiarPestana(int paso)
        {
            int nueva = ReglaInicio.MoverIndice((int)actual, paso, pestanas.Length, true);
            Mostrar((PestanaOpciones)nueva);
            pestanas[nueva].ElegirSinSonido();
            Sonidos.Reproducir(BotonInicio.SonidoMover);
        }

        private FilaOpcion[][] FilasPorPestana()
        {
            return new[]
            {
                new[] { calidad, resolucion, pantallaCompleta, momentoDia },
                volumenes,
                FilasControles(),
            };
        }

        // La navegación automática de Unity falla entre una pestaña de la orilla y las filas anchas (elige una fila lejana): se ata a mano.
        private void ConectarNavegacion()
        {
            FilaOpcion[][] filas = FilasPorPestana();
            for (int t = 0; t < pestanas.Length; t++)
            {
                Navigation nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnDown = filas[t][0];
                nav.selectOnLeft = t > 0 ? pestanas[t - 1] : null;
                nav.selectOnRight = t < pestanas.Length - 1 ? pestanas[t + 1] : null;
                pestanas[t].navigation = nav;
                for (int r = 0; r < filas[t].Length; r++)
                {
                    Navigation fila = new Navigation { mode = Navigation.Mode.Explicit };
                    fila.selectOnUp = r == 0 ? (Selectable)pestanas[t] : filas[t][r - 1];
                    fila.selectOnDown = r == filas[t].Length - 1 ? (Selectable)volver : filas[t][r + 1];
                    filas[t][r].navigation = fila;
                }
            }
        }

        public void Mostrar(PestanaOpciones cual)
        {
            actual = cual;
            if (volver != null)
            {
                FilaOpcion[] ultimas = FilasPorPestana()[(int)cual];
                Navigation nav = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = ultimas[ultimas.Length - 1] };
                volver.navigation = nav;
            }
            for (int i = 0; i < contenidos.Length; i++)
            {
                if (contenidos[i] != null)
                {
                    contenidos[i].gameObject.SetActive(i == (int)cual);
                }
            }
        }

        /// <summary>Pone cada fila con lo que tiene el servicio, sin avisar al servicio.</summary>
        public void Refrescar()
        {
            if (calidad != null && ModoVR.Activo != filasVR)
            {
                AplicarFilasVR();
            }
            if (calidad != null && giroVR != null && vinetaVR != null && correrVR != null)
            {
                refrescando = true;
                giroVR.FijarIndice((int)OpcionesVR.Giro);
                if (velocidadGiroVR != null)
                {
                    velocidadGiroVR.FijarValor(OpcionesVR.VelocidadGiro);
                }
                vinetaVR.FijarIndice((int)OpcionesVR.NivelVineta);
                if (rutaVR != null)
                {
                    rutaVR.FijarActivo(OpcionesVR.Ruta);
                }
                correrVR.FijarIndice((int)OpcionesVR.Correr);
                refrescando = false;
            }
            ServicioOpciones s = Servicio;
            if (s == null || calidad == null)
            {
                return;
            }
            refrescando = true;
            calidad.FijarIndice((int)s.Calidad);
            string[] textos = new string[s.Resoluciones.Count];
            int elegida = 0;
            for (int i = 0; i < textos.Length; i++)
            {
                textos[i] = ReglaInicio.TextoResolucion(s.Resoluciones[i]);
                if (s.Resoluciones[i] == s.Resolucion)
                {
                    elegida = i;
                }
            }
            if (textos.Length > 0)
            {
                resolucion.FijarOpciones(textos, elegida);
            }
            pantallaCompleta.FijarActivo(s.PantallaCompleta);
            momentoDia.FijarIndice((int)s.MomentoDia);
            for (int i = 0; i < volumenes.Length; i++)
            {
                volumenes[i].FijarValor(s.Volumen(SeccionesVolumen[i]));
            }
            sensibilidad.FijarValor(s.Sensibilidad);
            invertirY.FijarActivo(s.InvertirY);
            apuntado.FijarIndice((int)s.ApuntadoAsistido);
            tutorial.FijarActivo(s.Tutorial);
            refrescando = false;
        }

        private void AlCambiarResolucion(FilaOpcion fila)
        {
            ServicioOpciones s = Servicio;
            if (!refrescando && s != null && fila.Indice >= 0 && fila.Indice < s.Resoluciones.Count)
            {
                s.FijarResolucion(s.Resoluciones[fila.Indice]);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Titulo
{
    /// <summary>
    /// La creación de personaje sin mechas (T-413, D-tony-246), con el kit en un canvas de orden 80: a la izquierda el muñeco elegido
    /// girando (una cámara pequeña lejos del mundo que dibuja en una RawImage) y a la derecha PERSONAJE (A "CHAMARRA" o B "MÁSCARA",
    /// que es <see cref="AparienciaJugador.Cara"/> 0 o 1), TU NOMBRE, TU PANDILLA (se escriben), COLOR (las 4 muestras de
    /// <see cref="ColoresPandilla"/>), EMPEZAR y VOLVER. EMPEZAR llena el perfil (las reglas son las de <see cref="ServicioPerfil"/>) y
    /// llama <see cref="FlujoPartida.IrANuevaPartida(IReadOnlyDictionary{string, string})"/> con él; si hay AUTOGUARDADO pregunta antes
    /// "SE REEMPLAZA EL AUTOGUARDADO" (SÍ / NO). VOLVER o Esc regresan al título.
    /// </summary>
    public class PantallaCreacionPandilla : MonoBehaviour
    {
        public const int OrdenCanvas = 80;
        public const string TextoTitulo = "TU PERSONAJE Y TU PANDILLA";
        public const string TextoPersonaje = "PERSONAJE";
        public const string TextoNombre = "TU NOMBRE";
        public const string TextoPandilla = "TU PANDILLA";
        public const string TextoColor = "COLOR";
        public const string TextoEmpezar = "EMPEZAR";
        public const string TextoVolver = "VOLVER";
        public const string TextoAyuda = "↑ ↓ ELEGIR · ← → CAMBIAR · ESCRIBE EN LOS CAMPOS · ESC VOLVER";
        public const string TextoPregunta = "¿EMPEZAR DE NUEVO?";
        public const string TextoAviso = "SE REEMPLAZA EL AUTOGUARDADO";
        public const string TextoSi = "SÍ";
        public const string TextoNo = "NO";
        public static readonly string[] Personajes = { "CHAMARRA", "MÁSCARA" };
        private const string TextoNombreVacio = "SIN NOMBRE";
        private const int CantidadFilas = 4;
        private const int CantidadBotones = 2;
        private static readonly Vector3 LugarEscenario = new Vector3(0f, -500f, 0f);

        [Header("Personajes")]
        [Tooltip("Un prefab por personaje, en el orden de Apariencia.Cara (0 = A, 1 = B).")]
        [SerializeField] private GameObject[] modelos = new GameObject[0];

        [Header("Vista previa")]
        [Tooltip("Tamaño de la RenderTexture (px).")]
        [SerializeField] private Vector2Int texturaVista = new Vector2Int(512, 768);
        [Tooltip("Giro del muñeco (°/s).")]
        [SerializeField] private float giro = 30f;
        [Tooltip("Distancia de la cámara al muñeco (m).")]
        [Min(0.5f)] [SerializeField] private float distanciaCamara = 3.2f;
        [Tooltip("Altura de la cámara y del punto que mira (m).")]
        [Min(0f)] [SerializeField] private float alturaCamara = 1f;
        [Tooltip("Apertura vertical de la cámara (°).")]
        [Range(10f, 90f)] [SerializeField] private float aperturaCamara = 40f;
        [Tooltip("Fondo de la vista previa.")]
        [SerializeField] private Color fondoVista = new Color(0.04f, 0.04f, 0.07f, 1f);
        [Tooltip("Intensidad de la luz de la vista previa.")]
        [Min(0f)] [SerializeField] private float intensidadLuz = 6f;
        [Tooltip("Alcance de la luz de la vista previa (m); no llega a la ciudad.")]
        [Min(1f)] [SerializeField] private float alcanceLuz = 8f;

        [Header("Medidas (px a 1920x1080)")]
        [SerializeField] private Vector2 tamanoVista = new Vector2(480f, 620f);
        [Tooltip("Separación entre la vista previa y las filas.")]
        [Min(0f)] [SerializeField] private float separacionColumnas = 48f;
        [Tooltip("Lado de cada muestra de color.")]
        [Min(8f)] [SerializeField] private float ladoMuestra = 34f;
        [Tooltip("Dónde empiezan las muestras, desde la izquierda de la fila.")]
        [Min(0f)] [SerializeField] private float inicioMuestras = 170f;
        [SerializeField] private Vector2 tamanoConfirmar = new Vector2(760f, 300f);
        [Range(0f, 1f)] [SerializeField] private float alfaPanel = 0.94f;

        [Header("Piezas (las arma el creador con el kit)")]
        [SerializeField] private GameObject contenido;
        [SerializeField] private MenuCyberfix menu;
        [SerializeField] private RawImage vista;
        [SerializeField] private Text nombrePersonaje;
        [SerializeField] private SelectorCyberfix personaje;
        [SerializeField] private CampoTextoCyberfix nombre;
        [SerializeField] private CampoTextoCyberfix pandilla;
        [SerializeField] private SelectorCyberfix color;
        [SerializeField] private Image[] muestras = new Image[0];
        [SerializeField] private GameObject[] marcasMuestra = new GameObject[0];
        [SerializeField] private BotonCyberfix empezar;
        [SerializeField] private BotonCyberfix volver;
        [SerializeField] private GameObject confirmar;
        [SerializeField] private MenuCyberfix menuConfirmar;
        [SerializeField] private BotonCyberfix si;
        [SerializeField] private BotonCyberfix no;

        private GameObject escenario;
        private Transform pivote;
        private Camera camara;
        private RenderTexture textura;
        private readonly List<GameObject> munecos = new List<GameObject>();

        public bool Abierta => contenido != null && contenido.activeSelf;
        public bool ConfirmacionAbierta => confirmar != null && confirmar.activeSelf;
        public MenuCyberfix Menu => menu;
        public MenuCyberfix MenuConfirmar => menuConfirmar;
        public SelectorCyberfix SelectorPersonaje => personaje;
        public CampoTextoCyberfix Nombre => nombre;
        public CampoTextoCyberfix Pandilla => pandilla;
        public SelectorCyberfix SelectorColor => color;
        public BotonCyberfix Empezar => empezar;
        public BotonCyberfix Volver => volver;
        public BotonCyberfix Si => si;
        public BotonCyberfix No => no;
        public RawImage Vista => vista;
        public Canvas Canvas => contenido != null ? contenido.GetComponent<Canvas>() : null;

        /// <summary>El muñeco que se ve en la vista previa (el del personaje elegido), o null.</summary>
        public GameObject MunecoVisible => personaje != null && personaje.Indice < munecos.Count ? munecos[personaje.Indice] : null;

        /// <summary>VOLVER, Esc, o EMPEZAR antes de cambiar de escena.</summary>
        public event Action Cerrada;

        public void Armar()
        {
            EstiloCyberfix e = KitUI.Estilo;
            float relleno = e.Relleno;
            Vector2 fila = e.TamanoFila;
            Vector2 boton = e.TamanoBotonMenu;
            float separacion = e.SeparacionBotones;
            float altoTitulo = e.Tamano(RolTexto.Encabezado) + relleno;
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasCreacion", OrdenCanvas);
            contenido = canvas.gameObject;
            KitUI.CrearFondoPantalla(canvas.transform, "FondoPantalla");

            Text titulo = KitUI.CrearTexto(canvas.transform, "Titulo", RolTexto.Encabezado, TextAnchor.MiddleCenter);
            KitUI.Colocar(titulo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -e.MargenBorde), new Vector2(KitUI.AnchoReferencia, altoTitulo));
            titulo.text = TextoTitulo;
            titulo.color = e.Naranja;

            // Columnas: vista previa a la izquierda y filas a la derecha, juntas al centro.
            float altoFilas = CantidadFilas * fila.y + (CantidadFilas - 1) * separacion;
            float altoBotones = CantidadBotones * boton.y + (CantidadBotones - 1) * separacion;
            Vector2 tamanoPanel = new Vector2(fila.x + 2f * relleno,
                relleno + altoFilas + e.SeparacionBloques + altoBotones + e.SeparacionBloques + e.AltoFilaHud + relleno);
            float anchoTotal = tamanoVista.x + separacionColumnas + tamanoPanel.x;
            float xVista = -anchoTotal / 2f + tamanoVista.x / 2f;
            float xPanel = anchoTotal / 2f - tamanoPanel.x / 2f;
            // Las dos columnas alineadas por arriba.
            float altoMayor = Mathf.Max(tamanoVista.y, tamanoPanel.y);

            RectTransform marco = KitUI.CrearPanel(canvas.transform, "Vista", tamanoVista);
            marco.anchoredPosition = new Vector2(xVista, (altoMayor - tamanoVista.y) / 2f);
            Opacar(marco);
            GameObject objetoVista = new GameObject("Muneco", typeof(RectTransform));
            objetoVista.transform.SetParent(marco, false);
            vista = objetoVista.AddComponent<RawImage>();
            vista.raycastTarget = false;
            RectTransform rectVista = vista.rectTransform;
            KitUI.Estirar(rectVista);
            rectVista.offsetMin = new Vector2(relleno, relleno + e.AltoFilaHud + relleno);
            rectVista.offsetMax = new Vector2(-relleno, -relleno);
            nombrePersonaje = KitUI.CrearTexto(marco, "NombrePersonaje", RolTexto.Boton, TextAnchor.MiddleCenter);
            KitUI.Colocar(nombrePersonaje.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, relleno), new Vector2(tamanoVista.x, e.AltoFilaHud));
            nombrePersonaje.color = e.Naranja;

            RectTransform panel = KitUI.CrearPanel(canvas.transform, "Panel", tamanoPanel);
            panel.anchoredPosition = new Vector2(xPanel, (altoMayor - tamanoPanel.y) / 2f);
            Opacar(panel);
            menu = panel.gameObject.AddComponent<MenuCyberfix>();
            float y = -relleno;
            personaje = KitUI.CrearSelector(panel, TextoPersonaje, Personajes, 0, null);
            Fila(personaje, ref y);
            nombre = KitUI.CrearCampo(panel, TextoNombre, ReglaAlias.LargoMaximo, TextoNombreVacio);
            Fila(nombre, ref y);
            pandilla = KitUI.CrearCampo(panel, TextoPandilla, ColoresPandilla.LargoMaximo, ColoresPandilla.NombrePredeterminado);
            Fila(pandilla, ref y);
            string[] nombresColor = new string[ColoresPandilla.Cantidad];
            for (int i = 0; i < nombresColor.Length; i++)
            {
                nombresColor[i] = ColoresPandilla.Nombre(i);
            }
            color = KitUI.CrearSelector(panel, TextoColor, nombresColor, ColoresPandilla.Predeterminado, null);
            Fila(color, ref y);
            muestras = new Image[ColoresPandilla.Cantidad];
            marcasMuestra = new GameObject[ColoresPandilla.Cantidad];
            for (int i = 0; i < muestras.Length; i++)
            {
                Image muestra = KitUI.CrearImagen(color.transform, "Muestra_" + nombresColor[i], ColoresPandilla.Color(i));
                KitUI.Colocar(muestra.rectTransform, new Vector2(0f, 0.5f),
                    new Vector2(inicioMuestras + i * (ladoMuestra + separacion), 0f), Vector2.one * ladoMuestra);
                marcasMuestra[i] = KitUI.CrearBorde(muestra.rectTransform, e.BordeElegido, e.Texto);
                muestras[i] = muestra;
            }

            y -= e.SeparacionBloques - separacion;
            empezar = Boton(panel, TextoEmpezar, ref y);
            volver = Boton(panel, TextoVolver, ref y);
            Text ayuda = KitUI.CrearTexto(panel, "Ayuda", RolTexto.Ayuda, TextAnchor.MiddleCenter);
            KitUI.Colocar(ayuda.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, relleno), new Vector2(tamanoPanel.x, e.AltoFilaHud));
            ayuda.text = TextoAyuda;
            ayuda.color = e.Turquesa;

            // ¿EMPEZAR DE NUEVO? SE REEMPLAZA EL AUTOGUARDADO · SÍ / NO, en magenta.
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

        /// <summary>Para el creador del prefab.</summary>
        public void ConfigurarModelos(GameObject[] nuevos) => modelos = nuevos ?? new GameObject[0];

        private static void Fila(BotonCyberfix control, ref float y)
        {
            EstiloCyberfix e = KitUI.Estilo;
            KitUI.Colocar((RectTransform)control.transform, new Vector2(0.5f, 1f), new Vector2(0f, y), e.TamanoFila);
            y -= e.TamanoFila.y + e.SeparacionBotones;
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
                UnityEngine.Color c = fondo.color;
                c.a = Mathf.Max(c.a, alfaPanel);
                fondo.color = c;
            }
        }

        private void Awake()
        {
            if (contenido == null || menu == null || personaje == null || nombre == null || pandilla == null || color == null || empezar == null)
            {
                Armar();
            }
            personaje.CambioValor += AlCambiarPersonaje;
            color.CambioValor += AlCambiarColor;
            empezar.AlPulsar.AddListener(AlEmpezar);
            volver.AlPulsar.AddListener(Cerrar);
            si.AlPulsar.AddListener(EmpezarPartida);
            no.AlPulsar.AddListener(CancelarConfirmacion);
            menu.Regresar += Cerrar;
            menuConfirmar.Regresar += CancelarConfirmacion;
            confirmar.SetActive(false);
            contenido.SetActive(false);
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
            if (camara != null)
            {
                camara.targetTexture = null;
            }
            if (textura != null)
            {
                textura.Release();
                Destroy(textura);
            }
            if (escenario != null)
            {
                Destroy(escenario);
            }
        }

        private void Update()
        {
            if (Abierta && pivote != null)
            {
                pivote.Rotate(0f, giro * Time.unscaledDeltaTime, 0f, Space.World);
            }
        }

        public void Abrir()
        {
            if (Abierta)
            {
                return;
            }
            PrepararEscenario();
            PilaPantallas.Abrir(this);
            confirmar.SetActive(false);
            contenido.SetActive(true);
            menu.enabled = true;
            menu.Recargar();
            AlCambiarPersonaje(personaje.Indice);
            AlCambiarColor(color.Indice);
            if (escenario != null)
            {
                escenario.SetActive(true);
            }
            Debug.Log("[UI] Creación abierta.");
        }

        public void Cerrar()
        {
            if (!Abierta || ConfirmacionAbierta)
            {
                return;
            }
            contenido.SetActive(false);
            if (escenario != null)
            {
                escenario.SetActive(false);
            }
            PilaPantallas.Cerrar(this);
            Cerrada?.Invoke();
        }

        // Escenario lejos del mundo: luz, cámara a una RenderTexture y un muñeco por personaje (solo se prende el elegido).
        private void PrepararEscenario()
        {
            if (escenario != null)
            {
                return;
            }
            escenario = new GameObject("EscenarioCreacion");
            escenario.transform.position = LugarEscenario;
            pivote = new GameObject("Pivote").transform;
            pivote.SetParent(escenario.transform, false);
            pivote.localRotation = Quaternion.Euler(0f, 180f, 0f);
            foreach (GameObject modelo in modelos)
            {
                GameObject muneco = modelo != null ? Instantiate(modelo, pivote) : new GameObject("SinModelo");
                muneco.transform.SetParent(pivote, false);
                muneco.transform.localPosition = Vector3.zero;
                muneco.transform.localRotation = Quaternion.identity;
                muneco.SetActive(false);
                munecos.Add(muneco);
            }
            GameObject objetoCamara = new GameObject("CamaraCreacion");
            objetoCamara.transform.SetParent(escenario.transform, false);
            objetoCamara.transform.localPosition = new Vector3(0f, alturaCamara, -distanciaCamara);
            objetoCamara.transform.localRotation = Quaternion.identity;
            camara = objetoCamara.AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = fondoVista;
            camara.fieldOfView = aperturaCamara;
            camara.nearClipPlane = 0.1f;
            camara.farClipPlane = distanciaCamara * 3f;
            textura = new RenderTexture(texturaVista.x, texturaVista.y, 24, RenderTextureFormat.ARGB32) { name = "VistaCreacion" };
            textura.Create();
            camara.targetTexture = textura;
            vista.texture = textura;
            Light luz = new GameObject("LuzCreacion").AddComponent<Light>();
            luz.transform.SetParent(escenario.transform, false);
            luz.transform.localPosition = new Vector3(1.5f, 2.5f, -2.5f);
            luz.transform.LookAt(escenario.transform.position + Vector3.up * alturaCamara);
            luz.type = LightType.Point;
            luz.range = alcanceLuz;
            luz.intensity = intensidadLuz;
        }

        private void AlCambiarPersonaje(int indice)
        {
            for (int i = 0; i < munecos.Count; i++)
            {
                munecos[i].SetActive(i == indice);
            }
            nombrePersonaje.text = indice >= 0 && indice < Personajes.Length ? Personajes[indice] : string.Empty;
        }

        private void AlCambiarColor(int indice)
        {
            for (int i = 0; i < marcasMuestra.Length; i++)
            {
                if (marcasMuestra[i] != null)
                {
                    marcasMuestra[i].SetActive(i == indice);
                }
            }
        }

        private void AlEmpezar()
        {
            ServicioGuardado servicio = ServicioGuardado.Actual;
            if (servicio != null && !servicio.ResumenDe(ServicioGuardado.RanuraAuto).Vacia)
            {
                confirmar.SetActive(true);
                menu.enabled = false;
                no.Elegir();
                return;
            }
            EmpezarPartida();
        }

        private void CancelarConfirmacion()
        {
            if (!ConfirmacionAbierta)
            {
                return;
            }
            menu.enabled = true;
            confirmar.SetActive(false);
            empezar.Elegir();
        }

        /// <summary>Llena el perfil con lo elegido y empieza la historia en una escena nueva con ese perfil.</summary>
        private void EmpezarPartida()
        {
            ServicioPerfil perfil = Registro<IPerfilJugador>.Actual as ServicioPerfil;
            if (perfil == null)
            {
                Debug.LogWarning("[UI] Creación sin ServicioPerfil: la partida empieza con el perfil por defecto.", this);
                FlujoPartida.IrANuevaPartida();
                return;
            }
            perfil.Fijar(nombre.Valor, null);
            perfil.FijarApariencia(new AparienciaJugador(personaje.Indice, 0, 0));
            perfil.FijarPandilla(pandilla.Valor, color.Indice);
            Debug.Log("[UI] EMPEZAR: " + perfil.Nombre + ", " + Personajes[personaje.Indice] + ", " + perfil.NombrePandilla + " ("
                + ColoresPandilla.Nombre(perfil.ColorPandilla) + ").");
            Dictionary<string, string> piezas = new Dictionary<string, string> { { perfil.ClaveGuardado, perfil.Guardar() } };
            menuConfirmar.enabled = false;
            menu.enabled = false;
            FlujoPartida.IrANuevaPartida(piezas);
        }
    }
}

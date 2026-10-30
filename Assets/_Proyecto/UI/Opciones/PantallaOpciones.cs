using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion.Opciones;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Opciones
{
    /// <summary>
    /// La pantalla de OPCIONES (T-411, D-tony-243), hecha con el kit en un canvas de orden 70: SENSIBILIDAD ("1.00x"), INVERTIR Y,
    /// VOLUMEN GENERAL, EFECTOS, VOCES, AMBIENTE, MÚSICA, INTERFAZ (0 a 100 % de 5 en 5), CALIDAD, RESOLUCIÓN, PANTALLA COMPLETA y VOLVER.
    /// ← → (o la rueda, o el clic en los selectores y el conmutador) cambian el valor de la fila elegida y el <see cref="ServicioOpciones"/>
    /// lo aplica y lo guarda al instante. Esc o VOLVER la cierran y avisan <see cref="Cerrada"/> (la pausa vuelve a su menú). Se apila en
    /// <see cref="PilaPantallas"/>.
    /// </summary>
    public class PantallaOpciones : MonoBehaviour
    {
        public const int OrdenCanvas = 70;
        public const string TextoTitulo = "OPCIONES";
        public const string TextoSensibilidad = "SENSIBILIDAD";
        public const string TextoInvertirY = "INVERTIR Y";
        public const string TextoCalidad = "CALIDAD";
        public const string TextoResolucion = "RESOLUCIÓN";
        public const string TextoPantallaCompleta = "PANTALLA COMPLETA";
        public const string TextoVolver = "VOLVER";
        public const string TextoAyuda = "↑ ↓ ELEGIR · ← → CAMBIAR · ESC VOLVER";
        public const string FormatoSensibilidad = "{0:0.00}x";
        public static readonly string[] TextosCalidad = { "BAJA", "MEDIA", "ALTA" };
        // En el orden de SeccionSonido.
        public static readonly string[] TextosVolumen = { "VOLUMEN GENERAL", "EFECTOS", "VOCES", "AMBIENTE", "MÚSICA", "INTERFAZ" };

        [Header("Medidas (px a 1920x1080)")]
        [Tooltip("Separación entre filas (px); menos que la de los botones para que las 12 quepan en 1080.")]
        [Min(0f)] [SerializeField] private float separacionFilas = 8f;
        [Tooltip("Opacidad del fondo del panel (0 a 1): opaco, porque va encima de la pausa y sus letras se leían a través.")]
        [Range(0f, 1f)] [SerializeField] private float alfaPanel = 1f;

        [Header("Piezas (las arma el creador con el kit)")]
        [SerializeField] private GameObject contenido;
        [SerializeField] private MenuCyberfix menu;
        [SerializeField] private DeslizadorCyberfix sensibilidad;
        [SerializeField] private ConmutadorCyberfix invertirY;
        [SerializeField] private DeslizadorCyberfix[] volumenes;
        [SerializeField] private SelectorCyberfix calidad;
        [SerializeField] private SelectorCyberfix resolucion;
        [SerializeField] private ConmutadorCyberfix pantallaCompleta;
        [SerializeField] private BotonCyberfix volver;

        private ServicioOpciones servicio;
        private ControladorAPie controlCongelado;

        public bool Abierta => contenido != null && contenido.activeSelf;
        public MenuCyberfix Menu => menu;
        public DeslizadorCyberfix Sensibilidad => sensibilidad;
        public ConmutadorCyberfix InvertirY => invertirY;
        public DeslizadorCyberfix Volumen(SeccionSonido seccion) => volumenes[(int)seccion];
        public SelectorCyberfix Calidad => calidad;
        public SelectorCyberfix Resolucion => resolucion;
        public ConmutadorCyberfix PantallaCompleta => pantallaCompleta;
        public BotonCyberfix Volver => volver;

        /// <summary>Se dispara al cerrarse (Esc o VOLVER).</summary>
        public event Action Cerrada;

        /// <summary>Arma todas las piezas con el kit (lo llama el creador del prefab, o Awake si faltan).</summary>
        public void Armar()
        {
            EstiloCyberfix e = KitUI.Estilo;
            float relleno = e.Relleno;
            Vector2 fila = e.TamanoFila;
            float altoTitulo = e.Tamano(RolTexto.Encabezado) + relleno;
            float altoAyuda = e.AltoFilaHud;
            int cantidadFilas = 5 + TextosVolumen.Length;
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasOpciones", OrdenCanvas);
            contenido = canvas.gameObject;
            KitUI.CrearFondoPantalla(canvas.transform, "FondoPantalla");

            float altoFilas = cantidadFilas * fila.y + (cantidadFilas - 1) * separacionFilas;
            Vector2 tamanoPanel = new Vector2(fila.x + 4f * relleno,
                relleno + altoTitulo + relleno + altoFilas + e.SeparacionBloques + e.TamanoBotonMenu.y + relleno + altoAyuda + relleno);
            RectTransform panel = KitUI.CrearPanel(canvas.transform, "Panel", tamanoPanel);
            menu = panel.gameObject.AddComponent<MenuCyberfix>();
            Image fondo = panel.GetComponent<Image>();
            if (fondo != null)
            {
                Color color = fondo.color;
                color.a = Mathf.Max(color.a, alfaPanel);
                fondo.color = color;
            }

            Text titulo = KitUI.CrearTexto(panel, "Titulo", RolTexto.Encabezado, TextAnchor.MiddleCenter);
            KitUI.Colocar(titulo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -relleno), new Vector2(tamanoPanel.x, altoTitulo));
            titulo.text = TextoTitulo;
            titulo.color = e.Naranja;

            float y = -(relleno + altoTitulo + relleno);
            sensibilidad = KitUI.CrearDeslizador(panel, TextoSensibilidad, ReglaOpciones.SensibilidadMinima, ReglaOpciones.SensibilidadMaxima,
                ReglaOpciones.PasoSensibilidad, 1f, null);
            sensibilidad.FijarFormato(FormatoSensibilidad);
            Fila(sensibilidad, ref y);
            invertirY = KitUI.CrearConmutador(panel, TextoInvertirY, false, null);
            Fila(invertirY, ref y);
            volumenes = new DeslizadorCyberfix[TextosVolumen.Length];
            for (int i = 0; i < TextosVolumen.Length; i++)
            {
                volumenes[i] = KitUI.CrearDeslizador(panel, TextosVolumen[i], 0f, 1f, ReglaOpciones.PasoVolumen, ReglaOpciones.VolumenPorDefecto, null);
                Fila(volumenes[i], ref y);
            }
            calidad = KitUI.CrearSelector(panel, TextoCalidad, TextosCalidad, (int)CalidadGrafica.Alta, null);
            Fila(calidad, ref y);
            resolucion = KitUI.CrearSelector(panel, TextoResolucion, new[] { "1920 x 1080" }, 0, null);
            Fila(resolucion, ref y);
            pantallaCompleta = KitUI.CrearConmutador(panel, TextoPantallaCompleta, true, null);
            Fila(pantallaCompleta, ref y);

            y += separacionFilas - e.SeparacionBloques;
            volver = KitUI.CrearBoton(panel, TextoVolver, null);
            volver.Texto.alignment = TextAnchor.MiddleCenter;
            KitUI.Colocar((RectTransform)volver.transform, new Vector2(0.5f, 1f), new Vector2(0f, y), e.TamanoBotonMenu);

            Text ayuda = KitUI.CrearTexto(panel, "Ayuda", RolTexto.Ayuda, TextAnchor.MiddleCenter);
            KitUI.Colocar(ayuda.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, relleno), new Vector2(tamanoPanel.x, altoAyuda));
            ayuda.text = TextoAyuda;
            ayuda.color = e.Turquesa;
            contenido.SetActive(false);
        }

        private void Fila(BotonCyberfix control, ref float y)
        {
            Vector2 tamano = KitUI.Estilo.TamanoFila;
            KitUI.Colocar((RectTransform)control.transform, new Vector2(0.5f, 1f), new Vector2(0f, y), tamano);
            y -= tamano.y + separacionFilas;
        }

        private void Awake()
        {
            if (contenido == null || menu == null || sensibilidad == null || volumenes == null || volumenes.Length != TextosVolumen.Length)
            {
                Armar();
            }
            sensibilidad.FijarFormato(FormatoSensibilidad);
            sensibilidad.CambioValor += v => Servicio?.FijarSensibilidad(v);
            invertirY.CambioValor += v => Servicio?.FijarInvertirY(v);
            for (int i = 0; i < volumenes.Length; i++)
            {
                SeccionSonido seccion = (SeccionSonido)i;
                volumenes[i].CambioValor += v => Servicio?.FijarVolumen(seccion, v);
            }
            calidad.CambioValor += i => Servicio?.FijarCalidad((CalidadGrafica)i);
            resolucion.CambioValor += AlCambiarResolucion;
            pantallaCompleta.CambioValor += v => Servicio?.FijarPantallaCompleta(v);
            volver.AlPulsar.AddListener(Cerrar);
            menu.Regresar += Cerrar;
            contenido.SetActive(false);
        }

        private void OnDisable() => Cerrar();

        private void OnDestroy()
        {
            if (menu != null)
            {
                menu.Regresar -= Cerrar;
            }
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

        public void Abrir()
        {
            if (Abierta)
            {
                return;
            }
            Refrescar();
            PilaPantallas.Abrir(this);
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
            contenido.SetActive(false);
            if (controlCongelado != null)
            {
                controlCongelado.ActivarControl(true);
                controlCongelado = null;
            }
            PilaPantallas.Cerrar(this);
            Cerrada?.Invoke();
        }

        // Pone cada fila con lo que tiene el servicio, sin avisar a nadie.
        private void Refrescar()
        {
            ServicioOpciones s = Servicio;
            if (s == null)
            {
                Debug.LogWarning("[UI] PantallaOpciones sin ServicioOpciones en la escena.", this);
                return;
            }
            sensibilidad.FijarValor(s.Sensibilidad);
            invertirY.Configurar(s.InvertirY);
            for (int i = 0; i < volumenes.Length; i++)
            {
                volumenes[i].FijarValor(s.Volumen((SeccionSonido)i));
            }
            calidad.Configurar(TextosCalidad, (int)s.Calidad);
            string[] textos = new string[s.Resoluciones.Count];
            int elegida = 0;
            for (int i = 0; i < textos.Length; i++)
            {
                Vector2Int r = s.Resoluciones[i];
                textos[i] = r.x + " x " + r.y;
                if (r == s.Resolucion)
                {
                    elegida = i;
                }
            }
            resolucion.Configurar(textos, elegida);
            pantallaCompleta.Configurar(s.PantallaCompleta);
        }

        private void AlCambiarResolucion(int indice)
        {
            ServicioOpciones s = Servicio;
            if (s != null && indice >= 0 && indice < s.Resoluciones.Count)
            {
                s.FijarResolucion(s.Resoluciones[indice]);
            }
        }
    }
}

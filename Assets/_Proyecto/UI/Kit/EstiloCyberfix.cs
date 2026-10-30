using UnityEngine;

namespace MechaCyberpunk.UI.Kit
{
    /// <summary>
    /// Estilo de toda la UI de CYBERFIX (D-tony-238, D-tony-239): fuente, los 6 colores, los 7 tamaños, márgenes y medidas de botón de
    /// docs/diseno/ui-cyberfix.md. El asset es UI/Datos/EstiloCyberfix_Base.asset; <see cref="KitUI"/> lo usa para todo lo que crea.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Estilo CYBERFIX", fileName = "EstiloCyberfix_Nuevo")]
    public class EstiloCyberfix : ScriptableObject
    {
        public const string NombreFuenteIntegrada = "LegacyRuntime.ttf";

        [Tooltip("Fuente de todo el juego. Vacía = la integrada de Unity (LegacyRuntime.ttf).")]
        [SerializeField] private Font fuente;

        [Header("Colores")]
        [Tooltip("Tú, el foco, el objetivo y los títulos (#FF801A).")]
        [SerializeField] private Color naranja = new Color(1f, 0.502f, 0.102f, 1f);
        [Tooltip("Información: valores, distancias, teclas (#1AE6E6).")]
        [SerializeField] private Color turquesa = new Color(0.102f, 0.902f, 0.902f, 1f);
        [Tooltip("Alertas y peligro; también SKULLS (#FF1ACC).")]
        [SerializeField] private Color magenta = new Color(1f, 0.102f, 0.8f, 1f);
        [Tooltip("Fondo de panel (#0A0A12 al 85 %).")]
        [SerializeField] private Color fondo = new Color(0.039f, 0.039f, 0.071f, 0.851f);
        [Tooltip("Texto normal (#F2F2F2).")]
        [SerializeField] private Color texto = new Color(0.949f, 0.949f, 0.949f, 1f);
        [Tooltip("Botón o fila que no se puede usar (#7A7A85).")]
        [SerializeField] private Color desactivado = new Color(0.478f, 0.478f, 0.522f, 1f);
        [Tooltip("Fondo a pantalla completa detrás de pausa y mapa (#000000A6).")]
        [SerializeField] private Color fondoPantalla = new Color(0f, 0f, 0f, 0.651f);
        [Tooltip("Contorno oscuro del rombo y la flecha del objetivo, para que se lean sobre cualquier color.")]
        [SerializeField] private Color contorno = new Color(0.039f, 0.039f, 0.071f, 0.9f);
        [Tooltip("Grosor del contorno del rombo y la flecha (px).")]
        [Min(0f)] [SerializeField] private float grosorContorno = 4f;
        [Tooltip("Sombra de todo texto (negro al 85 %).")]
        [SerializeField] private Color sombra = new Color(0f, 0f, 0f, 0.85f);
        [Tooltip("Desplazamiento de la sombra (px de referencia).")]
        [SerializeField] private Vector2 distanciaSombra = new Vector2(1.5f, -1.5f);

        [Header("Tamaños (px a 1920x1080)")]
        [Min(20)] [SerializeField] private int tamanoTitulo = 96;
        [Min(20)] [SerializeField] private int tamanoEncabezado = 48;
        [Min(20)] [SerializeField] private int tamanoBoton = 34;
        [Min(20)] [SerializeField] private int tamanoTexto = 28;
        [Min(20)] [SerializeField] private int tamanoHud = 26;
        [Min(20)] [SerializeField] private int tamanoAyuda = 22;
        [Min(20)] [SerializeField] private int tamanoMinimo = 20;

        [Header("Márgenes (px de referencia)")]
        [Tooltip("Margen al borde de la pantalla (px).")]
        [Min(0f)] [SerializeField] private float margenBorde = 48f;
        [Tooltip("Separación entre bloques (px).")]
        [Min(0f)] [SerializeField] private float separacionBloques = 24f;
        [Tooltip("Relleno dentro de un panel (px).")]
        [Min(0f)] [SerializeField] private float relleno = 16f;
        [Tooltip("Grosor del borde de panel (px).")]
        [Min(0f)] [SerializeField] private float bordePanel = 2f;
        [Tooltip("Opacidad del borde turquesa del panel (0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float alfaBordePanel = 0.6f;
        [Tooltip("Alto de una fila de texto del HUD (px).")]
        [Min(20f)] [SerializeField] private float altoFilaHud = 36f;

        [Header("Botones y filas (px de referencia)")]
        [SerializeField] private Vector2 tamanoBotonMenu = new Vector2(440f, 64f);
        [Tooltip("Separación entre botones (px).")]
        [Min(0f)] [SerializeField] private float separacionBotones = 12f;
        [SerializeField] private Vector2 tamanoFila = new Vector2(760f, 56f);
        [Tooltip("Grosor del borde naranja del elegido (px).")]
        [Min(0f)] [SerializeField] private float bordeElegido = 3f;
        [Tooltip("Opacidad del fondo naranja del elegido (0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float alfaElegido = 0.25f;
        [Tooltip("Opacidad del fondo naranja al pulsar (0 a 1).")]
        [Range(0f, 1f)] [SerializeField] private float alfaPulsado = 0.6f;

        public Font Fuente
        {
            get
            {
                if (fuente == null)
                {
                    fuente = Resources.GetBuiltinResource<Font>(NombreFuenteIntegrada);
                }
                return fuente;
            }
        }

        public Color Naranja => naranja;
        public Color Turquesa => turquesa;
        public Color Magenta => magenta;
        public Color Fondo => fondo;
        public Color Texto => texto;
        public Color Desactivado => desactivado;
        public Color Sombra => sombra;
        public Color FondoPantalla => fondoPantalla;
        public Color Contorno => contorno;
        public float GrosorContorno => grosorContorno;
        public Vector2 DistanciaSombra => distanciaSombra;
        public float MargenBorde => margenBorde;
        public float SeparacionBloques => separacionBloques;
        public float Relleno => relleno;
        public float BordePanel => bordePanel;
        public Color BordeDePanel => new Color(turquesa.r, turquesa.g, turquesa.b, alfaBordePanel);
        public float AltoFilaHud => altoFilaHud;
        public Vector2 TamanoBotonMenu => tamanoBotonMenu;
        public float SeparacionBotones => separacionBotones;
        public Vector2 TamanoFila => tamanoFila;
        public float BordeElegido => bordeElegido;
        public Color FondoElegido => new Color(naranja.r, naranja.g, naranja.b, alfaElegido);
        public Color FondoPulsado => new Color(naranja.r, naranja.g, naranja.b, alfaPulsado);

        /// <summary>Tamaño de letra (px a 1920x1080) de un rol; nunca menos que el mínimo.</summary>
        public int Tamano(RolTexto rol)
        {
            int t;
            switch (rol)
            {
                case RolTexto.Titulo: t = tamanoTitulo; break;
                case RolTexto.Encabezado: t = tamanoEncabezado; break;
                case RolTexto.Boton: t = tamanoBoton; break;
                case RolTexto.Texto: t = tamanoTexto; break;
                case RolTexto.Hud: t = tamanoHud; break;
                case RolTexto.Ayuda: t = tamanoAyuda; break;
                default: t = tamanoMinimo; break;
            }
            return Mathf.Max(tamanoMinimo, t);
        }
    }
}

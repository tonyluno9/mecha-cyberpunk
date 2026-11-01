using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.UI.Kit
{
    /// <summary>
    /// Las piezas gráficas de la interfaz (T-521): marcos de 9-slice, botones, barras, iconos y reticulas de Assets/Arte/UI/Sprites. El asset es
    /// UI/Resources/PiezasUI.asset (lo arma CreadorPiezasUI), así que cualquier script lo carga con <see cref="Actual"/> sin referencias de escena.
    /// Los menús (T-522) usan las mismas piezas con <see cref="KitUI.CrearPanelMarco"/> y <see cref="KitUI.CrearBotonMarco"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Piezas UI", fileName = "PiezasUI")]
    public class PiezasUI : ScriptableObject
    {
        public const string NombreRecurso = "PiezasUI";
        private static PiezasUI actual;

        [Header("Marcos (9-slice)")]
        [SerializeField] private Sprite marcoPanel;
        [SerializeField] private Sprite botonNormal;
        [SerializeField] private Sprite botonResaltado;
        [SerializeField] private Sprite botonPulsado;

        [Header("HUD")]
        [SerializeField] private Sprite barraVida;
        [SerializeField] private Sprite barraCalor3;
        [SerializeField] private Sprite calorRelleno;
        [SerializeField] private Sprite simboloBiits;
        [SerializeField] private Sprite marcoMinimapa;
        [SerializeField] private Sprite panelArma;

        [Header("Iconos")]
        [Tooltip("En el orden de ArmaAPie: puños, pistola, escopeta, fusil, sniper.")]
        [SerializeField] private Sprite[] armas = new Sprite[5];
        [SerializeField] private Sprite insigniaSkulls;
        [SerializeField] private Sprite insigniaSnakes;
        [SerializeField] private Sprite insigniaJugador;
        [Tooltip("Respeto, calor, aliados, implantes, objetivo, teléfono.")]
        [SerializeField] private Sprite[] estados = new Sprite[6];
        [Tooltip("Casa, mercado negro, clínica, taller, bar, casino, misión.")]
        [SerializeField] private Sprite[] iconosMapa = new Sprite[7];
        [Tooltip("Mira, mira de sniper, marca de impacto.")]
        [SerializeField] private Sprite[] reticulas = new Sprite[3];

        [Header("Menú (T-522)")]
        [SerializeField] private Sprite logo;
        [SerializeField] private Sprite fondoMenu;

        public static PiezasUI Actual
        {
            get
            {
                if (actual == null)
                {
                    actual = Resources.Load<PiezasUI>(NombreRecurso);
                }
                return actual;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => actual = null;

        public Sprite MarcoPanel => marcoPanel;
        public Sprite BotonNormal => botonNormal;
        public Sprite BotonResaltado => botonResaltado;
        public Sprite BotonPulsado => botonPulsado;
        public Sprite BarraVida => barraVida;
        public Sprite BarraCalor3 => barraCalor3;
        public Sprite CalorRelleno => calorRelleno;
        public Sprite SimboloBiits => simboloBiits;
        public Sprite MarcoMinimapa => marcoMinimapa;
        public Sprite PanelArma => panelArma;
        public Sprite Logo => logo;
        public Sprite FondoMenu => fondoMenu;
        public Sprite[] Armas => armas;
        public Sprite[] Estados => estados;
        public Sprite[] IconosMapa => iconosMapa;
        public Sprite[] Reticulas => reticulas;

        /// <summary>Icono del arma en el orden de ArmaAPie (0 a 4), o null.</summary>
        public Sprite Arma(int indice) => indice >= 0 && indice < armas.Length ? armas[indice] : null;

        /// <summary>Insignia de la pandilla; null para Ninguna y NOVA (no tienen).</summary>
        public Sprite Insignia(IdPandilla pandilla)
        {
            switch (pandilla)
            {
                case IdPandilla.Calaveras: return insigniaSkulls;
                case IdPandilla.Viboras: return insigniaSnakes;
                case IdPandilla.Jugador: return insigniaJugador;
                default: return null;
            }
        }
    }
}

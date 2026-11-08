using UnityEngine;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// Fuente (Rajdhani, OFL) e íconos de línea fina del HUD nuevo (T-700). El asset es UI/Resources/ConfigHudH10.asset
    /// (lo crea CreadorHudH10); <see cref="Actual"/> lo carga una vez. Los sprites son blancos: el HUD los tiñe.
    /// </summary>
    public class ConfigHudH10 : ScriptableObject
    {
        public const string NombreRecurso = "ConfigHudH10";
        public const int Zonas = 4;

        [SerializeField] private Font fuente;
        [SerializeField] private Font fuenteNumeros;
        [SerializeField] private Sprite estrellaContorno;
        [SerializeField] private Sprite estrellaLlena;
        [Tooltip("Cabeza, torso, brazos, piernas (en ese orden).")]
        [SerializeField] private Sprite[] cuerpoLinea = new Sprite[Zonas];
        [Tooltip("Cabeza, torso, brazos, piernas (en ese orden).")]
        [SerializeField] private Sprite[] cuerpoRelleno = new Sprite[Zonas];
        [SerializeField] private string[] idsObjeto = new string[0];
        [SerializeField] private Sprite[] iconosObjeto = new Sprite[0];
        [SerializeField] private Sprite conoMinimapa;
        [SerializeField] private Sprite puntoRuta;
        [Header("Gráficos del chat de arte (T-725)")]
        [SerializeField] private Sprite marcoEscudo;
        [SerializeField] private Sprite marcoVida;
        [SerializeField] private Sprite rellenoEscudo;
        [SerializeField] private Sprite rellenoVida;
        [SerializeField] private Sprite emblemaVida;
        [SerializeField] private Sprite marcoMinimapa;
        [SerializeField] private Sprite casillaObjeto;
        [Tooltip("Segundos que la parte perdida de la barra se queda antes de bajar (s).")]
        [Min(0f)] [SerializeField] private float segundosEsperaPerdida = 0.45f;
        [Tooltip("Segundos que tarda la parte perdida en bajar hasta la vida actual (s).")]
        [Min(0.01f)] [SerializeField] private float segundosBajaPerdida = 0.6f;

        private static ConfigHudH10 actual;

        public static ConfigHudH10 Actual
        {
            get
            {
                if (actual == null)
                {
                    actual = Resources.Load<ConfigHudH10>(NombreRecurso);
                }
                return actual;
            }
        }

        public Font Fuente => fuente;
        public Font FuenteNumeros => fuenteNumeros != null ? fuenteNumeros : fuente;
        public Sprite EstrellaContorno => estrellaContorno;
        public Sprite EstrellaLlena => estrellaLlena;
        public Sprite ConoMinimapa => conoMinimapa;
        public Sprite PuntoRuta => puntoRuta;
        public Sprite MarcoEscudo => marcoEscudo;
        public Sprite MarcoVida => marcoVida;
        public Sprite RellenoEscudo => rellenoEscudo;
        public Sprite RellenoVida => rellenoVida;
        public Sprite EmblemaVida => emblemaVida;
        public Sprite MarcoMinimapa => marcoMinimapa;
        public Sprite CasillaObjeto => casillaObjeto;
        public float SegundosEsperaPerdida => segundosEsperaPerdida;
        public float SegundosBajaPerdida => segundosBajaPerdida;

        public Sprite CuerpoLinea(ZonaCuerpoH10 zona) => Elegir(cuerpoLinea, (int)zona);

        public Sprite CuerpoRelleno(ZonaCuerpoH10 zona) => Elegir(cuerpoRelleno, (int)zona);

        /// <summary>El ícono de línea del objeto con ese id, o null si no hay.</summary>
        public Sprite Icono(string id)
        {
            for (int i = 0; i < idsObjeto.Length && i < iconosObjeto.Length; i++)
            {
                if (idsObjeto[i] == id)
                {
                    return iconosObjeto[i];
                }
            }
            return null;
        }

        private static Sprite Elegir(Sprite[] lista, int indice) => lista != null && indice >= 0 && indice < lista.Length ? lista[indice] : null;

#if UNITY_EDITOR
        /// <summary>Solo para el creador del editor.</summary>
        public void Llenar(Font f, Font n, Sprite contorno, Sprite llena, Sprite[] linea, Sprite[] relleno, string[] ids, Sprite[] iconos, Sprite cono, Sprite punto)
        {
            fuente = f;
            fuenteNumeros = n;
            estrellaContorno = contorno;
            estrellaLlena = llena;
            cuerpoLinea = linea;
            cuerpoRelleno = relleno;
            idsObjeto = ids;
            iconosObjeto = iconos;
            conoMinimapa = cono;
            puntoRuta = punto;
        }

        /// <summary>Solo para el creador del editor: los gráficos de las barras, el marco del minimapa y la casilla.</summary>
        public void LlenarGraficos(Sprite marcoE, Sprite marcoV, Sprite rellenoE, Sprite rellenoV, Sprite emblema, Sprite marcoMini, Sprite casilla)
        {
            marcoEscudo = marcoE;
            marcoVida = marcoV;
            rellenoEscudo = rellenoE;
            rellenoVida = rellenoV;
            emblemaVida = emblema;
            marcoMinimapa = marcoMini;
            casillaObjeto = casilla;
        }
#endif
    }
}

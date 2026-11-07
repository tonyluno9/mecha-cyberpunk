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
#endif
    }
}

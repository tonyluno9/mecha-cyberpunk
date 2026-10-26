using MechaCyberpunk.Combate.Habilidades;
using MechaCyberpunk.Economia;
using UnityEngine;

namespace MechaCyberpunk.Mecha
{
    /// <summary>Una pieza del mecha: es un objeto del inventario (tipo siempre Pieza) con bonos (T-166).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de pieza", fileName = "DatosPieza_Nueva")]
    public class DatosPieza : DatosObjeto
    {
        [Tooltip("Ranura donde se pone.")]
        [SerializeField] private RanuraPieza ranura;

        [Tooltip("Rama o estilo.")]
        [SerializeField] private RamaPieza rama;

        [Tooltip("Nivel: 0 = de serie, 1 a 4.")]
        [Range(0, 4)] [SerializeField] private int nivel;

        [Tooltip("Precio en el taller ($, entero). 0 = no se compra.")]
        [Min(0)] [SerializeField] private int precio;

        [Tooltip("Vida que suma (puntos).")]
        [SerializeField] private float bonoVida;

        [Tooltip("Daño que suma por golpe (puntos).")]
        [SerializeField] private float bonoDanio;

        [Tooltip("Velocidad que suma (m/s).")]
        [SerializeField] private float bonoVelocidad;

        [Tooltip("Recarga entre golpes que suma (s). Negativo = golpea más seguido.")]
        [SerializeField] private float bonoRecarga;

        [Header("Habilidad (T-174)")]
        [Tooltip("Habilidad especial que trae la pieza. Vacío = no trae (piezas de serie, brazo derecho, piernas, cabina).")]
        [SerializeField] private DatosHabilidad habilidad;

        [Tooltip("Munición que gasta la habilidad. Vacío si no hay habilidad.")]
        [SerializeField] private DatosObjeto municion;

        [Tooltip("Cuánto acorta los enfriamientos de las habilidades (fracción, 0.03 = 3 %). Solo las piezas tech.")]
        [Range(0f, 0.4f)] [SerializeField] private float bonoEnfriamiento;

        public DatosHabilidad Habilidad => habilidad;
        public DatosObjeto Municion => municion;
        public float BonoEnfriamiento => bonoEnfriamiento;
        public RanuraPieza Ranura => ranura;
        public RamaPieza Rama => rama;
        public int Nivel => nivel;
        public int Precio => precio;
        public float BonoVida => bonoVida;
        public float BonoDanio => bonoDanio;
        public float BonoVelocidad => bonoVelocidad;
        public float BonoRecarga => bonoRecarga;

        /// <summary>Crea una pieza en memoria (para pruebas y herramientas).</summary>
        public static DatosPieza Crear(string id, string nombre, RanuraPieza ranura, RamaPieza rama, int nivel, int precio,
            float bonoVida, float bonoDanio, float bonoVelocidad, float bonoRecarga, float bonoEnfriamiento = 0f,
            DatosHabilidad habilidad = null, DatosObjeto municion = null)
        {
            var p = CreateInstance<DatosPieza>();
            p.Configurar(id, nombre, TipoObjeto.Pieza, precio / 2, 2);
            p.ranura = ranura;
            p.rama = rama;
            p.nivel = Mathf.Clamp(nivel, 0, 4);
            p.precio = Mathf.Max(0, precio);
            p.bonoVida = bonoVida;
            p.bonoDanio = bonoDanio;
            p.bonoVelocidad = bonoVelocidad;
            p.bonoRecarga = bonoRecarga;
            p.bonoEnfriamiento = bonoEnfriamiento;
            p.habilidad = habilidad;
            p.municion = municion;
            return p;
        }
    }
}

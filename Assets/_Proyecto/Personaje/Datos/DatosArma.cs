using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>
    /// Un arma a pie en un nivel (T-403, D-tony-215): es un objeto del inventario (tipo Arma) como <c>DatosPieza</c>, un asset por arma
    /// y nivel. El nivel 0 es el arma base; los niveles 1 a 3 (I a III) se compran como mejoras.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de arma", fileName = "DatosArma_Nueva")]
    public class DatosArma : DatosObjeto
    {
        [Tooltip("Arma a la que pertenece.")]
        [SerializeField] private ArmaAPie arma;

        [Tooltip("Nivel: 0 = base, 1 a 3 = I a III.")]
        [Range(0, 3)] [SerializeField] private int nivel;

        [Tooltip("Precio en el mercado negro ($, entero). 0 = no se compra.")]
        [Min(0)] [SerializeField] private int precio;

        [Tooltip("Daño de cada bala o perdigón (puntos).")]
        [Min(0f)] [SerializeField] private float danio = 25f;

        [Tooltip("Perdigones por disparo (1 = una bala).")]
        [Min(1)] [SerializeField] private int perdigones = 1;

        [Tooltip("Balas que caben en el cargador.")]
        [Min(1)] [SerializeField] private int cargador = 8;

        [Tooltip("Tiempo de recarga (s). En los puños, el tiempo de recuperación del golpe.")]
        [Min(0.01f)] [SerializeField] private float recarga = 1.4f;

        [Tooltip("Tiempo mínimo entre disparos (s).")]
        [Min(0.01f)] [SerializeField] private float tiempoEntreDisparos = 0.25f;

        [Tooltip("True: mantener el clic sigue disparando.")]
        [SerializeField] private bool automatica;

        [Tooltip("Dispersión del disparo sin apuntar (grados, semiángulo del cono).")]
        [Min(0f)] [SerializeField] private float dispersion;

        [Tooltip("Dispersión apuntando (grados).")]
        [Min(0f)] [SerializeField] private float dispersionApuntando;

        [Tooltip("Distancia máxima del disparo (m).")]
        [Min(1f)] [SerializeField] private float alcance = 60f;

        public ArmaAPie Arma => arma;
        public int Nivel => nivel;
        public int Precio => precio;
        public float Danio => danio;
        public int Perdigones => perdigones;
        public int Cargador => cargador;
        public float Recarga => recarga;
        public float TiempoEntreDisparos => tiempoEntreDisparos;
        public bool Automatica => automatica;
        public float Dispersion => dispersion;
        public float DispersionApuntando => dispersionApuntando;
        public float Alcance => alcance;

        /// <summary>Crea un arma en memoria (para pruebas y herramientas).</summary>
        public static DatosArma Crear(string id, string nombre, ArmaAPie arma, int nivel, int precio, float danio, int perdigones,
            int cargador, float recarga, float tiempoEntreDisparos, bool automatica, float dispersion, float dispersionApuntando,
            float alcance)
        {
            var a = CreateInstance<DatosArma>();
            a.Configurar(id, nombre, TipoObjeto.Arma, precio / 2, 1);
            a.arma = arma;
            a.nivel = Mathf.Clamp(nivel, 0, 3);
            a.precio = Mathf.Max(0, precio);
            a.danio = danio;
            a.perdigones = Mathf.Max(1, perdigones);
            a.cargador = Mathf.Max(1, cargador);
            a.recarga = recarga;
            a.tiempoEntreDisparos = tiempoEntreDisparos;
            a.automatica = automatica;
            a.dispersion = dispersion;
            a.dispersionApuntando = dispersionApuntando;
            a.alcance = alcance;
            return a;
        }
    }
}

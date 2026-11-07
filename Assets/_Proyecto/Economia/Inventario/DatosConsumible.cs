using UnityEngine;

namespace MechaCyberpunk.Economia
{
    /// <summary>
    /// Un objeto que se usa con una tecla y da un efecto: cura, escudo, tiempo sin recibir daño o velocidad (T-685).
    /// Se compra en las expendedoras (su <c>Oferta</c> lleva el precio) y se guarda en el inventario como cualquier objeto.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de consumible", fileName = "DatosConsumible_Nuevo")]
    public class DatosConsumible : DatosObjeto
    {
        [Tooltip("Texto corto que ve el jugador (por ejemplo 'Cura 25 y 5 s sin daño').")]
        [SerializeField] private string descripcion = "";

        [Tooltip("Vida que cura en total (puntos). 0 = no cura.")]
        [Min(0f)] [SerializeField] private float curacion;

        [Tooltip("Segundos en que se reparte la curación (s). 0 = de golpe.")]
        [Min(0f)] [SerializeField] private float segundosCuracion;

        [Tooltip("Segundos sin recibir daño (s). 0 = ninguno.")]
        [Min(0f)] [SerializeField] private float segundosSinDanio;

        [Tooltip("Escudo que recupera de golpe (puntos). 0 = ninguno.")]
        [Min(0f)] [SerializeField] private float escudo;

        [Tooltip("Multiplicador de velocidad mientras dura (1 = sin efecto).")]
        [Min(1f)] [SerializeField] private float factorVelocidad = 1f;

        [Tooltip("Segundos que dura la velocidad extra (s).")]
        [Min(0f)] [SerializeField] private float segundosVelocidad;

        public string Descripcion => descripcion;
        public float Curacion => curacion;
        public float SegundosCuracion => segundosCuracion;
        public float SegundosSinDanio => segundosSinDanio;
        public float Escudo => escudo;
        public float FactorVelocidad => factorVelocidad;
        public float SegundosVelocidad => segundosVelocidad;

        /// <summary>Crea un consumible en memoria (para pruebas y herramientas).</summary>
        public static DatosConsumible Crear(string id, string nombre, int maximo, float curacion, float segundosCuracion,
            float segundosSinDanio, float escudo, float factorVelocidad, float segundosVelocidad)
        {
            var o = CreateInstance<DatosConsumible>();
            o.Configurar(id, nombre, TipoObjeto.Consumible, 0, maximo);
            o.curacion = curacion;
            o.segundosCuracion = segundosCuracion;
            o.segundosSinDanio = segundosSinDanio;
            o.escudo = escudo;
            o.factorVelocidad = factorVelocidad;
            o.segundosVelocidad = segundosVelocidad;
            return o;
        }
    }
}

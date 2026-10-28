using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Una misión como datos (T-183, D-tony-147): pasos, pago, reputaciones, favor, chatarra y requisitos.</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de misión", fileName = "DatosMision_Nueva")]
    public class DatosMision : ScriptableObject
    {
        [Tooltip("Identificador estable de la misión (texto). Es la clave del guardado.")]
        [SerializeField] private string id = "mision";

        [Tooltip("Título para pantallas, en mayúsculas (texto).")]
        [SerializeField] private string titulo = "MISIÓN";

        [Tooltip("Una frase que cuenta de qué va (texto).")]
        [SerializeField] private string descripcion = "";

        [Tooltip("Clase de misión.")]
        [SerializeField] private TipoMision tipo = TipoMision.Barrio;

        [Tooltip("Dónde se ofrece.")]
        [SerializeField] private OrigenMision origen = OrigenMision.Calle;

        [Tooltip("En qué parte del día se ofrece.")]
        [SerializeField] private FranjaMision franja = FranjaMision.Cualquiera;

        [Tooltip("Id del punto Encargo donde se ofrece (prefijo Mision_). Solo para origen Calle; vacío en los demás.")]
        [SerializeField] private string idEncargo = "";

        [Tooltip("Los pasos, en orden.")]
        [SerializeField] private PasoMision[] pasos = Array.Empty<PasoMision>();

        [Tooltip("Dinero que paga al cumplirse ($, entero).")]
        [Min(0)] [SerializeField] private int pago;

        [Tooltip("Objeto que entrega al cumplirse (por ejemplo la chatarra). Vacío = ninguno.")]
        [SerializeField] private DatosObjeto chatarraObjeto;

        [Tooltip("Cuántas unidades del objeto entrega (entero).")]
        [Min(0)] [SerializeField] private int chatarraCantidad;

        [Tooltip("Cambio en la reputación del barrio al cumplirse (puntos, de -100 a 100).")]
        [Range(-100, 100)] [SerializeField] private int deltaBarrio;

        [Tooltip("Cambio en la reputación de las corporaciones al cumplirse (puntos, de -100 a 100).")]
        [Range(-100, 100)] [SerializeField] private int deltaCorporaciones;

        [Tooltip("Favor que da al cumplirse. Vacío = ninguno.")]
        [SerializeField] private DatosFavor favor;

        [Tooltip("Si es verdadero, hay que gastar un favor del tipo de abajo para aceptarla.")]
        [SerializeField] private bool exigeFavor;

        [Tooltip("Tipo de favor que se gasta al aceptar (solo si exige favor).")]
        [SerializeField] private TipoFavor favorRequerido = TipoFavor.Informacion;

        [Tooltip("Nivel mínimo de la reputación del barrio para ofrecerla (de -2 a 2; -2 = sin requisito).")]
        [Range(-2, 2)] [SerializeField] private int nivelBarrioMinimo = -2;

        [Tooltip("Nivel mínimo de la reputación de las corporaciones para ofrecerla (de -2 a 2; -2 = sin requisito).")]
        [Range(-2, 2)] [SerializeField] private int nivelCorporacionesMinimo = -2;

        [Tooltip("Liga mínima abierta para ofrecerla (de 1 a 4).")]
        [Range(1, 4)] [SerializeField] private int ligaMinima = 1;

        [Tooltip("Tiempo máximo para cumplirla (segundos). 0 = sin límite.")]
        [Min(0f)] [SerializeField] private float limiteSegundos;

        [Tooltip("Si es verdadero, se puede repetir al día siguiente.")]
        [SerializeField] private bool repetible = true;

        public string Id => id;
        public string Titulo => titulo;
        public string Descripcion => descripcion;
        public TipoMision Tipo => tipo;
        public OrigenMision Origen => origen;
        public FranjaMision Franja => franja;
        public string IdEncargo => idEncargo ?? "";
        public PasoMision[] Pasos => pasos ?? Array.Empty<PasoMision>();
        public int Pago => Mathf.Max(0, pago);
        public DatosObjeto ChatarraObjeto => chatarraObjeto;
        public int ChatarraCantidad => chatarraObjeto == null ? 0 : Mathf.Max(0, chatarraCantidad);
        public int DeltaBarrio => deltaBarrio;
        public int DeltaCorporaciones => deltaCorporaciones;
        public DatosFavor Favor => favor;
        public bool ExigeFavor => exigeFavor;
        public TipoFavor FavorRequerido => favorRequerido;
        public int NivelBarrioMinimo => nivelBarrioMinimo;
        public int NivelCorporacionesMinimo => nivelCorporacionesMinimo;
        public int LigaMinima => ligaMinima;
        public float LimiteSegundos => Mathf.Max(0f, limiteSegundos);
        public bool Repetible => repetible;

        /// <summary>Valor de la chatarra que da, a su precio de venta ($).</summary>
        public int ValorChatarra => chatarraObjeto == null ? 0 : ChatarraCantidad * chatarraObjeto.ValorVenta;

        /// <summary>Calor total que suman sus pasos.</summary>
        public float CalorTotal
        {
            get
            {
                float total = 0f;
                foreach (PasoMision p in Pasos) total += p.Calor;
                return total;
            }
        }

        /// <summary>Crea una misión en memoria (para pruebas y herramientas).</summary>
        public static DatosMision Crear(string id, string titulo, TipoMision tipo, OrigenMision origen, FranjaMision franja,
            PasoMision[] pasos, int pago = 0, int deltaBarrio = 0, int deltaCorporaciones = 0, string idEncargo = "",
            float limiteSegundos = 0f, bool repetible = true, DatosFavor favor = null, DatosObjeto chatarraObjeto = null,
            int chatarraCantidad = 0, bool exigeFavor = false, TipoFavor favorRequerido = TipoFavor.Informacion,
            int nivelBarrioMinimo = -2, int nivelCorporacionesMinimo = -2, int ligaMinima = 1, string descripcion = "")
        {
            var m = CreateInstance<DatosMision>();
            m.id = id;
            m.titulo = titulo;
            m.descripcion = descripcion;
            m.tipo = tipo;
            m.origen = origen;
            m.franja = franja;
            m.idEncargo = idEncargo ?? "";
            m.pasos = pasos ?? Array.Empty<PasoMision>();
            m.pago = pago;
            m.deltaBarrio = deltaBarrio;
            m.deltaCorporaciones = deltaCorporaciones;
            m.limiteSegundos = limiteSegundos;
            m.repetible = repetible;
            m.favor = favor;
            m.chatarraObjeto = chatarraObjeto;
            m.chatarraCantidad = chatarraCantidad;
            m.exigeFavor = exigeFavor;
            m.favorRequerido = favorRequerido;
            m.nivelBarrioMinimo = nivelBarrioMinimo;
            m.nivelCorporacionesMinimo = nivelCorporacionesMinimo;
            m.ligaMinima = ligaMinima;
            return m;
        }

        private void OnValidate()
        {
            if (pago < 0) pago = 0;
            if (chatarraCantidad < 0) chatarraCantidad = 0;
            if (limiteSegundos < 0f) limiteSegundos = 0f;
            if (ligaMinima < 1) ligaMinima = 1;
            nivelBarrioMinimo = Mathf.Clamp(nivelBarrioMinimo, -2, 2);
            nivelCorporacionesMinimo = Mathf.Clamp(nivelCorporacionesMinimo, -2, 2);
        }
    }
}

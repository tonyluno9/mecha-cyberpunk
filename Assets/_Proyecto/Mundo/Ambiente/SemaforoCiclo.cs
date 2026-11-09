using MechaCyberpunk.Mundo.Datos;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// T-910: el semáforo ES01 de una esquina. Tres lentes (rojo, ámbar, verde: discos sobre las lentes del modelo) de las que solo una está
    /// encendida a la vez, según <see cref="CalculoSemaforo"/>. Cambia la emisión con un MaterialPropertyBlock (no crea materiales) y, si hay,
    /// el color de la luz. Con <c>contrafase</c> arranca a medio ciclo: la esquina de la calle cruzada. No toca la IA del tráfico.
    /// </summary>
    public class SemaforoCiclo : MonoBehaviour
    {
        private static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");
        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");

        [SerializeField] private ConfigSemaforo config;
        [Tooltip("Lentes en este orden: rojo, ámbar, verde.")]
        [SerializeField] private Renderer[] lentes = new Renderer[3];
        [Tooltip("Color de cada lente encendida, en el mismo orden.")]
        [SerializeField] private Color[] colores = { new Color(1f, 0.12f, 0.08f), new Color(1f, 0.62f, 0.05f), new Color(0.1f, 1f, 0.3f) };
        [Tooltip("Luz opcional que toma el color de la lente encendida.")]
        [SerializeField] private Light luz;
        [Tooltip("Arranca a medio ciclo: la esquina de la calle cruzada.")]
        [SerializeField] private bool contrafase;

        private CalculoSemaforo calculo;
        private MaterialPropertyBlock bloque;
        private int fasePintada = -1;

        /// <summary>La luz encendida ahora.</summary>
        public FaseSemaforo Fase
        {
            get
            {
                Preparar();
                return calculo.Fase;
            }
        }

        public bool Contrafase => contrafase;

        private void Awake()
        {
            Preparar();
            Pintar();
        }

        private void Update()
        {
            Preparar();
            calculo.Avanzar(Time.deltaTime);
            Pintar();
        }

        private void Preparar()
        {
            if (calculo != null)
            {
                return;
            }
            bloque = new MaterialPropertyBlock();
            if (config == null)
            {
                Debug.LogError("[Mundo] SemaforoCiclo en '" + name + "' necesita su ConfigSemaforo.", this);
                enabled = false;
                calculo = new CalculoSemaforo(8f, 2f, 10f);
                return;
            }
            calculo = new CalculoSemaforo(config.SegundosVerde, config.SegundosAmbar, config.SegundosRojo, contrafase ? config.Ciclo * 0.5f : 0f);
        }

        private void Pintar()
        {
            if (config == null)
            {
                return;
            }
            // Orden de las lentes: rojo (0), ámbar (1), verde (2).
            int activa = calculo.Fase == FaseSemaforo.Rojo ? 0 : calculo.Fase == FaseSemaforo.Ambar ? 1 : 2;
            if (activa == fasePintada)
            {
                return;
            }
            fasePintada = activa;
            for (int i = 0; i < lentes.Length && i < colores.Length; i++)
            {
                if (lentes[i] == null)
                {
                    continue;
                }
                bool encendida = i == activa;
                lentes[i].GetPropertyBlock(bloque);
                bloque.SetColor(IdColorBase, encendida ? colores[i] : colores[i] * config.NivelApagada);
                bloque.SetColor(IdEmision, encendida ? colores[i] * config.IntensidadEmision : Color.black);
                lentes[i].SetPropertyBlock(bloque);
            }
            if (luz != null)
            {
                luz.color = colores[activa];
            }
        }
    }
}

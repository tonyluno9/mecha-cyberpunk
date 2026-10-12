using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using UnityEngine;

namespace MechaCyberpunk.IA.Rival
{
    /// <summary>
    /// Lo que se ve del rival al recibir daño y al caer. Va fuera de la lista <c>controlados</c> del peleador para
    /// seguir vivo en el KO: parpadea con cada daño aplicado, se inclina hacia atrás hasta quedar tirado y vuelve a
    /// ponerse de pie cuando la pelea se prepara de nuevo.
    /// </summary>
    public class PresentacionRival : MonoBehaviour
    {
        private const float GradosTirado = 90f;
        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");

        [SerializeField] private ConfigPresentacionRival config;
        [Tooltip("Pivote en los pies que contiene el cuerpo y el brazo. Es lo que se inclina al caer.")]
        [SerializeField] private Transform visual;
        [Tooltip("Pieza del cuerpo que parpadea.")]
        [SerializeField] private Renderer cuerpo;

        private VidaMecha vida;
        private PeleadorMecha peleador;
        private IPeleador identidad;
        private MaterialPropertyBlock bloque;
        private float parpadeoRestante;
        private float progresoCaida;
        private bool cayendo;

        public bool Parpadeando => parpadeoRestante > 0f;
        /// <summary>Qué tan inclinado está el cuerpo respecto a vertical (°). 0 de pie, 90 tirado.</summary>
        public float Inclinacion => progresoCaida * GradosTirado;
        public Renderer Cuerpo => cuerpo;
        public Transform Visual => visual;

        private void Awake()
        {
            vida = GetComponent<VidaMecha>();
            peleador = GetComponent<PeleadorMecha>();
            identidad = peleador;
            if (config == null || visual == null || cuerpo == null || vida == null || peleador == null)
            {
                Debug.LogError("[IA] Faltan referencias en PresentacionRival de " + name, this);
                enabled = false;
                return;
            }
            bloque = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            EventosPelea.ImpactoRecibido += AlRecibirImpacto;
            vida.KO += AlLlegarAKO;
            peleador.PeleaPreparada += AlPrepararPelea;
        }

        private void OnDisable()
        {
            EventosPelea.ImpactoRecibido -= AlRecibirImpacto;
            vida.KO -= AlLlegarAKO;
            peleador.PeleaPreparada -= AlPrepararPelea;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (parpadeoRestante > 0f)
            {
                parpadeoRestante -= dt;
                if (parpadeoRestante <= 0f)
                {
                    cuerpo.SetPropertyBlock(null);
                }
            }
            if (cayendo && progresoCaida < 1f)
            {
                progresoCaida = Mathf.Min(1f, progresoCaida + dt / config.DuracionCaida);
                visual.localRotation = Quaternion.Euler(-Inclinacion, 0f, 0f);
            }
        }

        private void AlRecibirImpacto(IPeleador objetivo, InfoDanio info, ResultadoDanio resultado)
        {
            if (objetivo != identidad || resultado.DanioAplicado <= 0f)
            {
                return;
            }
            parpadeoRestante = config.DuracionParpadeo;
            cuerpo.GetPropertyBlock(bloque);
            bloque.SetColor(IdColorBase, config.ColorParpadeo);
            cuerpo.SetPropertyBlock(bloque);
        }

        private void AlLlegarAKO()
        {
            cayendo = true;
        }

        private void AlPrepararPelea()
        {
            cayendo = false;
            progresoCaida = 0f;
            parpadeoRestante = 0f;
            visual.localRotation = Quaternion.identity;
            cuerpo.SetPropertyBlock(null);
        }
    }
}

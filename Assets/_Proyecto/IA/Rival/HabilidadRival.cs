using MechaCyberpunk.Combate.Habilidades;
using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using UnityEngine;

namespace MechaCyberpunk.IA.Rival
{
    /// <summary>
    /// El jefe usa una habilidad de su liga (T-178, D-tony-139): espera su enfriamiento, se detiene y brilla con el color de la rama
    /// durante el aviso y luego la lanza hacia donde mira. No gasta munición. Se reinicia en cada pelea (<c>PeleaIniciada</c>); con KO,
    /// sin control o aturdido corta el aviso sin lanzar. Va junto a <see cref="RivalBasico"/> y <see cref="LanzadorHabilidades"/>, y fuera
    /// de la lista <c>controlados</c>: tiene que seguir vivo mientras el rival está aturdido para cortar el aviso.
    /// </summary>
    [DefaultExecutionOrder(ColorOrdenEjecucion)]
    [RequireComponent(typeof(RivalBasico))]
    [RequireComponent(typeof(LanzadorHabilidades))]
    [RequireComponent(typeof(PeleadorMecha))]
    [RequireComponent(typeof(VidaMecha))]
    public class HabilidadRival : MonoBehaviour
    {
        // Después de FuriaJefe y PresentacionRival, para que el color del aviso gane sobre el pulso de furia.
        private const int ColorOrdenEjecucion = 100;
        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");
        private static readonly int IdColorEmision = Shader.PropertyToID("_EmissionColor");

        [Tooltip("Qué habilidad lanza y cada cuánto (DatosHabilidadRival_Jefe1..4).")]
        [SerializeField] private DatosHabilidadRival datos;

        private RivalBasico rival;
        private LanzadorHabilidades lanzador;
        private PeleadorMecha peleador;
        private VidaMecha vida;
        private FuriaJefe furia;
        private PresentacionRival presentacion;
        private ReglaHabilidadRival regla;
        private MaterialPropertyBlock bloque;
        private bool enPelea;
        private bool pintado;
        private GameObject objetivoCacheado;
        private IPeleador peleadorObjetivo;
        private bool objetivoEnRango;

        /// <summary>
        /// Segundos que faltan para que empiece su próximo aviso (T-194): 0 si ya avisa o está lista; -1 si no se sabe (fuera de pelea
        /// o con el enfriamiento listo pero el objetivo fuera de distancia).
        /// </summary>
        public float SegundosParaAviso
        {
            get
            {
                if (regla == null || !enPelea)
                {
                    return -1f;
                }
                if (regla.Avisando)
                {
                    return 0f;
                }
                float restante = (1f - regla.Progreso) * regla.Enfriamiento(furia != null && furia.EnFuria);
                return restante <= 0f && !objetivoEnRango ? -1f : Mathf.Max(0f, restante);
            }
        }

        /// <summary>El rival está avisando: quieto y brillando antes de lanzar.</summary>
        public bool EnAviso => regla != null && regla.Avisando;
        public DatosHabilidadRival Datos => datos;
        /// <summary>Cuánto del enfriamiento ya pasó (0 a 1).</summary>
        public float ProgresoEnfriamiento => regla != null ? regla.Progreso : 0f;

        /// <summary>Se avisa al empezar el aviso.</summary>
        public event System.Action AvisoIniciado;
        /// <summary>Se avisa al lanzar la habilidad.</summary>
        public event System.Action Lanzada;

        private void Awake()
        {
            rival = GetComponent<RivalBasico>();
            lanzador = GetComponent<LanzadorHabilidades>();
            peleador = GetComponent<PeleadorMecha>();
            vida = GetComponent<VidaMecha>();
            furia = GetComponent<FuriaJefe>();
            presentacion = GetComponent<PresentacionRival>();
            if (datos == null || datos.Habilidad == null || presentacion == null)
            {
                Debug.LogError("[IA] Faltan referencias en HabilidadRival de " + name, this);
                enabled = false;
                return;
            }
            regla = new ReglaHabilidadRival(datos.Habilidad.Enfriamiento, datos.FactorEnfriamiento,
                datos.FactorEnfriamientoEnFuria, datos.SegundosAviso);
            bloque = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            EventosPelea.PeleaIniciada += AlIniciarPelea;
            EventosPelea.PeleaTerminada += AlTerminarPelea;
        }

        private void OnDisable()
        {
            EventosPelea.PeleaIniciada -= AlIniciarPelea;
            EventosPelea.PeleaTerminada -= AlTerminarPelea;
            enPelea = false;
            Detener();
        }

        private void AlIniciarPelea()
        {
            regla.Reiniciar();
            enPelea = true;
            Detener();
        }

        private void AlTerminarPelea(ResultadoPelea resultado)
        {
            enPelea = false;
            Detener();
        }

        private void Update()
        {
            if (!enPelea)
            {
                return;
            }
            bool control = peleador.ControlActivo && !peleador.EstaAturdido && !vida.EstaKO;
            objetivoEnRango = ObjetivoEnRango();
            bool puedeEmpezar = control && !rival.EnAviso && objetivoEnRango;
            bool enFuria = furia != null && furia.EnFuria;
            PasoHabilidadRival paso = regla.Avanzar(Time.deltaTime, enFuria, puedeEmpezar, control);
            switch (paso)
            {
                case PasoHabilidadRival.EmpezoAviso:
                    AvisoIniciado?.Invoke();
                    break;
                case PasoHabilidadRival.Lanzar:
                    lanzador.Lanzar(datos.Habilidad, datos.Nivel, false);
                    Lanzada?.Invoke();
                    break;
            }
            // Quieto durante el aviso y mientras dura la embestida.
            rival.Detenido = regla.Avisando || lanzador.Embistiendo;
        }

        private void LateUpdate()
        {
            if (regla.Avisando)
            {
                // Un parpadeo de daño manda sobre el color del aviso.
                if (!presentacion.Parpadeando)
                {
                    Pintar();
                }
            }
            else if (pintado)
            {
                Despintar();
            }
        }

        private bool ObjetivoEnRango()
        {
            GameObject objetivo = rival.Objetivo;
            if (objetivo == null)
            {
                return false;
            }
            if (objetivo != objetivoCacheado)
            {
                objetivoCacheado = objetivo;
                peleadorObjetivo = objetivo.GetComponent<IPeleador>();
            }
            if (peleadorObjetivo == null || peleadorObjetivo.Raiz == null
                || (peleadorObjetivo.Vida != null && peleadorObjetivo.Vida.EstaKO))
            {
                return false;
            }
            Vector3 hacia = peleadorObjetivo.Raiz.position - transform.position;
            hacia.y = 0f;
            float distancia = hacia.magnitude;
            return distancia >= datos.DistanciaMinima && distancia <= datos.DistanciaMaxima;
        }

        private void Pintar()
        {
            Color color = datos.Habilidad.ColorRama;
            Renderer cuerpo = presentacion.Cuerpo;
            cuerpo.GetPropertyBlock(bloque);
            bloque.SetColor(IdColorBase, color);
            bloque.SetColor(IdColorEmision, color);
            cuerpo.SetPropertyBlock(bloque);
            pintado = true;
        }

        private void Despintar()
        {
            pintado = false;
            if (!presentacion.Parpadeando)
            {
                presentacion.Cuerpo.SetPropertyBlock(null);
            }
        }

        // Cancela el aviso sin lanzar y suelta al rival.
        private void Detener()
        {
            if (regla != null)
            {
                regla.Reiniciar();
            }
            if (rival != null)
            {
                rival.Detenido = false;
            }
            if (pintado)
            {
                Despintar();
            }
        }
    }
}

using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Combate.Vida;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Combate.Bloqueo
{
    /// <summary>
    /// La guardia del mecha: mientras se mantiene Bloquear sube en <c>TiempoSubirGuardia</c> y se queda arriba; al soltar baja.
    /// No sube mientras un golpe propio está en Preparando.
    /// </summary>
    [RequireComponent(typeof(VidaMecha))]
    public class GuardiaMecha : MonoBehaviour
    {
        [SerializeField] private InputActionReference bloquear;
        [Tooltip("Verdadero: no lee entrada; sube y baja con Sostener(bool). Lo usa el rival (T-201). Falso: igual que el jugador.")]
        [SerializeField] private bool mandoPorCodigo;
        [Tooltip("Falso: el bloqueo de este mecha nunca es perfecto, siempre Normal (T-201). Verdadero: igual que siempre.")]
        [SerializeField] private bool permitePerfecto = true;

        private VidaMecha vida;
        private GolpeMecha golpe;
        private float progreso;
        private float tiempoLlegada;
        private bool sostenidoPorCodigo;

        /// <summary>Subida de la guardia de 0 (abajo) a 1 (arriba).</summary>
        public float Progreso => progreso;
        /// <summary>Si la guardia ya terminó de subir.</summary>
        public bool EstaArriba => progreso >= 1f;
        /// <summary>Si la guardia está subiendo, arriba o bajando (no está del todo abajo).</summary>
        public bool Levantada => progreso > 0f;
        /// <summary>Si <see cref="Sostener"/> la tiene pedida (solo en mando por código).</summary>
        public bool Pedida => mandoPorCodigo && sostenidoPorCodigo;
        /// <summary>Si el bloqueo de este mecha puede ser perfecto; en falso <c>VidaMecha</c> lo calcula con ventana 0.</summary>
        public bool PermitePerfecto => permitePerfecto;
        /// <summary>Segundos que lleva arriba; 0 si no está arriba.</summary>
        public float SegundosArriba => EstaArriba ? Time.time - tiempoLlegada : 0f;

        private void Awake()
        {
            vida = GetComponent<VidaMecha>();
            golpe = GetComponent<GolpeMecha>();
            if (bloquear == null && !mandoPorCodigo)
            {
                Debug.LogError("[Combate] Falta la acción Bloquear en GuardiaMecha de " + name, this);
                enabled = false;
            }
        }

        /// <summary>Sube (true) o baja (false) la guardia. Solo cuenta con el modo de mando por código.</summary>
        public void Sostener(bool sostener)
        {
            sostenidoPorCodigo = sostener;
        }

        private void OnEnable()
        {
            if (bloquear != null)
            {
                bloquear.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (bloquear != null)
            {
                bloquear.action.Disable();
            }
            progreso = 0f;
            sostenidoPorCodigo = false;
        }

        private void Update()
        {
            if (vida.Datos == null)
            {
                return;
            }
            float tiempoSubir = vida.Datos.TiempoSubirGuardia;
            float paso = Time.deltaTime / tiempoSubir;
            bool sostenido = mandoPorCodigo ? sostenidoPorCodigo : bloquear.action.IsPressed();
            bool puedeSubir = golpe == null || golpe.Fase != FaseGolpe.Preparando;

            if (!sostenido)
            {
                progreso = Mathf.Max(0f, progreso - paso);
            }
            else if (puedeSubir && progreso < 1f)
            {
                float nuevo = progreso + paso;
                if (nuevo >= 1f)
                {
                    // La guardia llegó arriba dentro de este frame: cuenta el tiempo sobrante.
                    tiempoLlegada = Time.time - (nuevo - 1f) * tiempoSubir;
                    nuevo = 1f;
                }
                progreso = nuevo;
            }
        }
    }
}

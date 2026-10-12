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

        private VidaMecha vida;
        private GolpeMecha golpe;
        private float progreso;
        private float tiempoLlegada;

        /// <summary>Subida de la guardia de 0 (abajo) a 1 (arriba).</summary>
        public float Progreso => progreso;
        /// <summary>Si la guardia ya terminó de subir.</summary>
        public bool EstaArriba => progreso >= 1f;
        /// <summary>Si la guardia está subiendo, arriba o bajando (no está del todo abajo).</summary>
        public bool Levantada => progreso > 0f;
        /// <summary>Segundos que lleva arriba; 0 si no está arriba.</summary>
        public float SegundosArriba => EstaArriba ? Time.time - tiempoLlegada : 0f;

        private void Awake()
        {
            vida = GetComponent<VidaMecha>();
            golpe = GetComponent<GolpeMecha>();
            if (bloquear == null)
            {
                Debug.LogError("[Combate] Falta la acción Bloquear en GuardiaMecha de " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            bloquear.action.Enable();
        }

        private void OnDisable()
        {
            bloquear.action.Disable();
            progreso = 0f;
        }

        private void Update()
        {
            if (vida.Datos == null)
            {
                return;
            }
            float tiempoSubir = vida.Datos.TiempoSubirGuardia;
            float paso = Time.deltaTime / tiempoSubir;
            bool sostenido = bloquear.action.IsPressed();
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

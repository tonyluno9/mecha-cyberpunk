using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// Zoom del sniper (T-416): con el sniper en la mano, mantener el clic derecho lleva el FOV a 15 en 0.15 s, baja la sensibilidad del
    /// ratón y cada tiro da una sacudida hacia arriba que vuelve sola. <see cref="CamaraHombro"/> lee <see cref="Mezcla"/> y <see cref="Sacudida"/>;
    /// la mira (<c>MiraSniper</c>) lee <see cref="Activo"/>. Al soltar o cambiar de arma todo vuelve.
    /// </summary>
    [RequireComponent(typeof(ControladorAPie))]
    [RequireComponent(typeof(ArmasAPie))]
    [RequireComponent(typeof(PistolaAPie))]
    [RequireComponent(typeof(VidaAPie))]
    public class ZoomSniper : MonoBehaviour
    {
        [SerializeField] private ConfigCamaraHombro config;

        private ControladorAPie controlador;
        private ArmasAPie armas;
        private PistolaAPie pistola;
        private VidaAPie vida;
        private float mezcla;
        private float inicioSacudida = -100f;

        /// <summary>0 sin zoom, 1 con el zoom completo.</summary>
        public float Mezcla => mezcla;
        /// <summary>True cuando el zoom ya cubre más de la mitad: la mira se prende.</summary>
        public bool Activo => mezcla > 0.5f;

        /// <summary>Sacudida vertical actual (°, positivo sube la mira).</summary>
        public float Sacudida
        {
            get
            {
                float duracion = config != null ? config.DuracionSacudida : 0.3f;
                float t = Mathf.Clamp01((Time.time - inicioSacudida) / duracion);
                return (config != null ? config.SacudidaZoom : 1.5f) * (1f - t);
            }
        }

        private void Awake()
        {
            controlador = GetComponent<ControladorAPie>();
            armas = GetComponent<ArmasAPie>();
            pistola = GetComponent<PistolaAPie>();
            vida = GetComponent<VidaAPie>();
        }

        private void OnEnable()
        {
            pistola.Disparo += AlDisparar;
        }

        private void OnDisable()
        {
            pistola.Disparo -= AlDisparar;
            mezcla = 0f;
            controlador.FactorSensibilidad = 1f;
        }

        private void AlDisparar()
        {
            if (Activo && armas.ArmaActiva == ArmaAPie.Sniper)
            {
                inicioSacudida = Time.time;
            }
        }

        private void Update()
        {
            bool quiere = armas.ArmaActiva == ArmaAPie.Sniper && controlador.Apuntando && controlador.ControlActivo && !vida.EstaKO;
            bool estabaActivo = Activo;
            float duracion = config != null ? config.DuracionZoom : 0.15f;
            mezcla = Mathf.MoveTowards(mezcla, quiere ? 1f : 0f, Time.deltaTime / duracion);
            if (!estabaActivo && Activo)
            {
                Sonidos.Reproducir("ARM_SNI_ZOOM");
            }
            float factor = config != null ? config.SensibilidadZoom : 0.35f;
            controlador.FactorSensibilidad = Mathf.Lerp(1f, factor, mezcla);
        }
    }
}

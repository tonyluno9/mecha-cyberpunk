using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// El jefe de una zona rival (T-404, D-tony-218). Va en un punto de la banqueta con un pandillero armado apagado como hijo (con los datos del jefe:
    /// 250 de vida, escopeta). Aparece cuando <see cref="ITerritorio.JefeDisponible"/> y no hay otro vivo; al vencerlo avisa
    /// <c>RegistrarVencido(..., jefe: true)</c> y la zona pasa a ser tuya. Ataca mientras el jugador esté a pie a menos de su radio de aviso.
    /// </summary>
    public class JefePandilla : MonoBehaviour
    {
        private const float SegundosBusqueda = 0.5f;

        [Tooltip("Id de la zona que manda este jefe (fosa, tianguis, neon).")]
        [SerializeField] private string zona = "tianguis";
        [SerializeField] private IdPandilla pandilla = IdPandilla.Calaveras;
        [Tooltip("El pandillero armado del jefe (hijo apagado).")]
        [SerializeField] private PandilleroAPie jefe;
        [Tooltip("Distancia a la que el jefe ya dispara al jugador (m).")]
        [Min(1f)] [SerializeField] private float radioAviso = 40f;

        private float proximaBusqueda;
        private bool aparecio;

        public string Zona => zona;
        public PandilleroAPie Jefe => jefe;
        /// <summary>True mientras el jefe está en pie.</summary>
        public bool Vivo => jefe != null && jefe.gameObject.activeSelf && !jefe.EstaKO;

        private void Awake()
        {
            if (jefe == null)
            {
                Debug.LogError("[IA] Falta el pandillero jefe en JefePandilla de " + name, this);
                enabled = false;
                return;
            }
            jefe.Vencido += AlVencer;
            jefe.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (jefe != null)
            {
                jefe.Vencido -= AlVencer;
            }
        }

        private void Update()
        {
            if (Vivo)
            {
                IJugador jugador = Registro<IJugador>.Actual;
                bool cerca = jugador != null && jugador.Raiz != null && jugador.Modo == ModoJugador.APie
                    && (jugador.Raiz.position - transform.position).sqrMagnitude <= radioAviso * radioAviso;
                if (cerca && !jefe.Alerta)
                {
                    Sonidos.ReproducirEn("PAN_JEFE_ENTRADA", transform.position);
                }
                jefe.Alerta = cerca;
                return;
            }
            if (Time.unscaledTime < proximaBusqueda)
            {
                return;
            }
            proximaBusqueda = Time.unscaledTime + SegundosBusqueda;
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            if (territorio == null || !territorio.JefeDisponible(zona) || jefe.gameObject.activeSelf)
            {
                return;
            }
            jefe.gameObject.SetActive(true);
            jefe.Configurar(transform.position, null);
            jefe.Reaparecer();
            aparecio = true;
            Debug.Log("[IA] Aparece el jefe de " + zona + ".");
        }

        private void AlVencer(PandilleroAPie vencido)
        {
            if (!aparecio)
            {
                return;
            }
            aparecio = false;
            Registro<ITerritorio>.Actual?.RegistrarVencido(vencido.transform.position, pandilla, true);
        }
    }
}

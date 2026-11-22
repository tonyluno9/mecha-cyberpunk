using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1631 (T17): "NOVA INTERVIENE": el policía a pie deja de atacar y se aleja del jugador hasta salir de su vista; solo entonces desaparece (nunca frente al jugador).
    /// Toma el mando del <see cref="PandilleroAPie"/> (IConductaAPie, que se evalúa antes que su IA normal) y apaga la conducta de escalera. Lo agrega
    /// <see cref="RespuestaNova"/> con <see cref="Ordenar"/>.
    /// </summary>
    public class ConductaRetiradaPolicia : MonoBehaviour, IConductaAPie
    {
        private const float GradosGiro = 540f;
        private const float AlturaSonda = 0.9f;
        private const float DistanciaSonda = 1.4f;
        private const float SegundosRevisarRumbo = 0.4f;
        private const float SegundosRevisarVista = 0.5f;
        /// <summary>Alcance (m) hasta donde la cámara cuenta como que "lo ve".</summary>
        private const float AlcanceVista = 250f;
        private const float MargenVista = 0.1f;

        private PandilleroAPie policia;
        private Vector3 rumbo;
        private float proximoRumbo;
        private float proximaVista;
        private int intento;

        public bool Controla => true;
        public Vector3 Rumbo => rumbo;

        /// <summary>Le da la orden de retirarse a este policía (una sola vez). Devuelve la conducta, o null si no aplica.</summary>
        public static ConductaRetiradaPolicia Ordenar(PandilleroAPie p)
        {
            if (p == null || p.Datos == null || !p.Datos.EsPolicia || p.EstaKO)
            {
                return null;
            }
            ConductaRetiradaPolicia ya = p.GetComponent<ConductaRetiradaPolicia>();
            if (ya != null)
            {
                return ya;
            }
            ConductaEscaleraPolicia escalera = p.GetComponent<ConductaEscaleraPolicia>();
            if (escalera != null)
            {
                escalera.enabled = false; // que no empiece a subir una escalera mientras se retira
            }
            ConductaRetiradaPolicia c = p.gameObject.AddComponent<ConductaRetiradaPolicia>();
            c.policia = p;
            p.ImponerConducta(c);
            DirectorRespuesta.MarcarRetirado(p.gameObject); // retirarse no es una baja: libera su tope al instante
            return c;
        }

        public void Actualizar(PandilleroAPie p, float dt)
        {
            if (p == null)
            {
                return;
            }
            Transform jugador = p.Jugador;
            Vector3 aqui = p.transform.position;
            Vector3 desde = jugador != null ? jugador.position : aqui - p.transform.forward;
            if (Time.time >= proximoRumbo)
            {
                proximoRumbo = Time.time + SegundosRevisarRumbo;
                ElegirRumbo(aqui, desde);
            }
            p.MirarHacia(rumbo, GradosGiro);
            p.MoverPlano(rumbo * PandilleroAPie.VelocidadCorrerPolicia);
            if (Time.time >= proximaVista)
            {
                proximaVista = Time.time + SegundosRevisarVista;
                Vector3 d = aqui - desde;
                d.y = 0f;
                bool loVe = LoVe(Camera.main, aqui);
                if (CalculoRetiradaPolicia.PuedeDesaparecer(d.magnitude, loVe, DirectorRespuesta.Parametros.MetrosMinimosRetiroPolicia))
                {
                    Destroy(p.gameObject);
                }
            }
        }

        // Se aleja del jugador; si hay algo tapando el camino, prueba rumbos desviados (izquierda, derecha, más abierto...) hasta uno libre.
        private void ElegirRumbo(Vector3 aqui, Vector3 desde)
        {
            Vector3 base0 = CalculoRetiradaPolicia.Rumbo(aqui, desde, policia != null ? policia.transform.forward : Vector3.forward);
            for (intento = 0; intento < 6; intento++)
            {
                Vector3 candidato = CalculoRetiradaPolicia.RumboDesviado(base0, intento);
                if (!Physics.Raycast(aqui + Vector3.up * AlturaSonda, candidato, DistanciaSonda, ~0, QueryTriggerInteraction.Ignore))
                {
                    rumbo = candidato;
                    return;
                }
            }
            rumbo = base0;
        }

        private static bool LoVe(Camera camara, Vector3 punto)
        {
            if (camara == null)
            {
                return false;
            }
            Vector3 v = camara.WorldToViewportPoint(punto + Vector3.up);
            return v.z > 0f && v.z < AlcanceVista && v.x > -MargenVista && v.x < 1f + MargenVista && v.y > -MargenVista && v.y < 1f + MargenVista;
        }
    }
}

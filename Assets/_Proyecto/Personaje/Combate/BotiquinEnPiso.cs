using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Vida;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// T-drops-24nov: botiquín pequeño que suelta un enemigo al caer. Flota y gira con un brillo verde suave; se recoge al pasar cerca (en VR
    /// también al acercar la mano) y cura <see cref="ConfigRecompensasEnemigo.CuracionBotiquin"/> a la <see cref="VidaAPie"/> del jugador.
    /// Con la vida llena se queda en el piso. Lo pone <see cref="RecompensasEnemigo"/>; desaparece solo a los segundos de la config.
    /// </summary>
    public class BotiquinEnPiso : MonoBehaviour
    {
        /// <summary>Prefab en Resources (lo crea RecompensasEnemigoT24nov en Personaje/Editor, con un modelo de consumible de Assets/Arte).</summary>
        public const string RecursoModelo = "BotiquinEnPiso_Modelo";
        public const string IdSonido = "CON_CURACION";
        private const float AlturaFlotar = 0.3f;
        private const float AmplitudFlotar = 0.05f;
        private const float RapidezFlotar = 4f;
        private const float GiroPorSegundo = 80f;
        private const float SegundosAntesDeRecoger = 0.6f;

        private Transform modelo;
        private float recogibleDesde;
        private bool tomado;

        public bool FueTomado => tomado;

        /// <summary>Se dispara al recoger, con la vida que curó.</summary>
        public static event Action<float> Recogido;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => Recogido = null;

        public static BotiquinEnPiso Crear(Vector3 posicionPiso)
        {
            var raiz = new GameObject("BotiquinEnPiso");
            raiz.transform.position = posicionPiso;
            var botiquin = raiz.AddComponent<BotiquinEnPiso>();
            botiquin.recogibleDesde = Time.time + SegundosAntesDeRecoger;
            botiquin.Armar();
            return botiquin;
        }

        private void Armar()
        {
            ConfigRecompensasEnemigo config = ConfigRecompensasEnemigo.Actual;
            modelo = new GameObject("Modelo").transform;
            modelo.SetParent(transform, false);
            GameObject prefab = Resources.Load<GameObject>(RecursoModelo);
            if (prefab != null)
            {
                GameObject instancia = Instantiate(prefab, modelo, false);
                instancia.name = "Botiquin";
                foreach (Collider c in instancia.GetComponentsInChildren<Collider>(true))
                {
                    Destroy(c);
                }
            }
            else
            {
                Debug.LogWarning("[Personaje] Falta Resources/" + RecursoModelo + ": corre RecompensasEnemigoT24nov.Aplicar. El botiquín solo brilla.");
            }
            Light luz = new GameObject("Luz").AddComponent<Light>();
            luz.transform.SetParent(transform, false);
            luz.transform.localPosition = Vector3.up * AlturaFlotar;
            luz.type = LightType.Point;
            luz.color = config.ColorBotiquin;
            luz.intensity = config.IntensidadBrillo;
            luz.range = config.AlcanceBrillo;
            luz.shadows = LightShadows.None;
        }

        private void Update()
        {
            if (tomado)
            {
                return;
            }
            if (modelo != null)
            {
                modelo.localPosition = Vector3.up * (AlturaFlotar + Mathf.Sin(Time.time * RapidezFlotar) * AmplitudFlotar);
                modelo.Rotate(0f, GiroPorSegundo * Time.deltaTime, 0f, Space.Self);
            }
            IJugador jugador = Registro<IJugador>.Actual;
            if (Time.time < recogibleDesde || jugador == null || jugador.Raiz == null)
            {
                return;
            }
            ConfigRecompensasEnemigo config = ConfigRecompensasEnemigo.Actual;
            int n = ManosRecogida.Posiciones(out Vector3[] manos);
            if (ReglaRecompensasEnemigo.Cerca(transform.position, jugador.Raiz.position, config.RadioRecoger, manos, n, config.RadioMano))
            {
                Recoger(VidaDe(jugador.Raiz));
            }
        }

        private Transform raizVida;
        private VidaAPie vidaJugador;

        // La vida del jugador, cacheada mientras el jugador sea el mismo (no se busca cada cuadro).
        private VidaAPie VidaDe(Transform raiz)
        {
            if (raiz != raizVida)
            {
                raizVida = raiz;
                vidaJugador = raiz.GetComponentInParent<VidaAPie>();
                if (vidaJugador == null)
                {
                    vidaJugador = raiz.GetComponentInChildren<VidaAPie>();
                }
            }
            return vidaJugador;
        }

        /// <summary>Cura y desaparece. Si no hay vida que curar (llena o KO), se queda en el piso.</summary>
        public void Recoger(VidaAPie vida)
        {
            if (tomado || vida == null || vida.EstaKO || vida.Actual >= vida.Maxima)
            {
                return;
            }
            float curo = vida.Curar(ConfigRecompensasEnemigo.Actual.CuracionBotiquin);
            if (curo <= 0f)
            {
                return;
            }
            tomado = true;
            Sonidos.ReproducirEn(IdSonido, transform.position);
            Recogido?.Invoke(curo);
            Destroy(gameObject);
        }
    }
}

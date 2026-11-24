using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>
    /// T-1310: al aterrizar de una caída alta (más de AlturaRodar, ~4 m) el jugador amortigua rodando (PJ01_Parkour_RodarCaida) en vez de recibir todo el golpe:
    /// pierde poca vida (nunca la deja en 0) y avanza rodando unos metros con el control congelado lo que dura el clip. Así se puede uno tirar desde un techo.
    /// Lo agrega <see cref="TreparAPie"/>. Corre después de CubiertaYRodada y antes de ControladorAPie, que son quienes escriben FactorVelocidad y DesplazamientoExterno.
    /// Sin ConfigAnimacionParkour (o sin el clip) no hace nada.
    /// </summary>
    [DefaultExecutionOrder(-5)]
    [RequireComponent(typeof(ControladorAPie))]
    public class CaidaJugador : MonoBehaviour
    {
        private const float SegundosMinimosAire = 0.3f;
        /// <summary>Más que esto (m) en un solo cuadro no es caer: es que alguien movió al jugador.</summary>
        private const float DistanciaTeletransporte = 3f;
        private Vector3 posicionAnterior;
        private bool tienePosicionAnterior;

        private ControladorAPie controlador;
        private VidaAPie vida;
        private TreparAPie trepar;
        private VisualPersonaje visual;
        private ReproductorClipUnico reproductor;
        private bool estabaEnAire;
        private float alturaMaxima;
        private float segundosAire;
        private float segundosRodando;
        private Vector3 direccionRodada;
        private float rapidezRodada;

        /// <summary>True mientras rueda tras la caída.</summary>
        public bool Rodando => segundosRodando > 0f;

        /// <summary>Caída (m) del último aterrizaje que se midió, para las pruebas.</summary>
        public float UltimaCaida { get; private set; }

        /// <summary>Vida que quitó el último aterrizaje, para las pruebas.</summary>
        public float UltimoDanio { get; private set; }

        private void Awake()
        {
            controlador = GetComponent<ControladorAPie>();
            vida = GetComponent<VidaAPie>();
            trepar = GetComponent<TreparAPie>();
            visual = GetComponentInChildren<VisualPersonaje>(true);
        }

        private void Update()
        {
            if (controlador == null || !controlador.ControlActivo && !Rodando)
            {
                return;
            }
            float dt = Time.deltaTime;
            // Un salto de varios metros en un cuadro es un teletransporte (Colocar, cargar partida, bajar del carro), no una caída: se olvida lo medido.
            Vector3 posicion = transform.position;
            bool teletransporte = tienePosicionAnterior && (posicion - posicionAnterior).sqrMagnitude > DistanciaTeletransporte * DistanciaTeletransporte;
            posicionAnterior = posicion;
            tienePosicionAnterior = true;
            if (teletransporte)
            {
                estabaEnAire = false;
                segundosAire = 0f;
                if (Rodando)
                {
                    segundosRodando = 0f;
                    controlador.FactorVelocidad = 1f;
                    controlador.DesplazamientoExterno = Vector3.zero;
                    controlador.SaltoBloqueado = false;
                }
                return;
            }
            if (Rodando)
            {
                segundosRodando -= dt;
                controlador.FactorVelocidad = 0f;
                controlador.DesplazamientoExterno = direccionRodada * rapidezRodada;
                if (segundosRodando <= 0f)
                {
                    controlador.FactorVelocidad = 1f;
                    controlador.DesplazamientoExterno = Vector3.zero;
                    controlador.SaltoBloqueado = false;
                }
                return;
            }
            if (trepar != null && trepar.Trepando)
            {
                estabaEnAire = false;
                segundosAire = 0f;
                return;
            }
            if (!controlador.EnPiso)
            {
                if (!estabaEnAire)
                {
                    estabaEnAire = true;
                    alturaMaxima = transform.position.y;
                    segundosAire = 0f;
                }
                alturaMaxima = Mathf.Max(alturaMaxima, transform.position.y);
                segundosAire += dt;
                return;
            }
            if (!estabaEnAire)
            {
                return;
            }
            estabaEnAire = false;
            if (segundosAire < SegundosMinimosAire)
            {
                return;
            }
            Aterrizar(alturaMaxima - transform.position.y);
        }

        private void Aterrizar(float caida)
        {
            UltimaCaida = caida;
            UltimoDanio = 0f;
            ConfigAnimacionParkour config = ConfigAnimacionParkour.Actual;
            if (config == null || vida == null || vida.EstaKO)
            {
                return;
            }
            float danio = ReglaParkour.DanioCaida(caida, config.UmbralDanioCaida, config.DanioPorMetro, config.FraccionDanioMaximo, vida.Actual, vida.Maxima);
            if (danio > 0f)
            {
                float antes = vida.Actual;
                vida.RecibirDanioSoloVida(new InfoDanio(danio, TipoDanio.Golpe, transform.position, Vector3.down, null)); // T-1327: el escudo no absorbe la caída
                UltimoDanio = antes - vida.Actual;
                Debug.Log("[Personaje] T-1310: caída de " + caida.ToString("0.0") + " m, daño " + UltimoDanio.ToString("0.0"));
            }
            // VR (vr/quest3): con visor no rueda al caer (la rodada mueve la cabeza sola).
            if (!ModoVR.Activo && config.RodarCaida != null && ReglaParkour.DebeRodar(caida, config.AlturaRodar) && visual != null)
            {
                IniciarRodada(config);
                Sonidos.ReproducirEn(ReglaSonidosJugador.Rodada, transform.position);
            }
        }

        private void IniciarRodada(ConfigAnimacionParkour config)
        {
            if (reproductor == null)
            {
                reproductor = visual.GetComponent<ReproductorClipUnico>();
                if (reproductor == null)
                {
                    reproductor = visual.gameObject.AddComponent<ReproductorClipUnico>();
                }
            }
            if (!reproductor.Reproducir(visual.Animador, config.RodarCaida))
            {
                return;
            }
            Vector3 frente = transform.forward;
            frente.y = 0f;
            direccionRodada = frente.sqrMagnitude > 0f ? frente.normalized : Vector3.forward;
            segundosRodando = config.RodarCaida.length;
            rapidezRodada = config.MetrosRodando / Mathf.Max(0.1f, segundosRodando);
            controlador.SaltoBloqueado = true;
        }
    }
}

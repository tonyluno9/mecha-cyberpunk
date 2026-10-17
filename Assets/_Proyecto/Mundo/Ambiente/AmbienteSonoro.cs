using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Datos;
using MechaCyberpunk.Mundo.Lugares;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// Ambiente sonoro de la ciudad (T-097): cuatro capas en bucle (lluvia, zumbido de neón, tráfico lejano y música de local) cuyo volumen
    /// sale de CalculoAmbienteSonoro según la hora, la lluvia y si el jugador está dentro de un local. Los clips son provisionales, generados
    /// por código (CreadorAudio); se cambian por finales sin tocar nada más.
    /// </summary>
    public class AmbienteSonoro : MonoBehaviour
    {
        [SerializeField] private ConfigAudioCiudad config;
        [SerializeField] private ConfigLluvia configLluvia;
        [SerializeField] private AudioClip clipLluvia;
        [SerializeField] private AudioClip clipNeon;
        [SerializeField] private AudioClip clipTrafico;
        [SerializeField] private AudioClip clipMusica;

        private AudioSource lluvia;
        private AudioSource neon;
        private AudioSource trafico;
        private AudioSource musica;
        private const float SegundosEntreRevisiones = 0.5f;

        private ZonaInterior[] zonas;
        private bool sonando;
        private AudioListener escucha;
        private float volLluvia;
        private float volNeon;
        private float volTrafico;
        private float volMusica;
        private float proximaRevision;

        /// <summary>Volumen objetivo calculado en el último fotograma.</summary>
        public MezclaAmbiente Objetivo { get; private set; }

        public float VolumenLluvia => volLluvia;
        public float VolumenNeon => volNeon;
        public float VolumenTrafico => volTrafico;
        public float VolumenMusica => volMusica;

        private void Awake()
        {
            if (config == null || configLluvia == null || clipLluvia == null || clipNeon == null || clipTrafico == null || clipMusica == null)
            {
                Debug.LogError("[Mundo] AmbienteSonoro en '" + name + "' necesita configs y los cuatro clips.", this);
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            zonas = FindObjectsByType<ZonaInterior>(FindObjectsSortMode.None);
        }

        private void Update()
        {
            if (sonando && (escucha == null || !escucha.isActiveAndEnabled))
            {
                Detener();     // se destruyó el AudioListener: sin él Unity avisa en la consola por cada fuente que suena
            }
            Arrancar();
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            bool deNoche = reloj == null || reloj.EsDeNoche;
            bool hayLluvia = CalculoLluvia.DebeLlover(LluviaSigueJugador.Activa, false, deNoche, configLluvia.LluviaDeDia);
            Objetivo = CalculoAmbienteSonoro.Calcular(EnInterior(), deNoche, hayLluvia, config.Niveles);
            float dt = Time.unscaledDeltaTime;
            volLluvia = CalculoAmbienteSonoro.Acercar(volLluvia, Objetivo.Lluvia, dt, config.SegundosCambio);
            volNeon = CalculoAmbienteSonoro.Acercar(volNeon, Objetivo.Neon, dt, config.SegundosCambio);
            volTrafico = CalculoAmbienteSonoro.Acercar(volTrafico, Objetivo.Trafico, dt, config.SegundosCambio);
            volMusica = CalculoAmbienteSonoro.Acercar(volMusica, Objetivo.Musica, dt, config.SegundosCambio);
            if (sonando)
            {
                lluvia.volume = volLluvia;
                neon.volume = volNeon;
                trafico.volume = volTrafico;
                musica.volume = volMusica;
            }
        }

        // Las capas empiezan a sonar cuando hay un AudioListener en la escena (Unity avisa en la consola si no lo hay).
        private void Arrancar()
        {
            if (sonando || Time.unscaledTime < proximaRevision)
            {
                return;
            }
            proximaRevision = Time.unscaledTime + SegundosEntreRevisiones;
            escucha = BuscarEscucha();
            if (escucha == null)
            {
                return;
            }
            sonando = true;
            lluvia = CrearCapa("Lluvia", clipLluvia);
            neon = CrearCapa("Neon", clipNeon);
            trafico = CrearCapa("Trafico", clipTrafico);
            musica = CrearCapa("Musica", clipMusica);
            lluvia.Play();
            neon.Play();
            trafico.Play();
            musica.Play();
        }

        // Un AudioListener que cuenta para Unity: activo y con el componente habilitado.
        private static AudioListener BuscarEscucha()
        {
            foreach (AudioListener candidato in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            {
                if (candidato.isActiveAndEnabled)
                {
                    return candidato;
                }
            }
            return null;
        }

        private void Detener()
        {
            sonando = false;
            foreach (AudioSource fuente in new[] { lluvia, neon, trafico, musica })
            {
                if (fuente != null)
                {
                    fuente.Stop();
                    Destroy(fuente.gameObject);
                }
            }
        }

        private void OnDisable()
        {
            if (sonando)
            {
                Detener();
            }
        }

        private bool EnInterior()
        {
            if (zonas == null)
            {
                return false;
            }
            foreach (ZonaInterior zona in zonas)
            {
                if (zona != null && zona.JugadorDentro)
                {
                    return true;
                }
            }
            return false;
        }

        private AudioSource CrearCapa(string nombre, AudioClip clip)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(transform, false);
            AudioSource fuente = go.AddComponent<AudioSource>();
            fuente.clip = clip;
            fuente.loop = true;
            fuente.spatialBlend = 0f;
            fuente.volume = 0f;
            fuente.playOnAwake = false;
            return fuente;
        }
    }
}

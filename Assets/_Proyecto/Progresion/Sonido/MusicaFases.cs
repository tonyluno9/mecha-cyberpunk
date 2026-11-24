using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>
    /// T-musica-24nov: la música del juego según <see cref="FaseJuego"/>: piano en la parte guiada y las cinemáticas, la del campo de tiro, SKULLS,
    /// policía y NOVA en el modo libre y otra vez el piano en HAS MUERTO. Cruza dos fuentes en <see cref="ReglaMusicaFases.SegundosCruce"/> s,
    /// nunca vuelve a una fase anterior mientras el jugador vive (<see cref="ReglaMusicaFases.Elegir"/>), baja en interiores del modo libre y
    /// ~-10 dB con <see cref="DuckingNarracion.Activo"/>. No suena con el tema del menú ni fuera del juego. Va por el grupo Música del mezclador.
    /// Se crea sola (una vez, sobrevive a las escenas) en cuanto hay un gestor de sonidos; los clips salen del catálogo (ImportarMusicaFases).
    /// </summary>
    public class MusicaFases : MonoBehaviour
    {
        public const string NombreObjeto = "MusicaFases";
        /// <summary>Lo más que avanza un fundido en un cuadro (s), aunque el cuadro dure más (carga de escena).</summary>
        public const float PasoMaximo = 0.05f;

        private static MusicaFases actual;

        private readonly AudioSource[] fuentes = new AudioSource[2];
        private readonly float[] niveles = new float[2];
        private readonly string[] pistas = new string[2];
        private readonly List<IdLugar> lugares = new List<IdLugar>();
        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private readonly HashSet<string> avisados = new HashSet<string>();
        private int activa;
        private bool enJuego;
        private FaseJuego.Fase faseSonando;
        private float factor = 1f;

        public static MusicaFases Actual => actual;
        /// <summary>La pista que está entrando o sonando (null si nada).</summary>
        public string PistaActual => enJuego ? pistas[activa] : null;
        /// <summary>La fase que suena ahora.</summary>
        public FaseJuego.Fase FaseSonando => faseSonando;
        public float Factor => factor;
        public AudioSource FuenteActiva => fuentes[activa];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => actual = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AlArrancar()
        {
            SceneManager.sceneLoaded -= AlCargarEscena;
            SceneManager.sceneLoaded += AlCargarEscena;
            Asegurar();
        }

        private static void AlCargarEscena(Scene escena, LoadSceneMode modo) => Asegurar();

        /// <summary>Crea el objeto si hay gestor de sonidos y aún no existe (lo usan el arranque y las pruebas).</summary>
        public static MusicaFases Asegurar()
        {
            if (actual != null || Registro<ISonidos>.Actual == null)
            {
                return actual;
            }
            GameObject go = new GameObject(NombreObjeto);
            DontDestroyOnLoad(go);
            actual = go.AddComponent<MusicaFases>();
            return actual;
        }

        private void Awake()
        {
            for (int i = 0; i < fuentes.Length; i++)
            {
                AudioSource f = gameObject.AddComponent<AudioSource>();
                f.playOnAwake = false;
                f.loop = true;
                f.spatialBlend = 0f;
                f.priority = 0;
                f.volume = 0f;
                // HAS MUERTO y la pausa pueden pausar el AudioListener: la música sigue.
                f.ignoreListenerPause = true;
                fuentes[i] = f;
            }
        }

        private void OnEnable()
        {
            EventosLugares.JugadorEntro += AlEntrar;
            EventosLugares.JugadorSalio += AlSalir;
        }

        private void OnDisable()
        {
            EventosLugares.JugadorEntro -= AlEntrar;
            EventosLugares.JugadorSalio -= AlSalir;
        }

        private void OnDestroy()
        {
            if (actual == this)
            {
                actual = null;
            }
        }

        private void AlEntrar(IdLugar nuevo)
        {
            lugares.Remove(nuevo);
            if (ReglaMusicaFases.EsInterior(nuevo))
            {
                lugares.Add(nuevo);
            }
        }

        private void AlSalir(IdLugar anterior) => lugares.Remove(anterior);

        private IdLugar Lugar => lugares.Count > 0 ? lugares[lugares.Count - 1] : IdLugar.Calle;

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, PasoMaximo);
            bool jugando = !MusicaMenu.Sonando && (Registro<IJugador>.Actual != null || CinematicaEnCurso.Activa);
            if (!jugando)
            {
                if (enJuego)
                {
                    enJuego = false;
                    lugares.Clear();
                }
                Fundir(-1, dt);
                return;
            }
            FaseJuego.Fase pedida = FaseJuego.Actual;
            faseSonando = enJuego ? ReglaMusicaFases.Elegir(faseSonando, pedida) : pedida;
            enJuego = true;
            string pista = ReglaMusicaFases.PistaDe(faseSonando);
            if (pista != pistas[activa] || !fuentes[activa].isPlaying && niveles[activa] <= 0f)
            {
                Empezar(pista);
            }
            float meta = ReglaMusicaFases.FactorInterior(Lugar, faseSonando) * ReglaMusicaFases.FactorNarracion(DuckingNarracion.Activo);
            factor = ReglaMusicaMenu.Fundir(factor, meta, dt, ReglaMusicaFases.SegundosAjuste);
            Fundir(activa, dt);
        }

        private void Empezar(string pista)
        {
            AudioClip clip = Clip(pista);
            if (clip == null)
            {
                pistas[activa] = pista; // no se reintenta cada cuadro
                return;
            }
            if (pistas[activa] != pista)
            {
                activa = 1 - activa;
            }
            AudioSource f = fuentes[activa];
            pistas[activa] = pista;
            f.outputAudioMixerGroup = MezclaAudio.Grupo(SeccionSonido.Musica);
            if (f.clip != clip || !f.isPlaying)
            {
                f.clip = clip;
                f.time = 0f;
                f.Play();
            }
        }

        // Sube la fuente "subir" (-1: ninguna) y baja las demás; detiene las que llegan a 0.
        private void Fundir(int subir, float dt)
        {
            float ganancia = Ganancia();
            for (int i = 0; i < fuentes.Length; i++)
            {
                float objetivo = i == subir && fuentes[i].clip != null ? 1f : 0f;
                niveles[i] = ReglaMusicaMenu.Fundir(niveles[i], objetivo, dt, ReglaMusicaFases.SegundosCruce);
                if (niveles[i] <= 0f && objetivo <= 0f)
                {
                    if (fuentes[i].isPlaying)
                    {
                        fuentes[i].Stop();
                    }
                    continue;
                }
                fuentes[i].volume = niveles[i] * ganancia * factor;
            }
        }

        private AudioClip Clip(string id)
        {
            if (clips.TryGetValue(id, out AudioClip c) && c != null)
            {
                return c;
            }
            if (avisados.Contains(id))
            {
                return null; // ya se buscó y no está: no se recorre el catálogo cada cuadro
            }
            GestorSonidos gestor = Registro<ISonidos>.Actual as GestorSonidos;
            CatalogoSonidos catalogo = gestor != null ? gestor.Catalogo : null;
            if (catalogo != null)
            {
                foreach (CatalogoSonidos.Entrada e in catalogo.Entradas)
                {
                    if (e != null && e.id == id && e.finales != null && e.finales.Length > 0)
                    {
                        clips[id] = e.finales[0];
                        return e.finales[0];
                    }
                }
            }
            if (avisados.Add(id))
            {
                Debug.LogWarning("[Sonido] No hay " + id + " en el catálogo (correr ImportarMusicaFases.Aplicar): esa fase queda sin música.");
            }
            return null;
        }

        private static float Ganancia()
        {
            GestorSonidos gestor = Registro<ISonidos>.Actual as GestorSonidos;
            return gestor != null && gestor.Catalogo != null ? gestor.Catalogo.Ganancia(SeccionSonido.Musica) : 1f;
        }
    }
}

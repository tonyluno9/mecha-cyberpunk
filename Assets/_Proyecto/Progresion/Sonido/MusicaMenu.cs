using System.Linq;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>
    /// El tema del menú principal (MUS_MENU_PRINCIPAL, grupo Música del AudioMixer). Sobrevive a los cambios de escena para no
    /// reiniciarse; el menú llama a <see cref="Sonar"/> (entra con fundido) y la pantalla de carga a <see cref="Apagar"/> (baja con
    /// fundido y se detiene antes de que empiece el juego). El archivo no empalma limpio, así que el bucle se hace con dos fuentes y un
    /// cruce suave al final en vez de loop. Usa tiempo sin escala (la pausa no lo detiene).
    /// </summary>
    public class MusicaMenu : MonoBehaviour
    {
        public const string Id = "MUS_MENU_PRINCIPAL";
        /// <summary>Fundido de entrada al abrir el juego o al volver al menú (s).</summary>
        public const float SegundosEntrada = 1.5f;
        /// <summary>Fundido de salida al empezar a cargar (s); menos que el mínimo de la pantalla de carga, así se apaga antes del juego.</summary>
        public const float SegundosSalida = 1.6f;
        /// <summary>Cruce entre el final y el inicio del tema (s).</summary>
        public const float SegundosCruce = 2.5f;

        private static MusicaMenu actual;

        private AudioSource[] fuentes;
        private int activa;
        private float nivel;
        private float objetivo;
        private float segundosFundido = SegundosEntrada;
        private float cruce = 1f;
        private bool cruzando;

        public static MusicaMenu Actual => actual;
        /// <summary>True si suena o está entrando (no si está apagada o bajando).</summary>
        public static bool Sonando => actual != null && actual.objetivo > 0f;
        public float Nivel => nivel;
        public AudioSource FuenteActiva => fuentes != null ? fuentes[activa] : null;
        public int Vueltas { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => actual = null;

        /// <summary>Que suene el tema: si ya sonaba sigue sin reiniciarse; si no, entra desde el principio con fundido.</summary>
        public static void Sonar()
        {
            if (actual == null)
            {
                AudioClip clip = Clip();
                if (clip == null)
                {
                    Debug.LogWarning("[Sonido] No hay " + Id + " en el catálogo: el menú queda sin música.");
                    return;
                }
                GameObject go = new GameObject("MusicaMenu");
                DontDestroyOnLoad(go);
                actual = go.AddComponent<MusicaMenu>();
                actual.Armar(clip);
            }
            actual.Encender();
        }

        /// <summary>Baja el tema con fundido y lo detiene (al empezar una carga).</summary>
        public static void Apagar(float segundos = SegundosSalida)
        {
            if (actual == null)
            {
                return;
            }
            actual.objetivo = 0f;
            actual.segundosFundido = segundos;
        }

        private static AudioClip Clip()
        {
            GestorSonidos gestor = Registro<ISonidos>.Actual as GestorSonidos;
            CatalogoSonidos catalogo = gestor != null ? gestor.Catalogo : null;
            CatalogoSonidos.Entrada e = catalogo != null ? catalogo.Entradas.FirstOrDefault(x => x != null && x.id == Id) : null;
            return e != null && e.finales != null && e.finales.Length > 0 ? e.finales[0] : null;
        }

        private void Armar(AudioClip clip)
        {
            fuentes = new AudioSource[2];
            for (int i = 0; i < fuentes.Length; i++)
            {
                AudioSource a = gameObject.AddComponent<AudioSource>();
                a.clip = clip;
                a.playOnAwake = false;
                a.loop = false;
                a.spatialBlend = 0f;
                a.ignoreListenerPause = true;
                a.priority = 0;
                a.volume = 0f;
                a.outputAudioMixerGroup = MezclaAudio.Grupo(SeccionSonido.Musica);
                fuentes[i] = a;
            }
        }

        private void Encender()
        {
            objetivo = 1f;
            segundosFundido = SegundosEntrada;
            if (!fuentes[activa].isPlaying)
            {
                cruzando = false;
                cruce = 1f;
                fuentes[activa].time = 0f;
                fuentes[activa].Play();
            }
        }

        private void OnDestroy()
        {
            if (actual == this)
            {
                actual = null;
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            nivel = ReglaMusicaMenu.Fundir(nivel, objetivo, dt, segundosFundido);
            if (nivel <= 0f && objetivo <= 0f)
            {
                foreach (AudioSource a in fuentes) a.Stop();
                cruzando = false;
                return;
            }
            AudioSource principal = fuentes[activa];
            AudioSource otra = fuentes[1 - activa];
            if (!cruzando && principal.isPlaying && ReglaMusicaMenu.TocaCruce(principal.time, principal.clip.length, SegundosCruce))
            {
                cruzando = true;
                cruce = 0f;
                otra.time = 0f;
                otra.Play();
            }
            if (cruzando)
            {
                cruce = ReglaMusicaMenu.Fundir(cruce, 1f, dt, SegundosCruce);
                if (cruce >= 1f || !principal.isPlaying)
                {
                    principal.Stop();
                    activa = 1 - activa;
                    cruzando = false;
                    cruce = 1f;
                    Vueltas++;
                }
            }
            float ganancia = Ganancia();
            // Cruce de igual potencia (seno/coseno): no baja el volumen a la mitad del cruce.
            fuentes[activa].volume = nivel * ganancia * (cruzando ? Mathf.Cos(cruce * Mathf.PI * 0.5f) : 1f);
            fuentes[1 - activa].volume = cruzando ? nivel * ganancia * Mathf.Sin(cruce * Mathf.PI * 0.5f) : 0f;
        }

        private static float Ganancia()
        {
            GestorSonidos gestor = Registro<ISonidos>.Actual as GestorSonidos;
            return gestor != null && gestor.Catalogo != null ? gestor.Catalogo.Ganancia(SeccionSonido.Musica) : 1f;
        }
    }
}

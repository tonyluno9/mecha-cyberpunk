using System.Linq;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>
    /// El tema del menú principal (MUS_MENU_PRINCIPAL, grupo Música del AudioMixer). Sobrevive a los cambios de escena para no
    /// reiniciarse; el menú llama a <see cref="Sonar"/> (entra con fundido) y la pantalla de carga a <see cref="Apagar"/> (baja con
    /// fundido y se detiene antes de que empiece el juego). Tony hizo el tema para empalmar sin clic: se toca con loop normal en una sola
    /// fuente. Usa tiempo sin escala (la pausa no lo detiene).
    /// </summary>
    public class MusicaMenu : MonoBehaviour
    {
        public const string Id = "MUS_MENU_PRINCIPAL";
        /// <summary>Fundido de entrada al abrir el juego o al volver al menú (s).</summary>
        public const float SegundosEntrada = 1.5f;
        /// <summary>Fundido de salida al empezar a cargar (s); menos que el mínimo de la pantalla de carga, así se apaga antes del juego.</summary>
        public const float SegundosSalida = 1.6f;
        /// <summary>Lo más que avanza un fundido en un cuadro (s), aunque el cuadro dure más (carga de escena).</summary>
        public const float PasoMaximo = 0.05f;

        private static MusicaMenu actual;

        private AudioSource fuente;
        private float nivel;
        private float objetivo;
        private float segundosFundido = SegundosEntrada;
        private float anterior;

        public static MusicaMenu Actual => actual;
        /// <summary>True si suena o está entrando (no si está apagada o bajando).</summary>
        public static bool Sonando => actual != null && actual.objetivo > 0f;
        public float Nivel => nivel;
        public AudioSource FuenteActiva => fuente;
        /// <summary>Segundos que lleva el tema en esta vuelta (posición del audio).</summary>
        public float TiempoPista => fuente != null ? fuente.time : 0f;
        /// <summary>Cuántas veces volvió a empezar el bucle (la posición regresó al inicio).</summary>
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
            fuente = gameObject.AddComponent<AudioSource>();
            fuente.clip = clip;
            fuente.playOnAwake = false;
            fuente.loop = true;
            fuente.spatialBlend = 0f;
            fuente.ignoreListenerPause = true;
            fuente.priority = 0;
            fuente.volume = 0f;
            fuente.outputAudioMixerGroup = MezclaAudio.Grupo(SeccionSonido.Musica);
        }

        private void Encender()
        {
            objetivo = 1f;
            segundosFundido = SegundosEntrada;
            if (!fuente.isPlaying)
            {
                fuente.time = 0f;
                fuente.Play();
                anterior = 0f;
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
            // Al cargar escenas hay cuadros de varios segundos: el fundido avanza como mucho un paso por cuadro para no saltarse.
            float dt = Mathf.Min(Time.unscaledDeltaTime, PasoMaximo);
            nivel = ReglaMusicaMenu.Fundir(nivel, objetivo, dt, segundosFundido);
            if (nivel <= 0f && objetivo <= 0f)
            {
                fuente.Stop();
                return;
            }
            if (fuente.isPlaying)
            {
                if (fuente.time + 1f < anterior)
                {
                    Vueltas++;
                }
                anterior = fuente.time;
            }
            fuente.volume = nivel * Ganancia();
        }

        private static float Ganancia()
        {
            GestorSonidos gestor = Registro<ISonidos>.Actual as GestorSonidos;
            return gestor != null && gestor.Catalogo != null ? gestor.Catalogo.Ganancia(SeccionSonido.Musica) : 1f;
        }
    }
}

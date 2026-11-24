using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T-1625 (T10): una cinemática de la historia. Mientras no tenga <see cref="Video"/>, sale la pantalla negra de "cinemática pendiente"
    /// (nombre, una línea de texto y una barra) y dura exactamente <see cref="Duracion"/>. Con el VideoClip puesto se reproduce el video a pantalla
    /// completa con su audio, sin tocar código: solo se arrastra el clip al asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Datos de cinemática", fileName = "DatosCinematica_Nueva")]
    public class DatosCinematica : ScriptableObject
    {
        [Tooltip("Id estable: C01 a C09.")]
        [SerializeField] private string id = "C00";
        [Tooltip("Nombre en mayúsculas que sale en pantalla: CINEMÁTICA · <NOMBRE>.")]
        [SerializeField] private string nombre = "SIN NOMBRE";
        [Tooltip("Lo que pasa en la escena, una línea.")]
        [TextArea(2, 5)] [SerializeField] private string texto = string.Empty;
        [Tooltip("Duración prevista del video (s). La pantalla pendiente dura exactamente esto.")]
        [Min(0.5f)] [SerializeField] private float duracion = 5f;
        [Tooltip("Opcional: el video real. Si está, se reproduce en vez de la pantalla negra y dura lo que dura el clip.")]
        [SerializeField] private VideoClip video;
        [Tooltip("T-cinematicas-videos-23nov: los clips en orden, uno tras otro sin negro entre ellos. Si la lista tiene clips, manda sobre Video.")]
        [SerializeField] private VideoClip[] videos = new VideoClip[0];
        [Tooltip("T-cinematicas-narracion-24nov: la narración de cada clip de Videos, en el mismo orden (vacío o null = ese clip sin voz). El clip y su frase empiezan juntos.")]
        [SerializeField] private AudioClip[] narraciones = new AudioClip[0];
        [Tooltip("Segundos de pausa en el último cuadro tras cada clip con narración, en el mismo orden (s). Si falta, 1 s. Los clips sin narración se unen sin pausa.")]
        [SerializeField] private float[] pausas = new float[0];
        [Tooltip("Segundos de pantalla pendiente después de los videos (la parte que aún no tiene video). 0 = sin parte pendiente.")]
        [Min(0f)] [SerializeField] private float segundosPendientes;
        [Tooltip("La línea de la parte pendiente.")]
        [TextArea(2, 5)] [SerializeField] private string textoPendiente = string.Empty;

        public string Id => id;
        public string Nombre => nombre;
        public string Texto => texto;
        public float Duracion => duracion;
        /// <summary>El primer clip (o null si no hay ninguno).</summary>
        public VideoClip Video => Clips.Count > 0 ? Clips[0] : null;

        /// <summary>Los clips en orden: la lista si tiene alguno; si no, el clip suelto; si no, vacía.</summary>
        public IReadOnlyList<VideoClip> Clips
        {
            get
            {
                var lista = new List<VideoClip>();
                if (videos != null)
                {
                    foreach (VideoClip c in videos)
                    {
                        if (c != null) lista.Add(c);
                    }
                }
                if (lista.Count == 0 && video != null) lista.Add(video);
                return lista;
            }
        }

        /// <summary>
        /// T-cinematicas-narracion-24nov: la narración del clip <paramref name="indice"/> de <see cref="Clips"/> (null si ese clip no tiene voz).
        /// </summary>
        public AudioClip Narracion(int indice)
        {
            int i = IndiceEnLista(indice);
            return i >= 0 && narraciones != null && i < narraciones.Length ? narraciones[i] : null;
        }

        /// <summary>Pausa tras el clip <paramref name="indice"/> (s): la de los datos (1 s si falta) si tiene narración; 0 si no tiene.</summary>
        public float Pausa(int indice)
        {
            if (Narracion(indice) == null)
            {
                return 0f;
            }
            int i = IndiceEnLista(indice);
            return i >= 0 && pausas != null && i < pausas.Length ? Mathf.Max(0f, pausas[i]) : ReglaNarracion.PausaPorDefecto;
        }

        /// <summary>Segundos que ocupa el clip <paramref name="indice"/> con la regla de la narración (video, congelado y pausa).</summary>
        public float TramoClip(int indice)
        {
            IReadOnlyList<VideoClip> lista = Clips;
            if (indice < 0 || indice >= lista.Count)
            {
                return 0f;
            }
            AudioClip voz = Narracion(indice);
            return ReglaNarracion.Tramo((float)lista[indice].length, voz != null ? voz.length : 0f, Pausa(indice));
        }

        // El índice de Clips (sin los null) en el arreglo videos (donde van las narraciones en paralelo). -1 si no hay lista.
        private int IndiceEnLista(int indiceClip)
        {
            if (videos == null || indiceClip < 0)
            {
                return -1;
            }
            int n = 0;
            for (int i = 0; i < videos.Length; i++)
            {
                if (videos[i] == null)
                {
                    continue;
                }
                if (n == indiceClip)
                {
                    return i;
                }
                n++;
            }
            return -1;
        }

        /// <summary>Segundos de pantalla pendiente tras los videos (solo cuenta si hay videos).</summary>
        public float SegundosPendientes => segundosPendientes;

        /// <summary>La línea de la parte pendiente (si está vacía se usa <see cref="Texto"/>).</summary>
        public string TextoPendiente => string.IsNullOrEmpty(textoPendiente) ? texto : textoPendiente;

        /// <summary>Segundos de los videos sumados.</summary>
        public float DuracionVideos
        {
            get
            {
                float total = 0f;
                foreach (VideoClip c in Clips) total += (float)c.length;
                return total;
            }
        }

        /// <summary>Segundos de los clips con la regla de la narración (cada clip: máx(video, voz) + pausa si tiene voz).</summary>
        public float DuracionConNarracion
        {
            get
            {
                float total = 0f;
                int n = Clips.Count;
                for (int i = 0; i < n; i++) total += TramoClip(i);
                return total;
            }
        }

        /// <summary>Segundos que dura el contenido: los clips con su narración (más la parte pendiente) si hay video, si no la duración de los datos.</summary>
        public float DuracionEfectiva => Clips.Count > 0 && DuracionVideos > 0.1f ? DuracionConNarracion + segundosPendientes : duracion;

        /// <summary>Pone los clips en orden y la parte pendiente (editor y pruebas).</summary>
        public void ConfigurarVideos(VideoClip[] clips, float pendientes, string lineaPendiente)
        {
            videos = clips ?? new VideoClip[0];
            video = null;
            narraciones = new AudioClip[0];
            pausas = new float[0];
            segundosPendientes = Mathf.Max(0f, pendientes);
            textoPendiente = lineaPendiente ?? string.Empty;
        }

        /// <summary>T-cinematicas-narracion-24nov: la narración y la pausa de cada clip de la lista, en el mismo orden (editor y pruebas).</summary>
        public void ConfigurarNarracion(AudioClip[] voces, float[] pausasClip)
        {
            narraciones = voces ?? new AudioClip[0];
            pausas = pausasClip ?? new float[0];
        }

        /// <summary>Para crear los datos desde código (editor y pruebas).</summary>
        public void Configurar(string nuevoId, string nuevoNombre, string nuevoTexto, float nuevaDuracion)
        {
            id = nuevoId;
            nombre = nuevoNombre;
            texto = nuevoTexto;
            duracion = Mathf.Max(0.5f, nuevaDuracion);
        }
    }
}

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

        /// <summary>Segundos que dura el contenido: los clips (más la parte pendiente) si hay video, si no la duración de los datos.</summary>
        public float DuracionEfectiva => Clips.Count > 0 && DuracionVideos > 0.1f ? DuracionVideos + segundosPendientes : duracion;

        /// <summary>Pone los clips en orden y la parte pendiente (editor y pruebas).</summary>
        public void ConfigurarVideos(VideoClip[] clips, float pendientes, string lineaPendiente)
        {
            videos = clips ?? new VideoClip[0];
            video = null;
            segundosPendientes = Mathf.Max(0f, pendientes);
            textoPendiente = lineaPendiente ?? string.Empty;
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

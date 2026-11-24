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
        [Tooltip("T97: imagen fija de cada toma, en paralelo con Videos (mismo índice). Una toma sin video y con imagen se muestra fija (Ken Burns) lo que dura su narración + la pausa; sin narración 3 s.")]
        [SerializeField] private Texture2D[] fotos = new Texture2D[0];
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

        /// <summary>
        /// Las tomas en orden, una entrada por toma: su VideoClip, o null si la toma es una imagen fija (T97, ver <see cref="Foto"/>).
        /// La lista si tiene alguna toma; si no, el clip suelto; si no, vacía. Count = número de tomas.
        /// </summary>
        public IReadOnlyList<VideoClip> Clips
        {
            get
            {
                var lista = new List<VideoClip>();
                int n = CantidadEnLista;
                for (int i = 0; i < n; i++)
                {
                    if (EsToma(i)) lista.Add(VideoEn(i));
                }
                if (lista.Count == 0 && video != null) lista.Add(video);
                return lista;
            }
        }

        /// <summary>T97: la imagen fija de la toma <paramref name="indice"/> de <see cref="Clips"/> (null si esa toma es video).</summary>
        public Texture2D Foto(int indice)
        {
            int i = IndiceEnLista(indice);
            return i >= 0 && VideoEn(i) == null ? FotoEn(i) : null;
        }

        /// <summary>T97: true si la toma <paramref name="indice"/> es una imagen fija.</summary>
        public bool EsFoto(int indice) => Foto(indice) != null;

        /// <summary>T97: cuántas tomas son imagen fija.</summary>
        public int CantidadFotos
        {
            get
            {
                int total = 0;
                int n = Clips.Count;
                for (int i = 0; i < n; i++)
                {
                    if (EsFoto(i)) total++;
                }
                return total;
            }
        }

        // Largo de las listas paralelas (videos y fotos pueden tener largos distintos).
        private int CantidadEnLista => Mathf.Max(videos != null ? videos.Length : 0, fotos != null ? fotos.Length : 0);
        private VideoClip VideoEn(int i) => videos != null && i < videos.Length ? videos[i] : null;
        private Texture2D FotoEn(int i) => fotos != null && i < fotos.Length ? fotos[i] : null;
        private bool EsToma(int i) => VideoEn(i) != null || FotoEn(i) != null;

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

        /// <summary>
        /// Segundos que ocupa el clip <paramref name="indice"/> con la regla de la narración (video, congelado y pausa). T97: si la toma es una
        /// imagen fija, lo de <see cref="ReglaFotoCinematica.Duracion(float, float)"/> (narración + pausa; sin narración 3 s).
        /// </summary>
        public float TramoClip(int indice)
        {
            IReadOnlyList<VideoClip> lista = Clips;
            if (indice < 0 || indice >= lista.Count)
            {
                return 0f;
            }
            AudioClip voz = Narracion(indice);
            float segundosVoz = voz != null ? voz.length : 0f;
            if (lista[indice] == null)
            {
                return EsFoto(indice) ? ReglaFotoCinematica.Duracion(segundosVoz, Pausa(indice)) : 0f;
            }
            return ReglaNarracion.Tramo((float)lista[indice].length, segundosVoz, Pausa(indice));
        }

        // El índice de Clips (las tomas con video o imagen) en los arreglos paralelos (videos, fotos, narraciones, pausas). -1 si no hay lista.
        private int IndiceEnLista(int indiceClip)
        {
            if (indiceClip < 0)
            {
                return -1;
            }
            int n = 0;
            int largo = CantidadEnLista;
            for (int i = 0; i < largo; i++)
            {
                if (!EsToma(i))
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
                foreach (VideoClip c in Clips)
                {
                    if (c != null) total += (float)c.length;
                }
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

        /// <summary>
        /// Segundos que dura el contenido: las tomas (video o imagen fija) con su narración, más la parte pendiente, si hay tomas; si no, la
        /// duración de los datos.
        /// </summary>
        public float DuracionEfectiva
        {
            get
            {
                if (Clips.Count == 0)
                {
                    return duracion;
                }
                float tomas = DuracionConNarracion;
                return tomas > 0.1f ? tomas + segundosPendientes : duracion;
            }
        }

        /// <summary>Pone los clips en orden y la parte pendiente (editor y pruebas).</summary>
        public void ConfigurarVideos(VideoClip[] clips, float pendientes, string lineaPendiente)
        {
            videos = clips ?? new VideoClip[0];
            fotos = new Texture2D[0];
            video = null;
            narraciones = new AudioClip[0];
            pausas = new float[0];
            segundosPendientes = Mathf.Max(0f, pendientes);
            textoPendiente = lineaPendiente ?? string.Empty;
        }

        /// <summary>
        /// T97: pone las tomas en orden (editor y pruebas). <paramref name="clips"/> e <paramref name="imagenes"/> van en paralelo: en cada
        /// índice, un video o una imagen fija (si están los dos, manda el video). Borra la narración: después va <see cref="ConfigurarNarracion"/>.
        /// </summary>
        public void ConfigurarTomas(VideoClip[] clips, Texture2D[] imagenes, float pendientes, string lineaPendiente)
        {
            ConfigurarVideos(clips, pendientes, lineaPendiente);
            fotos = imagenes ?? new Texture2D[0];
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

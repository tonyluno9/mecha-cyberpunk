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

        public string Id => id;
        public string Nombre => nombre;
        public string Texto => texto;
        public float Duracion => duracion;
        public VideoClip Video => video;

        /// <summary>Segundos que dura el contenido: el clip si hay video, si no la duración de los datos.</summary>
        public float DuracionEfectiva => video != null && video.length > 0.1 ? (float)video.length : duracion;

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

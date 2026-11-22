using UnityEngine.Audio;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// El AudioMixer del juego (grupos Master, Musica, Efectos y Voces). Cualquier AudioSource que no pase por <see cref="Sonidos"/>
    /// toma de aquí su grupo, para que los deslizadores de volumen lo controlen. Se obtiene con <c>Registro&lt;IMezclaAudio&gt;.Actual</c>.
    /// </summary>
    public interface IMezclaAudio
    {
        /// <summary>El grupo del mezclador de una sección (Ambiente e Interfaz van por Efectos). Null si no hay mezclador.</summary>
        AudioMixerGroup Grupo(SeccionSonido seccion);
    }
}

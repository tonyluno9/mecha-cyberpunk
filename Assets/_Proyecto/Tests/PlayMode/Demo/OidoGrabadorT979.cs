using UnityEngine;

namespace MechaCyberpunk.Tests.Demo
{
    /// <summary>T-979: va junto al AudioListener; pasa la mezcla final al grabador mientras graba (hilo de audio).</summary>
    public class OidoGrabadorT979 : MonoBehaviour
    {
        public static int Llamadas;

        private void OnAudioFilterRead(float[] datos, int canales)
        {
            Llamadas++;
            if (GrabadorVideoT979.Grabando)
            {
                GrabadorVideoT979.Sonido.Enqueue((float[])datos.Clone());
            }
        }
    }
}

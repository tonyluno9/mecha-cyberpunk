using UnityEngine;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T-cinematicas-narracion-24nov: lógica pura de un clip de cinemática con narración. El video y la frase empiezan juntos; si el video acaba
    /// antes, su último cuadro se queda congelado hasta que acaba la frase; con los dos acabados va la pausa y luego el clip siguiente.
    /// Nunca se acelera el video ni se corta la voz. Sin narración, el clip acaba cuando acaba el video (sin pausa). El tiempo entra como parámetro.
    /// </summary>
    public sealed class ReglaNarracion
    {
        /// <summary>Pausa en el último cuadro tras una frase, si los datos no dicen otra (s).</summary>
        public const float PausaPorDefecto = 1f;
        /// <summary>Fundido de la voz al saltar la cinemática (s).</summary>
        public const float SegundosFundidoVoz = 0.2f;
        /// <summary>Volumen de los efectos del video mientras suena la narración (~-6 dB).</summary>
        public const float VolumenVideoConVoz = 0.5f;

        private readonly float duracionVoz;
        private readonly float pausa;
        private float tiempo;
        private bool videoAcabado;
        private float finAmbos = -1f;

        /// <param name="segundosVoz">Duración de la frase (s); 0 = clip sin narración.</param>
        /// <param name="segundosPausa">Pausa tras acabar video y frase (s); solo cuenta con narración.</param>
        public ReglaNarracion(float segundosVoz, float segundosPausa)
        {
            duracionVoz = Mathf.Max(0f, segundosVoz);
            pausa = duracionVoz > 0f ? Mathf.Max(0f, segundosPausa) : 0f;
        }

        /// <summary>True si el clip tiene narración.</summary>
        public bool ConVoz => duracionVoz > 0f;

        /// <summary>True mientras la frase suena (desde que empieza el clip hasta su duración).</summary>
        public bool VozSonando => ConVoz && tiempo < duracionVoz;

        /// <summary>True si el video acabó y la voz sigue: el último cuadro se queda en pantalla.</summary>
        public bool Congelado => videoAcabado && !Listo;

        /// <summary>True cuando el clip ya puede dar paso al siguiente (video y voz acabados y la pausa cumplida).</summary>
        public bool Listo { get; private set; }

        /// <summary>Avanza <paramref name="dt"/> s desde que empezó el clip. <paramref name="videoTermino"/>: el VideoPlayer llegó al final.</summary>
        public void Avanzar(float dt, bool videoTermino)
        {
            if (Listo)
            {
                return;
            }
            tiempo += Mathf.Max(0f, dt);
            videoAcabado |= videoTermino;
            if (!videoAcabado || tiempo < duracionVoz)
            {
                return;
            }
            if (finAmbos < 0f)
            {
                finAmbos = tiempo;
            }
            Listo = tiempo - finAmbos >= pausa;
        }

        /// <summary>Segundos del tramo de un clip: máx(video, voz) más la pausa si tiene voz.</summary>
        public static float Tramo(float segundosVideo, float segundosVoz, float segundosPausa)
        {
            float video = Mathf.Max(0f, segundosVideo);
            if (segundosVoz <= 0f)
            {
                return video;
            }
            return Mathf.Max(video, segundosVoz) + Mathf.Max(0f, segundosPausa);
        }
    }
}

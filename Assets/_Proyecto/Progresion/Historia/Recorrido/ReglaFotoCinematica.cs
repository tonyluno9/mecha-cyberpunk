using UnityEngine;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T97: lógica pura de una toma de cinemática que es una imagen fija (frame) en vez de video. Dura lo que dura su narración más la pausa
    /// (regla de T-cinematicas-narracion-24nov: 1 s por defecto); sin narración, <see cref="SegundosSinNarracion"/> s. Entra con un fundido
    /// corto (<see cref="SegundosFundido"/> s) desde la toma anterior (nunca negro largo) y se mueve lento tipo Ken Burns: un zoom leve
    /// (<see cref="ZoomKenBurns"/>) con un paneo suave, alternando el sentido de una toma a otra. El tiempo entra como parámetro.
    /// </summary>
    public sealed class ReglaFotoCinematica
    {
        /// <summary>Duración de una imagen fija sin narración (s).</summary>
        public const float SegundosSinNarracion = 3f;
        /// <summary>Fundido cruzado con la toma anterior al empezar (s). Se encima al principio de la toma: no suma tiempo.</summary>
        public const float SegundosFundido = 0.4f;
        /// <summary>Zoom extra del Ken Burns (0.08 = 8 % más cerca al final, o al principio si la toma se aleja).</summary>
        public const float ZoomKenBurns = 0.08f;
        /// <summary>Parte del margen que sobra (por el zoom) que recorre el paneo (0 a 1).</summary>
        public const float RecorridoPaneo = 0.8f;

        private readonly float duracion;
        private float tiempo;

        public ReglaFotoCinematica(float segundos)
        {
            duracion = Mathf.Max(0f, segundos);
        }

        /// <summary>Segundos que dura la toma.</summary>
        public float Segundos => duracion;

        /// <summary>Segundos desde que empezó la toma.</summary>
        public float Tiempo => tiempo;

        /// <summary>True cuando la toma ya puede dar paso a la siguiente.</summary>
        public bool Listo => tiempo >= duracion;

        /// <summary>0 a 1 de la toma (para el Ken Burns).</summary>
        public float Avance => duracion <= 0f ? 1f : Mathf.Clamp01(tiempo / duracion);

        /// <summary>Opacidad de esta toma sobre la anterior: 0 a 1 en los primeros <see cref="SegundosFundido"/> s.</summary>
        public float AlfaEntrada => Mathf.Clamp01(tiempo / SegundosFundido);

        public void Avanzar(float dt)
        {
            tiempo += Mathf.Max(0f, dt);
        }

        /// <summary>Segundos de una imagen fija: narración + pausa si tiene voz; si no, <see cref="SegundosSinNarracion"/>.</summary>
        public static float Duracion(float segundosVoz, float segundosPausa)
        {
            return segundosVoz > 0f ? segundosVoz + Mathf.Max(0f, segundosPausa) : SegundosSinNarracion;
        }

        /// <summary>
        /// La parte de la imagen que se ve (UV 0 a 1, origen abajo a la izquierda) en el momento <paramref name="avance"/> (0 a 1) de la toma
        /// <paramref name="indiceToma"/>. Llena el destino sin deformar (recorta lo que sobra si los aspectos no coinciden). Las tomas pares
        /// se acercan y las impares se alejan; el paneo cambia de sentido cada toma.
        /// </summary>
        public static Rect Encuadre(float avance, int indiceToma, float aspectoImagen, float aspectoDestino)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(avance));
            float ancho = 1f;
            float alto = 1f;
            if (aspectoImagen > 0f && aspectoDestino > 0f)
            {
                if (aspectoImagen > aspectoDestino)
                {
                    ancho = aspectoDestino / aspectoImagen;
                }
                else
                {
                    alto = aspectoImagen / aspectoDestino;
                }
            }
            bool acerca = indiceToma % 2 == 0;
            float zoom = 1f + ZoomKenBurns * (acerca ? t : 1f - t);
            ancho /= zoom;
            alto /= zoom;
            int fase = ((indiceToma % 4) + 4) % 4;
            float sentidoX = fase < 2 ? 1f : -1f;
            float sentidoY = fase == 1 || fase == 2 ? 1f : -1f;
            float centroX = 0.5f + sentidoX * (t - 0.5f) * (1f - ancho) * RecorridoPaneo;
            float centroY = 0.5f + sentidoY * (t - 0.5f) * (1f - alto) * RecorridoPaneo * 0.5f;
            return new Rect(centroX - ancho * 0.5f, centroY - alto * 0.5f, ancho, alto);
        }
    }
}

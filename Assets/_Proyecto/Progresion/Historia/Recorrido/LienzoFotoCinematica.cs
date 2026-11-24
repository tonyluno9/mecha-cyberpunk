using UnityEngine;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T97: compone una toma de imagen fija en una RenderTexture (el "lienzo" de la cinemática): la toma anterior abajo (ya compuesta, a
    /// pantalla completa) y la imagen encima con su encuadre (Ken Burns, de <see cref="ReglaFotoCinematica.Encuadre"/>) y su opacidad
    /// (el fundido cruzado). Sin anterior, el fondo es negro. Una sola textura sirve igual para la pantalla de PC (RawImage) y para la
    /// pantalla grande de VR. Usa Graphics.DrawTexture (sin shaders propios).
    /// </summary>
    public static class LienzoFotoCinematica
    {
        /// <summary>Tamaño del lienzo (px): 16:9, como los videos de las cinemáticas.</summary>
        public const int Ancho = 1920;
        public const int Alto = 1080;
        public const float Aspecto = (float)Ancho / Alto;

        // Graphics.DrawTexture: el color neutro (que no cambia la imagen) es 0.5 en cada canal, alfa incluido.
        private const float Neutro = 0.5f;

        /// <summary>
        /// Dibuja en <paramref name="destino"/>: negro, luego <paramref name="anterior"/> entera (si hay) y encima <paramref name="foto"/>
        /// (la parte <paramref name="uvFoto"/>, UV con origen abajo a la izquierda) con opacidad <paramref name="alfaFoto"/> (0 a 1).
        /// </summary>
        public static void Componer(RenderTexture destino, Texture anterior, Texture foto, Rect uvFoto, float alfaFoto)
        {
            if (destino == null)
            {
                return;
            }
            RenderTexture previa = RenderTexture.active;
            RenderTexture.active = destino;
            GL.PushMatrix();
            GL.LoadPixelMatrix(0f, destino.width, destino.height, 0f);
            GL.Clear(true, true, Color.black);
            var todo = new Rect(0f, 0f, destino.width, destino.height);
            float alfa = Mathf.Clamp01(alfaFoto);
            if (anterior != null && alfa < 1f)
            {
                Graphics.DrawTexture(todo, anterior, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, new Color(Neutro, Neutro, Neutro, Neutro));
            }
            if (foto != null && alfa > 0f)
            {
                Graphics.DrawTexture(todo, foto, uvFoto, 0, 0, 0, 0, new Color(Neutro, Neutro, Neutro, Neutro * alfa));
            }
            GL.PopMatrix();
            RenderTexture.active = previa;
        }
    }
}

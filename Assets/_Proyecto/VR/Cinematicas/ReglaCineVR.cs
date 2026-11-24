using UnityEngine.Video;

namespace MechaCyberpunk.VR.Cinematicas
{
    /// <summary>
    /// V12-B, lógica pura: qué cámaras y qué videos son "de cinemática" en VR. Esas no van al visor (la cabeza del jugador nunca la mueve
    /// una animación): se dibujan en una textura y se ven en <see cref="Menus.PantallaCineVR"/>.
    /// </summary>
    public static class ReglaCineVR
    {
        /// <summary>
        /// True si una cámara prendida es de cinemática:
        /// <list type="bullet">
        /// <item>nunca la cabeza (la del visor o la del jugador), una excluida, una que ya dibuja en su propia textura ni una Overlay de URP;</item>
        /// <item>las forzadas (el flashback) siempre;</item>
        /// <item>con el jugador VR armado, la que en PC taparía su vista: profundidad igual o mayor que la de su cámara (silla del Doc, asalto...).</item>
        /// </list>
        /// Sin jugador (rig de menús: Inicio, Menu_Calles, creación) la cámara de la escena no es cinemática: es el fondo del menú.
        /// </summary>
        public static bool EsCamaraDeCine(bool esCabeza, bool excluida, bool tieneTextura, bool esBase, bool hayJugadorVR, bool forzada,
            float profundidad, float profundidadCabeza)
        {
            if (esCabeza || excluida || tieneTextura || !esBase)
            {
                return false;
            }
            if (forzada)
            {
                return true;
            }
            return hayJugadorVR && profundidad >= profundidadCabeza;
        }

        /// <summary>True si el video se dibuja sobre una cámara (plano cercano o lejano): en VR se pasaría pegado a la cara; va a una textura.</summary>
        public static bool DibujaEnCamara(VideoRenderMode modo)
        {
            return modo == VideoRenderMode.CameraNearPlane || modo == VideoRenderMode.CameraFarPlane;
        }

        /// <summary>
        /// True si el video va en la pantalla de cine: con el jugador VR armado, no excluido, reproduciéndose y de pantalla
        /// (redirigido desde una cámara, o su textura la muestra un RawImage de un Canvas de pantalla, como la cinemática con video).
        /// </summary>
        public static bool VideoAlCine(bool hayJugadorVR, bool excluido, bool reproduciendo, bool redirigido, bool enLienzoDePantalla)
        {
            return hayJugadorVR && !excluido && reproduciendo && (redirigido || enLienzoDePantalla);
        }

        /// <summary>VR36: lo más que se queda el último cuadro de un video parado en la pantalla mientras sigue la cinemática (s).</summary>
        public const float SegundosSinCorte = 1f;

        /// <summary>
        /// VR36: true si un video que ya estaba en la pantalla de cine y se paró (fin de un clip, el otro reproductor prepara el siguiente)
        /// se queda con su último cuadro: solo mientras la cinemática sigue y por poco tiempo (después sale lo que la cinemática ponga).
        /// </summary>
        public static bool MantenerEnCine(bool cinematicaEnCurso, bool reproduciendo, float segundosParado)
        {
            return cinematicaEnCurso && !reproduciendo && segundosParado < SegundosSinCorte;
        }
    }
}

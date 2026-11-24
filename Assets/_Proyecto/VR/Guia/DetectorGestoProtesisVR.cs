namespace MechaCyberpunk.VR.Guia
{
    /// <summary>
    /// VR20, lógica pura: la pose con los dos grips apretados se sostiene <see cref="ReglaGestosProtesisVR.SegundosGesto"/> s y entonces
    /// se dispara una vez. No vuelve a dispararse hasta soltar un grip o salir de la pose. Cambiar de pose empieza la cuenta de nuevo.
    /// </summary>
    public sealed class DetectorGestoProtesisVR
    {
        private readonly float segundos;
        private GestoProtesisVR pose;
        private float sostenido;
        private bool disparado;

        public DetectorGestoProtesisVR(float segundosGesto = ReglaGestosProtesisVR.SegundosGesto)
        {
            segundos = segundosGesto > 0f ? segundosGesto : ReglaGestosProtesisVR.SegundosGesto;
        }

        /// <summary>La pose que se está sosteniendo con los grips (Ninguno si no).</summary>
        public GestoProtesisVR Pose => pose;

        /// <summary>De 0 a 1, cuánto falta sostener.</summary>
        public float Avance => pose == GestoProtesisVR.Ninguno ? 0f : sostenido / segundos;

        /// <summary>Avanza el reloj; devuelve la prótesis a activar en este cuadro (Ninguno casi siempre).</summary>
        public GestoProtesisVR Avanzar(GestoProtesisVR poseAhora, bool dosGrips, float dt)
        {
            if (poseAhora == GestoProtesisVR.Ninguno || !dosGrips)
            {
                Reiniciar();
                return GestoProtesisVR.Ninguno;
            }
            if (poseAhora != pose)
            {
                pose = poseAhora;
                sostenido = 0f;
                disparado = false;
            }
            sostenido += dt > 0f ? dt : 0f;
            if (!disparado && sostenido >= segundos)
            {
                disparado = true;
                return pose;
            }
            return GestoProtesisVR.Ninguno;
        }

        public void Reiniciar()
        {
            pose = GestoProtesisVR.Ninguno;
            sostenido = 0f;
            disparado = false;
        }
    }
}

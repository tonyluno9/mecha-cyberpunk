using UnityEngine;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR21: cuándo se cierra suave la vista al manejar en VR (lógica pura). Al girar más de <see cref="GiroMinimo"/> o al acelerar o frenar más de
    /// <see cref="AceleracionMinima"/>, y se sostiene <see cref="SegundosSostener"/> para no parpadear. Con VIÑETA: NO nunca.
    /// </summary>
    public static class ReglaVinetaManejoVR
    {
        /// <summary>Giro del vehículo (°/s, valor absoluto) desde el que se pone la viñeta.</summary>
        public const float GiroMinimo = 25f;
        /// <summary>Aceleración o frenado hacia adelante (m/s², valor absoluto) desde el que se pone la viñeta.</summary>
        public const float AceleracionMinima = 2.5f;
        /// <summary>Cuánto se queda puesta después del último giro o arrancón (s).</summary>
        public const float SegundosSostener = 0.3f;

        /// <summary>Devuelve los segundos que le quedan a la viñeta (más de 0: puesta).</summary>
        public static float Avanzar(float restante, float giroGradosSegundo, float aceleracion, bool permitida, float dt)
        {
            if (!permitida)
            {
                return 0f;
            }
            if (Mathf.Abs(giroGradosSegundo) > GiroMinimo || Mathf.Abs(aceleracion) > AceleracionMinima)
            {
                return SegundosSostener;
            }
            return Mathf.Max(0f, restante - dt);
        }
    }
}

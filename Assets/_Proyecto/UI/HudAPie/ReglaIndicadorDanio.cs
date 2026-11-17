using UnityEngine;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>T-1328: cálculos puros del indicador de dirección de daño.</summary>
    public static class ReglaIndicadorDanio
    {
        /// <summary>
        /// Ángulo (°) de la fuente del daño respecto a hacia dónde mira el jugador, en el plano: 0 enfrente, 90 a la derecha, -90 a la izquierda,
        /// 180 atrás. <paramref name="direccionDelGolpe"/> es la dirección en que viajó el golpe (del atacante al jugador). False si no se puede saber.
        /// </summary>
        public static bool Angulo(Vector3 adelante, Vector3 direccionDelGolpe, out float grados)
        {
            Vector3 fuente = -new Vector3(direccionDelGolpe.x, 0f, direccionDelGolpe.z);
            Vector3 frente = new Vector3(adelante.x, 0f, adelante.z);
            grados = 0f;
            if (fuente.sqrMagnitude < 1e-6f || frente.sqrMagnitude < 1e-6f)
            {
                return false;
            }
            grados = Vector3.SignedAngle(frente, fuente, Vector3.up);
            return true;
        }

        /// <summary>Opacidad de la marca: 1 al nacer y 0 al cumplirse la duración.</summary>
        public static float Alfa(float transcurrido, float duracion)
        {
            return duracion <= 0f ? 0f : Mathf.Clamp01(1f - transcurrido / duracion);
        }
    }
}

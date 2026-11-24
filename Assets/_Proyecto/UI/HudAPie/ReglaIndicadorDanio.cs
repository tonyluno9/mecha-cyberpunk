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

        /// <summary>
        /// T-indicador-24nov: qué orilla destella. <paramref name="direccionDelGolpe"/> es hacia dónde viajó el golpe (del atacante al jugador).
        /// Si <paramref name="puedeSerArriba"/> (disparos y explosiones; no golpes ni caídas) y la fuente está a más de <paramref name="gradosArriba"/>°
        /// sobre el horizonte, Arriba. Si no, por el ángulo en el plano: dentro de <paramref name="gradosFrente"/>° del frente, Frente; dentro de
        /// <paramref name="gradosAtras"/>° de atrás, Atras; si no, Izquierda o Derecha. Ninguna si no se puede saber.
        /// </summary>
        public static OrillaDanio Orilla(Vector3 adelante, Vector3 direccionDelGolpe, bool puedeSerArriba, float gradosArriba, float gradosFrente,
            float gradosAtras)
        {
            Vector3 fuente = -direccionDelGolpe;
            if (fuente.sqrMagnitude < 1e-6f)
            {
                return OrillaDanio.Ninguna;
            }
            float elevacion = Mathf.Asin(Mathf.Clamp(fuente.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
            if (puedeSerArriba && elevacion >= gradosArriba)
            {
                return OrillaDanio.Arriba;
            }
            if (!Angulo(adelante, direccionDelGolpe, out float grados))
            {
                return OrillaDanio.Ninguna;
            }
            float abs = Mathf.Abs(grados);
            if (abs <= gradosFrente)
            {
                return OrillaDanio.Frente;
            }
            if (abs >= GradosMediaVuelta - gradosAtras)
            {
                return OrillaDanio.Atras;
            }
            return grados > 0f ? OrillaDanio.Derecha : OrillaDanio.Izquierda;
        }

        private const float GradosMediaVuelta = 180f;
        private const float DistanciaFueraDelCuerpo = 0.5f;
        private const float CosenoAlejandose = 0.7f;

        /// <summary>
        /// T-indicador-24nov: true si el impacto ocurrió lejos del jugador y viaja alejándose de él (un golpe o bala del jugador a un enemigo
        /// cercano, que también se publica con objetivo null): ese no es daño al jugador y no debe destellar.
        /// </summary>
        public static bool SeAlejaDelJugador(Vector3 punto, Vector3 jugador, Vector3 direccionDelGolpe)
        {
            Vector3 haciaPunto = new Vector3(punto.x - jugador.x, 0f, punto.z - jugador.z);
            Vector3 viaja = new Vector3(direccionDelGolpe.x, 0f, direccionDelGolpe.z);
            if (haciaPunto.magnitude <= DistanciaFueraDelCuerpo || viaja.sqrMagnitude < 1e-6f)
            {
                return false;
            }
            return Vector3.Dot(haciaPunto.normalized, viaja.normalized) > CosenoAlejandose;
        }

        /// <summary>Opacidad de la marca: 1 al nacer y 0 al cumplirse la duración.</summary>
        public static float Alfa(float transcurrido, float duracion)
        {
            return duracion <= 0f ? 0f : Mathf.Clamp01(1f - transcurrido / duracion);
        }
    }
}

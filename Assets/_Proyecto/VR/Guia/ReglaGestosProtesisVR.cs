using UnityEngine;

namespace MechaCyberpunk.VR.Guia
{
    /// <summary>
    /// VR20, lógica pura: qué pose de prótesis hacen las dos manos. Las posiciones van en el marco de la cabeza sin inclinación
    /// (x a la derecha, y arriba, z al frente plano de la cabeza; el origen son los ojos), así vale para cualquier giro del cuerpo.
    /// PIERNAS: cada mano a menos de <see cref="RadioMuslo"/> del punto de su muslo (estimado desde los ojos: <see cref="AbajoMuslo"/> m abajo,
    /// <see cref="LadoMuslo"/> m a su lado). BRAZOS: cada mano a más de <see cref="LadoBrazoAbierto"/> m a su lado, a la altura de los hombros
    /// (± <see cref="ToleranciaHombros"/> m) y sin irse mucho al frente ni atrás. Cuenta solo si las DOS manos están en la misma pose.
    /// </summary>
    public static class ReglaGestosProtesisVR
    {
        /// <summary>Cuánto tiempo hay que sostener la pose con los dos grips (s).</summary>
        public const float SegundosGesto = 0.5f;

        /// <summary>El muslo queda tanto debajo de los ojos (m): media pierna de una persona de 1.70–1.85 m.</summary>
        public const float AbajoMuslo = 0.9f;
        /// <summary>El muslo queda tanto a cada lado del centro de la cabeza (m).</summary>
        public const float LadoMuslo = 0.17f;
        /// <summary>El muslo queda un poco adelante de los ojos (m), donde se apoya la mano.</summary>
        public const float AdelanteMuslo = 0.05f;
        /// <summary>Qué tan lejos del punto del muslo puede estar la mano (m).</summary>
        public const float RadioMuslo = 0.25f;

        /// <summary>Lo mínimo que la mano se aleja a su lado de la cabeza con el brazo abierto (m).</summary>
        public const float LadoBrazoAbierto = 0.55f;
        /// <summary>Los hombros quedan tanto debajo de los ojos (m).</summary>
        public const float AbajoHombros = 0.25f;
        /// <summary>Cuánto puede subir o bajar la mano respecto a los hombros (m).</summary>
        public const float ToleranciaHombros = 0.3f;
        /// <summary>Cuánto puede irse la mano al frente o atrás con los brazos abiertos (m): brazos al frente no cuentan.</summary>
        public const float FondoBrazoAbierto = 0.45f;

        /// <summary>
        /// La posición <paramref name="mano"/> (mundo) en el marco de la cabeza: x derecha, y arriba, z frente plano. Mirando casi recto
        /// abajo (a los muslos) el frente plano sale del "arriba" de la cabeza.
        /// </summary>
        public static Vector3 EnMarcoCabeza(Vector3 ojos, Vector3 frenteCabeza, Vector3 arribaCabeza, Vector3 mano)
        {
            Vector3 frente = new Vector3(frenteCabeza.x, 0f, frenteCabeza.z);
            if (frente.sqrMagnitude < 0.05f)
            {
                Vector3 porArriba = new Vector3(arribaCabeza.x, 0f, arribaCabeza.z) * Mathf.Sign(-frenteCabeza.y);
                frente = porArriba.sqrMagnitude > frente.sqrMagnitude ? porArriba : frente;
            }
            if (frente.sqrMagnitude < 1e-6f)
            {
                frente = Vector3.forward;
            }
            frente.Normalize();
            Vector3 derecha = Vector3.Cross(Vector3.up, frente);
            Vector3 d = mano - ojos;
            return new Vector3(Vector3.Dot(d, derecha), d.y, Vector3.Dot(d, frente));
        }

        /// <summary>El punto del muslo de ese lado, en el marco de la cabeza.</summary>
        public static Vector3 PuntoMuslo(bool derecha) => new Vector3(derecha ? LadoMuslo : -LadoMuslo, -AbajoMuslo, AdelanteMuslo);

        /// <summary>True si la mano (marco de la cabeza) está sobre el muslo de su lado.</summary>
        public static bool EnMuslo(Vector3 mano, bool derecha)
        {
            bool suLado = derecha ? mano.x > 0f : mano.x < 0f;
            return suLado && Vector3.Distance(mano, PuntoMuslo(derecha)) <= RadioMuslo;
        }

        /// <summary>True si la mano (marco de la cabeza) está con el brazo abierto a su lado, a la altura del hombro.</summary>
        public static bool BrazoAbierto(Vector3 mano, bool derecha)
        {
            float lado = derecha ? mano.x : -mano.x;
            return lado >= LadoBrazoAbierto
                && Mathf.Abs(mano.y + AbajoHombros) <= ToleranciaHombros
                && Mathf.Abs(mano.z) <= FondoBrazoAbierto;
        }

        /// <summary>La pose que hacen las dos manos juntas (marco de la cabeza); Ninguno si no están las dos en la misma.</summary>
        public static GestoProtesisVR Pose(Vector3 izquierda, Vector3 derecha)
        {
            if (EnMuslo(izquierda, false) && EnMuslo(derecha, true))
            {
                return GestoProtesisVR.Piernas;
            }
            if (BrazoAbierto(izquierda, false) && BrazoAbierto(derecha, true))
            {
                return GestoProtesisVR.Brazos;
            }
            return GestoProtesisVR.Ninguno;
        }
    }
}

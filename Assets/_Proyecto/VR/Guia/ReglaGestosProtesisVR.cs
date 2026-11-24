using UnityEngine;

namespace MechaCyberpunk.VR.Guia
{
    /// <summary>
    /// VR20/VR45/VR31b, lógica pura: qué pose de prótesis hacen las dos manos. Las posiciones van en el marco de la cabeza sin inclinación
    /// (x a la derecha, y arriba, z al frente plano de la cabeza; el origen son los ojos), así vale para cualquier giro del cuerpo.
    /// PIERNAS (VR45, más tolerante): las dos manos abajo, entre la cadera y la rodilla (de <see cref="AbajoCadera"/> a <see cref="AbajoRodilla"/> m
    /// bajo los ojos), con cualquier separación entre ellas, sin irse mucho al frente, atrás ni a los lados.
    /// BRAZOS (pose T aproximada): cada mano a más de <see cref="LadoBrazoAbierto"/> m a su lado, a la altura de los hombros
    /// (± <see cref="ToleranciaHombros"/> m) y sin irse mucho al frente ni atrás.
    /// VISIÓN (VR31b): cada mano a un lado de la cabeza (de <see cref="LadoCabezaMinimo"/> a <see cref="LadoCabezaMaximo"/> m a su lado), a la
    /// altura de los ojos (± <see cref="AltoCabeza"/> m) y cerca del plano de la cara. Cuenta solo si las DOS manos están en la misma pose.
    /// </summary>
    public static class ReglaGestosProtesisVR
    {
        /// <summary>Cuánto tiempo hay que sostener la pose con los dos grips (s).</summary>
        public const float SegundosGesto = 0.5f;

        /// <summary>VR45: la cadera queda tanto debajo de los ojos (m); la mano más arriba ya no cuenta como piernas.</summary>
        public const float AbajoCadera = 0.6f;
        /// <summary>VR45: la rodilla queda tanto debajo de los ojos (m); la mano más abajo ya no cuenta.</summary>
        public const float AbajoRodilla = 1.25f;
        /// <summary>VR45: cuánto puede irse la mano a su lado con las manos abajo (m).</summary>
        public const float LadoPiernas = 0.5f;
        /// <summary>VR45: cuánto puede cruzar la mano al otro lado del centro (m): las manos pueden juntarse.</summary>
        public const float CruceCentroPiernas = 0.15f;
        /// <summary>VR45: cuánto puede irse la mano al frente o atrás con las manos abajo (m).</summary>
        public const float FondoPiernas = 0.5f;

        /// <summary>Lo mínimo que la mano se aleja a su lado de la cabeza con el brazo abierto (m). VR45: más tolerante (antes 0.55).</summary>
        public const float LadoBrazoAbierto = 0.45f;
        /// <summary>Los hombros quedan tanto debajo de los ojos (m).</summary>
        public const float AbajoHombros = 0.25f;
        /// <summary>Cuánto puede subir o bajar la mano respecto a los hombros (m). VR45: antes 0.3.</summary>
        public const float ToleranciaHombros = 0.35f;
        /// <summary>Cuánto puede irse la mano al frente o atrás con los brazos abiertos (m): brazos al frente no cuentan. VR45: antes 0.45.</summary>
        public const float FondoBrazoAbierto = 0.5f;

        /// <summary>VR31b: lo mínimo que la mano se aleja a su lado del centro de la cabeza para la visión (m).</summary>
        public const float LadoCabezaMinimo = 0.04f;
        /// <summary>VR31b: lo más que la mano se aleja a su lado de la cabeza para la visión (m).</summary>
        public const float LadoCabezaMaximo = 0.32f;
        /// <summary>VR31b: cuánto puede subir o bajar la mano respecto a los ojos (m).</summary>
        public const float AltoCabeza = 0.2f;
        /// <summary>VR31b: cuánto puede irse la mano atrás de los ojos (m).</summary>
        public const float AtrasCabeza = 0.2f;
        /// <summary>VR31b: cuánto puede irse la mano al frente de los ojos (m).</summary>
        public const float FrenteCabeza = 0.2f;

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

        /// <summary>VR45: true si la mano (marco de la cabeza) está abajo, entre la cadera y la rodilla, de su lado o cerca del centro.</summary>
        public static bool EnPiernas(Vector3 mano, bool derecha)
        {
            float lado = derecha ? mano.x : -mano.x;
            return mano.y <= -AbajoCadera && mano.y >= -AbajoRodilla
                && lado >= -CruceCentroPiernas && lado <= LadoPiernas
                && Mathf.Abs(mano.z) <= FondoPiernas;
        }

        /// <summary>Compatibilidad con VR20: ahora es <see cref="EnPiernas"/> (de la cadera a la rodilla).</summary>
        public static bool EnMuslo(Vector3 mano, bool derecha) => EnPiernas(mano, derecha);

        /// <summary>True si la mano (marco de la cabeza) está con el brazo abierto a su lado, a la altura del hombro.</summary>
        public static bool BrazoAbierto(Vector3 mano, bool derecha)
        {
            float lado = derecha ? mano.x : -mano.x;
            return lado >= LadoBrazoAbierto
                && Mathf.Abs(mano.y + AbajoHombros) <= ToleranciaHombros
                && Mathf.Abs(mano.z) <= FondoBrazoAbierto;
        }

        /// <summary>VR31b: true si la mano (marco de la cabeza) está a su lado de la cabeza, a la altura de los ojos.</summary>
        public static bool JuntoACabeza(Vector3 mano, bool derecha)
        {
            float lado = derecha ? mano.x : -mano.x;
            return lado >= LadoCabezaMinimo && lado <= LadoCabezaMaximo
                && Mathf.Abs(mano.y) <= AltoCabeza
                && mano.z >= -AtrasCabeza && mano.z <= FrenteCabeza;
        }

        /// <summary>La pose que hacen las dos manos juntas (marco de la cabeza); Ninguno si no están las dos en la misma.</summary>
        public static GestoProtesisVR Pose(Vector3 izquierda, Vector3 derecha)
        {
            if (EnPiernas(izquierda, false) && EnPiernas(derecha, true))
            {
                return GestoProtesisVR.Piernas;
            }
            if (BrazoAbierto(izquierda, false) && BrazoAbierto(derecha, true))
            {
                return GestoProtesisVR.Brazos;
            }
            if (JuntoACabeza(izquierda, false) && JuntoACabeza(derecha, true))
            {
                return GestoProtesisVR.Vision;
            }
            return GestoProtesisVR.Ninguno;
        }
    }
}

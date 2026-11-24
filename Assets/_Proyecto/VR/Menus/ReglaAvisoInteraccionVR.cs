using UnityEngine;

namespace MechaCyberpunk.VR.Menus
{
    /// <summary>
    /// V6, lógica pura: dónde va y de qué tamaño es el letrero de interactuar en VR (<see cref="AvisoInteraccionVR"/>).
    /// Va un poco por encima de lo que se usa (sus bounds) o, si no se puede medir, a una altura fija sobre su punto en el piso,
    /// y crece con la distancia para leerse igual de 1 a 3 m.
    /// </summary>
    public static class ReglaAvisoInteraccionVR
    {
        /// <summary>Separación entre lo alto del objeto (o la cabeza del NPC) y el letrero (m).</summary>
        public const float MargenArriba = 0.25f;
        /// <summary>Altura del letrero sobre el punto del piso cuando no hay bounds que medir (m).</summary>
        public const float AlturaRespaldo = 2.0f;
        /// <summary>Un renderer o collider cuenta solo si queda a esta distancia o menos (en el plano) del punto que se usa (m).</summary>
        public const float RadioPiezas = 1.5f;
        /// <summary>Piezas más altas que esto (edificios, fachadas) no cuentan para el letrero (m).</summary>
        public const float AlturaMaximaPieza = 5f;
        /// <summary>A esta distancia de la cabeza el letrero tiene escala 1 (m).</summary>
        public const float DistanciaReferencia = 1.5f;
        public const float EscalaMinima = 0.75f;
        public const float EscalaMaxima = 2f;

        /// <summary>True si una pieza con estos bounds sirve para medir lo que se usa en <paramref name="punto"/>.</summary>
        public static bool PiezaCuenta(Bounds pieza, Vector3 punto)
        {
            if (pieza.size.y > AlturaMaximaPieza)
            {
                return false;
            }
            Vector3 cercano = pieza.ClosestPoint(new Vector3(punto.x, pieza.center.y, punto.z));
            cercano.y = 0f;
            Vector3 plano = new Vector3(punto.x, 0f, punto.z);
            return (cercano - plano).sqrMagnitude <= RadioPiezas * RadioPiezas;
        }

        /// <summary>Dónde va el letrero: sobre el centro de los bounds, un poco arriba; sin bounds, sobre el punto del piso.</summary>
        public static Vector3 Posicion(bool hayBounds, Bounds bounds, Vector3 punto)
        {
            if (!hayBounds)
            {
                return punto + Vector3.up * AlturaRespaldo;
            }
            return new Vector3(bounds.center.x, bounds.max.y + MargenArriba, bounds.center.z);
        }

        /// <summary>El giro del letrero: de pie y de cara a la cabeza (su +Z apunta lejos de ella, así se lee el texto).</summary>
        public static Quaternion Giro(Vector3 letrero, Vector3 cabeza)
        {
            Vector3 hacia = letrero - cabeza;
            hacia.y = 0f;
            if (hacia.sqrMagnitude < 1e-6f)
            {
                return Quaternion.identity;
            }
            return Quaternion.LookRotation(hacia.normalized, Vector3.up);
        }

        /// <summary>Escala del letrero a esta distancia de la cabeza: crece con la distancia para que se lea a 3 m.</summary>
        public static float Escala(float distancia)
        {
            return Mathf.Clamp(distancia / DistanciaReferencia, EscalaMinima, EscalaMaxima);
        }
    }
}

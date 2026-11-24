using UnityEngine;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR21: dónde va la cabeza al manejar en VR (lógica pura, sin MonoBehaviour).
    /// <list type="bullet">
    /// <item>Con asiento (todos los <c>CarroViejo</c>, carros y motos): los ojos van donde la primera persona de la <c>CamaraCarro</c> de PC,
    /// <c>asiento + arriba del vehículo × AlturaOjos</c> de su ConfigCamaraCarro (1.15 m por defecto; en los carros de los creadores da 78 % del
    /// alto). En carros, además, bajo el techo (<see cref="BajoElTecho"/>).</item>
    /// <item>Sin asiento o con una altura que no tiene sentido (fuera de <see cref="AlturaMinimaValida"/>..<see cref="AlturaMaximaValida"/> sobre
    /// el fondo del vehículo) se estima con los bounds del vehículo en su espacio local: carros adelante-izquierda (x = centro − 22 % del ancho,
    /// como CreadorCarrosT511/T1342; z = centro − 3 % del largo) a <see cref="AlturaOjosCarro"/> sobre el fondo; motos al centro a
    /// <see cref="AlturaOjosMoto"/>.</item>
    /// <item>El rig solo gira en yaw con el vehículo (sin cabeceo ni balanceo de la carrocería o de la moto: marea) y va pegado sin retraso.</item>
    /// </list>
    /// </summary>
    public static class ReglaAsientoVR
    {
        /// <summary>Ojos de un conductor sentado en un carro, sobre el fondo de los bounds (m).</summary>
        public const float AlturaOjosCarro = 1.15f;
        /// <summary>Ojos de quien va en moto, sobre el fondo de los bounds (m).</summary>
        public const float AlturaOjosMoto = 1.4f;
        /// <summary>Del centro hacia la izquierda, en fracción del ancho (el asiento del conductor de los creadores de carros).</summary>
        public const float FraccionLateralCarro = 0.22f;
        /// <summary>Del centro hacia atrás, en fracción del largo.</summary>
        public const float FraccionAtras = 0.03f;
        /// <summary>Una altura de ojos (m sobre el fondo del vehículo) por debajo de esto no es creíble y se estima.</summary>
        public const float AlturaMinimaValida = 0.6f;
        /// <summary>Una altura de ojos (m sobre el fondo del vehículo) por encima de esto no es creíble y se estima.</summary>
        public const float AlturaMaximaValida = 2.2f;
        /// <summary>Cuánto debajo del techo (lo alto de los bounds) van los ojos en un carro, como mínimo (m).</summary>
        public const float MargenTecho = 0.15f;
        /// <summary>Tope de la altura de los ojos de alguien sentado en un carro (m sobre el fondo del vehículo).</summary>
        public const float AlturaMaximaSentado = 1.35f;
        private const float MinimoPlano = 1e-4f;

        /// <summary>Los ojos de la primera persona de PC: el asiento más la altura de ojos hacia arriba del vehículo.</summary>
        public static Vector3 OjosDesdeAsiento(Vector3 asiento, Vector3 arribaVehiculo, float alturaOjos)
        {
            return asiento + arribaVehiculo.normalized * alturaOjos;
        }

        /// <summary>Los ojos estimados en el espacio local del vehículo, con sus bounds locales (escala local: se pasa la del vehículo).</summary>
        public static Vector3 OjosEstimadosLocal(Bounds caja, bool moto, float escalaVertical = 1f)
        {
            float x = moto ? caja.center.x : caja.center.x - caja.size.x * FraccionLateralCarro;
            float z = caja.center.z - caja.size.z * FraccionAtras;
            float altura = moto ? AlturaOjosMoto : AlturaOjosCarro;
            float y = caja.min.y + altura / Mathf.Max(MinimoPlano, escalaVertical);
            return new Vector3(x, y, z);
        }

        /// <summary>True si los ojos (local) quedan a una altura creíble sobre el fondo de los bounds (en m del mundo).</summary>
        public static bool AlturaValida(Vector3 ojosLocal, Bounds caja, float escalaVertical = 1f)
        {
            float altura = (ojosLocal.y - caja.min.y) * escalaVertical;
            return altura >= AlturaMinimaValida && altura <= AlturaMaximaValida;
        }

        /// <summary>
        /// En carros la cabeza va bajo el techo: como mucho <see cref="MargenTecho"/> debajo de lo alto de los bounds (el CarroViejo de prueba
        /// tiene la cámara de PC en 1.9 m, sobre su cabina de 1.45 m). En motos no se limita (la cabeza va sobre la moto).
        /// </summary>
        public static Vector3 BajoElTecho(Vector3 ojosLocal, Bounds caja, float escalaVertical = 1f)
        {
            float techo = caja.max.y - MargenTecho / Mathf.Max(MinimoPlano, escalaVertical);
            // Sentado al volante los ojos no pasan de AlturaMaximaSentado sobre el fondo, aunque los bounds sean altos (antenas, carga, colisión de techo).
            techo = Mathf.Min(techo, caja.min.y + AlturaMaximaSentado / Mathf.Max(MinimoPlano, escalaVertical));
            if (ojosLocal.y > techo)
            {
                ojosLocal.y = techo;
            }
            return ojosLocal;
        }

        /// <summary>El yaw (°) de un frente; si el frente es casi vertical (vehículo volcado), se queda el <paramref name="previo"/>.</summary>
        public static float Yaw(Vector3 frente, float previo)
        {
            frente.y = 0f;
            return frente.sqrMagnitude > MinimoPlano ? Mathf.Atan2(frente.x, frente.z) * Mathf.Rad2Deg : previo;
        }

        /// <summary>
        /// Pose del rig (XR Origin) para que la cabeza, tal como estaba en el cuarto al subir (<paramref name="cabezaRefEnRig"/>, espacio del rig, y
        /// <paramref name="yawCabezaRef"/>), quede justo en <paramref name="ojos"/> mirando al frente del vehículo (<paramref name="yawVehiculo"/>).
        /// Si la persona mueve la cabeza en el cuarto, la cabeza se mueve igual dentro de la cabina.
        /// </summary>
        public static Pose PoseRig(Vector3 ojos, float yawVehiculo, Vector3 cabezaRefEnRig, float yawCabezaRef)
        {
            Quaternion giro = Quaternion.Euler(0f, yawVehiculo - yawCabezaRef, 0f);
            return new Pose(ojos - giro * cabezaRefEnRig, giro);
        }
    }
}

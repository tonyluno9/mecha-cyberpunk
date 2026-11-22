using UnityEngine;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>
    /// T-1628: lógica pura de colgarse de un borde alto con las manos (PJ01_Colgado_Idle). Un borde que queda a la altura de las manos
    /// (más alto que lo que se trepa de un tirón y dentro del alcance de agarre) cuelga al jugador; de ahí sube (PJ01_Colgado_Subir) o se suelta.
    /// </summary>
    public static class ReglaColgarse
    {
        /// <summary>True si el borde a esa altura sobre los pies (m) se alcanza colgándose: dentro del rango [minima, maxima].</summary>
        public static bool DebeColgarse(bool activo, bool visualPuede, float alturaBorde, float minima, float maxima)
        {
            return activo && visualPuede && alturaBorde >= minima && alturaBorde <= maxima;
        }

        /// <summary>Dónde quedan los pies colgado: bajo el borde a la altura de las manos y separados de la pared (la pared queda al frente).</summary>
        public static Vector3 PosicionColgado(Vector3 destinoArriba, Vector3 frente, float radio, float margen, float distanciaPared, float alturaManos)
        {
            Vector3 plano = new Vector3(frente.x, 0f, frente.z);
            plano = plano.sqrMagnitude > 1e-6f ? plano.normalized : Vector3.forward;
            // destinoArriba está a radio + margen detrás de la pared (ya sobre el techo): se retrocede ese tramo, el cuerpo y la distancia a la pared.
            Vector3 xz = destinoArriba - plano * (2f * (radio + margen) + distanciaPared);
            return new Vector3(xz.x, destinoArriba.y - margen - alturaManos, xz.z);
        }

        /// <summary>Qué hace el colgado con la entrada: subir con salto o con W recién pulsada, soltarse con S; nada antes del tiempo mínimo ni durante el acercamiento.</summary>
        public static AccionColgado Decidir(float segundosColgado, float segundosEntrada, float segundosMinimos, bool saltoPulsado, bool adelanteNuevo, bool atras)
        {
            if (segundosColgado < segundosEntrada + segundosMinimos)
            {
                return AccionColgado.Nada;
            }
            if (atras)
            {
                return AccionColgado.Soltar;
            }
            return saltoPulsado || adelanteNuevo ? AccionColgado.Subir : AccionColgado.Nada;
        }
    }

    /// <summary>Lo que decide el jugador colgado.</summary>
    public enum AccionColgado
    {
        Nada,
        Subir,
        Soltar
    }
}

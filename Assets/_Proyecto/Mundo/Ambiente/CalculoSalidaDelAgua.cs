using UnityEngine;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>Lógica pura de <see cref="SalidaDelAgua"/> (T-1358): cuándo algo cuenta como "en el agua" y a dónde se saca.</summary>
    public static class CalculoSalidaDelAgua
    {
        /// <summary>True si la altura está por debajo del nivel de nado seguro (m).</summary>
        public static bool EstaEnAgua(float y, float nivelMaximo)
        {
            return y < nivelMaximo;
        }

        /// <summary>Segundos acumulados en el agua: suben con dt dentro y vuelven a 0 al salir.</summary>
        public static float Acumular(float segundos, bool enAgua, float dt)
        {
            return enAgua ? segundos + dt : 0f;
        }

        /// <summary>Punto de orilla más cercano en planta (x, z). Con la lista vacía devuelve la misma posición.</summary>
        public static Vector3 PuntoMasCercano(Vector3 posicion, Vector3[] puntos)
        {
            if (puntos == null || puntos.Length == 0)
            {
                return posicion;
            }
            Vector3 mejor = puntos[0];
            float menor = float.MaxValue;
            foreach (Vector3 p in puntos)
            {
                float dx = p.x - posicion.x;
                float dz = p.z - posicion.z;
                float d = dx * dx + dz * dz;
                if (d < menor)
                {
                    menor = d;
                    mejor = p;
                }
            }
            return mejor;
        }
    }
}

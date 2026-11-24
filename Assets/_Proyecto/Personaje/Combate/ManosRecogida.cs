using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// T-drops-24nov: las manos de VR que pueden recoger cosas del piso al acercarse (las registra ManoVR al encenderse). Sin VR queda vacía y
    /// los pickups solo se recogen al pasar con el cuerpo.
    /// </summary>
    public static class ManosRecogida
    {
        private const int MaxManos = 4;
        private static readonly List<Transform> manos = new List<Transform>(MaxManos);
        private static readonly Vector3[] posiciones = new Vector3[MaxManos];

        public static void Agregar(Transform mano)
        {
            if (mano != null && !manos.Contains(mano) && manos.Count < MaxManos)
            {
                manos.Add(mano);
            }
        }

        public static void Quitar(Transform mano)
        {
            manos.Remove(mano);
        }

        /// <summary>Copia las posiciones de las manos activas a un arreglo reutilizado y devuelve cuántas hay.</summary>
        public static int Posiciones(out Vector3[] resultado)
        {
            int n = 0;
            for (int i = manos.Count - 1; i >= 0; i--)
            {
                if (manos[i] == null)
                {
                    manos.RemoveAt(i);
                }
            }
            foreach (Transform m in manos)
            {
                if (m.gameObject.activeInHierarchy)
                {
                    posiciones[n++] = m.position;
                }
            }
            resultado = posiciones;
            return n;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => manos.Clear();
    }
}

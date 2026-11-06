using System;
using System.Collections.Generic;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>Las reglas de los huecos de bala (T-808), sin Unity: si una superficie es de metal y cuánto se ve un hueco con el tiempo.</summary>
    public static class ReglaHuecosBala
    {
        /// <summary>
        /// Metal si es un vehículo (cuerpo rígido o algo que se monta) o si alguno de los nombres (del objeto, sus padres o su material) lleva una
        /// de las palabras, sin distinguir mayúsculas. Si no, concreto.
        /// </summary>
        public static bool EsMetal(IEnumerable<string> nombres, bool conCuerpoRigido, bool montable, IReadOnlyList<string> palabras)
        {
            if (conCuerpoRigido || montable)
            {
                return true;
            }
            if (nombres == null || palabras == null)
            {
                return false;
            }
            foreach (string nombre in nombres)
            {
                if (string.IsNullOrEmpty(nombre))
                {
                    continue;
                }
                foreach (string palabra in palabras)
                {
                    if (!string.IsNullOrEmpty(palabra) && nombre.IndexOf(palabra, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>Opacidad del hueco con <paramref name="edad"/> segundos: 1 mientras se ve, luego baja a 0 en <paramref name="apagado"/>.</summary>
        public static float Alfa(float edad, float visible, float apagado)
        {
            if (edad <= visible)
            {
                return 1f;
            }
            if (apagado <= 0f)
            {
                return 0f;
            }
            float t = 1f - (edad - visible) / apagado;
            return t < 0f ? 0f : t > 1f ? 1f : t;
        }
    }
}

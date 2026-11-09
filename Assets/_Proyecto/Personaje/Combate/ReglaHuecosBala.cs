using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>Las reglas de los huecos de bala (T-808), sin Unity: si una superficie es de metal y cuánto se ve un hueco con el tiempo.</summary>
    public static class ReglaHuecosBala
    {
        /// <summary>T-931: cuánto se amplían (m) los límites de un Renderer vecino para decidir si el punto del impacto cae sobre una superficie que se ve.</summary>
        public const float MargenSuperficie = 0.05f;

        /// <summary>T-931: cuánto se puede mover (m) la superficie antes de que el hueco desaparezca.</summary>
        public const float UmbralMovimiento = 0.02f;

        /// <summary>T-931: hay hueco solo si el collider no es trigger, no es una persona y hay una superficie visible en ese punto.</summary>
        public static bool HayHueco(bool esTrigger, bool esPersona, bool haySuperficieVisible) => !esTrigger && !esPersona && haySuperficieVisible;

        /// <summary>T-931: true si el punto está dentro de la caja (centro y semiextensiones) ampliada <paramref name="margen"/> por cada lado.</summary>
        public static bool PuntoEnCaja(Vector3 centro, Vector3 extension, Vector3 punto, float margen)
        {
            return Mathf.Abs(punto.x - centro.x) <= extension.x + margen
                && Mathf.Abs(punto.y - centro.y) <= extension.y + margen
                && Mathf.Abs(punto.z - centro.z) <= extension.z + margen;
        }

        /// <summary>T-931: true si el punto del hueco se movió más de <paramref name="umbral"/> metros respecto a donde quedó.</summary>
        public static bool SeMovio(Vector3 donde, Vector3 ahora, float umbral = UmbralMovimiento) => (ahora - donde).sqrMagnitude > umbral * umbral;

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

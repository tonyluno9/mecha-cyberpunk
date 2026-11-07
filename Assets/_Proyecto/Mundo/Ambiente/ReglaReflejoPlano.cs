using UnityEngine;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// Reflejo en un piso horizontal (charcos): dónde va la cámara del reflejo y hacia dónde mira. Lógica pura para probarla sin escena.
    /// </summary>
    public static class ReglaReflejoPlano
    {
        /// <summary>El punto reflejado en el plano horizontal de altura <paramref name="altura"/> (m).</summary>
        public static Vector3 Reflejar(Vector3 punto, float altura) => new Vector3(punto.x, 2f * altura - punto.y, punto.z);

        /// <summary>La dirección reflejada en un plano horizontal (solo cambia el signo de la vertical).</summary>
        public static Vector3 ReflejarDireccion(Vector3 direccion) => new Vector3(direccion.x, -direccion.y, direccion.z);

        /// <summary>
        /// Giro de la cámara del reflejo: mira a lo largo del frente reflejado con el arriba reflejado. Cubre el mismo pedazo de piso que
        /// la cámara original (el espejo horizontal solo voltea la imagen, y el shader proyecta cada punto del charco en esta cámara).
        /// </summary>
        public static Quaternion Giro(Vector3 adelante, Vector3 arriba)
        {
            return Quaternion.LookRotation(ReflejarDireccion(adelante), ReflejarDireccion(arriba));
        }
    }
}

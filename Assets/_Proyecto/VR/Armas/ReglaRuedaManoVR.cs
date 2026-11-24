using UnityEngine;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR19, lógica pura: con la rueda de armas abierta (mantener Y), el cursor de la rueda se mueve con la mano izquierda o con el stick izquierdo.
    /// - Mano: donde estaba la mano al abrir es el centro de la rueda; moverla <see cref="MetrosAlBorde"/> hacia un lado (en el plano de la rueda)
    ///   lleva el cursor al borde de ese lado.
    /// - Stick: inclinarlo pasa de <see cref="ZonaStick"/> pone el cursor en esa dirección (a fondo, en el borde) y toma la mano de ahí como
    ///   nuevo punto de partida, así se pueden mezclar.
    /// El resultado es la posición del cursor en unidades de la rueda (radio <c>radio</c>).
    /// </summary>
    public sealed class ReglaRuedaManoVR
    {
        /// <summary>Lo que hay que mover la mano (m) para llevar el cursor del centro al borde.</summary>
        public const float MetrosAlBorde = 0.12f;
        /// <summary>El stick por encima de esto (0..1) manda sobre la mano.</summary>
        public const float ZonaStick = 0.3f;

        private Vector3 manoBase;
        private Vector2 cursorBase;
        private bool abierta;

        /// <summary>True entre <see cref="Abrir"/> y <see cref="Cerrar"/>.</summary>
        public bool Abierta => abierta;

        /// <summary>La rueda se abrió con la mano en <paramref name="mano"/>: ahí queda el centro.</summary>
        public void Abrir(Vector3 mano)
        {
            abierta = true;
            manoBase = mano;
            cursorBase = Vector2.zero;
        }

        public void Cerrar()
        {
            abierta = false;
        }

        /// <summary>
        /// El cursor (unidades de la rueda) con la mano en <paramref name="mano"/>, los ejes del plano de la rueda (<paramref name="derecha"/>,
        /// <paramref name="arriba"/>, en el mismo espacio que la mano) y el stick izquierdo. Abre sola si no estaba abierta.
        /// </summary>
        public Vector2 Avanzar(Vector3 mano, Vector3 derecha, Vector3 arriba, Vector2 stick, float radio)
        {
            if (!abierta)
            {
                Abrir(mano);
            }
            float fuerza = stick.magnitude;
            if (fuerza > ZonaStick)
            {
                cursorBase = stick / Mathf.Max(1f, fuerza) * radio;
                manoBase = mano;
                return cursorBase;
            }
            Vector3 d = mano - manoBase;
            Vector3 der = derecha.sqrMagnitude > 1e-6f ? derecha.normalized : Vector3.right;
            Vector3 arr = arriba.sqrMagnitude > 1e-6f ? arriba.normalized : Vector3.up;
            Vector2 movido = new Vector2(Vector3.Dot(d, der), Vector3.Dot(d, arr)) / MetrosAlBorde * radio;
            return Vector2.ClampMagnitude(cursorBase + movido, radio);
        }
    }
}

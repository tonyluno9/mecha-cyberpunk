using System;
using UnityEngine;

namespace MechaCyberpunk.UI.Rueda
{
    /// <summary>Qué hace la rueda al soltar la tecla.</summary>
    public enum TipoSeleccionRueda
    {
        Nada,
        Arma,
        Objeto,
    }

    /// <summary>Lo que eligió la rueda: un arma (<see cref="Indice"/> en el orden de ArmaAPie) o un objeto (lugar en el catálogo de consumibles).</summary>
    public readonly struct SeleccionRueda
    {
        public readonly TipoSeleccionRueda Tipo;
        public readonly int Indice;

        public SeleccionRueda(TipoSeleccionRueda tipo, int indice)
        {
            Tipo = tipo;
            Indice = indice;
        }

        public static readonly SeleccionRueda Ninguna = new SeleccionRueda(TipoSeleccionRueda.Nada, -1);
    }

    /// <summary>
    /// T-991: la lógica pura de la rueda de armas y objetos (docs/diseno/rueda-y-habilidades.md). Diez casillas en anillo de 36° cada una, con los ángulos
    /// medidos en el sentido del reloj desde arriba. Armas, en la mitad de arriba, de izquierda a derecha: Puños 288°, Pistola 324°, Escopeta 0°, Fusil 36°
    /// y Francotirador 72°. Objetos, en la mitad de abajo, de derecha a izquierda: Bebida 108°, Papitas 144°, Venda 180°, Café 216° y Batería 252°.
    /// La casilla 0 a 4 es el arma (mismo orden que ArmaAPie) y la 5 a 9 es el objeto 0 a 4 del catálogo. El cursor virtual tiene su origen en el centro, con +y hacia arriba.
    /// </summary>
    public static class ReglaRuedaArmas
    {
        public const int Casillas = 10;
        public const int CasillasArma = 5;
        public const float GradosPorCasilla = 36f;
        public const float AnguloPrimera = 288f;
        /// <summary>Radio máximo del cursor virtual (px de referencia).</summary>
        public const float RadioMaximo = 160f;
        /// <summary>Dentro de este radio (px) no se resalta nada.</summary>
        public const float ZonaMuerta = 40f;
        /// <summary>Escala de tiempo mientras la rueda está abierta.</summary>
        public const float EscalaTiempo = 0.25f;

        /// <summary>El ángulo (° en el sentido del reloj desde arriba) del centro de la casilla.</summary>
        public static float AnguloDeCasilla(int casilla)
        {
            return Mathf.Repeat(AnguloPrimera + GradosPorCasilla * casilla, 360f);
        }

        public static bool EsArma(int casilla) => casilla >= 0 && casilla < CasillasArma;

        public static bool EsObjeto(int casilla) => casilla >= CasillasArma && casilla < Casillas;

        /// <summary>El ángulo del cursor (° en el sentido del reloj desde arriba, 0 a 360).</summary>
        public static float AnguloDeCursor(Vector2 cursor)
        {
            return Mathf.Repeat(Mathf.Atan2(cursor.x, cursor.y) * Mathf.Rad2Deg, 360f);
        }

        /// <summary>La casilla a la que señala el ángulo (cada una cubre 18° a cada lado de su centro).</summary>
        public static int CasillaEnAngulo(float grados)
        {
            float desde = Mathf.Repeat(grados - AnguloPrimera, 360f);
            return Mathf.RoundToInt(desde / GradosPorCasilla) % Casillas;
        }

        /// <summary>Suma el movimiento del ratón al cursor y lo deja dentro del radio máximo.</summary>
        public static Vector2 MoverCursor(Vector2 cursor, Vector2 delta)
        {
            return Vector2.ClampMagnitude(cursor + delta, RadioMaximo);
        }

        /// <summary>
        /// La casilla resaltada con el cursor en esa posición: -1 dentro de la zona muerta o si la casilla señalada no se puede elegir.
        /// <paramref name="disponible"/> dice si la casilla (0 a 9) se puede elegir (arma que se tiene, objeto con piezas).
        /// </summary>
        public static int CasillaResaltada(Vector2 cursor, Func<int, bool> disponible)
        {
            if (cursor.magnitude < ZonaMuerta)
            {
                return -1;
            }
            int casilla = CasillaEnAngulo(AnguloDeCursor(cursor));
            return disponible == null || disponible(casilla) ? casilla : -1;
        }

        /// <summary>Lo que hace la rueda al soltar con esta casilla resaltada: equipar el arma, usar el objeto o nada.</summary>
        public static SeleccionRueda Resultado(int casillaResaltada)
        {
            if (EsArma(casillaResaltada))
            {
                return new SeleccionRueda(TipoSeleccionRueda.Arma, casillaResaltada);
            }
            if (EsObjeto(casillaResaltada))
            {
                return new SeleccionRueda(TipoSeleccionRueda.Objeto, casillaResaltada - CasillasArma);
            }
            return SeleccionRueda.Ninguna;
        }
    }
}

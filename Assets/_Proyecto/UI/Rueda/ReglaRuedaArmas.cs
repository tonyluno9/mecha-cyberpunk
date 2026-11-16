using System;
using System.Collections.Generic;
using MechaCyberpunk.Personaje.Combate;
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
    /// La lógica pura de la rueda de armas y objetos (T-991; T-1010: dibujo como el mockup M30_RuedaArmas.png). Cada casilla tiene un id fijo: el arma
    /// es su valor de ArmaAPie (0 a <see cref="CasillasArma"/> − 1) y el objeto i del catálogo de consumibles es <see cref="CasillaObjeto"/>(i).
    /// La rueda solo muestra lo que llevas, en sectores iguales en el sentido del reloj desde arriba: primero las armas de fuego en el orden de
    /// ArmaAPie, luego los puños y luego los objetos con piezas. Con menos de <see cref="MinimoSectores"/> casillas se rellena con sectores vacíos.
    /// El cursor virtual tiene su origen en el centro, con +y hacia arriba.
    /// </summary>
    public static class ReglaRuedaArmas
    {
        /// <summary>Casillas de arma: una por valor de ArmaAPie (los puños incluidos).</summary>
        public static readonly int CasillasArma = Enum.GetValues(typeof(ArmaAPie)).Length;
        /// <summary>Objetos del catálogo de consumibles que caben en la rueda.</summary>
        public const int ObjetosMaximos = 5;
        /// <summary>Sectores mínimos del anillo (los que sobran quedan vacíos), como el mockup M30.</summary>
        public const int MinimoSectores = 8;
        /// <summary>Id de un sector vacío.</summary>
        public const int Vacia = -1;
        /// <summary>Radio máximo del cursor virtual (px de referencia).</summary>
        public const float RadioMaximo = 160f;
        /// <summary>Dentro de este radio (px) no se resalta nada.</summary>
        public const float ZonaMuerta = 40f;
        /// <summary>Escala de tiempo mientras la rueda está abierta.</summary>
        public const float EscalaTiempo = 0.25f;

        /// <summary>Total de ids de casilla (armas más objetos).</summary>
        public static int Casillas => CasillasArma + ObjetosMaximos;

        /// <summary>El id de la casilla del objeto en ese lugar del catálogo.</summary>
        public static int CasillaObjeto(int lugar) => CasillasArma + lugar;

        public static bool EsArma(int casilla) => casilla >= 0 && casilla < CasillasArma;

        public static bool EsObjeto(int casilla) => casilla >= CasillasArma && casilla < Casillas;

        /// <summary>
        /// El orden de los sectores (ids de casilla, <see cref="Vacia"/> para un sector vacío): armas de fuego visibles, puños y objetos visibles.
        /// <paramref name="visible"/> dice si la casilla se muestra (arma que tienes, objeto con piezas); los puños siempre se muestran.
        /// </summary>
        public static List<int> Disposicion(Func<int, bool> visible)
        {
            List<int> lista = new List<int>();
            for (int arma = 1; arma < CasillasArma; arma++)
            {
                if (visible == null || visible(arma))
                {
                    lista.Add(arma);
                }
            }
            lista.Add((int)ArmaAPie.Punos);
            for (int lugar = 0; lugar < ObjetosMaximos; lugar++)
            {
                int id = CasillaObjeto(lugar);
                if (visible == null || visible(id))
                {
                    lista.Add(id);
                }
            }
            while (lista.Count < MinimoSectores)
            {
                lista.Add(Vacia);
            }
            return lista;
        }

        /// <summary>Grados que cubre cada sector con <paramref name="sectores"/> sectores.</summary>
        public static float GradosPorSector(int sectores) => 360f / Mathf.Max(1, sectores);

        /// <summary>El ángulo (° en el sentido del reloj desde arriba) del centro del sector en esa posición.</summary>
        public static float AnguloDeSector(int posicion, int sectores) => Mathf.Repeat(GradosPorSector(sectores) * posicion, 360f);

        /// <summary>El ángulo del cursor (° en el sentido del reloj desde arriba, 0 a 360).</summary>
        public static float AnguloDeCursor(Vector2 cursor) => Mathf.Repeat(Mathf.Atan2(cursor.x, cursor.y) * Mathf.Rad2Deg, 360f);

        /// <summary>La posición del sector al que señala el ángulo (cada uno cubre la mitad de su ancho a cada lado de su centro).</summary>
        public static int SectorEnAngulo(float grados, int sectores)
        {
            int n = Mathf.Max(1, sectores);
            return Mathf.RoundToInt(Mathf.Repeat(grados, 360f) / GradosPorSector(n)) % n;
        }

        /// <summary>Suma el movimiento del ratón al cursor y lo deja dentro del radio máximo.</summary>
        public static Vector2 MoverCursor(Vector2 cursor, Vector2 delta) => Vector2.ClampMagnitude(cursor + delta, RadioMaximo);

        /// <summary>
        /// La casilla resaltada (id) con el cursor en esa posición: -1 dentro de la zona muerta, en un sector vacío o si la casilla no se puede elegir.
        /// </summary>
        public static int CasillaResaltada(Vector2 cursor, IList<int> disposicion, Func<int, bool> disponible)
        {
            if (cursor.magnitude < ZonaMuerta || disposicion == null || disposicion.Count == 0)
            {
                return -1;
            }
            int casilla = disposicion[SectorEnAngulo(AnguloDeCursor(cursor), disposicion.Count)];
            if (casilla == Vacia)
            {
                return -1;
            }
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

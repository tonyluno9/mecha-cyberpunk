using System.Collections.Generic;
using MechaCyberpunk.Economia;

namespace MechaCyberpunk.Mecha.Taller
{
    /// <summary>
    /// Regla pura del taller de piezas (T-168, D-tony-117 y D-tony-119): para una ranura devuelve, en orden, las piezas del inventario
    /// ("PONER"), las de la tienda de nivel igual o menor que la liga ("COMPRAR $precio") y las del nivel siguiente, bloqueadas.
    /// La pieza que ya llevas no aparece.
    /// </summary>
    public static class ReglaTallerPiezas
    {
        public const string TextoPoner = "PONER";
        public const string FormatoComprar = "COMPRAR {0}";
        public const string FormatoNivelBloqueado = "SE ABRE EN LA LIGA {0}";

        /// <param name="ranura">Ranura que se cambia.</param>
        /// <param name="puesta">Pieza que lleva el mecha en esa ranura (puede ser null).</param>
        /// <param name="inventario">Piezas que hay en el inventario (de cualquier ranura; se filtra).</param>
        /// <param name="tienda">Piezas del catálogo (de cualquier ranura; se filtra).</param>
        /// <param name="liga">Liga actual (mínimo 1).</param>
        /// <param name="dinero">Dinero ($), puede ser negativo.</param>
        /// <param name="tallerBloqueado">True si la renta vencida bloquea el taller.</param>
        public static List<OpcionPieza> Opciones(RanuraPieza ranura, DatosPieza puesta, IEnumerable<DatosPieza> inventario,
            IEnumerable<DatosPieza> tienda, int liga, int dinero, bool tallerBloqueado)
        {
            var opciones = new List<OpcionPieza>();
            if (liga < 1) liga = 1;
            var yaVistas = new HashSet<DatosPieza>();

            if (inventario != null)
            {
                foreach (DatosPieza p in inventario)
                {
                    if (p == null || p.Ranura != ranura || p == puesta || !yaVistas.Add(p)) continue;
                    opciones.Add(new OpcionPieza(TipoOpcionPieza.Poner, TextoPoner, p, 0, !tallerBloqueado,
                        tallerBloqueado ? MotivoNoElegible.TallerBloqueado : MotivoNoElegible.Ninguno, Dif(p, puesta, 0), Dif(p, puesta, 1),
                        Dif(p, puesta, 2)));
                }
            }

            var compras = new List<DatosPieza>();
            var cerradas = new List<DatosPieza>();
            if (tienda != null)
            {
                foreach (DatosPieza p in tienda)
                {
                    if (p == null || p.Ranura != ranura || p == puesta || p.Precio <= 0 || p.Rama == RamaPieza.DeSerie) continue;
                    if (p.Nivel <= liga) compras.Add(p);
                    else if (p.Nivel == liga + 1) cerradas.Add(p);
                }
            }
            // List.Sort no es estable; se ordena por nivel y luego por posición original.
            compras = PorNivel(compras);
            cerradas = PorNivel(cerradas);

            foreach (DatosPieza p in compras)
            {
                bool alcanza = dinero >= p.Precio;
                MotivoNoElegible motivo = tallerBloqueado ? MotivoNoElegible.TallerBloqueado
                    : alcanza ? MotivoNoElegible.Ninguno : MotivoNoElegible.NoAlcanza;
                opciones.Add(new OpcionPieza(TipoOpcionPieza.Comprar, string.Format(FormatoComprar, FormatoDinero.Texto(p.Precio)), p,
                    p.Precio, motivo == MotivoNoElegible.Ninguno, motivo, Dif(p, puesta, 0), Dif(p, puesta, 1), Dif(p, puesta, 2)));
            }
            foreach (DatosPieza p in cerradas)
            {
                opciones.Add(new OpcionPieza(TipoOpcionPieza.Bloqueada, string.Format(FormatoNivelBloqueado, p.Nivel), p, p.Precio, false,
                    MotivoNoElegible.NivelBloqueado, Dif(p, puesta, 0), Dif(p, puesta, 1), Dif(p, puesta, 2)));
            }
            return opciones;
        }

        private static List<DatosPieza> PorNivel(List<DatosPieza> piezas)
        {
            var resultado = new List<DatosPieza>(piezas.Count);
            for (int nivel = 0; nivel <= 4; nivel++)
            {
                foreach (DatosPieza p in piezas)
                {
                    if (p.Nivel == nivel) resultado.Add(p);
                }
            }
            return resultado;
        }

        // 0 = vida, 1 = daño, 2 = velocidad. Los bonos son aditivos (ReglaEstadisticasPiezas), así que la diferencia de los valores
        // compuestos es la diferencia de los bonos.
        private static float Dif(DatosPieza nueva, DatosPieza puesta, int que)
        {
            float a = que == 0 ? nueva.BonoVida : que == 1 ? nueva.BonoDanio : nueva.BonoVelocidad;
            float b = puesta == null ? 0f : que == 0 ? puesta.BonoVida : que == 1 ? puesta.BonoDanio : puesta.BonoVelocidad;
            return a - b;
        }
    }
}

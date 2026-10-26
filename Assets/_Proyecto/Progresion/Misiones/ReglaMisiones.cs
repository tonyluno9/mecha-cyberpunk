using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Por qué se ofrece o no una misión. El primero que falla, en este orden.</summary>
    public enum MotivoMision
    {
        Disponible,
        FueraDeLibre,
        OtraActiva,
        FranjaEquivocada,
        BarrioNoTeHabla,
        CorporacionesNoLlaman,
        ReputacionBaja,
        LigaBaja,
        FaltaFavor,
        HechaHoy,
        YaHecha,
        FaltanPuntos
    }

    /// <summary>Lo que ve la regla del mundo al decidir si ofrece una misión.</summary>
    public struct ContextoMision
    {
        /// <summary>Valor de <see cref="UltimoDiaCumplida"/> cuando nunca se cumplió.</summary>
        public const int Nunca = -1;

        public EtapaPrologo Etapa;
        /// <summary>False si no hay reloj del mundo: entonces vale cualquier franja.</summary>
        public bool HayReloj;
        public FranjaDia Franja;
        public int Dia;
        /// <summary>Liga más alta abierta (1 a 4).</summary>
        public int Liga;
        public int NivelBarrio;
        public int NivelCorporaciones;
        /// <summary>Cuántos favores de un tipo hay en el bolsillo. Null = ninguno.</summary>
        public Func<TipoFavor, int> CantidadFavor;
        public bool OtraActiva;
        /// <summary>Día en que se cumplió por última vez esta misión, o <see cref="Nunca"/>.</summary>
        public int UltimoDiaCumplida;
        /// <summary>True si existen en la escena todos los puntos que usa.</summary>
        public bool ExistenPuntos;
    }

    /// <summary>Lo que pasa en el mundo al cumplir una misión.</summary>
    public readonly struct RecompensaMision
    {
        public readonly int Pago;
        public readonly DatosObjeto Chatarra;
        public readonly int ChatarraCantidad;
        public readonly int DeltaBarrio;
        public readonly int DeltaCorporaciones;
        public readonly DatosFavor Favor;

        public RecompensaMision(int pago, DatosObjeto chatarra, int cantidad, int deltaBarrio, int deltaCorp, DatosFavor favor)
        {
            Pago = pago;
            Chatarra = chatarra;
            ChatarraCantidad = cantidad;
            DeltaBarrio = deltaBarrio;
            DeltaCorporaciones = deltaCorp;
            Favor = favor;
        }
    }

    /// <summary>Cambio de reputación (al fallar o abandonar).</summary>
    public readonly struct PenalizacionMision
    {
        public readonly int Barrio;
        public readonly int Corporaciones;

        public PenalizacionMision(int barrio, int corporaciones)
        {
            Barrio = barrio;
            Corporaciones = corporaciones;
        }
    }

    /// <summary>Lógica pura de misiones (T-183, D-tony-147): disponibilidad, recompensa y penalización.</summary>
    public static class ReglaMisiones
    {
        /// <summary>Lo que se resta a cada reputación que la misión subía, al fallar o abandonar.</summary>
        public const int PenalizacionPorFallar = 3;

        /// <summary>Decide si se ofrece. El primero que falla, en el orden de <see cref="MotivoMision"/>.</summary>
        public static MotivoMision Decidir(DatosMision datos, ContextoMision c)
        {
            if (c.Etapa != EtapaPrologo.Libre) return MotivoMision.FueraDeLibre;
            if (c.OtraActiva) return MotivoMision.OtraActiva;
            if (c.HayReloj && datos.Franja != FranjaMision.Cualquiera)
            {
                FranjaDia pedida = datos.Franja == FranjaMision.Dia ? FranjaDia.Dia : FranjaDia.Noche;
                if (c.Franja != pedida) return MotivoMision.FranjaEquivocada;
            }
            if (datos.Tipo == TipoMision.Barrio && c.NivelBarrio <= -2) return MotivoMision.BarrioNoTeHabla;
            if (datos.Tipo == TipoMision.Corporativa && c.NivelCorporaciones <= -2) return MotivoMision.CorporacionesNoLlaman;
            if (c.NivelBarrio < datos.NivelBarrioMinimo || c.NivelCorporaciones < datos.NivelCorporacionesMinimo)
                return MotivoMision.ReputacionBaja;
            if (c.Liga < datos.LigaMinima) return MotivoMision.LigaBaja;
            if (datos.ExigeFavor && (c.CantidadFavor == null || c.CantidadFavor(datos.FavorRequerido) < 1))
                return MotivoMision.FaltaFavor;
            if (c.UltimoDiaCumplida != ContextoMision.Nunca)
            {
                if (!datos.Repetible) return MotivoMision.YaHecha;
                if (c.UltimoDiaCumplida >= c.Dia) return MotivoMision.HechaHoy;
            }
            if (!c.ExistenPuntos) return MotivoMision.FaltanPuntos;
            return MotivoMision.Disponible;
        }

        /// <summary>Lo que se entrega al cumplirla.</summary>
        public static RecompensaMision Recompensa(DatosMision datos)
        {
            return new RecompensaMision(datos.Pago, datos.ChatarraObjeto, datos.ChatarraCantidad, datos.DeltaBarrio,
                datos.DeltaCorporaciones, datos.Favor);
        }

        /// <summary>Al fallar o abandonar: -3 a cada reputación que la misión subía; la otra no se toca.</summary>
        public static PenalizacionMision Penalizacion(DatosMision datos)
        {
            return new PenalizacionMision(datos.DeltaBarrio > 0 ? -PenalizacionPorFallar : 0,
                datos.DeltaCorporaciones > 0 ? -PenalizacionPorFallar : 0);
        }

        /// <summary>Tipo de punto que dice un id con la forma Mision_Tipo_Nombre. Null si no se entiende.</summary>
        public static TipoPuntoMision? TipoDePunto(string id)
        {
            if (string.IsNullOrEmpty(id) || !id.StartsWith("Mision_", StringComparison.Ordinal)) return null;
            string[] partes = id.Split('_');
            if (partes.Length < 3) return null;
            return Enum.TryParse(partes[1], false, out TipoPuntoMision t) ? t : (TipoPuntoMision?)null;
        }

        /// <summary>True si un paso de ese tipo puede apuntar a un punto de ese tipo. Escapar no usa punto.</summary>
        public static bool PuntoValidoParaPaso(TipoPaso paso, TipoPuntoMision punto)
        {
            switch (paso)
            {
                case TipoPaso.IrA:
                case TipoPaso.Entregar:
                    return punto == TipoPuntoMision.Destino || punto == TipoPuntoMision.Encargo;
                case TipoPaso.Recoger:
                    return punto == TipoPuntoMision.Botin || punto == TipoPuntoMision.Destino;
                case TipoPaso.Pelear:
                    return punto == TipoPuntoMision.Pelea;
                default:
                    return false;
            }
        }
    }
}

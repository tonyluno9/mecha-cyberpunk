using System;

namespace MechaCyberpunk.Progresion.Guardado
{
    /// <summary>
    /// El resumen de una ranura tal como lo leyó <see cref="ServicioGuardado.ResumenDe"/> (T-412). <see cref="Vacia"/> si no hay archivo,
    /// si está roto o si es de otra versión.
    /// </summary>
    public readonly struct ResumenRanura
    {
        public int Ranura { get; }
        public bool Vacia { get; }
        /// <summary>Fecha local en que se guardó (la que se muestra).</summary>
        public DateTime Fecha { get; }
        /// <summary>Momento del guardado en UTC (para elegir la más reciente).</summary>
        public DateTime FechaUtc { get; }
        public string Zona { get; }
        public int Dinero { get; }
        public string Objetivo { get; }

        public ResumenRanura(int ranura, DateTime fecha, DateTime fechaUtc, string zona, int dinero, string objetivo)
        {
            Ranura = ranura;
            Vacia = false;
            Fecha = fecha;
            FechaUtc = fechaUtc;
            Zona = zona ?? string.Empty;
            Dinero = dinero;
            Objetivo = objetivo ?? string.Empty;
        }

        private ResumenRanura(int ranura)
        {
            Ranura = ranura;
            Vacia = true;
            Fecha = DateTime.MinValue;
            FechaUtc = DateTime.MinValue;
            Zona = string.Empty;
            Dinero = 0;
            Objetivo = string.Empty;
        }

        public static ResumenRanura SinPartida(int ranura) => new ResumenRanura(ranura);
    }
}

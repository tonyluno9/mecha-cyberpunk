using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;

namespace MechaCyberpunk.Progresion
{
    /// <summary>Lo que de verdad entró (o se perdió) al terminar una misión (T-184). Sirve a las pantallas y al HUD.</summary>
    public readonly struct ResumenMision
    {
        /// <summary>Dinero abonado ($).</summary>
        public readonly int Pago;
        /// <summary>Objeto de chatarra que entró (null si ninguno).</summary>
        public readonly DatosObjeto Chatarra;
        /// <summary>Unidades que entraron de verdad (ya recortadas al máximo).</summary>
        public readonly int ChatarraCantidad;
        /// <summary>Cambio real de la reputación del barrio (puntos).</summary>
        public readonly int DeltaBarrio;
        /// <summary>Cambio real de la reputación de las corporaciones (puntos).</summary>
        public readonly int DeltaCorporaciones;
        /// <summary>Favor que entró (null si ninguno).</summary>
        public readonly DatosFavor Favor;
        /// <summary>True si había un favor que dar pero el bolsillo estaba lleno.</summary>
        public readonly bool FavorPerdido;
        /// <summary>Por qué terminó mal; Ninguno si se cumplió.</summary>
        public readonly MotivoFalloMision Motivo;
        /// <summary>Aviso extra para pantallas (por ejemplo, favor perdido). Vacío si no hay.</summary>
        public readonly string Aviso;

        public ResumenMision(int pago, DatosObjeto chatarra, int chatarraCantidad, int deltaBarrio, int deltaCorporaciones,
            DatosFavor favor, bool favorPerdido, MotivoFalloMision motivo, string aviso)
        {
            Pago = pago;
            Chatarra = chatarra;
            ChatarraCantidad = chatarraCantidad;
            DeltaBarrio = deltaBarrio;
            DeltaCorporaciones = deltaCorporaciones;
            Favor = favor;
            FavorPerdido = favorPerdido;
            Motivo = motivo;
            Aviso = aviso ?? "";
        }
    }

    /// <summary>Una misión con la razón por la que se ofrece o no (T-184).</summary>
    public readonly struct OfertaMision
    {
        public readonly DatosMision Datos;
        public readonly MotivoMision Motivo;

        public OfertaMision(DatosMision datos, MotivoMision motivo)
        {
            Datos = datos;
            Motivo = motivo;
        }
    }
}

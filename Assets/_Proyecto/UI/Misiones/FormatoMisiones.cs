using System.Globalization;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.Progresion;

namespace MechaCyberpunk.UI.Misiones
{
    /// <summary>Lógica pura de los textos de las pantallas de misiones (T-186): filas, motivos, tiempo y barras.</summary>
    public static class FormatoMisiones
    {
        private const int OffsetNivel = 2;
        private const int SegundosPorMinuto = 60;

        /// <summary>
        /// "LA TIENDITA DE DOÑA LUPE · $50 · BARRIO +8 · CORP. 0 · FAVOR: DESCUENTO". El pago, la chatarra y el favor solo si los hay.
        /// </summary>
        public static string Fila(DatosMision datos, ConfigPantallaMisiones config)
        {
            string texto = datos.Titulo;
            if (datos.Pago > 0)
            {
                texto += config.Separador + Economia.FormatoDinero.Texto(datos.Pago);
            }
            if (datos.ChatarraObjeto != null && datos.ChatarraCantidad > 0)
            {
                texto += config.Separador + string.Format(config.FormatoChatarra, datos.ChatarraCantidad, datos.ChatarraObjeto.Nombre.ToUpperInvariant());
            }
            texto += config.Separador + string.Format(config.FormatoReputacion, FormatoHud.Firmado(datos.DeltaBarrio),
                FormatoHud.Firmado(datos.DeltaCorporaciones));
            if (datos.Favor != null)
            {
                texto += config.Separador + string.Format(config.FormatoFavor, datos.Favor.Nombre.ToUpperInvariant());
            }
            if (datos.ExigeFavor)
            {
                texto += config.Separador + string.Format(config.FormatoUsaFavor, config.NombreFavor((int)datos.FavorRequerido));
            }
            return texto;
        }

        /// <summary>"DESCUENTO   2/3   REBAJA 30 % UNA COMPRA   ·   MERCADO NEGRO": una línea de la pestaña FAVORES del teléfono.</summary>
        public static string FilaFavor(ConfigPantallaMisiones config, TipoFavor tipo, DatosFavor datos, int cantidad, int maximo)
        {
            string nombre = datos != null ? datos.Nombre.ToUpperInvariant() : config.NombreFavor((int)tipo);
            string descripcion = datos != null ? datos.Descripcion.ToUpperInvariant() : string.Empty;
            return string.Format(config.FormatoFilaFavor, nombre, cantidad, maximo, descripcion, config.DondeSeUsaFavor((int)tipo));
        }

        /// <summary>"FAVOR PERDIDO: YA TIENES 3".</summary>
        public static string AvisoFavorPerdido(ConfigPantallaMisiones config, int maximo)
        {
            return string.Format(config.FormatoFavorPerdido, maximo);
        }

        /// <summary>La fila de una misión que no se puede aceptar: "LIMPIEZA DE ZONA · NECESITAS SER CONTRATISTA CON LAS CORPORACIONES".</summary>
        public static string FilaNoDisponible(DatosMision datos, string motivo, ConfigPantallaMisiones config)
        {
            return string.Format(config.FormatoNoDisponible, datos.Titulo, motivo);
        }

        /// <summary>El motivo en palabras. <paramref name="reputacion"/> puede ser null: entonces dice solo "NECESITAS MÁS REPUTACIÓN".</summary>
        public static string Motivo(MotivoMision motivo, DatosMision datos, DatosReputacion reputacion, ConfigPantallaMisiones config)
        {
            switch (motivo)
            {
                case MotivoMision.FranjaEquivocada:
                    return datos.Franja == FranjaMision.Noche ? config.TextoDeNoche : config.TextoDeDia;
                case MotivoMision.BarrioNoTeHabla:
                    return config.TextoBarrioNoTeHabla;
                case MotivoMision.CorporacionesNoLlaman:
                    return config.TextoCorporacionesNoLlaman;
                case MotivoMision.ReputacionBaja:
                    return MotivoReputacion(datos, reputacion, config);
                case MotivoMision.LigaBaja:
                    return string.Format(config.FormatoLiga, datos.LigaMinima);
                case MotivoMision.FaltaFavor:
                    return string.Format(config.FormatoFaltaFavor, config.NombreFavor((int)datos.FavorRequerido));
                case MotivoMision.HechaHoy:
                    return config.TextoHechaHoy;
                case MotivoMision.YaHecha:
                    return config.TextoYaHecha;
                case MotivoMision.OtraActiva:
                    return config.TextoOtraActiva;
                case MotivoMision.FueraDeLibre:
                    return config.TextoFueraDeLibre;
                case MotivoMision.FaltanPuntos:
                    return config.TextoFaltanPuntos;
                default:
                    return string.Empty;
            }
        }

        private static string MotivoReputacion(DatosMision datos, DatosReputacion reputacion, ConfigPantallaMisiones config)
        {
            if (reputacion == null)
            {
                return config.TextoReputacionBaja;
            }
            bool barrio = datos.NivelBarrioMinimo > -OffsetNivel;
            string[] nombres = barrio ? reputacion.NombresBarrio : reputacion.NombresCorporaciones;
            int nivel = barrio ? datos.NivelBarrioMinimo : datos.NivelCorporacionesMinimo;
            int indice = nivel + OffsetNivel;
            if (nombres == null || indice < 0 || indice >= nombres.Length)
            {
                return config.TextoReputacionBaja;
            }
            return string.Format(barrio ? config.FormatoReputacionBarrio : config.FormatoReputacionCorporaciones, nombres[indice]);
        }

        /// <summary>"02:30" para 150 s. Redondea hacia arriba; nunca negativo.</summary>
        public static string Tiempo(float segundos)
        {
            int total = UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Max(0f, segundos));
            return (total / SegundosPorMinuto).ToString("00", CultureInfo.InvariantCulture) + ":"
                + (total % SegundosPorMinuto).ToString("00", CultureInfo.InvariantCulture);
        }

        /// <summary>Posición del valor en la barra de -100 a 100, de 0 a 1 (0.5 es cero).</summary>
        public static float PosicionBarra(int valor, int minimo, int maximo)
        {
            return maximo > minimo ? UnityEngine.Mathf.Clamp01((valor - minimo) / (float)(maximo - minimo)) : 0.5f;
        }
    }
}

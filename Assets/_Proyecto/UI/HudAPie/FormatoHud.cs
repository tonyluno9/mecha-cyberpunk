using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.UI;
using UnityEngine;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>Lógica pura del HUD a pie: textos y fracciones, sin objetos de escena.</summary>
    public static class FormatoHud
    {
        /// <summary>"VIDA 75". La vida se redondea hacia arriba para no mostrar 0 con vida.</summary>
        public static string TextoVida(float actual, ConfigHudAPie config)
        {
            return string.Format(config.TextoVida, Mathf.CeilToInt(Mathf.Max(0f, actual)));
        }

        /// <summary>Parte de la barra llena, siempre entre 0 y 1.</summary>
        public static float Fraccion(float actual, float maxima)
        {
            return maxima > 0f ? Mathf.Clamp01(actual / maxima) : 0f;
        }

        /// <summary>Vida baja: la fracción es igual o menor que el umbral (igual que la pantalla de la cabina).</summary>
        public static bool EsVidaBaja(float actual, float maxima, float umbral)
        {
            return Fraccion(actual, maxima) <= umbral;
        }

        private static readonly string[] Numerales = { string.Empty, " I", " II", " III" };

        /// <summary>
        /// T-403: con el nivel. "ESCOPETA II 5/6", "FUSIL III · RECARGANDO", "PUÑOS II". El nivel 0 no lleva numeral.
        /// </summary>
        public static string TextoArma(ArmaAPie arma, int nivel, int balas, int tamanoCargador, bool recargando, ConfigHudAPie config)
        {
            string numeral = Numerales[Mathf.Clamp(nivel, 0, Numerales.Length - 1)];
            if (arma == ArmaAPie.Punos)
            {
                return config.TextoPunos + numeral;
            }
            if (arma == ArmaAPie.Pistola && nivel <= 0)
            {
                return TextoArma(arma, balas, tamanoCargador, recargando, config);
            }
            string nombre = arma == ArmaAPie.Escopeta ? "ESCOPETA" : arma == ArmaAPie.Fusil ? "FUSIL" : arma == ArmaAPie.Sniper ? "SNIPER" : "PISTOLA";
            if (recargando)
            {
                return nombre + numeral + " · RECARGANDO";
            }
            string texto = nombre + numeral + " " + balas + "/" + tamanoCargador;
            return balas <= 0 ? texto + " · R" : texto;
        }

        /// <summary>"PUÑOS", "PISTOLA 7/8","PISTOLA · RECARGANDO" o "PISTOLA 0/8 · R".</summary>
        public static string TextoArma(ArmaAPie arma, int balas, int tamanoCargador, bool recargando, ConfigHudAPie config)
        {
            if (arma == ArmaAPie.Punos)
            {
                return config.TextoPunos;
            }
            if (recargando)
            {
                return config.TextoPistolaRecargando;
            }
            string texto = string.Format(config.TextoPistola, balas, tamanoCargador);
            return balas <= 0 ? texto + config.TextoPistolaSinBalas : texto;
        }

        /// <summary>"+$600 · PREMIO" o "-$650 · REPARACIÓN". Vacío si el motivo es <see cref="MotivoDinero.Carga"/> o no hubo cambio.</summary>
        public static string TextoAvisoDinero(int diferencia, MotivoDinero motivo, ConfigHudAPie config)
        {
            if (motivo == MotivoDinero.Carga || diferencia == 0)
            {
                return string.Empty;
            }
            string cantidad = FormatoDinero.Texto(diferencia);
            return (diferencia > 0 ? "+" : string.Empty) + cantidad + " · " + config.NombreMotivo(motivo);
        }

        /// <summary>
        /// Las dos líneas del aviso al vencer al campeón de <paramref name="liga"/>: "¡CAMPEÓN DE LA LIGA SÓTANO!" y "SE ABRE LA LIGA CALLE".
        /// En la última liga solo "¡CAMPEÓN DEL CLANDESTINO!" y la segunda línea vacía.
        /// </summary>
        public static (string primera, string segunda) AvisoLiga(int liga, IProgresoLigas ligas, ConfigHudAPie config)
        {
            if (liga >= ligas.CantidadLigas)
            {
                return (config.TextoCampeonClandestino, string.Empty);
            }
            return (string.Format(config.TextoCampeonLiga, ligas.NombreLiga(liga)),
                string.Format(config.TextoSeAbreLiga, ligas.NombreLiga(liga + 1)));
        }

        /// <summary>El signo menos de los avisos (U+2212).</summary>
        public const string SignoMenos = "−";

        /// <summary>"+8", "−5" (con el signo menos tipográfico) o "0".</summary>
        public static string Firmado(int valor)
        {
            if (valor > 0)
            {
                return "+" + valor;
            }
            return valor < 0 ? SignoMenos + (-valor) : "0";
        }

        /// <summary>
        /// "BARRIO +8 · CORPORACIONES −5". Solo entran las reputaciones que cambiaron; vacío si ninguna cambió.
        /// </summary>
        public static string TextoAvisoReputacion(int deltaBarrio, int deltaCorporaciones, ConfigHudAPie config)
        {
            string barrio = deltaBarrio != 0 ? string.Format(config.TextoReputacionBarrio, Firmado(deltaBarrio)) : string.Empty;
            string corporaciones = deltaCorporaciones != 0
                ? string.Format(config.TextoReputacionCorporaciones, Firmado(deltaCorporaciones)) : string.Empty;
            if (barrio.Length > 0 && corporaciones.Length > 0)
            {
                return barrio + config.SeparadorAviso + corporaciones;
            }
            return barrio + corporaciones;
        }

        /// <summary>"AHORA ERES CONOCIDO EN EL BARRIO" o "AHORA ERES SOCIO CON LAS CORPORACIONES".</summary>
        public static string TextoNivel(TipoReputacion tipo, string nombreNivel, ConfigHudAPie config)
        {
            string formato = tipo == TipoReputacion.Barrio ? config.TextoNivelBarrio : config.TextoNivelCorporaciones;
            return string.Format(formato, nombreNivel);
        }

        /// <summary>
        /// "MISIÓN CUMPLIDA · +$50 · DESCUENTO" (pago, chatarra y favor solo si los hubo) o "MISIÓN FALLIDA".
        /// </summary>
        public static string TextoMisionTerminada(bool cumplida, ResumenMision resumen, ConfigHudAPie config)
        {
            if (!cumplida)
            {
                return config.TextoMisionFallida;
            }
            string texto = config.TextoMisionCumplida;
            if (resumen.Pago > 0)
            {
                texto += config.SeparadorAviso + "+" + FormatoDinero.Texto(resumen.Pago);
            }
            if (resumen.Chatarra != null && resumen.ChatarraCantidad > 0)
            {
                texto += config.SeparadorAviso + string.Format(config.TextoChatarraMision, resumen.ChatarraCantidad,
                    resumen.Chatarra.Nombre.ToUpperInvariant());
            }
            if (resumen.Favor != null)
            {
                texto += config.SeparadorAviso + resumen.Favor.Nombre.ToUpperInvariant();
            }
            return texto;
        }

        /// <summary>"RENTA VENCIDA · $500".</summary>
        public static string TextoRentaVencida(int adeudo, ConfigHudAPie config)
        {
            return string.Format(config.TextoRentaVencida, FormatoDinero.Texto(adeudo));
        }
    }
}

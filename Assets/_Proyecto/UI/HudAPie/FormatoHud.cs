using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
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

        /// <summary>"PUÑOS", "PISTOLA 7/8", "PISTOLA · RECARGANDO" o "PISTOLA 0/8 · R".</summary>
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

        /// <summary>"RENTA VENCIDA · $500".</summary>
        public static string TextoRentaVencida(int adeudo, ConfigHudAPie config)
        {
            return string.Format(config.TextoRentaVencida, FormatoDinero.Texto(adeudo));
        }
    }
}

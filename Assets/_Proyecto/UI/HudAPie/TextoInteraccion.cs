using MechaCyberpunk.Core;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>Lógica pura de los textos de interacción del HUD: qué hace la E y por qué no abrió una puerta.</summary>
    public static class TextoInteraccion
    {
        /// <summary>
        /// "E · Abrir Bar" para una puerta y "E · Subir Carro de prueba" para un montable; vacío si no hay candidato.
        /// Sin nombre, solo "E · Abrir" o "E · Subir".
        /// </summary>
        public static string Candidato(TipoCandidatoInteraccion tipo, string nombre, ConfigHudAPie config)
        {
            string verbo;
            switch (tipo)
            {
                case TipoCandidatoInteraccion.Puerta:
                    verbo = config.TextoAbrir;
                    break;
                case TipoCandidatoInteraccion.Montable:
                    verbo = config.TextoSubir;
                    break;
                default:
                    return string.Empty;
            }
            return string.IsNullOrEmpty(nombre) ? verbo : verbo + " " + nombre;
        }

        /// <summary>Aviso al pulsar E dentro de un montable que todavía no puede dejar: "Frena para bajar".</summary>
        public static string AvisoNoPuedeBajar(ConfigHudAPie config)
        {
            return config.TextoFrenaParaBajar;
        }

        /// <summary>
        /// Aviso tras intentar abrir una puerta: "CERRADO · abre 20:00" por horario, "CERRADO · PRÓXIMAMENTE" si está bloqueada.
        /// Vacío si abrió o ya estaba abierta. Sin horario conocido, el cierre por horario también dice PRÓXIMAMENTE.
        /// </summary>
        public static string Aviso(ResultadoPuerta resultado, HorarioLugar? horario, ConfigHudAPie config)
        {
            switch (resultado)
            {
                case ResultadoPuerta.CerradaPorHorario:
                    return horario.HasValue
                        ? string.Format(config.TextoCerradoAbre, HorarioLugar.TextoHora(horario.Value.Abre))
                        : config.TextoCerradoProximamente;
                case ResultadoPuerta.Bloqueada:
                    return config.TextoCerradoProximamente;
                default:
                    return string.Empty;
            }
        }
    }
}

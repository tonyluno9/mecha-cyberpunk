using MechaCyberpunk.Combate.Lectura;
using MechaCyberpunk.IA.Datos;

namespace MechaCyberpunk.IA.Adaptacion
{
    /// <summary>Regla pura (T-199, D-tony-193): ¿se vio este hábito del jugador en la pelea? Los umbrales salen de DatosAdaptacionRival.</summary>
    public static class ReglaEstilo
    {
        private const float SegundosPorMinuto = 60f;
        private const float Tolerancia = 0.00001f;

        public static bool Vio(TipoAdaptacion tipo, PerfilEstilo perfil, float segundos, DatosAdaptacionRival datos)
        {
            if (perfil == null || datos == null || segundos < datos.SegundosMinimos || segundos <= 0f)
            {
                return false;
            }
            switch (tipo)
            {
                case TipoAdaptacion.Guardia:
                    return perfil.GolpesLanzados >= datos.GolpesLanzadosMinimos
                        && perfil.GolpesLanzados * SegundosPorMinuto / segundos >= datos.GolpesPorMinutoMinimo - Tolerancia;
                case TipoAdaptacion.Fintas:
                    return perfil.GolpesRival >= datos.GolpesRivalMinimos
                        && (float)perfil.GolpesRivalCubiertos / perfil.GolpesRival >= datos.FraccionCubiertaMinima - Tolerancia;
                case TipoAdaptacion.Retirada:
                    return perfil.GolpesConectados >= datos.GolpesConectadosMinimos
                        && (float)perfil.ConectadosCastigando / perfil.GolpesConectados >= datos.FraccionCastigoMinima - Tolerancia;
                default:
                    return false;
            }
        }
    }
}

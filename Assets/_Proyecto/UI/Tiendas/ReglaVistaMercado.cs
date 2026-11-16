using MechaCyberpunk.Personaje;
using UnityEngine;

namespace MechaCyberpunk.UI.Tiendas
{
    /// <summary>Una barra de la vista de El Mercado (M32): el número que se escribe y cuánto se llena (0 a 1).</summary>
    public readonly struct BarraMercado
    {
        public readonly string Etiqueta;
        public readonly int Valor;
        public readonly float Llenado;

        public BarraMercado(string etiqueta, int valor, float llenado)
        {
            Etiqueta = etiqueta;
            Valor = valor;
            Llenado = Mathf.Clamp01(llenado);
        }
    }

    /// <summary>
    /// T-1012: los números de las cuatro barras de El Mercado, como el mockup M32_TiendaArmas.png: DAÑO (de un disparo completo), CARGADOR (balas),
    /// CADENCIA (disparos por cada 10 s) y ALCANCE (m). El llenado de cada barra es el valor entre su máximo (los topes de abajo); lo que pasa se queda lleno.
    /// </summary>
    public static class ReglaVistaMercado
    {
        public const int Barras = 4;
        public const float DanioLleno = 100f;
        public const float CargadorLleno = 40f;
        public const float CadenciaLlena = 100f;
        public const float AlcanceLleno = 150f;
        private const float SegundosCadencia = 10f;

        public static readonly string[] Etiquetas = { "DAÑO", "CARGADOR", "CADENCIA", "ALCANCE" };

        /// <summary>Las cuatro barras del arma en ese nivel (vacías si no hay datos).</summary>
        public static BarraMercado[] De(DatosArma datos)
        {
            BarraMercado[] barras = new BarraMercado[Barras];
            float danio = datos != null ? datos.Danio * datos.Perdigones : 0f;
            int cargador = datos != null ? datos.Cargador : 0;
            float cadencia = datos != null && datos.TiempoEntreDisparos > 0f ? SegundosCadencia / datos.TiempoEntreDisparos : 0f;
            float alcance = datos != null ? datos.Alcance : 0f;
            barras[0] = new BarraMercado(Etiquetas[0], Mathf.RoundToInt(danio), danio / DanioLleno);
            barras[1] = new BarraMercado(Etiquetas[1], cargador, cargador / CargadorLleno);
            barras[2] = new BarraMercado(Etiquetas[2], Mathf.RoundToInt(cadencia), cadencia / CadenciaLlena);
            barras[3] = new BarraMercado(Etiquetas[3], Mathf.RoundToInt(alcance), alcance / AlcanceLleno);
            return barras;
        }
    }
}

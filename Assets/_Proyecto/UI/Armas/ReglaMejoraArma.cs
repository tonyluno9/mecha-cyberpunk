using System;
using System.Collections.Generic;
using MechaCyberpunk.Personaje;

namespace MechaCyberpunk.UI.Armas
{
    /// <summary>
    /// Las reglas del banco de armas de Tino (T-814), sin Unity: qué pieza sigue, cuáles están instaladas o bloqueadas, el precio y la
    /// tabla antes/después. Las piezas son los niveles del arma en orden fijo (cargador = I, cañón = II, mira = III).
    /// </summary>
    public static class ReglaMejoraArma
    {
        public const int NivelMaximo = 3;
        public const int CantidadPiezas = 3;

        private static readonly EstadisticaArma[] Orden =
        {
            EstadisticaArma.Danio, EstadisticaArma.Cargador, EstadisticaArma.Recarga, EstadisticaArma.Cadencia, EstadisticaArma.Dispersion
        };

        /// <summary>Las estadísticas en el orden de la tabla.</summary>
        public static IReadOnlyList<EstadisticaArma> OrdenEstadisticas => Orden;

        /// <summary>El nivel del arma que da la pieza (1 a 3).</summary>
        public static int NivelDe(PiezaArma pieza) => (int)pieza + 1;

        /// <summary>La pieza que da ese nivel (1 a 3).</summary>
        public static PiezaArma PiezaDeNivel(int nivel) => (PiezaArma)(Math.Max(1, Math.Min(NivelMaximo, nivel)) - 1);

        /// <summary>Estado de la pieza con el arma en <paramref name="nivelActual"/> (-1 = no se tiene el arma).</summary>
        public static EstadoPieza Estado(PiezaArma pieza, int nivelActual)
        {
            if (nivelActual < 0)
            {
                return EstadoPieza.Bloqueada;
            }
            int nivel = NivelDe(pieza);
            if (nivel <= nivelActual)
            {
                return EstadoPieza.Instalada;
            }
            return nivel == nivelActual + 1 ? EstadoPieza.Disponible : EstadoPieza.Bloqueada;
        }

        /// <summary>True si hay pieza que comprar (se tiene el arma y no está al máximo).</summary>
        public static bool HaySiguiente(int nivelActual) => nivelActual >= 0 && nivelActual < NivelMaximo;

        /// <summary>La pieza que sigue; si no hay (arma al máximo o sin arma), la última.</summary>
        public static PiezaArma Siguiente(int nivelActual) => HaySiguiente(nivelActual) ? PiezaDeNivel(nivelActual + 1) : PiezaArma.Mira;

        /// <summary>
        /// La pieza que hay que comprar antes de esta (para el aviso "primero el cargador"). Si la pieza no está bloqueada, ella misma.
        /// </summary>
        public static PiezaArma PiezaQueFalta(PiezaArma pieza, int nivelActual)
        {
            return Estado(pieza, nivelActual) == EstadoPieza.Bloqueada && nivelActual >= 0 ? Siguiente(nivelActual) : pieza;
        }

        /// <summary>
        /// Lo que cuesta un nivel: el precio de su oferta del mercado si tiene (lo que cobra la tienda), si no el de <see cref="DatosArma"/>.
        /// </summary>
        public static int Precio(int precioOferta, bool tieneOferta, int precioArma) => tieneOferta ? Math.Max(0, precioOferta) : Math.Max(0, precioArma);

        public static int Precio(DatosArma datos)
        {
            if (datos == null)
            {
                return 0;
            }
            return Precio(datos.Oferta != null ? datos.Oferta.Precio : 0, datos.Oferta != null, datos.Precio);
        }

        /// <summary>True si en esta estadística un número menor es mejor (recarga y dispersión).</summary>
        public static bool MenorEsMejor(EstadisticaArma estadistica) => estadistica == EstadisticaArma.Recarga || estadistica == EstadisticaArma.Dispersion;

        /// <summary>1 si <paramref name="despues"/> mejora, -1 si empeora, 0 si queda igual (con tolerancia de redondeo).</summary>
        public static int Cambio(EstadisticaArma estadistica, float antes, float despues)
        {
            const float Tolerancia = 0.0001f;
            float diferencia = despues - antes;
            if (Math.Abs(diferencia) < Tolerancia)
            {
                return 0;
            }
            bool sube = diferencia > 0f;
            return sube != MenorEsMejor(estadistica) ? 1 : -1;
        }

        /// <summary>La tabla antes/después en el orden de <see cref="OrdenEstadisticas"/>, con la estadística de la pieza resaltada.</summary>
        public static void Comparar(EstadisticasArma antes, EstadisticasArma despues, EstadisticaArma resaltada, List<FilaEstadistica> filas)
        {
            filas.Clear();
            foreach (EstadisticaArma estadistica in Orden)
            {
                float a = antes.Valor(estadistica);
                float d = despues.Valor(estadistica);
                filas.Add(new FilaEstadistica(estadistica, a, d, Cambio(estadistica, a, d), estadistica == resaltada));
            }
        }

        /// <summary>
        /// Lo lleno de la barra (0 a 1) de un valor contra la referencia (el nivel III del arma). En recarga y dispersión, menos llena más.
        /// </summary>
        public static float Fraccion(EstadisticaArma estadistica, float valor, float referencia)
        {
            if (MenorEsMejor(estadistica))
            {
                if (valor <= 0f)
                {
                    return 1f;
                }
                return Clamp01(referencia / valor);
            }
            return referencia <= 0f ? 0f : Clamp01(valor / referencia);
        }

        /// <summary>El número como se lee en la tabla (enteros sin decimales, tiempos y grados con uno).</summary>
        public static string Texto(EstadisticaArma estadistica, float valor)
        {
            System.Globalization.CultureInfo cultura = System.Globalization.CultureInfo.InvariantCulture;
            switch (estadistica)
            {
                case EstadisticaArma.Danio:
                case EstadisticaArma.Cargador:
                    return Math.Round(valor).ToString("0", cultura);
                case EstadisticaArma.Recarga:
                    return valor.ToString("0.0", cultura) + " s";
                case EstadisticaArma.Cadencia:
                    return valor.ToString("0.0", cultura) + "/s";
                default:
                    return valor.ToString("0.0", cultura) + "°";
            }
        }

        /// <summary>La diferencia con signo ("+5", "-0.2 s"); vacía si queda igual.</summary>
        public static string TextoDiferencia(EstadisticaArma estadistica, float antes, float despues)
        {
            float diferencia = despues - antes;
            if (Math.Abs(diferencia) < 0.0001f)
            {
                return string.Empty;
            }
            string numero = Texto(estadistica, Math.Abs(diferencia));
            return (diferencia > 0f ? "+" : "-") + numero;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}

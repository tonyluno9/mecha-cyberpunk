using System;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1108: patrón del jefe SNAKES (E11, mini jefe con cuchilla). Lógica pura, sin escena. Fases: Acecho (rodea al jugador de lado y cambia de
    /// sentido), tajos (aviso, 3 tajos seguidos, recuperación), estocada (aviso, embestida recta, recuperación castigable) y un llamado de refuerzos
    /// una sola vez al 50% de vida, que además acorta los avisos 25%. Nunca hace dos estocadas seguidas. La conducta traduce las fases a movimiento y daño.
    /// </summary>
    public sealed class ReglaJefeSnakes
    {
        public enum Fase { Acecho, AvisoTajos, Tajos, RecuperaTajos, AvisoEstocada, Estocada, RecuperaEstocada }

        public enum Ataque { Tajos, Estocada }

        /// <summary>Tiempos y distancias del patrón (los del diseño). Todos en m, s o m/s.</summary>
        public readonly struct Parametros
        {
            public readonly float RadioAcecho;
            public readonly float CirculoMinimo;
            public readonly float CambioSentidoMinimo;
            public readonly float CambioSentidoMaximo;
            public readonly float AlcanceTajos;
            public readonly float RadioDeseadoTajos;
            public readonly float RadioDeseadoEstocada;
            public readonly float AvisoTajos;
            public readonly int Tajos;
            public readonly float IntervaloTajo;
            public readonly float RecuperaTajos;
            public readonly float EstocadaMinima;
            public readonly float EstocadaMaxima;
            public readonly float AvisoEstocada;
            public readonly float DuracionEstocada;
            public readonly float RecuperaEstocada;
            public readonly float UmbralRefuerzos;
            public readonly float FactorAvisoRefuerzos;

            public Parametros(float radioAcecho, float circuloMinimo, float cambioSentidoMinimo, float cambioSentidoMaximo, float alcanceTajos,
                float radioDeseadoTajos, float radioDeseadoEstocada, float avisoTajos, int tajos, float intervaloTajo, float recuperaTajos,
                float estocadaMinima, float estocadaMaxima, float avisoEstocada, float duracionEstocada, float recuperaEstocada,
                float umbralRefuerzos, float factorAvisoRefuerzos)
            {
                RadioAcecho = radioAcecho;
                CirculoMinimo = circuloMinimo;
                CambioSentidoMinimo = cambioSentidoMinimo;
                CambioSentidoMaximo = cambioSentidoMaximo;
                AlcanceTajos = alcanceTajos;
                RadioDeseadoTajos = radioDeseadoTajos;
                RadioDeseadoEstocada = radioDeseadoEstocada;
                AvisoTajos = avisoTajos;
                Tajos = tajos;
                IntervaloTajo = intervaloTajo;
                RecuperaTajos = recuperaTajos;
                EstocadaMinima = estocadaMinima;
                EstocadaMaxima = estocadaMaxima;
                AvisoEstocada = avisoEstocada;
                DuracionEstocada = duracionEstocada;
                RecuperaEstocada = recuperaEstocada;
                UmbralRefuerzos = umbralRefuerzos;
                FactorAvisoRefuerzos = factorAvisoRefuerzos;
            }

            /// <summary>Los valores de docs/diseno/enemigos-nuevos-16nov.md, sección 2. La estocada se lanza de 5 a 7 m (no de 5 a 9): la embestida recorre 12 m/s x 0.5 s = 6 m y debe poder alcanzar a 1.2 m a quien no se mueve.</summary>
            public static Parametros Diseno => new Parametros(4f, 1f, 1.5f, 2.5f, 2.2f, 1.8f, 6f, 0.45f, 3, 0.25f, 0.9f, 5f, 7f, 0.6f, 0.5f, 1.4f, 0.5f, 0.75f);
        }

        public struct Entrada
        {
            /// <summary>Distancia en el plano al jugador (m).</summary>
            public float Distancia;
            /// <summary>Vida del jefe de 0 a 1.</summary>
            public float VidaFraccion;
            /// <summary>False si el jugador está fuera de su alcance de pelea o no se le puede pegar: el jefe vuelve a esperar.</summary>
            public bool EnPelea;
        }

        public struct Salida
        {
            /// <summary>Un tajo cae en este cuadro.</summary>
            public bool Tajo;
            /// <summary>Empieza el aviso de la estocada (la conducta fija la dirección y grita).</summary>
            public bool EmpezoAvisoEstocada;
            /// <summary>Empieza la embestida.</summary>
            public bool EmpezoEstocada;
            /// <summary>Al 50% de vida: llama los refuerzos (una sola vez).</summary>
            public bool LlamarRefuerzos;
        }

        private readonly Parametros p;
        private readonly Func<float> azar;
        private float tFase;
        private float tAcecho;
        private float tSentido;
        private int tajosHechos;
        private bool ultimaFueEstocada;
        private bool avisoEstocadaPendiente;

        public ReglaJefeSnakes(Parametros parametros, Func<float> azar)
        {
            p = parametros;
            this.azar = azar ?? (() => 0.5f);
            Reiniciar();
        }

        public Fase Actual { get; private set; }
        /// <summary>El ataque que prepara mientras acecha.</summary>
        public Ataque Planeado { get; private set; }
        /// <summary>+1 o -1: hacia qué lado rodea al jugador.</summary>
        public int Sentido { get; private set; } = 1;
        public bool RefuerzosLlamados { get; private set; }
        /// <summary>Se le pega x1.5 solo mientras se recupera de la estocada.</summary>
        public bool Castigable => Actual == Fase.RecuperaEstocada;
        public bool UltimaFueEstocada => ultimaFueEstocada;
        /// <summary>Multiplicador de los avisos: 1, o 0.75 después de llamar los refuerzos.</summary>
        public float FactorAviso => RefuerzosLlamados ? p.FactorAvisoRefuerzos : 1f;
        public float DuracionAvisoTajos => p.AvisoTajos * FactorAviso;
        public float DuracionAvisoEstocada => p.AvisoEstocada * FactorAviso;
        public float TiempoEnFase => tFase;

        /// <summary>Avance del aviso actual de 0 a 1 (0 fuera de un aviso).</summary>
        public float ProgresoAviso
        {
            get
            {
                switch (Actual)
                {
                    case Fase.AvisoTajos: return Math.Min(1f, tFase / DuracionAvisoTajos);
                    case Fase.AvisoEstocada: return Math.Min(1f, tFase / DuracionAvisoEstocada);
                    default: return 0f;
                }
            }
        }

        /// <summary>A qué distancia del jugador quiere estar el jefe mientras acecha (m).</summary>
        public float RadioDeseado(float distanciaActual)
        {
            if (tAcecho < p.CirculoMinimo)
            {
                return RadioCirculo(distanciaActual);
            }
            return Planeado == Ataque.Tajos ? p.RadioDeseadoTajos : p.RadioDeseadoEstocada;
        }

        private float RadioCirculo(float distanciaActual)
        {
            return Planeado == Ataque.Tajos
                ? Math.Min(distanciaActual, p.RadioAcecho)
                : Math.Max(p.EstocadaMinima + 0.5f, Math.Min(distanciaActual, p.EstocadaMaxima - 0.2f));
        }

        public void Reiniciar()
        {
            Actual = Fase.Acecho;
            tFase = 0f;
            tAcecho = 0f;
            tajosHechos = 0;
            ultimaFueEstocada = false;
            RefuerzosLlamados = false;
            avisoEstocadaPendiente = false;
            Sentido = azar() < 0.5f ? 1 : -1;
            tSentido = Lerp(p.CambioSentidoMinimo, p.CambioSentidoMaximo, azar());
            ElegirAtaque();
        }

        /// <summary>Un cuadro de <paramref name="dt"/> segundos.</summary>
        public Salida Avanzar(float dt, Entrada e)
        {
            Salida s = default;
            if (!RefuerzosLlamados && e.VidaFraccion <= p.UmbralRefuerzos)
            {
                RefuerzosLlamados = true;
                s.LlamarRefuerzos = true;
            }
            if (!e.EnPelea)
            {
                VolverAlAcecho();
                tAcecho = 0f;
                return s;
            }
            tFase += dt;
            switch (Actual)
            {
                case Fase.Acecho:
                    AvanzarAcecho(dt, e);
                    break;
                case Fase.AvisoTajos:
                    if (tFase >= DuracionAvisoTajos)
                    {
                        Actual = Fase.Tajos;
                        tFase = 0f;
                        tajosHechos = 1;
                        s.Tajo = true;
                        if (tajosHechos >= p.Tajos)
                        {
                            Actual = Fase.RecuperaTajos;
                        }
                    }
                    break;
                case Fase.Tajos:
                    if (tFase >= tajosHechos * p.IntervaloTajo)
                    {
                        tajosHechos++;
                        s.Tajo = true;
                        if (tajosHechos >= p.Tajos)
                        {
                            Actual = Fase.RecuperaTajos;
                            tFase = 0f;
                        }
                    }
                    break;
                case Fase.RecuperaTajos:
                    if (tFase >= p.RecuperaTajos)
                    {
                        ultimaFueEstocada = false;
                        VolverAlAcecho();
                    }
                    break;
                case Fase.AvisoEstocada:
                    if (tFase >= DuracionAvisoEstocada)
                    {
                        Actual = Fase.Estocada;
                        tFase = 0f;
                        s.EmpezoEstocada = true;
                    }
                    break;
                case Fase.Estocada:
                    if (tFase >= p.DuracionEstocada)
                    {
                        Actual = Fase.RecuperaEstocada;
                        tFase = 0f;
                    }
                    break;
                case Fase.RecuperaEstocada:
                    if (tFase >= p.RecuperaEstocada)
                    {
                        ultimaFueEstocada = true;
                        VolverAlAcecho();
                    }
                    break;
            }
            s.EmpezoAvisoEstocada = avisoEstocadaPendiente;
            avisoEstocadaPendiente = false;
            return s;
        }

        private void AvanzarAcecho(float dt, Entrada e)
        {
            tAcecho += dt;
            tSentido -= dt;
            if (tSentido <= 0f)
            {
                Sentido = -Sentido;
                tSentido = Lerp(p.CambioSentidoMinimo, p.CambioSentidoMaximo, azar());
            }
            if (e.Distancia <= p.AlcanceTajos && tAcecho >= p.CirculoMinimo * 0.4f)
            {
                // Lo tiene encima: tajos, sea cual sea el ataque que planeaba.
                Actual = Fase.AvisoTajos;
                tFase = 0f;
                return;
            }
            if (tAcecho < p.CirculoMinimo)
            {
                return;
            }
            if (Planeado == Ataque.Estocada && e.Distancia >= p.EstocadaMinima && e.Distancia <= p.EstocadaMaxima)
            {
                Actual = Fase.AvisoEstocada;
                tFase = 0f;
                avisoEstocadaPendiente = true;
            }
        }

        private void VolverAlAcecho()
        {
            Actual = Fase.Acecho;
            tFase = 0f;
            tAcecho = 0f;
            tajosHechos = 0;
            ElegirAtaque();
        }

        // Después de una estocada siempre toca tajos; si no, mitad y mitad.
        private void ElegirAtaque()
        {
            Planeado = ultimaFueEstocada || azar() < 0.5f ? Ataque.Tajos : Ataque.Estocada;
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}

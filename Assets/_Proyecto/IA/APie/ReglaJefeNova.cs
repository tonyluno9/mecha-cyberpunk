using System;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// El patrón del jefe de Nova (T-693), sin escena. Acecha y se acerca; a su alcance telegrafía el golpe (pausa y brillo), golpea en área corta y se
    /// queda recuperándose (ahí se le puede castigar). Cada <c>golpesPorCarga</c> golpes, desde lejos, telegrafía una carga, embiste en línea recta y
    /// tarda más en recuperarse. Recibe tiempo y distancia; devuelve qué hacer ahora.
    /// </summary>
    public sealed class ReglaJefeNova
    {
        /// <summary>Fase actual del patrón.</summary>
        public enum Fase
        {
            Acecho,
            TelegrafiaGolpe,
            Recupera,
            TelegrafiaCarga,
            Carga,
            RecuperaCarga
        }

        /// <summary>Qué hace con el cuerpo en este paso.</summary>
        public enum Orden
        {
            Quieto,
            Acercarse,
            Cargar
        }

        /// <summary>Lo que decide en este paso.</summary>
        public struct Salida
        {
            public Orden Orden;
            /// <summary>True en el paso en que su golpe de área conecta (si el jugador sigue a su alcance).</summary>
            public bool Impacto;
            /// <summary>True en el paso en que empieza a embestir: ahí se fija la dirección de la carga.</summary>
            public bool InicioCarga;
        }

        private readonly float alcance;
        private readonly float telegrafiaGolpe;
        private readonly float recuperaGolpe;
        private readonly int golpesPorCarga;
        private readonly float telegrafiaCarga;
        private readonly float duracionCarga;
        private readonly float recuperaCarga;
        private readonly float distanciaMinCarga;
        private readonly float distanciaMaxCarga;
        private float temporizador;

        public ReglaJefeNova(float alcance, float telegrafiaGolpe, float recuperaGolpe, int golpesPorCarga, float telegrafiaCarga,
            float duracionCarga, float recuperaCarga, float distanciaMinCarga, float distanciaMaxCarga)
        {
            this.alcance = alcance;
            this.telegrafiaGolpe = telegrafiaGolpe;
            this.recuperaGolpe = recuperaGolpe;
            this.golpesPorCarga = Math.Max(1, golpesPorCarga);
            this.telegrafiaCarga = telegrafiaCarga;
            this.duracionCarga = duracionCarga;
            this.recuperaCarga = recuperaCarga;
            this.distanciaMinCarga = distanciaMinCarga;
            this.distanciaMaxCarga = distanciaMaxCarga;
        }

        public Fase Actual { get; private set; } = Fase.Acecho;

        /// <summary>Golpes dados desde la última carga.</summary>
        public int GolpesDesdeCarga { get; private set; }

        /// <summary>True mientras se recupera: es el momento de castigarlo.</summary>
        public bool Vulnerable => Actual == Fase.Recupera || Actual == Fase.RecuperaCarga;

        /// <summary>True mientras avisa un golpe o una carga (pausa y brillo).</summary>
        public bool Telegrafiando => Actual == Fase.TelegrafiaGolpe || Actual == Fase.TelegrafiaCarga;

        /// <summary>Avance del aviso, de 0 a 1 (0 fuera de un aviso).</summary>
        public float ProgresoAviso
        {
            get
            {
                if (Actual == Fase.TelegrafiaGolpe) return Math.Min(1f, temporizador / telegrafiaGolpe);
                if (Actual == Fase.TelegrafiaCarga) return Math.Min(1f, temporizador / telegrafiaCarga);
                return 0f;
            }
        }

        /// <summary>True si el siguiente ataque será una carga.</summary>
        public bool TocaCarga => GolpesDesdeCarga >= golpesPorCarga;

        public void Reiniciar()
        {
            Actual = Fase.Acecho;
            temporizador = 0f;
            GolpesDesdeCarga = 0;
        }

        private void Ir(Fase fase)
        {
            Actual = fase;
            temporizador = 0f;
        }

        public Salida Avanzar(float dt, bool jugadorAtacable, float distancia)
        {
            Salida salida = new Salida { Orden = Orden.Quieto };
            if (!jugadorAtacable)
            {
                if (Actual != Fase.Acecho)
                {
                    Ir(Fase.Acecho);
                }
                return salida;
            }
            switch (Actual)
            {
                case Fase.Acecho:
                    if (TocaCarga && distancia >= distanciaMinCarga && distancia <= distanciaMaxCarga)
                    {
                        Ir(Fase.TelegrafiaCarga);
                    }
                    else if (distancia <= alcance)
                    {
                        Ir(Fase.TelegrafiaGolpe);
                    }
                    else
                    {
                        salida.Orden = Orden.Acercarse;
                    }
                    break;
                case Fase.TelegrafiaGolpe:
                    temporizador += dt;
                    if (temporizador >= telegrafiaGolpe)
                    {
                        salida.Impacto = true;
                        GolpesDesdeCarga++;
                        Ir(Fase.Recupera);
                    }
                    break;
                case Fase.Recupera:
                    temporizador += dt;
                    if (temporizador >= recuperaGolpe)
                    {
                        Ir(Fase.Acecho);
                    }
                    break;
                case Fase.TelegrafiaCarga:
                    temporizador += dt;
                    if (temporizador >= telegrafiaCarga)
                    {
                        salida.InicioCarga = true;
                        Ir(Fase.Carga);
                        salida.Orden = Orden.Cargar;
                    }
                    break;
                case Fase.Carga:
                    temporizador += dt;
                    salida.Orden = Orden.Cargar;
                    if (temporizador >= duracionCarga)
                    {
                        GolpesDesdeCarga = 0;
                        Ir(Fase.RecuperaCarga);
                        salida.Orden = Orden.Quieto;
                    }
                    break;
                case Fase.RecuperaCarga:
                    temporizador += dt;
                    if (temporizador >= recuperaCarga)
                    {
                        Ir(Fase.Acecho);
                    }
                    break;
            }
            return salida;
        }
    }
}

using System;
using System.Collections.Generic;

namespace MechaCyberpunk.Mundo.Reloj
{
    /// <summary>Tipo de aviso que produce el reloj al avanzar.</summary>
    public enum TipoEventoReloj
    {
        /// <summary>Pasó una hora entera: el valor es la hora (0 a 23).</summary>
        HoraEntera,

        /// <summary>Cambió la franja: el valor es 1 si ahora es de noche, 0 si es de día.</summary>
        Franja,

        /// <summary>Pasaron las 00:00: el valor es el número de día nuevo.</summary>
        Dia
    }

    /// <summary>Un aviso del reloj, en el orden en que ocurrió.</summary>
    public readonly struct EventoReloj
    {
        public readonly TipoEventoReloj Tipo;
        public readonly int Valor;

        public EventoReloj(TipoEventoReloj tipo, int valor)
        {
            Tipo = tipo;
            Valor = valor;
        }
    }

    /// <summary>
    /// Lógica pura del reloj del mundo (sin Unity): avanza con segundos reales a la velocidad de la franja en que está
    /// (un dt que cruza el amanecer o el anochecer avanza cada parte a su velocidad) y cuenta horas enteras, franjas y días.
    /// </summary>
    public class CalculoReloj
    {
        private const double Tolerancia = 1e-9;
        private const float HoraMaxima = 23.999998f;

        private readonly double amanecer;
        private readonly double anochecer;
        private readonly double horasPorSegundoDia;
        private readonly double horasPorSegundoNoche;
        private double hora;

        public CalculoReloj(float horaAmanecer, float horaAnochecer, float minutosDia, float minutosNoche, float horaInicial, int diaInicial)
        {
            if (!(horaAmanecer >= 0f && horaAnochecer > horaAmanecer && horaAnochecer < 24f))
            {
                throw new ArgumentException("Debe cumplirse 0 <= amanecer < anochecer < 24.");
            }
            if (minutosDia <= 0f || minutosNoche <= 0f)
            {
                throw new ArgumentException("Los minutos de día y de noche deben ser positivos.");
            }
            amanecer = horaAmanecer;
            anochecer = horaAnochecer;
            horasPorSegundoDia = (anochecer - amanecer) / (minutosDia * 60.0);
            horasPorSegundoNoche = (24.0 - (anochecer - amanecer)) / (minutosNoche * 60.0);
            hora = Normalizar(horaInicial);
            Dia = Math.Max(1, diaInicial);
        }

        /// <summary>Hora del día en [0, 24). Nunca vale 24.</summary>
        public float Hora => Math.Min((float)hora, HoraMaxima);

        /// <summary>Número de día, empieza en el día inicial y sube al pasar las 00:00.</summary>
        public int Dia { get; private set; }

        /// <summary>True si el reloj no avanza por sí solo con <see cref="Avanzar"/>.</summary>
        public bool Pausado { get; private set; }

        public float HoraAmanecer => (float)amanecer;
        public float HoraAnochecer => (float)anochecer;
        public bool EsDeNoche => EsNoche(hora);

        public void Pausar(bool pausado) => Pausado = pausado;

        /// <summary>
        /// Avanza <paramref name="segundos"/> reales y agrega a <paramref name="eventos"/> (sin vaciarla) lo que pasó, en orden.
        /// En pausa o con segundos no positivos no hace nada.
        /// </summary>
        public void Avanzar(float segundos, List<EventoReloj> eventos)
        {
            if (Pausado || segundos <= 0f)
            {
                return;
            }

            double restante = segundos;
            while (restante > 0.0)
            {
                bool noche = EsNoche(hora);
                double velocidad = noche ? horasPorSegundoNoche : horasPorSegundoDia;
                double limite = SiguienteLimite(hora);
                double segundosAlLimite = (limite - hora) / velocidad;

                if (segundosAlLimite > restante + Tolerancia)
                {
                    hora += restante * velocidad;
                    break;
                }

                restante -= Math.Max(segundosAlLimite, 0.0);
                hora = limite;
                if (hora >= 24.0 - Tolerancia)
                {
                    hora = 0.0;
                    Dia++;
                    eventos?.Add(new EventoReloj(TipoEventoReloj.Dia, Dia));
                }
                if (EsNoche(hora) != noche)
                {
                    eventos?.Add(new EventoReloj(TipoEventoReloj.Franja, EsNoche(hora) ? 1 : 0));
                }
                if (Math.Abs(hora - Math.Round(hora)) < Tolerancia)
                {
                    eventos?.Add(new EventoReloj(TipoEventoReloj.HoraEntera, (int)Math.Round(hora) % 24));
                }
            }
        }

        /// <summary>
        /// Salta hacia adelante hasta la próxima vez que el reloj marque esa hora (normalizada a [0, 24)): si ya pasó hoy,
        /// al día siguiente; si es la hora actual no cambia nada. Avisa un día por medianoche cruzada, una franja solo si
        /// cambió y una sola hora entera, la final. Funciona aunque esté en pausa.
        /// </summary>
        public void AvanzarHasta(float objetivo, List<EventoReloj> eventos)
        {
            double destino = Normalizar(objetivo);
            if (Math.Abs(destino - hora) < 1e-5)
            {
                return;
            }

            bool nocheAntes = EsNoche(hora);
            if (destino < hora)
            {
                Dia++;
                eventos?.Add(new EventoReloj(TipoEventoReloj.Dia, Dia));
            }
            hora = destino;
            if (EsNoche(hora) != nocheAntes)
            {
                eventos?.Add(new EventoReloj(TipoEventoReloj.Franja, EsNoche(hora) ? 1 : 0));
            }
            eventos?.Add(new EventoReloj(TipoEventoReloj.HoraEntera, (int)Math.Floor(hora)));
        }

        /// <summary>
        /// Pone el reloj en el día y la hora dados (la hora se normaliza a [0, 24)), como al cargar una partida. No avisa de día ni de hora
        /// entera: solo agrega el evento de franja si la franja cambió. Devuelve false, sin cambiar nada, si el día es menor que 1 o la hora no es un número.
        /// </summary>
        public bool Restaurar(int dia, float horaNueva, List<EventoReloj> eventos)
        {
            if (dia < 1 || float.IsNaN(horaNueva) || float.IsInfinity(horaNueva))
            {
                return false;
            }
            bool nocheAntes = EsNoche(hora);
            Dia = dia;
            hora = Normalizar(horaNueva);
            if (EsNoche(hora) != nocheAntes)
            {
                eventos?.Add(new EventoReloj(TipoEventoReloj.Franja, EsNoche(hora) ? 1 : 0));
            }
            return true;
        }

        private bool EsNoche(double h) => h < amanecer || h >= anochecer;

        // Siguiente hora entera o cambio de franja, lo que llegue primero (24 incluida).
        private double SiguienteLimite(double h)
        {
            double limite = Math.Floor(h + Tolerancia) + 1.0;
            if (amanecer > h + Tolerancia && amanecer < limite)
            {
                limite = amanecer;
            }
            if (anochecer > h + Tolerancia && anochecer < limite)
            {
                limite = anochecer;
            }
            return limite;
        }

        private static double Normalizar(float h)
        {
            double n = h % 24.0;
            if (n < 0.0)
            {
                n += 24.0;
            }
            return n;
        }
    }
}

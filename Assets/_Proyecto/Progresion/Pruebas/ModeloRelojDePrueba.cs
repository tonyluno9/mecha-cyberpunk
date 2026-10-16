using System;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.Progresion.Pruebas
{
    /// <summary>Lógica pura de horas del reloj de prueba de Tony (T-107). Cumple las reglas de avisos de <see cref="IRelojMundo"/>.</summary>
    public class ModeloRelojDePrueba
    {
        private const float HorasPorDia = 24f;
        private const float Margen = 0.0001f;

        public ModeloRelojDePrueba(float horaInicial = 21f, int diaInicial = 1, float horaAmanecer = 7f, float horaAnochecer = 19f)
        {
            HoraAmanecer = horaAmanecer;
            HoraAnochecer = horaAnochecer;
            Hora = Normalizar(horaInicial);
            Dia = Math.Max(1, diaInicial);
        }

        public float Hora { get; private set; }
        public int Dia { get; private set; }
        public float HoraAmanecer { get; }
        public float HoraAnochecer { get; }
        public bool Pausado { get; private set; }
        public FranjaDia Franja => FranjaDeHora(Hora);
        public bool EsDeNoche => Franja == FranjaDia.Noche;

        public event Action<int> HoraEnteraCambio;
        public event Action<FranjaDia> FranjaCambio;
        public event Action<int> DiaCambio;

        public void Pausar(bool pausado) => Pausado = pausado;

        /// <summary>Avanza horas de juego (sin mirar la pausa). Avisa cada hora entera cruzada, en orden.</summary>
        public void Avanzar(float horas)
        {
            if (horas <= 0f) return;
            float restante = horas;
            while (true)
            {
                float siguiente = (float)Math.Floor(Hora) + 1f;
                float falta = siguiente - Hora;
                if (restante < falta)
                {
                    Hora = Math.Min(Hora + restante, siguiente - Margen);
                    break;
                }
                restante -= falta;
                FranjaDia antes = Franja;
                Hora = siguiente;
                if (Hora >= HorasPorDia)
                {
                    Hora = 0f;
                    Dia++;
                    DiaCambio?.Invoke(Dia);
                }
                HoraEnteraCambio?.Invoke((int)Hora);
                if (Franja != antes) FranjaCambio?.Invoke(Franja);
            }
        }

        /// <summary>Salta a la próxima vez que marque esa hora. Ver <see cref="IRelojMundo.AvanzarHasta"/>.</summary>
        public void AvanzarHasta(float hora)
        {
            hora = Normalizar(hora);
            float delta = hora - Hora;
            if (Math.Abs(delta) < 1e-6f) return;
            if (delta < 0f) delta += HorasPorDia;
            FranjaDia antes = Franja;
            int medianoches = (int)Math.Floor((Hora + delta) / HorasPorDia);
            Hora = hora;
            for (int i = 0; i < medianoches; i++)
            {
                Dia++;
                DiaCambio?.Invoke(Dia);
            }
            HoraEnteraCambio?.Invoke((int)Math.Floor(Hora));
            if (Franja != antes) FranjaCambio?.Invoke(Franja);
        }

        /// <summary>Fija día y hora (carga de partida): no avisa día ni hora entera; sí la franja si cambió.</summary>
        public void Restablecer(int dia, float hora)
        {
            FranjaDia antes = Franja;
            Dia = Math.Max(1, dia);
            Hora = Normalizar(hora);
            if (Franja != antes) FranjaCambio?.Invoke(Franja);
        }

        private FranjaDia FranjaDeHora(float hora) => hora >= HoraAmanecer && hora < HoraAnochecer ? FranjaDia.Dia : FranjaDia.Noche;

        private static float Normalizar(float hora)
        {
            float h = hora % HorasPorDia;
            if (h < 0f) h += HorasPorDia;
            return h >= HorasPorDia ? 0f : h;
        }
    }
}

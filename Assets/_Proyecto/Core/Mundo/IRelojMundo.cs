using System;

namespace MechaCyberpunk.Core
{
    /// <summary>
    /// El reloj del mundo: hora, día y franja (día de 20 minutos reales: 12 de noche y 8 de día).
    /// Lo implementa Alan (Mundo/), una vez por escena, y lo agrega a <c>Registro&lt;IRelojMundo&gt;</c>.
    /// Lo leen Tony (renta cada 7 días, misiones, HUD) y las herramientas de prueba.
    /// Las horas de amanecer y anochecer son datos de Alan.
    /// </summary>
    public interface IRelojMundo
    {
        /// <summary>Hora del día en [0, 24) con decimales (21.5 es 21:30). Nunca vale 24.</summary>
        float Hora { get; }

        /// <summary>Número de día, empieza en 1 y sube al pasar las 00:00.</summary>
        int Dia { get; }

        /// <summary>Franja actual. Es Dia si <c>Hora</c> está en [HoraAmanecer, HoraAnochecer); si no, Noche.</summary>
        FranjaDia Franja { get; }

        /// <summary>True si <c>Franja</c> es Noche.</summary>
        bool EsDeNoche { get; }

        /// <summary>Hora a la que empieza el día (h).</summary>
        float HoraAmanecer { get; }

        /// <summary>Hora a la que empieza la noche (h).</summary>
        float HoraAnochecer { get; }

        /// <summary>True si el reloj no avanza por sí solo. <see cref="AvanzarHasta"/> sigue funcionando.</summary>
        bool Pausado { get; }

        /// <summary>
        /// Avisa cada hora entera que pasa (el argumento es la hora, de 0 a 23), en orden. Si un frame largo
        /// pasa varias horas, avisa una vez por cada una, sin saltarse ninguna.
        /// </summary>
        event Action<int> HoraEnteraCambio;

        /// <summary>Avisa con la franja nueva cuando cambia de Dia a Noche o al revés.</summary>
        event Action<FranjaDia> FranjaCambio;

        /// <summary>Avisa con el número de día nuevo al pasar las 00:00.</summary>
        event Action<int> DiaCambio;

        /// <summary>Pausa (true) o reanuda (false) el avance del reloj. Pausar dos veces seguidas no hace nada extra.</summary>
        void Pausar(bool pausado);

        /// <summary>
        /// Salta hacia adelante hasta la próxima vez que el reloj marque esa hora (la hora se normaliza a [0, 24)):
        /// si ya pasó hoy, al día siguiente; si es la hora actual, no cambia nada y no avisa nada.
        /// Avisa <c>HoraEnteraCambio</c> una sola vez, con la hora entera final; <c>DiaCambio</c> una vez por cada
        /// medianoche cruzada; y <c>FranjaCambio</c> solo si la franja final es distinta de la inicial.
        /// Sirve para dormir (T-F5) y para las herramientas de prueba.
        /// </summary>
        void AvanzarHasta(float hora);
    }
}

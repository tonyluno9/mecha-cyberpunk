using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>Eventos globales de la pelea, para que las áreas se hablen sin referenciarse.</summary>
    public static class EventosPelea
    {
        /// <summary>Cuenta regresiva: 3, 2, 1 y 0, donde 0 significa "¡pelea!".</summary>
        public static event Action<int> ConteoCambio;
        /// <summary>
        /// Qué pelea va a empezar. Lo publica el flujo de pelea de Tony una vez por pelea (también en cada revancha), justo antes
        /// de la presentación del presentador o, si no hay, del conteo. Sirve para que el mundo acompañe (por ejemplo, cambiar las
        /// luces de la jaula si <see cref="InfoPelea.EsJefe"/>).
        /// </summary>
        public static event Action<InfoPelea> PeleaPresentada;
        public static event Action PeleaIniciada;
        public static event Action<ResultadoPelea> PeleaTerminada;
        /// <summary>Objetivo, daño pedido y resultado. El objetivo es null si quien recibió el daño no es un <see cref="IPeleador"/> (personas a pie, muñecos); tolera null.</summary>
        public static event Action<IPeleador, InfoDanio, ResultadoDanio> ImpactoRecibido;

        public static void PublicarConteo(int numero) => ConteoCambio?.Invoke(numero);
        public static void PublicarPresentacion(InfoPelea info) => PeleaPresentada?.Invoke(info);
        public static void PublicarInicio() => PeleaIniciada?.Invoke();
        public static void PublicarFin(ResultadoPelea resultado) => PeleaTerminada?.Invoke(resultado);

        public static void PublicarImpacto(IPeleador objetivo, InfoDanio info, ResultadoDanio resultado)
            => ImpactoRecibido?.Invoke(objetivo, info, resultado);

        /// <summary>Quita a todos los suscriptores. Para pruebas y para limpiar al entrar a Play.</summary>
        public static void LimpiarSuscriptores()
        {
            ConteoCambio = null;
            PeleaPresentada = null;
            PeleaIniciada = null;
            PeleaTerminada = null;
            ImpactoRecibido = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => LimpiarSuscriptores();
    }
}

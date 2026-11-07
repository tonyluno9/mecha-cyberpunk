using UnityEngine;

namespace MechaCyberpunk.Personaje.Vida
{
    /// <summary>
    /// Quemarse (T-691), sin escena: el fuego hace daño por tick mientras dura y se apaga solo a los <see cref="SegundosDuracion"/> segundos,
    /// antes si el jugador rueda, y antes si se aleja más de <see cref="DistanciaApagado"/> m del lugar donde se prendió (salió del área en llamas).
    /// Nunca daña sin fin: el daño total máximo es <see cref="DanioTotalMaximo"/>.
    /// </summary>
    public sealed class ReglaQuemadura
    {
        /// <summary>Por qué se apagó la última vez.</summary>
        public enum Apagado
        {
            Ninguno,
            Tiempo,
            Rodar,
            SalioDelArea,
            Forzado
        }

        public const float SegundosDuracion = 4f;
        public const float DanioPorSegundo = 5f;
        public const float SegundosPorTick = 0.5f;
        public const float DistanciaApagado = 5f;
        public const float DanioTotalMaximo = SegundosDuracion * DanioPorSegundo;

        private float restante;
        private float acumulado;
        private Vector3 origen;

        public bool Ardiendo => restante > 0f;
        public float Restante => restante;
        public Apagado UltimoApagado { get; private set; }

        /// <summary>Prende (o renueva el tiempo) con el origen del fuego, donde se mide si sales del área.</summary>
        public void Prender(Vector3 puntoOrigen)
        {
            if (!Ardiendo)
            {
                acumulado = 0f;
            }
            restante = SegundosDuracion;
            origen = puntoOrigen;
            UltimoApagado = Apagado.Ninguno;
        }

        public void Apagar(Apagado motivo = Apagado.Forzado)
        {
            if (Ardiendo)
            {
                UltimoApagado = motivo;
            }
            restante = 0f;
            acumulado = 0f;
        }

        /// <summary>Avanza el fuego y devuelve el daño que toca aplicar ahora (0 casi siempre; un tick cada medio segundo).</summary>
        public float Avanzar(float dt, bool rodando, Vector3 posicionJugador)
        {
            if (!Ardiendo)
            {
                return 0f;
            }
            if (rodando)
            {
                Apagar(Apagado.Rodar);
                return 0f;
            }
            Vector3 d = posicionJugador - origen;
            d.y = 0f;
            if (d.magnitude > DistanciaApagado)
            {
                Apagar(Apagado.SalioDelArea);
                return 0f;
            }
            float paso = Mathf.Min(dt, restante);
            restante -= dt;
            acumulado += paso;
            float danio = 0f;
            while (acumulado >= SegundosPorTick)
            {
                acumulado -= SegundosPorTick;
                danio += DanioPorSegundo * SegundosPorTick;
            }
            if (restante <= 0f)
            {
                restante = 0f;
                acumulado = 0f;
                UltimoApagado = Apagado.Tiempo;
            }
            return danio;
        }
    }
}

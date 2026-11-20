using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1528: números y reglas puras de los maleantes de calle (SKULLS y SNAKES ambientales, de riña o de acoso). Primero provocan (se acercan, gritan y empujan casi sin daño)
    /// y solo atacan si el jugador responde (les pega, saca el arma o dispara cerca). Ya atacando, el daño, la precisión y la cadencia empiezan limitados y suben con el tiempo.
    /// No aplica a guion de misión ni a Nova (se activa con <c>DatosPandilleroAPie.ProvocaAntesDeAtacar</c>).
    /// </summary>
    public static class ReglaProvocacionCalle
    {
        /// <summary>Se queda a esta distancia del jugador provocando (m).</summary>
        public const float DistanciaProvocar = 2.4f;
        /// <summary>Hasta aquí empuja (m).</summary>
        public const float DistanciaEmpujon = 3.2f;
        /// <summary>Camina (no corre) hacia el jugador a esta velocidad máxima (m/s).</summary>
        public const float VelocidadAcercamiento = 2.2f;
        /// <summary>Daño del empujón (casi nada).</summary>
        public const float DanioEmpujon = 1f;
        public const float EmpujonMinimo = 3f;
        public const float EmpujonMaximo = 5.5f;
        /// <summary>Una respuesta del jugador (sacar el arma o disparar) cuenta si ocurre a menos de esto del maleante (m).</summary>
        public const float RadioRespuesta = 30f;
        /// <summary>El disparo o el arma sacada debe venir de menos de esto del jugador (m).</summary>
        public const float CercaniaJugador = 3f;
        /// <summary>Sin alerta durante tanto, se calma y vuelve a provocar antes de atacar (s).</summary>
        public const float SegundosCalma = 15f;
        /// <summary>Tiempo atacando hasta llegar al daño, precisión y cadencia completos (s).</summary>
        public const float SegundosRampa = 90f;
        public const float EscalaDanioInicial = 0.35f;
        public const float EscalaPrecisionInicial = 0.5f;
        /// <summary>Segundos extra entre ráfagas al empezar a atacar.</summary>
        public const float RecargaExtraInicial = 1.6f;

        public static readonly string[] Gritos =
        {
            "¿Qué onda, bro?", "¿Qué ves?", "Esta es mi calle", "Lárgate de aquí", "¿Tienes algún problema?"
        };

        private static float Avance(float segundosAtacando) => Mathf.Clamp01(segundosAtacando / SegundosRampa);

        public static float EscalaDanio(float segundosAtacando) => Mathf.Lerp(EscalaDanioInicial, 1f, Avance(segundosAtacando));

        public static float EscalaPrecision(float segundosAtacando) => Mathf.Lerp(EscalaPrecisionInicial, 1f, Avance(segundosAtacando));

        public static float RecargaExtra(float segundosAtacando) => Mathf.Lerp(RecargaExtraInicial, 0f, Avance(segundosAtacando));

        /// <summary>True si un aviso del jugador (arma sacada o disparo en <paramref name="puntoEvento"/>) es una respuesta que debe ver este maleante.</summary>
        public static bool EsRespuesta(Vector3 puntoEvento, Vector3 jugador, Vector3 maleante)
        {
            return Plano(puntoEvento, jugador) <= CercaniaJugador && Plano(maleante, jugador) <= RadioRespuesta;
        }

        private static float Plano(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}

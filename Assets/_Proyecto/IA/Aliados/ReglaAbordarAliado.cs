using UnityEngine;

namespace MechaCyberpunk.IA.Aliados
{
    /// <summary>
    /// Reglas puras de cuándo y por dónde el aliado se sube a tu carro (T-706). Se sube por la puerta del copiloto (el espejo de la del conductor)
    /// caminando hasta ella, no teletransportándose; solo si va cerca, el carro está casi quieto y hay lugar. En la moto no sube.
    /// </summary>
    public static class ReglaAbordarAliado
    {
        /// <summary>A más distancia del carro que esto (m) ya no lo alcanza: te sigue a pie.</summary>
        public const float DistanciaMaxima = 14f;
        /// <summary>Con el carro a más rapidez que esto (m/s) no empieza a subir.</summary>
        public const float RapidezMaximaParaSubir = 3f;
        /// <summary>Si mientras camina a la puerta el carro pasa de esta rapidez (m/s), se rinde y te sigue a pie.</summary>
        public const float RapidezParaRendirse = 5f;
        /// <summary>Lo más que tarda en llegar a la puerta (s); después se rinde.</summary>
        public const float SegundosMaximosAcercando = 7f;
        /// <summary>Distancia a la puerta (m) desde la que empieza el clip de subir.</summary>
        public const float DistanciaPuerta = 0.7f;
        /// <summary>Cuánto se arrima el punto de la puerta hacia el asiento (m), igual que el del jugador.</summary>
        public const float RetiroPuerta = 0.8f;
        /// <summary>Cuánto cae el modelo sentado respecto del punto del asiento (m): ahí van sus pies.</summary>
        public const float CaidaAlAsiento = 0.55f;
        /// <summary>Hacia fuera del carro, a esta distancia de la puerta (m), queda al bajar.</summary>
        public const float SalidaDePuerta = 0.9f;

        /// <summary>True si el aliado puede empezar a subirse a ese carro.</summary>
        public static bool PuedeSubir(bool siguiendo, bool esMoto, bool hayLugar, float distanciaAlCarro, float rapidezDelCarro)
        {
            return siguiendo && !esMoto && hayLugar && distanciaAlCarro <= DistanciaMaxima && rapidezDelCarro <= RapidezMaximaParaSubir;
        }

        /// <summary>True si debe rendirse y seguirte a pie: el carro arrancó o ya tardó demasiado.</summary>
        public static bool DebeRendirse(float rapidezDelCarro, float segundosAcercando)
        {
            return rapidezDelCarro > RapidezParaRendirse || segundosAcercando > SegundosMaximosAcercando;
        }

        /// <summary>El punto junto a la puerta del copiloto: el espejo del punto de subida del conductor respecto del plano central del carro.</summary>
        public static Vector3 PuertaCopiloto(Vector3 puntoSubida, Transform marco)
        {
            return Espejar(puntoSubida, marco);
        }

        /// <summary>Dónde van los pies del aliado sentado en el asiento del copiloto: el espejo del asiento, más abajo.</summary>
        public static Vector3 AsientoCopiloto(Vector3 asiento, Transform marco)
        {
            return Espejar(asiento, marco) + Vector3.down * CaidaAlAsiento;
        }

        /// <summary>Arrima el punto de la puerta hacia el asiento (horizontal): queda pegado a la carrocería.</summary>
        public static Vector3 PuntoPuerta(Vector3 puerta, Vector3 asiento)
        {
            Vector3 hacia = asiento - puerta;
            hacia.y = 0f;
            float distancia = hacia.magnitude;
            return distancia < 1e-4f ? puerta : puerta + hacia / distancia * Mathf.Min(RetiroPuerta, distancia);
        }

        /// <summary>Un punto fuera del carro: la puerta más <see cref="SalidaDePuerta"/> m hacia afuera (lejos del asiento).</summary>
        public static Vector3 PuntoSalida(Vector3 puerta, Vector3 asiento)
        {
            Vector3 afuera = puerta - asiento;
            afuera.y = 0f;
            return afuera.sqrMagnitude < 1e-6f ? puerta : puerta + afuera.normalized * SalidaDePuerta;
        }

        private static Vector3 Espejar(Vector3 punto, Transform marco)
        {
            Vector3 local = marco.InverseTransformPoint(punto);
            local.x = -local.x;
            return marco.TransformPoint(local);
        }
    }
}

using System;

namespace MechaCyberpunk.Vehiculos
{
    /// <summary>
    /// T-1534: reglas puras del sonido del carro según lo que hay bajo las llantas y de lado. La superficie sale de los nombres del piso (material físico, material,
    /// collider y objeto) igual que los pasos del jugador. Grava y tierra suenan VEH_LLANTAS_GRAVA en bucle mientras el carro rueda; una junta de puente suena VEH_PUENTE
    /// una vez al entrar; un raspón de lado suena VEH_CHOQUE (LEEME_SONIDOS: el contacto con el carro usa los VEH_CHOQUE_*).
    /// </summary>
    public static class ReglaSueloCarro
    {
        public const string IdLlantasGrava = "VEH_LLANTAS_GRAVA";
        public const string IdPuente = "VEH_PUENTE";
        public const string IdRaspon = ReglasSonidoVehiculoT1475.IdChoque;

        /// <summary>Rapidez (m/s) desde la que ruedan sobre grava: más lento no se oye.</summary>
        public const float RapidezMinimaGrava = 2f;
        /// <summary>Rapidez (m/s) por debajo de la que el bucle de grava se corta (histéresis).</summary>
        public const float RapidezCortaGrava = 1f;
        /// <summary>Rapidez (m/s) mínima para que una junta de puente suene.</summary>
        public const float RapidezMinimaPuente = 4f;
        public const float SegundosEntrePuente = 1.5f;
        /// <summary>Rapidez (m/s) mínima del carro para un raspón y tiempo mínimo entre uno y otro.</summary>
        public const float RapidezMinimaRaspon = 4f;
        public const float SegundosEntreRaspon = 1.2f;
        /// <summary>Componente (0 a 1) de la normal del contacto sobre el costado del carro desde la que el golpe es de lado.</summary>
        public const float LateralMinimoRaspon = 0.7f;
        /// <summary>Velocidad (m/s) máxima contra el contacto (a lo largo de su normal) para que sea un roce y no un golpe.</summary>
        public const float VelocidadNormalMaximaRaspon = 2.5f;

        private static readonly string[] PalabrasGrava = { "grava", "gravel", "tierra", "dirt", "baldio", "terreno", "lote", "obra", "escombro", "rubble", "balasto", "deshuesadero" };
        private static readonly string[] PalabrasPuente = { "puente", "junta", "expansion", "viaducto", "bridge" };

        /// <summary>Si el piso es de grava o tierra suelta (suena VEH_LLANTAS_GRAVA).</summary>
        public static bool EsGrava(string nombrePiso) => Contiene(nombrePiso, PalabrasGrava);

        /// <summary>Si el piso es una junta o un tramo de puente (suena VEH_PUENTE al entrar).</summary>
        public static bool EsPuente(string nombrePiso) => Contiene(nombrePiso, PalabrasPuente);

        /// <summary>El bucle de grava suena con piso de grava y rapidez suficiente; con histéresis si ya sonaba.</summary>
        public static bool SuenaGrava(bool pisoGrava, float rapidez, bool yaSonaba)
        {
            return pisoGrava && Math.Abs(rapidez) >= (yaSonaba ? RapidezCortaGrava : RapidezMinimaGrava);
        }

        /// <summary>Suena VEH_PUENTE al pasar de un piso que no es puente a uno que sí, yendo a buena rapidez.</summary>
        public static bool SuenaPuente(bool eraPuente, bool esPuente, float rapidez, float segundosDesdeUltimo)
        {
            return !eraPuente && esPuente && Math.Abs(rapidez) >= RapidezMinimaPuente && segundosDesdeUltimo >= SegundosEntrePuente;
        }

        /// <summary>
        /// Raspón: el carro rueda a cierta rapidez y roza una pared o un carro con el costado: la normal del contacto cae sobre el costado del carro
        /// (<paramref name="lateralNormal"/>, valor absoluto de normal . derecha del carro) y casi no se mete contra él (<paramref name="velocidadNormal"/>, m/s).
        /// </summary>
        public static bool EsRaspon(float rapidezCarro, float lateralNormal, float velocidadNormal, float segundosDesdeUltimo)
        {
            return rapidezCarro >= RapidezMinimaRaspon && segundosDesdeUltimo >= SegundosEntreRaspon
                && Math.Abs(lateralNormal) >= LateralMinimoRaspon && Math.Abs(velocidadNormal) <= VelocidadNormalMaximaRaspon;
        }

        private static bool Contiene(string texto, string[] palabras)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return false;
            }
            foreach (string p in palabras)
            {
                if (texto.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}

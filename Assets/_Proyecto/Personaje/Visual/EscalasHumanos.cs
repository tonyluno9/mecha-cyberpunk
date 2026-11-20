using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-1521: la altura animada (m, en pose de reposo, medida con la malla horneada y no con los bounds) a la que debe quedar cada humano de la ciudad
    /// (doctor, inventor, hacker, mecánico, meseras, civiles, pandilleros, policías) para que midan lo mismo que el jugador (~1.75 m ± 0.1).
    /// Una sola tabla: la leen el editor que escala los prefabs (EscalaHumanosT1521) y las pruebas. Los robots y gigantes de Nova (E15, E16, E18, el perro E17,
    /// los guardias GuardiaNova) y los jefes y pesados de armadura (E11, E12, E14) no están: conservan su tamaño de diseño.
    /// </summary>
    public static class EscalasHumanos
    {
        /// <summary>Altura de referencia del jugador animado (m; la mide EscalaJugadorT670Pruebas).</summary>
        public const float AlturaJugador = 1.80f;
        /// <summary>Un humano por debajo de esto es enano contra el jugador (m).</summary>
        public const float AlturaMinima = 1.65f;
        /// <summary>Un humano por encima de esto es gigante contra el jugador (m).</summary>
        public const float AlturaMaxima = 1.85f;
        /// <summary>Si la altura medida ya está a esta distancia del objetivo, no se vuelve a escalar (m). Hace repetible a Aplicar.</summary>
        public const float ToleranciaAplicar = 0.03f;
        /// <summary>Margen con el que las pruebas aceptan la altura medida (m).</summary>
        public const float ToleranciaPrueba = 0.08f;
        /// <summary>Una corrección que pida más que esto (o menos que su inverso) es señal de que la medida salió mal y no se aplica.</summary>
        public const float FactorMaximo = 2f;
        /// <summary>Altura medida por debajo de la cual no se confía en la medición (m).</summary>
        public const float AlturaMedidaMinimaConfiable = 0.3f;

        private const string PrefijoVisual = "Visual_";
        private const float AlturaGeneral = 1.76f;

        private static readonly (string visual, float altura)[] Tabla =
        {
            ("Visual_Doctor", 1.80f),          // el Doc ya no es más alto que el jugador (el 1.09 de T-812 sale)
            ("Visual_Inventor", AlturaGeneral),
            ("Visual_Hacker", AlturaGeneral),
            ("Visual_Mecanico", AlturaGeneral),
            ("Visual_MeseraRobot", 1.70f),     // la mesera del casino y las del mundo
            ("Visual_man", 1.80f),             // pandillero y vendedor
            ("Visual_woman", 1.74f),
            ("Visual_woman2", 1.74f),
            ("Visual_RobotHombre", 1.78f),     // peatón robot (1 de cada 4)
            ("Visual_SkullHombre", 1.80f),     // pandillero SKULLS y su jefe
            ("Visual_Policia", 1.80f),
            ("Visual_E10_PandilleroSnakes", 1.78f),
            ("Visual_E19_PandilleroSkulls_a_2", 1.78f),
            ("Visual_E19_PandilleroSkulls_b", 1.78f),
            ("Visual_E20_MatonSkulls_a", 1.82f),
            ("Visual_E20_MatonSkulls_b", 1.82f),
            ("Visual_NP07_Pescador_a", AlturaGeneral),
            ("Visual_NP07_Pescador_b", AlturaGeneral),
            ("Visual_NP08_Turista_a", AlturaGeneral),
            ("Visual_NP08_Turista_b", AlturaGeneral),
            ("Visual_NP09_Corredor_a", AlturaGeneral),
            ("Visual_NP09_Corredor_b", AlturaGeneral),
            ("Visual_NP10_Elotero_a", 1.70f),
            ("Visual_NP11_Estibador_a", 1.80f),
            ("Visual_NP11_Estibador_b", 1.80f),
            ("Visual_NP12_Abuela_a", 1.66f),
            ("Visual_NP13_Repartidor_a", AlturaGeneral),
            ("Visual_NP13_Repartidor_b", AlturaGeneral),
            ("Visual_NP14_Oficinista_a", AlturaGeneral),
            ("Visual_NP14_Oficinista_b", AlturaGeneral),
        };

        /// <summary>Los prefabs Visual_ que se escalan, en el orden de la tabla.</summary>
        public static string[] Prefabs
        {
            get
            {
                var nombres = new string[Tabla.Length];
                for (int i = 0; i < Tabla.Length; i++)
                {
                    nombres[i] = Tabla[i].visual;
                }
                return nombres;
            }
        }

        /// <summary>Altura objetivo (m) de un visual ("Visual_Doctor" o "Doctor"); 0 si no es de los que se escalan.</summary>
        public static float Objetivo(string visual)
        {
            if (string.IsNullOrEmpty(visual))
            {
                return 0f;
            }
            string nombre = visual.StartsWith(PrefijoVisual) ? visual : PrefijoVisual + visual;
            for (int i = 0; i < Tabla.Length; i++)
            {
                if (Tabla[i].visual == nombre)
                {
                    return Tabla[i].altura;
                }
            }
            return 0f;
        }

        /// <summary>Objetivo contra los 1.80 m del jugador (1 = igual de alto).</summary>
        public static float Relativa(string visual) => Objetivo(visual) / AlturaJugador;

        /// <summary>
        /// Cuánto hay que multiplicar la escala del Modelo para pasar de la altura animada medida a la objetivo. 1 si la medida no es de fiar
        /// (muy chica, o una corrección absurda): mejor no tocar que romper un prefab.
        /// </summary>
        public static float FactorCorreccion(float alturaMedida, float objetivo)
        {
            if (alturaMedida < AlturaMedidaMinimaConfiable || objetivo <= 0f)
            {
                return 1f;
            }
            float factor = objetivo / alturaMedida;
            return factor > FactorMaximo || factor < 1f / FactorMaximo ? 1f : factor;
        }
    }
}

using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-1552 (decisión de Tony, 20-nov): la altura (m) de cada personaje con cuerpo humanoide de la ciudad. Regla: todos los humanos miden mínimo 1.84 m
    /// (la abuela 1.64), los robots humanoides 1.94, el soldado azul E18 2.14 y los robots grandes (E14 y E16) 3.14. El jugador PJ01 (1.84), el policía y el perro E17 no cambian.
    /// Cada fila trae la altura de hoy (m, medida en Blender, en pose) y la nueva; el factor es nueva / hoy y se multiplica por la escala que el prefab Visual_ tiene hoy.
    /// Una sola tabla: la leen el editor que escala los prefabs (AlturasT1552), el que ajusta las cápsulas y las pruebas. Viene de T-1521, que tenía a los humanos a ~1.76 m animados.
    /// </summary>
    public static class EscalasHumanos
    {
        /// <summary>Altura del jugador PJ01 (m, medida en Blender; PJ01 no se baja). Animado en Play mide ~1.80 (lo mide EscalaJugadorT670Pruebas).</summary>
        public const float AlturaJugador = 1.84f;
        /// <summary>Altura animada del jugador en Play (m): la referencia de las pruebas de PlayMode.</summary>
        public const float AlturaJugadorAnimada = 1.80f;
        /// <summary>Un humano por debajo de esto es enano contra el jugador (m); solo la abuela baja de aquí.</summary>
        public const float AlturaMinimaHumano = 1.84f;
        public const float AlturaAbuela = 1.64f;
        /// <summary>La altura más baja de la tabla (m): la abuela.</summary>
        public const float AlturaMinima = AlturaAbuela;
        /// <summary>Robots humanoides y guardias de Nova (m).</summary>
        public const float AlturaRobotHumanoide = 1.94f;
        public const float AlturaSoldadoAzulE18 = 2.14f;
        /// <summary>Robots grandes: E14 (jefe final) y E16 (robot pesado) (m).</summary>
        public const float AlturaRobotGrande = 3.14f;
        /// <summary>La altura más alta de la tabla (m): los robots grandes.</summary>
        public const float AlturaMaxima = AlturaRobotGrande;
        /// <summary>Si la altura medida ya está a esta distancia del objetivo, no se vuelve a escalar (m). Hace repetible a Aplicar.</summary>
        public const float ToleranciaAplicar = 0.03f;
        /// <summary>Margen con el que las pruebas aceptan la altura medida (m). Para los grandes, ver <see cref="TolerarPrueba"/>.</summary>
        public const float ToleranciaPrueba = 0.08f;
        /// <summary>Margen relativo de las pruebas (fracción de la altura objetivo): la tabla es de Blender en pose y la malla animada difiere unos centímetros.</summary>
        public const float ToleranciaPruebaRelativa = 0.06f;
        /// <summary>Una corrección que pida más que esto (o menos que su inverso) es señal de que la medida salió mal y no se aplica.</summary>
        public const float FactorMaximo = 2f;
        /// <summary>Altura medida por debajo de la cual no se confía en la medición (m).</summary>
        public const float AlturaMedidaMinimaConfiable = 0.3f;

        private const string PrefijoVisual = "Visual_";

        /// <summary>visual, altura de hoy (m, Blender) y altura nueva (m).</summary>
        private static readonly (string visual, float hoy, float nueva)[] Tabla =
        {
            ("Visual_NP12_Abuela_a", 1.42f, AlturaAbuela),
            ("Visual_woman", 1.80f, 1.84f),
            ("Visual_woman2", 1.73f, 1.84f),
            ("Visual_MeseraRobot", 1.65f, 1.84f),
            ("Visual_NP09_Corredor_a", 1.61f, 1.84f),
            ("Visual_NP09_Corredor_b", 1.63f, 1.85f),
            ("Visual_NP14_Oficinista_a", 1.59f, 1.84f),
            ("Visual_NP14_Oficinista_b", 1.59f, 1.86f),
            ("Visual_NP10_Elotero_a", 1.51f, 1.84f),
            ("Visual_Inventor", 1.96f, 1.84f),        // Tino: baja de 1.96 a 1.84
            ("Visual_Mecanico", 1.80f, 1.85f),        // El Canepa
            ("Visual_AL10", 1.79f, 1.86f),
            ("Visual_AL11", 1.79f, 1.86f),
            ("Visual_Policia", 1.86f, 1.86f),         // sin cambio
            ("Visual_NP13_Repartidor_a", 1.51f, 1.86f),
            ("Visual_NP13_Repartidor_b", 1.51f, 1.86f),
            ("Visual_NP08_Turista_a", 1.56f, 1.86f),
            ("Visual_NP08_Turista_b", 1.59f, 1.86f),
            ("Visual_NP07_Pescador_a", 1.56f, 1.87f),
            ("Visual_NP07_Pescador_b", 1.55f, 1.87f),
            ("Visual_SkullHombre", 1.73f, 1.87f),
            ("Visual_Doctor", 1.96f, 1.88f),          // el Doc baja
            ("Visual_man", 1.76f, 1.88f),             // pandillero armado y vendedor
            ("Visual_E19_PandilleroSkulls_a_2", 1.60f, 1.89f),
            ("Visual_E19_PandilleroSkulls_b", 1.58f, 1.89f),
            ("Visual_E10_PandilleroSnakes", 1.62f, 1.89f),
            ("Visual_Hacker", 1.80f, 1.90f),
            ("Visual_NP11_Estibador_a", 1.61f, 1.90f),
            ("Visual_NP11_Estibador_b", 1.66f, 1.90f),
            ("Visual_E15_CentinelaNova_a", 2.37f, AlturaRobotHumanoide),
            ("Visual_E15_CentinelaNova_b", 2.37f, AlturaRobotHumanoide),
            ("Visual_GuardiaNova", 2.46f, AlturaRobotHumanoide),
            ("Visual_RobotHombre", 1.80f, AlturaRobotHumanoide),
            ("Visual_E20_MatonSkulls_a", 1.70f, 1.96f),
            ("Visual_E20_MatonSkulls_b", 1.67f, 1.96f),
            ("Visual_E11_JefeSnakes", 1.78f, 1.98f),
            ("Visual_E12_GuardiaPesadoNova", 1.77f, 2.04f),
            ("Visual_E18_EliteNova_a", 1.93f, AlturaSoldadoAzulE18),
            ("Visual_E14_JefeFinalNova", 1.89f, AlturaRobotGrande),
            ("Visual_E16_RobotPesadoNova_a", 2.92f, AlturaRobotGrande),
            ("Visual_E16_RobotPesadoNova_b", 3.02f, AlturaRobotGrande),
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

        /// <summary>Altura nueva (m) de un visual ("Visual_Doctor" o "Doctor"); 0 si no es de los que se escalan.</summary>
        public static float Objetivo(string visual)
        {
            int i = Buscar(visual);
            return i >= 0 ? Tabla[i].nueva : 0f;
        }

        /// <summary>Altura de hoy (m, medida en Blender) de un visual de la tabla; 0 si no está.</summary>
        public static float Hoy(string visual)
        {
            int i = Buscar(visual);
            return i >= 0 ? Tabla[i].hoy : 0f;
        }

        /// <summary>Factor de la tabla (nueva / hoy) que se multiplica por la escala de hoy del prefab; 1 si el visual no está.</summary>
        public static float Factor(string visual)
        {
            int i = Buscar(visual);
            return i >= 0 ? Tabla[i].nueva / Tabla[i].hoy : 1f;
        }

        private static int Buscar(string visual)
        {
            if (string.IsNullOrEmpty(visual))
            {
                return -1;
            }
            string nombre = visual.StartsWith(PrefijoVisual) ? visual : PrefijoVisual + visual;
            for (int i = 0; i < Tabla.Length; i++)
            {
                if (Tabla[i].visual == nombre)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>Objetivo contra los 1.84 m del jugador (1 = igual de alto).</summary>
        public static float Relativa(string visual) => Objetivo(visual) / AlturaJugador;

        /// <summary>Margen con el que una prueba acepta la altura medida de un visual: el mayor entre el fijo y la fracción de su altura (los robots grandes llevan más).</summary>
        public static float TolerarPrueba(string visual) => Mathf.Max(ToleranciaPrueba, Objetivo(visual) * ToleranciaPruebaRelativa);

        /// <summary>
        /// Cuánto hay que multiplicar la escala del Modelo para pasar de la altura medida a la objetivo. 1 si la medida no es de fiar
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

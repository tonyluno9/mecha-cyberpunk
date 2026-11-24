using System;
using UnityEngine;

namespace MechaCyberpunk.Core
{
    /// <summary>T-1620: todos los números del director de respuesta (datos serializables; los guarda <see cref="ConfigRespuesta"/>).</summary>
    [Serializable]
    public class ParametrosRespuesta
    {
        [Tooltip("Topes y cooldowns por tipo de unidad.")]
        public AjusteTipoRespuesta[] Tipos = new AjusteTipoRespuesta[0];
        [Tooltip("Máximo de unidades vivas de respuesta en total.")]
        public int TopeTotal = 8;
        [Tooltip("Nivel más alto de la jerarquía (1 a 8) al que PUEDE llegar la respuesta con 1, 2, 3, 4 y 5 estrellas.")]
        public int[] NivelMaximoPorEstrellas = { 1, 2, 3, 5, 8 };
        [Tooltip("Segundos que un nivel tiene para detenerte; pasado el tiempo (y sigues ahí) se desbloquea el siguiente (s).")]
        public float SegundosParaSubir = 45f;
        [Tooltip("Segundos que tarda en llegar la primera unidad de un nivel recién desbloqueado (s).")]
        public float RetrasoNivelNuevo = 10f;
        [Tooltip("Cada cuántas unidades perdidas de un tipo se permite una más a la vez.")]
        public int PerdidasParaUnoMas = 2;
        [Tooltip("Segundos mínimos entre dos unidades nuevas del mismo tipo (nada de golpe) (s).")]
        public float SegundosEntreGenerados = 4f;
        [Tooltip("Estrellas con las que responde NOVA en una misión protegida que la permite.")]
        public int EstrellasNovaEnMision = 4;
        [Tooltip("T-1631 (T17): con estas estrellas (el máximo) NOVA INTERVIENE: deja de llegar policía, la que hay se retira y llega solo Nova.")]
        public int EstrellasModoNova = 5;
        [Tooltip("T-1631: máximo de unidades de NOVA vivas a la vez (humanoides, medianos, perros, grandes y drones de Nova; el jefe no cuenta) en modo Nova.")]
        public int TopeNovaVivos = 3;
        [Tooltip("T-1631: unidades de un nivel de NOVA que hay que vencer para que se desbloquee el siguiente en modo Nova (el tiempo también lo desbloquea).")]
        public int PerdidasNovaParaSubir = 2;
        [Tooltip("T-1631: nivel de la jerarquía (1 a 8) desde el que llegan los drones de NOVA en cápsula en modo Nova.")]
        public int NivelDronNova = 6;
        [Tooltip("T-1631: un policía a pie que se retira solo desaparece si está al menos a esta distancia del jugador y fuera de su vista (m).")]
        public float MetrosMinimosRetiroPolicia = 15f;
        [Tooltip("T-1660: policías a pie que hay que matar en la búsqueda antes de que llegue Nova (0 = sin esa condición).")]
        public int PoliciasMuertosParaNova = 10;
        [Tooltip("T-1660: camionetas que deben haber llegado antes de que llegue Nova (0 = sin esa condición; sin modelo de camioneta no se espera).")]
        public int CamionetasParaNova = 1;
        [Tooltip("Escribe una línea [Respuesta] en consola por cada unidad que sale o se retira.")]
        public bool Registrar = true;

        // ----- T-historia-libre-23nov: modo libre (escalada progresiva tras el campo de tiro) -----
        [Header("Modo libre (T-historia-libre-23nov)")]
        [Tooltip("T94: fase 0 del modo libre (solo SKULLS, sin policía aunque tengas estrellas): segundos MÍNIMOS que dura (s). Termina al pasar este tiempo si ya mataste SkullsFaseSkulls; si no, cuando los mates (o al tope SegundosMaximosFaseSkulls).")]
        [Min(0f)] public float SegundosFaseSkulls = 120f;
        [Tooltip("T94: SKULLS vencidos en la fase 0 para que termine (cuenta desde el principio de la fase; solo la deja terminar tras SegundosFaseSkulls). 0 = solo el tiempo.")]
        [Min(0)] public int SkullsFaseSkulls = 10;
        [Tooltip("T94: tope de la fase 0 para que no se atore: a estos segundos llega la policía aunque no hayas matado a los SKULLS pedidos (s).")]
        [Min(0f)] public float SegundosMaximosFaseSkulls = 240f;
        [Tooltip("T94: segundos entre dos grupos de SKULLS del goteo de la fase 0 (s).")]
        [Min(0.5f)] public float SegundosEntreGruposSkulls = 12f;
        [Tooltip("T94: segundos hasta el primer grupo de SKULLS al empezar la fase 0 (s).")]
        [Min(0f)] public float SegundosPrimerGrupoSkulls = 4f;
        [Tooltip("T94: SKULLS por grupo del goteo, mínimo.")]
        [Min(1)] public int SkullsPorGrupoMin = 2;
        [Tooltip("T94: SKULLS por grupo del goteo, máximo.")]
        [Min(1)] public int SkullsPorGrupoMax = 3;
        [Tooltip("T94: máximo de SKULLS del goteo vivos a la vez (además respeta TopeEnemigosLibre / TopeEnemigosLibreVR).")]
        [Min(1)] public int SkullsVivosFaseSkulls = 6;
        [Tooltip("T94: fracción de los SKULLS del goteo que llegan armados (pistola; nunca rifles). El resto pelea a golpes. 0 a 1.")]
        [Range(0f, 1f)] public float FraccionSkullsArmados = 0.2f;
        [Tooltip("T94: los SKULLS del goteo aparecen a lo más a esta distancia del jugador (m); la mínima es MetrosMinimosRefuerzo y fuera de la vista (GradosVistaRefuerzo).")]
        [Min(1f)] public float MetrosMaximosAparicionSkulls = 45f;
        [Tooltip("Fases 1 a 6 de la escalada del modo libre: cada una dura su tiempo o hasta sus bajas (lo que pase primero).")]
        public FaseModoLibre[] FasesLibre = FasesLibrePorOmision();
        [Tooltip("Modo libre en PC: máximo de enemigos de respuesta vivos a la vez (policías a pie, drones, NOVA; sin contar los vehículos). T-rendimiento-tope-24nov: medido en el build de PC (RTX 5070 Laptop, 2560x1600, Ultra): 24 enemigos dan 87.6 fps de promedio y 62.8 el 1 % más bajo; 31 dan 83.9 / 60.6.")]
        [Min(1)] public int TopeEnemigosLibre = 24;
        [Tooltip("Modo libre en VR: máximo de enemigos vivos a la vez. Conservador (VR dibuja dos veces y pide 72 fps estables); sin medir en el visor.")]
        [Min(1)] public int TopeEnemigosLibreVR = 12;

        /// <summary>El tope de enemigos del modo libre según el modo (PC o VR).</summary>
        public int TopeEnemigosLibreActual => ModoVR.Activo ? Mathf.Min(TopeEnemigosLibreVR, TopeEnemigosLibre) : TopeEnemigosLibre;
        [Tooltip("Modo libre: máximo de patrullas y camionetas a la vez.")]
        [Min(0)] public int TopePatrullasLibre = 3;
        [Tooltip("Modo libre: máximo de drones a la vez.")]
        [Min(0)] public int TopeDronesLibre = 4;
        [Tooltip("Modo libre: máximo de cápsulas de NOVA cayendo a la vez.")]
        [Min(0)] public int TopeCapsulasLibre = 2;
        [Tooltip("Modo libre: máximo de robots grandes (E16) a la vez.")]
        [Min(0)] public int TopeRobotsGrandesLibre = 2;
        [Tooltip("Modo libre: segundos mínimos tras una baja para que llegue su reemplazo (goteo constante, sin huecos) (s).")]
        [Min(0f)] public float SegundosReposicionLibre = 6f;
        [Tooltip("Modo libre: los refuerzos a pie aparecen al menos a esta distancia del jugador, también a sus espaldas (m).")]
        [Min(0f)] public float MetrosMinimosRefuerzo = 25f;
        [Tooltip("Modo libre: medio ángulo de la vista (°); un refuerzo no aparece dentro de él.")]
        [Range(0f, 180f)] public float GradosVistaRefuerzo = 70f;

        /// <summary>T-historia-libre-23nov: las fases de Tony (24-nov 02:29): policía a pie, más patrullas, camiones, drones de NOVA, soldados NOVA, NOVA pesada sin fin.</summary>
        public static FaseModoLibre[] FasesLibrePorOmision()
        {
            TipoRespuesta[] policias = { TipoRespuesta.PoliciaAPie };
            TipoRespuesta[] soldados = { TipoRespuesta.NovaHumanoide, TipoRespuesta.RobotMediano };
            return new[]
            {
                new FaseModoLibre("POLICIA A PIE Y PATRULLAS", 300f, 2, false, new[] { TipoRespuesta.PoliciaAPie, TipoRespuesta.Patrulla }, 20, policias),
                new FaseModoLibre("MAS PATRULLAS", 300f, 3, false, new[] { TipoRespuesta.PoliciaAPie, TipoRespuesta.Patrulla }, 30, policias,
                    6, new[] { TipoRespuesta.Patrulla, TipoRespuesta.Camioneta }),
                new FaseModoLibre("CAMIONES Y REFUERZOS PESADOS", 300f, 4, false,
                    new[] { TipoRespuesta.PoliciaAPie, TipoRespuesta.Patrulla, TipoRespuesta.Camioneta, TipoRespuesta.Dron }, 40, policias),
                new FaseModoLibre("DRONES DE NOVA", 240f, 5, true, new[] { TipoRespuesta.Dron }, 10, new[] { TipoRespuesta.Dron }),
                new FaseModoLibre("SOLDADOS NOVA", 300f, 5, true, new[] { TipoRespuesta.NovaHumanoide, TipoRespuesta.RobotMediano, TipoRespuesta.Dron }, 20, soldados),
                new FaseModoLibre("NOVA PESADA", 0f, 5, true,
                    new[] { TipoRespuesta.NovaHumanoide, TipoRespuesta.RobotMediano, TipoRespuesta.Perro, TipoRespuesta.RobotGrande, TipoRespuesta.Dron }, 0, null),
            };
        }

        public AjusteTipoRespuesta AjusteDe(TipoRespuesta tipo)
        {
            if (Tipos != null)
            {
                foreach (AjusteTipoRespuesta a in Tipos)
                {
                    if (a != null && a.Tipo == tipo)
                    {
                        return a;
                    }
                }
            }
            return null;
        }

        /// <summary>Los valores de la tarea T8.</summary>
        public static ParametrosRespuesta PorOmision()
        {
            return new ParametrosRespuesta
            {
                Tipos = new[]
                {
                    new AjusteTipoRespuesta(TipoRespuesta.PoliciaAPie, 6, 6, 0f, 0f),
                    new AjusteTipoRespuesta(TipoRespuesta.Patrulla, 3, 1, 30f, 30f),
                    new AjusteTipoRespuesta(TipoRespuesta.Camioneta, 2, 1, 35f, 45f),
                    new AjusteTipoRespuesta(TipoRespuesta.Dron, 2, 1, 25f, 35f),
                    new AjusteTipoRespuesta(TipoRespuesta.NovaHumanoide, 2, 1, 20f, 30f),
                    new AjusteTipoRespuesta(TipoRespuesta.RobotMediano, 1, 1, 25f, 35f),
                    new AjusteTipoRespuesta(TipoRespuesta.Perro, 2, 1, 20f, 30f),
                    new AjusteTipoRespuesta(TipoRespuesta.RobotGrande, 1, 1, 40f, 60f),
                    new AjusteTipoRespuesta(TipoRespuesta.Jefe, 1, 1, 120f, 120f)
                }
            };
        }
    }
}

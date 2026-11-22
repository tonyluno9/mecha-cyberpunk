using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>T-1560: acciones sostenidas de NPC, robots y pandilleros (parámetro entero <see cref="EstadosNpcT1560.AccionNpc"/>). 0 = ninguna.</summary>
    public enum AccionNpcT1560
    {
        Ninguna = 0,
        PlaticarA = 1,
        PlaticarB = 2,
        PlaticarC = 3,
        PlaticarD = 4,
        Discutir1 = 5,
        Discutir2 = 6,
        Discutir3 = 7,
        Teclear = 8,
        TrabajarAparato = 9,
        LlamarPolicia = 10,
        HeridoPedirAyuda = 11,
        HeridoIdle = 12,
        HeridoArrastrandose = 13,
        Cojear = 14,
        CelularCaminando = 15,
        MensajearCaminando1 = 16,
        MensajearCaminando2 = 17,
        CaminarCasual = 18,
        CaminarCirculo = 19,
        Arrestado = 20,
        Rendirse = 21,
        Caerse = 22,
    }

    /// <summary>
    /// T-1560: gestos de una vez de NPC, pandilleros y robots (trigger Gesto + GestoTipo, igual que <see cref="GestoVisual"/>). Los ids empiezan en 40 para no chocar con los de GestoVisual (0 a 14) ni
    /// con los de T-1561 (15 a 20); la excepción es BloquearTambaleo = 19, que T-1561 ya definió. Se lanzan con <see cref="EstadosNpcT1560.Gestear"/>.
    /// </summary>
    public enum GestoNpcT1560
    {
        Senalar = 40,
        Senalar2 = 41,
        SenalarOrdenar = 42,
        VoltearAtras = 43,
        BurlaPandillero1 = 44,
        BurlaPandillero2 = 45,
        BurlaGesto = 46,
        Gritar = 47,
        Empujar = 48,
        Patada = 49,
        SacarArmaAmenaza = 50,
        /// <summary>Id 19: lo fijó T-1561 (GestoVisual.BloquearTambaleo); el runtime lo dispara con VisualPersonaje.BloquearGolpeFuerte desde ConductaGuardiaPesado.</summary>
        BloquearTambaleo = 19,
        Encenderse = 52,
        SacadoDelCarroHuir = 53,
        CaidaLevantarse = 54,
    }

    /// <summary>Cómo entra a su estado cada fila de <see cref="EstadosNpcT1560.Filas"/>.</summary>
    public enum TipoEstadoT1560
    {
        /// <summary>Se queda mientras AccionNpc valga su id (bucle, o última pose si el clip no repite). Entra desde Idle, Caminar y los giros.</summary>
        Accion,
        /// <summary>Una sola vez: trigger Gesto + GestoTipo = id, desde cualquier estado (menos al caer); al terminar vuelve a Idle.</summary>
        Gesto,
    }

    /// <summary>
    /// T-1560: los estados que se agregan a la capa base del ControladorBase para los clips de Blender y Mixamo del 21-nov que no tenían estado (NPC, pandilleros, rendirse y arrestado, robots).
    /// Datos puros: los lee el método de editor AnimacionesT1560.AgregarEstados (que los arma), el diagnóstico y las pruebas. Las acciones del jugador con armas, granada e interacciones
    /// (sacar y guardar arma, recargas, beber, botón, puerta, recoger, sentarse, camilla, escalera, colgarse, saltar barda, trepar) son de T-1561 y no están aquí.
    /// </summary>
    public static class EstadosNpcT1560
    {
        /// <summary>Parámetro entero del controlador base: qué acción sostenida juega (<see cref="AccionNpcT1560"/>; 0 ninguna).</summary>
        public const string AccionNpc = "AccionNpc";

        /// <summary>Texto de la columna de disparador cuando ninguna lógica de juego lo lanza todavía (el estado está listo; falta que la IA llame a <see cref="Gestear"/> o <see cref="PonerAccion"/>).</summary>
        public const string SinDisparador = "sin disparador";

        /// <summary>
        /// Estado -> clip base (el que lleva el controlador base y el que usan todos los overrides que no lo cambian), tipo, id (AccionNpc o GestoTipo) y disparador.
        /// Los clips de robots (HeridoIdle, Cojear, CaidaLevantarse) llevan el clip del E15 o del PJ01 como base; Override_E15 y Override_E16 ponen el suyo.
        /// </summary>
        public static readonly (string Estado, string Clip, TipoEstadoT1560 Tipo, int Id, string Disparador)[] Filas =
        {
            ("NpcPlaticarA", "NPC_Platicar_A", TipoEstadoT1560.Accion, (int)AccionNpcT1560.PlaticarA, SinDisparador),
            ("NpcPlaticarB", "NPC_Platicar_B", TipoEstadoT1560.Accion, (int)AccionNpcT1560.PlaticarB, SinDisparador),
            ("NpcPlaticarC", "NPC_Platicar_C", TipoEstadoT1560.Accion, (int)AccionNpcT1560.PlaticarC, SinDisparador),
            ("NpcPlaticarD", "NPC_Platicar_D", TipoEstadoT1560.Accion, (int)AccionNpcT1560.PlaticarD, SinDisparador),
            ("NpcDiscutir1", "NPC_Discutir_1", TipoEstadoT1560.Accion, (int)AccionNpcT1560.Discutir1, SinDisparador),
            ("NpcDiscutir2", "NPC_Discutir_2", TipoEstadoT1560.Accion, (int)AccionNpcT1560.Discutir2, SinDisparador),
            ("NpcDiscutir3", "NPC_Discutir_3", TipoEstadoT1560.Accion, (int)AccionNpcT1560.Discutir3, SinDisparador),
            ("NpcTeclear", "NPC_Teclear", TipoEstadoT1560.Accion, (int)AccionNpcT1560.Teclear, SinDisparador),
            ("NpcTrabajarAparato", "NPC_Trabajar_Aparato", TipoEstadoT1560.Accion, (int)AccionNpcT1560.TrabajarAparato, SinDisparador),
            ("NpcLlamarPolicia", "NPC_LlamarPolicia", TipoEstadoT1560.Accion, (int)AccionNpcT1560.LlamarPolicia, SinDisparador),
            ("NpcHeridoPedirAyuda", "NPC_Herido_PedirAyuda", TipoEstadoT1560.Accion, (int)AccionNpcT1560.HeridoPedirAyuda, SinDisparador),
            ("HeridoIdle", "PJ01_Herido_Idle", TipoEstadoT1560.Accion, (int)AccionNpcT1560.HeridoIdle, SinDisparador),
            ("HeridoArrastrandose", "PJ01_Pandillero_HeridoArrastrandose", TipoEstadoT1560.Accion, (int)AccionNpcT1560.HeridoArrastrandose, SinDisparador),
            ("Cojear", "E15_Cojear", TipoEstadoT1560.Accion, (int)AccionNpcT1560.Cojear, SinDisparador),
            ("NpcCelularCaminando", "NPC_Celular_Caminando", TipoEstadoT1560.Accion, (int)AccionNpcT1560.CelularCaminando, SinDisparador),
            ("NpcMensajear1", "NPC_Mensajear_Caminando_1", TipoEstadoT1560.Accion, (int)AccionNpcT1560.MensajearCaminando1, SinDisparador),
            ("NpcMensajear2", "NPC_Mensajear_Caminando_2", TipoEstadoT1560.Accion, (int)AccionNpcT1560.MensajearCaminando2, SinDisparador),
            ("NpcCaminarCasual", "NPC_Caminar_Casual", TipoEstadoT1560.Accion, (int)AccionNpcT1560.CaminarCasual, SinDisparador),
            ("NpcCaminarCirculo", "NPC_Caminar_Circulo", TipoEstadoT1560.Accion, (int)AccionNpcT1560.CaminarCirculo, SinDisparador),
            ("Arrestado", "PJ01_Arrestado_Arrodillarse", TipoEstadoT1560.Accion, (int)AccionNpcT1560.Arrestado, SinDisparador),
            ("Rendirse", "PJ01_Rendirse", TipoEstadoT1560.Accion, (int)AccionNpcT1560.Rendirse, SinDisparador),
            ("Caerse", "PJ01_Caerse", TipoEstadoT1560.Accion, (int)AccionNpcT1560.Caerse, SinDisparador),

            ("NpcSenalar", "NPC_Senalar", TipoEstadoT1560.Gesto, (int)GestoNpcT1560.Senalar, SinDisparador),
            ("NpcSenalar2", "NPC_Senalar_2", TipoEstadoT1560.Gesto, (int)GestoNpcT1560.Senalar2, SinDisparador),
            ("SenalarOrdenar", "PJ01_Senalar", TipoEstadoT1560.Gesto, (int)GestoNpcT1560.SenalarOrdenar, SinDisparador),
            ("NpcVoltearAtras", "NPC_Voltear_Atras", TipoEstadoT1560.Gesto, (int)GestoNpcT1560.VoltearAtras, SinDisparador),
            ("PandilleroBurla1", "PJ01_Pandillero_Burla1", TipoEstadoT1560.Gesto, (int)GestoNpcT1560.BurlaPandillero1, SinDisparador),
            ("PandilleroBurla2", "PJ01_Pandillero_Burla2", TipoEstadoT1560.Gesto, (int)GestoNpcT1560.BurlaPandillero2, SinDisparador),
            ("PandilleroBurlaGesto", "PJ01_Pandillero_BurlaGesto", TipoEstadoT1560.Gesto, (int)GestoNpcT1560.BurlaGesto, SinDisparador),
            ("PandilleroGritar", "PJ01_Pandillero_Gritar", TipoEstadoT1560.Gesto, (int)GestoNpcT1560.Gritar, SinDisparador),
            ("PandilleroEmpujar", "PJ01_Pandillero_Empujar", TipoEstadoT1560.Gesto, (int)GestoNpcT1560.Empujar, SinDisparador),
            ("PandilleroPatada", "PJ01_Pandillero_Patada", TipoEstadoT1560.Gesto, (int)GestoNpcT1560.Patada, SinDisparador),
            ("PandilleroSacarArma", "PJ01_Pandillero_SacarArma", TipoEstadoT1560.Gesto, (int)GestoNpcT1560.SacarArmaAmenaza, SinDisparador),
            ("BloquearTambaleo", "E16_Bloquear_Tambaleo", TipoEstadoT1560.Gesto, (int)GestoNpcT1560.BloquearTambaleo, SinDisparador),
            ("Encenderse", "E16_Encenderse", TipoEstadoT1560.Gesto, (int)GestoNpcT1560.Encenderse, SinDisparador),
            ("NpcSacadoHuir", "NPC_SacadoDelCarro_Huir", TipoEstadoT1560.Gesto, (int)GestoNpcT1560.SacadoDelCarroHuir, SinDisparador),
            ("CaidaLevantarse", "E18_Caida_Levantarse", TipoEstadoT1560.Gesto, (int)GestoNpcT1560.CaidaLevantarse, SinDisparador),
        };

        /// <summary>Los nombres de los estados nuevos (para las listas y pruebas que cuentan los estados de la capa base).</summary>
        public static string[] Nombres
        {
            get
            {
                var lista = new string[Filas.Length];
                for (int i = 0; i < Filas.Length; i++)
                {
                    lista[i] = Filas[i].Estado;
                }
                return lista;
            }
        }

        /// <summary>El GestoVisual que hay que mandar a <c>VisualPersonaje.Gestear</c> para lanzar este gesto.</summary>
        public static GestoVisual Gesto(GestoNpcT1560 gesto) => (GestoVisual)(int)gesto;

        /// <summary>Lanza el gesto en el visual (false si está caído, sin Animator o el controlador no tiene el gesto).</summary>
        public static bool Gestear(VisualPersonaje visual, GestoNpcT1560 gesto) => visual != null && visual.Gestear(Gesto(gesto));

        /// <summary>
        /// Pone o quita una acción sostenida en el Animator (AccionNpc). Devuelve false si el controlador no tiene el parámetro (no es el ControladorBase con los estados de T-1560).
        /// </summary>
        public static bool PonerAccion(Animator animador, AccionNpcT1560 accion)
        {
            if (animador == null || animador.runtimeAnimatorController == null)
            {
                return false;
            }
            foreach (AnimatorControllerParameter p in animador.parameters)
            {
                if (p.name == AccionNpc && p.type == AnimatorControllerParameterType.Int)
                {
                    animador.SetInteger(AccionNpc, (int)accion);
                    return true;
                }
            }
            return false;
        }
    }
}

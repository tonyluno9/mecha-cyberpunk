using System;
using System.Collections.Generic;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>
    /// T-1471: los sonidos nuevos del 19-nov. Dos cosas puras: la lista de IDs nuevos (siempre encendidos y con política de mezcla propia) y el mapa
    /// de IDs viejos -> nuevos: el código que ya pide un ID viejo suena con el archivo nuevo cuando el catálogo lo tiene (si no, con el viejo).
    /// </summary>
    public static class MapaSonidos19nov
    {
        /// <summary>Un ID viejo, el nuevo con el que suena, su tono base y si queda encendido por defecto.</summary>
        public readonly struct Par
        {
            public Par(string viejo, string nuevo, float tonoBase = 1f, bool encender = true)
            {
                Viejo = viejo;
                Nuevo = nuevo;
                TonoBase = tonoBase;
                Encender = encender;
            }

            public string Viejo { get; }
            public string Nuevo { get; }
            public float TonoBase { get; }
            public bool Encender { get; }
        }

        /// <summary>Los IDs nuevos del brief (docs de T-1470..T-1476).</summary>
        public static readonly string[] IdsNuevos =
        {
            "UI_MOVER", "UI_SELECCIONAR", "UI_ATRAS", "UI_ERROR", "UI_PESTANA", "UI_MENU_ABRIR", "UI_MENU_CERRAR", "UI_LOGO_INTRO", "HUD_LOGRO", "HUD_NOTIFICACION", "HUD_VIDA_BAJA",
            "TIE_COMPRAR", "TIE_EQUIPAR", "TIE_DESEQUIPAR",
            "ARM_W01_DISPARO", "ARM_W02_DISPARO", "ARM_W02_BOMBA", "ARM_W03_DISPARO", "ARM_W03_RECARGA_CARGADOR", "ARM_W04_DISPARO", "ARM_W04_CERROJO",
            "ARM_W06_DISPARO", "ARM_W06_SACAR", "ARM_W07_DISPARO", "ARM_W07_CARGA",
            "ARM_GRANADA_SEGURO", "ARM_GRANADA_REBOTE", "ARM_COLA_CALLE", "IMP_LASER_DISPARO", "IMP_MISILES_FIJAR",
            "IMP_BALA_CABEZA", "IMP_BALA_CARNE", "IMP_BALA_CONCRETO", "IMP_BALA_CRISTAL", "IMP_BALA_METAL", "IMP_BALA_ROBOT", "IMP_BALA_ZUMBIDO",
            "EXP_GRANADA", "EXP_ONDA", "FUE_LOOP_CHICO",
            "PAS_CONCRETO", "PAS_MADERA", "PAS_METAL", "PAS_MOJADO", "PAS_TIERRA", "PAS_GRAVA",
            "JUG_ATERRIZAJE", "JUG_RODADA", "JUG_TREPAR", "MEL_GOLPE", "MEL_GOLPE_AIRE", "MEL_GOLPE_CARNE",
            "INT_HACKEO_EXITO", "INT_HACKEO_FALLO", "INT_INTERRUPTOR_LUZ", "INT_MALETIN_CERRAR", "INT_PUERTA_CRISTAL", "INT_PUERTA_MADERA", "INT_PUERTA_METAL", "INT_PUERTA_REJA",
            "INT_RECOGER_OBJETO", "INT_TECLEO_HACKEO",
            "CON_BEBER", "CON_CURACION", "CON_LATA_ABRIR", "CON_PAPITAS_COMER", "CON_VENDA",
            "VEH_NORMAL_ARRANQUE", "VEH_NORMAL_RALENTI", "VEH_NORMAL_APAGAR", "MOTO_ARRANQUE", "MOTO_RALENTI", "MOTO_APAGAR", "VEH_PESADO_ARRANQUE", "VEH_PESADO_RALENTI", "VEH_PESADO_APAGAR",
            "VEH_ACELERON", "VEH_ALARMA", "VEH_CHOQUE", "VEH_CHOQUE_FUERTE", "VEH_CLAXON_CAMION", "VEH_CLAXON_NORMAL", "VEH_CRISTAL_ROMPER", "VEH_FRENO_FUERTE", "VEH_LLANTAS_GRAVA",
            "VEH_PUENTE", "VEH_PUERTA_ABRIR", "VEH_PUERTA_CERRAR", "VEH_TOPE", "VEH_ELECTRICO_ACELERAR", "VEH_ELECTRICO_ARRANQUE", "VEH_ELECTRICO_MOVER", "VEH_ELECTRICO_PASAR",
            "POL_RADIO_AVISO", "POL_SIRENA_CORTA", "POL_SIRENA_LOOP",
        };

        /// <summary>
        /// ID viejo (el que ya pide el código) -> ID nuevo. ARM_BALA_ROZA se mapea pero queda apagado (T-729: su sonido era el silbato de la policía);
        /// VEH_CLAXON_TRAFICO también (T-977: spam de tráfico). VEH_CLAXON_VIEJO sí se enciende: lo toca el jugador, no el tráfico.
        /// </summary>
        public static readonly Par[] Pares =
        {
            new Par("ARM_PIS_DISPARO", "ARM_W01_DISPARO"),
            new Par("ARM_SUB_DISPARO", "ARM_W01_DISPARO", TonoSubfusil),
            new Par("ARM_ESC_DISPARO", "ARM_W02_DISPARO"),
            new Par("ARM_ESC_BOMBEO", "ARM_W02_BOMBA"),
            new Par("ARM_FUS_DISPARO", "ARM_W03_DISPARO"),
            new Par("ARM_FUS_RECARGA", "ARM_W03_RECARGA_CARGADOR"),
            new Par("ARM_SUB_RECARGA", "ARM_W03_RECARGA_CARGADOR"),
            new Par("ARM_SNI_DISPARO", "ARM_W04_DISPARO"),
            new Par("ARM_SNI_CERROJO", "ARM_W04_CERROJO"),
            new Par("ARM_REV_DISPARO", "ARM_W06_DISPARO"),
            new Par("ARM_PLA_DISPARO", "ARM_W07_DISPARO"),
            new Par("ARM_PLA_CARGA", "ARM_W07_CARGA"),
            new Par("ARM_BALA_IMPACTO_CARNE", "IMP_BALA_CARNE"),
            new Par("ARM_BALA_IMPACTO_CONCRETO", "IMP_BALA_CONCRETO"),
            new Par("ARM_BALA_IMPACTO_METAL", "IMP_BALA_METAL"),
            new Par("ARM_BALA_ZUMBIDO", "IMP_BALA_ZUMBIDO"),
            new Par("ARM_BALA_ROZA", "IMP_BALA_ZUMBIDO", 1f, false),
            new Par("UI_ACEPTAR", "UI_SELECCIONAR"),
            new Par("UI_CANCELAR", "UI_ATRAS"),
            new Par("UI_MAPA_ABRIR", "UI_MENU_ABRIR"),
            new Par("UI_MAPA_CERRAR", "UI_MENU_CERRAR"),
            new Par("UI_TITULO_INICIO", "UI_LOGO_INTRO"),
            new Par("UI_OBJETIVO_NUEVO", "HUD_NOTIFICACION"),
            new Par("UI_OBJETIVO_CUMPLIDO", "HUD_LOGRO"),
            new Par("JUG_VIDA_BAJA", "HUD_VIDA_BAJA"),
            new Par("ECO_COMPRA", "TIE_COMPRAR"),
            new Par("ECO_SIN_DINERO", "UI_ERROR"),
            new Par("ECO_ARMA_RECOGER", "INT_RECOGER_OBJETO"),
            new Par("JUG_PASO_ASFALTO", "PAS_CONCRETO"),
            new Par("JUG_PASO_METAL", "PAS_METAL"),
            new Par("JUG_PASO_ASFALTO_MOJADO", "PAS_MOJADO"),
            new Par("JUG_PASO_INTERIOR", "PAS_MADERA"),
            new Par("JUG_CAER", "JUG_ATERRIZAJE"),
            new Par("PUN_GOLPE_CARNE", "MEL_GOLPE_CARNE"),
            new Par("PUN_GOLPE_AIRE", "MEL_GOLPE_AIRE"),
            new Par("MUN_PUERTA_CASA_ABRIR", "INT_PUERTA_MADERA"),
            new Par("MUN_PUERTA_CASA_CERRAR", "INT_PUERTA_MADERA"),
            new Par("VEH_VIEJO_ARRANQUE", "VEH_NORMAL_ARRANQUE"),
            new Par("VEH_VIEJO_MOTOR_RALENTI", "VEH_NORMAL_RALENTI"),
            new Par("VEH_VIEJO_MOTOR_ACELERA", "VEH_ACELERON"),
            new Par("VEH_CHOQUE_LEVE", "VEH_CHOQUE"),
            new Par("VEH_FRENO_DERRAPE", "VEH_FRENO_FUERTE"),
            new Par("VEH_ALARMA_ROBO", "VEH_ALARMA"),
            new Par("VEH_CLAXON_VIEJO", "VEH_CLAXON_NORMAL"),
            new Par("VEH_CLAXON_TRAFICO", "VEH_CLAXON_NORMAL", 1f, false),
            new Par("CAL_PATRULLA_SIRENA", "POL_SIRENA_LOOP"),
        };

        /// <summary>El subfusil usa el disparo de la pistola (W01) más agudo.</summary>
        public const float TonoSubfusil = 1.25f;

        private static readonly Dictionary<string, Par> PorViejo = Armar();
        private static readonly HashSet<string> Nuevos = new HashSet<string>(IdsNuevos);

        private static Dictionary<string, Par> Armar()
        {
            Dictionary<string, Par> d = new Dictionary<string, Par>();
            foreach (Par p in Pares) d[p.Viejo] = p;
            return d;
        }

        /// <summary>Mezcla las entradas de las carpetas nuevas en las viejas: un ID que ya existía cambia sus finales por los nuevos (no se mezclan); los provisionales se conservan.</summary>
        public static void Reemplazar(IDictionary<string, CatalogoSonidos.Entrada> entradas, IEnumerable<CatalogoSonidos.Entrada> nuevas)
        {
            foreach (CatalogoSonidos.Entrada n in nuevas)
            {
                if (n == null || string.IsNullOrEmpty(n.id) || n.finales == null || n.finales.Length == 0) continue;
                if (entradas.TryGetValue(n.id, out CatalogoSonidos.Entrada vieja)) vieja.finales = n.finales;
                else entradas[n.id] = n;
            }
        }

        /// <summary>Si el ID es uno de los nuevos del brief (19-nov, o 20-nov: T-1531).</summary>
        public static bool EsNuevo(string id) => id != null && (Nuevos.Contains(id) || MapaSonidos20nov.EsNuevo(id));

        /// <summary>El ID nuevo con el que suena un ID viejo; null si no está mapeado.</summary>
        public static string Nuevo(string viejo) => MapaSonidos20nov.Nuevo(viejo) ?? (viejo != null && PorViejo.TryGetValue(viejo, out Par p) ? p.Nuevo : null);

        /// <summary>Tono base del ID viejo mapeado (1 si no está mapeado).</summary>
        public static float TonoBase(string viejo) => MapaSonidos20nov.EstaMapeado(viejo) ? MapaSonidos20nov.TonoBase(viejo) : viejo != null && PorViejo.TryGetValue(viejo, out Par p) ? p.TonoBase : 1f;

        /// <summary>Si el ID viejo mapeado queda encendido por defecto.</summary>
        public static bool Enciende(string viejo) => MapaSonidos20nov.EstaMapeado(viejo) ? MapaSonidos20nov.Enciende(viejo) : viejo != null && PorViejo.TryGetValue(viejo, out Par p) && p.Encender;

        /// <summary>Si el ID está en el mapa (como viejo).</summary>
        public static bool EstaMapeado(string viejo) => MapaSonidos20nov.EstaMapeado(viejo) || (viejo != null && PorViejo.ContainsKey(viejo));
    }
}

using System;
using System.Collections.Generic;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>
    /// T-1531: los sonidos nuevos del 20-nov (carpetas 11_Nova a 18_Radio_Musica y los que se añadieron a las 01 a 10). Igual que <see cref="MapaSonidos19nov"/>:
    /// la lista de IDs nuevos (siempre encendidos, con política en <see cref="TablaMezcla20nov"/>) y los alias de IDs viejos que el código aún pide.
    /// Si un alias aparece en las dos tablas, gana esta. <see cref="MapaSonidos19nov"/> consulta esta clase, así que quien ya pregunta ahí no cambia.
    /// </summary>
    public static class MapaSonidos20nov
    {
        /// <summary>IDs nuevos de 20-nov (los que no existían el 19-nov), uno por fila de <see cref="TablaMezcla20nov"/>.</summary>
        public static readonly string[] IdsNuevos = ArmarIds();

        /// <summary>
        /// ID viejo (el que el código ya pide o puede pedir) -> ID nuevo, según el LEEME_SONIDOS del 20-nov. T-1532 conecta los eventos finos directo al ID nuevo.
        /// Los cinco primeros son los archivos que T-1530 renombró.
        /// </summary>
        public static readonly MapaSonidos19nov.Par[] Pares =
        {
            // Integración: tono bajo para los robots pesados y el apagado (LEEME: "reciclar con el pitch más bajo").
            new MapaSonidos19nov.Par("ROB_PES_PASO", "NOV_PASOS_CAMINAR", 0.7f),
            new MapaSonidos19nov.Par("ROB_APAGADO", "ROB_SERVOS", 0.6f),
            new MapaSonidos19nov.Par("ROB_CAIDA", "ROB_MUERTE", 0.7f),
            new MapaSonidos19nov.Par("ARM_W07_RAYO", "ARM_W07_RAYO_LOOP"),
            new MapaSonidos19nov.Par("ARM_NOVA_DISPARO", "ARM_W07_GATILLO"),
            new MapaSonidos19nov.Par("ARM_NOVA_CARGA_DISPARO", "ARM_W07_SIN_ENERGIA"),
            new MapaSonidos19nov.Par("POL_NOVA_AVISO", "NOV_CAIDA_Y_ENCENDIDO"),
            new MapaSonidos19nov.Par("POL_RADIO_DESPACHO", "POL_RADIO_AVISO"),
            // Policía: el altavoz también sale de las voces "Mexican Police".
            new MapaSonidos19nov.Par("CAL_PATRULLA_MEGAFONO", "POL_RADIO_AVISO"),
            new MapaSonidos19nov.Par("CAL_SUBE_NIVEL", "POL_ESTRELLA_SUBE"),
            // Explosiones y rueda (la conexión fina la hace T-1532).
            new MapaSonidos19nov.Par("VEH_EXPLOSION", "EXP_CARRO"),
            new MapaSonidos19nov.Par("UI_CAMARA_LENTA", "EXO_RUEDA_CAMARA_LENTA_ENTRAR"),
            // Armas y dinero.
            new MapaSonidos19nov.Par("ARM_VACIO", "ARM_SIN_BALAS"),
            new MapaSonidos19nov.Par("ARM_CASQUILLO", "CAS_CHICO"),
            new MapaSonidos19nov.Par("ECO_DINERO_RECOGER", "INT_RECOGER_DINERO"),
            // HUD y misiones (LEEME: se repiten con lo que ya hay).
            new MapaSonidos19nov.Par("UI_GUARDADO", "HUD_NOTIFICACION"),
            new MapaSonidos19nov.Par("UI_OBJETIVO_ACTUALIZADO", "HUD_NOTIFICACION"),
            new MapaSonidos19nov.Par("UI_OBJETIVO_FALLIDO", "UI_ERROR"),
            new MapaSonidos19nov.Par("UI_CHECKPOINT", "UI_SELECCIONAR"),
            new MapaSonidos19nov.Par("UI_MARCADOR", "UI_SELECCIONAR"),
            new MapaSonidos19nov.Par("UI_INDICADOR_DANIO", "IMP_BALA_ZUMBIDO"),
            new MapaSonidos19nov.Par("UI_TELEFONO", "HUD_MENSAJE_CELULAR"),
        };

        /// <summary>IDs cuyos archivos siguen en Assets pero ya no se usan (LEEME 20-nov: las voces de despachador). El catálogo no los lee.</summary>
        public static readonly string[] IdsDescartados = { "POL_RADIO_DESPACHO" };

        /// <summary>Carpetas nuevas del 20-nov dentro de Assets/Arte/Audio.</summary>
        public static readonly string[] Carpetas =
        {
            "11_Nova", "12_NPCs", "13_Robots", "14_Ambiente", "15_Exo_Implantes", "16_Casino", "17_Misiones_HUD", "18_Radio_Musica",
        };

        // Los interiores comparten el prefijo INT_ con las puertas y el hackeo: son ambiente solo estos.
        private static readonly string[] InterioresAmbiente =
        {
            "INT_TALLER_CANEPA", "INT_TIENDA_TINO_RADIO", "INT_TIENDA_TINO_VENTILADOR", "INT_ARMERIA_VENTILADOR", "INT_BODEGA_GOTERAS",
        };

        private static readonly Dictionary<string, MapaSonidos19nov.Par> PorViejo = ArmarPares();
        private static readonly Dictionary<string, TablaMezcla20nov.Fila> PorId = ArmarFilas();

        private static string[] ArmarIds()
        {
            string[] ids = new string[TablaMezcla20nov.Filas.Length];
            for (int i = 0; i < ids.Length; i++) ids[i] = TablaMezcla20nov.Filas[i].Id;
            return ids;
        }

        private static Dictionary<string, MapaSonidos19nov.Par> ArmarPares()
        {
            Dictionary<string, MapaSonidos19nov.Par> d = new Dictionary<string, MapaSonidos19nov.Par>();
            foreach (MapaSonidos19nov.Par p in Pares) d[p.Viejo] = p;
            return d;
        }

        private static Dictionary<string, TablaMezcla20nov.Fila> ArmarFilas()
        {
            Dictionary<string, TablaMezcla20nov.Fila> d = new Dictionary<string, TablaMezcla20nov.Fila>();
            foreach (TablaMezcla20nov.Fila f in TablaMezcla20nov.Filas) d[f.Id] = f;
            return d;
        }

        /// <summary>Si el ID es de los nuevos del 20-nov.</summary>
        public static bool EsNuevo(string id) => id != null && PorId.ContainsKey(id);

        /// <summary>La fila de mezcla del ID nuevo.</summary>
        public static bool TryFila(string id, out TablaMezcla20nov.Fila fila)
        {
            if (id != null) return PorId.TryGetValue(id, out fila);
            fila = default;
            return false;
        }

        /// <summary>El ID nuevo con el que suena un ID viejo; null si esta tabla no lo mapea.</summary>
        public static string Nuevo(string viejo) => viejo != null && PorViejo.TryGetValue(viejo, out MapaSonidos19nov.Par p) ? p.Nuevo : null;

        public static bool EstaMapeado(string viejo) => viejo != null && PorViejo.ContainsKey(viejo);

        public static float TonoBase(string viejo) => viejo != null && PorViejo.TryGetValue(viejo, out MapaSonidos19nov.Par p) ? p.TonoBase : 1f;

        public static bool Enciende(string viejo) => viejo != null && PorViejo.TryGetValue(viejo, out MapaSonidos19nov.Par p) && p.Encender;

        /// <summary>Si el ID es un interior que va como ambiente (no una puerta ni el hackeo).</summary>
        public static bool EsInteriorAmbiente(string id) => Array.IndexOf(InterioresAmbiente, id) >= 0;

        /// <summary>Si el ID es de los que el catálogo no debe leer.</summary>
        public static bool EsDescartado(string id) => Array.IndexOf(IdsDescartados, id) >= 0;

        /// <summary>Cuántos bucles del mismo ID pueden sonar a la vez (0 si no es un bucle de esta tabla).</summary>
        public static int Bucles(string id) => id != null && PorId.TryGetValue(id, out TablaMezcla20nov.Fila f) ? f.Bucles : 0;
    }
}

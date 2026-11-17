using System;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>
    /// Reglas puras de mezcla por ID (T-593 y siguientes): límite de voces, volumen y variación de tono. Los sonidos largos de las armas
    /// (colas de 1 a 5 s) se cortaban unos a otros mal y se encimaban; aquí cada arma comparte un grupo de una sola voz.
    /// </summary>
    public static class ReglaMezclaSonidos
    {
        /// <summary>Grupo de lo que hace el arma activa: disparo, recarga, cambio, vacío, cola de ráfaga. Una sola voz: lo nuevo corta lo anterior.</summary>
        public const string GrupoArma = "arma";

        public const string GrupoClaxon = "claxon";

        // Prefijo de los pasos (sin ser un ID completo, para que la prueba de IDs no lo tome por uno).
        private static readonly string PrefijoPaso = "JUG_" + "PASO_";

        public const string GrupoArmaAccesorio = "arma-accesorio";
        public const string GrupoImpacto = "arma-impacto";
        public const string GrupoArmaEnemiga = "arma-enemiga";

        private static readonly PoliticaSonido SinPolitica = new PoliticaSonido(null, 0, 1f, 0f, 0f);

        /// <summary>T-620: voces puntuales y bucles que pueden sonar a la vez entre todos los IDs.</summary>
        public const int MaxVocesTotal = 12;

        /// <summary>T-620: un mismo ID no se repite antes de este tiempo (s).</summary>
        public const float SegundosMinimosPorId = 0.1f;

        /// <summary>T-620: volumen relativo parejo de todos los sonidos encendidos.</summary>
        public const float VolumenParejo = 0.8f;

        public const string IdExplosion = "VEH_EXPLOSION";

        /// <summary>T-620: los unicos IDs que suenan por defecto; el resto se prende uno por uno en los interruptores del CatalogoSonidos.</summary>
        public static readonly string[] IdsEncendidos =
        {
            "ARM_PIS_DISPARO", "ARM_ESC_DISPARO", "ARM_FUS_DISPARO", "ARM_SNI_DISPARO", "ARM_PIS_RECARGA", "ARM_ESC_RECARGA_CARTUCHO",
            "ARM_FUS_RECARGA", "ARM_SNI_RECARGA", "ARM_VACIO", "ARM_BALA_IMPACTO_CARNE", "PUN_GOLPE_CARNE", "JUG_DANIO", "JUG_CAE_DERROTADO",
            "VEH_VIEJO_MOTOR_RALENTI", "VEH_VIEJO_MOTOR_ACELERA", "VEH_CHOQUE_LEVE", "MUN_CHISPAS_ELECTRICAS", IdExplosion, "CAL_PATRULLA_SIRENA",
            "UI_MOVER", "UI_ACEPTAR", "UI_CANCELAR", "ZON_CONQUISTADA", "ECO_COMPRA",
        };

        /// <summary>
        /// T-977: los que regresan, en el orden de docs/sonidos-sugerencias.md. Cada uno trae su volumen, su limite de voces y su tiempo entre
        /// repeticiones (no suenan parejos: sus archivos van de -46 a -11 dB). Los molestos de esa lista (voces de NPC, jadeos, claxon, hologramas,
        /// neon, perros, aire, disparos lejanos, UI_TEXTO) siguen apagados.
        /// </summary>
        public static readonly string[] IdsRegresados =
        {
            ReglaAmbienteZona.CalleNoche, "VEH_PUERTA_ABRIR", "VEH_PUERTA_CERRAR", "ARM_BALA_IMPACTO_CONCRETO", "PAN_RIVAL_ALERTA", "PAN_JEFE_ENTRADA",
            "UI_OBJETIVO_NUEVO", "UI_OBJETIVO_CUMPLIDO", IdDerrape, "JUG_PASO_ASFALTO", "CAL_SUBE_NIVEL", "ECO_DINERO_RECOGER",
            "MUN_PUERTA_CASA_ABRIR", "MUN_PUERTA_CASA_CERRAR", ReglaAmbienteZona.Casino, ReglaAmbienteZona.Clinica,
        };

        /// <summary>T-977: los que no se prenden sin probar (docs/sonidos-sugerencias.md: hacian spam).</summary>
        public static readonly string[] IdsMolestos =
        {
            "PAN_VECINO_AUXILIO", "PAN_SILBIDO_LLAMAR", "PAN_RIVAL_PROVOCA", "PAN_ALIADO_SIGUE", "PAN_ALIADO_ESPERA", "JUG_CORRER_RESPIRA", "VEH_CLAXON_TRAFICO",
            "VEH_CLAXON_VIEJO", "MUN_HOLOGRAMA", "MUN_NEON_ZUMBIDO", "AMB_NEON_ALTO", "MUN_PERRO_LADRA", "MUN_AIRE_ACONDICIONADO", "ARM_DISPARO_LEJANO", "UI_TEXTO",
        };

        public const string IdDerrape = "VEH_FRENO_DERRAPE";

        /// <summary>Si el ID es de los que suenan por defecto (los minimos de T-620 y los que regresan en T-977).</summary>
        public static bool EncendidoPorDefecto(string id) => Array.IndexOf(IdsEncendidos, id) >= 0 || Array.IndexOf(IdsRegresados, id) >= 0;

        /// <summary>
        /// T-977: nivel de cada ambiente sobre la ganancia de la seccion, para que todos queden cerca de -45 dB aunque sus archivos no:
        /// calle de noche -46.5 dB, casino -37.8 dB, clinica -27.1 dB (RMS medido). Un ambiente sin nivel propio va a la mitad.
        /// </summary>
        public static float VolumenAmbiente(string id)
        {
            switch (id)
            {
                case ReglaAmbienteZona.CalleNoche: return 1f;
                case ReglaAmbienteZona.Casino: return 0.55f;
                case ReglaAmbienteZona.Clinica: return 0.13f;
                default: return 0.5f;
            }
        }

        /// <summary>Si el mismo ID se pidio muy pronto despues de la vez anterior (<paramref name="antes"/>).</summary>
        public static bool Repite(float ahora, float antes, float segundosMinimosPolitica)
        {
            return ahora - antes < Math.Max(segundosMinimosPolitica, SegundosMinimosPorId);
        }

        /// <summary>Politica del ID (sin etiqueta de voz). Un ID sin regla suena normal: sin limite, volumen 1 y tono 1. Los encendidos suenan parejos.</summary>
        public static PoliticaSonido Para(string id)
        {
            PoliticaSonido p = ParaBase(id);
            if (Array.IndexOf(IdsEncendidos, id) < 0) return p;
            return new PoliticaSonido(p.Grupo, p.MaxVoces, VolumenParejo, p.VariacionTono, p.SegundosMinimos, p.DistanciaMaxima);
        }

        private static PoliticaSonido ParaBase(string id)
        {
            if (string.IsNullOrEmpty(id)) return SinPolitica;
            if (id.StartsWith("ARM_", StringComparison.Ordinal))
            {
                return Arma(id);
            }
            switch (id)
            {
                // T-597: voces del jugador y de los pandilleros; cada una corta la anterior de su tipo y varía un poco.
                case "JUG_DANIO": return new PoliticaSonido("voz-jugador", 1, 0.9f, 0.04f, 0.12f, 30f);
                case "JUG_CAE_DERROTADO": return new PoliticaSonido("voz-jugador", 1, 1f, 0.03f, 0f, 30f);
                case "JUG_CORRER_RESPIRA": return new PoliticaSonido("voz-jugador", 1, 1f, 0.03f, 2f, 15f);
                case "JUG_SALTO": return new PoliticaSonido("voz-jugador", 1, 0.8f, 0.05f, 0.2f, 20f);
                case "PAN_RIVAL_DANIO": return new PoliticaSonido("voz-rival-danio", 2, 0.85f, 0.08f, 0.1f, 35f);
                case "PAN_RIVAL_CAE": return new PoliticaSonido("voz-rival-cae", 3, 0.9f, 0.08f, 0f, 35f);
                // T-977: un solo "¡Ey!" aunque te descubran varios a la vez (el archivo es fuerte, -11 dB).
                case "PAN_RIVAL_ALERTA": return new PoliticaSonido("voz-rival-alerta", 1, 0.6f, 0.08f, 2.5f, 35f);
                case "PAN_JEFE_ENTRADA": return new PoliticaSonido("voz-jefe", 1, 0.9f, 0f, 10f, 45f);
                // T-977: avisos de objetivo, calor y dinero: uno a la vez y sin metralleta.
                case "UI_OBJETIVO_NUEVO":
                case "UI_OBJETIVO_CUMPLIDO": return new PoliticaSonido("objetivo", 1, 0.6f, 0f, 1f);
                case "CAL_SUBE_NIVEL": return new PoliticaSonido("calor", 1, 0.65f, 0f, 3f);
                case "ECO_DINERO_RECOGER": return new PoliticaSonido("dinero", 1, 0.55f, 0.05f, 0.25f, 20f);
                case "MUN_PUERTA_CASA_ABRIR":
                case "MUN_PUERTA_CASA_CERRAR": return new PoliticaSonido("puerta-casa", 1, 0.6f, 0.04f, 0.5f, 20f);
                case "PAN_ALIADO_RECLUTA":
                case "PAN_ALIADO_CAE": return new PoliticaSonido("voz-aliado", 1, 0.9f, 0.05f, 0f, 30f);
            }
            if (id.StartsWith(PrefijoPaso, StringComparison.Ordinal))
            {
                // T-596: pasos bajos, con tono distinto cada vez, y de poco alcance.
                // T-977: solo los del jugador, una voz y bajitos (debajo de los disparos y las voces).
                return new PoliticaSonido("paso", 1, 0.45f, 0.06f, 0.15f, 20f);
            }
            if (id.StartsWith("VEH_", StringComparison.Ordinal))
            {
                return Vehiculo(id);
            }
            if (id == "UI_TEXTO")
            {
                // T-594: el tic de un aviso o línea de diálogo: bajito, con un poco de variación y sin repetirse como metralleta.
                return new PoliticaSonido("texto", 1, 0.5f, 0.05f, 0.12f);
            }
            return SinPolitica;
        }

        // T-595: el claxon y el paso de los carros duran de 3 a 5 s; con tráfico sonaban todos a la vez, siempre igual y se oían de lejos.
        private static PoliticaSonido Vehiculo(string id)
        {
            switch (id)
            {
                case "VEH_CLAXON_TRAFICO": return new PoliticaSonido(GrupoClaxon, 2, 0.6f, 0.12f, 0.8f, 25f);
                case "VEH_CLAXON_VIEJO": return new PoliticaSonido(GrupoClaxon, 2, 0.7f, 0.08f, 0.2f, 35f);
                case "VEH_PASA_CERCA": return new PoliticaSonido("pasa-cerca", 1, 0.8f, 0.06f, 1.5f, 30f);
                case "VEH_VIEJO_ARRANQUE": return new PoliticaSonido("arranque", 1, 0.8f, 0.04f, 0f, 30f);
                case "VEH_PUERTA_ABRIR":
                case "VEH_PUERTA_CERRAR": return new PoliticaSonido("puerta-carro", 1, 0.6f, 0.04f, 0.5f, 25f);
                // T-977: el derrape es un bucle (el archivo dura 11.6 s) que el carro corta al terminar; el tiempo minimo evita que rearranque a cada rato.
                case IdDerrape: return new PoliticaSonido("derrape", 1, 0.5f, 0.05f, 1.5f, 35f);
                case "VEH_CHOQUE_LEVE":
                case "VEH_CHOQUE_FUERTE": return new PoliticaSonido("choque", 2, 0.85f, 0.05f, 0.15f);
                default: return SinPolitica;
            }
        }

        private static PoliticaSonido Arma(string id)
        {
            if (id == "ARM_DISPARO_LEJANO") return new PoliticaSonido(GrupoArmaEnemiga, 3, 0.6f, 0.05f, 0.05f);
            if (id.EndsWith("_DISPARO", StringComparison.Ordinal))
            {
                // T-676: el disparo suena siempre igual (sin variación de tono; el catálogo también elige siempre el mismo clip).
                return new PoliticaSonido(GrupoArma, 1, id == "ARM_SNI_DISPARO" ? 0.9f : 0.8f, 0f, 0f);
            }
            if (id.EndsWith("_RECARGA", StringComparison.Ordinal) || id == "ARM_ESC_RECARGA_CARTUCHO")
            {
                return new PoliticaSonido(GrupoArma, 1, 0.7f, 0.02f, 0f);
            }
            switch (id)
            {
                case "ARM_FUS_RAFAGA_COLA": return new PoliticaSonido(GrupoArma, 1, 0.5f, 0f, 0f);
                case "ARM_CAMBIAR":
                case "ARM_PIS_SACAR": return new PoliticaSonido(GrupoArma, 1, 0.6f, 0.03f, 0f);
                case "ARM_VACIO": return new PoliticaSonido(GrupoArma, 1, 0.5f, 0.03f, 0f);
                case "ARM_CASQUILLO": return new PoliticaSonido(GrupoArmaAccesorio, 2, 0.4f, 0.1f, 0.04f);
                case "ARM_ESC_BOMBEO":
                case "ARM_SNI_CERROJO": return new PoliticaSonido(GrupoArmaAccesorio, 2, 0.7f, 0.02f, 0f);
                // T-977: el tiro que falla pega en el concreto: una vez por tiro, mas bajo que el disparo y sin repetirse en rafaga.
                case "ARM_BALA_IMPACTO_CONCRETO": return new PoliticaSonido(GrupoImpacto, 2, 0.5f, 0.08f, 0.12f, 30f);
                case "ARM_BALA_IMPACTO_CARNE":
                case "ARM_BALA_IMPACTO_METAL": return new PoliticaSonido(GrupoImpacto, 2, 0.7f, 0.06f, 0f);
                default: return SinPolitica;
            }
        }

        /// <summary>Tono para un valor al azar en [0, 1): entre 1 - variacion y 1 + variacion.</summary>
        public static float Tono(float variacion, float azar)
        {
            return 1f + (azar * 2f - 1f) * variacion;
        }
    }
}

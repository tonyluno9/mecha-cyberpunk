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

        /// <summary>T-1309: un disparo propio por arma (subfusil, revólver, lanzagranadas, rifle de plasma) y el chispeo al cargar el plasma.</summary>
        public static readonly string[] IdsArmasT1309 = { "ARM_SUB_DISPARO", "ARM_REV_DISPARO", "ARM_LGR_DISPARO", "ARM_PLA_DISPARO", "ARM_PLA_CARGA", "CAL_DRON_ZUMBIDO" }; // T-1309b: y el motor de los drones

        /// <summary>T-977: los que no se prenden sin probar (docs/sonidos-sugerencias.md: hacian spam).</summary>
        public static readonly string[] IdsMolestos =
        {
            "PAN_VECINO_AUXILIO", "PAN_SILBIDO_LLAMAR", "PAN_RIVAL_PROVOCA", "PAN_ALIADO_SIGUE", "PAN_ALIADO_ESPERA", "JUG_CORRER_RESPIRA", "VEH_CLAXON_TRAFICO",
            "MUN_HOLOGRAMA", "MUN_NEON_ZUMBIDO", "AMB_NEON_ALTO", "MUN_PERRO_LADRA", "MUN_AIRE_ACONDICIONADO", "ARM_DISPARO_LEJANO", "UI_TEXTO",
        };

        /// <summary>T-1471: variación chica del tono del disparo (±2.5 %).</summary>
        public const float VariacionDisparo = 0.025f;

        /// <summary>T-1471: variación chica del volumen del disparo (hasta -5 %).</summary>
        public const float VariacionVolumenDisparo = 0.05f;

        public const string IdDerrape = "VEH_FRENO_DERRAPE";

        /// <summary>Si el ID es de los que suenan por defecto (los minimos de T-620 y los que regresan en T-977).</summary>
        public static bool EncendidoPorDefecto(string id) =>
            Array.IndexOf(IdsEncendidos, id) >= 0 || Array.IndexOf(IdsRegresados, id) >= 0 || Array.IndexOf(IdsArmasT1309, id) >= 0
            || MapaSonidos19nov.EsNuevo(id) || MapaSonidos19nov.Enciende(id); // T-1471: los nuevos y los viejos mapeados

        /// <summary>T-1471: los mapeados que no son disparo ni recarga llevan el volumen de su política nueva (UI 0.55, impactos 0.65...), no el parejo de T-620.</summary>
        public static bool TieneVolumenPropio(string id) =>
            MapaSonidos19nov.EsNuevo(id) || (MapaSonidos19nov.EstaMapeado(id) && (!id.StartsWith("ARM_", StringComparison.Ordinal) || id.StartsWith("ARM_" + "BALA_", StringComparison.Ordinal)));

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
                default: return MapaSonidos20nov.TryFila(id, out TablaMezcla20nov.Fila fila) ? fila.Volumen : 0.5f; // T-1531: los de 20-nov traen su nivel (ya compensado por su archivo)
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
            if (EsSinPolitica(p) && MapaSonidos19nov.Nuevo(id) != null)
            {
                // T-1471: un ID viejo sin regla propia usa la del ID nuevo con el que suena.
                PoliticaSonido n = ParaDelNuevo(MapaSonidos19nov.Nuevo(id));
                p = new PoliticaSonido(n.Grupo, n.MaxVoces, n.Volumen, n.VariacionTono, n.SegundosMinimos, n.DistanciaMaxima, MapaSonidos19nov.TonoBase(id), n.VariacionVolumen);
            }
            if (Array.IndexOf(IdsEncendidos, id) < 0 || TieneVolumenPropio(id)) return p;
            return new PoliticaSonido(p.Grupo, p.MaxVoces, VolumenParejo, p.VariacionTono, p.SegundosMinimos, p.DistanciaMaxima, p.TonoBase, p.VariacionVolumen);
        }

        private static bool EsSinPolitica(PoliticaSonido p) => p.Grupo == null && p.MaxVoces == 0 && p.Volumen == 1f && p.VariacionTono == 0f && p.SegundosMinimos == 0f;

        /// <summary>T-1471: prioridad de AudioSource (0 = más alta, 256 = más baja): disparos y voces arriba, impactos y vehículos en medio, ambiente abajo.</summary>
        public static int Prioridad(string id)
        {
            if (string.IsNullOrEmpty(id)) return 128;
            // T-1531: ambiente, clima, interiores y objetos abajo; disparos, avisos de HUD y voces arriba.
            if (id.StartsWith("AMB_", StringComparison.Ordinal) || id.StartsWith("CLI_", StringComparison.Ordinal) || id.StartsWith("OBJ_", StringComparison.Ordinal)
                || id == "FUE_LOOP_CHICO" || id == "FUE_LOOP_GRANDE" || MapaSonidos20nov.EsInteriorAmbiente(id)) return 200;
            if ((id.StartsWith("ARM_", StringComparison.Ordinal) && id.Contains("_DISPARO")) || id == "DRN_PES_AMETRALLADORA") return 32;
            if (id.StartsWith("HUD_", StringComparison.Ordinal) || id.StartsWith("MIS_", StringComparison.Ordinal)) return 40;
            if (id.StartsWith("PAN_", StringComparison.Ordinal) || id.StartsWith("JUG_", StringComparison.Ordinal) || id.StartsWith("POL_", StringComparison.Ordinal)
                || id.StartsWith("NPC_", StringComparison.Ordinal) || id.StartsWith("ROB_VOZ", StringComparison.Ordinal) || id == "NOV_CAIDA_Y_ENCENDIDO") return 48;
            if (id.StartsWith("PAS_", StringComparison.Ordinal) || id.StartsWith(PrefijoPaso, StringComparison.Ordinal) || id == "NOV_PASOS_CAMINAR" || id == "NOV_PASOS_CORRER") return 160;
            if (id.StartsWith("EXP_", StringComparison.Ordinal) || id.StartsWith("IMP_", StringComparison.Ordinal) || id.StartsWith("MEL_", StringComparison.Ordinal)
                || id.StartsWith("NOV_", StringComparison.Ordinal) || id.StartsWith("ROB_", StringComparison.Ordinal) || id.StartsWith("EXO_", StringComparison.Ordinal)) return 64;
            return 100;
        }

        /// <summary>T-1471: si este ID abate un poco el ambiente un instante (disparos y explosiones).</summary>
        public static bool BajaAmbiente(string id) =>
            !string.IsNullOrEmpty(id) && ((id.StartsWith("ARM_", StringComparison.Ordinal) && id.EndsWith("_DISPARO", StringComparison.Ordinal)) || id.StartsWith("EXP_", StringComparison.Ordinal) || id == IdExplosion);

        /// <summary>T-1471: cuántos bucles del mismo ID pueden sonar a la vez: la sirena tantos como voces de su política (patrullas); el resto, uno.</summary>
        public static int MaxBuclesDelId(string id)
        {
            switch (id)
            {
                case "POL_SIRENA_LOOP":
                case "CAL_PATRULLA_SIRENA": return Math.Max(1, Para(id).MaxVoces); // una por patrulla
                case "VEH_NORMAL_RALENTI":
                case "MOTO_RALENTI":
                case "VEH_PESADO_RALENTI": return MaxMotoresRalenti; // un ralentí por carro cercano
                case "VEH_ELECTRICO_MOVER":
                case "FUE_LOOP_CHICO": return 3;
                case "VEH_ALARMA": return 2;
                default: return Math.Max(1, MapaSonidos20nov.Bucles(id)); // T-1531: ambulancia, rotores, fuego grande y objetos que zumban admiten varios
            }
        }

        /// <summary>T-1471: ralentís de carros que pueden sonar a la vez (de las 12 voces del gestor).</summary>
        public const int MaxMotoresRalenti = 4;

        /// <summary>T-1471: un tiro 3D a menos de esta distancia (m) del jugador cuenta como suyo (grupo de arma de una voz); más lejos es ajeno.</summary>
        public const float DistanciaPropia = 3f;

        /// <summary>T-1471: el ambiente baja a este factor (0.3 s) cuando suena un disparo o una explosión.</summary>
        public const float FactorDucking = 0.7f;

        /// <summary>T-1471: duración (s) del bajón del ambiente.</summary>
        public const float SegundosDucking = 0.3f;

        private static PoliticaSonido ParaBase(string id)
        {
            if (string.IsNullOrEmpty(id)) return SinPolitica;
            if (MapaSonidos20nov.TryFila(id, out TablaMezcla20nov.Fila fila)) return Politica20nov(fila); // T-1531
            if (MapaSonidos19nov.EsNuevo(id)) return ParaNuevo(id);
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
                // T-1531: el altavoz de la patrulla sale de las voces "Mexican Police" (POL_RADIO_AVISO) pero se oye más lejos.
                case "CAL_PATRULLA_MEGAFONO": return new PoliticaSonido("megafono", 1, 0.7f, 0.03f, 3f, 45f, 1f, 0.06f);
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
                // T-676 (T-1471): el mismo clip por arma (lo elige el catálogo) pero con variación chica de tono (±2.5 %) y de volumen (5 %): nada suena idéntico.
                return new PoliticaSonido(GrupoArma, 1, id == "ARM_SNI_DISPARO" ? 0.9f : 0.8f, VariacionDisparo, 0f, 100f, MapaSonidos19nov.TonoBase(id), VariacionVolumenDisparo);
            }
            if (id.EndsWith("_RECARGA", StringComparison.Ordinal) || id == "ARM_ESC_RECARGA_CARTUCHO")
            {
                return new PoliticaSonido(GrupoArma, 1, 0.7f, 0.02f, 0f);
            }
            switch (id)
            {
                // T-1309: chispeo en bucle de la carga del rifle de plasma (suave, no tapa el resto).
                case "CAL_DRON_ZUMBIDO": return new PoliticaSonido("dron", 3, 0.45f, 0f, 0f, 60f); // T-1309b: motor en bucle, hasta 3 drones a la vez
                case "ARM_PLA_CARGA": return new PoliticaSonido(GrupoArmaAccesorio, 1, 0.5f, 0f, 0f);
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

        private static PoliticaSonido P(string grupo, int voces, float vol, float tono, float minimo, float distancia, float vvol = 0.06f)
        {
            return new PoliticaSonido(grupo, voces, vol, tono, minimo, distancia, 1f, vvol);
        }

        private static PoliticaSonido ParaDelNuevo(string nuevo) => MapaSonidos20nov.TryFila(nuevo, out TablaMezcla20nov.Fila f) ? Politica20nov(f) : ParaNuevo(nuevo);

        /// <summary>T-1531: la política de un ID nuevo del 20-nov desde su fila de <see cref="TablaMezcla20nov"/>.</summary>
        private static PoliticaSonido Politica20nov(TablaMezcla20nov.Fila f)
        {
            return new PoliticaSonido(f.Grupo, f.Voces, f.Volumen, f.Tono, f.Minimo, f.Distancia, 1f, f.VariacionVolumen);
        }

        /// <summary>T-1471: política de cada ID nuevo (por ID y por prefijo). Todos varían tono o volumen; los del mundo traen distancia máxima.</summary>
        public static PoliticaSonido ParaNuevo(string id)
        {
            switch (id)
            {
                // Armas: disparos arriba (0.8 a 0.9), con el mismo clip por arma y variación chica (T-676).
                case "ARM_W01_DISPARO": return new PoliticaSonido(GrupoArma, 1, 0.8f, VariacionDisparo, 0f, 100f, 1f, VariacionVolumenDisparo);
                case "ARM_W02_DISPARO": return new PoliticaSonido(GrupoArma, 1, 0.85f, VariacionDisparo, 0f, 100f, 1f, VariacionVolumenDisparo);
                case "ARM_W03_DISPARO": return new PoliticaSonido(GrupoArma, 1, 0.8f, VariacionDisparo, 0f, 120f, 1f, VariacionVolumenDisparo);
                case "ARM_W04_DISPARO": return new PoliticaSonido(GrupoArma, 1, 0.9f, VariacionDisparo, 0f, 150f, 1f, VariacionVolumenDisparo);
                case "ARM_W06_DISPARO": return new PoliticaSonido(GrupoArma, 1, 0.85f, VariacionDisparo, 0f, 110f, 1f, VariacionVolumenDisparo);
                case "ARM_W07_DISPARO": return new PoliticaSonido(GrupoArma, 1, 0.8f, VariacionDisparo, 0f, 100f, 1f, VariacionVolumenDisparo);
                case "ARM_W03_RECARGA_CARGADOR": return P(GrupoArma, 1, 0.7f, 0.02f, 0f, 0f, 0.05f);
                case "ARM_W06_SACAR": return P(GrupoArma, 1, 0.6f, 0.03f, 0f, 0f, 0.05f);
                case "ARM_W02_BOMBA":
                case "ARM_W04_CERROJO": return P(GrupoArmaAccesorio, 2, 0.7f, 0.02f, 0f, 0f, 0.05f);
                case "ARM_W07_CARGA": return P(GrupoArmaAccesorio, 1, 0.5f, 0.03f, 0f, 0f, 0.05f);
                case "ARM_GRANADA_SEGURO": return P(GrupoArmaAccesorio, 2, 0.6f, 0.04f, 0.1f, 40f);
                case "ARM_GRANADA_REBOTE": return P("granada-rebote", 2, 0.55f, 0.06f, 0.1f, 40f);
                case "ARM_COLA_CALLE": return P("arma-cola", 1, 0.5f, 0.03f, 0.1f, 150f);
                case "IMP_LASER_DISPARO": return P("laser", 1, 0.7f, 0.04f, 0.1f, 80f);
                case "IMP_MISILES_FIJAR": return P("misiles", 1, 0.6f, 0.03f, 0.5f, 60f);
                // Impactos 0.6 a 0.7, de poco alcance y sin metralleta.
                case "IMP_BALA_CABEZA": return P(GrupoImpacto, 2, 0.7f, 0.06f, 0.1f, 35f, 0.08f);
                case "IMP_BALA_CRISTAL": return P(GrupoImpacto, 2, 0.6f, 0.06f, 0.1f, 35f, 0.08f);
                case "IMP_BALA_CARNE":
                case "IMP_BALA_CONCRETO":
                case "IMP_BALA_METAL":
                case "IMP_BALA_ROBOT": return P(GrupoImpacto, 2, 0.65f, 0.06f, 0.1f, 35f, 0.08f);
                case "IMP_BALA_ZUMBIDO": return P("bala-zumbido", 2, 0.5f, 0.08f, 0.25f, 30f, 0.08f);
                // Explosiones y fuego.
                case "EXP_GRANADA": return P("explosion", 2, 0.9f, 0.04f, 0.1f, 160f, 0.05f);
                case "EXP_ONDA": return P("explosion", 2, 0.9f, 0.04f, 0.1f, 200f, 0.05f);
                case "FUE_LOOP_CHICO": return P("fuego", 3, 0.3f, 0f, 0.5f, 30f, 0.1f);
                // Jugador y cuerpo a cuerpo.
                case "JUG_ATERRIZAJE": return P("jugador-cuerpo", 1, 0.6f, 0.05f, 0.3f, 25f);
                case "JUG_RODADA": return P("jugador-cuerpo", 1, 0.55f, 0.05f, 0.3f, 25f);
                case "JUG_TREPAR": return P("jugador-cuerpo", 1, 0.5f, 0.05f, 0.3f, 25f);
                case "MEL_GOLPE":
                case "MEL_GOLPE_AIRE":
                case "MEL_GOLPE_CARNE": return P("melee", 2, 0.75f, 0.05f, 0.1f, 30f);
                // Interacción.
                case "INT_PUERTA_CRISTAL":
                case "INT_PUERTA_MADERA":
                case "INT_PUERTA_METAL":
                case "INT_PUERTA_REJA": return P("puerta-interaccion", 2, 0.6f, 0.04f, 0.3f, 22f);
                case "INT_HACKEO_EXITO":
                case "INT_HACKEO_FALLO": return P("hackeo", 1, 0.6f, 0.03f, 0.5f, 15f);
                case "INT_TECLEO_HACKEO": return P("hackeo-tecleo", 1, 0.4f, 0.05f, 0.1f, 10f);
                case "INT_INTERRUPTOR_LUZ":
                case "INT_MALETIN_CERRAR": return P("interaccion", 1, 0.5f, 0.04f, 0.2f, 15f);
                case "INT_RECOGER_OBJETO": return P("dinero", 1, 0.55f, 0.05f, 0.25f, 20f);
                // Vehículos.
                case "VEH_NORMAL_ARRANQUE":
                case "VEH_NORMAL_APAGAR":
                case "MOTO_ARRANQUE":
                case "MOTO_APAGAR":
                case "VEH_ELECTRICO_ARRANQUE": return P("arranque", 1, 0.6f, 0.03f, 0.3f, 50f, 0.05f);
                case "VEH_PESADO_ARRANQUE":
                case "VEH_PESADO_APAGAR": return P("arranque", 1, 0.6f, 0.03f, 0.3f, 60f, 0.05f);
                case "VEH_NORMAL_RALENTI":
                case "MOTO_RALENTI": return P("motor", 4, 0.5f, 0.03f, 0.3f, 45f, 0.05f);
                case "VEH_PESADO_RALENTI": return P("motor", 4, 0.5f, 0.03f, 0.3f, 60f, 0.05f);
                case "VEH_ELECTRICO_MOVER": return P("motor", 4, 0.45f, 0.03f, 0.3f, 40f, 0.05f);
                case "VEH_ACELERON":
                case "VEH_ELECTRICO_ACELERAR": return P("motor-acelera", 2, 0.55f, 0.05f, 0.5f, 50f, 0.06f);
                case "VEH_ELECTRICO_PASAR": return P("pasa-cerca", 1, 0.7f, 0.06f, 1.5f, 45f);
                case "VEH_ALARMA": return P("alarma", 1, 0.45f, 0.02f, 1f, 60f, 0.05f);
                case "VEH_CHOQUE": return P("choque", 2, 0.75f, 0.05f, 0.15f, 60f);
                case "VEH_CHOQUE_FUERTE": return P("choque", 2, 0.85f, 0.05f, 0.15f, 80f);
                case "VEH_CLAXON_CAMION": return P(GrupoClaxon, 2, 0.6f, 0.06f, 0.8f, 70f);
                case "VEH_CLAXON_NORMAL": return P(GrupoClaxon, 2, 0.6f, 0.08f, 0.8f, 40f);
                case "VEH_CRISTAL_ROMPER": return P("cristal", 2, 0.65f, 0.05f, 0.2f, 40f);
                case "VEH_FRENO_FUERTE": return P("derrape", 1, 0.5f, 0.05f, 1.5f, 40f);
                case "VEH_LLANTAS_GRAVA": return P("llantas", 1, 0.4f, 0.03f, 1f, 30f);
                case "VEH_PUENTE": return P("puente", 1, 0.5f, 0.04f, 1f, 40f);
                case "VEH_PUERTA_ABRIR":
                case "VEH_PUERTA_CERRAR": return P("puerta-carro", 1, 0.6f, 0.04f, 0.5f, 25f);
                case "VEH_TOPE": return P("tope", 1, 0.5f, 0.04f, 0.5f, 30f);
                // Policía.
                case "POL_SIRENA_LOOP": return P("sirena", 3, 0.7f, 0f, 0.5f, 120f, 0.05f);
                case "POL_SIRENA_CORTA": return P("sirena-corta", 1, 0.7f, 0.03f, 1f, 100f);
                case "POL_RADIO_AVISO": return P("radio-policia", 1, 0.6f, 0.03f, 2f, 25f);
                // Interfaz y HUD (2D, sin distancia).
                case "UI_LOGO_INTRO": return P("ui-logo", 1, 0.6f, 0f, 1f, 0f, 0.03f);
                case "UI_MENU_ABRIR":
                case "UI_MENU_CERRAR": return P("ui-menu", 1, 0.55f, 0.02f, 0.1f, 0f, 0.05f);
                case "UI_ERROR": return P("ui", 2, 0.6f, 0.03f, 0.1f, 0f, 0.05f);
                case "HUD_LOGRO":
                case "HUD_NOTIFICACION": return P("objetivo", 1, 0.6f, 0.02f, 1f, 0f, 0.05f);
                case "HUD_VIDA_BAJA": return P("vida-baja", 1, 0.55f, 0.02f, 2f, 0f, 0.05f);
                case "TIE_COMPRAR":
                case "TIE_EQUIPAR":
                case "TIE_DESEQUIPAR": return P("tienda", 1, 0.55f, 0.03f, 0.2f, 0f, 0.05f);
            }
            if (id.StartsWith("PAS_", StringComparison.Ordinal))
            {
                // Igual que JUG_PASO_: grupo "paso", una voz, bajitos, ±6 % de tono y poco alcance.
                return P("paso", 1, 0.45f, 0.06f, 0.15f, 20f, 0.1f);
            }
            if (id.StartsWith("UI_", StringComparison.Ordinal)) return P("ui", 2, 0.55f, 0.03f, 0.05f, 0f, 0.05f);
            if (id.StartsWith("CON_", StringComparison.Ordinal)) return P("consumible", 1, 0.6f, 0.03f, 0.3f, 0f, 0.05f);
            return P("nuevo", 2, 0.6f, 0.04f, 0.1f, 30f);
        }


        /// <summary>Tono para un valor al azar en [0, 1): entre 1 - variacion y 1 + variacion.</summary>
        public static float Tono(float variacion, float azar)
        {
            return 1f + (azar * 2f - 1f) * variacion;
        }
    }
}

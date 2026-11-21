using System.Collections.Generic;
using System.Linq;
using System.Text;
using MechaCyberpunk.Arte.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-1551: revisa y conecta las animaciones aprobadas de combate y eventos que seguían sin usarse (pedido de Tony 20-nov, punto 2). Todo se puede repetir (idempotente) y no reconstruye el
    /// controlador base ni toca ConstructorControladorBase. Qué hace <see cref="Aplicar"/>:
    /// 1) los papeles que no eran el jugador ni el civil (aliados, SKULLS, SNAKES, robots, profes, jefe y el hacker, que parte de SNAKES) mueren con las muertes aprobadas PJ01_Muerte_Frente y
    ///    PJ01_Muerte_Espaldas (antes PJ01_Muerte_Adelante/Atras, las viejas de Mixamo); la tabla está en <see cref="MuertesNoJugadorT1551"/> y la engancha ClipsNpcNovaT1506.AgregarATabla,
    ///    así un rearmado del controlador no la pierde;
    /// 2) E15 (centinela) usa E15_Cubrirse (aprobado 19-nov, sin usar) agachado y en cobertura (filas en ClipsNpcNovaT1506.OverridesExtra);
    /// 3) el policía (Policia.controller, propio) recibe PJ01_Dano_Frente al ser herido y PJ01_Muerte_Frente al caer;
    /// 4) si falta alguna de las dos configs de clips de una vez (ConfigClipsNovaNpc: caída de Nova, susto, plática, banca y agachado) la rehace.
    /// <see cref="Diagnostico"/> comprueba esto y confirma lo que ya estaba conectado (daño del jugador, derribos, robo de carro, vehículos, recargas, interacciones, NPC, Nova) y lista lo que sigue sin clip
    /// (<see cref="Matriz"/>). Orden: Aplicar y luego Diagnostico. Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.AnimacionesT1551.Aplicar (y ...Diagnostico).
    /// </summary>
    public static class AnimacionesT1551
    {
        private const string Etiqueta = "[T-1551] ";

        public const string RutaPolicia = "Assets/Arte/Personajes/Policia/Policia.controller";

        /// <summary>Estados de la matriz de acciones.</summary>
        public const string YaEstaba = "ya estaba", ConectadoAhora = "conectado ahora", SinClip = "sin clip", PendienteTony = "pendiente de Tony", OtraTarea = "otra tarea";

        /// <summary>
        /// Papeles (de ConstructorControladorBase.Papeles) que reciben las muertes aprobadas. Quedan fuera: Jugador (ya las trae), NPC (NPC_Muerte_*, que son el mismo clip) y Guardias
        /// (Guardia_Muerte y NPC_Muerte_Espaldas, el mismo clip también).
        /// </summary>
        public static readonly string[] PapelesConMuertesT1551 = { "Aliados", "SKULLS", "SNAKES", "Robots", "Profes", "Jefe" };

        /// <summary>Clave del controlador base -> clip PJ01 aprobado. El v2 del 21-nov de Muerte_Frente (51 cuadros, termina en el piso) y Muerte_Espaldas (46).</summary>
        public static (string Base, string Papel)[] MuertesNoJugadorT1551 => new[]
        {
            ("Muerte_Adelante", "PJ01_Muerte_Frente"), ("Muerte_Atras", "PJ01_Muerte_Espaldas"),
        };

        /// <summary>Claves de override que recalcula <see cref="AplicarOverrides"/> con las tablas.</summary>
        public static readonly string[] ClavesT1551 = { "Muerte_Adelante", "Muerte_Atras", "Loco_AgachadoIdle", "PJ01_Cobertura_Quieto" };

        /// <summary>Estado del Policia.controller -> clip PJ01 que le pone <see cref="AplicarPolicia"/>.</summary>
        public static readonly (string Estado, string Clip)[] ClipsPolicia =
        {
            ("RecibirGolpe", "PJ01_Dano_Frente"), ("Caer", "PJ01_Muerte_Frente"),
        };

        /// <summary>Lo llama ClipsNpcNovaT1506.AgregarATabla (desde ConstructorControladorBase.ConstruirClipsPorPapel): agrega las muertes de <see cref="MuertesNoJugadorT1551"/> a los papeles que toca.</summary>
        public static void AgregarATabla(Dictionary<string, (string Base, string Papel)[]> mapa)
        {
            foreach (string papel in PapelesConMuertesT1551)
            {
                if (mapa.ContainsKey(papel))
                {
                    mapa[papel] = ClipsNpcNovaT1506.Combinar(mapa[papel], MuertesNoJugadorT1551);
                }
            }
        }

        // ---------- Matriz de acciones ----------

        /// <summary>
        /// Acción -> clip -> estado, la tabla que se le entrega a Tony. "Conectado ahora" es lo de esta tarea; "ya estaba" lo confirma <see cref="Diagnostico"/>; "sin clip" es un faltante de Blender (el clip
        /// que aparece en la columna, o "-", no existe aprobado); "pendiente de Tony" se dejó como estaba; "otra tarea" son T-1550 (locomoción y salto) y T-1553 (E18, E16, conducta de los Nova).
        /// </summary>
        public static readonly (string Accion, string Clip, string Estado)[] Matriz =
        {
            // Daños y muertes.
            ("Daño de balazo del jugador, de frente y de espalda", "PJ01_Dano_Frente/PJ01_Dano_Espalda", YaEstaba),
            ("Daño del jugador por izquierda y derecha", "PJ01_Dano_Izquierda/PJ01_Dano_Derecha", YaEstaba),
            ("Golpe recibido fuerte (jugador y demás)", "PJ01_Pistola_Dano_Fuerte", YaEstaba),
            ("Daño de balazo de pandilleros, aliados, guardias, robots, profes y jefe", "PJ01_Dano_Frente/PJ01_Dano_Espalda", YaEstaba),
            ("Daño de balazo del civil, de frente y de espalda", "NPC_Balazo_Frente/NPC_Balazo_Espalda", YaEstaba),
            ("Daño del civil por izquierda y derecha", "-", SinClip),
            ("Herido cojeando (civil)", "-", SinClip),
            ("Muerte del jugador de frente y de espalda", "PJ01_Muerte_Frente/PJ01_Muerte_Espaldas", YaEstaba),
            ("Muerte de pandilleros, aliados, robots, profes, jefe y hacker de frente y de espalda", "PJ01_Muerte_Frente/PJ01_Muerte_Espaldas", ConectadoAhora),
            ("Muerte del policía y daño del policía", "PJ01_Muerte_Frente/PJ01_Dano_Frente", ConectadoAhora),
            ("Muerte del civil de frente y de espalda", "NPC_Muerte_Frente/NPC_Muerte_Espaldas", YaEstaba),
            ("Muerte de los guardias de Nova (GuardiaNova, E12)", "Guardia_Muerte", YaEstaba),
            ("Muerte de E15 y de E16", "E15_Muerte/E16_Muerte", YaEstaba),
            ("Muerte por explosión, atropello y caída de altura (jugador y pandilleros)", "PJ01_Muerte_Explosion/PJ01_Muerte_Atropello/PJ01_Muerte_CaidaAltura", YaEstaba),
            ("Muerte de un civil de Trafico por explosión o atropello (PeatonMundo no avisa la causa)", "PJ01_Muerte_Explosion/PJ01_Muerte_Atropello", SinClip),
            ("Derribo por explosión y levantarse", "PJ01_Derribo_Explosion/PJ01_Dano_Levantarse_BocaArriba", YaEstaba),
            ("Empujón de un maleante (lo recibe el jugador o el civil) y explosión cercana", "PJ01_Dano_Empujon/PJ01_Dano_ExplosionCercana", YaEstaba),
            ("Levantarse al iniciar y tras caer", "PJ01_Dano_Levantarse_BocaArriba/PJ01_Dano_Levantarse_BocaAbajo", YaEstaba),
            ("Muerte de E18", "E18_Muerte", OtraTarea),
            ("Muerte con chispas, tambaleo, cojear, bloquear con escudo, muerte explotando (robots)", "-", SinClip),
            ("Arrestado con manos arriba y esposado, cubrirse tras el carro y asomarse (policía propio)", "-", SinClip),
            // Vehículos.
            ("Subir al carro y bajar del carro (jugador, aliados, policía)", "PJ01_Carro_Entrar/PJ01_Carro_Salir", YaEstaba),
            ("Robar carro: sacar al conductor y subir", "PJ01_Carro_SacarConductor", YaEstaba),
            ("Conductor al que sacan del carro", "PJ01_Conductor_SerSacado", YaEstaba),
            ("Manejar el carro y la moto", "PJ01_Manejar_Carro/PJ01_Manejar_Moto", YaEstaba),
            ("Subir y bajar de la moto", "PJ01_Moto_Subir/PJ01_Moto_Bajar", YaEstaba),
            ("Pitar el claxon (se queda en la pose de manejar)", "-", SinClip),
            ("Tirarse del carro en movimiento, caer de la moto", "-", SinClip),
            ("Disparar desde el carro o la moto (usa la pose de pistola)", "-", SinClip),
            // Armas e interacciones.
            ("Recargar pistola", "PJ01_Pistola_Recargar/PJ01_Pistola_RecargarCaminar", YaEstaba),
            ("Recargar fusil, escopeta y francotirador", "PJ01_Rifle_Recargar", YaEstaba),
            ("Recargar revólver, subfusil, lanzagranadas y plasma", "-", SinClip),
            ("Lanzar granada", "-", SinClip),
            ("Abrir puerta, botón, recoger y teléfono", "PJ01_Inter_AbrirPuerta/PJ01_Inter_Boton/PJ01_Inter_Recoger/PJ01_Inter_Telefono", YaEstaba),
            ("Ver el teléfono", "PJ01_Inter_VerTelefono", YaEstaba),
            ("Beber, comer papitas, vendarse, sentarse en la silla del Doc, colgarse, nadar", "-", SinClip),
            // Implantes.
            ("Caminar y correr con implante, frenar, girar con implante", "PJ01_Caminar_ConImplante/PJ01_Correr_ConImplante", YaEstaba),
            ("Prótesis de ataque (idle, golpe y apuntar); están en imageassets pero sin importar", "PJ01_Protesis_Idle/PJ01_Protesis_Golpe/PJ01_Protesis_Apuntar", PendienteTony),
            ("Escudo, visor, salto potenciado, brazo láser y misiles (ataques)", "-", SinClip),
            // NPC.
            ("Asustarse, huir y agacharse cubriéndose la cabeza", "NPC_Asustarse/NPC_Huir_Corriendo/NPC_Agachado_CubriendoCabeza", YaEstaba),
            ("Platicar, sentado en la banca, esperar, caminar con celular", "NPC_Platicar_1/NPC_Platicar_2/NPC_Sentado_Banca/NPC_Esperar/NPC_Caminar_Celular", YaEstaba),
            ("Hacker tecleando, mesera con charola", "NPC_Hacker_Tecleando/NPC_Mesera_Charola", YaEstaba),
            ("Tirarse al piso, levantar las manos, grabar con el celular, discutir, señalar, comer, vender, barrer", "-", SinClip),
            ("Hacker voltea al jugador, mesera sirve, Doc cura e instala, Tino y Canepa", "-", SinClip),
            // Pandilleros.
            ("Provocar: burlarse al ver al jugador", "Skull_Burla", YaEstaba),
            ("Provocar: retar, gritar, sacar el arma amenazando", "-", SinClip),
            ("Empujar (el maleante empuja; el que lo recibe ya tiene su clip)", "-", SinClip),
            ("Patada, herido arrastrándose, E20 golpe con brazo robótico", "-", SinClip),
            ("Presumir (Nova_Flexing) de Nova y jefes: se queda en Punos_Guardia", "-", SinClip),
            ("E14 jefe: entrada, ataques especiales y fase herida (hay Jefe_* genéricos sin aprobar)", "Jefe_Ataque/Jefe_Burla", PendienteTony),
            // Nova.
            ("Caída del cielo, aterrizaje y levantarse de GuardiaNova, E12 y E18 (Guardia_Caida_* es la misma que E18_Caida_*)", "E18_Caida_Aire/E18_Caida_Aterrizaje/E18_Caida_Levantarse", YaEstaba),
            ("Caída del cielo propia de E15 y de E16", "-", SinClip),
            ("E15 cubrirse (agachado y en cobertura)", "E15_Cubrirse", ConectadoAhora),
            ("E15 impacto, caminar, correr, disparar", "E15_Impacto/E15_Caminar/E15_Correr/E15_Disparar", YaEstaba),
            ("E16 caminar, girar, golpe de escudo, impacto", "E16_Caminar/E16_Girar_Derecha/E16_Golpe_Escudo/E16_Impacto", YaEstaba),
            ("E16 caminar con escudo y cañón, E18 caminar, correr y golpe, conducta de los Nova", "E16_CaminarEscudo/E16_Disparar_Canon/E18_Caminar/E18_Correr/E18_Golpe_Der", OtraTarea),
            ("Locomoción y salto de PJ01 (trote, sprint, salto corriendo, rodada)", "PJ01_Trote/PJ01_Salto_Correr_Despegue", OtraTarea),
            ("Perro E17: caminar, correr, ladrar, morder, saltar, morir", "E17_Caminar/E17_Correr/E17_Ladrar/E17_Morder/E17_SaltoAtaque/E17_Muerte", YaEstaba),
            ("Perro E17: olfatear, impacto, salir de la cápsula", "-", SinClip),
            // Policía.
            ("Policía: caminar apuntando en 4 direcciones, correr con arma, disparar, recargar (clips de PJ01)", "PJ01_Pistola_Disparo/PJ01_Pistola_Recargar", YaEstaba),
            ("Policía: arrestar al jugador, hablar por radio, rapel desde la nave", "-", SinClip),
        };

        // ---------- Aplicar ----------

        [MenuItem("Mecha Cyberpunk/T-1551/Aplicar todo")]
        public static void Aplicar()
        {
            var baseCtrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);
            if (baseCtrl == null)
            {
                Debug.LogError(Etiqueta + "falta " + ConstructorControladorBase.RutaControlador);
                return;
            }
            int overrides = AplicarOverrides(baseCtrl);
            AssetDatabase.SaveAssets();
            int policia = AplicarPolicia();
            int configs = ReparaConfigs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(Etiqueta + "Aplicar listo: " + overrides + " clips cambiados en los overrides, " + policia + " estados del policía, " + configs + " configs rehechas.");
        }

        /// <summary>
        /// Pone en cada override el clip que dice su tabla (ConstructorControladorBase.ClipsPorPapel más ClipsNpcNovaT1506.OverridesExtra) para las <see cref="ClavesT1551"/>; el resto no se toca.
        /// Devuelve cuántos clips cambió.
        /// </summary>
        public static int AplicarOverrides(AnimatorController baseCtrl)
        {
            int cambios = 0;
            foreach ((string ruta, (string Base, string Papel)[] tabla) in AnimacionesT1540.TablasDeOverrides())
            {
                var o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ruta);
                if (o == null)
                {
                    Debug.LogError(Etiqueta + "falta " + ruta);
                    continue;
                }
                o.runtimeAnimatorController = baseCtrl;
                var pares = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                o.GetOverrides(pares);
                for (int i = 0; i < pares.Count; i++)
                {
                    AnimationClip clave = pares[i].Key;
                    if (clave == null || !ClavesT1551.Contains(clave.name))
                    {
                        continue;
                    }
                    AnimationClip esperado = clave;
                    foreach ((string b, string p) in tabla)
                    {
                        if (b == clave.name)
                        {
                            AnimationClip nuevo = ConstructorControladorBase.Clip(p);
                            if (nuevo != null) { esperado = nuevo; }
                        }
                    }
                    if (pares[i].Value != esperado)
                    {
                        pares[i] = new KeyValuePair<AnimationClip, AnimationClip>(clave, esperado);
                        cambios++;
                    }
                }
                o.ApplyOverrides(pares);
                EditorUtility.SetDirty(o);
            }
            return cambios;
        }

        /// <summary>Pone en Policia.controller el clip de cada estado de <see cref="ClipsPolicia"/> (solo cambia el clip: las transiciones y parámetros siguen). Devuelve cuántos estados cambió.</summary>
        public static int AplicarPolicia()
        {
            var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaPolicia);
            if (c == null)
            {
                Debug.LogError(Etiqueta + "falta " + RutaPolicia);
                return 0;
            }
            int cambios = 0;
            foreach ((string estado, string clip) in ClipsPolicia)
            {
                AnimatorState s = BuscarEstado(c, estado);
                AnimationClip nuevo = ConstructorControladorBase.Clip(clip);
                if (s == null || nuevo == null)
                {
                    Debug.LogError(Etiqueta + "Policia.controller: no se pudo poner " + clip + " en " + estado);
                    continue;
                }
                if (s.motion != nuevo)
                {
                    s.motion = nuevo;
                    cambios++;
                }
            }
            EditorUtility.SetDirty(c);
            return cambios;
        }

        /// <summary>Si a ConfigClipsNovaNpc le falta algún clip (caída de Nova, susto, plática, banca, agachado) lo rehace con ClipsNpcNovaT1506.CrearConfig y ClipsNpcPlaticarT1516.AplicarConfig. Devuelve 1 si lo rehizo.</summary>
        public static int ReparaConfigs()
        {
            var config = AssetDatabase.LoadAssetAtPath<ConfigClipsNovaNpc>(ClipsNpcNovaT1506.RutaConfig);
            if (ConfigCompleta(config))
            {
                return 0;
            }
            ClipsNpcNovaT1506.CrearConfig();
            AssetDatabase.SaveAssets();
            ClipsNpcPlaticarT1516.AplicarConfig();
            AssetDatabase.SaveAssets();
            return 1;
        }

        /// <summary>True si la config trae los ocho clips de una vez (los cuatro de T-1506 y los cuatro de T-1516).</summary>
        public static bool ConfigCompleta(ConfigClipsNovaNpc c)
        {
            return c != null && c.CaidaAire != null && c.CaidaAterrizaje != null && c.CaidaLevantarse != null && c.Asustarse != null
                && c.Platicar1 != null && c.Platicar2 != null && c.SentadoBanca != null && c.Agachado != null;
        }

        // ---------- Utilidades de lectura ----------

        /// <summary>Estado por nombre en cualquier capa y sub-máquina de un controlador (null si no hay).</summary>
        public static AnimatorState BuscarEstado(AnimatorController c, string nombre)
        {
            foreach (AnimatorControllerLayer capa in c.layers)
            {
                AnimatorState s = BuscarEstado(capa.stateMachine, nombre);
                if (s != null)
                {
                    return s;
                }
            }
            return null;
        }

        private static AnimatorState BuscarEstado(AnimatorStateMachine m, string nombre)
        {
            foreach (ChildAnimatorState h in m.states)
            {
                if (h.state.name == nombre)
                {
                    return h.state;
                }
            }
            foreach (ChildAnimatorStateMachine hija in m.stateMachines)
            {
                AnimatorState s = BuscarEstado(hija.stateMachine, nombre);
                if (s != null)
                {
                    return s;
                }
            }
            return null;
        }

        /// <summary>Clave (clip del controlador base) -> clip efectivo de un override.</summary>
        public static Dictionary<string, string> ValoresDeOverride(string ruta)
        {
            var o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ruta);
            var salida = new Dictionary<string, string>();
            if (o == null)
            {
                return salida;
            }
            var pares = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            o.GetOverrides(pares);
            foreach (KeyValuePair<AnimationClip, AnimationClip> p in pares)
            {
                if (p.Key != null)
                {
                    salida[p.Key.name] = p.Value != null ? p.Value.name : p.Key.name;
                }
            }
            return salida;
        }

        /// <summary>Clip efectivo de un estado del controlador base visto por un override (el clip del estado pasado por el mapa); "(sin clip)" si no hay, "(árbol)" si es un árbol de mezcla.</summary>
        public static string ClipEfectivoDeEstado(AnimatorController baseCtrl, string estado, Dictionary<string, string> mapa)
        {
            AnimatorState s = BuscarEstado(baseCtrl, estado);
            if (s == null)
            {
                return "(sin estado)";
            }
            if (s.motion is AnimationClip clip)
            {
                return mapa != null && mapa.TryGetValue(clip.name, out string v) ? v : clip.name;
            }
            return s.motion == null ? "(sin clip)" : "(árbol)";
        }

        /// <summary>True si hay un FBX con ese nombre bajo Assets/Arte (clips de PJ01, de NPC, de Nova, del perro, o los de Mixamo de Animaciones).</summary>
        public static bool ExisteClip(string nombre)
        {
            foreach (string guid in AssetDatabase.FindAssets(nombre + " t:Model", new[] { "Assets/Arte" }))
            {
                if (System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid)) == nombre)
                {
                    return true;
                }
            }
            return false;
        }

        // ---------- Diagnóstico ----------

        /// <summary>
        /// Revisa lo de esta tarea y confirma lo que ya estaba. Escribe en el log con el prefijo [T-1551] la matriz y cada comprobación; devuelve el número de problemas (0 = todo bien).
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T-1551/Diagnóstico")]
        public static int Diagnostico()
        {
            int problemas = 0;
            var baseCtrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);
            if (baseCtrl == null)
            {
                Debug.LogError(Etiqueta + "falta el ControladorBase");
                return 1;
            }
            var sb = new StringBuilder(Etiqueta + "acción -> clip -> estado\n");
            foreach ((string accion, string clip, string estado) in Matriz)
            {
                sb.AppendLine("  [" + estado + "] " + accion + " -> " + clip);
            }
            Debug.Log(sb.ToString());

            // 1) Daño del jugador: PJ01_Dano_Frente y Espalda en Override_Jugador (y que Visual_PJ01 y Visual_woman usen ese override).
            Dictionary<string, string> jugador = ValoresDeOverride(ConstructorControladorBase.RutaOverride("Jugador"));
            problemas += Comprobar("Override_Jugador Pistola_Dano_Frente", Valor(jugador, "Pistola_Dano_Frente"), "PJ01_Dano_Frente");
            problemas += Comprobar("Override_Jugador Pistola_Dano_Espalda", Valor(jugador, "Pistola_Dano_Espalda"), "PJ01_Dano_Espalda");
            problemas += Comprobar("Override_Jugador Pistola_Dano_Fuerte", Valor(jugador, "Pistola_Dano_Fuerte"), "PJ01_Pistola_Dano_Fuerte");
            foreach (string visual in new[] { "PJ01", "woman" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(visual));
                Animator animador = prefab != null ? prefab.GetComponentInChildren<Animator>(true) : null;
                string controlador = animador != null && animador.runtimeAnimatorController != null ? animador.runtimeAnimatorController.name : "(sin controlador)";
                problemas += Comprobar("Visual_" + visual + " usa el override del jugador", controlador, "Override_Jugador");
            }

            // 2) Cada override trae, en las claves de T-1550/T-1551 que le tocan, lo que dice su tabla.
            foreach ((string ruta, (string Base, string Papel)[] tabla) in AnimacionesT1540.TablasDeOverrides())
            {
                Dictionary<string, string> valores = ValoresDeOverride(ruta);
                string nombre = System.IO.Path.GetFileNameWithoutExtension(ruta);
                foreach (string clave in ClavesT1551.Concat(AnimacionesT1540.ClavesOverride))
                {
                    if (!valores.ContainsKey(clave))
                    {
                        continue;
                    }
                    string esperado = clave;
                    foreach ((string b, string p) in tabla)
                    {
                        if (b == clave) { esperado = p; }
                    }
                    problemas += Comprobar(nombre + " " + clave, valores[clave], esperado);
                }
            }

            // 3) Lo de esta tarea, explícito.
            foreach (string papel in PapelesConMuertesT1551)
            {
                Dictionary<string, string> v = ValoresDeOverride(ConstructorControladorBase.RutaOverride(papel));
                problemas += Comprobar("Override_" + papel + " Muerte_Adelante", Valor(v, "Muerte_Adelante"), "PJ01_Muerte_Frente");
                problemas += Comprobar("Override_" + papel + " Muerte_Atras", Valor(v, "Muerte_Atras"), "PJ01_Muerte_Espaldas");
            }
            Dictionary<string, string> hacker = ValoresDeOverride(ClipsNpcNovaT1506.RutaOverrideExtra("Hacker"));
            problemas += Comprobar("Override_Hacker Muerte_Adelante", Valor(hacker, "Muerte_Adelante"), "PJ01_Muerte_Frente");
            Dictionary<string, string> e15 = ValoresDeOverride(ClipsNpcNovaT1506.RutaOverrideExtra("E15"));
            problemas += Comprobar("Override_E15 Loco_AgachadoIdle", Valor(e15, "Loco_AgachadoIdle"), "E15_Cubrirse");
            problemas += Comprobar("Override_E15 PJ01_Cobertura_Quieto", Valor(e15, "PJ01_Cobertura_Quieto"), "E15_Cubrirse");

            // 4) Policía.
            var policia = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaPolicia);
            if (policia == null) { Debug.LogError(Etiqueta + "falta " + RutaPolicia); problemas++; }
            else
            {
                foreach ((string estado, string clip) in ClipsPolicia)
                {
                    AnimatorState s = BuscarEstado(policia, estado);
                    problemas += Comprobar("Policia.controller " + estado, s != null && s.motion != null ? s.motion.name : "(sin clip)", clip);
                }
            }

            // 5) Lo que ya estaba: estados del controlador base vistos por Override_Jugador.
            foreach ((string estado, string clip) in EstadosConfirmados)
            {
                problemas += Comprobar("Jugador, estado " + estado, ClipEfectivoDeEstado(baseCtrl, estado, jugador), clip);
            }

            // 6) Clips de una vez que juega el código (caída de Nova, susto, plática, banca, agachado; robo de carro; parkour).
            var config = AssetDatabase.LoadAssetAtPath<ConfigClipsNovaNpc>(ClipsNpcNovaT1506.RutaConfig);
            if (!ConfigCompleta(config)) { Debug.LogError(Etiqueta + "ConfigClipsNovaNpc incompleta: corre Aplicar"); problemas++; }
            var parkour = AssetDatabase.LoadAssetAtPath<ConfigAnimacionParkour>("Assets/_Proyecto/Personaje/Datos/Resources/ConfigAnimacionParkour.asset");
            if (parkour == null || parkour.CarroSacarConductor == null || parkour.ConductorSerSacado == null || parkour.SaltarBarda == null || parkour.TreparBorde == null || parkour.RodarCaida == null)
            {
                Debug.LogError(Etiqueta + "ConfigAnimacionParkour incompleta (robo de carro, parkour)");
                problemas++;
            }
            else
            {
                problemas += Comprobar("ConfigAnimacionParkour.CarroSacarConductor", parkour.CarroSacarConductor.name, "PJ01_Carro_SacarConductor");
                problemas += Comprobar("ConfigAnimacionParkour.ConductorSerSacado", parkour.ConductorSerSacado.name, "PJ01_Conductor_SerSacado");
            }

            // 7) Los clips de la matriz marcados como existentes existen; los marcados sin clip, no.
            foreach ((string accion, string clip, string estado) in Matriz)
            {
                if (clip == "-" || estado == SinClip)
                {
                    continue;
                }
                foreach (string nombre in clip.Split('/'))
                {
                    bool existe = ExisteClip(nombre);
                    bool esperado = estado != PendienteTony || !nombre.StartsWith("PJ01_Protesis_");
                    if (existe != esperado)
                    {
                        Debug.LogError(Etiqueta + "la matriz dice \"" + estado + "\" para " + nombre + " (" + accion + ") pero " + (existe ? "ya existe" : "no existe") + " en Assets/Arte");
                        problemas++;
                    }
                }
            }
            Debug.Log(Etiqueta + "Diagnostico: " + problemas + " problema(s)");
            return problemas;
        }

        /// <summary>Estado del controlador base -> clip PJ01 que ve el jugador y que ya estaba conectado (se confirma en el diagnóstico y en las pruebas).</summary>
        public static readonly (string Estado, string Clip)[] EstadosConfirmados =
        {
            ("CarroEntrar", "PJ01_Carro_Entrar"), ("CarroSalir", "PJ01_Carro_Salir"), ("CarroManejar", "PJ01_Manejar_Carro"),
            ("MotoSubir", "PJ01_Moto_Subir"), ("MotoBajar", "PJ01_Moto_Bajar"), ("MotoManejar", "PJ01_Manejar_Moto"),
            ("AbrirPuerta", "PJ01_Inter_AbrirPuerta"), ("Boton", "PJ01_Inter_Boton"), ("Recoger", "PJ01_Inter_Recoger"),
            ("Telefono", "PJ01_Inter_Telefono"), ("VerTelefono", "PJ01_Inter_VerTelefono"),
            ("PistolaRecargar", "PJ01_Pistola_Recargar"), ("PistolaRecargarCaminar", "PJ01_Pistola_RecargarCaminar"), ("LargaRecargar", "PJ01_Rifle_Recargar"),
            ("DerriboExplosion", "PJ01_Derribo_Explosion"), ("DanoEmpujon", "PJ01_Dano_Empujon"), ("DanoExplosionCercana", "PJ01_Dano_ExplosionCercana"),
            ("MuerteExplosion", "PJ01_Muerte_Explosion"), ("MuerteAtropello", "PJ01_Muerte_Atropello"), ("MuerteCaidaAltura", "PJ01_Muerte_CaidaAltura"),
            ("MuerteAdelante", "PJ01_Muerte_Frente"), ("MuerteAtras", "PJ01_Muerte_Espaldas"),
            ("LevantarseBocaArriba", "PJ01_Dano_Levantarse_BocaArriba"), ("LevantarseBocaAbajo", "PJ01_Dano_Levantarse_BocaAbajo"),
            ("DanoFrente", "PJ01_Dano_Frente"), ("DanoEspalda", "PJ01_Dano_Espalda"), ("DanoIzquierda", "PJ01_Dano_Izquierda"), ("DanoDerecha", "PJ01_Dano_Derecha"),
        };

        private static string Valor(Dictionary<string, string> mapa, string clave) => mapa.TryGetValue(clave, out string v) ? v : "(sin fila)";

        private static int Comprobar(string que, string real, string esperado)
        {
            bool ok = real == esperado;
            if (ok)
            {
                Debug.Log(Etiqueta + "ok " + que + " = " + real);
                return 0;
            }
            Debug.LogError(Etiqueta + "PROBLEMA " + que + ": es " + real + ", debería ser " + esperado);
            return 1;
        }
    }
}

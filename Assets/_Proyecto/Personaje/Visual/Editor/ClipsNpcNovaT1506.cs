using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Arte.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-1506: integra los clips de Blender del 19-nov (bitácora, PEDIDO 19-NOV NOCHE, bloques A no locomoción, B NPC y C Nova) en el jugador, los peatones, las meseras,
    /// el hacker y los enemigos de Nova. Los FBX ya están en Assets/Arte (PJ01/Clips y E17_PerroRobotNova/Clips). Se pueden repetir los dos pasos, en este orden:
    /// 1) <see cref="Importar"/>  2) <see cref="Aplicar"/>.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.ClipsNpcNovaT1506.Importar y luego ...Aplicar.
    /// Para volver a un clip viejo basta cambiar su fila en las tablas de este archivo (y repetir Aplicar).
    /// </summary>
    public static class ClipsNpcNovaT1506
    {
        public const string CarpetaPerro = "Assets/Arte/Personajes/E17_PerroRobotNova/Clips/";
        // T-1516: los clips E17_* son del rig a escala real; su modelo es E17_PerroRobotNova_esc (el _a viejo queda sin usar). Se crea con PerroEscalaRealT1516.Importar.
        public const string RutaModeloPerro = "Assets/Arte/Personajes/E17_PerroRobotNova/E17_PerroRobotNova_esc.fbx";
        public const string RutaConfig = "Assets/_Proyecto/Personaje/Datos/Resources/ConfigClipsNovaNpc.asset";
        public const string ControladorMesera = "Assets/Arte/Personajes/MeseraRobot/MeseraRobot.controller";
        public const string ControladorPerro = "Assets/Arte/Personajes/E17_PerroRobotNova/E17_PerroRobotNova_esc.controller";

        // ---------- Qué se importa ----------

        /// <summary>Clips Humanoid sobre la armadura PJ01 (van a Assets/Arte/Personajes/PJ01/Clips con los ajustes de los demás PJ01).</summary>
        public static readonly string[] ClipsHumanoides =
        {
            // A) PJ01 que no son locomoción.
            "PJ01_CaminarApuntando_Adelante", "PJ01_CaminarApuntando_Atras", "PJ01_CaminarApuntando_Derecha", "PJ01_CaminarApuntando_Izquierda",
            "PJ01_Cobertura_Quieto", "PJ01_Cobertura_Asomarse", "PJ01_Cobertura_Regresar", "PJ01_Golpe1_Directo", "PJ01_Golpe2_Gancho",
            "PJ01_Dano_Frente", "PJ01_Dano_Espalda", "PJ01_Dano_Izquierda", "PJ01_Dano_Derecha", "PJ01_Muerte_Frente", "PJ01_Muerte_Espaldas",
            "PJ01_Manejar_Carro", "PJ01_Manejar_Moto",
            // B) NPC.
            "NPC_Caminar_Hombre", "NPC_Caminar_Mujer", "NPC_Caminar_Mayor_Baston", "NPC_Caminar_Celular", "NPC_Esperar", "NPC_Platicar_1", "NPC_Platicar_2",
            "NPC_Sentado_Banca", "NPC_Asustarse", "NPC_Agachado_CubriendoCabeza", "NPC_Huir_Corriendo", "NPC_Balazo_Frente", "NPC_Balazo_Espalda",
            "NPC_Muerte_Frente", "NPC_Muerte_Espaldas", "NPC_Mesera_Charola", "NPC_Hacker_Tecleando",
            // C) Nova (E17 es Generic y va aparte).
            "E15_Caminar", "E15_Correr", "E15_Cubrirse", "E15_Disparar", "E15_Impacto", "E15_Muerte",
            "E16_Caminar", "E16_Disparar_Canon", "E16_Girar_Derecha", "E16_Girar_Izquierda", "E16_Golpe_Escudo", "E16_Impacto", "E16_Muerte",
            "E18_Caida_Aire", "E18_Caida_Aterrizaje", "E18_Caida_Levantarse", "E18_Disparar", "E18_Jetpack_Volar", "E18_Muerte",
            "Guardia_Caida_Aire", "Guardia_Caida_Aterrizaje", "Guardia_Caida_Levantarse", "Guardia_Caminar", "Guardia_Disparar", "Guardia_Muerte",
        };

        /// <summary>Clips del perro robot (rig propio Generic, raíz "Raiz").</summary>
        public static readonly string[] ClipsPerro =
        {
            "E17_Caminar", "E17_Correr", "E17_Ladrar", "E17_Morder", "E17_Muerte", "E17_SaltoAtaque",
        };

        /// <summary>Prefijos de los clips que no llevan "PJ01_" pero viven en la carpeta de clips de PJ01 (los busca ConstructorControladorBase.Clip).</summary>
        public static readonly string[] PrefijosBlender = { "NPC_", "E15_", "E16_", "E18_", "Guardia_" };

        public static bool EsClipBlender(string nombre) => PrefijosBlender.Any(nombre.StartsWith);

        // ---------- Qué clip va en qué papel (clip base del controlador -> clip de Blender) ----------

        /// <summary>
        /// Jugador (no locomoción). Golpe 1 = derecha (PJ01_Golpe1_Directo), golpe 2 = izquierda (PJ01_Golpe2_Gancho): VisualPersonaje alterna GolpeDer y GolpeIzq y el combo es de 2 golpes (T-1490).
        /// Daño frente y espalda van en la capa aditiva del torso (Pistola_Dano_*); izquierda y derecha se importan pero el controlador aún no tiene estado para ellas.
        /// Para volver al clip anterior: borrar la fila.
        /// </summary>
        public static readonly (string Base, string Papel)[] ClipsSoloJugador =
        {
            ("Punos_GolpeDer", "PJ01_Golpe1_Directo"), ("Punos_GolpeIzq", "PJ01_Golpe2_Gancho"),
            ("Muerte_Adelante", "PJ01_Muerte_Frente"), ("Muerte_Atras", "PJ01_Muerte_Espaldas"),
            ("Pistola_Dano_Frente", "PJ01_Dano_Frente"), ("Pistola_Dano_Espalda", "PJ01_Dano_Espalda"),
            ("Carro_Manejar_Bucle", "PJ01_Manejar_Carro"), ("Carro_Pitar", "PJ01_Manejar_Carro"),
            ("Pistola_ApuntarAtras", "PJ01_CaminarApuntando_Atras"),
            ("Pistola_ApuntarLateral_Derecha", "PJ01_CaminarApuntando_Derecha"), ("Pistola_ApuntarLateral_Izquierda", "PJ01_CaminarApuntando_Izquierda"),
        };

        /// <summary>Peatones y demás papel NPC (hombre por omisión): esperar, huir, balazo y muerte.</summary>
        public static readonly (string Base, string Papel)[] ClipsNpcComun =
        {
            ("Loco_Idle", "NPC_Esperar"),
            ("Loco_Caminar", "NPC_Caminar_Hombre"), ("Loco_Caminar_ConImplante", "NPC_Caminar_Hombre"),
            ("Loco_Correr", "NPC_Huir_Corriendo"), (ConstructorControladorBase.RolTrote, "NPC_Huir_Corriendo"), // T-1506: huir a 3.6 m/s cae en el extremo rápido (trote) de Caminar, no en Correr (>5.5)
             ("Loco_Correr_ConImplante", "NPC_Huir_Corriendo"),
            ("Pistola_Dano_Frente", "NPC_Balazo_Frente"), ("Pistola_Dano_Espalda", "NPC_Balazo_Espalda"), ("Pistola_Dano_Fuerte", "NPC_Balazo_Frente"),
            ("Muerte_Adelante", "NPC_Muerte_Frente"), ("Muerte_Atras", "NPC_Muerte_Espaldas"),
        };

        /// <summary>Guardias de Nova humanos (GuardiaNova y E12): caminar y disparar de Guardia, caída de Nova en el aire, muerte de frente.</summary>
        public static readonly (string Base, string Papel)[] ClipsGuardias =
        {
            ("Loco_Caminar", "Guardia_Caminar"), ("Loco_Caminar_ConImplante", "Guardia_Caminar"),
            ("Pistola_Disparo", "Guardia_Disparar"), ("Rifle_DisparoCaminar", "Guardia_Disparar"),
            ("Loco_Caida", "Guardia_Caida_Aire"), ("Loco_SaltoAterrizaje", "Guardia_Caida_Aterrizaje"),
            ("Muerte_Adelante", "Guardia_Muerte"), ("Muerte_Atras", "NPC_Muerte_Espaldas"),
        };

        /// <summary>Overrides nuevos (no están en ConstructorControladorBase.Papeles): nombre, papel del que parten y clips propios.</summary>
        public static readonly (string Nombre, string Padre, (string Base, string Papel)[] Clips)[] OverridesExtra =
        {
            ("NPC_Mujer", "NPC", new[] { ("Loco_Caminar", "NPC_Caminar_Mujer"), ("Loco_Caminar_ConImplante", "NPC_Caminar_Mujer") }),
            ("NPC_Mayor", "NPC", new[] { ("Loco_Caminar", "NPC_Caminar_Mayor_Baston"), ("Loco_Caminar_ConImplante", "NPC_Caminar_Mayor_Baston") }),
            ("NPC_Celular", "NPC", new[] { ("Loco_Caminar", "NPC_Caminar_Celular"), ("Loco_Caminar_ConImplante", "NPC_Caminar_Celular") }),
            ("E15", "Guardias", new[]
            {
                ("Loco_Caminar", "E15_Caminar"), ("Loco_Caminar_ConImplante", "E15_Caminar"), ("Loco_Correr", "E15_Correr"), ("Loco_Correr_ConImplante", "E15_Correr"), (ConstructorControladorBase.RolTrote, "E15_Correr"),
                ("Pistola_Disparo", "E15_Disparar"), ("Rifle_DisparoCaminar", "E15_Disparar"),
                ("Pistola_Dano_Frente", "E15_Impacto"), ("Pistola_Dano_Espalda", "E15_Impacto"), ("Pistola_Dano_Fuerte", "E15_Impacto"),
            }),
            ("E16", "Guardias", new[]
            {
                ("Loco_Caminar", "E16_Caminar"), ("Loco_Caminar_ConImplante", "E16_Caminar"), ("Loco_Correr", "E16_Caminar"), ("Loco_Correr_ConImplante", "E16_Caminar"), (ConstructorControladorBase.RolTrote, "E16_Caminar"),
                ("Loco_GirarDer", "E16_Girar_Derecha"), ("Loco_GirarIzq", "E16_Girar_Izquierda"),
                ("Pistola_Disparo", "E16_Disparar_Canon"), ("Rifle_DisparoCaminar", "E16_Disparar_Canon"),
                ("Punos_GolpeDer", "E16_Golpe_Escudo"), ("Punos_GolpeIzq", "E16_Golpe_Escudo"), ("Punos_Fuerte", "E16_Golpe_Escudo"),
                ("Pistola_Dano_Frente", "E16_Impacto"), ("Pistola_Dano_Espalda", "E16_Impacto"), ("Pistola_Dano_Fuerte", "E16_Impacto"),
            }),
            ("E18", "Guardias", new[]
            {
                ("Pistola_Disparo", "E18_Disparar"), ("Rifle_DisparoCaminar", "E18_Disparar"),
                ("Loco_Caida", "E18_Jetpack_Volar"), ("Loco_SaltoAire", "E18_Jetpack_Volar"),
                ("Loco_SaltoAterrizaje", "E18_Caida_Aterrizaje"),
            }),
            ("Hacker", "SNAKES", new[] { ("Loco_Idle", "NPC_Hacker_Tecleando") }),
        };

        /// <summary>Visual (sin "Visual_") -> override y velocidades de los clips de caminar y correr (m/s de la bitácora). Override null = conserva el que tiene.</summary>
        public static readonly (string Visual, string Override, float Caminar, float Correr)[] Visuales =
        {
            // Peatones: género deducido del nombre (Pescador, Elotero, Estibador, Corredor: hombre; Abuela: mujer mayor con bastón; Oficinista a: mujer, b: hombre;
            // Turista y Repartidor: caminan con el celular en la mano).
            ("NP07_Pescador_a", "NPC", 1.40f, 5.0f), ("NP07_Pescador_b", "NPC", 1.40f, 5.0f),
            ("NP08_Turista_a", "NPC_Celular", 1.20f, 5.0f), ("NP08_Turista_b", "NPC_Celular", 1.20f, 5.0f),
            ("NP09_Corredor_a", "NPC", 1.40f, 5.0f), ("NP09_Corredor_b", "NPC", 1.40f, 5.0f),
            ("NP10_Elotero_a", "NPC", 1.40f, 5.0f),
            ("NP11_Estibador_a", "NPC", 1.40f, 5.0f), ("NP11_Estibador_b", "NPC", 1.40f, 5.0f),
            ("NP12_Abuela_a", "NPC_Mayor", 0.60f, 5.0f),
            ("NP13_Repartidor_a", "NPC_Celular", 1.20f, 5.0f), ("NP13_Repartidor_b", "NPC_Celular", 1.20f, 5.0f),
            ("NP14_Oficinista_a", "NPC_Mujer", 1.30f, 5.0f), ("NP14_Oficinista_b", "NPC", 1.40f, 5.0f),
            ("Doctor", "NPC", 1.40f, 5.0f),
            ("GuardiaNova", "Guardias", 1.10f, ReglasAnimacion.VelocidadClipCorrerSinImplante), ("E12_GuardiaPesadoNova", "Guardias", 1.10f, ReglasAnimacion.VelocidadClipCorrerSinImplante),
            ("E15_CentinelaNova_a", "E15", 1.10f, 3.60f), ("E15_CentinelaNova_b", "E15", 1.10f, 3.60f),
            ("E16_RobotPesadoNova_a", "E16", 0.80f, 0.80f),
            ("E18_EliteNova_a", "E18", ReglasAnimacion.VelocidadClipCaminarSinImplante, ReglasAnimacion.VelocidadClipCorrerSinImplante),
            ("Hacker", "Hacker", 0f, 0f),
            ("MeseraRobot", null, 1.20f, 1.20f),
            ("E17_PerroRobotNova_a", null, 0.90f, 5.00f),
        };

        /// <summary>Estado del controlador propio -> clip, para la mesera y el perro (no son override del controlador base).</summary>
        public static readonly (string Controlador, string Estado, string Clip)[] ClipsDeEstado =
        {
            (ControladorMesera, "Caminar", "NPC_Mesera_Charola"), (ControladorMesera, "Correr", "NPC_Mesera_Charola"),
        };

        // ---------- Gancho de ConstructorControladorBase.ClipsPorPapel ----------

        /// <summary>Agrega a la tabla de papeles las filas de T-1506 (jugador, NPC y guardias). Lo llama ConstruirClipsPorPapel.</summary>
        public static void AgregarATabla(Dictionary<string, (string Base, string Papel)[]> mapa)
        {
            mapa["Jugador"] = Combinar(mapa["Jugador"], ClipsSoloJugador);
            mapa["NPC"] = Combinar(mapa["NPC"], ClipsNpcComun);
            mapa["Guardias"] = Combinar(mapa["Guardias"], ClipsGuardias);
        }

        public static (string Base, string Papel)[] Combinar((string Base, string Papel)[] tabla, (string Base, string Papel)[] cambios)
        {
            var salida = new List<(string Base, string Papel)>(tabla);
            foreach ((string b, string p) in cambios)
            {
                int i = salida.FindIndex(par => par.Base == b);
                if (i >= 0) { salida[i] = (b, p); } else { salida.Add((b, p)); }
            }
            return salida.ToArray();
        }

        public static string RutaOverrideExtra(string nombre) => ConstructorControladorBase.CarpetaOverrides + "/Override_" + nombre + ".overrideController";

        // ---------- Paso 1: importar ----------

        [MenuItem("Mecha Cyberpunk/T-1506/1 Importar clips de NPC y Nova")]
        public static void Importar()
        {
            AssetDatabase.Refresh();
            foreach (string nombre in ClipsHumanoides)
            {
                ImportarHumanoide(nombre);
            }
            ImportarClipsPerro();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Personaje] T-1506: clips importados (" + ClipsHumanoides.Length + " Humanoid y " + ClipsPerro.Length + " del perro).");
        }

        /// <summary>T-1516: importa los seis clips E17_* con la escala del modelo <see cref="RutaModeloPerro"/>.</summary>
        public static void ImportarClipsPerro()
        {
            foreach (string nombre in ClipsPerro)
            {
                ImportarPerro(nombre);
            }
        }

        private static void ImportarHumanoide(string nombre)
        {
            string ruta = ImportadorClipsPJ01.Carpeta + nombre + ".fbx";
            if (AssetDatabase.LoadAssetAtPath<Object>(ruta) == null)
            {
                Debug.LogError("[Personaje] T-1506: falta " + ruta);
                return;
            }
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
            ModelImporter importador = AssetImporter.GetAtPath(ruta) as ModelImporter;
            if (importador == null)
            {
                return;
            }
            // Igual que ImportarClipsRonda2T1310: Humanoid con la descripción de PJ01, escala de PJ01, bucle por nombre (ClipsCiclicosT1506).
            ImportadorClipsPJ01.ConfigurarModelo(importador);
            importador.SaveAndReimport();
            ImportadorClipsPJ01.CopiarDescripcion(importador);
            ImportadorClipsPJ01.ConfigurarClip(importador, ruta);
            importador.SaveAndReimport();
        }

        private static void ImportarPerro(string nombre)
        {
            string ruta = CarpetaPerro + nombre + ".fbx";
            if (AssetDatabase.LoadAssetAtPath<Object>(ruta) == null)
            {
                Debug.LogError("[Personaje] T-1506: falta " + ruta);
                return;
            }
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
            ModelImporter importador = AssetImporter.GetAtPath(ruta) as ModelImporter;
            var modelo = AssetImporter.GetAtPath(RutaModeloPerro) as ModelImporter;
            if (importador == null || modelo == null)
            {
                Debug.LogError("[Personaje] T-1506: sin importador del clip o del modelo del perro para " + nombre);
                return;
            }
            // El modelo del perro es Generic sin avatar (T-1389b): los clips igual y con su misma escala; las curvas se enlazan por la ruta de los huesos.
            importador.importAnimation = true;
            importador.animationType = ModelImporterAnimationType.Generic;
            importador.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
            importador.useFileScale = modelo.useFileScale;
            importador.globalScale = modelo.globalScale;
            // T-1516: el modelo a escala real tiene la malla y el rig como hermanos (el nodo del rig queda en la ruta); el clip solo trae el rig y Unity lo colapsaría.
            importador.preserveHierarchy = true;
            importador.userData = modelo.userData; // el postprocesador de escala lee userData: sin esto el clip vuelve a escala 1
            importador.materialImportMode = ModelImporterMaterialImportMode.None;
            importador.SaveAndReimport();
            importador = AssetImporter.GetAtPath(ruta) as ModelImporter;
            ModelImporterClipAnimation[] clips = importador.defaultClipAnimations;
            if (clips.Length == 0)
            {
                Debug.LogError("[Personaje] T-1506: " + nombre + " no trae animación");
                return;
            }
            ModelImporterClipAnimation clip = clips[0];
            clip.name = nombre;
            clip.loopTime = ClipsCiclicosT1506.EsCiclico(nombre);
            clip.loopPose = clip.loopTime;
            importador.clipAnimations = new[] { clip };
            importador.SaveAndReimport();
        }

        // ---------- Paso 2: aplicar ----------

        [MenuItem("Mecha Cyberpunk/T-1506/2 Aplicar clips a overrides, controladores y prefabs")]
        public static void Aplicar()
        {
            // Reconstruye el controlador base y los 9 overrides (los gana la tabla ClipsPorPapel, que ya trae las filas de T-1506) y los overrides extra.
            ConstructorControladorBase.Construir();
            AplicarOverridesExtra();
            AplicarClipsDeEstado();
            CrearConfig();
            AplicarAPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Personaje] T-1506: overrides, controladores propios, config y prefabs listos.");
        }

        /// <summary>Crea o actualiza Override_NPC_Mujer, NPC_Mayor, NPC_Celular, E15, E16, E18 y Hacker. Lo llama también ConstructorControladorBase.Construir.</summary>
        public static void AplicarOverridesExtra()
        {
            var baseCtrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);
            if (baseCtrl == null)
            {
                return;
            }
            foreach ((string nombre, string padre, (string Base, string Papel)[] clips) in OverridesExtra)
            {
                string ruta = RutaOverrideExtra(nombre);
                var o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ruta);
                if (o == null)
                {
                    o = new AnimatorOverrideController(baseCtrl);
                    AssetDatabase.CreateAsset(o, ruta);
                }
                o.runtimeAnimatorController = baseCtrl;
                (string Base, string Papel)[] tabla = Combinar(ConstructorControladorBase.ClipsPorPapel[padre], clips);
                var pares = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                o.GetOverrides(pares);
                for (int i = 0; i < pares.Count; i++)
                {
                    // Parte de cero (cada clip = el suyo) y se aplica la tabla del padre más los clips propios.
                    AnimationClip clip = pares[i].Key;
                    foreach ((string b, string p) in tabla)
                    {
                        if (pares[i].Key.name == b)
                        {
                            AnimationClip nuevo = ConstructorControladorBase.Clip(p);
                            if (nuevo != null) { clip = nuevo; }
                        }
                    }
                    pares[i] = new KeyValuePair<AnimationClip, AnimationClip>(pares[i].Key, clip);
                }
                o.ApplyOverrides(pares);
                EditorUtility.SetDirty(o);
            }
        }

        /// <summary>Cambia el clip de un estado de los controladores propios (mesera y perro).</summary>
        public static void AplicarClipsDeEstado()
        {
            foreach ((string rutaControlador, string estado, string clip) in ClipsDeEstado)
            {
                var controlador = AssetDatabase.LoadAssetAtPath<AnimatorController>(rutaControlador);
                if (controlador == null)
                {
                    Debug.LogError("[Personaje] T-1506: falta " + rutaControlador);
                    continue;
                }
                AnimationClip nuevo = CargarClip(clip);
                foreach (AnimatorControllerLayer capa in controlador.layers)
                {
                    foreach (ChildAnimatorState hijo in capa.stateMachine.states)
                    {
                        if (hijo.state.name == estado && nuevo != null)
                        {
                            hijo.state.motion = nuevo;
                        }
                    }
                }
                EditorUtility.SetDirty(controlador);
            }
        }

        /// <summary>Un clip de Blender por nombre: los E17_ salen de la carpeta del perro, los demás de la de PJ01.</summary>
        public static AnimationClip CargarClip(string nombre)
        {
            if (!nombre.StartsWith("E17_"))
            {
                return ConstructorControladorBase.Clip(nombre);
            }
            string ruta = CarpetaPerro + nombre + ".fbx";
            return AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<AnimationClip>().FirstOrDefault(c => c.name == nombre);
        }

        public static void CrearConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<ConfigClipsNovaNpc>(RutaConfig);
            if (config == null)
            {
                string carpeta = System.IO.Path.GetDirectoryName(RutaConfig).Replace('\\', '/');
                if (!AssetDatabase.IsValidFolder(carpeta))
                {
                    System.IO.Directory.CreateDirectory(carpeta);
                    AssetDatabase.Refresh();
                }
                config = ScriptableObject.CreateInstance<ConfigClipsNovaNpc>();
                AssetDatabase.CreateAsset(config, RutaConfig);
            }
            config.Configurar(CargarClip("E18_Caida_Aire"), CargarClip("E18_Caida_Aterrizaje"), CargarClip("E18_Caida_Levantarse"), CargarClip("NPC_Asustarse"));
            EditorUtility.SetDirty(config);
        }

        /// <summary>Pone el override y las velocidades de los clips en cada Visual_ de la tabla; el hacker deja la pose por huesos y usa el clip.</summary>
        public static void AplicarAPrefabs()
        {
            foreach ((string visual, string nombreOverride, float caminar, float correr) in Visuales)
            {
                string ruta = CreadorVisualPersonaje.RutaPrefab(visual);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) == null)
                {
                    Debug.LogWarning("[Personaje] T-1506: no existe " + ruta);
                    continue;
                }
                GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
                try
                {
                    Animator animador = contenido.GetComponentInChildren<Animator>(true);
                    if (nombreOverride != null && animador != null)
                    {
                        string rutaOverride = nombreOverride == "NPC" || ConstructorControladorBase.Papeles.Contains(nombreOverride)
                            ? ConstructorControladorBase.RutaOverride(nombreOverride)
                            : RutaOverrideExtra(nombreOverride);
                        animador.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(rutaOverride);
                    }
                    VisualPersonaje vp = contenido.GetComponent<VisualPersonaje>();
                    if (vp != null && caminar > 0f)
                    {
                        vp.ConfigurarClips(caminar, correr, ReglasAnimacion.RapidezMaximaLocomocion);
                    }
                    PoseSentado pose = contenido.GetComponentInChildren<PoseSentado>(true);
                    if (pose != null)
                    {
                        pose.UsarClip = true;
                    }
                    PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contenido);
                }
            }
        }
    }
}

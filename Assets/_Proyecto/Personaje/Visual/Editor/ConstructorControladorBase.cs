using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-561: arma el controlador base humanoide para todos los personajes (Assets/Arte/Personajes/Animaciones/ControladorBase.controller)
    /// con una capa base (locomoción, salto, cobertura, muerte) y una capa de arriba con máscara de torso (puños, pistola, arma larga, daño),
    /// y un AnimatorOverrideController por papel. Se puede repetir: rearma el mismo asset.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.ConstructorControladorBase.Construir
    /// </summary>
    public static class ConstructorControladorBase
    {
        public const string Carpeta = "Assets/Arte/Personajes/Animaciones";
        public const string RutaControlador = Carpeta + "/ControladorBase.controller";
        public const string RutaMascara = Carpeta + "/MascaraTorso.mask";
        public const string CarpetaOverrides = Carpeta + "/Overrides";
        /// <summary>T-749: los clips de Blender ya aplicados al esqueleto de PJ01 (PJ01_*.fbx); el jugador los usa en lugar de los de Mixamo.</summary>
        public const string CarpetaClipsPJ01 = "Assets/Arte/Personajes/PJ01/Clips";
        public const string PrefijoClipPJ01 = "PJ01_";

        /// <summary>Papeles con override. El nombre del archivo es Override_&lt;Papel&gt;.overrideController.</summary>
        public static readonly string[] Papeles = { "Jugador", "Aliados", "NPC", "SKULLS", "SNAKES", "Guardias", "Robots", "Profes", "Jefe" };

        private const float Mezcla = 0.12f;
        private const float MezclaCorta = 0.06f;
        private const float SalidaUnaVez = 0.88f;
        // T-735: los Pistola_Dano_* duran 0.47 s y terminan en la pose de apuntar: se dejan completos (la capa es aditiva).
        private const float SalidaDano = 0.95f;
        private const float UmbralCaminar = ReglasVisual.UmbralCaminar;

        public static string RutaOverride(string papel) => CarpetaOverrides + "/Override_" + papel + ".overrideController";

        public static AnimationClip Clip(string nombre)
        {
            // T-976: si hay copia limpia del clip (tramo malo de Blender rehecho), se usa esa.
            AnimationClip limpio = LimpiarClipsMotoT976.Limpio(nombre);
            if (limpio != null)
            {
                return limpio;
            }
            string carpeta = nombre.StartsWith(PrefijoClipPJ01) || ClipsNpcNovaT1506.EsClipBlender(nombre) ? CarpetaClipsPJ01 : Carpeta;
            foreach (string guid in AssetDatabase.FindAssets(nombre + " t:Model", new[] { carpeta }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(ruta) != nombre)
                {
                    continue;
                }
                foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ruta))
                {
                    if (o is AnimationClip c && c.name == nombre && !c.name.StartsWith("__preview"))
                    {
                        return c;
                    }
                }
            }
            Debug.LogError("[Personaje] Falta el clip " + nombre + " en " + carpeta);
            return null;
        }

        [MenuItem("Mecha Cyberpunk/Personajes/T-561 Construir controlador base")]
        public static void Construir()
        {
            AvatarMask mascara = CrearMascara();
            AnimatorController c = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);
            if (c == null)
            {
                c = AnimatorController.CreateAnimatorControllerAtPath(RutaControlador);
            }
            Vaciar(c);
            Parametros(c);
            ConstruirBase(c);
            ConstruirArriba(c, mascara);
            ConstruirDano(c, mascara);
            EditorUtility.SetDirty(c);
            AssetDatabase.SaveAssets();
            CrearOverrides(c);
            AplicarClipsPorPapel();
            ClipsNpcNovaT1506.AplicarOverridesExtra(); // T-1506: overrides de NPC mujer/mayor/celular, E15, E16, E18 y hacker
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Personaje] T-561: controlador base y " + Papeles.Length + " overrides listos.");
        }

        // ---------- Máscara y overrides ----------

        private static AvatarMask CrearMascara()
        {
            AvatarMask m = AssetDatabase.LoadAssetAtPath<AvatarMask>(RutaMascara);
            if (m == null)
            {
                m = new AvatarMask();
                AssetDatabase.CreateAsset(m, RutaMascara);
            }
            foreach (AvatarMaskBodyPart parte in System.Enum.GetValues(typeof(AvatarMaskBodyPart)))
            {
                if (parte == AvatarMaskBodyPart.LastBodyPart)
                {
                    continue;
                }
                // T-749: Root va en la máscara: sin él la cadera viene de la capa base (otra postura) y el torso y la pistola salen girados hasta 46° respecto a Blender.
                bool arriba = parte == AvatarMaskBodyPart.Root || parte == AvatarMaskBodyPart.Body || parte == AvatarMaskBodyPart.Head
                    || parte == AvatarMaskBodyPart.LeftArm || parte == AvatarMaskBodyPart.RightArm
                    || parte == AvatarMaskBodyPart.LeftFingers || parte == AvatarMaskBodyPart.RightFingers
                    || parte == AvatarMaskBodyPart.LeftHandIK || parte == AvatarMaskBodyPart.RightHandIK;
                m.SetHumanoidBodyPartActive(parte, arriba);
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void CrearOverrides(AnimatorController baseCtrl)
        {
            if (!AssetDatabase.IsValidFolder(CarpetaOverrides))
            {
                AssetDatabase.CreateFolder(Carpeta, "Overrides");
            }
            foreach (string papel in Papeles)
            {
                string ruta = RutaOverride(papel);
                AnimatorOverrideController o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ruta);
                if (o == null)
                {
                    o = new AnimatorOverrideController(baseCtrl);
                    AssetDatabase.CreateAsset(o, ruta);
                }
                o.runtimeAnimatorController = baseCtrl;
                // Cada papel parte de los mismos clips que la base; el cambio de un clip para un papel se hace aquí sin tocar la base.
                var pares = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                o.GetOverrides(pares);
                for (int i = 0; i < pares.Count; i++)
                {
                    pares[i] = new KeyValuePair<AnimationClip, AnimationClip>(pares[i].Key, pares[i].Key);
                }
                o.ApplyOverrides(pares);
                EditorUtility.SetDirty(o);
            }
        }

        /// <summary>
        /// T-749: clip del controlador base -> clip PJ01_* de Assets/Arte/Personajes/PJ01/Clips. T-985: PJ01_Pistola_ApuntarCaminar_Derecha e Izquierda son los estados GirarApuntarDer/Izq (giros en el sitio
        /// con el arma apuntando, van directo en la base). T-960 a T-964: también los clips de Blender del 9-nov (locomoción, salto, puños, muertes,
        /// carro, arma larga, cobertura e interacciones). T-985: Burla, Presumir y Carro_Pitar del jugador también van con PJ01.
        /// </summary>
        public static readonly (string Base, string Papel)[] ClipsDeJugadorPJ01 =
        {
            ("Loco_Caminar", "PJ01_Caminar_SinImplante"), ("Loco_Correr", "PJ01_Correr_SinImplante"),
            ("Loco_Caminar_ConImplante", "PJ01_Caminar_ConImplante"), ("Loco_Correr_ConImplante", "PJ01_Correr_ConImplante"),
            ("Loco_AgachadoIdle", "PJ01_Loco_AgachadoIdle"), ("Loco_AgachadoCaminar", "PJ01_Loco_AgachadoCaminar"), ("Loco_AgachadoAtras", "PJ01_Loco_AgachadoAtras"),
            ("Loco_AgachadoLateral_Derecha", "PJ01_Loco_AgachadoLateral_Derecha"), ("Loco_AgachadoLateral_Izquierda", "PJ01_Loco_AgachadoLateral_Izquierda"),
            ("Loco_Agacharse", "PJ01_Loco_Agacharse"), ("Loco_Pararse", "PJ01_Loco_Pararse"), ("Loco_Rodar", "PJ01_Loco_Rodar"),
            ("Pistola_Idle", "PJ01_Pistola_Idle"), ("Pistola_Caminar", "PJ01_Pistola_Caminar"), ("Pistola_Correr", "PJ01_Pistola_Correr"),
            ("Pistola_Sacar", "PJ01_Pistola_Sacar"), ("Pistola_Guardar", "PJ01_Pistola_Guardar"), ("Pistola_Apuntar", "PJ01_Pistola_Apuntar"),
            ("Pistola_ApuntarAtras", "PJ01_Pistola_ApuntarAtras"), ("Pistola_ApuntarLateral_Derecha", "PJ01_Pistola_ApuntarLateral_Derecha"),
            ("Pistola_ApuntarLateral_Izquierda", "PJ01_Pistola_ApuntarLateral_Izquierda"), ("Pistola_Disparo", "PJ01_Pistola_Disparo"),
            ("Pistola_Recargar", "PJ01_Pistola_Recargar"), ("Pistola_RecargarCaminar", "PJ01_Pistola_RecargarCaminar"),
            ("Pistola_AgachadoIdle", "PJ01_Pistola_AgachadoIdle"), ("Pistola_AgachadoCaminar", "PJ01_Pistola_AgachadoCaminar"),
            ("Pistola_Dano_Frente", "PJ01_Pistola_Dano_Frente"), ("Pistola_Dano_Espalda", "PJ01_Pistola_Dano_Espalda"), ("Pistola_Dano_Fuerte", "PJ01_Pistola_Dano_Fuerte"),
            // T-960 (A4): locomoción base de Blender; las versiones _ConImplante ya están en los árboles de la base (ClipConImplante).
            ("Loco_Idle", "PJ01_Loco_Idle"), ("Loco_CaminarAtras", "PJ01_Loco_CaminarAtras"), ("Loco_CaminarDer", "PJ01_Loco_CaminarDer"),
            ("Loco_CaminarIzq", "PJ01_Loco_CaminarIzq"), ("Loco_GirarDer", "PJ01_Loco_GirarDer"), ("Loco_GirarIzq", "PJ01_Loco_GirarIzq"),
            ("Loco_Frenar", "PJ01_Loco_Frenar"),
            // T-961 (A2): salto en tres fases (SaltoAire sin bucle: se queda en su última pose hasta aterrizar) y la caída larga en bucle.
            ("Loco_SaltoInicio", "PJ01_Loco_SaltoInicio"), ("Loco_SaltoAire", "PJ01_Loco_SaltoAire"),
            ("Loco_SaltoAterrizaje", "PJ01_Loco_SaltoAterrizaje"), ("Loco_Caida", "PJ01_Loco_Caida"),
            // T-962 (A3): puños, muertes rápidas y levantarse. Las muertes y levantarse mueven la cápsula con su root motion (RaizCaidaJugador). PJ01_Punos_Guardia es el estado PunosGuardia (T-985).
            ("Punos_GolpeIzq", "PJ01_Punos_GolpeIzq"), ("Punos_GolpeDer", "PJ01_Punos_GolpeDer"), ("Punos_Fuerte", "PJ01_Punos_Fuerte"),
            ("Muerte_Adelante", "PJ01_Muerte_Adelante"), ("Muerte_Atras", "PJ01_Muerte_Atras"), ("Muerte_Desmayo", "PJ01_Muerte_Desmayo"),
            ("Dano_Levantarse_BocaArriba", "PJ01_Dano_Levantarse_BocaArriba"), ("Dano_Levantarse_BocaAbajo", "PJ01_Dano_Levantarse_BocaAbajo"),
            // T-963 (A1): subir, manejar y bajar del carro (postura nueva: más abajo, piernas al piso, manos en el volante). La moto tiene sus propios estados.
            ("Carro_Entrar", "PJ01_Carro_Entrar"), ("Carro_Manejar_Bucle", "PJ01_Carro_Manejar"), ("Carro_Salir", "PJ01_Carro_Salir"),
            // T-964 (A5): arma larga (escopeta, fusil y francotirador), cobertura (son con pistola) e interacciones. Inter_Telefono va recortado al tramo útil.
            ("Rifle_Idle", "PJ01_Rifle_Idle"), ("Rifle_Caminar", "PJ01_Rifle_Caminar"), ("Rifle_Correr", "PJ01_Rifle_Correr"),
            ("Rifle_Apuntar", "PJ01_Rifle_Apuntar"), ("Rifle_ApuntarCaminar", "PJ01_Rifle_ApuntarCaminar"), ("Rifle_ApuntarCorrer", "PJ01_Rifle_ApuntarCorrer"),
            ("Rifle_DisparoCaminar", "PJ01_Rifle_DisparoCaminar"), ("Rifle_Recargar", "PJ01_Rifle_Recargar"),
            ("Cobertura_Entrar", "PJ01_Cobertura_Entrar"), ("Cobertura_Disparo", "PJ01_Cobertura_Disparo"), ("Cobertura_Disparo2", "PJ01_Cobertura_Disparo2"),
            ("Inter_AbrirPuerta", "PJ01_Inter_AbrirPuerta"), ("Inter_Boton", "PJ01_Inter_Boton"), ("Inter_Recoger", "PJ01_Inter_Recoger"),
            ("Inter_Telefono", "PJ01_Inter_Telefono"), ("Inter_VerTelefono_Bucle", "PJ01_Inter_VerTelefono"),
            // T-985: gestos que seguían de Mixamo en el jugador. Burla y Presumir (los usan otros papeles; el jugador no los lanza) quedan en la postura de guardia de puños; el claxon usa la pose de manejar.
            ("Skull_Burla", "PJ01_Punos_Guardia"), ("Nova_Flexing", "PJ01_Punos_Guardia"), ("Carro_Pitar", "PJ01_Carro_Manejar"),
        };

        /// <summary>
        /// T-1004: todos los papeles humanos (aliados, NPC, SKULLS, SNAKES, guardias de Nova, robots, profes y el jefe) usan los mismos clips de PJ01 que el jugador
        /// (ClipsDeJugadorPJ01): nadie camina con clips de Mixamo. Los clips propios que tenían SKULLS, el jefe y los guardias (T-624) ya no se usan.
        /// Los papeles que no mueven el parámetro ConImplante se quedan en la rama sin implante de los árboles.
        /// </summary>
        public static readonly Dictionary<string, (string Base, string Papel)[]> ClipsPorPapel = ConstruirClipsPorPapel();

        private static Dictionary<string, (string Base, string Papel)[]> ConstruirClipsPorPapel()
        {
            var mapa = new Dictionary<string, (string Base, string Papel)[]>();
            foreach (string papel in Papeles)
            {
                mapa[papel] = ClipsDeJugadorPJ01;
            }
            mapa["Jugador"] = ConCambios(ClipsDeJugadorPJ01, ClipsSoloJugador);
            ClipsNpcNovaT1506.AgregarATabla(mapa); // T-1506: golpes, muerte, daño y manejar del jugador; NPC y guardias de Nova
            return mapa;
        }

        // T-1505: papeles nuevos del estado Caminar: el extremo rápido (trote) de la mezcla 1D por Velocidad. Su clip por defecto es el que los demás papeles usan
        // hoy en ese lugar (así los NPC no cambian) y se nombran por ese clip; el override del jugador los manda a los clips de trote de Blender.
        /// <summary>Trote adelante (sin y con implante), y de lado y atrás: clip por defecto de los demás papeles.</summary>
        public const string RolTrote = "PJ01_Caminar_SinImplante", RolTroteConImplante = "PJ01_Caminar_ConImplante";
        public const string RolTroteAtras = "PJ01_Loco_CaminarAtras", RolTroteDer = "PJ01_Loco_CaminarDer", RolTroteIzq = "PJ01_Loco_CaminarIzq";
        /// <summary>Velocidad (m/s) de los extremos de la mezcla 1D del estado Caminar (caminar de verdad y trote): ReglasAnimacion.VelocidadPJ01Caminar y VelocidadPJ01Trote.</summary>
        private const float UmbralCaminarLento = ReglasAnimacion.VelocidadPJ01Caminar, UmbralTrote = ReglasAnimacion.VelocidadPJ01Trote;

        /// <summary>
        /// T-1505: clips solo del jugador (los NPC no cambian). Caminar de verdad 1.6 m/s (PJ01_Caminar) mezcla por Velocidad con el trote 4.5 (PJ01_Trote, y de lado y
        /// atrás PJ01_Trote_*); Correr es PJ01_Sprint 7.25; el salto y el aterrizaje son los de Blender. Con implante el trote sigue siendo PJ01_Correr_ConImplante
        /// (no hay trote con implante). Reemplaza el provisional de T-1484 (el clip de correr en Caminar).
        /// </summary>
        // Propiedad y no campo: ClipsPorPapel se inicializa antes en el orden de los campos estáticos.
        public static (string Base, string Papel)[] ClipsSoloJugador => new[]
        {
            ("Loco_Caminar", "PJ01_Caminar"), (RolTrote, "PJ01_Trote"), ("Loco_Correr", "PJ01_Sprint"),
            (RolTroteConImplante, "PJ01_Correr_ConImplante"),
            (RolTroteAtras, "PJ01_Trote_Atras"), (RolTroteDer, "PJ01_Trote_Derecha"), (RolTroteIzq, "PJ01_Trote_Izquierda"),
            ("Loco_SaltoInicio", "PJ01_Salto_Despegue"), ("Loco_SaltoAire", "PJ01_Salto_Aire"), ("Loco_SaltoAterrizaje", "PJ01_Salto_Aterrizaje"),
        };

        private static (string Base, string Papel)[] ConCambios((string Base, string Papel)[] tabla, (string Base, string Papel)[] cambios)
        {
            var salida = new List<(string Base, string Papel)>(tabla);
            foreach ((string b, string p) in cambios)
            {
                int i = salida.FindIndex(par => par.Base == b);
                if (i >= 0) salida[i] = (b, p); else salida.Add((b, p));
            }
            return salida.ToArray();
        }

        /// <summary>T-1484: vuelve a poner los clips del override del jugador sin reconstruir el controlador base.</summary>
        public static void AplicarClipsDelJugador()
        {
            AplicarClipsPorPapel("Jugador");
            AssetDatabase.SaveAssets();
        }

        private static void AplicarClipsPorPapel(string solo = null)
        {
            foreach (KeyValuePair<string, (string Base, string Papel)[]> papel in ClipsPorPapel)
            {
                if (solo != null && papel.Key != solo)
                {
                    continue;
                }
                AnimatorOverrideController o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(RutaOverride(papel.Key));
                var pares = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                o.GetOverrides(pares);
                foreach ((string clipBase, string clipPapel) in papel.Value)
                {
                    for (int i = 0; i < pares.Count; i++)
                    {
                        if (pares[i].Key != null && pares[i].Key.name == clipBase)
                        {
                            pares[i] = new KeyValuePair<AnimationClip, AnimationClip>(pares[i].Key, Clip(clipPapel));
                        }
                    }
                }
                o.ApplyOverrides(pares);
                EditorUtility.SetDirty(o);
            }
        }

        // ---------- Parámetros ----------

        private static void Vaciar(AnimatorController c)
        {
            while (c.parameters.Length > 0)
            {
                c.RemoveParameter(0);
            }
            while (c.layers.Length > 1)
            {
                c.RemoveLayer(c.layers.Length - 1);
            }
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(RutaControlador))
            {
                if (o is BlendTree)
                {
                    AssetDatabase.RemoveObjectFromAsset(o);
                    Object.DestroyImmediate(o, true);
                }
            }
            AnimatorStateMachine m = c.layers[0].stateMachine;
            foreach (ChildAnimatorState s in m.states) { m.RemoveState(s.state); }
            foreach (AnimatorStateTransition t in m.anyStateTransitions) { m.RemoveAnyStateTransition(t); }
            foreach (ChildAnimatorStateMachine sm in m.stateMachines) { m.RemoveStateMachine(sm.stateMachine); }
            AnimatorControllerLayer[] capas = c.layers;
            capas[0].name = ReglasAnimacion.CapaBase;
            c.layers = capas;
        }

        private static void Parametros(AnimatorController c)
        {
            foreach (string p in ReglasAnimacion.ParametrosFloat) { c.AddParameter(p, AnimatorControllerParameterType.Float); }
            foreach (string p in ReglasAnimacion.ParametrosBool) { c.AddParameter(p, AnimatorControllerParameterType.Bool); }
            foreach (string p in ReglasAnimacion.ParametrosInt) { c.AddParameter(p, AnimatorControllerParameterType.Int); }
            foreach (string p in ReglasAnimacion.ParametrosTrigger) { c.AddParameter(p, AnimatorControllerParameterType.Trigger); }
            AnimatorControllerParameter[] lista = c.parameters;
            foreach (AnimatorControllerParameter p in lista)
            {
                // Quien no calcula la dirección (la IA vieja) camina de frente, y por defecto está en el piso.
                if (p.name == ReglasVisual.EnSuelo) { p.defaultBool = true; }
                if (p.name == ReglasAnimacion.VelZ) { p.defaultFloat = 1f; }
                if (p.name == ReglasAnimacion.PoseCobertura) { p.defaultFloat = 0.999f; }
                if (p.name == ReglasAnimacion.RapidezGolpe) { p.defaultFloat = 1f; }
            }
            c.parameters = lista;
        }

        // ---------- Capa base ----------

        private static void ConstruirBase(AnimatorController c)
        {
            AnimatorStateMachine m = c.layers[0].stateMachine;
            var e = new Dictionary<string, AnimatorState>();
            AnimatorState Estado(string nombre, string clip, float velocidad = 1f)
            {
                AnimatorState s = m.AddState(nombre);
                if (clip != null) { s.motion = Clip(clip); }
                s.speed = velocidad * ReglasAnimacion.VelocidadEstado(nombre);
                e[nombre] = s;
                return s;
            }

            // T-960: quieto, giros y frenar también tienen versión con implante (PJ01_*_ConImplante, sin choques con IMP01 o IMP02): árbol 1D por ConImplante.
            // Los papeles que no lo mueven se quedan en 0 y ven solo el clip sin implante.
            Estado("Idle", null).motion = ArbolConImplante(c, "Idle", "Loco_Idle");
            AnimatorState caminar = Estado("Caminar", null);
            // T-739: dos árboles 2D (sin y con implante) mezclados por el parámetro ConImplante (0 o 1). Los papeles que no lo mueven se quedan en 0 y siguen con Loco_Caminar y Loco_Correr.
            caminar.motion = ArbolImplante(c, "Caminar", "Loco_Caminar", "Loco_Caminar_ConImplante");
            AnimatorState correr = Estado("Correr", null);
            correr.motion = ArbolImplante(c, "Correr", "Loco_Correr", "Loco_Correr_ConImplante");
            Estado("GirarDer", null).motion = ArbolConImplante(c, "GirarDer", "Loco_GirarDer");
            Estado("GirarIzq", null).motion = ArbolConImplante(c, "GirarIzq", "Loco_GirarIzq");
            Estado("Frenar", null).motion = ArbolConImplante(c, "Frenar", "Loco_Frenar");
            // T-985: giro en el sitio apuntando con la pistola (PJ01_Pistola_ApuntarCaminar_*, solo existen de PJ01: van directo en la base como los de la moto).
            Estado("GirarApuntarDer", "PJ01_Pistola_ApuntarCaminar_Derecha");
            Estado("GirarApuntarIzq", "PJ01_Pistola_ApuntarCaminar_Izquierda");
            Estado("SaltoInicio", "Loco_SaltoInicio");
            Estado("SaltoAire", "Loco_SaltoAire");
            Estado("SaltoAterrizaje", "Loco_SaltoAterrizaje");
            Estado("Caida", "Loco_Caida");
            // T-1505: aterrizaje fuerte tras una caída larga (trigger AterrizajeFuerte que solo dispara el jugador); ReglaBloqueoAnimacion lo bloquea mientras dura.
            Estado("AterrizajeFuerte", "PJ01_Aterrizaje_Fuerte");
            // T-733: los clips de agachado de Tony (agarre.txt): agacharse, quieto, caminar en las cuatro direcciones, pararse y rodar.
            AnimatorState agachadoCaminar = Estado("AgachadoCaminar", null);
            agachadoCaminar.motion = Arbol2D(c, "AgachadoCaminar", new (string, Vector2)[]
            {
                ("Loco_AgachadoCaminar", new Vector2(0f, 1f)), ("Loco_AgachadoAtras", new Vector2(0f, -1f)),
                ("Loco_AgachadoLateral_Derecha", new Vector2(1f, 0f)), ("Loco_AgachadoLateral_Izquierda", new Vector2(-1f, 0f)),
            });
            Estado("AgachadoQuieto", "Loco_AgachadoIdle");
            Estado("Agacharse", "Loco_Agacharse");
            Estado("Pararse", "Loco_Pararse");
            Estado("Rodar", "Loco_Rodar");
            Estado("CoberturaEntrar", "Cobertura_Entrar");
            // T-1515: Mantener (quieto en cobertura) es PJ01_Cobertura_Quieto en bucle (agachado respirando y vigilando); antes congelaba la última pose de Entrar.
            // Asomarse y Regresar son PJ01_Cobertura_Asomarse y PJ01_Cobertura_Regresar. Los tres van directo en la base (solo existen de PJ01).
            Estado("CoberturaMantener", "PJ01_Cobertura_Quieto");
            Estado("CoberturaAsomarse", "PJ01_Cobertura_Asomarse");
            Estado("CoberturaRegresar", "PJ01_Cobertura_Regresar");
            // T-1515: escalones de concreto (solo el jugador pone Escalones; la rapidez sigue a su velocidad). Son los clips PJ01_Escaleras_*, no la escalera de mano.
            foreach ((string nombreEscalon, string clipEscalon) in new[] { ("EscalonesSubir", "PJ01_Escaleras_Subir"), ("EscalonesBajar", "PJ01_Escaleras_Bajar") })
            {
                AnimatorState escalon = Estado(nombreEscalon, clipEscalon);
                escalon.speedParameterActive = true;
                escalon.speedParameter = ReglasAnimacion.RapidezEscalones;
            }
            Estado("CoberturaDisparo1", "Cobertura_Disparo");
            Estado("CoberturaDisparo2", "Cobertura_Disparo2");
            // T-1310: cobertura con arma larga (solo existen de PJ01: van directo en la base).
            Estado("CoberturaRifleEntrar", "PJ01_CoberturaRifle_Entrar");
            Estado("CoberturaRifleIdle", "PJ01_CoberturaRifle_Idle");
            Estado("CoberturaRifleDisparo", "PJ01_CoberturaRifle_Disparo");
            Estado("MuerteAdelante", "Muerte_Adelante");
            Estado("MuerteAtras", "Muerte_Atras");
            Estado("MuerteDesmayo", "Muerte_Desmayo");
            Estado("LevantarseBocaArriba", "Dano_Levantarse_BocaArriba");
            Estado("LevantarseBocaAbajo", "Dano_Levantarse_BocaAbajo");
            ConstruirGestos(m, e, Estado);
            m.defaultState = e["Idle"];

            string[] suelo = { "Idle", "Caminar", "Correr", "GirarDer", "GirarIzq", "GirarApuntarDer", "GirarApuntarIzq", "Frenar", "AgachadoCaminar", "AgachadoQuieto", "EscalonesSubir", "EscalonesBajar" };

            // Agachado (T-733): de pie se baja con Loco_Agacharse y queda en AgachadoQuieto o AgachadoCaminar (por velocidad); para levantarse usa Loco_Pararse.
            foreach (string s in new[] { "Idle", "Caminar", "Correr", "GirarDer", "GirarIzq", "Frenar" })
            {
                Unir(e[s], e["Agacharse"], false, 0f, Mezcla, Bool(ReglasAnimacion.Agachado, true), Bool(ReglasVisual.EnSuelo, true));
            }
            Unir(e["Agacharse"], e["AgachadoCaminar"], true, SalidaUnaVez, Mezcla, Bool(ReglasAnimacion.Agachado, true), Mayor(ReglasVisual.Velocidad, UmbralCaminar));
            Unir(e["Agacharse"], e["AgachadoQuieto"], true, SalidaUnaVez, Mezcla, Bool(ReglasAnimacion.Agachado, true));
            Unir(e["Agacharse"], e["Pararse"], false, 0f, Mezcla, Bool(ReglasAnimacion.Agachado, false));
            Unir(e["AgachadoQuieto"], e["AgachadoCaminar"], false, 0f, Mezcla, Bool(ReglasAnimacion.Agachado, true), Mayor(ReglasVisual.Velocidad, UmbralCaminar));
            Unir(e["AgachadoCaminar"], e["AgachadoQuieto"], false, 0f, Mezcla, Bool(ReglasAnimacion.Agachado, true), Menor(ReglasVisual.Velocidad, UmbralCaminar));
            foreach (string s in new[] { "AgachadoCaminar", "AgachadoQuieto" })
            {
                Unir(e[s], e["Pararse"], false, 0f, Mezcla, Bool(ReglasAnimacion.Agachado, false));
            }
            Unir(e["Pararse"], e["Caminar"], true, SalidaUnaVez, Mezcla, Mayor(ReglasVisual.Velocidad, UmbralCaminar));
            Unir(e["Pararse"], e["Idle"], true, SalidaUnaVez, Mezcla);
            // Rodar (desde cualquier estado menos las muertes): al acabar sigue agachado (caminando o quieto) o se levanta con Pararse.
            AnimatorStateTransition rodar = m.AddAnyStateTransition(e["Rodar"]);
            Configurar(rodar, false, 0f, MezclaCorta, false, Disparador(ReglasAnimacion.Rodar), Bool(ReglasVisual.Caer, false));
            Unir(e["Rodar"], e["Pararse"], true, SalidaUnaVez, Mezcla, Bool(ReglasAnimacion.Agachado, false));
            Unir(e["Rodar"], e["AgachadoCaminar"], true, SalidaUnaVez, Mezcla, Bool(ReglasAnimacion.Agachado, true), Mayor(ReglasVisual.Velocidad, UmbralCaminar));
            Unir(e["Rodar"], e["AgachadoQuieto"], true, SalidaUnaVez, Mezcla, Bool(ReglasAnimacion.Agachado, true));

            // Frenar al soltar el avance corriendo.
            foreach (string s in new[] { "Caminar", "Correr" })
            {
                Unir(e[s], e["Frenar"], false, 0f, MezclaCorta, Disparador(ReglasAnimacion.Frenar), Bool(ReglasVisual.EnSuelo, true));
            }
            Unir(e["Frenar"], e["Idle"], true, SalidaUnaVez, Mezcla);

            // Locomoción por Velocidad.
            Unir(e["Idle"], e["Caminar"], false, 0f, Mezcla, Mayor(ReglasVisual.Velocidad, UmbralCaminar));
            Unir(e["Caminar"], e["Idle"], false, 0f, Mezcla, Menor(ReglasVisual.Velocidad, UmbralCaminar));
            Unir(e["Caminar"], e["Correr"], false, 0f, Mezcla, Mayor(ReglasVisual.Velocidad, ReglasAnimacion.UmbralCorrer));
            Unir(e["Correr"], e["Caminar"], false, 0f, Mezcla, Menor(ReglasVisual.Velocidad, ReglasAnimacion.UmbralCorrer * ReglasAnimacion.HisteresisCorrer));
            Unir(e["Frenar"], e["Caminar"], false, 0f, Mezcla, Mayor(ReglasVisual.Velocidad, ReglasAnimacion.UmbralCorrer * 0.5f));

            // T-1515: escalones de concreto. De Caminar (o Correr, si el trote baja de la velocidad máxima) se pasa a EscalonesSubir/Bajar mientras Escalones lo pida y
            // la velocidad sea de andar; al terminar o acelerar vuelve a Caminar, al quedarse quieto a Idle, y en el aire a Caminar (que ya sabe saltar).
            for (int tipoEscalon = 1; tipoEscalon <= 2; tipoEscalon++)
            {
                string estadoEscalon = tipoEscalon == 1 ? "EscalonesSubir" : "EscalonesBajar";
                foreach (string s in new[] { "Caminar", "Correr" })
                {
                    Unir(e[s], e[estadoEscalon], false, 0f, Mezcla, Entero(ReglasAnimacion.Escalones, tipoEscalon), Bool(ReglasVisual.EnSuelo, true),
                        Mayor(ReglasVisual.Velocidad, UmbralCaminar), Menor(ReglasVisual.Velocidad, ReglasAnimacion.VelocidadMaximaEscalones));
                }
                Unir(e[estadoEscalon], e["Idle"], false, 0f, Mezcla, Menor(ReglasVisual.Velocidad, UmbralCaminar));
                Unir(e[estadoEscalon], e["Caminar"], false, 0f, Mezcla, Entero(ReglasAnimacion.Escalones, 0));
                Unir(e[estadoEscalon], e["Caminar"], false, 0f, Mezcla, Distinto(ReglasAnimacion.Escalones, tipoEscalon));
                Unir(e[estadoEscalon], e["Caminar"], false, 0f, Mezcla, Mayor(ReglasVisual.Velocidad, ReglasAnimacion.VelocidadMaximaEscalones + 0.3f));
                Unir(e[estadoEscalon], e["Caminar"], false, 0f, Mezcla, Bool(ReglasVisual.EnSuelo, false));
            }

            // T-985: apuntando con la pistola el giro en el sitio es PJ01_Pistola_ApuntarCaminar_*; va antes que GirarDer/GirarIzq (gana la primera transición cierta).
            int pistolaGiro = (int)TipoArmaVisual.Pistola;
            Unir(e["Idle"], e["GirarApuntarDer"], false, 0f, Mezcla, Mayor(ReglasAnimacion.Giro, ReglasAnimacion.UmbralGiro), Menor(ReglasVisual.Velocidad, UmbralCaminar), Bool(ReglasVisual.Apuntando, true), Entero(ReglasVisual.TipoArma, pistolaGiro));
            Unir(e["Idle"], e["GirarApuntarIzq"], false, 0f, Mezcla, Menor(ReglasAnimacion.Giro, -ReglasAnimacion.UmbralGiro), Menor(ReglasVisual.Velocidad, UmbralCaminar), Bool(ReglasVisual.Apuntando, true), Entero(ReglasVisual.TipoArma, pistolaGiro));
            foreach (string g in new[] { "GirarApuntarDer", "GirarApuntarIzq" })
            {
                Unir(e[g], e["Caminar"], false, 0f, Mezcla, Mayor(ReglasVisual.Velocidad, UmbralCaminar));
                Unir(e[g], e["Idle"], false, 0f, Mezcla, Bool(ReglasVisual.Apuntando, false));
                Unir(e[g], e["Idle"], true, SalidaUnaVez, Mezcla);
            }
            // Giro en el sitio.
            Unir(e["Idle"], e["GirarDer"], false, 0f, Mezcla, Mayor(ReglasAnimacion.Giro, ReglasAnimacion.UmbralGiro), Menor(ReglasVisual.Velocidad, UmbralCaminar));
            Unir(e["Idle"], e["GirarIzq"], false, 0f, Mezcla, Menor(ReglasAnimacion.Giro, -ReglasAnimacion.UmbralGiro), Menor(ReglasVisual.Velocidad, UmbralCaminar));
            foreach (string g in new[] { "GirarDer", "GirarIzq" })
            {
                Unir(e[g], e["Caminar"], false, 0f, Mezcla, Mayor(ReglasVisual.Velocidad, UmbralCaminar));
                Unir(e[g], e["Idle"], true, SalidaUnaVez, Mezcla);
            }

            // Salto en tres fases y caída.
            foreach (string s in suelo)
            {
                Unir(e[s], e["Caida"], false, 0f, Mezcla, Bool(ReglasVisual.EnSuelo, false), Menor(ReglasAnimacion.VelY, -ReglasAnimacion.VelocidadCaidaLibre));
                Unir(e[s], e["SaltoInicio"], false, 0f, MezclaCorta, Bool(ReglasVisual.EnSuelo, false));
            }
            Unir(e["SaltoInicio"], e["Caida"], false, 0f, Mezcla, Bool(ReglasVisual.EnSuelo, false), Menor(ReglasAnimacion.VelY, -ReglasAnimacion.VelocidadCaidaLibre));
            Unir(e["SaltoInicio"], e["SaltoAterrizaje"], false, 0f, MezclaCorta, Bool(ReglasVisual.EnSuelo, true));
            Unir(e["SaltoInicio"], e["SaltoAire"], true, 0.8f, Mezcla);
            Unir(e["SaltoAire"], e["Caida"], false, 0f, Mezcla, Menor(ReglasAnimacion.VelY, -ReglasAnimacion.VelocidadCaidaLibre));
            Unir(e["SaltoAire"], e["SaltoAterrizaje"], false, 0f, MezclaCorta, Bool(ReglasVisual.EnSuelo, true));
            Unir(e["Caida"], e["SaltoAterrizaje"], false, 0f, MezclaCorta, Bool(ReglasVisual.EnSuelo, true));
            AnimatorStateTransition fuerte = m.AddAnyStateTransition(e["AterrizajeFuerte"]);
            Configurar(fuerte, false, 0f, MezclaCorta, false, Disparador(ReglasAnimacion.AterrizajeFuerte), Bool(ReglasVisual.EnSuelo, true), Bool(ReglasVisual.Caer, false));
            Unir(e["AterrizajeFuerte"], e["Idle"], true, SalidaUnaVez, Mezcla);
            Unir(e["SaltoAterrizaje"], e["Idle"], true, SalidaUnaVez, Mezcla);
            Unir(e["SaltoAterrizaje"], e["Caida"], false, 0f, Mezcla, Bool(ReglasVisual.EnSuelo, false), Menor(ReglasAnimacion.VelY, -ReglasAnimacion.VelocidadCaidaLibre));

            // Cobertura: entrar, quedarse, asomarse a disparar y salir.
            Unir(e["CoberturaEntrar"], e["CoberturaMantener"], true, SalidaUnaVez, Mezcla);
            Unir(e["CoberturaMantener"], e["CoberturaDisparo1"], false, 0f, MezclaCorta, Disparador(ReglasAnimacion.DisparoCobertura), Entero(ReglasAnimacion.CoberturaTipo, 0));
            Unir(e["CoberturaMantener"], e["CoberturaDisparo2"], false, 0f, MezclaCorta, Disparador(ReglasAnimacion.DisparoCobertura), Entero(ReglasAnimacion.CoberturaTipo, 1));
            Unir(e["CoberturaDisparo1"], e["CoberturaMantener"], true, SalidaUnaVez, Mezcla);
            Unir(e["CoberturaDisparo2"], e["CoberturaMantener"], true, SalidaUnaVez, Mezcla);
            // T-1515: al apuntar se asoma (Asomarse y se queda en su última pose mientras apunta); al soltar regresa (Regresar) y vuelve a Quieto.
            Unir(e["CoberturaMantener"], e["CoberturaAsomarse"], false, 0f, MezclaCorta, Bool(ReglasVisual.Apuntando, true));
            Unir(e["CoberturaAsomarse"], e["CoberturaRegresar"], false, 0f, MezclaCorta, Bool(ReglasVisual.Apuntando, false));
            Unir(e["CoberturaRegresar"], e["CoberturaAsomarse"], false, 0f, MezclaCorta, Bool(ReglasVisual.Apuntando, true));
            Unir(e["CoberturaRegresar"], e["CoberturaMantener"], true, SalidaUnaVez, Mezcla);
            foreach (string s in new[] { "CoberturaEntrar", "CoberturaMantener", "CoberturaAsomarse", "CoberturaRegresar", "CoberturaDisparo1", "CoberturaDisparo2" })
            {
                Unir(e[s], e["Idle"], false, 0f, Mezcla, Bool(ReglasVisual.Cubierto, false));
            }

            // T-1310: cobertura con rifle. Se elige al entrar (ArmaLargaEquipada); entrar, quedarse (bucle), asomarse por la derecha a disparar y salir.
            Unir(e["CoberturaRifleEntrar"], e["CoberturaRifleIdle"], true, SalidaUnaVez, Mezcla);
            Unir(e["CoberturaRifleIdle"], e["CoberturaRifleDisparo"], false, 0f, MezclaCorta, Disparador(ReglasAnimacion.DisparoCobertura));
            Unir(e["CoberturaRifleDisparo"], e["CoberturaRifleIdle"], true, SalidaUnaVez, Mezcla);
            foreach (string s in new[] { "CoberturaRifleEntrar", "CoberturaRifleIdle", "CoberturaRifleDisparo" })
            {
                Unir(e[s], e["Idle"], false, 0f, Mezcla, Bool(ReglasVisual.Cubierto, false));
            }

            // Muerte según el tipo (desde cualquier estado) y levantarse boca arriba o abajo.
            string[] muertes = { "MuerteAdelante", "MuerteAtras", "MuerteDesmayo" };
            for (int i = 0; i < muertes.Length; i++)
            {
                AnimatorStateTransition t = m.AddAnyStateTransition(e[muertes[i]]);
                Configurar(t, false, 0f, Mezcla, false, Bool(ReglasVisual.Caer, true), Entero(ReglasAnimacion.MuerteTipo, i));
            }
            foreach (string muerte in muertes)
            {
                Unir(e[muerte], e["LevantarseBocaAbajo"], false, 0f, Mezcla, Bool(ReglasVisual.Caer, false), Entero(ReglasAnimacion.MuerteTipo, 0));
                Unir(e[muerte], e["LevantarseBocaArriba"], false, 0f, Mezcla, Bool(ReglasVisual.Caer, false), Mayor(ReglasAnimacion.MuerteTipo, 0));
            }
            Unir(e["LevantarseBocaArriba"], e["Idle"], true, SalidaUnaVez, Mezcla);
            Unir(e["LevantarseBocaAbajo"], e["Idle"], true, SalidaUnaVez, Mezcla);

            // Entrar en cobertura desde el suelo (no desde cualquier estado: si no, saldría de Mantener y entraría otra vez en bucle).
            foreach (string s in new[] { "Idle", "Caminar", "Correr", "GirarDer", "GirarIzq", "GirarApuntarDer", "GirarApuntarIzq", "Frenar", "AgachadoCaminar", "AgachadoQuieto", "Agacharse", "Pararse", "SaltoAterrizaje", "EscalonesSubir", "EscalonesBajar" })
            {
                // T-1310: con arma larga va antes (gana la primera transición cierta).
                Unir(e[s], e["CoberturaRifleEntrar"], false, 0f, Mezcla, Bool(ReglasVisual.Cubierto, true), Bool(ReglasAnimacion.ArmaLargaEquipada, true));
                Unir(e[s], e["CoberturaEntrar"], false, 0f, Mezcla, Bool(ReglasVisual.Cubierto, true));
            }

            // IK en la capa base: manos de ApuntadoIK, pies y mirada de IKPiesYMirada.
            AnimatorControllerLayer[] capas = c.layers;
            capas[0].iKPass = true;
            c.layers = capas;
        }

        /// <summary>Clip por defecto de cada estado de gesto (un clip propio por estado, para que los overrides de papel no se pisen).</summary>
        public static readonly (string Estado, string Clip, GestoVisual Gesto)[] ClipsGestos =
        {
            ("CarroEntrar", "Carro_Entrar", GestoVisual.CarroEntrar), ("CarroSalir", "Carro_Salir", GestoVisual.CarroSalir),
            ("AbrirPuerta", "Inter_AbrirPuerta", GestoVisual.AbrirPuerta), ("Boton", "Inter_Boton", GestoVisual.Boton),
            ("Recoger", "Inter_Recoger", GestoVisual.Recoger), ("Telefono", "Inter_Telefono", GestoVisual.Telefono),
            ("Burla", "Skull_Burla", GestoVisual.Burla), ("Presumir", "Nova_Flexing", GestoVisual.Presumir),
            // T-963: la moto no tenía clips (era una pose por IK); los de Blender van directo en la base (no hay de Mixamo que cambiar).
            ("MotoSubir", "PJ01_Moto_Subir", GestoVisual.MotoSubir), ("MotoBajar", "PJ01_Moto_Bajar", GestoVisual.MotoBajar),
        };

        // T-622 a T-624: gestos de una vez (trigger Gesto + GestoTipo, desde cualquier estado menos las muertes), manejar y ver el celular.
        private static void ConstruirGestos(AnimatorStateMachine m, Dictionary<string, AnimatorState> e, System.Func<string, string, float, AnimatorState> estado)
        {
            foreach ((string nombre, string clip, GestoVisual _) in ClipsGestos)
            {
                estado(nombre, clip, 1f).tag = ReglasAnimacion.EtiquetaGesto;
            }
            estado("CarroManejar", "Carro_Manejar_Bucle", 1f).tag = ReglasAnimacion.EtiquetaGesto;
            estado("VerTelefono", "Inter_VerTelefono_Bucle", 1f).tag = ReglasAnimacion.EtiquetaGesto;
            estado("MotoManejar", "PJ01_Manejar_Moto", 1f).tag = ReglasAnimacion.EtiquetaGesto;

            for (int i = 0; i < ClipsGestos.Length; i++)
            {
                AnimatorStateTransition t = m.AddAnyStateTransition(e[ClipsGestos[i].Estado]);
                // T-976: de manejar la moto a bajar no se mezcla: el primer cuadro de MotoBajar ya es la postura de MotoManejar (cadera a 2 mm) y la mezcla corta
                // bajaba todo el cuerpo 6 cm a medio camino.
                float mezclaGesto = ClipsGestos[i].Estado == "MotoBajar" ? 0f : MezclaCorta;
                Configurar(t, false, 0f, mezclaGesto, true, Disparador(ReglasAnimacion.Gesto), Entero(ReglasAnimacion.GestoTipo, (int)ClipsGestos[i].Gesto));
                if (ClipsGestos[i].Estado == "CarroEntrar")
                {
                    // Sube y se queda al volante; si Manejando ya se quitó, vuelve a pararse.
                    Unir(e["CarroEntrar"], e["CarroManejar"], true, SalidaUnaVez, Mezcla, Bool(ReglasAnimacion.Manejando, true));
                }
                if (ClipsGestos[i].Estado == "MotoSubir")
                {
                    // T-963: igual en la moto: termina de subir y se queda manejando.
                    Unir(e["MotoSubir"], e["MotoManejar"], true, SalidaUnaVez, Mezcla, Bool(ReglasAnimacion.Manejando, true));
                }
                if (ClipsGestos[i].Estado == "MotoBajar")
                {
                    // T-976: la mezcla con Idle cruzaba la pierna derecha por el aire (pie a 1.5 m en medio de la mezcla). El último cuadro de MotoBajar ya es la postura de
                    // Idle (pies a menos de 4 cm), así que se cambia sin mezcla al terminar el clip.
                    Unir(e["MotoBajar"], e["Idle"], true, 1f, 0f);
                    continue;
                }
                Unir(e[ClipsGestos[i].Estado], e["Idle"], true, SalidaUnaVez, Mezcla);
            }
            Unir(e["CarroManejar"], e["Idle"], false, 0f, Mezcla, Bool(ReglasAnimacion.Manejando, false));
            Unir(e["MotoManejar"], e["Idle"], false, 0f, Mezcla, Bool(ReglasAnimacion.Manejando, false));
            foreach (string s in new[] { "Idle", "Caminar", "GirarDer", "GirarIzq" })
            {
                Unir(e[s], e["VerTelefono"], false, 0f, Mezcla, Bool(ReglasAnimacion.ViendoTelefono, true), Menor(ReglasVisual.Velocidad, UmbralCaminar));
            }
            Unir(e["VerTelefono"], e["Idle"], false, 0f, Mezcla, Bool(ReglasAnimacion.ViendoTelefono, false));
            Unir(e["VerTelefono"], e["Caminar"], false, 0f, Mezcla, Mayor(ReglasVisual.Velocidad, UmbralCaminar));
        }

        private static BlendTree Arbol2D(AnimatorController c, string nombre, (string clip, Vector2 pos)[] hijos, string parametroX = ReglasAnimacion.VelX, string parametroY = ReglasAnimacion.VelZ)
        {
            var arbol = new BlendTree
            {
                name = nombre,
                blendType = BlendTreeType.SimpleDirectional2D,
                blendParameter = parametroX,
                blendParameterY = parametroY,
                useAutomaticThresholds = false,
            };
            foreach ((string clip, Vector2 pos) in hijos)
            {
                arbol.AddChild(Clip(clip), pos);
            }
            AssetDatabase.AddObjectToAsset(arbol, c);
            return arbol;
        }

        /// <summary>T-960: la versión con implante de un clip de locomoción de Mixamo es el clip de Blender PJ01_&lt;clip&gt;_ConImplante (solo existe para PJ01).</summary>
        public static string ClipConImplante(string clipLoco) => PrefijoClipPJ01 + clipLoco + "_ConImplante";

        /// <summary>T-960: árbol 1D por ConImplante: 0 el clip de la base (el override de cada papel lo cambia) y 1 su versión con implante de PJ01.</summary>
        private static BlendTree ArbolConImplante(AnimatorController c, string nombre, string clipSin)
        {
            return Arbol1D(c, nombre, ReglasAnimacion.ConImplante, new (string, float)[] { (clipSin, 0f), (ClipConImplante(clipSin), 1f) });
        }

        // T-1505: Caminar es 1D por Velocidad (clip lento en 1.6 m/s, clip rápido en 4.5) dentro de cada dirección del 2D. Correr (nombre "Correr") deja
        // adelante el clip de correr y de lado y atrás los roles de trote.
        private static BlendTree ArbolImplante(AnimatorController c, string nombre, string clipSin, string clipCon)
        {
            bool esCaminar = nombre == "Caminar";
            BlendTree Mezcla1D(string sufijo, string lento, string rapido) => Arbol1D(c, nombre + sufijo, ReglasVisual.Velocidad,
                new (string, float)[] { (lento, UmbralCaminarLento), (rapido, UmbralTrote) });
            // Un lado o atrás: en Caminar (sin implante) mezcla el clip lento con el rol de trote; con implante el clip con implante; en Correr el rol de trote.
            Motion Direccion(string sufijo, string lento, string rol, bool implante)
            {
                if (implante) { return Clip(ClipConImplante(lento)); }
                return esCaminar ? (Motion)Mezcla1D(sufijo, lento, rol) : Clip(rol);
            }
            // T-960: los lados y atrás con implante son los PJ01_Loco_Caminar*_ConImplante (antes el árbol con implante repetía los de Mixamo).
            BlendTree Lados(string sufijo, string adelante, bool implante)
            {
                var arbol = new BlendTree
                {
                    name = nombre + sufijo,
                    blendType = BlendTreeType.SimpleDirectional2D,
                    blendParameter = ReglasAnimacion.VelX,
                    blendParameterY = ReglasAnimacion.VelZ,
                    useAutomaticThresholds = false,
                };
                AssetDatabase.AddObjectToAsset(arbol, c);
                Motion adelanteMotion = esCaminar ? (Motion)Mezcla1D(sufijo + "_Ade", adelante, implante ? RolTroteConImplante : RolTrote) : Clip(adelante);
                arbol.AddChild(adelanteMotion, new Vector2(0f, 1f));
                arbol.AddChild(Direccion(sufijo + "_Atr", "Loco_CaminarAtras", RolTroteAtras, implante), new Vector2(0f, -1f));
                arbol.AddChild(Direccion(sufijo + "_Der", "Loco_CaminarDer", RolTroteDer, implante), new Vector2(1f, 0f));
                arbol.AddChild(Direccion(sufijo + "_Izq", "Loco_CaminarIzq", RolTroteIzq, implante), new Vector2(-1f, 0f));
                return arbol;
            }
            var raiz = new BlendTree
            {
                name = nombre,
                blendType = BlendTreeType.Simple1D,
                blendParameter = ReglasAnimacion.ConImplante,
                useAutomaticThresholds = false,
            };
            raiz.AddChild(Lados("_Sin", clipSin, false), 0f);
            raiz.AddChild(Lados("_Con", clipCon, true), 1f);
            AssetDatabase.AddObjectToAsset(raiz, c);
            return raiz;
        }

        private static BlendTree Arbol1D(AnimatorController c, string nombre, string parametro, (string clip, float umbral)[] hijos)
        {
            var arbol = new BlendTree
            {
                name = nombre,
                blendType = BlendTreeType.Simple1D,
                blendParameter = parametro,
                useAutomaticThresholds = false,
            };
            foreach ((string clip, float umbral) in hijos)
            {
                arbol.AddChild(Clip(clip), umbral);
            }
            AssetDatabase.AddObjectToAsset(arbol, c);
            return arbol;
        }

        // ---------- Capa de arriba ----------

        private static void ConstruirArriba(AnimatorController c, AvatarMask mascara)
        {
            c.AddLayer(ReglasAnimacion.CapaArriba);
            AnimatorControllerLayer[] capas = c.layers;
            capas[1].avatarMask = mascara;
            capas[1].blendingMode = AnimatorLayerBlendingMode.Override;
            capas[1].defaultWeight = 1f;
            capas[1].iKPass = true;
            c.layers = capas;
            AnimatorStateMachine m = c.layers[1].stateMachine;
            var e = new Dictionary<string, AnimatorState>();
            AnimatorState Estado(string nombre, Motion motion)
            {
                AnimatorState s = m.AddState(nombre);
                s.motion = motion;
                s.speed = ReglasAnimacion.VelocidadEstado(nombre);
                e[nombre] = s;
                return s;
            }

            Estado("Vacio", null);
            // T-985: guardia de puños (PJ01_Punos_Guardia) unos segundos después de cada golpe del jugador (parámetro EnGuardia, ver VisualPersonaje.EnGuardia).
            Estado("PunosGuardia", Clip("PJ01_Punos_Guardia"));
            m.defaultState = e["Vacio"];
            // T-652: solo el jugador acelera sus puños (RapidezGolpe); los enemigos dejan el parámetro en 1 y conservan su tiempo.
            foreach (string golpe in new[] { "GolpeIzq", "GolpeDer", "GolpeFuerte" })
            {
                AnimatorState g = Estado(golpe, Clip(golpe == "GolpeIzq" ? "Punos_GolpeIzq" : golpe == "GolpeDer" ? "Punos_GolpeDer" : "Punos_Fuerte"));
                g.speedParameterActive = true;
                g.speedParameter = ReglasAnimacion.RapidezGolpe;
            }
            Estado("PistolaIdle", Arbol1D(c, "PistolaIdle", ReglasVisual.Velocidad, new (string, float)[]
            {
                ("Pistola_Idle", 0f), ("Pistola_Caminar", ReglasAnimacion.VelocidadAndar), ("Pistola_Correr", ReglasAnimacion.VelocidadCarrera),
            }));
            // T-733: sacar y guardar la pistola (la pistola solo se ve mientras se apunta), apuntar quieto o caminando en las cuatro direcciones
            // (árbol 2D sobre ApuntarX/ApuntarZ) y apuntar agachado (quieto o caminando, sin disparar).
            Estado("PistolaSacar", Clip("Pistola_Sacar"));
            Estado("PistolaGuardar", Clip("Pistola_Guardar"));
            Estado("PistolaApuntar", Arbol2D(c, "PistolaApuntar", new (string, Vector2)[]
            {
                ("Pistola_Apuntar", Vector2.zero), ("Pistola_Caminar", new Vector2(0f, 1f)), ("Pistola_ApuntarAtras", new Vector2(0f, -1f)),
                ("Pistola_ApuntarLateral_Derecha", new Vector2(1f, 0f)), ("Pistola_ApuntarLateral_Izquierda", new Vector2(-1f, 0f)),
            }, ReglasAnimacion.ApuntarX, ReglasAnimacion.ApuntarZ));
            Estado("PistolaAgachado", Arbol1D(c, "PistolaAgachado", ReglasVisual.Velocidad, new (string, float)[]
            {
                ("Pistola_AgachadoIdle", 0f), ("Pistola_AgachadoCaminar", ReglasAnimacion.VelocidadAgachadoCaminando),
            }));
            Estado("PistolaDisparo", Clip("Pistola_Disparo"));
            Estado("PistolaRecargar", Clip("Pistola_Recargar"));
            Estado("PistolaRecargarCaminar", Clip("Pistola_RecargarCaminar"));
            Estado("LargaIdle", Arbol1D(c, "LargaIdle", ReglasVisual.Velocidad, new (string, float)[]
            {
                ("Rifle_Idle", 0f), ("Rifle_Caminar", ReglasAnimacion.VelocidadAndar), ("Rifle_Correr", ReglasAnimacion.VelocidadCarrera),
            }));
            Estado("LargaApuntar", Arbol1D(c, "LargaApuntar", ReglasVisual.Velocidad, new (string, float)[]
            {
                ("Rifle_Apuntar", 0f), ("Rifle_ApuntarCaminar", ReglasAnimacion.VelocidadAndar), ("Rifle_ApuntarCorrer", ReglasAnimacion.VelocidadCarrera),
            }));
            Estado("LargaDisparo", Clip("Rifle_DisparoCaminar"));
            Estado("LargaRecargar", Clip("Rifle_Recargar"));
            // T-622: tocar el claxon sentado al volante: solo los brazos (la capa de arriba) se mueven.
            Estado("Pitar", Clip("Carro_Pitar"));

            int pistola = (int)TipoArmaVisual.Pistola;
            int larga = (int)TipoArmaVisual.ArmaLarga;

            // Vacío es el centro: de ahí se va al arma que se tiene en la mano. La pistola se saca (Pistola_Sacar) y al dejar de apuntar se guarda.
            Unir(e["Vacio"], e["PistolaSacar"], false, 0f, MezclaCorta, Entero(ReglasVisual.TipoArma, pistola));
            Unir(e["PistolaSacar"], e["PistolaAgachado"], true, SalidaUnaVez, Mezcla, Bool(ReglasVisual.Apuntando, true), Bool(ReglasAnimacion.Agachado, true));
            Unir(e["PistolaSacar"], e["PistolaApuntar"], true, SalidaUnaVez, Mezcla, Bool(ReglasVisual.Apuntando, true));
            Unir(e["PistolaSacar"], e["PistolaIdle"], true, SalidaUnaVez, Mezcla);
            Unir(e["PistolaSacar"], e["PistolaGuardar"], false, 0f, MezclaCorta, Distinto(ReglasVisual.TipoArma, pistola));
            Unir(e["PistolaApuntar"], e["PistolaAgachado"], false, 0f, Mezcla, Bool(ReglasAnimacion.Agachado, true));
            Unir(e["PistolaAgachado"], e["PistolaApuntar"], false, 0f, Mezcla, Bool(ReglasAnimacion.Agachado, false));
            Unir(e["PistolaAgachado"], e["PistolaIdle"], false, 0f, Mezcla, Bool(ReglasVisual.Apuntando, false));
            Unir(e["PistolaAgachado"], e["PistolaGuardar"], false, 0f, MezclaCorta, Distinto(ReglasVisual.TipoArma, pistola));
            Unir(e["PistolaGuardar"], e["Vacio"], true, SalidaUnaVez, Mezcla);
            // T-734: si se vuelve a apuntar mientras se guarda, se saca de nuevo sin esperar a que termine Guardar.
            Unir(e["PistolaGuardar"], e["PistolaSacar"], false, 0f, MezclaCorta, Entero(ReglasVisual.TipoArma, pistola));
            Unir(e["Vacio"], e["LargaIdle"], false, 0f, Mezcla, Entero(ReglasVisual.TipoArma, larga));
            foreach ((string idle, string apuntar, int arma) in new[] { ("PistolaIdle", "PistolaApuntar", pistola), ("LargaIdle", "LargaApuntar", larga) })
            {
                Unir(e[idle], e[apuntar], false, 0f, Mezcla, Bool(ReglasVisual.Apuntando, true));
                // T-734: solo pasa a Idle si el arma sigue levantada (disparó hace poco); si se guarda, va directo a Pistola_Guardar y su evento llega a los 0.53 s.
                Unir(e[apuntar], e[idle], false, 0f, Mezcla, Bool(ReglasVisual.Apuntando, false), Entero(ReglasVisual.TipoArma, arma));
                // La pistola pasa por Pistola_Guardar; las armas largas se sueltan directo.
                string guardar = arma == pistola ? "PistolaGuardar" : "Vacio";
                Unir(e[idle], e[guardar], false, 0f, arma == pistola ? MezclaCorta : Mezcla, Distinto(ReglasVisual.TipoArma, arma));
                Unir(e[apuntar], e[guardar], false, 0f, arma == pistola ? MezclaCorta : Mezcla, Distinto(ReglasVisual.TipoArma, arma));
            }

            // Disparo y recarga desde cualquier estado de arriba, con el arma que se tiene.
            AnyA(m, e["PistolaDisparo"], MezclaCorta, true, Disparador(ReglasVisual.Disparar), Entero(ReglasVisual.TipoArma, pistola));
            AnyA(m, e["LargaDisparo"], MezclaCorta, true, Disparador(ReglasVisual.Disparar), Entero(ReglasVisual.TipoArma, larga));
            AnyA(m, e["PistolaRecargar"], Mezcla, false, Disparador(ReglasVisual.Recargar), Entero(ReglasVisual.TipoArma, pistola), Menor(ReglasVisual.Velocidad, UmbralCaminar * 5f));
            AnyA(m, e["PistolaRecargarCaminar"], Mezcla, false, Disparador(ReglasVisual.Recargar), Entero(ReglasVisual.TipoArma, pistola));
            AnyA(m, e["LargaRecargar"], Mezcla, false, Disparador(ReglasVisual.Recargar), Entero(ReglasVisual.TipoArma, larga));
            foreach (string disparo in new[] { "PistolaDisparo", "LargaDisparo" })
            {
                string apuntar = disparo == "PistolaDisparo" ? "PistolaApuntar" : "LargaApuntar";
                string idle = disparo == "PistolaDisparo" ? "PistolaIdle" : "LargaIdle";
                Unir(e[disparo], e[apuntar], true, SalidaUnaVez, Mezcla, Bool(ReglasVisual.Apuntando, true));
                Unir(e[disparo], e[idle], true, SalidaUnaVez, Mezcla);
            }
            foreach ((string recarga, string idle) in new[] { ("PistolaRecargar", "PistolaIdle"), ("PistolaRecargarCaminar", "PistolaIdle"), ("LargaRecargar", "LargaIdle") })
            {
                Unir(e[recarga], e[idle], true, SalidaUnaVez, Mezcla);
            }

            Unir(e["Vacio"], e["PunosGuardia"], false, 0f, Mezcla, Bool(ReglasAnimacion.EnGuardia, true), Entero(ReglasVisual.TipoArma, 0));
            Unir(e["PunosGuardia"], e["Vacio"], false, 0f, Mezcla, Bool(ReglasAnimacion.EnGuardia, false));
            Unir(e["PunosGuardia"], e["Vacio"], false, 0f, Mezcla, Distinto(ReglasVisual.TipoArma, 0));
            // Puños: izquierda, derecha o fuerte según GolpeTipo.
            string[] golpes = { "GolpeIzq", "GolpeDer", "GolpeFuerte" };
            for (int i = 0; i < golpes.Length; i++)
            {
                AnyA(m, e[golpes[i]], MezclaCorta, true, Disparador(ReglasVisual.Golpe), Entero(ReglasAnimacion.GolpeTipo, i));
                Unir(e[golpes[i]], e["PunosGuardia"], true, SalidaUnaVez, Mezcla, Bool(ReglasAnimacion.EnGuardia, true));
                Unir(e[golpes[i]], e["Vacio"], true, SalidaUnaVez, Mezcla);
            }

            AnyA(m, e["Pitar"], MezclaCorta, true, Disparador(ReglasAnimacion.Gesto), Entero(ReglasAnimacion.GestoTipo, (int)GestoVisual.Pitar));
            Unir(e["Pitar"], e["Vacio"], true, SalidaUnaVez, Mezcla);

        }

        // ---------- Capa de daño (T-735) ----------

        // Aditiva, con la máscara del torso: Pistola_Dano_Frente, Espalda y Fuerte (0.47 s, empiezan y terminan en Pistola_Apuntar, su pose de referencia)
        // suman el estremecimiento a lo que juegue la capa de arriba, así el jugador sigue apuntando y disparando mientras lo golpean. Un solo estado a la vez:
        // un golpe nuevo reemplaza al anterior y no se encima.
        private static void ConstruirDano(AnimatorController c, AvatarMask mascara)
        {
            c.AddLayer(ReglasAnimacion.CapaDano);
            AnimatorControllerLayer[] capas = c.layers;
            int i = capas.Length - 1;
            capas[i].avatarMask = mascara;
            capas[i].blendingMode = AnimatorLayerBlendingMode.Additive;
            capas[i].defaultWeight = 1f;
            c.layers = capas;
            AnimatorStateMachine m = c.layers[i].stateMachine;
            AnimatorState vacio = m.AddState("Vacio");
            m.defaultState = vacio;
            // T-1515: Izquierda (3) y Derecha (4) van directo con PJ01_Dano_Izquierda/Derecha (aditivos, primer cuadro = referencia); solo el jugador manda DanoTipo 3 o 4.
            string[] clips = { "Pistola_Dano_Frente", "Pistola_Dano_Espalda", "Pistola_Dano_Fuerte", "PJ01_Dano_Izquierda", "PJ01_Dano_Derecha" };
            string[] estados = { "DanoFrente", "DanoEspalda", "DanoFuerte", "DanoIzquierda", "DanoDerecha" };
            for (int k = 0; k < estados.Length; k++)
            {
                AnimatorState s = m.AddState(estados[k]);
                s.motion = Clip(clips[k]);
                s.speed = ReglasAnimacion.VelocidadEstado(estados[k]);
                AnimatorStateTransition entrada = m.AddAnyStateTransition(s);
                Configurar(entrada, false, 0f, MezclaCorta, true, Disparador(ReglasVisual.Herido), Entero(ReglasAnimacion.DanoTipo, k));
                Unir(s, vacio, true, SalidaDano, MezclaCorta);
            }
        }

        // ---------- Transiciones ----------

        private static void AnyA(AnimatorStateMachine m, AnimatorState estado, float mezcla, bool aSiMismo, params AnimatorCondition[] condiciones)
        {
            AnimatorStateTransition t = m.AddAnyStateTransition(estado);
            Configurar(t, false, 0f, mezcla, aSiMismo, condiciones);
        }

        private static void Unir(AnimatorState desde, AnimatorState hacia, bool salida, float tiempo, float mezcla, params AnimatorCondition[] condiciones)
        {
            AnimatorStateTransition t = desde.AddTransition(hacia);
            Configurar(t, salida, tiempo, mezcla, false, condiciones);
        }

        private static void Configurar(AnimatorStateTransition t, bool salida, float tiempo, float mezcla, bool sobreSiMismo, params AnimatorCondition[] condiciones)
        {
            t.hasExitTime = salida;
            t.exitTime = tiempo;
            t.duration = mezcla;
            t.hasFixedDuration = true;
            t.canTransitionToSelf = sobreSiMismo;
            foreach (AnimatorCondition cond in condiciones)
            {
                t.AddCondition(cond.mode, cond.threshold, cond.parameter);
            }
        }

        private static AnimatorCondition Mayor(string p, float v) => new AnimatorCondition { parameter = p, mode = AnimatorConditionMode.Greater, threshold = v };
        private static AnimatorCondition Menor(string p, float v) => new AnimatorCondition { parameter = p, mode = AnimatorConditionMode.Less, threshold = v };
        private static AnimatorCondition Bool(string p, bool v) => new AnimatorCondition { parameter = p, mode = v ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot };
        private static AnimatorCondition Entero(string p, int v) => new AnimatorCondition { parameter = p, mode = AnimatorConditionMode.Equals, threshold = v };
        private static AnimatorCondition Distinto(string p, int v) => new AnimatorCondition { parameter = p, mode = AnimatorConditionMode.NotEqual, threshold = v };
        private static AnimatorCondition Disparador(string p) => new AnimatorCondition { parameter = p, mode = AnimatorConditionMode.If };
    }
}

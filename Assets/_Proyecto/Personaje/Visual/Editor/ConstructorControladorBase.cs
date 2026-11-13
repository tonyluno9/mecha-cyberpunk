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
            string carpeta = nombre.StartsWith(PrefijoClipPJ01) ? CarpetaClipsPJ01 : Carpeta;
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
        /// T-624: clips propios de cada papel (clip del controlador base -> clip del papel). SKULLS, su jefe y los guardias de Nova usan los
        /// clips de Enemigos y jefe; el resto de los papeles se queda con los de la base.
        /// </summary>
        public static readonly Dictionary<string, (string Base, string Papel)[]> ClipsPorPapel = new Dictionary<string, (string, string)[]>
        {
            // T-739: el jugador sin implante camina y corre con los clips SinImplante; con implante, el árbol usa los ConImplante (parámetro ConImplante).
            // T-749: todos son los clips de Blender ya aplicados a PJ01 (Humanoid, sin retargeting entre esqueletos distintos); ver ClipsDeJugadorPJ01.
            ["Jugador"] = ClipsDeJugadorPJ01,
            ["SKULLS"] = new[] { ("Loco_Idle", "Skull_Idle"), ("Loco_Caminar", "Skull_Caminar") },
            ["Jefe"] = new[]
            {
                ("Loco_Idle", "Jefe_Idle"), ("Loco_Caminar", "Jefe_Caminar"), ("Skull_Burla", "Jefe_Burla"), ("Pistola_Disparo", "Jefe_Ataque"),
                ("Muerte_Adelante", "Jefe_Muerte"), ("Muerte_Atras", "Jefe_Muerte"), ("Muerte_Desmayo", "Jefe_Muerte"),
            },
            ["Guardias"] = new[]
            {
                ("Loco_Idle", "Nova_Idle"), ("Loco_Caminar", "Nova_Caminar"), ("Loco_Correr", "Nova_Correr"),
                ("Punos_GolpeIzq", "Nova_Punch"), ("Punos_GolpeDer", "Nova_Punch"), ("Punos_Fuerte", "Nova_Barrido"),
                ("Muerte_Adelante", "Nova_Muerte"), ("Muerte_Atras", "Nova_Muerte"), ("Muerte_Desmayo", "Nova_Muerte"),
            },
        };

        private static void AplicarClipsPorPapel()
        {
            foreach (KeyValuePair<string, (string Base, string Papel)[]> papel in ClipsPorPapel)
            {
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
            // Mantener = la última pose de Entrar congelada (no hay clip de estar cubierto).
            // (con speed 0 las transiciones de salida no avanzan: el tiempo se fija con un parámetro).
            AnimatorState mantener = Estado("CoberturaMantener", "Cobertura_Entrar");
            mantener.timeParameterActive = true;
            mantener.timeParameter = ReglasAnimacion.PoseCobertura;
            Estado("CoberturaDisparo1", "Cobertura_Disparo");
            Estado("CoberturaDisparo2", "Cobertura_Disparo2");
            Estado("MuerteAdelante", "Muerte_Adelante");
            Estado("MuerteAtras", "Muerte_Atras");
            Estado("MuerteDesmayo", "Muerte_Desmayo");
            Estado("LevantarseBocaArriba", "Dano_Levantarse_BocaArriba");
            Estado("LevantarseBocaAbajo", "Dano_Levantarse_BocaAbajo");
            ConstruirGestos(m, e, Estado);
            m.defaultState = e["Idle"];

            string[] suelo = { "Idle", "Caminar", "Correr", "GirarDer", "GirarIzq", "GirarApuntarDer", "GirarApuntarIzq", "Frenar", "AgachadoCaminar", "AgachadoQuieto" };

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
            Unir(e["SaltoAterrizaje"], e["Idle"], true, SalidaUnaVez, Mezcla);
            Unir(e["SaltoAterrizaje"], e["Caida"], false, 0f, Mezcla, Bool(ReglasVisual.EnSuelo, false), Menor(ReglasAnimacion.VelY, -ReglasAnimacion.VelocidadCaidaLibre));

            // Cobertura: entrar, quedarse, asomarse a disparar y salir.
            Unir(e["CoberturaEntrar"], e["CoberturaMantener"], true, SalidaUnaVez, Mezcla);
            Unir(e["CoberturaMantener"], e["CoberturaDisparo1"], false, 0f, MezclaCorta, Disparador(ReglasAnimacion.DisparoCobertura), Entero(ReglasAnimacion.CoberturaTipo, 0));
            Unir(e["CoberturaMantener"], e["CoberturaDisparo2"], false, 0f, MezclaCorta, Disparador(ReglasAnimacion.DisparoCobertura), Entero(ReglasAnimacion.CoberturaTipo, 1));
            Unir(e["CoberturaDisparo1"], e["CoberturaMantener"], true, SalidaUnaVez, Mezcla);
            Unir(e["CoberturaDisparo2"], e["CoberturaMantener"], true, SalidaUnaVez, Mezcla);
            foreach (string s in new[] { "CoberturaEntrar", "CoberturaMantener", "CoberturaDisparo1", "CoberturaDisparo2" })
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
            foreach (string s in new[] { "Idle", "Caminar", "Correr", "GirarDer", "GirarIzq", "GirarApuntarDer", "GirarApuntarIzq", "Frenar", "AgachadoCaminar", "AgachadoQuieto", "Agacharse", "Pararse", "SaltoAterrizaje" })
            {
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
            estado("MotoManejar", "PJ01_Moto_Manejar", 1f).tag = ReglasAnimacion.EtiquetaGesto;

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

        private static BlendTree ArbolImplante(AnimatorController c, string nombre, string clipSin, string clipCon)
        {
            // T-960: los lados y atrás con implante son los PJ01_Loco_Caminar*_ConImplante (antes el árbol con implante repetía los de Mixamo).
            BlendTree Lados(string sufijo, string adelante, bool implante) => Arbol2D(c, nombre + sufijo, new (string, Vector2)[]
            {
                (adelante, new Vector2(0f, 1f)), (implante ? ClipConImplante("Loco_CaminarAtras") : "Loco_CaminarAtras", new Vector2(0f, -1f)),
                (implante ? ClipConImplante("Loco_CaminarDer") : "Loco_CaminarDer", new Vector2(1f, 0f)),
                (implante ? ClipConImplante("Loco_CaminarIzq") : "Loco_CaminarIzq", new Vector2(-1f, 0f)),
            });
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
            string[] clips = { "Pistola_Dano_Frente", "Pistola_Dano_Espalda", "Pistola_Dano_Fuerte" };
            string[] estados = { "DanoFrente", "DanoEspalda", "DanoFuerte" };
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

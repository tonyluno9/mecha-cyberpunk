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

        /// <summary>Papeles con override. El nombre del archivo es Override_&lt;Papel&gt;.overrideController.</summary>
        public static readonly string[] Papeles = { "Jugador", "Aliados", "NPC", "SKULLS", "SNAKES", "Guardias", "Robots", "Profes", "Jefe" };

        private const float Mezcla = 0.12f;
        private const float MezclaCorta = 0.06f;
        private const float SalidaUnaVez = 0.88f;
        private const float UmbralCaminar = ReglasVisual.UmbralCaminar;

        public static string RutaOverride(string papel) => CarpetaOverrides + "/Override_" + papel + ".overrideController";

        public static AnimationClip Clip(string nombre)
        {
            foreach (string guid in AssetDatabase.FindAssets(nombre + " t:Model", new[] { Carpeta }))
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
            Debug.LogError("[Personaje] Falta el clip " + nombre + " en " + Carpeta);
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
            ConstruirPiernas(c, CrearMascaraPiernas());
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
                bool arriba = parte == AvatarMaskBodyPart.Body || parte == AvatarMaskBodyPart.Head
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
        /// T-624: clips propios de cada papel (clip del controlador base -> clip del papel). SKULLS, su jefe y los guardias de Nova usan los
        /// clips de Enemigos y jefe; el resto de los papeles se queda con los de la base.
        /// </summary>
        public static readonly Dictionary<string, (string Base, string Papel)[]> ClipsPorPapel = new Dictionary<string, (string, string)[]>
        {
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

            Estado("Idle", "Loco_Idle");
            AnimatorState caminar = Estado("Caminar", null);
            caminar.motion = Arbol2D(c, "Caminar", new (string, Vector2)[]
            {
                ("Loco_Caminar", new Vector2(0f, 1f)), ("Loco_CaminarAtras", new Vector2(0f, -1f)),
                ("Loco_CaminarDer", new Vector2(1f, 0f)), ("Loco_CaminarIzq", new Vector2(-1f, 0f)),
            });
            AnimatorState correr = Estado("Correr", null);
            correr.motion = Arbol2D(c, "Correr", new (string, Vector2)[]
            {
                ("Loco_Correr", new Vector2(0f, 1f)), ("Loco_CaminarAtras", new Vector2(0f, -1f)),
                ("Loco_CaminarDer", new Vector2(1f, 0f)), ("Loco_CaminarIzq", new Vector2(-1f, 0f)),
            });
            Estado("GirarDer", "Loco_GirarDer");
            Estado("GirarIzq", "Loco_GirarIzq");
            Estado("Frenar", "Loco_Frenar");
            Estado("SaltoInicio", "Loco_SaltoInicio");
            Estado("SaltoAire", "Loco_SaltoAire");
            Estado("SaltoAterrizaje", "Loco_SaltoAterrizaje");
            Estado("Caida", "Loco_Caida");
            Estado("AgachadoCaminar", "Rifle_AgachadoCaminar");
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

            string[] suelo = { "Idle", "Caminar", "Correr", "GirarDer", "GirarIzq", "Frenar", "AgachadoCaminar" };

            // Agachado caminando (rifle).
            foreach (string s in new[] { "Idle", "Caminar" })
            {
                Unir(e[s], e["AgachadoCaminar"], false, 0f, Mezcla, Bool(ReglasAnimacion.Agachado, true), Mayor(ReglasVisual.Velocidad, UmbralCaminar));
            }
            Unir(e["AgachadoCaminar"], e["Idle"], false, 0f, Mezcla, Bool(ReglasAnimacion.Agachado, false));
            Unir(e["AgachadoCaminar"], e["Idle"], false, 0f, Mezcla, Menor(ReglasVisual.Velocidad, UmbralCaminar));

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
            foreach (string s in new[] { "Idle", "Caminar", "Correr", "GirarDer", "GirarIzq", "Frenar", "AgachadoCaminar", "SaltoAterrizaje" })
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

            for (int i = 0; i < ClipsGestos.Length; i++)
            {
                AnimatorStateTransition t = m.AddAnyStateTransition(e[ClipsGestos[i].Estado]);
                Configurar(t, false, 0f, MezclaCorta, true, Disparador(ReglasAnimacion.Gesto), Entero(ReglasAnimacion.GestoTipo, (int)ClipsGestos[i].Gesto));
                if (ClipsGestos[i].Estado == "CarroEntrar")
                {
                    // Sube y se queda al volante; si Manejando ya se quitó, vuelve a pararse.
                    Unir(e["CarroEntrar"], e["CarroManejar"], true, SalidaUnaVez, Mezcla, Bool(ReglasAnimacion.Manejando, true));
                }
                Unir(e[ClipsGestos[i].Estado], e["Idle"], true, SalidaUnaVez, Mezcla);
            }
            Unir(e["CarroManejar"], e["Idle"], false, 0f, Mezcla, Bool(ReglasAnimacion.Manejando, false));
            foreach (string s in new[] { "Idle", "Caminar", "GirarDer", "GirarIzq" })
            {
                Unir(e[s], e["VerTelefono"], false, 0f, Mezcla, Bool(ReglasAnimacion.ViendoTelefono, true), Menor(ReglasVisual.Velocidad, UmbralCaminar));
            }
            Unir(e["VerTelefono"], e["Idle"], false, 0f, Mezcla, Bool(ReglasAnimacion.ViendoTelefono, false));
            Unir(e["VerTelefono"], e["Caminar"], false, 0f, Mezcla, Mayor(ReglasVisual.Velocidad, UmbralCaminar));
        }

        private static BlendTree Arbol2D(AnimatorController c, string nombre, (string clip, Vector2 pos)[] hijos)
        {
            var arbol = new BlendTree
            {
                name = nombre,
                blendType = BlendTreeType.SimpleDirectional2D,
                blendParameter = ReglasAnimacion.VelX,
                blendParameterY = ReglasAnimacion.VelZ,
                useAutomaticThresholds = false,
            };
            foreach ((string clip, Vector2 pos) in hijos)
            {
                arbol.AddChild(Clip(clip), pos);
            }
            AssetDatabase.AddObjectToAsset(arbol, c);
            return arbol;
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

        // ---------- Capa de piernas con arma (T-651) ----------

        public const string RutaMascaraPiernas = Carpeta + "/MascaraPiernas.mask";

        private static AvatarMask CrearMascaraPiernas()
        {
            AvatarMask m = AssetDatabase.LoadAssetAtPath<AvatarMask>(RutaMascaraPiernas);
            if (m == null)
            {
                m = new AvatarMask();
                AssetDatabase.CreateAsset(m, RutaMascaraPiernas);
            }
            foreach (AvatarMaskBodyPart parte in System.Enum.GetValues(typeof(AvatarMaskBodyPart)))
            {
                if (parte == AvatarMaskBodyPart.LastBodyPart)
                {
                    continue;
                }
                m.SetHumanoidBodyPartActive(parte, parte == AvatarMaskBodyPart.LeftLeg || parte == AvatarMaskBodyPart.RightLeg);
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// Capa de piernas con máscara de piernas: con arma en la mano y sin apuntar, VisualPersonaje le da peso y las piernas juegan
        /// Pistola_Caminar/Correr o Rifle_Caminar/Correr mezclados por Velocidad (en sincronía con el torso de la capa de arriba).
        /// </summary>
        private static void ConstruirPiernas(AnimatorController c, AvatarMask mascara)
        {
            c.AddLayer(ReglasAnimacion.CapaPiernas);
            AnimatorControllerLayer[] capas = c.layers;
            capas[2].avatarMask = mascara;
            capas[2].blendingMode = AnimatorLayerBlendingMode.Override;
            capas[2].defaultWeight = 0f;
            c.layers = capas;
            AnimatorStateMachine m = c.layers[2].stateMachine;
            AnimatorState vacio = m.AddState("Vacio");
            m.defaultState = vacio;
            int pistola = (int)TipoArmaVisual.Pistola;
            int larga = (int)TipoArmaVisual.ArmaLarga;
            foreach ((string nombre, string caminar, string correr, int arma) in new[]
            {
                ("PistolaPiernas", "Pistola_Caminar", "Pistola_Correr", pistola), ("LargaPiernas", "Rifle_Caminar", "Rifle_Correr", larga),
            })
            {
                AnimatorState s = m.AddState(nombre);
                s.motion = Arbol1D(c, nombre, ReglasVisual.Velocidad, new (string, float)[]
                {
                    (caminar, ReglasAnimacion.VelocidadAndar), (correr, ReglasAnimacion.VelocidadCarrera),
                });
                Unir(vacio, s, false, 0f, Mezcla, Entero(ReglasVisual.TipoArma, arma));
                Unir(s, vacio, false, 0f, Mezcla, Distinto(ReglasVisual.TipoArma, arma));
            }
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
            m.defaultState = e["Vacio"];
            // T-652: solo el jugador acelera sus puños (RapidezGolpe); los enemigos dejan el parámetro en 1 y conservan su tiempo.
            foreach (string golpe in new[] { "GolpeIzq", "GolpeDer", "GolpeFuerte" })
            {
                AnimatorState g = Estado(golpe, Clip(golpe == "GolpeIzq" ? "Punos_GolpeIzq" : golpe == "GolpeDer" ? "Punos_GolpeDer" : "Punos_Fuerte"));
                g.speedParameterActive = true;
                g.speedParameter = ReglasAnimacion.RapidezGolpe;
            }
            Estado("DanoFrente", Clip("Dano_Frente"));
            Estado("DanoEspalda", Clip("Dano_Espalda"));
            Estado("DanoFuerte", Clip("Dano_Fuerte"));
            Estado("PistolaIdle", Arbol1D(c, "PistolaIdle", ReglasVisual.Velocidad, new (string, float)[]
            {
                ("Pistola_Idle", 0f), ("Pistola_Caminar", ReglasAnimacion.VelocidadAndar), ("Pistola_Correr", ReglasAnimacion.VelocidadCarrera),
            }));
            Estado("PistolaApuntar", Arbol1D(c, "PistolaApuntar", ReglasAnimacion.VelX, new (string, float)[]
            {
                ("Pistola_ApuntarCaminar_Izquierda", -1f), ("Pistola_Apuntar", 0f), ("Pistola_ApuntarCaminar_Derecha", 1f),
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

            // Vacío es el centro: de ahí se va al arma que se tiene en la mano.
            Unir(e["Vacio"], e["PistolaIdle"], false, 0f, Mezcla, Entero(ReglasVisual.TipoArma, pistola));
            Unir(e["Vacio"], e["LargaIdle"], false, 0f, Mezcla, Entero(ReglasVisual.TipoArma, larga));
            foreach ((string idle, string apuntar, int arma) in new[] { ("PistolaIdle", "PistolaApuntar", pistola), ("LargaIdle", "LargaApuntar", larga) })
            {
                Unir(e[idle], e[apuntar], false, 0f, Mezcla, Bool(ReglasVisual.Apuntando, true));
                Unir(e[apuntar], e[idle], false, 0f, Mezcla, Bool(ReglasVisual.Apuntando, false));
                Unir(e[idle], e["Vacio"], false, 0f, Mezcla, Distinto(ReglasVisual.TipoArma, arma));
                Unir(e[apuntar], e["Vacio"], false, 0f, Mezcla, Distinto(ReglasVisual.TipoArma, arma));
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

            // Puños: izquierda, derecha o fuerte según GolpeTipo.
            string[] golpes = { "GolpeIzq", "GolpeDer", "GolpeFuerte" };
            for (int i = 0; i < golpes.Length; i++)
            {
                AnyA(m, e[golpes[i]], MezclaCorta, true, Disparador(ReglasVisual.Golpe), Entero(ReglasAnimacion.GolpeTipo, i));
                Unir(e[golpes[i]], e["Vacio"], true, SalidaUnaVez, Mezcla);
            }

            AnyA(m, e["Pitar"], MezclaCorta, true, Disparador(ReglasAnimacion.Gesto), Entero(ReglasAnimacion.GestoTipo, (int)GestoVisual.Pitar));
            Unir(e["Pitar"], e["Vacio"], true, SalidaUnaVez, Mezcla);

            // Daño de frente, de espalda o fuerte.
            string[] danos = { "DanoFrente", "DanoEspalda", "DanoFuerte" };
            for (int i = 0; i < danos.Length; i++)
            {
                AnyA(m, e[danos[i]], MezclaCorta, true, Disparador(ReglasVisual.Herido), Entero(ReglasAnimacion.DanoTipo, i));
                Unir(e[danos[i]], e["Vacio"], true, SalidaUnaVez, Mezcla);
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

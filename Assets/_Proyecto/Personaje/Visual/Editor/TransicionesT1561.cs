using System.Collections.Generic;
using System.Linq;
using System.Text;
using MechaCyberpunk.Personaje.Combate;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-1561 (decisión de Tony, 21-nov: "transiciones con lógica"): agrega al ControladorBase los estados y las transiciones que faltan, sin rearmarlo. Todo es repetible (idempotente).
    /// - Capa de brazos (Arriba): sacar y guardar el arma larga (PJ01_Rifle_Sacar y Guardar), granada de pie y agachado, salida de la guardia a 0.2 s, interrupción por daño
    ///   (trigger Interrumpir: golpe, recarga y granada vuelven a su reposo) y la recarga que sigue como RecargarCaminar si se echa a correr.
    /// - Capa base: beber, camilla, sentarse, colgarse y subir de colgado (gestos 15 a 20).
    /// Los parámetros nuevos: triggers Granada e Interrumpir, bools Sentado y Colgado.
    /// <see cref="Aplicar"/> hace todo (controlador, ConfigClipsNpcT1561 y el prefab del jugador con sacarAntesDeDisparar); <see cref="Diagnostico"/> lista lo que hay y mide los cuadros de la granada.
    /// Orden en Unity: 1) lo de T-1560 (importación de clips), 2) TransicionesT1561.Aplicar, 3) TransicionesT1561.Diagnostico.
    /// ConstructorControladorBase.Construir también llama a <see cref="AgregarEstados"/> al final, así un rearmado no los pierde.
    /// </summary>
    public static class TransicionesT1561
    {
        private const string Etiqueta = "[T-1561] ";
        private const float MezclaCorta = 0.1f;
        private const float Mezcla = 0.15f;
        private const float SalidaUnaVez = 0.92f;
        public const string RutaConfigNpc = "Assets/_Proyecto/Personaje/Datos/Resources/ConfigClipsNpcT1561.asset";
        public const string RutaPrefabJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";

        /// <summary>Gestos de la capa base: estado, clip, gesto, rapidez, y de qué manera termina (fracción de salida; bucle con su bool; o se levanta).</summary>
        private static readonly (string Estado, string Clip, GestoVisual Gesto, float Rapidez, float Salida, string BoolBucle)[] GestosBase =
        {
            // Beber: 178 cuadros, sale desde el 160 (0.9). A 2.2x dura 3.4 s (ReglaBloqueoAnimacion bloquea el movimiento solo eso).
            ("Beber", "PJ01_Beber", GestoVisual.Beber, 2.2f, 0.9f, null),
            // Camilla: 173 cuadros, termina acostado; al acabar se levanta con PJ01_Dano_Levantarse_BocaArriba.
            ("CamillaAcostarse", "PJ01_Camilla_Acostarse", GestoVisual.CamillaAcostarse, 2f, 0.98f, null),
            ("ColgadoSubir", "PJ01_Colgado_Subir", GestoVisual.ColgadoSubir, 1.5f, 0.92f, null),
            // En bucle mientras el bool siga prendido (VisualPersonaje.Sentado y .Colgado).
            ("Sentarse", "PJ01_Sentado", GestoVisual.Sentarse, 1f, 0f, ReglasTransicionesT1561.ParametroSentado),
            ("Colgarse", "PJ01_Colgado_Idle", GestoVisual.Colgarse, 1f, 0f, ReglasTransicionesT1561.ParametroColgado),
        };

        [MenuItem("Mecha Cyberpunk/T-1561/Aplicar todo")]
        public static void Aplicar()
        {
            var controlador = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);
            if (controlador == null)
            {
                Debug.LogError(Etiqueta + "falta " + ConstructorControladorBase.RutaControlador);
                return;
            }
            int tocados = AgregarEstados(controlador);
            EditorUtility.SetDirty(controlador);
            AssetDatabase.SaveAssets();
            bool config = CrearConfigNpc();
            bool prefab = PonerSacarAntesDeDisparar(true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(Etiqueta + "Aplicar listo: " + tocados + " estados nuevos o actualizados; ConfigClipsNpcT1561 " + (config ? "lista" : "NO se pudo crear")
                + "; prefab del jugador con sacarAntesDeDisparar " + (prefab ? "encendido" : "NO se pudo cambiar") + ".");
        }

        // ---------- Controlador ----------

        /// <summary>Agrega o actualiza los estados y transiciones de T-1561. Devuelve cuántos estados tocó (0 si falta algo en el controlador).</summary>
        public static int AgregarEstados(AnimatorController c)
        {
            AsegurarParametros(c);
            AnimatorStateMachine arriba = Capa(c, ReglasAnimacion.CapaArriba);
            AnimatorStateMachine baseM = Capa(c, ReglasAnimacion.CapaBase);
            if (arriba == null || baseM == null)
            {
                Debug.LogError(Etiqueta + "el ControladorBase no tiene las capas Base y Arriba: no se agregó nada");
                return 0;
            }
            int tocados = ArmaLargaSacarGuardar(arriba);
            tocados += Granada(arriba);
            tocados += GuardiaYGolpes(arriba);
            tocados += InterrupcionYRecarga(arriba);
            tocados += GestosDeLaBase(baseM);
            return tocados;
        }

        private static int ArmaLargaSacarGuardar(AnimatorStateMachine m)
        {
            AnimatorState vacio = Estado(m, "Vacio"), largaIdle = Estado(m, "LargaIdle"), largaApuntar = Estado(m, "LargaApuntar");
            if (vacio == null || largaIdle == null || largaApuntar == null)
            {
                Debug.LogError(Etiqueta + "falta Vacio, LargaIdle o LargaApuntar en la capa de brazos");
                return 0;
            }
            AnimatorState sacar = Preparar(m, "RifleSacar", ConstructorControladorBase.Clip("PJ01_Rifle_Sacar"));
            AnimatorState guardar = Preparar(m, "RifleGuardar", ConstructorControladorBase.Clip("PJ01_Rifle_Guardar"));
            sacar.speed = ReglasTransicionesT1561.RapidezSacarArmaLarga;
            guardar.speed = ReglasTransicionesT1561.RapidezSacarArmaLarga;
            int larga = (int)TipoArmaVisual.ArmaLarga;
            // El arma larga ya no aparece de golpe: se saca (Rifle_Sacar) y se guarda (Rifle_Guardar), como la pistola. Se quitan las uniones directas con Vacio.
            QuitarHacia(vacio, largaIdle);
            QuitarHacia(largaIdle, vacio);
            QuitarHacia(largaApuntar, vacio);
            // Repetir no duplica: se quitan también las que ya llegan a Sacar y Guardar.
            QuitarHacia(vacio, sacar);
            QuitarHacia(largaIdle, guardar);
            QuitarHacia(largaApuntar, guardar);
            Unir(vacio, sacar, false, 0f, MezclaCorta, Entero(ReglasVisual.TipoArma, larga));
            Unir(sacar, guardar, false, 0f, MezclaCorta, Distinto(ReglasVisual.TipoArma, larga));
            Unir(sacar, largaApuntar, true, ReglasTransicionesT1561.SalidaSacarArmaLarga, Mezcla, Bool(ReglasVisual.Apuntando, true));
            Unir(sacar, largaIdle, true, ReglasTransicionesT1561.SalidaSacarArmaLarga, Mezcla);
            Unir(largaIdle, guardar, false, 0f, MezclaCorta, Distinto(ReglasVisual.TipoArma, larga));
            Unir(largaApuntar, guardar, false, 0f, MezclaCorta, Distinto(ReglasVisual.TipoArma, larga));
            Unir(guardar, sacar, false, 0f, MezclaCorta, Entero(ReglasVisual.TipoArma, larga));
            Unir(guardar, vacio, true, ReglasTransicionesT1561.SalidaGuardarArmaLarga, Mezcla);
            return 2;
        }

        private static int Granada(AnimatorStateMachine m)
        {
            AnimatorState vacio = Estado(m, "Vacio"), pistolaIdle = Estado(m, "PistolaIdle");
            if (vacio == null || pistolaIdle == null)
            {
                Debug.LogError(Etiqueta + "falta Vacio o PistolaIdle en la capa de brazos");
                return 0;
            }
            int pistola = (int)TipoArmaVisual.Pistola;
            foreach (bool agachado in new[] { false, true })
            {
                AnimatorState s = Preparar(m, agachado ? "GranadaAgachado" : "GranadaDePie", ConstructorControladorBase.Clip(ReglasTransicionesT1561.ClipGranada(agachado)));
                s.speed = ReglasTransicionesT1561.RapidezGranada;
                // Desde cualquier estado de brazos: solo la postura (pie o agachado) decide el clip. La granada se suelta en el cuadro del lanzamiento (ArmasEspecialesAPie).
                AnyA(m, s, MezclaCorta, true, Disparador(ReglasAnimacion.Granada), Bool(ReglasAnimacion.Agachado, agachado));
                Unir(s, pistolaIdle, true, SalidaUnaVez, Mezcla, Entero(ReglasVisual.TipoArma, pistola));
                Unir(s, vacio, true, SalidaUnaVez, Mezcla);
            }
            return 2;
        }

        // Pelear -> correr: los brazos salen de la guardia en ~0.2 s. Correr -> pelear: la guardia sube en 0.15 s (antes del golpe, que espera 0.2 s: GolpeAPie).
        private static int GuardiaYGolpes(AnimatorStateMachine m)
        {
            AnimatorState vacio = Estado(m, "Vacio"), guardia = Estado(m, "PunosGuardia");
            if (vacio == null || guardia == null)
            {
                Debug.LogError(Etiqueta + "falta Vacio o PunosGuardia en la capa de brazos");
                return 0;
            }
            float salida = ReglasTransicionesT1561.SegundosSalidaGuardia;
            foreach (AnimatorStateTransition t in guardia.transitions.Where(x => x.destinationState == vacio))
            {
                t.duration = salida;
            }
            foreach (AnimatorStateTransition t in vacio.transitions.Where(x => x.destinationState == guardia))
            {
                t.duration = Mezcla;
            }
            foreach (string golpe in new[] { "GolpeIzq", "GolpeDer", "GolpeFuerte" })
            {
                AnimatorState g = Estado(m, golpe);
                if (g == null) { continue; }
                // Al terminar el golpe, con movimiento, los brazos bajan a normales en 0.2 s (la guardia ya no está permitida: VisualPersonaje.EnGuardia).
                foreach (AnimatorStateTransition t in g.transitions.Where(x => x.destinationState == vacio))
                {
                    t.duration = salida;
                }
            }
            return 0;
        }

        // Interrumpir: el daño corta golpe, recarga y lanzamiento y la capa vuelve al reposo que toque. La recarga de pistola sigue como RecargarCaminar si se echa a correr.
        private static int InterrupcionYRecarga(AnimatorStateMachine m)
        {
            AnimatorState vacio = Estado(m, "Vacio"), guardia = Estado(m, "PunosGuardia"), pistolaIdle = Estado(m, "PistolaIdle"), largaIdle = Estado(m, "LargaIdle");
            AnimatorState recargar = Estado(m, "PistolaRecargar"), recargarCaminar = Estado(m, "PistolaRecargarCaminar"), recargarLarga = Estado(m, "LargaRecargar");
            if (vacio == null || guardia == null || pistolaIdle == null || largaIdle == null)
            {
                Debug.LogError(Etiqueta + "faltan estados de reposo en la capa de brazos");
                return 0;
            }
            int pistola = (int)TipoArmaVisual.Pistola;
            foreach (string golpe in new[] { "GolpeIzq", "GolpeDer", "GolpeFuerte" })
            {
                AnimatorState g = Estado(m, golpe);
                if (g == null) { continue; }
                QuitarPorParametro(g, ReglasAnimacion.Interrumpir);
                Primero(g, g.AddTransition(vacio), false, 0f, MezclaCorta, Disparador(ReglasAnimacion.Interrumpir));
                Primero(g, g.AddTransition(guardia), false, 0f, MezclaCorta, Disparador(ReglasAnimacion.Interrumpir), Bool(ReglasAnimacion.EnGuardia, true));
            }
            foreach ((AnimatorState s, AnimatorState reposo) in new[] { (recargar, pistolaIdle), (recargarCaminar, pistolaIdle), (recargarLarga, largaIdle) })
            {
                if (s == null) { continue; }
                QuitarPorParametro(s, ReglasAnimacion.Interrumpir);
                Primero(s, s.AddTransition(reposo), false, 0f, MezclaCorta, Disparador(ReglasAnimacion.Interrumpir));
            }
            foreach (string nombre in new[] { "GranadaDePie", "GranadaAgachado" })
            {
                AnimatorState s = Estado(m, nombre);
                if (s == null) { continue; }
                QuitarPorParametro(s, ReglasAnimacion.Interrumpir);
                Primero(s, s.AddTransition(vacio), false, 0f, MezclaCorta, Disparador(ReglasAnimacion.Interrumpir));
                Primero(s, s.AddTransition(pistolaIdle), false, 0f, MezclaCorta, Disparador(ReglasAnimacion.Interrumpir), Entero(ReglasVisual.TipoArma, pistola));
            }
            if (recargar != null && recargarCaminar != null)
            {
                QuitarPorParametro(recargar, ReglasVisual.Velocidad);
                // Echó a correr con la recarga en curso: sigue con Pistola_RecargarCaminar (las armas largas cancelan la recarga en AnimacionJugador).
                Primero(recargar, recargar.AddTransition(recargarCaminar), false, 0f, Mezcla, Mayor(ReglasVisual.Velocidad, ReglasTransicionesT1561.VelocidadMaximaGuardia));
            }
            return 0;
        }

        private static int GestosDeLaBase(AnimatorStateMachine m)
        {
            AnimatorState idle = Estado(m, "Idle"), bocaArriba = Estado(m, "LevantarseBocaArriba");
            if (idle == null || bocaArriba == null)
            {
                Debug.LogError(Etiqueta + "falta Idle o LevantarseBocaArriba en la capa base");
                return 0;
            }
            int tocados = 0;
            foreach ((string nombre, string clip, GestoVisual gesto, float rapidez, float salida, string boolBucle) in GestosBase)
            {
                AnimatorState s = Preparar(m, nombre, ConstructorControladorBase.Clip(clip));
                s.speed = rapidez;
                s.tag = ReglasAnimacion.EtiquetaGesto;
                tocados++;
                // Desde cualquier estado (menos tirado): el gesto manda sobre la locomoción.
                AnyA(m, s, MezclaCorta, true, Disparador(ReglasAnimacion.Gesto), Entero(ReglasAnimacion.GestoTipo, (int)gesto), Bool(ReglasVisual.Caer, false));
                if (boolBucle != null)
                {
                    Unir(s, idle, false, 0f, Mezcla, Bool(boolBucle, false));
                }
                else if (nombre == "CamillaAcostarse")
                {
                    Unir(s, bocaArriba, true, salida, Mezcla);
                }
                else
                {
                    Unir(s, idle, true, salida, Mezcla);
                }
            }
            return tocados;
        }

        private static void AsegurarParametros(AnimatorController c)
        {
            foreach (string p in new[] { ReglasAnimacion.Granada, ReglasAnimacion.Interrumpir })
            {
                if (!c.parameters.Any(x => x.name == p))
                {
                    c.AddParameter(p, AnimatorControllerParameterType.Trigger);
                }
            }
            foreach (string p in new[] { ReglasTransicionesT1561.ParametroSentado, ReglasTransicionesT1561.ParametroColgado })
            {
                if (!c.parameters.Any(x => x.name == p))
                {
                    c.AddParameter(p, AnimatorControllerParameterType.Bool);
                }
            }
        }

        // ---------- Config de clips de NPC y prefab del jugador ----------

        private static AnimationClip CargarClip(string nombre)
        {
            string ruta = ConstructorControladorBase.CarpetaClipsPJ01 + "/" + nombre + ".fbx";
            return AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<AnimationClip>().FirstOrDefault(x => x.name == nombre && !x.name.StartsWith("__preview"));
        }

        /// <summary>Crea o actualiza Resources/ConfigClipsNpcT1561 con NPC_Herido_PedirAyuda, PJ01_Herido_Idle, NPC_LlamarPolicia y NPC_SacadoDelCarro_Huir.</summary>
        public static bool CrearConfigNpc()
        {
            var config = AssetDatabase.LoadAssetAtPath<ConfigClipsNpcT1561>(RutaConfigNpc);
            if (config == null)
            {
                string carpeta = System.IO.Path.GetDirectoryName(RutaConfigNpc).Replace('\\', '/');
                if (!AssetDatabase.IsValidFolder(carpeta))
                {
                    System.IO.Directory.CreateDirectory(carpeta);
                    AssetDatabase.Refresh();
                }
                config = ScriptableObject.CreateInstance<ConfigClipsNpcT1561>();
                AssetDatabase.CreateAsset(config, RutaConfigNpc);
            }
            AnimationClip ayuda = CargarClip("NPC_Herido_PedirAyuda"), herido = CargarClip("PJ01_Herido_Idle"), llamar = CargarClip("NPC_LlamarPolicia"), sacado = CargarClip("NPC_SacadoDelCarro_Huir");
            config.Configurar(ayuda, herido, llamar, sacado);
            EditorUtility.SetDirty(config);
            if (ayuda == null || herido == null || llamar == null || sacado == null)
            {
                Debug.LogWarning(Etiqueta + "ConfigClipsNpcT1561 con clips que faltan: ayuda " + (ayuda != null) + ", herido " + (herido != null) + ", llamar " + (llamar != null) + ", sacado " + (sacado != null));
            }
            return true;
        }

        /// <summary>Enciende o apaga PistolaAPie.sacarAntesDeDisparar en el prefab del jugador (con true, disparar con el arma guardada primero la saca y apunta).</summary>
        public static bool PonerSacarAntesDeDisparar(bool valor)
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefabJugador);
            try
            {
                var pistola = contenido.GetComponent<PistolaAPie>();
                if (pistola == null)
                {
                    Debug.LogError(Etiqueta + "el prefab del jugador no tiene PistolaAPie");
                    return false;
                }
                var so = new SerializedObject(pistola);
                SerializedProperty p = so.FindProperty("sacarAntesDeDisparar");
                if (p == null)
                {
                    Debug.LogError(Etiqueta + "PistolaAPie no tiene el campo sacarAntesDeDisparar");
                    return false;
                }
                p.boolValue = valor;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefabJugador);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        // ---------- Diagnóstico ----------

        [MenuItem("Mecha Cyberpunk/T-1561/Diagnóstico")]
        public static void Diagnostico()
        {
            var sb = new StringBuilder();
            var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);
            if (c == null)
            {
                Debug.LogError(Etiqueta + "falta " + ConstructorControladorBase.RutaControlador);
                return;
            }
            sb.AppendLine(Etiqueta + "Diagnóstico");
            foreach (string p in new[] { ReglasAnimacion.Granada, ReglasAnimacion.Interrumpir, ReglasTransicionesT1561.ParametroSentado, ReglasTransicionesT1561.ParametroColgado })
            {
                sb.AppendLine("  parámetro " + p + ": " + (c.parameters.Any(x => x.name == p) ? "ok" : "FALTA"));
            }
            AnimatorStateMachine arriba = Capa(c, ReglasAnimacion.CapaArriba), baseM = Capa(c, ReglasAnimacion.CapaBase);
            foreach (string n in new[] { "RifleSacar", "RifleGuardar", "GranadaDePie", "GranadaAgachado" })
            {
                sb.AppendLine("  " + DescribirEstado(arriba, n));
            }
            foreach (string n in new[] { "PunosGuardia", "GolpeIzq", "PistolaRecargar", "LargaRecargar", "Vacio" })
            {
                sb.AppendLine("  " + DescribirEstado(arriba, n));
            }
            foreach ((string estado, string _, GestoVisual _, float _, float _, string _) in GestosBase)
            {
                sb.AppendLine("  " + DescribirEstado(baseM, estado));
            }
            sb.AppendLine("  ConfigClipsNpcT1561: " + (AssetDatabase.LoadAssetAtPath<ConfigClipsNpcT1561>(RutaConfigNpc) != null ? "existe" : "FALTA (correr Aplicar)"));
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabJugador);
            PistolaAPie pistola = prefab != null ? prefab.GetComponent<PistolaAPie>() : null;
            sb.AppendLine("  prefab jugador sacarAntesDeDisparar: " + (pistola != null ? pistola.SacarAntesDeDisparar.ToString() : "sin PistolaAPie"));
            sb.AppendLine("  Granada de pie: " + MedirSuelta("PJ01_Granada_DePie", ReglasTransicionesT1561.CuadroSueltaDePie));
            sb.AppendLine("  Granada agachado: " + MedirSuelta("PJ01_Granada_Agachado", ReglasTransicionesT1561.CuadroSueltaAgachado));
            sb.AppendLine("  Lanzar (control, la bitácora dice ~cuadro 28): " + MedirSuelta("PJ01_Lanzar", 28));
            Debug.Log(sb.ToString());
        }

        private static string DescribirEstado(AnimatorStateMachine m, string nombre)
        {
            AnimatorState s = m != null ? Estado(m, nombre) : null;
            if (s == null)
            {
                return "estado " + nombre + ": FALTA";
            }
            int entran = m.anyStateTransitions.Count(t => t.destinationState == s);
            string clip = s.motion != null ? s.motion.name : "(sin clip)";
            return "estado " + nombre + ": clip " + clip + ", rapidez " + s.speed.ToString("F2") + ", " + s.transitions.Length + " salidas, " + entran + " entradas Any";
        }

        // Muestrea el clip sobre Visual_PJ01 cuadro a cuadro y busca el cuadro (1 = primero) en que la mano derecha va más rápido: ahí suelta la mano. Compara con el de las constantes.
        private static string MedirSuelta(string nombreClip, int cuadroEsperado)
        {
            AnimationClip clip = CargarClip(nombreClip);
            if (clip == null)
            {
                return "sin clip " + nombreClip;
            }
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab("PJ01"));
            if (modelo == null)
            {
                return "sin Visual_PJ01";
            }
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
            try
            {
                Animator a = instancia.GetComponentInChildren<Animator>(true);
                Transform mano = a != null && a.avatar != null && a.isHuman ? a.GetBoneTransform(HumanBodyBones.RightHand) : null;
                if (mano == null)
                {
                    return "sin mano derecha humanoide";
                }
                int cuadros = Mathf.RoundToInt(clip.length * clip.frameRate) + 1;
                Vector3 previo = Vector3.zero;
                float mejor = 0f;
                int cuadroMejor = 0;
                for (int i = 0; i < cuadros; i++)
                {
                    clip.SampleAnimation(a.gameObject, Mathf.Min(clip.length, i / clip.frameRate));
                    Vector3 p = mano.position;
                    if (i > 0)
                    {
                        float v = (p - previo).magnitude * clip.frameRate;
                        if (v > mejor)
                        {
                            mejor = v;
                            cuadroMejor = i + 1;
                        }
                    }
                    previo = p;
                }
                return clip.length.ToString("F2") + " s, " + cuadros + " cuadros; mano derecha más rápida en el cuadro " + cuadroMejor + " (" + mejor.ToString("F1") + " m/s); constante " + cuadroEsperado
                    + (Mathf.Abs(cuadroMejor - cuadroEsperado) <= 2 ? " (coincide)" : " (REVISAR la constante)");
            }
            finally
            {
                Object.DestroyImmediate(instancia);
            }
        }

        // ---------- Utilidades ----------

        private static AnimatorStateMachine Capa(AnimatorController c, string nombre)
        {
            foreach (AnimatorControllerLayer capa in c.layers)
            {
                if (capa.name == nombre)
                {
                    return capa.stateMachine;
                }
            }
            return null;
        }

        private static AnimatorState Estado(AnimatorStateMachine m, string nombre)
        {
            foreach (ChildAnimatorState hijo in m.states)
            {
                if (hijo.state.name == nombre)
                {
                    return hijo.state;
                }
            }
            return null;
        }

        // Crea el estado (o lo toma) y le quita sus transiciones de salida y las de Any State que llegan a él, para volver a armarlas: repetir no las duplica.
        private static AnimatorState Preparar(AnimatorStateMachine m, string nombre, Motion clip)
        {
            AnimatorState s = Estado(m, nombre) ?? m.AddState(nombre);
            s.motion = clip;
            foreach (AnimatorStateTransition t in s.transitions.ToArray())
            {
                s.RemoveTransition(t);
            }
            foreach (AnimatorStateTransition t in m.anyStateTransitions.Where(x => x.destinationState == s).ToArray())
            {
                m.RemoveAnyStateTransition(t);
            }
            return s;
        }

        private static void QuitarHacia(AnimatorState desde, AnimatorState hacia)
        {
            foreach (AnimatorStateTransition t in desde.transitions.Where(x => x.destinationState == hacia).ToArray())
            {
                desde.RemoveTransition(t);
            }
        }

        private static void QuitarPorParametro(AnimatorState desde, string parametro)
        {
            foreach (AnimatorStateTransition t in desde.transitions.Where(x => x.conditions.Any(k => k.parameter == parametro)).ToArray())
            {
                desde.RemoveTransition(t);
            }
        }

        private static void AnyA(AnimatorStateMachine m, AnimatorState destino, float mezcla, bool sobreSiMismo, params AnimatorCondition[] condiciones)
        {
            Configurar(m.AddAnyStateTransition(destino), false, 0f, mezcla, sobreSiMismo, condiciones);
        }

        private static void Unir(AnimatorState desde, AnimatorState hacia, bool salida, float tiempo, float mezcla, params AnimatorCondition[] condiciones)
        {
            Configurar(desde.AddTransition(hacia), salida, tiempo, mezcla, false, condiciones);
        }

        // La transición se crea al final de la lista del estado; se configura y se sube al primer lugar (gana la primera que se cumple).
        private static void Primero(AnimatorState desde, AnimatorStateTransition nueva, bool salida, float tiempo, float mezcla, params AnimatorCondition[] condiciones)
        {
            Configurar(nueva, salida, tiempo, mezcla, false, condiciones);
            var lista = new List<AnimatorStateTransition>(desde.transitions);
            lista.Remove(nueva);
            lista.Insert(0, nueva);
            desde.transitions = lista.ToArray();
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
        private static AnimatorCondition Bool(string p, bool v) => new AnimatorCondition { parameter = p, mode = v ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot };
        private static AnimatorCondition Entero(string p, int v) => new AnimatorCondition { parameter = p, mode = AnimatorConditionMode.Equals, threshold = v };
        private static AnimatorCondition Distinto(string p, int v) => new AnimatorCondition { parameter = p, mode = AnimatorConditionMode.NotEqual, threshold = v };
        private static AnimatorCondition Disparador(string p) => new AnimatorCondition { parameter = p, mode = AnimatorConditionMode.If };
    }
}

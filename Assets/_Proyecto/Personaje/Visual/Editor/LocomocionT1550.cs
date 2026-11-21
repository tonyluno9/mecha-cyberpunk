using System.Linq;
using System.Text;
using MechaCyberpunk.Arte.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-1550: locomoción y salto de PJ01 según la revisión con Tony del 20-nov (docs/tareas-unity-locomocion-20nov.md). Todo se puede repetir (idempotente).
    /// <see cref="Aplicar"/> hace, en este orden:
    /// 1) importa PJ01_CorrerRapido.fbx (Humanoid con el avatar de PJ01, toma CorrerRapido, cuadros 1 a 34, bucle, eventos AlPasoPJ01 en 0.424 y 0.909);
    /// 2) pone las velocidades del jugador en DatosPersonaje_Base (caminar 1.62, correr con Shift 4.53);
    /// 3) deja sin bucle los clips de aire del salto (PJ01_Salto_Aire y PJ01_Salto_Correr_Aire);
    /// 4) agrega al ControladorBase los estados, parámetros y transiciones del salto (AnimacionesT1540.AgregarEstados: agachada de 3 cuadros, salto corriendo si va a 3 m/s o más,
    ///    aterrizaje corriendo sin bloquear, rodada, aterrizaje sin input);
    /// 5) pone PJ01_CorrerRapido como el Loco_Correr del override del jugador (PJ01_Sprint deja de usarse; el archivo se queda).
    /// <see cref="Diagnostico"/> revisa todo eso y lista los problemas. Orden: Diagnostico (antes), Aplicar, Diagnostico (después).
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.LocomocionT1550.Aplicar y luego ...Diagnostico.
    /// </summary>
    public static class LocomocionT1550
    {
        private const string Etiqueta = "[T-1550] ";

        public const string NombreClipRapido = "PJ01_CorrerRapido";
        public const string TomaClipRapido = "CorrerRapido";
        public const string RutaClipRapido = ImportadorClipsPJ01.Carpeta + NombreClipRapido + ".fbx";

        /// <summary>Cuadros del clip (1 a 34, 30 fps).</summary>
        public const int PrimerCuadroRapido = 1, UltimoCuadroRapido = 34;

        /// <summary>Eventos de pisada del clip, en fracción del clip (0 a 1), como los de PJ01_Trote.fbx.meta: pie izquierdo (0) y derecho (1).</summary>
        public const float PisadaIzquierdaRapido = 0.424f, PisadaDerechaRapido = 0.909f;

        /// <summary>Velocidades del jugador a pie (m/s): WASD (≈1.6, la de PJ01_Caminar: 1.62) y Shift (≈4.5, la de PJ01_Trote: 4.53), iguales a las de los clips para que los pies no patinen.</summary>
        public const float VelocidadCaminarJugador = ReglasAnimacion.VelocidadPJ01Caminar, VelocidadCorrerJugador = ReglasAnimacion.VelocidadPJ01Trote;

        /// <summary>Clips de aire del salto: sin bucle (se quedan en la pose de bajada hasta tocar el piso).</summary>
        public static readonly string[] ClipsAireSinBucle = { "PJ01_Salto_Aire", "PJ01_Salto_Correr_Aire" };

        [MenuItem("Mecha Cyberpunk/T-1550/Aplicar todo")]
        public static void Aplicar()
        {
            AssetDatabase.Refresh();
            bool importado = ImportarCorrerRapido();
            PonerVelocidades();
            foreach (string clip in ClipsAireSinBucle)
            {
                QuitarBucle(clip);
            }
            var controlador = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);
            if (controlador == null)
            {
                Debug.LogError(Etiqueta + "falta " + ConstructorControladorBase.RutaControlador);
                return;
            }
            int umbrales = AjustarUmbralesCaminar(controlador);
            int estados = AnimacionesT1540.AgregarEstados(controlador);
            EditorUtility.SetDirty(controlador);
            AssetDatabase.SaveAssets();
            if (importado)
            {
                // Sin el clip importado el override quedaría vacío: solo se cambia si PJ01_CorrerRapido existe.
                ConstructorControladorBase.AplicarClipsDelJugador();
            }
            else
            {
                Debug.LogError(Etiqueta + "no se pudo importar " + RutaClipRapido + ": el override del jugador NO se cambió");
            }
            int rellenados = RellenarClavesSinClip(controlador);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(Etiqueta + "Aplicar listo: " + umbrales + " umbrales de Caminar ajustados, " + rellenados + " claves sin clip rellenadas en los overrides, " + estados + " estados del salto tocados en el ControladorBase; correr rapido importado=" + importado);
        }

        // ---------- Umbrales y claves sin clip ----------

        /// <summary>
        /// Los árboles 1D por Velocidad del estado Caminar (caminar -> trote) se armaron con 1.6 y 4.5: los pone en 1.62 y 4.53 (las velocidades de los clips) sin reconstruir el controlador.
        /// Devuelve cuántos umbrales cambió.
        /// </summary>
        public static int AjustarUmbralesCaminar(AnimatorController c)
        {
            int cambios = 0;
            AnimatorState caminar = c.layers[0].stateMachine.states.Select(h => h.state).FirstOrDefault(s => s.name == "Caminar");
            if (caminar != null && caminar.motion is BlendTree raiz)
            {
                RecorrerArbol(raiz, ref cambios);
            }
            if (cambios > 0) { EditorUtility.SetDirty(c); }
            return cambios;
        }

        private static void RecorrerArbol(BlendTree arbol, ref int cambios)
        {
            if (arbol.blendType == BlendTreeType.Simple1D && arbol.blendParameter == ReglasVisual.Velocidad && arbol.children.Length == 2)
            {
                ChildMotion[] hijos = arbol.children;
                int bajo = hijos[0].threshold <= hijos[1].threshold ? 0 : 1;
                int alto = 1 - bajo;
                if (!Mathf.Approximately(hijos[bajo].threshold, ReglasAnimacion.VelocidadPJ01Caminar) || !Mathf.Approximately(hijos[alto].threshold, ReglasAnimacion.VelocidadPJ01Trote))
                {
                    hijos[bajo].threshold = ReglasAnimacion.VelocidadPJ01Caminar;
                    hijos[alto].threshold = ReglasAnimacion.VelocidadPJ01Trote;
                    arbol.children = hijos;
                    cambios += 2;
                }
            }
            foreach (ChildMotion h in arbol.children)
            {
                if (h.motion is BlendTree hijo) { RecorrerArbol(hijo, ref cambios); }
            }
        }

        private static int UmbralesMalos(BlendTree arbol)
        {
            int malos = 0;
            if (arbol.blendType == BlendTreeType.Simple1D && arbol.blendParameter == ReglasVisual.Velocidad && arbol.children.Length == 2)
            {
                float min = Mathf.Min(arbol.children[0].threshold, arbol.children[1].threshold);
                float max = Mathf.Max(arbol.children[0].threshold, arbol.children[1].threshold);
                if (!Mathf.Approximately(min, ReglasAnimacion.VelocidadPJ01Caminar) || !Mathf.Approximately(max, ReglasAnimacion.VelocidadPJ01Trote)) { malos++; }
            }
            foreach (ChildMotion h in arbol.children)
            {
                if (h.motion is BlendTree hijo) { malos += UmbralesMalos(hijo); }
            }
            return malos;
        }

        /// <summary>
        /// Los estados nuevos (T-1540 y T-1550) dejan en cada override claves sin clip: se asigna a cada una el mismo clip base. Revisa los 9 papeles y los 7 extra. Devuelve cuántas rellenó.
        /// </summary>
        public static int RellenarClavesSinClip(AnimatorController baseCtrl)
        {
            int rellenadas = 0;
            foreach (var entrada in AnimacionesT1540.TablasDeOverrides())
            {
                var o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(entrada.Ruta);
                if (o == null) { continue; }
                o.runtimeAnimatorController = baseCtrl;
                var pares = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
                o.GetOverrides(pares);
                bool cambio = false;
                for (int i = 0; i < pares.Count; i++)
                {
                    if (pares[i].Key != null && pares[i].Value == null)
                    {
                        pares[i] = new System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>(pares[i].Key, pares[i].Key);
                        rellenadas++;
                        cambio = true;
                    }
                }
                if (cambio)
                {
                    o.ApplyOverrides(pares);
                    EditorUtility.SetDirty(o);
                }
            }
            return rellenadas;
        }

        // ---------- 1) Importar PJ01_CorrerRapido ----------

        /// <summary>
        /// Importa el FBX igual que los demás clips de PJ01 (ImportadorClipsPJ01: Humanoid, avatar y pose T de PJ01, escala 1.88 = Scale Factor de PJ01.fbx y de PJ01_Trote.fbx.meta)
        /// y le pone el clip PJ01_CorrerRapido: toma CorrerRapido, cuadros 1 a 34, bucle y los dos eventos de pisada. Devuelve false si falta el archivo.
        /// </summary>
        public static bool ImportarCorrerRapido()
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(RutaClipRapido) == null)
            {
                Debug.LogError(Etiqueta + "falta " + RutaClipRapido);
                return false;
            }
            AssetDatabase.ImportAsset(RutaClipRapido, ImportAssetOptions.ForceUpdate);
            var importador = AssetImporter.GetAtPath(RutaClipRapido) as ModelImporter;
            if (importador == null)
            {
                return false;
            }
            ImportadorClipsPJ01.ConfigurarModelo(importador);
            importador.SaveAndReimport();
            ImportadorClipsPJ01.CopiarDescripcion(importador);
            ImportadorClipsPJ01.ConfigurarClip(importador, RutaClipRapido);
            ModelImporterClipAnimation[] clips = importador.clipAnimations;
            if (clips.Length == 0)
            {
                Debug.LogError(Etiqueta + "el FBX no trae ninguna toma");
                return false;
            }
            ModelImporterClipAnimation clip = clips[0];
            if (clip.takeName != TomaClipRapido)
            {
                Debug.LogWarning(Etiqueta + "la toma del FBX se llama '" + clip.takeName + "', no '" + TomaClipRapido + "'");
            }
            clip.name = NombreClipRapido;
            clip.firstFrame = PrimerCuadroRapido;
            clip.lastFrame = UltimoCuadroRapido;
            clip.loopTime = true;
            clip.events = new[]
            {
                new AnimationEvent { functionName = ImportadorClipsPJ01.FuncionPasoPJ01, time = PisadaIzquierdaRapido, intParameter = ReglaPasosAnimados.PieIzquierdo },
                new AnimationEvent { functionName = ImportadorClipsPJ01.FuncionPasoPJ01, time = PisadaDerechaRapido, intParameter = ReglaPasosAnimados.PieDerecho },
            };
            importador.clipAnimations = new[] { clip };
            importador.SaveAndReimport();
            Debug.Log(Etiqueta + "importado " + RutaClipRapido + " (Humanoid, toma " + clip.takeName + ", cuadros " + PrimerCuadroRapido + "-" + UltimoCuadroRapido + ", bucle, 2 eventos de pisada)");
            return true;
        }

        // ---------- 2) Velocidades ----------

        /// <summary>Pone WASD en 1.62 m/s y Shift en 4.53 m/s en el DatosPersonaje del jugador.</summary>
        public static void PonerVelocidades()
        {
            Object datos = AssetDatabase.LoadAssetAtPath<Object>(AjustarSaltoT961.RutaDatosPersonaje);
            if (datos == null)
            {
                Debug.LogError(Etiqueta + "falta " + AjustarSaltoT961.RutaDatosPersonaje);
                return;
            }
            var so = new SerializedObject(datos);
            so.FindProperty("velocidadCaminar").floatValue = VelocidadCaminarJugador;
            so.FindProperty("velocidadCorrer").floatValue = VelocidadCorrerJugador;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssets();
        }

        // ---------- 3) Bucle de los clips de aire ----------

        /// <summary>Quita el bucle del clip (por nombre de archivo sin .fbx) si lo tiene; devuelve true si lo cambió.</summary>
        public static bool QuitarBucle(string nombreClip)
        {
            string ruta = ImportadorClipsPJ01.Carpeta + nombreClip + ".fbx";
            var importador = AssetImporter.GetAtPath(ruta) as ModelImporter;
            if (importador == null)
            {
                Debug.LogError(Etiqueta + "falta " + ruta);
                return false;
            }
            ModelImporterClipAnimation[] clips = importador.clipAnimations;
            if (clips.Length == 0 || !clips[0].loopTime)
            {
                return false;
            }
            clips[0].loopTime = false;
            importador.clipAnimations = clips;
            importador.SaveAndReimport();
            Debug.Log(Etiqueta + nombreClip + ": bucle apagado");
            return true;
        }

        // ---------- Diagnóstico ----------

        /// <summary>Revisa todo lo de esta tarea y escribe en el log, con el prefijo [T-1550], cada hallazgo; al final dice cuántos problemas hay (0 = todo bien).</summary>
        [MenuItem("Mecha Cyberpunk/T-1550/Diagnóstico")]
        public static void Diagnostico()
        {
            int problemas = ContarProblemas();
            Debug.Log(Etiqueta + "Diagnostico: " + problemas + " problema(s)");
        }

        /// <summary>Lo mismo que <see cref="Diagnostico"/>, devolviendo el número de problemas.</summary>
        public static int ContarProblemas()
        {
            int problemas = 0;
            var sb = new StringBuilder(Etiqueta + "revision\n");
            void Falla(string texto) { problemas++; sb.AppendLine("  PROBLEMA: " + texto); }
            void Ok(string texto) { sb.AppendLine("  ok: " + texto); }

            // 1) PJ01_CorrerRapido.
            var importador = AssetImporter.GetAtPath(RutaClipRapido) as ModelImporter;
            if (importador == null)
            {
                Falla("falta " + RutaClipRapido);
            }
            else
            {
                if (importador.animationType != ModelImporterAnimationType.Human) Falla("CorrerRapido no es Humanoid");
                if (!Mathf.Approximately(importador.globalScale, ImportadorClipsPJ01.EscalaClips)) Falla("escala " + importador.globalScale + " en vez de " + ImportadorClipsPJ01.EscalaClips);
                ModelImporterClipAnimation[] clips = importador.clipAnimations;
                if (clips.Length != 1 || clips[0].name != NombreClipRapido) Falla("el clip no se llama " + NombreClipRapido);
                else
                {
                    ModelImporterClipAnimation c = clips[0];
                    if (c.takeName != TomaClipRapido) Falla("toma " + c.takeName);
                    if (!Mathf.Approximately(c.firstFrame, PrimerCuadroRapido) || !Mathf.Approximately(c.lastFrame, UltimoCuadroRapido)) Falla("cuadros " + c.firstFrame + "-" + c.lastFrame);
                    if (!c.loopTime) Falla("sin bucle");
                    bool eventos = c.events != null && c.events.Length == 2
                        && c.events[0].functionName == ReglaPasosAnimados.FuncionPaso && Mathf.Approximately(c.events[0].time, PisadaIzquierdaRapido) && c.events[0].intParameter == ReglaPasosAnimados.PieIzquierdo
                        && c.events[1].functionName == ReglaPasosAnimados.FuncionPaso && Mathf.Approximately(c.events[1].time, PisadaDerechaRapido) && c.events[1].intParameter == ReglaPasosAnimados.PieDerecho;
                    if (!eventos) Falla("eventos AlPasoPJ01 distintos de 0.424 (pie 0) y 0.909 (pie 1)");
                    else Ok("CorrerRapido: Humanoid, toma " + c.takeName + ", " + c.firstFrame + "-" + c.lastFrame + ", bucle, eventos de pisada");
                }
                AnimationClip clip = ConstructorControladorBase.Clip(NombreClipRapido);
                if (clip == null) Falla("no hay AnimationClip " + NombreClipRapido);
                else Ok("clip " + clip.name + " " + clip.length.ToString("0.000") + " s, bucle=" + clip.isLooping + ", " + clip.events.Length + " eventos");
            }

            // 2) Velocidades.
            Object datos = AssetDatabase.LoadAssetAtPath<Object>(AjustarSaltoT961.RutaDatosPersonaje);
            if (datos == null) Falla("falta DatosPersonaje_Base");
            else
            {
                var so = new SerializedObject(datos);
                float caminar = so.FindProperty("velocidadCaminar").floatValue, correr = so.FindProperty("velocidadCorrer").floatValue;
                if (!Mathf.Approximately(caminar, VelocidadCaminarJugador) || !Mathf.Approximately(correr, VelocidadCorrerJugador)) Falla("DatosPersonaje_Base caminar " + caminar + " correr " + correr);
                else Ok("DatosPersonaje_Base: caminar " + caminar + ", correr " + correr);
                if (!Mathf.Approximately(so.FindProperty("alturaSalto").floatValue, AjustarSaltoT961.AlturaSalto)) Falla("alturaSalto distinta de " + AjustarSaltoT961.AlturaSalto);
            }
            var config = AssetDatabase.LoadAssetAtPath<Object>(AjustarSaltoT961.RutaConfigControles);
            if (config == null || !Mathf.Approximately(new SerializedObject(config).FindProperty("gravedad").floatValue, AjustarSaltoT961.Gravedad)) Falla("la gravedad no es " + AjustarSaltoT961.Gravedad);

            // 3) Clips de aire sin bucle.
            foreach (string nombre in ClipsAireSinBucle)
            {
                AnimationClip aire = ConstructorControladorBase.Clip(nombre);
                if (aire == null) Falla("no hay " + nombre);
                else if (aire.isLooping) Falla(nombre + " tiene bucle");
                else Ok(nombre + " sin bucle");
            }

            // 4) Controlador base.
            var c0 = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);
            if (c0 == null)
            {
                Falla("falta el ControladorBase");
            }
            else
            {
                problemas += RevisarControlador(c0, sb);
            }

            // Umbrales del arbol de Caminar y claves sin clip en todos los overrides.
            if (c0 != null)
            {
                AnimatorState cam = c0.layers[0].stateMachine.states.Select(h => h.state).FirstOrDefault(s => s.name == "Caminar");
                int malos = cam != null && cam.motion is BlendTree arbolCam ? UmbralesMalos(arbolCam) : 1;
                if (malos > 0) Falla(malos + " arbol(es) de Caminar con umbrales distintos de 1.62 y 4.53");
                else Ok("umbrales del arbol de Caminar 1.62 y 4.53");
                foreach (var entrada in AnimacionesT1540.TablasDeOverrides())
                {
                    var ov = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(entrada.Ruta);
                    if (ov == null) continue;
                    var ps = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
                    ov.GetOverrides(ps);
                    string[] vacias = ps.Where(p => p.Key != null && p.Value == null).Select(p => p.Key.name).ToArray();
                    if (vacias.Length > 0) Falla(System.IO.Path.GetFileNameWithoutExtension(entrada.Ruta) + ": claves sin clip " + string.Join(", ", vacias));
                }
            }

            // 5) Override del jugador.
            var o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ConstructorControladorBase.RutaOverride("Jugador"));
            if (o == null) Falla("falta el override del jugador");
            else
            {
                var pares = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
                o.GetOverrides(pares);
                AnimationClip correr = pares.Where(p => p.Key != null && p.Key.name == "Loco_Correr").Select(p => p.Value).FirstOrDefault();
                if (correr == null || correr.name != NombreClipRapido) Falla("Loco_Correr del jugador es " + (correr != null ? correr.name : "(vacío)") + ", no " + NombreClipRapido);
                else Ok("override del jugador: Loco_Correr -> " + correr.name);
                AnimationClip trote = pares.Where(p => p.Key != null && p.Key.name == ConstructorControladorBase.RolTrote).Select(p => p.Value).FirstOrDefault();
                AnimationClip caminarClip = pares.Where(p => p.Key != null && p.Key.name == "Loco_Caminar").Select(p => p.Value).FirstOrDefault();
                if (trote == null || trote.name != "PJ01_Trote" || caminarClip == null || caminarClip.name != "PJ01_Caminar") Falla("el jugador no mezcla PJ01_Caminar -> PJ01_Trote");
                else Ok("override del jugador: caminar PJ01_Caminar, trote PJ01_Trote");
                foreach (var par in pares)
                {
                    if (par.Value != null && par.Value.name == "PJ01_Sprint") Falla("el override del jugador todavía usa PJ01_Sprint en " + par.Key.name);
                }
            }
            Debug.Log(sb.ToString());
            return problemas;
        }

        private static int RevisarControlador(AnimatorController c, StringBuilder sb)
        {
            int problemas = 0;
            void Falla(string texto) { problemas++; sb.AppendLine("  PROBLEMA: " + texto); }
            void Ok(string texto) { sb.AppendLine("  ok: " + texto); }
            AnimatorStateMachine m = c.layers[0].stateMachine;
            AnimatorState Estado(string n) => m.states.Select(h => h.state).FirstOrDefault(s => s.name == n);
            bool Tiene(AnimatorState desde, AnimatorState hacia, params string[] parametros) =>
                desde != null && desde.transitions.Any(t => t.destinationState == hacia && parametros.All(p => t.conditions.Any(cond => cond.parameter == p)));

            AnimatorControllerParameter Param(string n) => c.parameters.FirstOrDefault(p => p.name == n);
            foreach (string p in new[] { ReglasAnimacion.RapidezSaltoInicio, ReglasAnimacion.RapidezSaltoAterrizaje })
            {
                AnimatorControllerParameter par = Param(p);
                if (par == null || par.type != AnimatorControllerParameterType.Float || !Mathf.Approximately(par.defaultFloat, 1f)) Falla("parámetro " + p + " ausente o con valor por defecto distinto de 1");
            }
            if (Param(ReglasAnimacion.PreparandoSalto) == null || Param(ReglasAnimacion.PreparandoSalto).type != AnimatorControllerParameterType.Bool) Falla("falta el parámetro bool " + ReglasAnimacion.PreparandoSalto);
            if (Param(ReglasAnimacion.AterrizajeRodada) == null || Param(ReglasAnimacion.AterrizajeRodada).type != AnimatorControllerParameterType.Trigger) Falla("falta el trigger " + ReglasAnimacion.AterrizajeRodada);

            AnimatorState inicio = Estado("SaltoInicio"), aire = Estado("SaltoAire"), aterriza = Estado("SaltoAterrizaje");
            AnimatorState cInicio = Estado("SaltoCorrerInicio"), cAire = Estado("SaltoCorrerAire"), cAterriza = Estado("SaltoCorrerAterrizaje"), rodada = Estado("SaltoCorrerRodada");
            AnimatorState caminar = Estado("Caminar"), correr = Estado("Correr");
            if (inicio == null || aire == null || aterriza == null || cInicio == null || cAire == null || cAterriza == null || rodada == null || caminar == null || correr == null)
            {
                Falla("faltan estados del salto o de la locomoción");
                return problemas;
            }
            // Velocidades de los estados: 1x (los clips del 20-nov vienen a velocidad real; el jugador compensa el 2.5x y el 2x con el parámetro).
            if (!inicio.speedParameterActive || inicio.speedParameter != ReglasAnimacion.RapidezSaltoInicio || !aterriza.speedParameterActive || aterriza.speedParameter != ReglasAnimacion.RapidezSaltoAterrizaje) Falla("SaltoInicio/SaltoAterrizaje sin parámetro de rapidez");
            else Ok("SaltoInicio x" + inicio.speed + " y SaltoAterrizaje x" + aterriza.speed + " con parámetro (el jugador los deja en " + ReglasAnimacion.AjusteSaltoJugador("SaltoInicio") + " y " + ReglasAnimacion.AjusteSaltoJugador("SaltoAterrizaje") + " = 1x)");
            if (!Mathf.Approximately(cInicio.speed, 1f) || !Mathf.Approximately(cAterriza.speed, 1f) || !Mathf.Approximately(rodada.speed, 1f)) Falla("los estados SaltoCorrer* no van a 1x");
            // Entradas al salto.
            if (!Tiene(caminar, cInicio, ReglasAnimacion.PreparandoSalto, ReglasVisual.Velocidad) || !Tiene(correr, cInicio, ReglasAnimacion.PreparandoSalto, ReglasVisual.Velocidad)) Falla("Caminar o Correr no entran al salto corriendo al pedir el salto");
            if (!Tiene(caminar, cInicio, ReglasVisual.EnSuelo, ReglasVisual.Velocidad) || !Tiene(correr, cInicio, ReglasVisual.EnSuelo, ReglasVisual.Velocidad)) Falla("Caminar o Correr no entran al salto corriendo en el aire");
            foreach (string nombre in new[] { "Idle", "Caminar", "Correr", "GirarDer", "GirarIzq" })
            {
                if (!Tiene(Estado(nombre), inicio, ReglasAnimacion.PreparandoSalto)) Falla(nombre + " no pasa a SaltoInicio al pedir el salto");
            }
            // SaltoAire sin ciclo: ninguna transición vuelve a SaltoAire desde sí mismo y su clip no tiene bucle (se revisó arriba).
            if (aire.transitions.Any(t => t.destinationState == aire)) Falla("SaltoAire se repite a sí mismo");
            // Aterrizajes.
            AnimatorStateTransition aterrizaPrep = inicio.transitions.FirstOrDefault(t => t.destinationState == aterriza && !t.hasExitTime);
            if (aterrizaPrep == null || !aterrizaPrep.conditions.Any(x => x.parameter == ReglasAnimacion.PreparandoSalto)) Falla("SaltoInicio aterriza aunque siga en la agachada (falta la condición PreparandoSalto)");
            foreach (AnimatorState enAire in new[] { cInicio, cAire })
            {
                if (!Tiene(enAire, cAterriza, ReglasVisual.EnSuelo, ReglasVisual.Velocidad)) Falla(enAire.name + " no aterriza corriendo con avance");
                if (!Tiene(enAire, aterriza, ReglasVisual.EnSuelo)) Falla(enAire.name + " no aterriza con PJ01_Salto_Aterrizaje sin input");
            }
            AnimatorStateTransition[] rodadaAny = m.anyStateTransitions.Where(t => t.destinationState == rodada).ToArray();
            if (rodadaAny.Length != 1 || !rodadaAny[0].conditions.Any(x => x.parameter == ReglasAnimacion.AterrizajeRodada)) Falla("la rodada no entra por el trigger " + ReglasAnimacion.AterrizajeRodada + " (una vez)");
            // Sin bloqueo: ni el aterrizaje corriendo ni la rodada bloquean el movimiento; el aterrizaje fuerte de tres puntos sí.
            foreach (string libre in new[] { "SaltoCorrerInicio", "SaltoCorrerAire", "SaltoCorrerAterrizaje", "SaltoCorrerRodada", "SaltoAterrizaje" })
            {
                if (Movimiento.ReglaBloqueoAnimacion.EstadoBloquea(libre)) Falla(libre + " bloquea el movimiento");
            }
            if (!Movimiento.ReglaBloqueoAnimacion.EstadoBloquea("AterrizajeFuerte")) Falla("AterrizajeFuerte ya no bloquea");
            if (problemas == 0) Ok("controlador: parámetros, entradas (parado y corriendo), aterrizajes, rodada y bloqueos");
            return problemas;
        }

    }
}

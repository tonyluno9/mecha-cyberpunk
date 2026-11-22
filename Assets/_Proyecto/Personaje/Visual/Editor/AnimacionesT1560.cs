using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MechaCyberpunk.Arte.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-1560 (pedido de Tony 21-nov, parte 1: "en Unity se ven deformadas; arréglalo"). Todo se puede repetir (idempotente) y no reconstruye el controlador base.
    /// Orden en Unity: <see cref="Diagnostico"/> (solo lee y escribe Logs/t1560-clips.txt), <see cref="Aplicar"/>, <see cref="Diagnostico"/> otra vez (debe dar 0 problemas).
    /// <see cref="Aplicar"/>: 1) avatar del E18 (solo si está roto); 2) importa cada clip PJ01_*, NPC_*, E15_*, E16_*, E18_* y E20_* según <see cref="TablaClipsT1560"/> (avatar de su dueño, escala, rango,
    /// raíz, bucle, sin compresión); 3) <see cref="AgregarEstados"/>; 4) overrides (golpes del jugador, muertes y daño lateral del NPC, burla y presumir de todos, caídas y heridos de robots, E20);
    /// 5) corrige el override de los prefabs Visual_* que no tengan el suyo. El postprocesador <see cref="ImportadorClipsT1560"/> reaplica la tabla en cada reimportación.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.AnimacionesT1560.Aplicar y luego ...Diagnostico.
    /// </summary>
    public static class AnimacionesT1560
    {
        private const string Etiqueta = "[T-1560] ";
        public const string RutaInformeClips = "Logs/t1560-clips.txt";
        public const string RutaInformeControladores = "Logs/t1560-controladores.txt";
        public const string RutaInformeAplicar = "Logs/t1560-aplicar.txt";
        private const float Mezcla = 0.12f;
        private const float MezclaCorta = 0.06f;
        private const float SalidaUnaVez = 0.88f;
        /// <summary>Compresión de animación de los clips: apagada (los clips Humanoid son pocas curvas y la compresión por defecto, 0.5, los deformaba).</summary>
        public const ModelImporterAnimationCompression Compresion = ModelImporterAnimationCompression.Off;
        /// <summary>Tolerancia (grados) con la que el esqueleto de un clip debe coincidir con la pose T de PJ01.</summary>
        private const float ToleranciaGrados = 0.5f;
        private const float ToleranciaMetros2 = 1e-6f;

        // ---------- Overrides (decisión de Tony 21-nov, BITACORA) ----------

        /// <summary>Todos los papeles: la burla de pandilleros (Skull_Burla) y presumir de Nova y jefes (Nova_Flexing) ya no son Punos_Guardia ni el Mixamo: son PJ01_Burla y PJ01_Presumir.</summary>
        public static (string Base, string Papel)[] BurlaYPresumirTodos => new[] { ("Skull_Burla", "PJ01_Burla"), ("Nova_Flexing", "PJ01_Presumir") };

        /// <summary>
        /// Jugador: golpe derecho PJ01_Golpe_Der, izquierdo PJ01_Golpe_Izq y fuerte PJ01_Golpe_Protesis (los tres estados GolpeDer, GolpeIzq y GolpeFuerte). La guardia (estado PunosGuardia) ya es PJ01_Punos_Guardia
        /// y el daño de frente y de espalda ya es PJ01_Dano_Frente y PJ01_Dano_Espalda. Descartados por Tony: PJ01_Golpe1_Directo, PJ01_Golpe2_Gancho y PJ01_Punos_Fuerte.
        /// </summary>
        public static (string Base, string Papel)[] GolpesJugadorT1560 => new[]
        {
            ("Punos_GolpeDer", "PJ01_Golpe_Der"), ("Punos_GolpeIzq", "PJ01_Golpe_Izq"), ("Punos_Fuerte", "PJ01_Golpe_Protesis"),
        };

        /// <summary>NPC (y sus tres variantes Mujer, Mayor y Celular): mismas muertes que el jugador (NPC_Muerte_* se descartan) y daño por izquierda y derecha propio.</summary>
        public static (string Base, string Papel)[] NpcT1560 => new[]
        {
            ("Muerte_Adelante", "PJ01_Muerte_Frente"), ("Muerte_Atras", "PJ01_Muerte_Espaldas"), ("Muerte_Desmayo", "PJ01_Muerte_Desmayo"),
            ("PJ01_Dano_Izquierda", "NPC_Dano_Izquierda"), ("PJ01_Dano_Derecha", "NPC_Dano_Derecha"),
        };

        /// <summary>Muertes de frente y de espaldas para todos los papeles (las del jugador aprobadas el 21-nov).</summary>
        public static (string Base, string Papel)[] MuertesTodos => new[]
        {
            ("Muerte_Adelante", "PJ01_Muerte_Frente"), ("Muerte_Atras", "PJ01_Muerte_Espaldas"),
        };

        /// <summary>Claves del controlador base cuyo clip fija <see cref="AplicarOverrides"/> con las tablas (las de arriba y las filas de robots y E20 de ClipsNpcNovaT1506.OverridesExtra).</summary>
        public static readonly string[] ClavesT1560 =
        {
            "Skull_Burla", "Nova_Flexing", "Punos_GolpeDer", "Punos_GolpeIzq", "Punos_Fuerte", "Muerte_Adelante", "Muerte_Atras", "Muerte_Desmayo",
            "PJ01_Dano_Izquierda", "PJ01_Dano_Derecha", "Loco_Caida", "Loco_SaltoAterrizaje", "PJ01_Herido_Idle", "E15_Cojear", "E18_Caida_Levantarse",
        };

        /// <summary>Lo llama ClipsNpcNovaT1506.AgregarATabla (desde ConstructorControladorBase.ConstruirClipsPorPapel): agrega las filas de T-1560 a los papeles, así un rearmado del controlador no las pierde.</summary>
        public static void AgregarATabla(Dictionary<string, (string Base, string Papel)[]> mapa)
        {
            foreach (string papel in mapa.Keys.ToArray())
            {
                mapa[papel] = ClipsNpcNovaT1506.Combinar(mapa[papel], BurlaYPresumirTodos);
                // Decisión de Tony 21-nov: todos mueren con las muertes aprobadas del jugador (PJ01_Muerte_Frente v2 y Espaldas), no las viejas.
                mapa[papel] = ClipsNpcNovaT1506.Combinar(mapa[papel], MuertesTodos);
            }
            if (mapa.ContainsKey("Jugador"))
            {
                mapa["Jugador"] = ClipsNpcNovaT1506.Combinar(mapa["Jugador"], GolpesJugadorT1560);
            }
            if (mapa.ContainsKey("NPC"))
            {
                mapa["NPC"] = ClipsNpcNovaT1506.Combinar(mapa["NPC"], NpcT1560);
            }
        }

        /// <summary>Visuales que no están en ClipsNpcNovaT1506.Visuales y su override (así los dejó T-1004/T-1506); <see cref="Diagnostico"/> avisa si cambia.</summary>
        public static readonly (string Visual, string Override)[] VisualesConOverrideFijo =
        {
            ("Visual_woman", "Jugador"), ("Visual_PJ01", "Jugador"), ("Visual_woman2", "Aliados"), ("Visual_AL10", "Aliados"), ("Visual_AL11", "Aliados"),
            ("Visual_man", "SKULLS"), ("Visual_SkullHombre", "SKULLS"), ("Visual_E19_PandilleroSkulls_a_2", "SKULLS"), ("Visual_E19_PandilleroSkulls_b", "SKULLS"),
            ("Visual_E10_PandilleroSnakes", "SNAKES"), ("Visual_E11_JefeSnakes", "Jefe"), ("Visual_E14_JefeFinalNova", "Jefe"), ("Visual_Inventor", "Profes"),
            ("Visual_RobotHombre", "Robots"), ("Visual_Hacker", "Hacker"),
        };

        /// <summary>
        /// Los clips del robo de carro y el parkour en ConfigAnimacionParkour se guardan por id interno del FBX: al rehacer la
        /// importación de un clip reemplazado (raíz PJ01_REV), la referencia queda vacía. Las vuelve a poner por nombre. Repetible.
        /// </summary>
        public static void ReasignarConfigParkour()
        {
            const string ruta = "Assets/_Proyecto/Personaje/Datos/Resources/ConfigAnimacionParkour.asset";
            var config = AssetDatabase.LoadAssetAtPath<ScriptableObject>(ruta);
            if (config == null)
            {
                Debug.LogError(Etiqueta + "falta " + ruta);
                return;
            }
            var so = new SerializedObject(config);
            int puestos = 0;
            foreach ((string campo, string clip) in new[] { ("carroSacarConductor", "PJ01_Carro_SacarConductor"), ("conductorSerSacado", "PJ01_Conductor_SerSacado") })
            {
                SerializedProperty prop = so.FindProperty(campo);
                AnimationClip c = ConstructorControladorBase.Clip(clip);
                if (prop != null && c != null && prop.objectReferenceValue != c)
                {
                    prop.objectReferenceValue = c;
                    puestos++;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log(Etiqueta + "ConfigAnimacionParkour: " + puestos + " clip(s) reasignados");
        }

        [MenuItem("Mecha Cyberpunk/T-1560/Aplicar todo")]
        public static void Aplicar()
        {
            var sb = new StringBuilder(Etiqueta + "Aplicar\n");
            AssetDatabase.Refresh();
            Paso(sb, "Avatar del E18 (solo si está roto)", () => sb.AppendLine("  " + RehacerAvatarE18SiHaceFalta()));
            Paso(sb, "Importar los clips según la tabla de decisiones", () => ImportarClips(sb));
            Paso(sb, "Estados nuevos del ControladorBase", () =>
            {
                var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);
                if (c == null)
                {
                    throw new FileNotFoundException("falta " + ConstructorControladorBase.RutaControlador);
                }
                int tocados = AgregarEstados(c);
                EditorUtility.SetDirty(c);
                AssetDatabase.SaveAssets();
                sb.AppendLine("  " + tocados + " estados nuevos o actualizados");
            });
            Paso(sb, "Overrides", () =>
            {
                var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);
                ClipsNpcNovaT1506.AplicarOverridesExtra(); // crea Override_E20 si falta y vuelve a leer los estados nuevos
                int cambios = AplicarOverrides(c);
                AssetDatabase.SaveAssets();
                sb.AppendLine("  " + cambios + " clips cambiados en los overrides");
            });
            Paso(sb, "Override de los prefabs Visual_*", () => sb.AppendLine("  " + CorregirPrefabs() + " prefabs corregidos"));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Directory.CreateDirectory("Logs");
            File.AppendAllText(RutaInformeControladores, sb + "\n");
            Debug.Log(sb.ToString());
        }

        private static void Paso(StringBuilder sb, string nombre, System.Action accion)
        {
            try
            {
                accion();
                sb.AppendLine("OK    " + nombre);
            }
            catch (System.Exception e)
            {
                sb.AppendLine("FALLO " + nombre + ": " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
                Debug.LogException(e);
            }
        }

        // ---------- Importación ----------

        /// <summary>
        /// Aplica al importador del clip la decisión de la tabla: sin compresión; los clips con rig propio y los que ya estaban aprobados: lo que corresponda; los del 21-nov: nombre, rango, bucle y raíz.
        /// Lo llama el postprocesador en cada importación y <see cref="ImportarClips"/>. Devuelve true si cambió algo.
        /// </summary>
        public static bool AjustarImportador(ModelImporter importador, string nombre)
        {
            DecisionClipT1560 d = TablaClipsT1560.Decidir(nombre);
            if (importador == null || d == null)
            {
                return false;
            }
            bool cambio = false;
            if (importador.animationCompression != Compresion)
            {
                importador.animationCompression = Compresion;
                cambio = true;
            }
            if (d.Nuevo && d.RigPropio)
            {
                ConfigurarRigPropio(importador, d);
                cambio = true;
            }
            ModelImporterClipAnimation[] clips = importador.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importador.defaultClipAnimations;
            }
            if (clips == null || clips.Length == 0)
            {
                return cambio; // todavía sin esqueleto: OnPostprocessAllAssets de ImportadorClipsPJ01 lo vuelve a importar
            }
            ModelImporterClipAnimation clip = clips[0];
            if (AjustarClip(clip, d))
            {
                cambio = true;
            }
            if (cambio)
            {
                importador.clipAnimations = new[] { clip };
            }
            return cambio;
        }

        /// <summary>Aplica a un clip (rango, bucle y raíz) lo que manda la tabla; solo los clips del 21-nov. Devuelve true si cambió algo.</summary>
        public static bool AjustarClip(ModelImporterClipAnimation clip, DecisionClipT1560 d)
        {
            bool cambio = false;
            if (clip.name != d.Nombre)
            {
                clip.name = d.Nombre;
                cambio = true;
            }
            if (!d.Nuevo)
            {
                return cambio;
            }
            if (d.Ultimo > 0)
            {
                if (!Mathf.Approximately(clip.firstFrame, 1f)) { clip.firstFrame = 1f; cambio = true; }
                if (!Mathf.Approximately(clip.lastFrame, d.Ultimo)) { clip.lastFrame = d.Ultimo; cambio = true; }
            }
            if (clip.loopTime != d.Bucle) { clip.loopTime = d.Bucle; cambio = true; }
            if (clip.loopPose != d.Bucle) { clip.loopPose = d.Bucle; cambio = true; }
            bool rot, altura, xz;
            switch (d.Raiz)
            {
                case RaizClipT1560.Sitio: rot = true; altura = true; xz = true; break;
                case RaizClipT1560.Avanza: rot = true; altura = true; xz = false; break;
                case RaizClipT1560.Sube: rot = true; altura = false; xz = true; break;
                case RaizClipT1560.SubeYAvanza: rot = true; altura = false; xz = false; break;
                case RaizClipT1560.Gira: rot = false; altura = true; xz = false; break;
                default: return cambio;
            }
            if (clip.lockRootRotation != rot) { clip.lockRootRotation = rot; cambio = true; }
            if (clip.lockRootHeightY != altura) { clip.lockRootHeightY = altura; cambio = true; }
            if (clip.lockRootPositionXZ != xz) { clip.lockRootPositionXZ = xz; cambio = true; }
            if (d.RigPropio)
            {
                // Como los clips de T-1553 que Tony aprobó: orientación del cuerpo (sin giro extra) y altura original.
                if (clip.keepOriginalOrientation) { clip.keepOriginalOrientation = false; cambio = true; }
                if (!Mathf.Approximately(clip.rotationOffset, 0f)) { clip.rotationOffset = 0f; cambio = true; }
                if (!clip.keepOriginalPositionY) { clip.keepOriginalPositionY = true; cambio = true; }
                if (clip.keepOriginalPositionXZ) { clip.keepOriginalPositionXZ = false; cambio = true; }
            }
            return cambio;
        }

        /// <summary>
        /// Clip con rig propio (E15, E16, E20): Humanoid con el avatar de su personaje (descripción del modelo sin los huesos que el clip no trae y la cabeza al cuello, ver ClipsRobotsNovaT1553.DescripcionSinCabeza)
        /// y la escala de ese modelo. Avatar propio del clip: con Copy From Other Avatar Unity no saca el clip (la jerarquía mixamorig del clip no coincide con la del modelo; T-1553).
        /// </summary>
        public static void ConfigurarRigPropio(ModelImporter importador, DecisionClipT1560 d)
        {
            var modelo = AssetImporter.GetAtPath(TablaClipsT1560.RutaModeloDe(d.Duenio)) as ModelImporter;
            importador.animationType = ModelImporterAnimationType.Human;
            importador.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importador.materialImportMode = ModelImporterMaterialImportMode.None;
            importador.importAnimation = true;
            importador.useFileScale = true;
            importador.importCameras = false;
            importador.importLights = false;
            importador.importBlendShapes = false;
            if (modelo != null)
            {
                importador.humanDescription = ClipsRobotsNovaT1553.DescripcionSinCabeza(importador.humanDescription, modelo, importador.assetPath);
                importador.globalScale = modelo.globalScale;
                importador.userData = modelo.userData;
            }
        }

        /// <summary>
        /// Importa todos los clips de la tabla que haya en disco. Los del 21-nov se reconfiguran del todo (Humanoid de PJ01 con su pose T copiada de PJ01.fbx, o el avatar de su personaje; rango, bucle y raíz).
        /// Los ya aprobados solo reciben compresión apagada y, si su esqueleto no coincide con la pose T de PJ01, la descripción de PJ01.
        /// </summary>
        public static void ImportarClips(StringBuilder sb)
        {
            int nuevos = 0, aprobados = 0, fallos = 0;
            foreach (string nombre in TablaClipsT1560.ClipsEnDisco())
            {
                DecisionClipT1560 d = TablaClipsT1560.Decidir(nombre);
                try
                {
                    if (ImportarUno(d))
                    {
                        if (d.Nuevo) { nuevos++; } else { aprobados++; }
                    }
                }
                catch (System.Exception e)
                {
                    fallos++;
                    sb.AppendLine("  FALLO " + nombre + ": " + e.Message);
                    Debug.LogException(e);
                }
            }
            sb.AppendLine("  clips del 21-nov reimportados: " + nuevos + "; aprobados ajustados: " + aprobados + "; fallos: " + fallos);
        }

        private static bool ImportarUno(DecisionClipT1560 d)
        {
            string ruta = TablaClipsT1560.RutaClip(d.Nombre);
            var importador = AssetImporter.GetAtPath(ruta) as ModelImporter;
            if (importador == null)
            {
                Debug.LogError(Etiqueta + "falta " + ruta);
                return false;
            }
            if (!d.Nuevo)
            {
                return RevisarAprobado(importador, d);
            }
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
            importador = AssetImporter.GetAtPath(ruta) as ModelImporter;
            if (d.RigPropio)
            {
                AjustarImportador(importador, d.Nombre);
                importador.SaveAndReimport();
                return true;
            }
            // Igual que ClipsNpcNovaT1506.ImportarHumanoide: Humanoid de PJ01, primera pasada para tener el esqueleto del clip, luego su descripción copiada de PJ01.fbx (pose T y mapeo).
            ImportadorClipsPJ01.ConfigurarModelo(importador);
            importador.SaveAndReimport();
            importador = AssetImporter.GetAtPath(ruta) as ModelImporter;
            ImportadorClipsPJ01.CopiarDescripcion(importador);
            ImportadorClipsPJ01.ConfigurarClip(importador, ruta);
            AjustarImportador(importador, d.Nombre);
            importador.SaveAndReimport();
            return true;
        }

        private static bool RevisarAprobado(ModelImporter importador, DecisionClipT1560 d)
        {
            bool cambio = false;
            if (importador.animationCompression != Compresion)
            {
                importador.animationCompression = Compresion;
                cambio = true;
            }
            if (!d.RigPropio && importador.animationType == ModelImporterAnimationType.Human && !DescripcionCoincideConPJ01(importador))
            {
                ImportadorClipsPJ01.CopiarDescripcion(importador);
                cambio = true;
            }
            if (cambio)
            {
                importador.SaveAndReimport();
            }
            return cambio;
        }

        /// <summary>
        /// True si el mapeo humanoide del clip es el de PJ01.fbx y los huesos que comparten tienen la misma pose T (la que copia ImportadorClipsPJ01.CopiarDescripcion). Si no, Unity retargetea con una
        /// pose de un cuadro cualquiera del clip y el cuerpo sale torcido. Devuelve true si no se puede comparar (falta PJ01.fbx).
        /// </summary>
        public static bool DescripcionCoincideConPJ01(ModelImporter clip)
        {
            var origen = AssetImporter.GetAtPath(ImportadorClipsPJ01.RutaModeloOrigen) as ModelImporter;
            if (origen == null || clip == null)
            {
                return true;
            }
            HumanDescription a = origen.humanDescription;
            HumanDescription b = clip.humanDescription;
            if (a.human == null || b.human == null || a.human.Length != b.human.Length || b.human.Length == 0)
            {
                return false;
            }
            var mapa = new Dictionary<string, string>();
            foreach (HumanBone h in a.human)
            {
                mapa[h.humanName] = h.boneName;
            }
            foreach (HumanBone h in b.human)
            {
                if (!mapa.TryGetValue(h.humanName, out string hueso) || hueso != h.boneName)
                {
                    return false;
                }
            }
            var pose = new Dictionary<string, SkeletonBone>();
            foreach (SkeletonBone s in a.skeleton)
            {
                pose[s.name] = s;
            }
            foreach (SkeletonBone s in b.skeleton)
            {
                if (pose.TryGetValue(s.name, out SkeletonBone o)
                    && (Quaternion.Angle(o.rotation, s.rotation) > ToleranciaGrados || (o.position - s.position).sqrMagnitude > ToleranciaMetros2))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Si el Avatar del E18 no es válido, lo rehace con RobotsNovaT1553.RehacerAvatarE18 (de MechaCyberpunk.Arte.Editor, que referencia a este ensamblado: se llama por reflexión).</summary>
        public static string RehacerAvatarE18SiHaceFalta()
        {
            if (ClipsRobotsNovaT1553.AvatarDelPersonaje("E18_Caminar") != null)
            {
                return "el Avatar del E18 es válido y humanoide: no se rehace";
            }
            System.Type tipo = System.Type.GetType("MechaCyberpunk.Arte.Editor.RobotsNovaT1553, MechaCyberpunk.Arte.Editor");
            MethodInfo metodo = tipo != null ? tipo.GetMethod("RehacerAvatarE18", BindingFlags.Public | BindingFlags.Static) : null;
            if (metodo == null)
            {
                return "el Avatar del E18 no es válido y no se encontró RobotsNovaT1553.RehacerAvatarE18: corre el menú Mecha Cyberpunk/T-1553/2 Aplicar robots Nova";
            }
            return (string)metodo.Invoke(null, null);
        }

        // ---------- Estados ----------

        /// <summary>
        /// Agrega (o actualiza) en la capa base los estados de <see cref="EstadosNpcT1560.Filas"/> y el parámetro entero AccionNpc. Las acciones sostenidas entran desde Idle, Caminar y los giros cuando
        /// AccionNpc vale su id y salen cuando cambia; los gestos entran desde cualquier estado con el trigger Gesto y GestoTipo = su id y vuelven a Idle. Devuelve cuántos estados tocó.
        /// También lo llama ConstructorControladorBase.Construir, así un rearmado no los pierde.
        /// </summary>
        public static int AgregarEstados(AnimatorController c)
        {
            AnimatorStateMachine m = c.layers[0].stateMachine;
            AsegurarParametros(c);
            AnimatorState idle = Estado(m, "Idle");
            AnimatorState caminar = Estado(m, "Caminar");
            AnimatorState correr = Estado(m, "Correr");
            if (idle == null || caminar == null || correr == null)
            {
                Debug.LogError(Etiqueta + "el ControladorBase no tiene Idle, Caminar o Correr: no se agregó nada");
                return 0;
            }
            float umbralCorrer = ReglasAnimacion.UmbralCorrer * ReglasAnimacion.HisteresisCorrer;
            int tocados = 0;
            foreach ((string estado, string clipBase, TipoEstadoT1560 tipo, int id, string disparadorIgnorado) in EstadosNpcT1560.Filas)
            {
                AnimationClip clip = ConstructorControladorBase.Clip(clipBase);
                AnimatorState s = Preparar(m, estado, clip);
                s.speed = 1f;
                tocados++;
                if (tipo == TipoEstadoT1560.Gesto)
                {
                    s.tag = ReglasAnimacion.EtiquetaGesto;
                    AnyA(m, s, MezclaCorta, true, Disparador(ReglasAnimacion.Gesto), Entero(ReglasAnimacion.GestoTipo, id), Bool(ReglasVisual.Caer, false));
                    if (estado == "NpcSacadoHuir")
                    {
                        // Termina corriendo (huye): sigue en Correr o Caminar según la velocidad.
                        Unir(s, correr, true, SalidaUnaVez, Mezcla, Mayor(ReglasVisual.Velocidad, umbralCorrer));
                        Unir(s, caminar, true, SalidaUnaVez, Mezcla, Mayor(ReglasVisual.Velocidad, ReglasVisual.UmbralCaminar));
                    }
                    Unir(s, idle, true, SalidaUnaVez, Mezcla);
                    continue;
                }
                s.tag = string.Empty;
                foreach (string origen in new[] { "Idle", "Caminar", "GirarDer", "GirarIzq" })
                {
                    AnimatorState desde = Estado(m, origen);
                    if (desde == null)
                    {
                        continue;
                    }
                    QuitarHacia(desde, s);
                    InsertarAlPrincipio(desde, desde.AddTransition(s), Mezcla, Entero(EstadosNpcT1560.AccionNpc, id), Bool(ReglasVisual.EnSuelo, true), Bool(ReglasVisual.Caer, false));
                }
                Unir(s, idle, false, 0f, Mezcla, Distinto(EstadosNpcT1560.AccionNpc, id));
            }
            return tocados;
        }

        private static void AsegurarParametros(AnimatorController c)
        {
            if (!c.parameters.Any(x => x.name == EstadosNpcT1560.AccionNpc))
            {
                c.AddParameter(EstadosNpcT1560.AccionNpc, AnimatorControllerParameterType.Int);
            }
        }

        private static void QuitarHacia(AnimatorState desde, AnimatorState hacia)
        {
            foreach (AnimatorStateTransition t in desde.transitions.Where(x => x.destinationState == hacia).ToArray())
            {
                desde.RemoveTransition(t);
            }
        }

        // La transición se crea al final de la lista del estado; se configura y se sube al primer lugar (gana la primera transición cierta).
        private static void InsertarAlPrincipio(AnimatorState desde, AnimatorStateTransition nueva, float mezcla, params AnimatorCondition[] condiciones)
        {
            Configurar(nueva, false, 0f, mezcla, false, condiciones);
            var lista = new List<AnimatorStateTransition>(desde.transitions);
            lista.Remove(nueva);
            lista.Insert(0, nueva);
            desde.transitions = lista.ToArray();
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

        // Crea el estado (o lo toma si ya existe) y le quita sus transiciones de salida y las de Any State para volver a armarlas: así repetir no las duplica.
        private static AnimatorState Preparar(AnimatorStateMachine m, string nombre, Motion clip)
        {
            AnimatorState s = Estado(m, nombre);
            if (s == null)
            {
                s = m.AddState(nombre);
            }
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

        private static void AnyA(AnimatorStateMachine m, AnimatorState destino, float mezcla, bool sobreSiMismo, params AnimatorCondition[] condiciones)
        {
            Configurar(m.AddAnyStateTransition(destino), false, 0f, mezcla, sobreSiMismo, condiciones);
        }

        private static void Unir(AnimatorState desde, AnimatorState hacia, bool salida, float tiempo, float mezcla, params AnimatorCondition[] condiciones)
        {
            Configurar(desde.AddTransition(hacia), salida, tiempo, mezcla, false, condiciones);
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

        // ---------- Overrides ----------

        /// <summary>
        /// Pone en cada override (los nueve papeles y los extra, incluido Override_E20) el clip que dice su tabla para las <see cref="ClavesT1560"/> y rellena toda clave vacía con su clip base (ningún estado
        /// queda sin clip). El resto de las claves no se toca. Devuelve cuántos clips cambió. Va después de <c>ClipsNpcNovaT1506.AplicarOverridesExtra</c>, que crea los extra que falten.
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
                o.runtimeAnimatorController = baseCtrl; // vuelve a leer los clips del controlador (ya trae los estados nuevos)
                var pares = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                o.GetOverrides(pares);
                for (int i = 0; i < pares.Count; i++)
                {
                    AnimationClip clave = pares[i].Key;
                    if (clave == null)
                    {
                        continue;
                    }
                    AnimationClip esperado = null;
                    if (ClavesT1560.Contains(clave.name))
                    {
                        foreach ((string b, string p) in tabla)
                        {
                            if (b == clave.name)
                            {
                                esperado = ConstructorControladorBase.Clip(p);
                            }
                        }
                    }
                    if (esperado == null)
                    {
                        esperado = pares[i].Value != null ? pares[i].Value : clave;
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

        // ---------- Prefabs Visual_* ----------

        /// <summary>El override que debe llevar el Animator de cada prefab Visual_*: las filas de ClipsNpcNovaT1506.Visuales con override y <see cref="VisualesConOverrideFijo"/>.</summary>
        public static List<(string Visual, string Override)> OverridesEsperadosDeVisuales()
        {
            var lista = new List<(string, string)>();
            foreach ((string visual, string nombreOverride, float velocidadCaminar, float velocidadCorrer) in ClipsNpcNovaT1506.Visuales)
            {
                if (nombreOverride != null)
                {
                    lista.Add(("Visual_" + visual, nombreOverride));
                }
            }
            foreach ((string visual, string nombreOverride) in VisualesConOverrideFijo)
            {
                if (!lista.Any(x => x.Item1 == visual))
                {
                    lista.Add((visual, nombreOverride));
                }
            }
            return lista;
        }

        public static string RutaDeOverride(string nombre) =>
            nombre == "NPC" || ConstructorControladorBase.Papeles.Contains(nombre) ? ConstructorControladorBase.RutaOverride(nombre) : ClipsNpcNovaT1506.RutaOverrideExtra(nombre);

        /// <summary>Pone el override esperado en el Animator de los prefabs Visual_* que llevan otro (solo esos: no se regraba el resto). Devuelve cuántos corrigió.</summary>
        public static int CorregirPrefabs()
        {
            int corregidos = 0;
            foreach ((string visual, string nombreOverride) in OverridesEsperadosDeVisuales())
            {
                string ruta = CreadorVisualPersonaje.RutaPrefab(visual.StartsWith("Visual_") ? visual.Substring("Visual_".Length) : visual);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                var esperado = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(RutaDeOverride(nombreOverride));
                if (prefab == null || esperado == null)
                {
                    continue;
                }
                Animator animador = prefab.GetComponentInChildren<Animator>(true);
                if (animador == null || animador.runtimeAnimatorController == esperado)
                {
                    continue;
                }
                GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
                try
                {
                    Animator a = contenido.GetComponentInChildren<Animator>(true);
                    a.runtimeAnimatorController = esperado;
                    PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
                    corregidos++;
                    Debug.Log(Etiqueta + visual + ": override " + nombreOverride);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contenido);
                }
            }
            return corregidos;
        }

        // ---------- Diagnóstico ----------

        /// <summary>
        /// Escribe Logs/t1560-clips.txt (una fila por clip, columnas separadas por tabulador, la última es PROBLEMA) y Logs/t1560-controladores.txt (estados, overrides y prefabs). Devuelve el número de
        /// problemas (0 = todo bien) y lo imprime con el prefijo [T-1560].
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T-1560/Diagnóstico")]
        public static int Diagnostico()
        {
            Directory.CreateDirectory("Logs");
            var usos = UsosDeClips();
            var filas = new StringBuilder();
            filas.AppendLine(string.Join("\t", new[]
            {
                "Clip", "Dueño", "Rig del FBX", "Tipo", "Avatar", "Escala", "Escala del modelo", "Toma del FBX (cuadros)", "Rango en .meta", "Rango esperado", "Raiz rot/Y/XZ", "Original or/Y/XZ",
                "Giro extra", "Bucle", "Compresion", "Errores de importación", "Estados y overrides que lo usan", "PROBLEMA",
            }));
            int problemas = 0;
            int filasConProblema = 0;
            foreach (string nombre in TablaClipsT1560.ClipsEnDisco())
            {
                string fila = FilaDeClip(nombre, usos, out string problema);
                filas.AppendLine(fila);
                if (problema.Length > 0)
                {
                    problemas++;
                    filasConProblema++;
                }
            }
            File.WriteAllText(RutaInformeClips, filas.ToString());
            int problemasControladores = DiagnosticoControladores();
            Debug.Log(Etiqueta + "Diagnostico: " + filasConProblema + " clip(s) con problema (ver " + RutaInformeClips + "), " + problemasControladores + " problema(s) de controladores y prefabs (ver "
                + RutaInformeControladores + ")");
            return problemas + problemasControladores;
        }

        private static string FilaDeClip(string nombre, Dictionary<string, List<string>> usos, out string problema)
        {
            DecisionClipT1560 d = TablaClipsT1560.Decidir(nombre);
            string ruta = TablaClipsT1560.RutaClip(nombre);
            var p = new StringBuilder();
            var importador = AssetImporter.GetAtPath(ruta) as ModelImporter;
            if (importador == null)
            {
                problema = "SIN IMPORTADOR (el FBX no está importado)";
                return nombre + "\t" + d.Duenio + "\t-\t-\t-\t-\t-\t-\t-\t-\t-\t-\t-\t-\t-\t-\t-\t" + problema;
            }
            string rigArchivo = LeerRig(ruta);
            ModelImporterClipAnimation[] clips = importador.clipAnimations;
            ModelImporterClipAnimation clip = clips != null && clips.Length > 0 ? clips[0] : null;
            ModelImporterClipAnimation[] porDefecto = importador.defaultClipAnimations;
            ModelImporterClipAnimation toma = porDefecto != null && porDefecto.Length > 0 ? porDefecto[0] : null;

            float escalaModelo = d.RigPropio ? EscalaDelModelo(d.Duenio) : TablaClipsT1560.EscalaPJ01;
            string avatar;
            if (importador.avatarSetup == ModelImporterAvatarSetup.CopyFromOther)
            {
                avatar = "copia de " + (importador.sourceAvatar != null ? importador.sourceAvatar.name : "(sin avatar)");
            }
            else if (d.RigPropio)
            {
                int huesos = importador.humanDescription.human != null ? importador.humanDescription.human.Length : 0;
                avatar = "descripción de " + d.Duenio + " (" + huesos + " huesos)";
            }
            else
            {
                avatar = DescripcionCoincideConPJ01(importador) ? "descripción de PJ01 (pose T copiada)" : "propia del clip (pose de un cuadro: NO es la de PJ01)";
            }
            if (importador.animationType != ModelImporterAnimationType.Human)
            {
                p.Append("no es Humanoid; ");
            }
            if (!d.RigPropio && !DescripcionCoincideConPJ01(importador))
            {
                p.Append("el avatar no es el de PJ01; ");
            }
            if (d.RigPropio && importador.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel && (importador.humanDescription.human == null || importador.humanDescription.human.Length < 15))
            {
                p.Append("el avatar del personaje no tiene los huesos humanoides; ");
            }
            if (!Mathf.Approximately(importador.globalScale, escalaModelo))
            {
                p.Append("escala " + importador.globalScale.ToString("0.#####") + ", la de su dueño es " + escalaModelo.ToString("0.#####") + "; ");
            }
            if (clips == null || clips.Length != 1 || clip == null || clip.name != nombre)
            {
                p.Append("no tiene un solo clip con el nombre del archivo; ");
            }
            string rango = "-", esperado = "-", raiz = "-", original = "-", giro = "-", bucle = "-";
            if (clip != null)
            {
                rango = clip.firstFrame.ToString("0") + "-" + clip.lastFrame.ToString("0");
                raiz = Si(clip.lockRootRotation) + "/" + Si(clip.lockRootHeightY) + "/" + Si(clip.lockRootPositionXZ);
                original = Si(clip.keepOriginalOrientation) + "/" + Si(clip.keepOriginalPositionY) + "/" + Si(clip.keepOriginalPositionXZ);
                giro = clip.rotationOffset.ToString("0");
                bucle = Si(clip.loopTime);
                if (d.Nuevo && d.Ultimo > 0)
                {
                    esperado = "1-" + d.Ultimo;
                    if (!Mathf.Approximately(clip.firstFrame, 1f) || !Mathf.Approximately(clip.lastFrame, d.Ultimo))
                    {
                        p.Append("rango " + rango + ", esperado " + esperado + "; ");
                    }
                }
                else if (toma != null)
                {
                    esperado = "toma completa";
                }
                if (d.Nuevo)
                {
                    if (clip.loopTime != d.Bucle)
                    {
                        p.Append("bucle " + bucle + ", esperado " + Si(d.Bucle) + "; ");
                    }
                    string raizEsperada = RaizEsperada(d.Raiz);
                    if (raizEsperada != null && raiz != raizEsperada)
                    {
                        p.Append("raíz " + raiz + " (" + d.Raiz + "), esperada " + raizEsperada + "; ");
                    }
                }
            }
            string tomaTexto = "-";
            if (toma != null)
            {
                int cuadros = Mathf.RoundToInt(toma.lastFrame - toma.firstFrame) + 1;
                tomaTexto = toma.firstFrame.ToString("0") + "-" + toma.lastFrame.ToString("0") + " (" + cuadros + ")";
                if (d.Nuevo && d.Ultimo > 0 && cuadros != d.Ultimo)
                {
                    p.Append("el FBX trae " + cuadros + " cuadros y la bitácora dice " + d.Ultimo + "; ");
                }
            }
            if (importador.animationCompression != Compresion)
            {
                p.Append("compresión " + importador.animationCompression + "; ");
            }
            string errores = LeerErrores(importador).Replace("\n", " ").Replace("\t", " ").Trim();
            if (errores.Length > 0)
            {
                p.Append("errores de importación; ");
            }
            if (d.RigPropio != (rigArchivo == "propio") && rigArchivo != "?")
            {
                p.Append("la tabla dice rig " + (d.RigPropio ? "propio" : "PJ01") + " y el FBX trae " + rigArchivo + "; ");
            }
            string uso = usos.TryGetValue(nombre, out List<string> lista) ? string.Join(", ", lista) : "SIN USO";
            problema = p.ToString().Trim();
            return string.Join("\t", new[]
            {
                nombre, d.Duenio, rigArchivo, importador.animationType.ToString(), avatar, importador.globalScale.ToString("0.#####"), escalaModelo.ToString("0.#####"), tomaTexto, rango, esperado,
                raiz, original, giro, bucle, importador.animationCompression + " r" + importador.animationRotationError.ToString("0.##") + "/p" + importador.animationPositionError.ToString("0.##"),
                errores.Length > 60 ? errores.Substring(0, 60) + "..." : errores, uso, problema,
            });
        }

        private static string Si(bool v) => v ? "1" : "0";

        /// <summary>Los errores de importación de animación que Unity guarda en el .meta (animationImportErrors); la API pública de ModelImporter no los expone, se leen por SerializedObject.</summary>
        public static string LeerErrores(ModelImporter importador)
        {
            var so = new SerializedObject(importador);
            SerializedProperty it = so.GetIterator();
            while (it.Next(true))
            {
                if (it.propertyType == SerializedPropertyType.String && it.name.ToLowerInvariant().Contains("animationimporterrors"))
                {
                    return it.stringValue ?? string.Empty;
                }
            }
            return string.Empty;
        }

        private static string RaizEsperada(RaizClipT1560 raiz)
        {
            switch (raiz)
            {
                case RaizClipT1560.Sitio: return "1/1/1";
                case RaizClipT1560.Avanza: return "1/1/0";
                case RaizClipT1560.Sube: return "1/0/1";
                case RaizClipT1560.SubeYAvanza: return "1/0/0";
                case RaizClipT1560.Gira: return "0/1/0";
                default: return null;
            }
        }

        /// <summary>La escala (globalScale) del modelo del dueño (E15, E16, E18, E20: la del modelo _a); la de PJ01 para los demás.</summary>
        public static float EscalaDelModelo(string duenio)
        {
            var modelo = AssetImporter.GetAtPath(TablaClipsT1560.RutaModeloDe(duenio)) as ModelImporter;
            return modelo != null ? modelo.globalScale : TablaClipsT1560.EscalaPJ01;
        }

        /// <summary>"propio" si el FBX trae huesos mixamorig (el rig del personaje), "PJ01" si trae la armadura BoneRoot, "?" si no se pudo leer (puntero de Git LFS sin descargar).</summary>
        public static string LeerRig(string ruta)
        {
            if (!File.Exists(ruta))
            {
                return "?";
            }
            var info = new FileInfo(ruta);
            if (info.Length < 4096)
            {
                return "?";
            }
            string texto = Encoding.GetEncoding(28591).GetString(File.ReadAllBytes(ruta));
            if (texto.Contains("mixamorig:Hips"))
            {
                return "propio";
            }
            return texto.Contains("BoneRoot") ? "PJ01" : "?";
        }

        // Estados y overrides que usan cada clip (por nombre).
        private static Dictionary<string, List<string>> UsosDeClips()
        {
            var usos = new Dictionary<string, List<string>>();
            void Anotar(string clip, string donde)
            {
                if (!usos.TryGetValue(clip, out List<string> lista))
                {
                    lista = new List<string>();
                    usos[clip] = lista;
                }
                if (!lista.Contains(donde))
                {
                    lista.Add(donde);
                }
            }
            var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);
            if (c != null)
            {
                foreach (AnimatorControllerLayer capa in c.layers)
                {
                    RecorrerMaquina(capa.stateMachine, (estado, clip) => Anotar(clip.name, "base:" + capa.name + "/" + estado));
                }
            }
            foreach ((string ruta, (string Base, string Papel)[] tablaIgnorada) in AnimacionesT1540.TablasDeOverrides())
            {
                var o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ruta);
                if (o == null)
                {
                    continue;
                }
                string papel = Path.GetFileNameWithoutExtension(ruta).Replace("Override_", string.Empty);
                var pares = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                o.GetOverrides(pares);
                foreach (KeyValuePair<AnimationClip, AnimationClip> par in pares)
                {
                    AnimationClip efectivo = par.Value != null ? par.Value : par.Key;
                    if (efectivo != null && par.Key != null && efectivo != par.Key)
                    {
                        Anotar(efectivo.name, "override:" + papel);
                    }
                }
            }
            return usos;
        }

        private static void RecorrerMaquina(AnimatorStateMachine m, System.Action<string, AnimationClip> alClip)
        {
            foreach (ChildAnimatorState s in m.states)
            {
                RecorrerMotion(s.state.name, s.state.motion, alClip);
            }
            foreach (ChildAnimatorStateMachine hija in m.stateMachines)
            {
                RecorrerMaquina(hija.stateMachine, alClip);
            }
        }

        private static void RecorrerMotion(string estado, Motion motion, System.Action<string, AnimationClip> alClip)
        {
            if (motion is BlendTree arbol)
            {
                foreach (ChildMotion h in arbol.children)
                {
                    RecorrerMotion(estado, h.motion, alClip);
                }
            }
            else if (motion is AnimationClip clip)
            {
                alClip(estado, clip);
            }
        }

        /// <summary>Revisa los estados nuevos, las tablas de override (jugador, NPC, burla, robots, E20), que ninguna clave quede vacía y el override de cada prefab. Escribe Logs/t1560-controladores.txt.</summary>
        private static int DiagnosticoControladores()
        {
            var sb = new StringBuilder(Etiqueta + "controladores, overrides y prefabs\n");
            int problemas = 0;
            var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);
            if (c == null)
            {
                sb.AppendLine("PROBLEMA falta el ControladorBase");
                File.WriteAllText(RutaInformeControladores, sb.ToString());
                return 1;
            }
            AnimatorStateMachine m = c.layers[0].stateMachine;
            if (!c.parameters.Any(x => x.name == EstadosNpcT1560.AccionNpc && x.type == AnimatorControllerParameterType.Int))
            {
                sb.AppendLine("PROBLEMA falta el parámetro entero " + EstadosNpcT1560.AccionNpc);
                problemas++;
            }
            sb.AppendLine("Estados nuevos (estado -> clip, entradas, salidas, disparador):");
            foreach ((string estado, string clipBase, TipoEstadoT1560 tipo, int id, string disparador) in EstadosNpcT1560.Filas)
            {
                AnimatorState s = Estado(m, estado);
                bool existe = s != null;
                bool clipOk = existe && s.motion != null && s.motion.name == clipBase;
                int entradas = 0;
                if (existe)
                {
                    entradas = tipo == TipoEstadoT1560.Gesto
                        ? m.anyStateTransitions.Count(t => t.destinationState == s)
                        : m.states.Sum(h => h.state.transitions.Count(t => t.destinationState == s));
                }
                int salidas = existe ? s.transitions.Length : 0;
                sb.AppendLine("  " + estado + " -> " + (existe && s.motion != null ? s.motion.name : "(sin clip)") + ", " + tipo + " " + id + ", entradas=" + entradas + ", salidas=" + salidas + ", " + disparador);
                if (!existe || !clipOk || entradas < 1 || salidas < 1)
                {
                    sb.AppendLine("  PROBLEMA con el estado " + estado);
                    problemas++;
                }
            }
            foreach (AnimatorControllerLayer capa in c.layers)
            {
                foreach (ChildAnimatorState s in capa.stateMachine.states)
                {
                    if (s.state.name != "Vacio" && s.state.motion == null)
                    {
                        sb.AppendLine("  PROBLEMA estado sin clip en el ControladorBase: " + capa.name + "/" + s.state.name);
                        problemas++;
                    }
                }
            }
            sb.AppendLine("Overrides:");
            foreach ((string ruta, (string Base, string Papel)[] tabla) in AnimacionesT1540.TablasDeOverrides())
            {
                var o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ruta);
                string nombre = Path.GetFileNameWithoutExtension(ruta);
                if (o == null)
                {
                    sb.AppendLine("  PROBLEMA falta " + ruta);
                    problemas++;
                    continue;
                }
                var pares = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                o.GetOverrides(pares);
                int vacios = pares.Count(x => x.Key != null && x.Value == null);
                if (vacios > 0)
                {
                    sb.AppendLine("  PROBLEMA " + nombre + ": " + vacios + " clave(s) sin clip (corre Aplicar para rellenarlas con su clip base)");
                    problemas++;
                }
                if (o.runtimeAnimatorController != c)
                {
                    sb.AppendLine("  PROBLEMA " + nombre + " no apunta al ControladorBase");
                    problemas++;
                }
                var mapa = new Dictionary<string, string>();
                foreach (KeyValuePair<AnimationClip, AnimationClip> par in pares)
                {
                    if (par.Key != null) { mapa[par.Key.name] = par.Value != null ? par.Value.name : par.Key.name; }
                }
                foreach ((string clave, string esperado) in tabla.Where(t => ClavesT1560.Contains(t.Base)))
                {
                    if (mapa.TryGetValue(clave, out string real) && real != esperado)
                    {
                        sb.AppendLine("  PROBLEMA " + nombre + ": " + clave + " -> " + real + ", esperado " + esperado);
                        problemas++;
                    }
                }
                sb.AppendLine("  " + nombre + ": " + string.Join(", ", ClavesT1560.Where(mapa.ContainsKey).Select(k => k + "=>" + mapa[k])));
            }
            sb.AppendLine("Prefabs Visual_* (override esperado):");
            foreach ((string visual, string nombreOverride) in OverridesEsperadosDeVisuales())
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab(visual.Substring("Visual_".Length)));
                Animator a = prefab != null ? prefab.GetComponentInChildren<Animator>(true) : null;
                string real = a != null && a.runtimeAnimatorController != null ? a.runtimeAnimatorController.name : "(ninguno)";
                string esperado = "Override_" + nombreOverride;
                sb.AppendLine("  " + visual + " -> " + real + (real == esperado ? string.Empty : "   PROBLEMA, esperado " + esperado));
                if (prefab != null && real != esperado)
                {
                    problemas++;
                }
            }
            sb.AppendLine("Prefabs de IA (controlador del Animator):");
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Proyecto/IA/Prefabs" }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                Animator a = prefab != null ? prefab.GetComponentInChildren<Animator>(true) : null;
                if (a == null)
                {
                    continue;
                }
                string real = a.runtimeAnimatorController != null ? a.runtimeAnimatorController.name : "(ninguno)";
                string archivo = Path.GetFileNameWithoutExtension(ruta);
                string esperado = null;
                if (archivo.Contains("E15_")) { esperado = "Override_E15"; }
                else if (archivo.Contains("E16_")) { esperado = "Override_E16"; }
                else if (archivo.Contains("E18_")) { esperado = "Override_E18"; }
                bool mal = esperado != null && real != esperado;
                sb.AppendLine("  " + archivo + " -> " + real + (mal ? "   PROBLEMA, esperado " + esperado : string.Empty));
                if (mal)
                {
                    problemas++;
                }
            }
            sb.AppendLine("Avatar del E18: " + (ClipsRobotsNovaT1553.AvatarDelPersonaje("E18_Caminar") != null ? "válido y humanoide" : "PROBLEMA, inválido (corre Aplicar)"));
            if (ClipsRobotsNovaT1553.AvatarDelPersonaje("E18_Caminar") == null)
            {
                problemas++;
            }
            sb.AppendLine(Etiqueta + problemas + " problema(s)");
            File.WriteAllText(RutaInformeControladores, sb.ToString());
            return problemas;
        }
    }
}

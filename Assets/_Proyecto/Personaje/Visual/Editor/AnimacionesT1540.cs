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
    /// T-1540: mete a los controladores las animaciones de Blender que estaban sin usar. Todo se puede repetir (idempotente) y no reconstruye el controlador base:
    /// 1) <see cref="ImportarClips"/> importa como Humanoid de PJ01 los seis borradores del 21-nov (PJ01_Muerte_Explosion, Muerte_Atropello, Muerte_CaidaAltura, Derribo_Explosion, Dano_Empujon,
    ///    Dano_ExplosionCercana; los FBX ya están copiados en Assets/Arte/Personajes/PJ01/Clips);
    /// 2) <see cref="AgregarEstados"/> agrega al ControladorBase los estados y parámetros nuevos (también lo llama ConstructorControladorBase.Construir, así un rearmado no los pierde);
    /// 3) <see cref="AplicarOverrides"/> pone en los 16 overrides el daño PJ01_Dano_* y la burla original de Skull (la tabla ClipsNoJugadorT1540) y los clips de robots de E15 y E16.
    /// <see cref="Aplicar"/> hace los tres en ese orden. <see cref="Diagnostico"/> lista estado -> clip efectivo de cada controlador y revisa lo de esta tarea.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.AnimacionesT1540.Aplicar y luego ...Diagnostico.
    /// </summary>
    public static class AnimacionesT1540
    {
        private const string Etiqueta = "[T-1540] ";
        private const float Mezcla = 0.12f;
        private const float MezclaCorta = 0.06f;
        /// <summary>T-1550: mezcla (s) al entrar a la agachada del salto: corta, para no comerse los 3 cuadros (0.1 s) antes del despegue.</summary>
        private const float MezclaPreparar = 0.03f;
        /// <summary>T-1550: estados de piso desde los que se pasa a la agachada del salto al pedirlo.</summary>
        private static readonly string[] EstadosDePiso =
        {
            "Idle", "Caminar", "Correr", "GirarDer", "GirarIzq", "GirarApuntarDer", "GirarApuntarIzq", "Frenar", "EscalonesSubir", "EscalonesBajar",
        };
        private const float SalidaUnaVez = 0.88f;

        /// <summary>Los clips que se importan en el paso 1 (nombre de archivo sin .fbx).</summary>
        public static readonly string[] ClipsNuevos =
        {
            "PJ01_Muerte_Explosion", "PJ01_Muerte_Atropello", "PJ01_Muerte_CaidaAltura", "PJ01_Derribo_Explosion", "PJ01_Dano_Empujon", "PJ01_Dano_ExplosionCercana",
        };

        /// <summary>Claves del controlador base cuyo clip cambia en los overrides en esta tarea.</summary>
        public static readonly string[] ClavesOverride = { "Pistola_Dano_Frente", "Pistola_Dano_Espalda", "Skull_Burla" };

        [MenuItem("Mecha Cyberpunk/T-1540/Aplicar todo")]
        public static void Aplicar()
        {
            ImportarClips();
            var controlador = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);
            if (controlador == null)
            {
                Debug.LogError(Etiqueta + "falta " + ConstructorControladorBase.RutaControlador);
                return;
            }
            int estados = AgregarEstados(controlador);
            EditorUtility.SetDirty(controlador);
            AssetDatabase.SaveAssets();
            int cambios = AplicarOverrides(controlador);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(Etiqueta + "Aplicar listo: " + estados + " estados nuevos o actualizados en el ControladorBase, " + cambios + " clips cambiados en los overrides.");
        }

        // ---------- Paso 1: importar ----------

        /// <summary>Importa Humanoid (avatar y escala de PJ01) los seis clips nuevos; los que no estén en la carpeta se avisan.</summary>
        public static int ImportarClips()
        {
            AssetDatabase.Refresh();
            int importados = 0;
            foreach (string nombre in ClipsNuevos)
            {
                string ruta = ImportadorClipsPJ01.Carpeta + nombre + ".fbx";
                if (AssetDatabase.LoadAssetAtPath<Object>(ruta) == null)
                {
                    Debug.LogError(Etiqueta + "falta " + ruta);
                    continue;
                }
                AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
                var importador = AssetImporter.GetAtPath(ruta) as ModelImporter;
                if (importador == null)
                {
                    continue;
                }
                ImportadorClipsPJ01.ConfigurarModelo(importador);
                importador.SaveAndReimport();
                ImportadorClipsPJ01.CopiarDescripcion(importador);
                ImportadorClipsPJ01.ConfigurarClip(importador, ruta);
                importador.SaveAndReimport();
                importados++;
            }
            Debug.Log(Etiqueta + "clips importados: " + importados + " de " + ClipsNuevos.Length);
            return importados;
        }

        // ---------- Paso 2: estados y parámetros ----------

        /// <summary>
        /// Agrega (o actualiza) en la capa base: las muertes por explosión, atropello y caída (MuerteTipo 3, 4 y 5), el derribo, el empujón y la explosión cercana (gestos 14, 12 y 13),
        /// el salto corriendo (4 estados), y los parámetros RapidezSaltoInicio y RapidezSaltoAterrizaje que multiplican la rapidez de SaltoInicio y SaltoAterrizaje. Devuelve cuántos estados tocó.
        /// </summary>
        public static int AgregarEstados(AnimatorController c)
        {
            AnimatorStateMachine m = c.layers[0].stateMachine;
            AsegurarParametros(c);
            int tocados = 0;
            AnimatorState idle = Estado(m, "Idle");
            AnimatorState bocaArriba = Estado(m, "LevantarseBocaArriba");
            AnimatorState bocaAbajo = Estado(m, "LevantarseBocaAbajo");
            if (idle == null || bocaArriba == null || bocaAbajo == null)
            {
                Debug.LogError(Etiqueta + "el ControladorBase no tiene Idle o Levantarse: no se agregó nada");
                return 0;
            }

            // Muertes (desde cualquier estado, igual que las tres de siempre) y derribo/empujón/tambaleo (gesto: trigger Gesto + GestoTipo, y no caído).
            foreach ((string nombre, string clip, int gesto) in ReglasAnimacion.EstadosT1540)
            {
                AnimatorState s = Preparar(m, nombre, ConstructorControladorBase.Clip(clip));
                s.speed = ReglasAnimacion.VelocidadEstado(nombre);
                tocados++;
                if (gesto >= 0)
                {
                    s.tag = ReglasAnimacion.EtiquetaGesto;
                    AnyA(m, s, MezclaCorta, true, Disparador(ReglasAnimacion.Gesto), Entero(ReglasAnimacion.GestoTipo, gesto), Bool(ReglasVisual.Caer, false));
                    if (nombre == "DerriboExplosion")
                    {
                        // Cae boca arriba y se levanta con PJ01_Dano_Levantarse_BocaArriba (que ya mueve la cápsula con RaizCaidaJugador).
                        Unir(s, bocaArriba, true, SalidaUnaVez, Mezcla);
                    }
                    else
                    {
                        Unir(s, idle, true, SalidaUnaVez, Mezcla);
                    }
                    continue;
                }
                int tipo = nombre == "MuerteExplosion" ? (int)MuerteVisual.Explosion : nombre == "MuerteAtropello" ? (int)MuerteVisual.Atropello : (int)MuerteVisual.CaidaAltura;
                AnyA(m, s, Mezcla, false, Bool(ReglasVisual.Caer, true), Entero(ReglasAnimacion.MuerteTipo, tipo));
                // Levantarse: la caída de altura es boca abajo; explosión y atropello, boca arriba.
                Unir(s, tipo == (int)MuerteVisual.CaidaAltura ? bocaAbajo : bocaArriba, false, 0f, Mezcla, Bool(ReglasVisual.Caer, false));
            }

            // Salto corriendo.
            AnimatorState inicio = null, aire = null, aterriza = null, rodada = null;
            foreach ((string nombre, string clip) in ReglasAnimacion.EstadosSaltoCorrerT1540)
            {
                AnimatorState s = Preparar(m, nombre, ConstructorControladorBase.Clip(clip));
                s.speed = 1f;
                tocados++;
                if (nombre == "SaltoCorrerInicio") inicio = s;
                else if (nombre == "SaltoCorrerAire") aire = s;
                else if (nombre == "SaltoCorrerAterrizaje") aterriza = s;
                else rodada = s;
            }
            AnimatorState caer = Estado(m, "Caida");
            AnimatorState correr = Estado(m, "Correr");
            AnimatorState caminar = Estado(m, "Caminar");
            AnimatorState saltoInicio = Estado(m, "SaltoInicio");
            AnimatorState saltoAterriza = Estado(m, "SaltoAterrizaje");
            float umbralCorrer = ReglasAnimacion.UmbralCorrer * ReglasAnimacion.HisteresisCorrer;
            float umbralCaminar = ReglasVisual.UmbralCaminar;
            float caidaLibre = -ReglasAnimacion.VelocidadCaidaLibre;
            // T-1550: corriendo es ir a ~3 m/s o mas hacia adelante (el trote de Shift, 4.5, vive en el estado Caminar: ya no hace falta pasar de Correr).
            float rapidezSalto = ReglasAnimacion.VelocidadSaltoCorriendo;
            float adelanteSalto = ReglasAnimacion.AdelanteMinimoSaltoCorriendo;
            // El impulso del salto sale 0.1 s despues de pedirlo (PreparandoSalto): ningun estado del salto aterriza mientras siga pidiendolo.
            AnimatorCondition prepara = Bool(ReglasAnimacion.PreparandoSalto, true), noPrepara = Bool(ReglasAnimacion.PreparandoSalto, false);
            Unir(inicio, caer, false, 0f, Mezcla, Bool(ReglasVisual.EnSuelo, false), Menor(ReglasAnimacion.VelY, caidaLibre));
            Unir(inicio, aire, true, 0.8f, Mezcla);
            Unir(aire, caer, false, 0f, Mezcla, Menor(ReglasAnimacion.VelY, caidaLibre));
            Unir(aterriza, caer, false, 0f, Mezcla, Bool(ReglasVisual.EnSuelo, false), Menor(ReglasAnimacion.VelY, caidaLibre));
            // Al tocar el piso: corriendo sigue el aterrizaje corriendo (sin bloquear); sin avance (sin input) es PJ01_Salto_Aterrizaje.
            foreach (AnimatorState enAire in new[] { inicio, aire })
            {
                Unir(enAire, aterriza, false, 0f, MezclaCorta, Bool(ReglasVisual.EnSuelo, true), noPrepara, Mayor(ReglasVisual.Velocidad, rapidezSalto));
                Unir(enAire, saltoAterriza, false, 0f, MezclaCorta, Bool(ReglasVisual.EnSuelo, true), noPrepara);
            }
            foreach (AnimatorState fin in new[] { aterriza, rodada })
            {
                // Termina corriendo (los clips acaban en la zancada: el trote de Shift es la mezcla de Caminar); si ya paro, camina o se queda quieto.
                Unir(fin, correr, true, SalidaUnaVez, Mezcla, Mayor(ReglasVisual.Velocidad, umbralCorrer));
                Unir(fin, caminar, true, SalidaUnaVez, Mezcla, Mayor(ReglasVisual.Velocidad, umbralCaminar));
                Unir(fin, idle, true, SalidaUnaVez, Mezcla);
            }
            // Saltar otra vez al caer (sin esperar a que acabe el aterrizaje): vuelve al despegue que toque.
            Unir(aterriza, inicio, false, 0f, MezclaPreparar, prepara, Mayor(ReglasVisual.Velocidad, rapidezSalto), Mayor(ReglasAnimacion.VelZ, adelanteSalto));
            Unir(aterriza, saltoInicio, false, 0f, MezclaPreparar, prepara);

            // SaltoInicio (parado): no aterriza mientras el salto sigue en la agachada (en el piso, PreparandoSalto).
            foreach (AnimatorStateTransition t in saltoInicio.transitions.Where(x => x.destinationState == saltoAterriza && !x.hasExitTime))
            {
                if (!t.conditions.Any(c => c.parameter == ReglasAnimacion.PreparandoSalto))
                {
                    t.AddCondition(noPrepara.mode, 0f, noPrepara.parameter);
                }
            }
            // Saltar parado otra vez al caer: SaltoAterrizaje -> SaltoInicio.
            QuitarHacia(saltoAterriza, saltoInicio);
            Unir(saltoAterriza, saltoInicio, false, 0f, MezclaPreparar, prepara);
            // Todo estado de piso pasa a la agachada del salto al pedirlo (antes esperaba a dejar el piso). Parado: SaltoInicio; corriendo (solo Caminar y Correr): SaltoCorrerInicio.
            foreach (string nombre in EstadosDePiso)
            {
                AnimatorState origen = Estado(m, nombre);
                if (origen == null) { continue; }
                QuitarHacia(origen, saltoInicio, ReglasAnimacion.PreparandoSalto);
                InsertarAlPrincipio(origen, origen.AddTransition(saltoInicio), MezclaPreparar, prepara, Bool(ReglasVisual.EnSuelo, true));
            }
            foreach (AnimatorState origen in new[] { caminar, correr })
            {
                QuitarHacia(origen, inicio);
                // En el aire (sin caida libre) y corriendo (p. ej. al salir de una cornisa corriendo), y al pedir el salto en el piso: salto corriendo. Va antes que SaltoInicio (gana la primera transicion cierta).
                InsertarAlPrincipio(origen, origen.AddTransition(inicio), MezclaCorta, Bool(ReglasVisual.EnSuelo, false), Mayor(ReglasAnimacion.VelY, caidaLibre),
                    Mayor(ReglasVisual.Velocidad, rapidezSalto), Mayor(ReglasAnimacion.VelZ, adelanteSalto));
                InsertarAlPrincipio(origen, origen.AddTransition(inicio), MezclaPreparar, prepara, Bool(ReglasVisual.EnSuelo, true),
                    Mayor(ReglasVisual.Velocidad, rapidezSalto), Mayor(ReglasAnimacion.VelZ, adelanteSalto));
            }
            // Caer corriendo desde alto = la rodada (trigger AterrizajeRodada de VisualPersonaje; ya decidio con TipoAterrizaje). Va antes que AterrizajeFuerte: es una transicion Any State.
            AnimatorStateTransition rod = m.AddAnyStateTransition(rodada);
            Configurar(rod, false, 0f, MezclaCorta, false, Disparador(ReglasAnimacion.AterrizajeRodada), Bool(ReglasVisual.EnSuelo, true), Bool(ReglasVisual.Caer, false));
            PonerPrimeroAny(m, rod);

            // La rapidez de SaltoInicio y SaltoAterrizaje se multiplica por un parámetro (1 = como siempre; el jugador lo baja).
            foreach ((string nombre, string parametro) in new[] { ("SaltoInicio", ReglasAnimacion.RapidezSaltoInicio), ("SaltoAterrizaje", ReglasAnimacion.RapidezSaltoAterrizaje) })
            {
                AnimatorState s = Estado(m, nombre);
                if (s != null)
                {
                    s.speedParameterActive = true;
                    s.speedParameter = parametro;
                    tocados++;
                }
            }
            return tocados;
        }

        private static void AsegurarParametros(AnimatorController c)
        {
            foreach (string p in new[] { ReglasAnimacion.RapidezSaltoInicio, ReglasAnimacion.RapidezSaltoAterrizaje })
            {
                if (!c.parameters.Any(x => x.name == p))
                {
                    c.AddParameter(new AnimatorControllerParameter { name = p, type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
                }
            }
            // T-1550: la agachada del salto (bool) y el trigger de la rodada.
            if (!c.parameters.Any(x => x.name == ReglasAnimacion.PreparandoSalto))
            {
                c.AddParameter(ReglasAnimacion.PreparandoSalto, AnimatorControllerParameterType.Bool);
            }
            if (!c.parameters.Any(x => x.name == ReglasAnimacion.AterrizajeRodada))
            {
                c.AddParameter(ReglasAnimacion.AterrizajeRodada, AnimatorControllerParameterType.Trigger);
            }
            // Tras un rearmado completo los dos parametros de rapidez quedan en 0 (AddParameter sin valor): se dejan en 1, que es "como siempre".
            AnimatorControllerParameter[] lista = c.parameters;
            bool cambio = false;
            foreach (AnimatorControllerParameter p in lista)
            {
                if ((p.name == ReglasAnimacion.RapidezSaltoInicio || p.name == ReglasAnimacion.RapidezSaltoAterrizaje) && !Mathf.Approximately(p.defaultFloat, 1f))
                {
                    p.defaultFloat = 1f;
                    cambio = true;
                }
            }
            if (cambio)
            {
                c.parameters = lista;
            }
        }

        // Quita las transiciones de "desde" a "hacia" (todas, o solo las que tienen una condicion sobre el parametro dado): repetir no las duplica.
        private static void QuitarHacia(AnimatorState desde, AnimatorState hacia, string parametro = null)
        {
            foreach (AnimatorStateTransition t in desde.transitions.Where(x => x.destinationState == hacia && (parametro == null || x.conditions.Any(c => c.parameter == parametro))).ToArray())
            {
                desde.RemoveTransition(t);
            }
        }

        // La transicion se crea al final de la lista del estado; se configura y se sube al primer lugar (la ultima que se sube queda primera).
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

        // Crea el estado (o lo toma si ya existe) y le quita sus transiciones para volver a armarlas: así repetir no las duplica.
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

        private static void PonerPrimeroAny(AnimatorStateMachine m, AnimatorStateTransition nueva)
        {
            var lista = new List<AnimatorStateTransition>(m.anyStateTransitions);
            lista.Remove(nueva);
            lista.Insert(0, nueva);
            m.anyStateTransitions = lista.ToArray();
        }

        private static AnimatorCondition Mayor(string p, float v) => new AnimatorCondition { parameter = p, mode = AnimatorConditionMode.Greater, threshold = v };
        private static AnimatorCondition Menor(string p, float v) => new AnimatorCondition { parameter = p, mode = AnimatorConditionMode.Less, threshold = v };
        private static AnimatorCondition Bool(string p, bool v) => new AnimatorCondition { parameter = p, mode = v ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot };
        private static AnimatorCondition Entero(string p, int v) => new AnimatorCondition { parameter = p, mode = AnimatorConditionMode.Equals, threshold = v };
        private static AnimatorCondition Disparador(string p) => new AnimatorCondition { parameter = p, mode = AnimatorConditionMode.If };

        // ---------- Paso 3: overrides ----------

        /// <summary>Los nombres de los 16 overrides (9 papeles y 7 extra) con su tabla de clips (base -> clip), tal como la arman ConstructorControladorBase y ClipsNpcNovaT1506.</summary>
        public static List<(string Ruta, (string Base, string Papel)[] Tabla)> TablasDeOverrides()
        {
            var lista = new List<(string, (string, string)[])>();
            foreach (string papel in ConstructorControladorBase.Papeles)
            {
                lista.Add((ConstructorControladorBase.RutaOverride(papel), ConstructorControladorBase.ClipsPorPapel[papel]));
            }
            foreach ((string nombre, string padre, (string Base, string Papel)[] clips) in ClipsNpcNovaT1506.OverridesExtra)
            {
                lista.Add((ClipsNpcNovaT1506.RutaOverrideExtra(nombre), ClipsNpcNovaT1506.Combinar(ConstructorControladorBase.ClipsPorPapel[padre], clips)));
            }
            return lista;
        }

        /// <summary>
        /// Pone en cada override el clip que dice su tabla para las <see cref="ClavesOverride"/> y las filas de robots de T-1540 (<see cref="ClipsNpcNovaT1506.OverridesExtra"/>: muerte y daño de E15 y E16);
        /// el resto no se toca. Devuelve cuántos clips cambió.
        /// </summary>
        public static int AplicarOverrides(AnimatorController baseCtrl)
        {
            int cambios = 0;
            foreach ((string ruta, (string Base, string Papel)[] tabla) in TablasDeOverrides())
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
                    bool tocaEstaClave = ClavesOverride.Contains(clave.name) || ClipsNpcNovaT1506.ClavesDeRobotT1540.Contains(clave.name);
                    if (!tocaEstaClave)
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

        // ---------- Diagnóstico ----------

        /// <summary>
        /// Lista, por controlador (ControladorBase y los 16 overrides), cada estado -> clip efectivo, y revisa lo de T-1540 (estados, clips, transiciones, overrides). Escribe en el log con el
        /// prefijo [T-1540] y devuelve el número de problemas (0 = todo bien).
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T-1540/Diagnóstico")]
        public static int Diagnostico()
        {
            int problemas = 0;
            var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);
            if (c == null)
            {
                Debug.LogError(Etiqueta + "falta el ControladorBase");
                return 1;
            }
            // 1) Clips nuevos: archivo, Humanoid, una sola vez, y que el controlador los use.
            foreach (string nombre in ClipsNuevos.Concat(ReglasAnimacion.EstadosSaltoCorrerT1540.Select(e => e.Clip)))
            {
                string ruta = ImportadorClipsPJ01.Carpeta + nombre + ".fbx";
                var importador = AssetImporter.GetAtPath(ruta) as ModelImporter;
                if (importador == null) { Debug.LogError(Etiqueta + "SIN ARCHIVO " + ruta); problemas++; continue; }
                bool humano = importador.animationType == ModelImporterAnimationType.Human;
                ModelImporterClipAnimation[] clips = importador.clipAnimations;
                bool unClip = clips.Length == 1 && clips[0].name == nombre;
                bool bucle = unClip && clips[0].loopTime;
                string aviso = (humano ? "" : " NO ES HUMANOID") + (unClip ? "" : " CLIP MAL NOMBRADO") + (bucle && !nombre.Contains("Aire") ? " TIENE BUCLE" : "");
                Debug.Log(Etiqueta + "clip " + nombre + ": humanoid=" + humano + ", un clip=" + unClip + ", bucle=" + bucle + aviso);
                if (aviso.Length > 0) { problemas++; }
            }
            // 2) Estados nuevos.
            AnimatorStateMachine m = c.layers[0].stateMachine;
            foreach ((string estado, string clip, int gesto) in ReglasAnimacion.EstadosT1540)
            {
                ChildAnimatorState hijo = m.states.FirstOrDefault(s => s.state.name == estado);
                AnimatorState s = hijo.state;
                bool existe = s != null;
                bool clipOk = existe && s.motion != null && s.motion.name == clip;
                int entradas = existe ? m.anyStateTransitions.Count(t => t.destinationState == s) : 0;
                int salidas = existe ? s.transitions.Length : 0;
                Debug.Log(Etiqueta + "estado " + estado + " -> " + (existe && s.motion != null ? s.motion.name : "(sin clip)") + ", entradas Any=" + entradas + ", salidas=" + salidas + (gesto >= 0 ? ", GestoTipo=" + gesto : ", MuerteTipo"));
                if (!existe || !clipOk || entradas != 1 || salidas < 1) { Debug.LogError(Etiqueta + "PROBLEMA con el estado " + estado); problemas++; }
            }
            foreach ((string estado, string clip) in ReglasAnimacion.EstadosSaltoCorrerT1540)
            {
                AnimatorState s = m.states.FirstOrDefault(x => x.state.name == estado).state;
                bool ok = s != null && s.motion != null && s.motion.name == clip && s.transitions.Length >= 2;
                Debug.Log(Etiqueta + "estado " + estado + " -> " + (s != null && s.motion != null ? s.motion.name : "(sin clip)") + ", salidas=" + (s != null ? s.transitions.Length : 0));
                if (!ok) { Debug.LogError(Etiqueta + "PROBLEMA con el estado " + estado); problemas++; }
            }
            foreach (string p in new[] { ReglasAnimacion.RapidezSaltoInicio, ReglasAnimacion.RapidezSaltoAterrizaje })
            {
                bool ok = c.parameters.Any(x => x.name == p && x.type == AnimatorControllerParameterType.Float && Mathf.Approximately(x.defaultFloat, 1f));
                if (!ok) { Debug.LogError(Etiqueta + "falta el parámetro " + p); problemas++; }
            }
            // 3) Tabla completa por controlador.
            var sb = new StringBuilder(Etiqueta + "estado -> clip efectivo\n");
            problemas += VolcarControlador(sb, "ControladorBase", c, null);
            foreach (var entrada in TablasDeOverrides())
            {
                string ruta = entrada.Ruta;
                var o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ruta);
                if (o == null) { Debug.LogError(Etiqueta + "falta " + ruta); problemas++; continue; }
                var pares = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                o.GetOverrides(pares);
                var mapa = new Dictionary<AnimationClip, AnimationClip>();
                foreach (KeyValuePair<AnimationClip, AnimationClip> par in pares)
                {
                    if (par.Key != null) { mapa[par.Key] = par.Value != null ? par.Value : par.Key; }
                }
                problemas += VolcarControlador(sb, System.IO.Path.GetFileNameWithoutExtension(ruta), c, mapa);
                // Lo que pide esta tarea: el daño de frente y de espalda y la burla.
                foreach ((string clave, string valor) in mapa.Select(kv => (kv.Key.name, kv.Value.name)).Where(kv => ClavesOverride.Contains(kv.Item1)))
                {
                    sb.AppendLine("    [" + System.IO.Path.GetFileNameWithoutExtension(ruta) + "] " + clave + " => " + valor);
                }
            }
            Debug.Log(sb.ToString());
            Debug.Log(Etiqueta + "Diagnostico: " + problemas + " problema(s)");
            return problemas;
        }

        private static int VolcarControlador(StringBuilder sb, string nombre, AnimatorController c, Dictionary<AnimationClip, AnimationClip> mapa)
        {
            int sinClip = 0;
            sb.AppendLine("== " + nombre);
            foreach (AnimatorControllerLayer capa in c.layers)
            {
                Maquina(sb, capa.name, capa.stateMachine, mapa, ref sinClip);
            }
            return sinClip;
        }

        private static void Maquina(StringBuilder sb, string ruta, AnimatorStateMachine m, Dictionary<AnimationClip, AnimationClip> mapa, ref int sinClip)
        {
            foreach (ChildAnimatorState s in m.states)
            {
                Motion(sb, ruta + "/" + s.state.name, s.state.motion, mapa, s.state.name != "Vacio", ref sinClip);
            }
            foreach (ChildAnimatorStateMachine hija in m.stateMachines)
            {
                Maquina(sb, ruta + "/" + hija.stateMachine.name, hija.stateMachine, mapa, ref sinClip);
            }
        }

        private static void Motion(StringBuilder sb, string ruta, Motion motion, Dictionary<AnimationClip, AnimationClip> mapa, bool debeTener, ref int sinClip)
        {
            if (motion is BlendTree arbol)
            {
                foreach (ChildMotion h in arbol.children)
                {
                    Motion(sb, ruta + "/" + arbol.name, h.motion, mapa, true, ref sinClip);
                }
            }
            else if (motion is AnimationClip clip)
            {
                AnimationClip efectivo = mapa != null && mapa.TryGetValue(clip, out AnimationClip v) ? v : clip;
                sb.AppendLine("  " + ruta + " -> " + efectivo.name);
            }
            else if (debeTener)
            {
                sb.AppendLine("  " + ruta + " -> (SIN CLIP)");
                sinClip++;
            }
        }
    }
}

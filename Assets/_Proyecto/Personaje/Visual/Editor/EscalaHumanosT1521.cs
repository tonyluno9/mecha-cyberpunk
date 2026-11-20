using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-1521: todos los humanos a la altura del jugador (tabla en <see cref="EscalasHumanos"/>). La altura se mide animada (malla horneada en pose de Idle, como
    /// EscalaJugadorT670Pruebas) y no con los bounds de la pose de reposo, que mienten (~1.80 m de reposo contra 1.1 a 1.4 m animados en algunos modelos).
    /// La escala vive en el Modelo de cada prefab Visual_ (Giro/Modelo): no se toca la escala de importación de los FBX ni ningún clip.
    /// En este orden, con -executeMethod MechaCyberpunk.Personaje.Visual.Editor.EscalaHumanosT1521.&lt;Método&gt;:
    ///   Diagnostico                 mide todos los Visual_ y escribe Logs/t1521-alturas.txt (no cambia nada)
    ///   Aplicar                     escala el Modelo de los prefabs de la tabla y quita en Ciudad_Prologo las escalas pisadas en las instancias (se puede repetir)
    ///   DiagnosticoCivilAcostado    busca las causas posibles de un civil acostado y vivo y escribe Logs/t1521-civil-acostado.txt
    /// </summary>
    public static class EscalaHumanosT1521
    {
        public const string CarpetaVisuales = "Assets/_Proyecto/Personaje/Visual/Prefabs/";
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string RutaTablaAlturas = "Logs/t1521-alturas.txt";
        public const string RutaCivilAcostado = "Logs/t1521-civil-acostado.txt";
        public const string Etiqueta = "[T-1521] ";

        private const string RutaModelo = "Giro/Modelo";
        private const string NombreIdle = "Idle";
        private const string ClipIdleRespaldo = "PJ01_Loco_Idle";
        private const string ClipDePieAlterno = "NPC_Esperar";
        private const string VisualJugador = "Visual_PJ01";
        /// <summary>Punto del ciclo del idle en que se mide (fracción de su duración).</summary>
        private const float FraccionIdle = 0.25f;
        /// <summary>Un idle con la cabeza por debajo de esto (m) es un personaje acostado o doblado.</summary>
        public const float AlturaCabezaIdleMinima = 1.2f;
        /// <summary>Más lejos que esto de su banca (m), un civil sentado se queda sentado en el aire.</summary>
        private const float DistanciaMaximaABanca = 1.5f;
        private const float MargenEscalaIgual = 0.001f;

        /// <summary>Resultado de medir un humano en reposo.</summary>
        public readonly struct Medida
        {
            public readonly bool Valida;
            public readonly float Malla;
            public readonly float Cabeza;
            public readonly string Clip;
            public readonly string Problema;

            public Medida(bool valida, float malla, float cabeza, string clip, string problema)
            {
                Valida = valida;
                Malla = malla;
                Cabeza = cabeza;
                Clip = clip;
                Problema = problema;
            }
        }

        [MenuItem("Mecha Cyberpunk/T-1521/1 Diagnostico de alturas")]
        public static void Diagnostico()
        {
            var salida = new StringBuilder();
            salida.AppendLine("T-1521 alturas animadas (m). malla = punto más alto menos el más bajo de la malla horneada en Idle; cabeza = hueso de la cabeza sobre la planta.");
            salida.AppendLine("visual | escala Modelo | clip idle | malla | cabeza | objetivo | estado");
            Medida jugador = MedirPrefab(CarpetaVisuales + VisualJugador + ".prefab");
            salida.AppendLine(VisualJugador + " | referencia | " + jugador.Clip + " | " + Formato(jugador.Malla) + " | " + Formato(jugador.Cabeza) + " | " + Formato(EscalasHumanos.AlturaJugador) + " | jugador");
            int desviados = 0;
            int medidos = 0;
            foreach (string visual in TodosLosVisuales())
            {
                Medida m = MedirPrefab(CarpetaVisuales + visual + ".prefab");
                float objetivo = EscalasHumanos.Objetivo(visual);
                string estado;
                if (!m.Valida)
                {
                    estado = "SIN MEDIDA: " + m.Problema;
                }
                else if (objetivo <= 0f)
                {
                    estado = "fuera de la tabla (no se escala)";
                }
                else if (Mathf.Abs(m.Malla - objetivo) <= EscalasHumanos.ToleranciaAplicar)
                {
                    estado = "OK";
                }
                else
                {
                    estado = "DESVIADO " + (m.Malla - objetivo).ToString("+0.00;-0.00");
                    desviados++;
                }
                if (m.Valida && m.Cabeza < AlturaCabezaIdleMinima && objetivo > 0f)
                {
                    estado += " | SOSPECHOSO: la cabeza queda a " + Formato(m.Cabeza) + " m en idle (acostado o doblado)";
                }
                medidos++;
                salida.AppendLine(visual + " | " + EscalaDelModelo(visual) + " | " + m.Clip + " | " + Formato(m.Malla) + " | " + Formato(m.Cabeza) + " | "
                    + (objetivo > 0f ? Formato(objetivo) : "-") + " | " + estado);
            }
            Escribir(RutaTablaAlturas, salida.ToString());
            Debug.Log(Etiqueta + "diagnóstico: " + medidos + " visuales medidos, " + desviados + " fuera de su altura objetivo; jugador " + Formato(jugador.Malla) + " m. Tabla en " + RutaTablaAlturas);
        }

        [MenuItem("Mecha Cyberpunk/T-1521/2 Aplicar escalas")]
        public static void Aplicar()
        {
            int cambiados = 0;
            int iguales = 0;
            int fallos = 0;
            var salida = new StringBuilder();
            salida.AppendLine("T-1521 aplicar: visual | escala antes | altura antes | escala después | altura después | objetivo");
            foreach (string visual in EscalasHumanos.Prefabs)
            {
                string ruta = CarpetaVisuales + visual + ".prefab";
                float objetivo = EscalasHumanos.Objetivo(visual);
                Medida antes = MedirPrefab(ruta);
                if (!antes.Valida)
                {
                    Debug.LogError(Etiqueta + visual + ": no se pudo medir (" + antes.Problema + "); no se toca.");
                    fallos++;
                    continue;
                }
                if (Mathf.Abs(antes.Malla - objetivo) <= EscalasHumanos.ToleranciaAplicar)
                {
                    iguales++;
                    continue;
                }
                float factor = EscalasHumanos.FactorCorreccion(antes.Malla, objetivo);
                if (Mathf.Approximately(factor, 1f))
                {
                    Debug.LogError(Etiqueta + visual + ": la medida (" + Formato(antes.Malla) + " m) no es de fiar para llegar a " + Formato(objetivo) + " m; no se toca.");
                    fallos++;
                    continue;
                }
                string escalaAntes = EscalaDelModelo(visual);
                if (!EscalarModelo(ruta, factor))
                {
                    fallos++;
                    continue;
                }
                Medida despues = MedirPrefab(ruta);
                cambiados++;
                salida.AppendLine(visual + " | " + escalaAntes + " | " + Formato(antes.Malla) + " | " + EscalaDelModelo(visual) + " | " + Formato(despues.Malla) + " | " + Formato(objetivo));
                if (!despues.Valida || Mathf.Abs(despues.Malla - objetivo) > EscalasHumanos.ToleranciaPrueba)
                {
                    Debug.LogWarning(Etiqueta + visual + ": después de escalar mide " + Formato(despues.Malla) + " m y el objetivo es " + Formato(objetivo) + " m.");
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Escribir("Logs/t1521-aplicar.txt", salida.ToString());
            Debug.Log(Etiqueta + "prefabs: " + cambiados + " escalados, " + iguales + " ya estaban a su altura, " + fallos + " sin tocar.");
            AplicarEscena();
        }

        /// <summary>Multiplica la escala del Modelo del prefab por <paramref name="factor"/>.</summary>
        private static bool EscalarModelo(string ruta, float factor)
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                Transform modelo = contenido.transform.Find(RutaModelo);
                if (modelo == null)
                {
                    Debug.LogError(Etiqueta + ruta + " no tiene " + RutaModelo);
                    return false;
                }
                modelo.localScale *= factor;
                PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        /// <summary>
        /// En Ciudad_Prologo las instancias de Visual_ pueden traer la escala del Modelo pisada (el Inventor a 1.67, el Mecánico a 1.52): esa copia ya no manda,
        /// se quita para que gobierne el prefab. Avisa también de cualquier padre con escala distinta de 1 (el humano heredaría su tamaño).
        /// </summary>
        public static void AplicarEscena()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError(Etiqueta + "no se toca la escena en Play.");
                return;
            }
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            int revisados = 0;
            int revertidos = 0;
            foreach (VisualPersonaje visual in Object.FindObjectsByType<VisualPersonaje>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                GameObject fuente = PrefabUtility.GetCorrespondingObjectFromSource(visual.gameObject);
                if (fuente == null || EscalasHumanos.Objetivo(fuente.name) <= 0f)
                {
                    continue;
                }
                revisados++;
                if (QuitarEscalaPisada(visual.transform, fuente.transform))
                {
                    revertidos++;
                }
                Transform modelo = visual.transform.Find(RutaModelo);
                Transform modeloFuente = fuente.transform.Find(RutaModelo);
                if (modelo != null && modeloFuente != null && QuitarEscalaPisada(modelo, modeloFuente))
                {
                    revertidos++;
                }
                for (Transform padre = visual.transform.parent; padre != null; padre = padre.parent)
                {
                    if (Mathf.Abs(padre.localScale.x - 1f) > MargenEscalaIgual || Mathf.Abs(padre.localScale.y - 1f) > MargenEscalaIgual)
                    {
                        Debug.LogWarning(Etiqueta + visual.name + " cuelga de " + padre.name + " con escala " + padre.localScale.ToString("F3") + ": hereda su tamaño.");
                    }
                }
            }
            if (revertidos > 0)
            {
                EditorSceneManager.MarkSceneDirty(escena);
                EditorSceneManager.SaveScene(escena);
            }
            Debug.Log(Etiqueta + "escena: " + revisados + " humanos revisados, " + revertidos + " escalas pisadas quitadas.");
        }

        private static bool QuitarEscalaPisada(Transform instancia, Transform fuente)
        {
            if ((instancia.localScale - fuente.localScale).sqrMagnitude < MargenEscalaIgual * MargenEscalaIgual)
            {
                return false;
            }
            Vector3 antes = instancia.localScale;
            SerializedProperty propiedad = new SerializedObject(instancia).FindProperty("m_LocalScale");
            PrefabUtility.RevertPropertyOverride(propiedad, InteractionMode.AutomatedAction);
            Debug.Log(Etiqueta + instancia.root.name + "/" + instancia.name + ": escala " + antes.ToString("F3") + " -> " + instancia.localScale.ToString("F3") + " (la del prefab).");
            return true;
        }

        /// <summary>Mide un prefab Visual_ en pose de Idle.</summary>
        public static Medida MedirPrefab(string ruta)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (prefab == null)
            {
                return new Medida(false, 0f, 0f, "-", "no existe " + ruta);
            }
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                instancia.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                return Medir(instancia);
            }
            finally
            {
                Object.DestroyImmediate(instancia);
            }
        }

        /// <summary>
        /// Altura de la malla horneada con el Animator muestreado en su clip de Idle: de la planta del pie al punto más alto de las mallas con piel,
        /// igual que EscalaJugadorT670Pruebas, pero siempre con un clip de pie (<see cref="ClipDePie"/>). <c>Cabeza</c> es el hueso de la cabeza sobre la planta (control cruzado, no depende del BakeMesh).
        /// </summary>
        public static Medida Medir(GameObject instancia)
        {
            Animator animador = instancia.GetComponentInChildren<Animator>(true);
            if (animador == null)
            {
                return new Medida(false, 0f, 0f, "-", "sin Animator");
            }
            AnimationClip clip = ClipDePie();
            if (clip == null)
            {
                return new Medida(false, 0f, 0f, "-", "sin clip de Idle");
            }
            animador.enabled = false;
            clip.SampleAnimation(animador.gameObject, clip.length * FraccionIdle);
            float bajo = float.MaxValue;
            float alto = float.MinValue;
            var malla = new Mesh();
            try
            {
                foreach (SkinnedMeshRenderer r in instancia.GetComponentsInChildren<SkinnedMeshRenderer>(false))
                {
                    if (!r.enabled || r.sharedMesh == null)
                    {
                        continue;
                    }
                    r.BakeMesh(malla, true);
                    foreach (Vector3 v in malla.vertices)
                    {
                        float y = r.transform.TransformPoint(v).y;
                        bajo = Mathf.Min(bajo, y);
                        alto = Mathf.Max(alto, y);
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(malla);
            }
            if (alto <= bajo)
            {
                return new Medida(false, 0f, 0f, clip.name, "sin mallas con piel");
            }
            Transform cabeza = animador.avatar != null && animador.isHuman ? animador.GetBoneTransform(HumanBodyBones.Head) : null;
            float alturaCabeza = cabeza != null ? cabeza.position.y - bajo : 0f;
            return new Medida(true, alto - bajo, alturaCabeza, clip.name, cabeza == null ? "sin hueso de cabeza (avatar no humanoide o inválido)" : null);
        }

        /// <summary>
        /// Clip de pie con el que se mide a todos (PJ01_Loco_Idle, o NPC_Esperar si falta). El Idle propio del visual no sirve: el del Hacker es sentado tecleando y el del
        /// Mecánico agachado, y medirlos así los dejaba a 2.4 m de pie. Los clips Humanoid se reproducen en cualquier avatar humanoide.
        /// </summary>
        public static AnimationClip ClipDePie()
        {
            AnimationClip clip = ConstructorControladorBase.Clip(ClipIdleRespaldo);
            return clip != null ? clip : ConstructorControladorBase.Clip(ClipDePieAlterno);
        }

        /// <summary>El clip del estado Idle de la capa 0 (con la sustitución del override si lo hay).</summary>
        public static AnimationClip ClipIdle(Animator animador)
        {
            RuntimeAnimatorController controlador = animador.runtimeAnimatorController;
            AnimationClip clip = null;
            if (controlador != null)
            {
                var sustituto = controlador as AnimatorOverrideController;
                var maquina = (sustituto != null ? sustituto.runtimeAnimatorController : controlador) as AnimatorController;
                if (maquina != null && maquina.layers.Length > 0)
                {
                    AnimatorStateMachine estados = maquina.layers[0].stateMachine;
                    AnimatorState idle = estados.states.Select(e => e.state).FirstOrDefault(s => s.name == NombreIdle) ?? estados.defaultState;
                    clip = PrimerClip(idle != null ? idle.motion : null);
                    if (clip != null && sustituto != null)
                    {
                        AnimationClip cambiado = sustituto[clip];
                        if (cambiado != null)
                        {
                            clip = cambiado;
                        }
                    }
                }
            }
            return clip != null ? clip : ConstructorControladorBase.Clip(ClipIdleRespaldo);
        }

        private static AnimationClip PrimerClip(Motion movimiento)
        {
            if (movimiento is AnimationClip clip)
            {
                return clip;
            }
            if (movimiento is BlendTree arbol)
            {
                foreach (ChildMotion hijo in arbol.children)
                {
                    AnimationClip encontrado = PrimerClip(hijo.motion);
                    if (encontrado != null)
                    {
                        return encontrado;
                    }
                }
            }
            return null;
        }

        private static IEnumerable<string> TodosLosVisuales()
        {
            return AssetDatabase.FindAssets("t:Prefab Visual_", new[] { CarpetaVisuales.TrimEnd('/') })
                .Select(g => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)))
                .Where(n => n != VisualJugador && n.StartsWith("Visual_"))
                .OrderBy(n => n);
        }

        private static string EscalaDelModelo(string visual)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaVisuales + visual + ".prefab");
            Transform modelo = prefab != null ? prefab.transform.Find(RutaModelo) : null;
            return modelo != null ? modelo.localScale.y.ToString("0.####") : "?";
        }

        // ---------- Civil acostado y vivo ----------

        /// <summary>
        /// Busca por qué un civil vivo puede quedar acostado en la calle (playtest 20-nov): idles con la cabeza baja, avatares inválidos, visuales de peatón sin la animación de muerte
        /// (el peatón cae girando 90 grados la raíz), humanos de la escena inclinados, sentados lejos de su banca. Escribe Logs/t1521-civil-acostado.txt.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T-1521/3 Diagnostico civil acostado")]
        public static void DiagnosticoCivilAcostado()
        {
            var salida = new StringBuilder();
            int hallazgos = 0;
            salida.AppendLine("T-1521 civil acostado: cada línea es un hallazgo; sin líneas en una sección = nada raro ahí.");

            salida.AppendLine("== 1. Prefabs Visual_ cuyo idle deja la cabeza baja, sin avatar humano o sin animación de caída (KO) ==");
            foreach (string visual in TodosLosVisuales())
            {
                string ruta = CarpetaVisuales + visual + ".prefab";
                Medida m = MedirPrefab(ruta);
                if (!m.Valida)
                {
                    salida.AppendLine("  " + visual + ": no se pudo medir (" + m.Problema + ")");
                    hallazgos++;
                    continue;
                }
                if (m.Cabeza < AlturaCabezaIdleMinima && m.Cabeza > 0f)
                {
                    salida.AppendLine("  " + visual + ": en Idle la cabeza queda a " + Formato(m.Cabeza) + " m (clip " + m.Clip + ")");
                    hallazgos++;
                }
                else if (m.Cabeza <= 0f)
                {
                    salida.AppendLine("  " + visual + ": " + m.Problema);
                    hallazgos++;
                }
                if (EscalasHumanos.Objetivo(visual) > 0f && !TieneParametroKO(ruta))
                {
                    salida.AppendLine("  " + visual + ": su controlador no tiene el parámetro KO; si muere o lo tumban, PeatonMundo gira la raíz 90 grados en vez de animarlo");
                    hallazgos++;
                }
            }

            salida.AppendLine("== 2. Humanos de Ciudad_Prologo inclinados o sentados lejos de su banca ==");
            if (!EditorApplication.isPlaying)
            {
                OpenSceneSafe(salida, ref hallazgos);
            }

            Escribir(RutaCivilAcostado, salida.ToString());
            Debug.Log(Etiqueta + "civil acostado: " + hallazgos + " hallazgos estáticos. Detalle en " + RutaCivilAcostado
                + ". El cambio de código es la guarda de AnimacionCivilMundo (ReglaPosturaCivil), que levanta a un civil vivo con la cabeza baja o tumbado de más.");
        }

        private static void OpenSceneSafe(StringBuilder salida, ref int hallazgos)
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            foreach (VisualPersonaje visual in Object.FindObjectsByType<VisualPersonaje>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Transform raiz = visual.transform;
                for (Transform t = raiz; t != null; t = t.parent)
                {
                    if (t.up.y < 0.9f)
                    {
                        salida.AppendLine("  " + raiz.name + ": " + t.name + " está inclinado (arriba = " + t.up.ToString("F2") + ")");
                        hallazgos++;
                        break;
                    }
                }
            }
            foreach (NpcSentadoBanca sentado in Object.FindObjectsByType<NpcSentadoBanca>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (sentado.Banca == null)
                {
                    salida.AppendLine("  " + sentado.name + ": sentado sin banca (se queda en el aire)");
                    hallazgos++;
                }
                else if (Vector3.Distance(sentado.transform.position, sentado.Banca.position) > DistanciaMaximaABanca)
                {
                    salida.AppendLine("  " + sentado.name + ": a " + Vector3.Distance(sentado.transform.position, sentado.Banca.position).ToString("F1") + " m de su banca");
                    hallazgos++;
                }
            }
        }

        private static bool TieneParametroKO(string ruta)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            Animator animador = prefab != null ? prefab.GetComponentInChildren<Animator>(true) : null;
            RuntimeAnimatorController controlador = animador != null ? animador.runtimeAnimatorController : null;
            if (controlador == null)
            {
                return false;
            }
            var sustituto = controlador as AnimatorOverrideController;
            var maquina = (sustituto != null ? sustituto.runtimeAnimatorController : controlador) as AnimatorController;
            return maquina != null && maquina.parameters.Any(p => p.name == ReglasVisual.Caer && p.type == AnimatorControllerParameterType.Bool);
        }

        private static string Formato(float valor) => valor.ToString("0.00");

        private static void Escribir(string ruta, string texto)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllText(ruta, texto);
        }
    }
}

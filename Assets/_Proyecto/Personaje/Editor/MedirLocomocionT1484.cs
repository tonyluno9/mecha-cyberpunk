using System.Collections.Generic;
using System.IO;
using System.Text;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Editor
{
    /// <summary>
    /// T-1484: mide la locomoción real del jugador a pie. Para cada clip de caminar, correr, lados, atrás y agachado: duración, averageSpeed del clip
    /// y la velocidad a la que debe avanzar el cuerpo para que el pie de apoyo no patine (muestreando el clip sobre Visual_PJ01 y siguiendo el pie de apoyo
    /// respecto de la cadera y de la raíz), longitud de paso y cadencia. También vuelca los números actuales del jugador y del Animator.
    /// Escribe Logs/t1484-medir.txt.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Editor.MedirLocomocionT1484.Medir
    /// </summary>
    public static class MedirLocomocionT1484
    {
        public const string RutaSalida = "Logs/t1484-medir.txt";
        public const string RutaVisual = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_PJ01.prefab";
        public const string RutaOverrideJugador = "Assets/Arte/Personajes/Animaciones/Overrides/Override_Jugador.overrideController";

        private const float CuadrosPorSegundo = 60f;
        // Un pie está apoyado si su altura está en esta fracción inferior de su recorrido vertical en el ciclo.
        private const float FraccionApoyo = 0.15f;
        private const float MargenApoyoMinimo = 0.01f;

        private static readonly string[] Clips =
        {
            "Loco_Caminar", "Loco_Correr", "Loco_CaminarAtras", "Loco_CaminarDer", "Loco_CaminarIzq",
            "Loco_Caminar_SinImplante", "Loco_Caminar_ConImplante", "Loco_Correr_SinImplante", "Loco_Correr_ConImplante",
            "Loco_AgachadoCaminar", "Loco_AgachadoAtras", "Loco_AgachadoLateral_Derecha", "Loco_AgachadoLateral_Izquierda",
            "PJ01_Caminar_SinImplante", "PJ01_Caminar_ConImplante", "PJ01_Correr_SinImplante", "PJ01_Correr_ConImplante",
            "PJ01_Loco_CaminarAtras", "PJ01_Loco_CaminarDer", "PJ01_Loco_CaminarIzq",
            "PJ01_Loco_CaminarAtras_ConImplante", "PJ01_Loco_CaminarDer_ConImplante", "PJ01_Loco_CaminarIzq_ConImplante",
            "PJ01_Loco_AgachadoCaminar", "PJ01_Loco_AgachadoAtras", "PJ01_Loco_AgachadoLateral_Derecha", "PJ01_Loco_AgachadoLateral_Izquierda",
            "PJ01_Pistola_Caminar", "PJ01_Pistola_Correr", "PJ01_Pistola_AgachadoCaminar", "PJ01_Rifle_Caminar", "PJ01_Rifle_Correr",
        };

        private static readonly string[] EstadosDelControlador =
        {
            "Idle", "Caminar", "Correr", "Frenar", "GirarDer", "GirarIzq", "Agacharse", "AgachadoQuieto", "AgachadoCaminar", "Pararse",
        };

        private static readonly string[] AssetsDeNumeros = { "DatosPersonaje", "ConfigControlesAPie", "DatosCubierta", "ConfigCamaraHombro" };

        [MenuItem("Mecha Cyberpunk/Personajes/T-1484 Medir locomoción")]
        public static void Medir()
        {
            var sb = new StringBuilder();
            sb.AppendLine("T-1484 medicion de locomocion del jugador a pie");
            sb.AppendLine();
            try
            {
                EscribirEstadoActual(sb);
            }
            catch (System.Exception e)
            {
                sb.AppendLine("ERROR estado actual: " + e);
            }
            sb.AppendLine();
            sb.AppendLine("=== MEDIDAS POR CLIP (sobre Visual_PJ01, metros del mundo) ===");
            sb.AppendLine("vel = velocidad del cuerpo (m/s) que deja quieto al pie de apoyo; 'cad' = pasos por segundo; paso = m por paso (un pie); zancada = 2 pasos.");
            sb.AppendLine("vel(cad) respecto de la cadera en horizontal; vel(raiz) respecto de la raiz del modelo; adel/der = componentes de la velocidad del cuerpo (adel>0 avanza).");
            sb.AppendLine();
            GameObject modelo = null;
            try
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisual);
                if (prefab == null)
                {
                    sb.AppendLine("ERROR: falta " + RutaVisual);
                }
                else
                {
                    modelo = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    modelo.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    foreach (string nombre in Clips)
                    {
                        try
                        {
                            MedirClip(sb, modelo, nombre);
                        }
                        catch (System.Exception e)
                        {
                            sb.AppendLine(nombre + ": ERROR " + e.Message);
                        }
                    }
                }
            }
            finally
            {
                if (modelo != null)
                {
                    Object.DestroyImmediate(modelo);
                }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(RutaSalida));
            File.WriteAllText(RutaSalida, sb.ToString());
            Debug.Log("[Personaje] T-1484: medicion escrita en " + RutaSalida);
        }

        // ---------- Estado actual ----------

        private static void EscribirEstadoActual(StringBuilder sb)
        {
            sb.AppendLine("=== VELOCIDADES Y CONSTANTES ACTUALES ===");
            sb.AppendLine("ReglasAnimacion.VelocidadAndar = " + ReglasAnimacion.VelocidadAndar + " m/s; VelocidadCarrera = " + ReglasAnimacion.VelocidadCarrera
                + "; VelocidadAgachadoCaminando = " + ReglasAnimacion.VelocidadAgachadoCaminando);
            sb.AppendLine("ReglasVisual.UmbralCaminar = " + ReglasVisual.UmbralCaminar + "; ReglasAnimacion.UmbralCorrer = " + ReglasAnimacion.UmbralCorrer
                + "; HisteresisCorrer = " + ReglasAnimacion.HisteresisCorrer + "; UmbralGiro = " + ReglasAnimacion.UmbralGiro + " grados/s");
            sb.AppendLine("ReglasAnimacion.VelocidadClipCaminar = " + ReglasAnimacion.VelocidadClipCaminar + "; VelocidadClipCorrer = " + ReglasAnimacion.VelocidadClipCorrer
                + "; SinImplante caminar/correr = " + ReglasAnimacion.VelocidadClipCaminarSinImplante + "/" + ReglasAnimacion.VelocidadClipCorrerSinImplante
                + "; ConImplante = " + ReglasAnimacion.VelocidadClipCaminarConImplante + "/" + ReglasAnimacion.VelocidadClipCorrerConImplante);
            sb.AppendLine("ReglasAnimacion.RapidezMaximaCaminar = " + ReglasAnimacion.RapidezMaximaCaminar + "; RapidezMaximaLocomocion = " + ReglasAnimacion.RapidezMaximaLocomocion);
            sb.AppendLine("AnimacionJugador.ClipsDeTony: ConfigurarClips(0, 0) => VisualPersonaje no ajusta Animator.speed (queda 1) y IKPiesYMirada.Pies = false.");
            sb.AppendLine();

            foreach (string tipo in AssetsDeNumeros)
            {
                foreach (string guid in AssetDatabase.FindAssets("t:" + tipo))
                {
                    string ruta = AssetDatabase.GUIDToAssetPath(guid);
                    if (!ruta.StartsWith("Assets/_Proyecto"))
                    {
                        continue;
                    }
                    Object o = AssetDatabase.LoadAssetAtPath<Object>(ruta);
                    sb.AppendLine("--- " + ruta);
                    VolcarSerializado(sb, o);
                }
            }
            sb.AppendLine();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisual);
            if (prefab != null)
            {
                sb.AppendLine("--- Visual_PJ01: VisualPersonaje");
                VisualPersonaje vp = prefab.GetComponentInChildren<VisualPersonaje>(true);
                if (vp != null)
                {
                    VolcarSerializado(sb, vp);
                }
                IKPiesYMirada ik = prefab.GetComponentInChildren<IKPiesYMirada>(true);
                if (ik != null)
                {
                    sb.AppendLine("--- Visual_PJ01: IKPiesYMirada (prefab; el jugador pone Pies=false en Awake)");
                    VolcarSerializado(sb, ik);
                }
                Animator a = prefab.GetComponentInChildren<Animator>(true);
                if (a != null)
                {
                    sb.AppendLine("Animator del visual: controlador = " + (a.runtimeAnimatorController != null ? a.runtimeAnimatorController.name : "(ninguno)")
                        + "; applyRootMotion = " + a.applyRootMotion + "; escala = " + a.transform.lossyScale.ToString("F3"));
                }
            }
            sb.AppendLine();

            sb.AppendLine("=== OVERRIDE_JUGADOR (papel -> clip real, solo locomocion) ===");
            var ov = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(RutaOverrideJugador);
            if (ov != null)
            {
                var pares = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                ov.GetOverrides(pares);
                foreach (KeyValuePair<AnimationClip, AnimationClip> p in pares)
                {
                    string n = p.Key != null ? p.Key.name : "?";
                    if (n.Contains("Caminar") || n.Contains("Correr") || n.Contains("Frenar") || n.Contains("Girar") || n.Contains("Idle"))
                    {
                        sb.AppendLine("  " + n + " -> " + (p.Value != null ? p.Value.name : "(sin override)"));
                    }
                }
            }
            else
            {
                sb.AppendLine("  (no se encontro " + RutaOverrideJugador + ")");
            }
            sb.AppendLine();

            sb.AppendLine("=== CONTROLADOR BASE: parametros y estados de locomocion (capa 0) ===");
            var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);
            if (c == null)
            {
                sb.AppendLine("  (no se encontro el controlador)");
                return;
            }
            foreach (AnimatorControllerParameter p in c.parameters)
            {
                sb.AppendLine("  param " + p.name + " " + p.type + " def=" + (p.type == AnimatorControllerParameterType.Float ? p.defaultFloat.ToString("F2") : p.type == AnimatorControllerParameterType.Int ? p.defaultInt.ToString() : p.defaultBool.ToString()));
            }
            AnimatorStateMachine m = c.layers[0].stateMachine;
            foreach (ChildAnimatorState cs in m.states)
            {
                AnimatorState s = cs.state;
                if (System.Array.IndexOf(EstadosDelControlador, s.name) < 0)
                {
                    continue;
                }
                sb.AppendLine("  estado " + s.name + " speed=" + s.speed + " speedParam=" + (s.speedParameterActive ? s.speedParameter : "-") + " cicloOffset=" + s.cycleOffset);
                VolcarMovimiento(sb, s.motion, "    ");
                foreach (AnimatorStateTransition t in s.transitions)
                {
                    sb.Append("    -> " + (t.destinationState != null ? t.destinationState.name : "(salida)")
                        + " exit=" + t.hasExitTime + "@" + t.exitTime.ToString("F2") + " dur=" + t.duration.ToString("F2") + (t.hasFixedDuration ? "s" : "norm")
                        + " offset=" + t.offset.ToString("F2") + " irrumpe=" + t.interruptionSource + " si:");
                    foreach (AnimatorCondition cond in t.conditions)
                    {
                        sb.Append(" [" + cond.parameter + " " + cond.mode + " " + cond.threshold.ToString("F2") + "]");
                    }
                    sb.AppendLine();
                }
            }
        }

        private static void VolcarSerializado(StringBuilder sb, Object o)
        {
            if (o == null)
            {
                return;
            }
            var so = new SerializedObject(o);
            SerializedProperty it = so.GetIterator();
            bool entrar = true;
            while (it.NextVisible(entrar))
            {
                entrar = false;
                if (it.name == "m_Script")
                {
                    continue;
                }
                string valor;
                switch (it.propertyType)
                {
                    case SerializedPropertyType.Float: valor = it.floatValue.ToString("0.###"); break;
                    case SerializedPropertyType.Integer: valor = it.intValue.ToString(); break;
                    case SerializedPropertyType.Boolean: valor = it.boolValue.ToString(); break;
                    case SerializedPropertyType.Enum: valor = it.enumNames.Length > it.enumValueIndex && it.enumValueIndex >= 0 ? it.enumNames[it.enumValueIndex] : it.enumValueIndex.ToString(); break;
                    case SerializedPropertyType.ObjectReference: valor = it.objectReferenceValue != null ? it.objectReferenceValue.name : "null"; break;
                    default: continue;
                }
                sb.AppendLine("    " + it.name + " = " + valor);
            }
        }

        private static void VolcarMovimiento(StringBuilder sb, Motion m, string sangria)
        {
            if (m is AnimationClip clip)
            {
                sb.AppendLine(sangria + "clip " + clip.name + " (" + clip.length.ToString("F3") + " s)");
            }
            else if (m is BlendTree b)
            {
                bool dos = b.blendType != BlendTreeType.Simple1D;
                sb.AppendLine(sangria + "arbol " + b.name + " tipo=" + b.blendType + " param=" + b.blendParameter + (dos ? "," + b.blendParameterY : ""));
                foreach (ChildMotion ch in b.children)
                {
                    sb.AppendLine(sangria + "  hijo umbral=" + ch.threshold.ToString("F2") + " pos=" + ch.position + " timeScale=" + ch.timeScale.ToString("F2") + " cicloOffset=" + ch.cycleOffset.ToString("F2"));
                    VolcarMovimiento(sb, ch.motion, sangria + "    ");
                }
            }
            else
            {
                sb.AppendLine(sangria + "(sin movimiento)");
            }
        }

        // ---------- Medida de un clip ----------

        private static void MedirClip(StringBuilder sb, GameObject modelo, string nombre)
        {
            AnimationClip clip = ConstructorControladorBase.Clip(nombre);
            if (clip == null)
            {
                sb.AppendLine(nombre + ": NO EXISTE");
                return;
            }
            Animator a = modelo.GetComponentInChildren<Animator>();
            a.enabled = false;
            Transform piI = a.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform piD = a.GetBoneTransform(HumanBodyBones.RightFoot);
            Transform cadera = a.GetBoneTransform(HumanBodyBones.Hips);
            Transform raiz = a.transform;
            Vector3 frente = Vector3.ProjectOnPlane(raiz.forward, Vector3.up).normalized;
            Vector3 derecha = Vector3.Cross(Vector3.up, frente);

            int n = Mathf.Max(8, Mathf.RoundToInt(clip.length * CuadrosPorSegundo));
            float dt = clip.length / n;
            // [pie][referencia: 0 cadera, 1 raiz][cuadro] -> (adelante, derecha)
            var rel = new Vector2[2, 2, n];
            var altura = new float[2, n];
            for (int i = 0; i < n; i++)
            {
                clip.SampleAnimation(a.gameObject, i * dt);
                Transform[] pies = { piI, piD };
                for (int f = 0; f < 2; f++)
                {
                    Vector3 p = pies[f].position;
                    altura[f, i] = p.y;
                    rel[f, 0, i] = Proyectar(p - cadera.position, frente, derecha);
                    rel[f, 1, i] = Proyectar(p - raiz.position, frente, derecha);
                }
            }
            float minimo = float.MaxValue;
            float maximo = float.MinValue;
            for (int f = 0; f < 2; f++)
            {
                for (int i = 0; i < n; i++)
                {
                    minimo = Mathf.Min(minimo, altura[f, i]);
                    maximo = Mathf.Max(maximo, altura[f, i]);
                }
            }
            float margen = Mathf.Max(MargenApoyoMinimo, (maximo - minimo) * FraccionApoyo);

            var apoyo = new bool[2, n];
            int pasos = 0;
            int[] apoyoPorPie = new int[2];
            for (int f = 0; f < 2; f++)
            {
                for (int i = 0; i < n; i++)
                {
                    apoyo[f, i] = altura[f, i] <= minimo + margen;
                    if (apoyo[f, i])
                    {
                        apoyoPorPie[f]++;
                    }
                }
                for (int i = 0; i < n; i++)
                {
                    if (apoyo[f, i] && !apoyo[f, (i + n - 1) % n])
                    {
                        pasos++;
                    }
                }
            }

            string linea = nombre + ": dur=" + clip.length.ToString("F3") + " s, bucle=" + clip.isLooping + ", fps=" + clip.frameRate.ToString("F0")
                + ", averageSpeed=" + clip.averageSpeed.ToString("F2") + " (|v|=" + new Vector2(clip.averageSpeed.x, clip.averageSpeed.z).magnitude.ToString("F2") + " m/s)"
                + ", apparentSpeed=" + clip.apparentSpeed.ToString("F2") + ", raizCurvas=" + clip.hasRootCurves + ", movCurvas=" + clip.hasMotionCurves
                + ", escalaModelo=" + raiz.lossyScale.y.ToString("F2");
            sb.AppendLine(linea);
            for (int r = 0; r < 2; r++)
            {
                string etiqueta = r == 0 ? "vel(cad)" : "vel(raiz)";
                float[] porPie = new float[2];
                float suma = 0f;
                float suma2 = 0f;
                float adel = 0f;
                float der = 0f;
                int total = 0;
                for (int f = 0; f < 2; f++)
                {
                    float sumaPie = 0f;
                    int cuentaPie = 0;
                    for (int i = 0; i < n; i++)
                    {
                        int j = (i + 1) % n;
                        if (!apoyo[f, i] || !apoyo[f, j])
                        {
                            continue;
                        }
                        Vector2 v = -(rel[f, r, j] - rel[f, r, i]) / dt;
                        float mag = v.magnitude;
                        sumaPie += mag;
                        suma += mag;
                        suma2 += mag * mag;
                        adel += v.x;
                        der += v.y;
                        cuentaPie++;
                        total++;
                    }
                    porPie[f] = cuentaPie > 0 ? sumaPie / cuentaPie : 0f;
                }
                if (total == 0)
                {
                    sb.AppendLine("    " + etiqueta + ": sin cuadros de apoyo");
                    continue;
                }
                float media = suma / total;
                float desv = Mathf.Sqrt(Mathf.Max(0f, suma2 / total - media * media));
                float paso = pasos > 0 ? media * clip.length / pasos : 0f;
                sb.AppendLine("    " + etiqueta + " = " + media.ToString("F2") + " m/s (desv " + desv.ToString("F2") + "; piI " + porPie[0].ToString("F2") + ", piD " + porPie[1].ToString("F2")
                    + "), adel " + (adel / total).ToString("F2") + ", der " + (der / total).ToString("F2")
                    + ", pasos/ciclo = " + pasos + ", cad = " + (pasos / clip.length).ToString("F2") + " pasos/s, paso = " + paso.ToString("F2") + " m, zancada = " + (paso * 2f).ToString("F2") + " m"
                    + ", apoyo piI/piD = " + (100f * apoyoPorPie[0] / n).ToString("F0") + "%/" + (100f * apoyoPorPie[1] / n).ToString("F0") + "%");
            }
            sb.AppendLine("    alturaPie min..max = " + (minimo - raiz.position.y).ToString("F3") + ".." + (maximo - raiz.position.y).ToString("F3") + " m, margenApoyo = " + margen.ToString("F3") + " m");
        }

        private static Vector2 Proyectar(Vector3 d, Vector3 frente, Vector3 derecha)
        {
            return new Vector2(Vector3.Dot(d, frente), Vector3.Dot(d, derecha));
        }
    }
}

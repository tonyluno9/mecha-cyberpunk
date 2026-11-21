using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1552 (decisión de Tony, 20-nov): las alturas de los personajes. La tabla (hoy, nueva, factor) vive en <see cref="EscalasHumanos"/> y la de Nova en <see cref="EscalasNova"/>.
    /// La escala se cambia en el Modelo (Giro/Modelo) de cada prefab Visual_: no se toca el globalScale de ningún FBX, así que no cambia ningún avatar Humanoid ni se reimporta nada.
    /// Cada prefab escalado guarda en su .meta una etiqueta "T1552_b&lt;base&gt;_f&lt;final&gt;" (escala de hoy y escala resultante): con ella Aplicar es repetible y no multiplica dos veces.
    /// En este orden, con -executeMethod MechaCyberpunk.Arte.Editor.AlturasT1552.&lt;Método&gt; (o el menú Mecha Cyberpunk/T-1552):
    ///   Diagnostico   mide todos los Visual_ de la tabla (malla horneada en pose de bind y en Idle) y escribe Logs/t1552-alturas.txt (no cambia nada)
    ///   Aplicar       lee la escala de Tino en la escena, escala el Modelo de cada prefab por su factor, deja la instancia de Tino en 1 y ajusta las cápsulas (se puede repetir)
    ///   Diagnostico   otra vez, para ver cómo quedó cada uno
    ///   Afinar        (solo si hace falta) corrige a mano la escala de quien, ya aplicado, mida en bind más de 5 cm de su altura nueva
    /// </summary>
    public static class AlturasT1552
    {
        public const string CarpetaVisuales = "Assets/_Proyecto/Personaje/Visual/Prefabs/";
        public const string CarpetaIA = "Assets/_Proyecto/IA/Prefabs/";
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string RutaInforme = "Logs/t1552-alturas.txt";
        public const string RutaInformeAplicar = "Logs/t1552-aplicar.txt";
        public const string Etiqueta = "[T-1552] ";
        public const string VisualTino = "Visual_Inventor";
        public const string PrefijoEtiquetaAsset = "T1552_";

        private const string RutaModelo = "Giro/Modelo";
        private const float MargenEscalaIgual = 0.001f;
        private const float ToleranciaRelativaAplicado = 0.001f;
        /// <summary>Si la malla en bind ya mide esto cerca de la altura nueva (m), el prefab ya está a su altura (p. ej. se rehízo con las alturas nuevas) y no se multiplica.</summary>
        public const float ToleranciaYaEnAltura = 0.02f;
        /// <summary>Afinar toca a quien mida en bind más lejos que esto de su altura nueva (m).</summary>
        public const float ToleranciaAfinar = 0.05f;
        /// <summary>Si la malla en bind de hoy difiere de la tabla más que esta fracción, Diagnostico lo marca: la tabla viene de Blender y la medida no coincide.</summary>
        private const float DesviacionTablaAviso = 0.06f;

        // ---------- Lógica pura (la prueban las de EditMode) ----------

        /// <summary>Qué hacer con el Modelo de un prefab.</summary>
        public readonly struct Decision
        {
            public readonly float Escala;
            public readonly float Base;
            public readonly bool Cambia;
            public readonly string Motivo;

            public Decision(float escala, float escalaBase, bool cambia, string motivo)
            {
                Escala = escala;
                Base = escalaBase;
                Cambia = cambia;
                Motivo = motivo;
            }
        }

        /// <summary>
        /// Decide la escala final del Modelo. <paramref name="actual"/> es la escala de hoy del prefab; <paramref name="baseEtiqueta"/> y <paramref name="finEtiqueta"/> las de su
        /// etiqueta (0 si no hay); <paramref name="baseEscena"/> la escala efectiva que ya se ve en la escena si manda sobre la del prefab (Tino; 0 si no hay);
        /// <paramref name="medidaBind"/> la altura de la malla en bind (0 si no se midió).
        /// </summary>
        public static Decision Decidir(string visual, float actual, float baseEtiqueta, float finEtiqueta, float baseEscena, float medidaBind)
        {
            float factor = EscalasHumanos.Factor(visual);
            float objetivo = EscalasHumanos.Objetivo(visual);
            if (objetivo <= 0f || actual <= 0f)
            {
                return new Decision(actual, actual, false, "fuera de la tabla");
            }
            if (finEtiqueta > 0f && Mathf.Abs(actual - finEtiqueta) <= finEtiqueta * ToleranciaRelativaAplicado)
            {
                return new Decision(actual, baseEtiqueta > 0f ? baseEtiqueta : actual, false, "ya aplicado (etiqueta)");
            }
            if (Mathf.Abs(factor - 1f) < 0.001f)
            {
                return new Decision(actual, actual, false, "sin cambio (factor 1)");
            }
            if (baseEscena <= 0f && medidaBind > 0f && Mathf.Abs(medidaBind - objetivo) <= ToleranciaYaEnAltura)
            {
                return new Decision(actual, actual, false, "ya mide su altura nueva en bind (" + medidaBind.ToString("0.00", CultureInfo.InvariantCulture) + " m)");
            }
            float baseEsc = baseEscena > 0f ? baseEscena : actual;
            float escala = baseEsc * factor;
            return new Decision(escala, baseEsc, Mathf.Abs(escala - actual) > 0.0001f, baseEscena > 0f ? "factor sobre la escala de la escena" : "factor sobre la escala del prefab");
        }

        /// <summary>Etiqueta de asset con la escala de hoy y la final.</summary>
        public static string CrearEtiqueta(float escalaBase, float escalaFinal)
        {
            return PrefijoEtiquetaAsset + "b" + escalaBase.ToString("0.#####", CultureInfo.InvariantCulture) + "_f" + escalaFinal.ToString("0.#####", CultureInfo.InvariantCulture);
        }

        /// <summary>Lee la etiqueta T1552_b..._f... de una lista de etiquetas; false si no hay.</summary>
        public static bool LeerEtiqueta(IEnumerable<string> etiquetas, out float escalaBase, out float escalaFinal)
        {
            escalaBase = 0f;
            escalaFinal = 0f;
            if (etiquetas == null)
            {
                return false;
            }
            foreach (string e in etiquetas)
            {
                if (e == null || !e.StartsWith(PrefijoEtiquetaAsset))
                {
                    continue;
                }
                string[] partes = e.Substring(PrefijoEtiquetaAsset.Length).Split('_');
                if (partes.Length == 2 && partes[0].StartsWith("b") && partes[1].StartsWith("f")
                    && float.TryParse(partes[0].Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out escalaBase)
                    && float.TryParse(partes[1].Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out escalaFinal))
                {
                    return true;
                }
            }
            escalaBase = 0f;
            escalaFinal = 0f;
            return false;
        }

        /// <summary>Medidas de la cápsula de un agente de IA.</summary>
        public readonly struct MedidasCapsula
        {
            public readonly float Alto;
            public readonly float Radio;
            public readonly float CentroY;
            public readonly float Paso;
            public readonly float EscalaCuerpo;

            public MedidasCapsula(float alto, float radio, float centroY, float paso, float escalaCuerpo)
            {
                Alto = alto;
                Radio = radio;
                CentroY = centroY;
                Paso = paso;
                EscalaCuerpo = escalaCuerpo;
            }
        }

        /// <summary>
        /// Cápsula de un agente de la altura nueva. <paramref name="completa"/> (Nova y el jefe final): alto, radio, paso y factor del cuerpo salen de <see cref="EscalasNova"/>.
        /// Si no, es un humano grande: solo crece el alto (nunca baja de lo que tiene) y el radio, el paso y el factor del cuerpo se conservan.
        /// </summary>
        public static MedidasCapsula CalcularCapsula(float alturaNueva, bool completa, float altoActual, float radioActual, float pasoActual, float escalaCuerpoActual)
        {
            if (completa)
            {
                float alto = EscalasNova.AlturaCapsula(alturaNueva);
                float paso = Mathf.Min(EscalasNova.PasoCapsula(alturaNueva), alto * 0.5f - 0.01f);
                return new MedidasCapsula(alto, EscalasNova.RadioCapsula(alturaNueva), alto * 0.5f, paso, EscalasNova.Factor(alturaNueva));
            }
            float altoHumano = Mathf.Max(altoActual, EscalasNova.AlturaCapsula(alturaNueva));
            return new MedidasCapsula(altoHumano, radioActual, altoHumano * 0.5f, pasoActual, escalaCuerpoActual);
        }

        /// <summary>Prefab de IA, su visual y si lleva el ajuste completo (Nova y E14).</summary>
        public readonly struct FilaCapsula
        {
            public readonly string Ruta;
            public readonly string Visual;
            public readonly bool Completa;

            public FilaCapsula(string ruta, string visual, bool completa)
            {
                Ruta = ruta;
                Visual = visual;
                Completa = completa;
            }
        }

        /// <summary>Los agentes de IA cuya cápsula sigue a la altura nueva.</summary>
        public static FilaCapsula[] FilasCapsula()
        {
            var filas = new List<FilaCapsula>();
            foreach (string variante in new[] { "E15_CentinelaNova_a", "E15_CentinelaNova_b", "E16_RobotPesadoNova_a", "E16_RobotPesadoNova_b", "E18_EliteNova_a" })
            {
                filas.Add(new FilaCapsula(PersonajesNuevosT1389b.RutaPrefabEnemigo(variante), "Visual_" + variante, true));
            }
            filas.Add(new FilaCapsula(CarpetaIA + "JefeFinalNova_E14.prefab", "Visual_E14_JefeFinalNova", true));
            filas.Add(new FilaCapsula(CarpetaIA + "JefeSnakes_E11.prefab", "Visual_E11_JefeSnakes", false));
            filas.Add(new FilaCapsula(CarpetaIA + "GuardiaPesado_E12.prefab", "Visual_E12_GuardiaPesadoNova", false));
            filas.Add(new FilaCapsula(CarpetaIA + "GuardiaNova_Armado.prefab", "Visual_GuardiaNova", false));
            filas.Add(new FilaCapsula(CarpetaIA + "PandilleroArmado_Snakes.prefab", "Visual_E10_PandilleroSnakes", false));
            filas.Add(new FilaCapsula(CarpetaIA + "PandilleroArmado_Skulls_E19_PandilleroSkulls_a_2.prefab", "Visual_E19_PandilleroSkulls_a_2", false));
            filas.Add(new FilaCapsula(CarpetaIA + "PandilleroArmado_Skulls_E19_PandilleroSkulls_b.prefab", "Visual_E19_PandilleroSkulls_b", false));
            filas.Add(new FilaCapsula(CarpetaIA + "PandilleroArmado_Skulls_E20_MatonSkulls_a.prefab", "Visual_E20_MatonSkulls_a", false));
            filas.Add(new FilaCapsula(CarpetaIA + "PandilleroArmado_Skulls_E20_MatonSkulls_b.prefab", "Visual_E20_MatonSkulls_b", false));
            return filas.ToArray();
        }

        // ---------- Diagnóstico ----------

        [MenuItem("Mecha Cyberpunk/T-1552/1 Diagnostico de alturas")]
        public static void Diagnostico()
        {
            var salida = new StringBuilder();
            salida.AppendLine("T-1552 alturas (m). bind = punto más alto menos el más bajo de la malla horneada en pose de bind (Animator apagado); idle = lo mismo muestreando PJ01_Loco_Idle.");
            salida.AppendLine("La tabla es de Blender: si bind difiere de 'hoy' más de " + (DesviacionTablaAviso * 100f).ToString("0") + " %, la medida no coincide con la tabla y conviene revisar antes de Aplicar.");
            salida.AppendLine("visual | escala Modelo | etiqueta | hoy (tabla) | bind | idle | nueva | factor | bind previsto tras Aplicar | estado");
            int avisos = 0;
            int medidos = 0;
            MedidaAltura jugador = MedirBind(CarpetaVisuales + "Visual_PJ01.prefab");
            salida.AppendLine("Visual_PJ01 | " + EscalaDelModelo("Visual_PJ01") + " | - | " + Formato(EscalasHumanos.AlturaJugador) + " | " + Formato(jugador.Altura) + " | "
                + Formato(EscalaHumanosT1521.MedirPrefab(CarpetaVisuales + "Visual_PJ01.prefab").Malla) + " | " + Formato(EscalasHumanos.AlturaJugador) + " | 1.000 | - | referencia, sin cambio");
            foreach (string visual in EscalasHumanos.Prefabs)
            {
                string ruta = CarpetaVisuales + visual + ".prefab";
                MedidaAltura bind = MedirBind(ruta);
                EscalaHumanosT1521.Medida idle = EscalaHumanosT1521.MedirPrefab(ruta);
                float hoy = EscalasHumanos.Hoy(visual);
                float nueva = EscalasHumanos.Objetivo(visual);
                float factor = EscalasHumanos.Factor(visual);
                bool hayEtiqueta = LeerEtiquetaDePrefab(ruta, out float baseEt, out float finEt);
                string estado;
                if (!bind.Valida)
                {
                    estado = "SIN MEDIDA: " + bind.Problema;
                    avisos++;
                }
                else if (hayEtiqueta)
                {
                    estado = Mathf.Abs(bind.Altura - nueva) <= ToleranciaAfinar ? "APLICADO, bind a " + (bind.Altura - nueva).ToString("+0.00;-0.00", CultureInfo.InvariantCulture) + " de la nueva"
                        : "APLICADO pero bind a " + (bind.Altura - nueva).ToString("+0.00;-0.00", CultureInfo.InvariantCulture) + " de la nueva (Afinar lo corrige)";
                    if (Mathf.Abs(bind.Altura - nueva) > ToleranciaAfinar)
                    {
                        avisos++;
                    }
                }
                else if (Mathf.Abs(bind.Altura - hoy) > hoy * DesviacionTablaAviso)
                {
                    estado = "OJO: bind " + Formato(bind.Altura) + " no coincide con 'hoy' " + Formato(hoy) + " (la tabla de Blender y la malla en Unity difieren)";
                    avisos++;
                }
                else
                {
                    estado = Mathf.Abs(factor - 1f) < 0.001f ? "sin cambio" : "pendiente";
                }
                medidos++;
                salida.AppendLine(visual + " | " + EscalaDelModelo(visual) + " | " + (hayEtiqueta ? CrearEtiqueta(baseEt, finEt) : "-") + " | " + Formato(hoy) + " | " + Formato(bind.Altura) + " | "
                    + Formato(idle.Malla) + " | " + Formato(nueva) + " | " + factor.ToString("0.000", CultureInfo.InvariantCulture) + " | " + (hayEtiqueta ? "-" : Formato(bind.Altura * factor)) + " | " + estado);
            }

            salida.AppendLine();
            salida.AppendLine("== Perro E17 y PJ01: sin cambio (no están en la tabla) ==");
            salida.AppendLine("== Tino en Ciudad_Prologo ==");
            if (!EditorApplication.isPlaying)
            {
                DiagnosticoTino(salida);
            }
            salida.AppendLine();
            salida.AppendLine("== Cápsulas de los agentes de IA (alto/radio/centro Y/paso/escalaCuerpo hoy -> con la altura nueva) ==");
            foreach (FilaCapsula fila in FilasCapsula())
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fila.Ruta);
                if (prefab == null)
                {
                    salida.AppendLine(Path.GetFileNameWithoutExtension(fila.Ruta) + " | no existe (se omite)");
                    continue;
                }
                CharacterController cc = prefab.GetComponent<CharacterController>();
                PandilleroAPie pandillero = prefab.GetComponent<PandilleroAPie>();
                float cuerpo = pandillero != null ? pandillero.EscalaCuerpo : 1f;
                if (cc == null)
                {
                    salida.AppendLine(prefab.name + " | sin CharacterController");
                    continue;
                }
                MedidasCapsula m = CalcularCapsula(EscalasHumanos.Objetivo(fila.Visual), fila.Completa, cc.height, cc.radius, cc.stepOffset, cuerpo);
                salida.AppendLine(prefab.name + " | " + Formato(cc.height) + "/" + Formato(cc.radius) + "/" + Formato(cc.center.y) + "/" + Formato(cc.stepOffset) + "/" + Formato(cuerpo)
                    + " -> " + Formato(m.Alto) + "/" + Formato(m.Radio) + "/" + Formato(m.CentroY) + "/" + Formato(m.Paso) + "/" + Formato(m.EscalaCuerpo));
            }
            salida.AppendLine("RespuestaNova.AlturaAgente = " + Formato(RespuestaNova.AlturaAgente) + " m (E16 y E14 a " + Formato(EscalasHumanos.AlturaRobotGrande) + " m más margen). No hay NavMeshAgent en el proyecto: los agentes usan CharacterController.");
            Escribir(RutaInforme, salida.ToString());
            Debug.Log(Etiqueta + "diagnóstico: " + medidos + " visuales medidos, " + avisos + " avisos. Informe en " + RutaInforme);
        }

        private static void DiagnosticoTino(StringBuilder salida)
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            int cuantos = 0;
            foreach (VisualPersonaje visual in Object.FindObjectsByType<VisualPersonaje>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                GameObject fuente = PrefabUtility.GetCorrespondingObjectFromSource(visual.gameObject);
                if (fuente == null || fuente.name != VisualTino)
                {
                    continue;
                }
                cuantos++;
                Transform modelo = visual.transform.Find(RutaModelo);
                salida.AppendLine("  " + visual.name + ": raíz " + visual.transform.localScale.ToString("F3") + ", Modelo " + (modelo != null ? modelo.localScale.ToString("F3") : "?")
                    + ", prefab Modelo " + EscalaDelModelo(VisualTino) + (EscalaEfectivaInstancia(visual.transform) > 0f ? ", efectiva " + EscalaEfectivaInstancia(visual.transform).ToString("0.####", CultureInfo.InvariantCulture) : string.Empty));
            }
            if (cuantos == 0)
            {
                salida.AppendLine("  (sin instancias de " + VisualTino + " en " + escena.name + ")");
            }
        }

        // ---------- Aplicar ----------

        [MenuItem("Mecha Cyberpunk/T-1552/2 Aplicar alturas")]
        public static void Aplicar()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError(Etiqueta + "no se aplica en Play.");
                return;
            }
            var informe = new StringBuilder();
            informe.AppendLine("T-1552 aplicar: visual | escala antes | escala después | factor | motivo");
            float escalaTino = LeerEscalaEfectivaDeTino(informe);
            int cambiados = 0;
            int iguales = 0;
            int fallos = 0;
            foreach (string visual in EscalasHumanos.Prefabs)
            {
                string ruta = CarpetaVisuales + visual + ".prefab";
                float baseEscena = visual == VisualTino ? escalaTino : 0f;
                int resultado = EscalarVisual(ruta, visual, baseEscena, informe);
                if (resultado > 0) { cambiados++; } else if (resultado == 0) { iguales++; } else { fallos++; }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            informe.AppendLine("== Escena ==");
            AplicarEscena(informe);
            informe.AppendLine("== Cápsulas ==");
            int capsulas = AplicarCapsulas(informe);
            Escribir(RutaInformeAplicar, informe.ToString());
            Debug.Log(Etiqueta + "prefabs: " + cambiados + " escalados, " + iguales + " sin cambio o ya aplicados, " + fallos + " con error; " + capsulas + " cápsulas ajustadas. Detalle en " + RutaInformeAplicar);
        }

        /// <summary>Escala el Modelo de un prefab Visual_. Devuelve 1 si lo cambió, 0 si no hacía falta, -1 si falló.</summary>
        private static int EscalarVisual(string ruta, string visual, float baseEscena, StringBuilder informe)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (asset == null)
            {
                Debug.LogError(Etiqueta + "no existe " + ruta);
                informe.AppendLine(visual + " | NO EXISTE");
                return -1;
            }
            LeerEtiquetaDePrefab(ruta, out float baseEt, out float finEt);
            MedidaAltura bind = MedirBind(ruta);
            GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                Transform modelo = contenido.transform.Find(RutaModelo);
                if (modelo == null)
                {
                    Debug.LogError(Etiqueta + ruta + " no tiene " + RutaModelo);
                    informe.AppendLine(visual + " | sin " + RutaModelo);
                    return -1;
                }
                Vector3 actual = modelo.localScale;
                Decision d = Decidir(visual, actual.y, baseEt, finEt, baseEscena, bind.Valida ? bind.Altura : 0f);
                if (d.Cambia)
                {
                    modelo.localScale = baseEscena > 0f ? Vector3.one * d.Escala : actual * (d.Escala / actual.y);
                    PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
                }
                if (d.Motivo.StartsWith("ya") || d.Cambia)
                {
                    PonerEtiqueta(asset, CrearEtiqueta(d.Base, d.Escala));
                }
                informe.AppendLine(visual + " | " + actual.y.ToString("0.####", CultureInfo.InvariantCulture) + " | " + d.Escala.ToString("0.####", CultureInfo.InvariantCulture) + " | "
                    + EscalasHumanos.Factor(visual).ToString("0.000", CultureInfo.InvariantCulture) + " | " + d.Motivo);
                return d.Cambia ? 1 : 0;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        private static void PonerEtiqueta(Object asset, string etiqueta)
        {
            List<string> etiquetas = AssetDatabase.GetLabels(asset).Where(e => !e.StartsWith(PrefijoEtiquetaAsset)).ToList();
            etiquetas.Add(etiqueta);
            AssetDatabase.SetLabels(asset, etiquetas.ToArray());
        }

        private static bool LeerEtiquetaDePrefab(string ruta, out float escalaBase, out float escalaFinal)
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(ruta);
            if (asset == null)
            {
                escalaBase = 0f;
                escalaFinal = 0f;
                return false;
            }
            return LeerEtiqueta(AssetDatabase.GetLabels(asset), out escalaBase, out escalaFinal);
        }

        /// <summary>
        /// Tino en Ciudad_Prologo trae la escala pisada en su instancia (1.67 contra 1.49 del prefab): lo que se ve es la de la instancia, así que el factor (0.939) se aplica sobre esa.
        /// Devuelve la escala efectiva del Modelo (raíz de la instancia por Modelo) o 0 si no hay instancia pisada.
        /// </summary>
        private static float LeerEscalaEfectivaDeTino(StringBuilder informe)
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaVisuales + VisualTino + ".prefab");
            float efectivaPrefab = prefab != null && prefab.transform.Find(RutaModelo) != null ? prefab.transform.localScale.y * prefab.transform.Find(RutaModelo).localScale.y : 0f;
            float resultado = 0f;
            foreach (VisualPersonaje visual in Object.FindObjectsByType<VisualPersonaje>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                GameObject fuente = PrefabUtility.GetCorrespondingObjectFromSource(visual.gameObject);
                if (fuente == null || fuente.name != VisualTino)
                {
                    continue;
                }
                float efectiva = EscalaEfectivaInstancia(visual.transform);
                informe.AppendLine("Tino " + visual.name + " en " + escena.name + ": escala efectiva " + efectiva.ToString("0.####", CultureInfo.InvariantCulture)
                    + " (prefab " + efectivaPrefab.ToString("0.####", CultureInfo.InvariantCulture) + ")");
                if (resultado <= 0f && efectiva > 0f && efectivaPrefab > 0f && Mathf.Abs(efectiva - efectivaPrefab) > MargenEscalaIgual)
                {
                    resultado = prefab.transform.Find(RutaModelo).localScale.y * efectiva / efectivaPrefab;
                }
            }
            return resultado;
        }

        private static float EscalaEfectivaInstancia(Transform visual)
        {
            Transform modelo = visual.Find(RutaModelo);
            return modelo != null ? visual.localScale.y * modelo.localScale.y : 0f;
        }

        /// <summary>Deja a Tino con la escala del prefab en todas sus instancias de la escena (quita la escala pisada de la raíz y del Modelo) y guarda la escena.</summary>
        public static void AplicarEscena(StringBuilder informe)
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            int revertidos = 0;
            foreach (VisualPersonaje visual in Object.FindObjectsByType<VisualPersonaje>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                GameObject fuente = PrefabUtility.GetCorrespondingObjectFromSource(visual.gameObject);
                if (fuente == null)
                {
                    continue;
                }
                Transform modelo = visual.transform.Find(RutaModelo);
                Transform modeloFuente = fuente.transform.Find(RutaModelo);
                if (fuente.name == VisualTino)
                {
                    revertidos += QuitarEscalaPisada(visual.transform, fuente.transform, informe) ? 1 : 0;
                    if (modelo != null && modeloFuente != null)
                    {
                        revertidos += QuitarEscalaPisada(modelo, modeloFuente, informe) ? 1 : 0;
                    }
                }
                else if (EscalasHumanos.Objetivo(fuente.name) > 0f && modelo != null && modeloFuente != null
                    && (modelo.localScale - modeloFuente.localScale).sqrMagnitude > MargenEscalaIgual * MargenEscalaIgual)
                {
                    informe.AppendLine("AVISO " + visual.name + " (" + fuente.name + "): trae el Modelo pisado en la instancia (" + modelo.localScale.ToString("F3") + " contra " + modeloFuente.localScale.ToString("F3")
                        + " del prefab); la altura nueva solo vale con la del prefab. No se toca (T-1552 solo pidió Tino).");
                }
                for (Transform padre = visual.transform.parent; padre != null; padre = padre.parent)
                {
                    if (Mathf.Abs(padre.localScale.x - 1f) > MargenEscalaIgual || Mathf.Abs(padre.localScale.y - 1f) > MargenEscalaIgual)
                    {
                        informe.AppendLine("AVISO " + visual.name + " cuelga de " + padre.name + " con escala " + padre.localScale.ToString("F3") + ": hereda su tamaño.");
                    }
                }
            }
            if (revertidos > 0)
            {
                EditorSceneManager.MarkSceneDirty(escena);
                EditorSceneManager.SaveScene(escena);
            }
            informe.AppendLine("escena " + escena.name + ": " + revertidos + " escalas pisadas de Tino quitadas.");
        }

        private static bool QuitarEscalaPisada(Transform instancia, Transform fuente, StringBuilder informe)
        {
            if ((instancia.localScale - fuente.localScale).sqrMagnitude < MargenEscalaIgual * MargenEscalaIgual)
            {
                return false;
            }
            Vector3 antes = instancia.localScale;
            SerializedProperty propiedad = new SerializedObject(instancia).FindProperty("m_LocalScale");
            PrefabUtility.RevertPropertyOverride(propiedad, InteractionMode.AutomatedAction);
            informe.AppendLine(instancia.root.name + "/" + instancia.name + ": escala " + antes.ToString("F3") + " -> " + instancia.localScale.ToString("F3") + " (la del prefab).");
            return true;
        }

        /// <summary>Ajusta cápsula y factor del cuerpo de los agentes de IA a la altura nueva (se puede repetir: calcula desde la tabla). Devuelve cuántos prefabs tocó.</summary>
        public static int AplicarCapsulas(StringBuilder informe)
        {
            int tocados = 0;
            foreach (FilaCapsula fila in FilasCapsula())
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(fila.Ruta) == null)
                {
                    informe.AppendLine(Path.GetFileNameWithoutExtension(fila.Ruta) + ": no existe, se omite");
                    continue;
                }
                GameObject contenido = PrefabUtility.LoadPrefabContents(fila.Ruta);
                try
                {
                    CharacterController cc = contenido.GetComponent<CharacterController>();
                    PandilleroAPie pandillero = contenido.GetComponent<PandilleroAPie>();
                    if (cc == null || pandillero == null)
                    {
                        informe.AppendLine(contenido.name + ": sin CharacterController o PandilleroAPie, se omite");
                        continue;
                    }
                    var so = new SerializedObject(pandillero);
                    SerializedProperty propiedadCuerpo = so.FindProperty("escalaCuerpo");
                    MedidasCapsula m = CalcularCapsula(EscalasHumanos.Objetivo(fila.Visual), fila.Completa, cc.height, cc.radius, cc.stepOffset, propiedadCuerpo.floatValue);
                    cc.height = m.Alto;
                    cc.radius = m.Radio;
                    cc.center = new Vector3(cc.center.x, m.CentroY, cc.center.z);
                    cc.stepOffset = m.Paso;
                    propiedadCuerpo.floatValue = m.EscalaCuerpo;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(contenido, fila.Ruta);
                    informe.AppendLine(contenido.name + ": alto " + Formato(m.Alto) + ", radio " + Formato(m.Radio) + ", paso " + Formato(m.Paso) + ", escalaCuerpo " + Formato(m.EscalaCuerpo));
                    tocados++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contenido);
                }
            }
            AssetDatabase.SaveAssets();
            return tocados;
        }

        // ---------- Afinar ----------

        /// <summary>
        /// Visuales cuya pose de bind no sirve para medir (woman2 viene agachada: bind 1.84 pero animada en Idle mide 2.17): Afinar los lleva a la altura nueva proporcional
        /// (nueva x 1.80 / 1.84) midiendo la malla animada en Idle.
        /// </summary>
        public static readonly string[] AfinadosPorIdle = { "Visual_woman2" };

        public static bool SeAfinaPorIdle(string visual) => System.Array.IndexOf(AfinadosPorIdle, visual) >= 0;

        private static MedidaAltura MedirIdle(string ruta)
        {
            EscalaHumanosT1521.Medida m = EscalaHumanosT1521.MedirPrefab(ruta);
            return new MedidaAltura(m.Valida, m.Malla, m.Problema);
        }

        /// <summary>
        /// Solo si hace falta: a quien ya tiene la etiqueta de Aplicar y mide en bind más de 5 cm lejos de su altura nueva (porque la tabla de Blender y la malla no coinciden),
        /// le pone la escala que lo lleva a la nueva midiendo la malla. Actualiza su etiqueta (la base se conserva).
        /// </summary>
        [MenuItem("Mecha Cyberpunk/T-1552/3 Afinar por medida")]
        public static void Afinar()
        {
            var informe = new StringBuilder();
            informe.AppendLine("T-1552 afinar: visual | bind antes | escala antes | escala después");
            int afinados = 0;
            foreach (string visual in EscalasHumanos.Prefabs)
            {
                string ruta = CarpetaVisuales + visual + ".prefab";
                if (!LeerEtiquetaDePrefab(ruta, out float baseEt, out _))
                {
                    continue;
                }
                bool porIdle = SeAfinaPorIdle(visual);
                MedidaAltura bind = porIdle ? MedirIdle(ruta) : MedirBind(ruta);
                float objetivo = porIdle ? EscalasHumanos.Objetivo(visual) * EscalasHumanos.AlturaJugadorAnimada / EscalasHumanos.AlturaJugador : EscalasHumanos.Objetivo(visual);
                if (!bind.Valida || Mathf.Abs(bind.Altura - objetivo) <= ToleranciaAfinar)
                {
                    continue;
                }
                float factor = EscalasHumanos.FactorCorreccion(bind.Altura, objetivo);
                if (Mathf.Approximately(factor, 1f))
                {
                    informe.AppendLine(visual + " | " + Formato(bind.Altura) + " | la medida no es de fiar; no se toca");
                    continue;
                }
                GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
                try
                {
                    Transform modelo = contenido.transform.Find(RutaModelo);
                    if (modelo == null) { continue; }
                    float antes = modelo.localScale.y;
                    modelo.localScale *= factor;
                    PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
                    PonerEtiqueta(AssetDatabase.LoadAssetAtPath<Object>(ruta), CrearEtiqueta(baseEt, modelo.localScale.y));
                    informe.AppendLine(visual + " | " + Formato(bind.Altura) + " | " + antes.ToString("0.####", CultureInfo.InvariantCulture) + " | " + modelo.localScale.y.ToString("0.####", CultureInfo.InvariantCulture));
                    afinados++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contenido);
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Escribir("Logs/t1552-afinar.txt", informe.ToString());
            Debug.Log(Etiqueta + "afinar: " + afinados + " prefabs corregidos por medida.");
        }

        // ---------- Medición ----------

        /// <summary>Resultado de medir una altura.</summary>
        public readonly struct MedidaAltura
        {
            public readonly bool Valida;
            public readonly float Altura;
            public readonly string Problema;

            public MedidaAltura(bool valida, float altura, string problema)
            {
                Valida = valida;
                Altura = altura;
                Problema = problema;
            }
        }

        /// <summary>
        /// Altura de un prefab en pose de bind: el Animator se apaga y se hornean las mallas con piel como están (de la planta al punto más alto). Sin skinned mesh, usa los bounds de los renderers.
        /// </summary>
        public static MedidaAltura MedirBind(string ruta)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (prefab == null)
            {
                return new MedidaAltura(false, 0f, "no existe " + ruta);
            }
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                instancia.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                foreach (Animator animador in instancia.GetComponentsInChildren<Animator>(true))
                {
                    animador.enabled = false;
                }
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
                    foreach (Renderer r in instancia.GetComponentsInChildren<Renderer>(false))
                    {
                        bajo = Mathf.Min(bajo, r.bounds.min.y);
                        alto = Mathf.Max(alto, r.bounds.max.y);
                    }
                }
                return alto > bajo ? new MedidaAltura(true, alto - bajo, null) : new MedidaAltura(false, 0f, "sin mallas");
            }
            finally
            {
                Object.DestroyImmediate(instancia);
            }
        }

        private static string EscalaDelModelo(string visual)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaVisuales + visual + ".prefab");
            Transform modelo = prefab != null ? prefab.transform.Find(RutaModelo) : null;
            return modelo != null ? modelo.localScale.y.ToString("0.####", CultureInfo.InvariantCulture) : "?";
        }

        private static string Formato(float valor) => valor.ToString("0.00", CultureInfo.InvariantCulture);

        private static void Escribir(string ruta, string texto)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllText(ruta, texto);
        }
    }
}

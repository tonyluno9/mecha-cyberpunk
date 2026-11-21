using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1553: los robots de Nova tras la revisión de Tony del 20-nov (docs/tareas-unity-robots-nova-20nov.md). Hace en Unity lo que Blender no puede:
    /// 1) el Avatar del E18 se rehace tras el arreglo del rig (avatar regenerado, brazos a T); 2) los clips E16_CaminarEscudo y E18_* viven en la carpeta Clips de su personaje y los cuatro
    /// que traen el rig del personaje (E16_CaminarEscudo, E18_Caminar, E18_Correr, E18_Golpe_Der) se importan Humanoid con el Avatar y la escala del personaje;
    /// 3) overrides (ver ClipsNpcNovaT1506), config con el clip de vuelo, prefabs: E16 con cañón y escudo, E18 con su conducta y sin arma, y la W10 del E15 bien orientada.
    /// Se pueden repetir. Orden: <see cref="Diagnostico"/> (solo lee y reporta), <see cref="Aplicar"/>, <see cref="Diagnostico"/> otra vez.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.RobotsNovaT1553.Aplicar (el informe queda en Logs/robots-nova-t1553.txt).
    /// </summary>
    public static class RobotsNovaT1553
    {
        public const string RutaPrefabE15 = "Assets/_Proyecto/IA/Prefabs/GuardiaNova_E15_CentinelaNova_a.prefab";
        public const string RutaPrefabE15b = "Assets/_Proyecto/IA/Prefabs/GuardiaNova_E15_CentinelaNova_b.prefab";
        public const string RutaPrefabE16 = "Assets/_Proyecto/IA/Prefabs/GuardiaPesado_E16_RobotPesadoNova_a.prefab";
        public const string RutaPrefabE16b = "Assets/_Proyecto/IA/Prefabs/GuardiaPesado_E16_RobotPesadoNova_b.prefab";
        public const string RutaPrefabE18 = "Assets/_Proyecto/IA/Prefabs/GuardiaNova_E18_EliteNova_a.prefab";
        public const string RutaInforme = "Logs/robots-nova-t1553.txt";
        // W10 del E15: la boca (+Z del arma) tiene que mirar hacia el frente del personaje con E15_Caminar y E15_Correr.
        public static readonly string[] ClipsArmaE15 = { "E15_Caminar", "E15_Correr" };
        public const int MuestrasPorClip = 8;
        /// <summary>Desvío máximo del cañón de la W10 respecto al frente (grados, en el plano) y cabeceo máximo (grados) para darla por bien orientada.</summary>
        public const float ToleranciaGiroArma = 25f;
        public const float ToleranciaCabeceoArma = 35f;
        public const string NombreBoca = "Boca";
        /// <summary>Desvío máximo (grados) entre la altura del brazo izquierdo y la del derecho, y de cada brazo a la horizontal, para dar el avatar por en T.</summary>
        public const float ToleranciaBrazoT = 1.5f;


        // ------------------------------------------------------------------ Avatar del E18

        /// <summary>Elevación (grados sobre la horizontal, en el marco del personaje) del brazo de un lado ("Left" o "Right"): del hombro al codo. En T vale 0.</summary>
        public static float ElevacionBrazo(GameObject modelo, string lado, out float desvioAdelante)
        {
            desvioAdelante = 0f;
            Transform brazo = Buscar(modelo.transform, "mixamorig:" + lado + "Arm");
            Transform codo = Buscar(modelo.transform, "mixamorig:" + lado + "ForeArm");
            if (brazo == null || codo == null)
            {
                return float.NaN;
            }
            Vector3 d = modelo.transform.InverseTransformDirection((codo.position - brazo.position).normalized);
            desvioAdelante = Mathf.Atan2(d.z, Mathf.Abs(d.x)) * Mathf.Rad2Deg;
            return Mathf.Asin(Mathf.Clamp(d.y, -1f, 1f)) * Mathf.Rad2Deg;
        }

        private static Transform Buscar(Transform raiz, string nombre)
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == nombre) { return t; }
            }
            return null;
        }

        /// <summary>Informe de los brazos del modelo (elevación izquierda y derecha en grados) o null si no se pudo medir.</summary>
        public static (float izquierdo, float derecho)? MedirBrazos(string rutaModelo)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rutaModelo);
            if (prefab == null) { return null; }
            GameObject instancia = Object.Instantiate(prefab);
            try
            {
                float i = ElevacionBrazo(instancia, "Left", out float _);
                float d = ElevacionBrazo(instancia, "Right", out float _);
                return float.IsNaN(i) || float.IsNaN(d) ? ((float, float)?)null : (i, d);
            }
            finally
            {
                Object.DestroyImmediate(instancia);
            }
        }

        /// <summary>True si los dos brazos están a menos de <see cref="ToleranciaBrazoT"/> grados de la horizontal.</summary>
        public static bool BrazosEnT((float izquierdo, float derecho)? brazos) =>
            brazos.HasValue && Mathf.Abs(brazos.Value.izquierdo) <= ToleranciaBrazoT && Mathf.Abs(brazos.Value.derecho) <= ToleranciaBrazoT;

        /// <summary>
        /// Rehace el Avatar del E18 desde cero tras el arreglo del rig (mochila en Spine2, brazos centrados), el equivalente a "Create From This Model": vacía el mapeo Humanoid y el
        /// esqueleto guardados en la descripción (estaban hechos con el rig viejo) con autoGenerateAvatarMappingIfUnspecified activado, así Unity los vuelve a armar del modelo
        /// actual al reimportar. No fuerza la pose T por código (Unity no lo expone): si los brazos siguen a más de <see cref="ToleranciaBrazoT"/> grados de la horizontal, el informe
        /// lo dice y hay que hacer a mano Rig, Configure, Pose, Enforce T-Pose, Apply. Devuelve el informe.
        /// </summary>
        public static string RehacerAvatarE18()
        {
            var sb = new StringBuilder("E18 avatar:\n");
            var importador = AssetImporter.GetAtPath(ClipsRobotsNovaT1553.RutaModeloE18) as ModelImporter;
            if (importador == null)
            {
                return "E18 avatar: falta " + ClipsRobotsNovaT1553.RutaModeloE18 + "\n";
            }
            sb.AppendLine("  brazos antes (elevación L/R): " + Formato(MedirBrazos(ClipsRobotsNovaT1553.RutaModeloE18)));
            importador.animationType = ModelImporterAnimationType.Human;
            importador.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importador.autoGenerateAvatarMappingIfUnspecified = true;
            HumanDescription d = importador.humanDescription;
            sb.AppendLine("  descripción guardada: " + d.human.Length + " huesos humanoides, " + d.skeleton.Length + " nodos (se vacía)");
            d.human = new HumanBone[0];
            d.skeleton = new SkeletonBone[0];
            importador.humanDescription = d;
            importador.SaveAndReimport();
            (float izquierdo, float derecho)? despues = MedirBrazos(ClipsRobotsNovaT1553.RutaModeloE18);
            sb.AppendLine("  brazos tras rehacer (elevación L/R): " + Formato(despues) + (BrazosEnT(despues) ? " (en T)" : " (FUERA DE T: hacer a mano Rig, Configure, Pose, Enforce T-Pose, Apply en E18_EliteNova_a.fbx)"));
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ClipsRobotsNovaT1553.RutaModeloE18).OfType<Avatar>().FirstOrDefault();
            sb.AppendLine("  avatar: " + (avatar == null ? "NO HAY" : (avatar.isValid && avatar.isHuman ? "válido y humanoide" : "INVÁLIDO")));
            return sb.ToString();
        }

        private static string Formato((float izquierdo, float derecho)? b) => b.HasValue ? b.Value.izquierdo.ToString("F1") + " / " + b.Value.derecho.ToString("F1") : "no se pudo medir";

        // ------------------------------------------------------------------ W10 del E15

        /// <summary>Resultado de medir la W10 muestreando un clip: direcciones de la boca en el marco del personaje y giro local sugerido para corregirla.</summary>
        public readonly struct MedidaArma
        {
            public readonly string Clip;
            public readonly int Muestras;
            public readonly float GiroMaximo;
            public readonly float CabeceoMedio;
            public readonly float CabeceoMaximo;
            public readonly Vector3 DireccionMedia;
            public readonly Quaternion CorreccionLocal;

            public MedidaArma(string clip, int muestras, float giroMaximo, float cabeceoMedio, float cabeceoMaximo, Vector3 direccionMedia, Quaternion correccionLocal)
            {
                Clip = clip;
                Muestras = muestras;
                GiroMaximo = giroMaximo;
                CabeceoMedio = cabeceoMedio;
                CabeceoMaximo = cabeceoMaximo;
                DireccionMedia = direccionMedia;
                CorreccionLocal = correccionLocal;
            }

            public bool Bien => Muestras > 0 && GiroMaximo <= ToleranciaGiroArma && CabeceoMaximo <= ToleranciaCabeceoArma;
        }

        /// <summary>
        /// Mide hacia dónde apunta la boca de la W10 (hijo "Boca") en el prefab <paramref name="enemigo"/> cuando juega <paramref name="clip"/> (muestras repartidas en su ciclo). El giro
        /// es el desvío en el plano respecto al frente (+Z del personaje); el cabeceo, el ángulo sobre la horizontal. CorreccionLocal es el giro (en el marco de la mano) que
        /// llevaría la boca al frente a nivel, promediado sobre las muestras. Devuelve null si falta el arma, el animador o el clip.
        /// </summary>
        public static MedidaArma? MedirArma(GameObject enemigo, AnimationClip clip)
        {
            Animator animador = enemigo.GetComponentInChildren<Animator>(true);
            Transform mano = enemigo.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == CreadorVisualTripo.NombreSocket);
            Transform arma = mano != null ? mano.Find(EscalaNovaT1497.NombreArma) : null;
            Transform boca = arma != null ? arma.Find(NombreBoca) : null;
            if (animador == null || boca == null || clip == null)
            {
                return null;
            }
            Transform raiz = animador.transform;
            float giroMax = 0f, cabeceoMax = 0f, cabeceoSuma = 0f;
            Vector3 suma = Vector3.zero;
            Vector4 acumulado = Vector4.zero;
            Quaternion primera = Quaternion.identity;
            int n = Mathf.Max(2, MuestrasPorClip);
            for (int i = 0; i < n; i++)
            {
                clip.SampleAnimation(animador.gameObject, clip.length * i / n);
                Vector3 d = raiz.InverseTransformDirection((boca.position - arma.position).normalized);
                float giro = Mathf.Abs(Mathf.Atan2(d.x, d.z)) * Mathf.Rad2Deg;
                float cabeceo = Mathf.Asin(Mathf.Clamp(d.y, -1f, 1f)) * Mathf.Rad2Deg;
                giroMax = Mathf.Max(giroMax, giro);
                cabeceoMax = Mathf.Max(cabeceoMax, Mathf.Abs(cabeceo));
                cabeceoSuma += cabeceo;
                suma += d;
                // Giro que lleva esta dirección al frente a nivel, expresado en el marco de la mano (la mano es el padre del socket del arma).
                Quaternion mundo = Quaternion.FromToRotation(raiz.TransformDirection(d), raiz.TransformDirection(Vector3.forward));
                Quaternion local = Quaternion.Inverse(mano.rotation) * mundo * mano.rotation;
                if (i == 0) { primera = local; }
                if (Quaternion.Dot(primera, local) < 0f) { local = new Quaternion(-local.x, -local.y, -local.z, -local.w); }
                acumulado += new Vector4(local.x, local.y, local.z, local.w);
            }
            var q = new Quaternion(acumulado.x, acumulado.y, acumulado.z, acumulado.w);
            float norma = Mathf.Sqrt(Quaternion.Dot(q, q));
            q = norma > 1e-5f ? new Quaternion(q.x / norma, q.y / norma, q.z / norma, q.w / norma) : Quaternion.identity;
            return new MedidaArma(clip.name, n, giroMax, cabeceoSuma / n, cabeceoMax, (suma / n).normalized, q);
        }

        /// <summary>Mide la W10 del prefab de enemigo E15 con cada clip de <see cref="ClipsArmaE15"/>.</summary>
        public static List<MedidaArma> MedirArmaE15(string rutaPrefab)
        {
            var salida = new List<MedidaArma>();
            GameObject contenido = PrefabUtility.LoadPrefabContents(rutaPrefab);
            try
            {
                foreach (string nombre in ClipsArmaE15)
                {
                    MedidaArma? m = MedirArma(contenido, ConstructorControladorBase.Clip(nombre));
                    if (m.HasValue) { salida.Add(m.Value); }
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            return salida;
        }

        /// <summary>
        /// Si la W10 del E15 no apunta bien con E15_Caminar y E15_Correr calcula el giro de corrección (promedio de las dos medidas), lo guarda en
        /// ConfigClipsNovaNpc.GiroArmaE15 y rehace las armas de los prefabs E15 con EscalaNovaT1497.PonerArma (que ya lee ese giro). Devuelve el informe.
        /// </summary>
        public static string CorregirArmaE15()
        {
            var sb = new StringBuilder("W10 del E15:\n");
            List<MedidaArma> medidas = MedirArmaE15(RutaPrefabE15);
            if (medidas.Count == 0)
            {
                return "W10 del E15: no se pudo medir (falta la W10 en ManoDer, el animador o los clips E15_Caminar y E15_Correr)\n";
            }
            foreach (MedidaArma m in medidas)
            {
                sb.AppendLine("  " + m.Clip + ": giro máx " + m.GiroMaximo.ToString("F1") + " grados, cabeceo medio " + m.CabeceoMedio.ToString("F1") + ", máx " + m.CabeceoMaximo.ToString("F1")
                    + (m.Bien ? " (bien)" : " (MAL)"));
            }
            if (medidas.All(m => m.Bien))
            {
                sb.AppendLine("  bien orientada: sin corrección");
                return sb.ToString();
            }
            var config = AssetDatabase.LoadAssetAtPath<ConfigClipsNovaNpc>(ClipsNpcNovaT1506.RutaConfig);
            if (config == null)
            {
                sb.AppendLine("  falta " + ClipsNpcNovaT1506.RutaConfig + " (corre ClipsNpcNovaT1506.Aplicar antes)");
                return sb.ToString();
            }
            Vector3 anterior = config.GiroArmaE15;
            Quaternion promedio = Quaternion.Slerp(medidas[0].CorreccionLocal, medidas[medidas.Count - 1].CorreccionLocal, 0.5f);
            // La corrección medida parte del giro que ya tiene la W10 (que ya incluye el giro extra anterior): se compone encima.
            Vector3 nuevo = (promedio * Quaternion.Euler(anterior)).eulerAngles;
            config.ConfigurarGiroArmaE15(nuevo);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            RehacerArmasE15();
            sb.AppendLine("  giro extra de la W10 del E15: " + anterior + " -> " + nuevo + " (ConfigClipsNovaNpc.GiroArmaE15)");
            List<MedidaArma> despues = MedirArmaE15(RutaPrefabE15);
            foreach (MedidaArma m in despues)
            {
                sb.AppendLine("  tras corregir, " + m.Clip + ": giro máx " + m.GiroMaximo.ToString("F1") + ", cabeceo máx " + m.CabeceoMaximo.ToString("F1") + (m.Bien ? " (bien)" : " (SIGUE MAL: revisar a mano)"));
            }
            return sb.ToString();
        }

        private static void RehacerArmasE15()
        {
            var arma = AssetDatabase.LoadAssetAtPath<GameObject>(EscalaNovaT1497.RutaArmaPrefab);
            if (arma == null) { return; }
            foreach (string ruta in new[] { RutaPrefabE15, RutaPrefabE15b })
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) == null) { continue; }
                string variante = Path.GetFileNameWithoutExtension(ruta).Replace("GuardiaNova_", "");
                GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
                try
                {
                    float factor = EscalasNova.Factor(EscalasNova.Altura(variante));
                    EscalaNovaT1497.PonerArma(contenido, arma, factor, EscalaNovaT1497.GiroExtraArma(variante));
                    PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contenido);
                }
            }
        }

        // ------------------------------------------------------------------ Prefabs de IA

        /// <summary>E16 (a y b): enciende el cañón y el escudo en su ConductaGuardiaPesado.</summary>
        public static void AplicarPrefabsE16()
        {
            foreach (string ruta in new[] { RutaPrefabE16, RutaPrefabE16b })
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) == null) { continue; }
                GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
                try
                {
                    ConductaGuardiaPesado conducta = contenido.GetComponent<ConductaGuardiaPesado>();
                    if (conducta == null)
                    {
                        Debug.LogError("[Personaje] T-1553: " + ruta + " no tiene ConductaGuardiaPesado");
                        continue;
                    }
                    conducta.ConfigurarRobot(true, true);
                    PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contenido);
                }
            }
        }

        /// <summary>E18: agrega su ConductaEliteNova y le quita la W10 (decisión de Tony 20-nov: sin arma).</summary>
        public static void AplicarPrefabE18()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabE18) == null) { return; }
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefabE18);
            try
            {
                if (contenido.GetComponent<ConductaEliteNova>() == null)
                {
                    contenido.AddComponent<ConductaEliteNova>();
                }
                EscalaNovaT1497.QuitarArma(contenido);
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefabE18);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        // ------------------------------------------------------------------ Diagnóstico y aplicar

        [MenuItem("Mecha Cyberpunk/T-1553/1 Diagnóstico de robots Nova")]
        public static void Diagnostico()
        {
            var sb = new StringBuilder("[T-1553] Diagnóstico de robots Nova\n");
            AssetDatabase.Refresh();
            sb.AppendLine("Clips (ubicación, importador, avatar):");
            foreach (string nombre in ClipsRobotsNovaT1553.ClipsDeRobot)
            {
                string ruta = ClipsRobotsNovaT1553.RutaClip(nombre);
                var imp = AssetImporter.GetAtPath(ruta) as ModelImporter;
                if (imp == null)
                {
                    sb.AppendLine("  " + nombre + ": NO ESTÁ en " + ruta + (File.Exists(ImportadorClipsPJ01.Carpeta + nombre + ".fbx") ? " (sigue en PJ01/Clips)" : ""));
                    continue;
                }
                Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Avatar>().FirstOrDefault();
                AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"));
                sb.AppendLine("  " + nombre + ": " + imp.animationType + ", avatar " + (avatar == null ? "ninguno" : (avatar.isValid && avatar.isHuman ? "válido" : "INVÁLIDO"))
                    + ", escala " + imp.globalScale + ", clip " + (clip != null ? clip.name + " " + clip.length.ToString("F2") + " s, bucle " + clip.isLooping : "NO HAY")
                    + (ClipsRobotsNovaT1553.TieneRigPropio(nombre) ? ", rig propio, " + (imp.avatarSetup == ModelImporterAvatarSetup.CopyFromOther ? "avatar copiado del personaje" : "AVATAR PROPIO (falta copiar el del personaje: repetir Aplicar)") : ", rig PJ01"));
            }
            sb.AppendLine("Brazos del E18 (elevación L/R en grados, 0 = en T): " + Formato(MedirBrazos(ClipsRobotsNovaT1553.RutaModeloE18)));
            Avatar a18 = AssetDatabase.LoadAllAssetsAtPath(ClipsRobotsNovaT1553.RutaModeloE18).OfType<Avatar>().FirstOrDefault();
            sb.AppendLine("Avatar E18: " + (a18 == null ? "NO HAY" : (a18.isValid && a18.isHuman ? "válido y humanoide" : "INVÁLIDO")));
            DiagnosticoOverrides(sb);
            DiagnosticoPrefabs(sb);
            sb.AppendLine("W10 del E15:");
            List<MedidaArma> medidas = MedirArmaE15(RutaPrefabE15);
            if (medidas.Count == 0) { sb.AppendLine("  no se pudo medir"); }
            foreach (MedidaArma m in medidas)
            {
                sb.AppendLine("  " + m.Clip + ": giro máx " + m.GiroMaximo.ToString("F1") + " grados, cabeceo medio " + m.CabeceoMedio.ToString("F1") + ", máx " + m.CabeceoMaximo.ToString("F1") + (m.Bien ? " (bien)" : " (MAL: Aplicar la corrige)"));
            }
            Escribir(sb);
        }

        private static void DiagnosticoOverrides(StringBuilder sb)
        {
            sb.AppendLine("Overrides:");
            foreach ((string nombre, string clipBase, string esperado) in new[]
            {
                ("E16", "Loco_Caminar", "E16_Caminar"), ("E16", "Loco_Caminar_ConImplante", "E16_CaminarEscudo"), ("E16", "Loco_Correr_ConImplante", "E16_CaminarEscudo"),
                ("E18", "Loco_Caminar", "E18_Caminar"), ("E18", "Loco_Correr", "E18_Correr"), ("E18", "Punos_GolpeDer", "E18_Golpe_Der"),
            })
            {
                var o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ClipsNpcNovaT1506.RutaOverrideExtra(nombre));
                string real = "sin override";
                if (o != null)
                {
                    var pares = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                    o.GetOverrides(pares);
                    KeyValuePair<AnimationClip, AnimationClip> par = pares.FirstOrDefault(p => p.Key != null && p.Key.name == clipBase);
                    real = par.Value != null ? par.Value.name : "(vacío)";
                }
                sb.AppendLine("  Override_" + nombre + " " + clipBase + " -> " + real + (real == esperado ? "" : "   <- esperado " + esperado));
            }
        }

        private static void DiagnosticoPrefabs(StringBuilder sb)
        {
            sb.AppendLine("Prefabs de IA:");
            foreach (string ruta in new[] { RutaPrefabE16, RutaPrefabE16b })
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                ConductaGuardiaPesado c = p != null ? p.GetComponent<ConductaGuardiaPesado>() : null;
                sb.AppendLine("  " + Path.GetFileNameWithoutExtension(ruta) + ": " + (p == null ? "no existe" : c == null ? "SIN ConductaGuardiaPesado" : "cañón " + c.DisparaCanon + ", escudo " + c.CaminaConEscudo));
            }
            var e18 = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabE18);
            if (e18 == null)
            {
                sb.AppendLine("  E18: no existe " + RutaPrefabE18);
                return;
            }
            Transform mano = e18.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == CreadorVisualTripo.NombreSocket);
            sb.AppendLine("  E18: ConductaEliteNova " + (e18.GetComponent<ConductaEliteNova>() != null) + ", W10 en la mano " + (mano != null && mano.Find(EscalaNovaT1497.NombreArma) != null) + " (debe ser false)");
        }

        [MenuItem("Mecha Cyberpunk/T-1553/2 Aplicar robots Nova")]
        public static void Aplicar()
        {
            var sb = new StringBuilder("[T-1553] Aplicar robots Nova\n");
            AssetDatabase.Refresh();
            Paso(sb, "Avatar del E18", () => sb.Append(RehacerAvatarE18()));
            Paso(sb, "Importar los clips de E16 y E18", () =>
            {
                foreach (string nombre in ClipsRobotsNovaT1553.ClipsDeRobot) { ClipsNpcNovaT1506.ImportarHumanoide(nombre); }
            });
            Paso(sb, "Avatar del personaje en los clips con rig propio", () =>
            {
                foreach (string nombre in ClipsRobotsNovaT1553.ClipsConRigPropio)
                {
                    ClipsRobotsNovaT1553.ImportarRigPropio(nombre);
                    var imp = AssetImporter.GetAtPath(ClipsRobotsNovaT1553.RutaClip(nombre)) as ModelImporter;
                    sb.AppendLine("  " + nombre + ": " + (imp != null && imp.avatarSetup == ModelImporterAvatarSetup.CopyFromOther ? "avatar del personaje" : "SIN avatar del personaje"));
                }
            });
            Paso(sb, "Overrides, controladores, config y prefabs visuales (ClipsNpcNovaT1506.Aplicar)", ClipsNpcNovaT1506.Aplicar);
            Paso(sb, "Prefabs E16 (cañón y escudo)", AplicarPrefabsE16);
            Paso(sb, "Prefab E18 (conducta, sin arma)", AplicarPrefabE18);
            Paso(sb, "W10 del E15", () => sb.Append(CorregirArmaE15()));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Escribir(sb);
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

        private static void Escribir(StringBuilder sb)
        {
            Directory.CreateDirectory("Logs");
            File.AppendAllText(RutaInforme, sb + "\n");
            Debug.Log(sb.ToString());
        }
    }
}

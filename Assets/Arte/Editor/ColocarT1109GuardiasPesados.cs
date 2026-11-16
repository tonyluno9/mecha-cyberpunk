using System.Linq;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1109: crea los datos (DatosPandilleroAPie_GuardiaPesadoE12), el material de la placa del escudo y el prefab GuardiaPesado_E12, y coloca 1 guardia pesado
    /// a cada lado de la entrada de la torre principal de Nova (el edificio más alto de S2c_Nova), en la banqueta y sin tapar el paso. Elige el punto por sondeo
    /// de física. Idempotente: rehace la raíz <see cref="Raiz"/>, con un <see cref="PuestoGuardiasPesados"/> que guarda a los dos guardias para T-1110.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT1109GuardiasPesados.Colocar
    /// </summary>
    public static class ColocarT1109GuardiasPesados
    {
        public const string Raiz = "T1109_GuardiasPesados";
        public const string RutaDatos = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_GuardiaPesadoE12.asset";
        public const string RutaPrefab = "Assets/_Proyecto/IA/Prefabs/GuardiaPesado_E12.prefab";
        public const string RutaMaterial = "Assets/_Proyecto/IA/Prefabs/Mat_PlacaEscudoNova.mat";
        private const string RutaDatosBase = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_JefeNova.asset";
        private const string RutaPrefabBase = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        private const string RutaVisualE12 = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_E12_GuardiaPesadoNova.prefab";
        private const float RadioLibre = 0.9f;
        private const float LateralIdeal = 4f;
        private const float ProfundidadIdeal = 2f;

        /// <summary>La torre principal de Nova: el edificio con renderers más alto de S2c_Nova (a igual altura, el de más al este).</summary>
        public static Transform TorrePrincipal()
        {
            Transform nova = ColocarS2.Buscar(ColocarS2cNova.Raiz);
            if (nova == null)
            {
                return null;
            }
            return nova.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Edificio_") && t.GetComponentInChildren<Renderer>(true) != null)
                .OrderByDescending(t => Mathf.Round(Limites(t).max.y)).ThenByDescending(t => Limites(t).center.x).FirstOrDefault();
        }

        public static Bounds Limites(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>(true);
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        [MenuItem("Mecha Cyberpunk/T1109/Colocar guardias pesados de Nova")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            AsegurarDatos();
            AsegurarPrefab();
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            Transform vieja = ColocarS2.Buscar(Raiz);
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja.gameObject);
            }
            Physics.SyncTransforms();

            Transform torre = TorrePrincipal();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatos);
            if (torre == null || prefab == null || datos == null)
            {
                ColocarS2.Informe.AppendLine("FALTA la torre principal de Nova, el prefab o los datos del guardia pesado");
                ColocarS2.Guardar("t1109-guardias");
                return;
            }
            Bounds b = Limites(torre);
            float haciaCalle = ColocarS2cNova.ZCentro > b.center.z ? 1f : -1f;
            float caraZ = haciaCalle > 0f ? b.max.z : b.min.z;
            ColocarS2.Informe.AppendLine("Torre " + ColocarS2.Ruta(torre) + " centro " + b.center.ToString("F1") + " techo " + b.max.y.ToString("F1") + "; cara a la calle z=" + caraZ.ToString("F2"));

            GameObject raiz = new GameObject(Raiz);
            ConductaGuardiaPesado[] guardias = new ConductaGuardiaPesado[2];
            for (int i = 0; i < 2; i++)
            {
                float lado = i == 0 ? -1f : 1f;
                Vector3? punto = Sondear(b.center.x, caraZ, haciaCalle, lado);
                if (punto == null)
                {
                    ColocarS2.Informe.AppendLine("FALTA un punto para el guardia " + (i + 1));
                    continue;
                }
                GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz.transform);
                g.name = i == 0 ? "Guardia_Poniente" : "Guardia_Oriente";
                g.transform.SetPositionAndRotation(punto.Value, Quaternion.LookRotation(new Vector3(0f, 0f, haciaCalle), Vector3.up));
                guardias[i] = g.GetComponent<ConductaGuardiaPesado>();
                ColocarS2.Informe.AppendLine(g.name + " en " + punto.Value.ToString("F2"));
            }
            PuestoGuardiasPesados puesto = raiz.AddComponent<PuestoGuardiasPesados>();
            SerializedObject so = new SerializedObject(puesto);
            SerializedProperty lista = so.FindProperty("guardias");
            lista.arraySize = guardias.Length;
            for (int i = 0; i < guardias.Length; i++)
            {
                lista.GetArrayElementAtIndex(i).objectReferenceValue = guardias[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            ColocarS2.Guardar("t1109-guardias");
        }

        // Un punto de la banqueta, a un lado de la entrada: piso plano a la altura de la calle, nada sólido alrededor.
        private static Vector3? Sondear(float xEntrada, float caraZ, float haciaCalle, float lado)
        {
            Vector3? mejor = null;
            float mejorCosto = float.MaxValue;
            for (float l = 2.5f; l <= 5.5f; l += 0.5f)
            {
                for (float d = 1.4f; d <= 3.6f; d += 0.4f)
                {
                    Vector3 o = new Vector3(xEntrada + lado * l, 3f, caraZ + haciaCalle * d);
                    if (!Physics.Raycast(o, Vector3.down, out RaycastHit h, 6f, ~0, QueryTriggerInteraction.Ignore) || h.point.y < -0.1f || h.point.y > 0.45f || h.normal.y < 0.95f)
                    {
                        ColocarS2.Informe.AppendLine("  piso no sirve en " + o.ToString("F1") + ": " + (h.collider != null ? h.collider.name + " y=" + h.point.y.ToString("F2") + " n=" + h.normal.y.ToString("F2") : "nada"));
                        continue;
                    }
                    Vector3 p = h.point;
                    Collider[] cerca = Physics.OverlapCapsule(p + Vector3.up * (RadioLibre + 0.05f), p + Vector3.up * 2.2f, RadioLibre, ~0, QueryTriggerInteraction.Ignore);
                    if (cerca.Length > 0)
                    {
                        ColocarS2.Informe.AppendLine("  ocupado en " + p.ToString("F1") + ": " + cerca[0].name);
                        continue;
                    }
                    float costo = Mathf.Abs(l - LateralIdeal) + Mathf.Abs(d - ProfundidadIdeal);
                    if (costo < mejorCosto)
                    {
                        mejorCosto = costo;
                        mejor = p;
                    }
                }
            }
            return mejor;
        }

        // ------------------------------------------------------------------ datos y prefab

        /// <summary>Datos del guardia pesado: vida 220, velocidad 2.2, color base blanco (se ve la textura E12).</summary>
        public static void AsegurarDatos()
        {
            if (AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatos) == null)
            {
                AssetDatabase.CopyAsset(RutaDatosBase, RutaDatos);
            }
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatos);
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("nombre").stringValue = "GUARDIA NOVA";
            so.FindProperty("vidaMaxima").floatValue = 220f;
            so.FindProperty("velocidad").floatValue = 2.2f;
            so.FindProperty("dinero").intValue = 150;
            so.FindProperty("seDefiende").boolValue = false;
            so.FindProperty("calorAlGolpear").floatValue = 0f;
            so.FindProperty("calorAlMatar").floatValue = 0f;
            so.FindProperty("calorAlVencer").floatValue = 4f;
            so.FindProperty("usaPistola").boolValue = false;
            so.FindProperty("jefeCuerpoACuerpo").boolValue = true;
            so.FindProperty("multiplicadorCastigo").floatValue = 1.5f;
            so.FindProperty("colorNormal").colorValue = Color.white;
            so.FindProperty("colorAviso").colorValue = new Color(1f, 0.6f, 0.1f);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssets();
            ColocarS2.Informe.AppendLine("datos " + RutaDatos + " (vida 220, velocidad 2.2)");
        }

        public static Material AsegurarMaterial()
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, RutaMaterial);
            }
            m.SetColor("_BaseColor", new Color(0.12f, 0.32f, 0.75f));
            m.SetFloat("_Metallic", 0.85f);
            m.SetFloat("_Smoothness", 0.7f);
            EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssets();
            return m;
        }

        /// <summary>Prefab del guardia: PandilleroAPie con el visual E12, <see cref="ConductaGuardiaPesado"/> y <see cref="EscudoFrontal"/>.</summary>
        public static void AsegurarPrefab()
        {
            Material placa = AsegurarMaterial();
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefabBase);
            try
            {
                Transform viejo = contenido.transform.Find("Modelo");
                MechaCyberpunk.Personaje.Visual.Editor.ConectarVisualesT520.Reemplazar(contenido, viejo, AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisualE12), "Modelo", false);
                contenido.name = "GuardiaPesado_E12";
                if (contenido.GetComponent<EscudoFrontal>() == null)
                {
                    contenido.AddComponent<EscudoFrontal>();
                }
                if (contenido.GetComponent<ConductaGuardiaPesado>() == null)
                {
                    contenido.AddComponent<ConductaGuardiaPesado>();
                }
                SerializedObject soE = new SerializedObject(contenido.GetComponent<EscudoFrontal>());
                soE.FindProperty("materialPlaca").objectReferenceValue = placa;
                soE.ApplyModifiedPropertiesWithoutUndo();
                SerializedObject so = new SerializedObject(contenido.GetComponent<PandilleroAPie>());
                so.FindProperty("datos").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatos);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            AssetDatabase.SaveAssets();
            ColocarS2.Informe.AppendLine("prefab " + RutaPrefab);
        }
    }
}

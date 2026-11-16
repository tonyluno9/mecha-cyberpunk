using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1110: crea los datos (DatosPandilleroAPie_JefeFinalNovaE14), los materiales de aros, escudo y barrera y el prefab JefeFinalNova_E14, y arma el recinto
    /// del jefe final en la calle de Nova, al pie de la torre principal y detrás de los guardias de T-1109: el jefe espera al fondo de la calle (extremo este),
    /// la barrera de energía cierra la entrada oeste y la zona cubre el ancho de la calle. Todo fuera del eje de la calle (z = -43), que otra prueba exige libre.
    /// Idempotente: rehace la raíz <see cref="Raiz"/>. Antes hay que correr ColocarT1109GuardiasPesados.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT1110JefeFinal.Colocar
    /// </summary>
    public static class ColocarT1110JefeFinal
    {
        public const string Raiz = "T1110_JefeFinalNova";
        public const string RutaDatos = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_JefeFinalNovaE14.asset";
        public const string RutaPrefab = "Assets/_Proyecto/IA/Prefabs/JefeFinalNova_E14.prefab";
        public const string RutaMatAro = "Assets/_Proyecto/IA/Prefabs/Mat_AroEmisivoNova.mat";
        public const string RutaMatEsfera = "Assets/_Proyecto/IA/Prefabs/Mat_EsferaEscudoEnergia.mat";
        public const string RutaMatBarrera = "Assets/_Proyecto/IA/Prefabs/Mat_BarreraEnergiaNova.mat";
        private const string RutaDatosBase = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_JefeNova.asset";
        private const string RutaPrefabBase = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        private const string RutaVisualE14 = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_E14_JefeFinalNova.prefab";

        /// <summary>X de la barrera de la entrada (oeste) y del jefe (este), sobre la calle de Nova.</summary>
        public const float XEntrada = 103.5f;
        public const float XJefePreferido = 124f;
        public const float AnchoZona = 18f;
        public const float LargoZona = 20f;
        private const float SeparacionDelEje = 2f;

        [MenuItem("Mecha Cyberpunk/T1110/Colocar jefe final de Nova")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            AsegurarDatos();
            AsegurarMateriales();
            AsegurarPrefab();
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            Transform vieja = ColocarS2.Buscar(Raiz);
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja.gameObject);
            }
            Physics.SyncTransforms();

            PuestoGuardiasPesados guardias = Object.FindFirstObjectByType<PuestoGuardiasPesados>(FindObjectsInactive.Include);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatos);
            if (guardias == null || prefab == null || datos == null)
            {
                ColocarS2.Informe.AppendLine("FALTA el puesto de guardias de T-1109, el prefab o los datos del jefe final");
                ColocarS2.Guardar("t1110-jefe");
                return;
            }
            Vector3? sitio = BuscarSitioJefe();
            if (sitio == null)
            {
                ColocarS2.Informe.AppendLine("FALTA un lugar libre para el jefe final");
                ColocarS2.Guardar("t1110-jefe");
                return;
            }
            Transform raiz = new GameObject(Raiz).transform;
            Vector3 entrada = new Vector3(XEntrada, 0f, ColocarS2cNova.ZCentro);
            RecintoJefe recinto = ConstructorRecintoJefe.Crear(raiz, "Recinto_JefeFinal", entrada, sitio.Value, AnchoZona, LargoZona, prefab, datos, guardias,
                AssetDatabase.LoadAssetAtPath<Material>(RutaMatBarrera));
            ColocarS2.Informe.AppendLine("Recinto en " + recinto.transform.position.ToString("F1") + ", jefe en " + sitio.Value.ToString("F1") + ", barrera en x=" + XEntrada
                + "; a " + ColocarT1107Rina.DistanciaALaRuta(sitio.Value).ToString("F0") + " m de la ruta de la demo");
            ColocarS2.Guardar("t1110-jefe");
        }

        // Un punto del fondo de la calle de Nova: piso plano a la altura de la banqueta o la calle, libre alrededor y fuera del eje de la calle.
        private static Vector3? BuscarSitioJefe()
        {
            Vector3? mejor = null;
            float mejorCosto = float.MaxValue;
            for (float x = XJefePreferido - 6f; x <= XJefePreferido + 5f; x += 1f)
            {
                for (float dz = -7f; dz <= 7f; dz += 1f)
                {
                    float z = ColocarS2cNova.ZCentro + dz;
                    if (Mathf.Abs(dz) < SeparacionDelEje)
                    {
                        continue;
                    }
                    if (!Physics.Raycast(new Vector3(x, 3f, z), Vector3.down, out RaycastHit h, 6f, ~0, QueryTriggerInteraction.Ignore) || h.point.y < -0.1f || h.point.y > 0.45f
                        || h.normal.y < 0.95f)
                    {
                        continue;
                    }
                    Vector3 p = h.point;
                    if (Physics.CheckCapsule(p + Vector3.up * 1.35f, p + Vector3.up * 2.6f, 1.3f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }
                    float costo = Mathf.Abs(x - XJefePreferido) + Mathf.Abs(dz + 3f);
                    if (costo < mejorCosto)
                    {
                        mejorCosto = costo;
                        mejor = p;
                    }
                }
            }
            return mejor;
        }

        // ------------------------------------------------------------------ datos, materiales y prefab

        /// <summary>Datos del jefe final: vida 600, velocidad 2.8, color base blanco (se ve la textura E14).</summary>
        public static void AsegurarDatos()
        {
            if (AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatos) == null)
            {
                AssetDatabase.CopyAsset(RutaDatosBase, RutaDatos);
            }
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatos);
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("nombre").stringValue = "JEFE FINAL NOVA";
            so.FindProperty("vidaMaxima").floatValue = 600f;
            so.FindProperty("velocidad").floatValue = 2.8f;
            so.FindProperty("dinero").intValue = 1000;
            so.FindProperty("seDefiende").boolValue = false;
            so.FindProperty("calorAlGolpear").floatValue = 0f;
            so.FindProperty("calorAlMatar").floatValue = 0f;
            so.FindProperty("calorAlVencer").floatValue = 15f;
            so.FindProperty("usaPistola").boolValue = false;
            so.FindProperty("jefeCuerpoACuerpo").boolValue = true;
            so.FindProperty("multiplicadorCastigo").floatValue = 1.5f;
            so.FindProperty("colorNormal").colorValue = Color.white;
            so.FindProperty("colorAviso").colorValue = new Color(1f, 0.2f, 0.1f);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssets();
            ColocarS2.Informe.AppendLine("datos " + RutaDatos + " (vida 600, velocidad 2.8)");
        }

        public static void AsegurarMateriales()
        {
            AditivoTransparente(RutaMatAro, new Color(1f, 0.25f, 0.15f));
            AditivoTransparente(RutaMatEsfera, new Color(0.05f, 0.35f, 0.45f));
            AditivoTransparente(RutaMatBarrera, new Color(0.1f, 0.45f, 0.6f));
            AssetDatabase.SaveAssets();
        }

        // Material URP/Unlit transparente de mezcla aditiva: brilla sin tapar lo que hay detrás.
        private static void AditivoTransparente(string ruta, Color color)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", 1f);
            m.SetFloat("_DstBlend", 1f);
            m.SetFloat("_SrcBlendAlpha", 1f);
            m.SetFloat("_DstBlendAlpha", 1f);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Cull", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            m.SetShaderPassEnabled("ShadowCaster", false);
            m.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(m);
        }

        /// <summary>Prefab del jefe: PandilleroAPie con el visual E14, <see cref="EscudoEnergia"/> y <see cref="ConductaJefeFinalNova"/>.</summary>
        public static void AsegurarPrefab()
        {
            AsegurarMateriales();
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefabBase);
            try
            {
                Transform viejo = contenido.transform.Find("Modelo");
                MechaCyberpunk.Personaje.Visual.Editor.ConectarVisualesT520.Reemplazar(contenido, viejo, AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisualE14), "Modelo", false);
                contenido.name = "JefeFinalNova_E14";
                if (contenido.GetComponent<EscudoEnergia>() == null)
                {
                    contenido.AddComponent<EscudoEnergia>();
                }
                if (contenido.GetComponent<ConductaJefeFinalNova>() == null)
                {
                    contenido.AddComponent<ConductaJefeFinalNova>();
                }
                CharacterController cc = contenido.GetComponent<CharacterController>();
                if (cc != null)
                {
                    cc.height = 2.3f;
                    cc.radius = 0.5f;
                    cc.center = new Vector3(0f, 1.15f, 0f);
                }
                SerializedObject soE = new SerializedObject(contenido.GetComponent<EscudoEnergia>());
                soE.FindProperty("materialEsfera").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(RutaMatEsfera);
                soE.ApplyModifiedPropertiesWithoutUndo();
                SerializedObject soC = new SerializedObject(contenido.GetComponent<ConductaJefeFinalNova>());
                soC.FindProperty("materialAro").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(RutaMatAro);
                soC.ApplyModifiedPropertiesWithoutUndo();
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

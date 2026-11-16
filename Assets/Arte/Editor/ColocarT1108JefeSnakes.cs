using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1108: crea los datos (DatosPandilleroAPie_JefeSnakesE11) y el prefab del jefe SNAKES (JefeSnakes_E11) y lo coloca con 2 escoltas SNAKES (E10) y
    /// un grupo de refuerzo apagado en un tramo de calle plano y despejado de Neón Alto (sondeo de física), a 45 m o más de la ruta de la demo y lejos de las
    /// riñas de T-1107. Idempotente: rehace la raíz <see cref="Raiz"/>.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT1108JefeSnakes.Colocar
    /// </summary>
    public static class ColocarT1108JefeSnakes
    {
        public const string Raiz = "T1108_JefeSnakes";
        public const string RutaDatos = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_JefeSnakesE11.asset";
        public const string RutaPrefab = "Assets/_Proyecto/IA/Prefabs/JefeSnakes_E11.prefab";
        private const string RutaDatosBase = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_JefeNova.asset";
        private const string RutaPrefabBase = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        private const string RutaVisualE11 = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_E11_JefeSnakes.prefab";
        private const string RutaDatosViboras = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Viboras.asset";

        private const float PasoMalla = 2f;
        private const float MitadEje = 5.5f;
        private const float MitadLado = 3.5f;
        private const float AlturaSuelo = 1f;
        private const float SueloMinY = -0.1f;
        private const float SueloMaxY = 0.45f;
        private const float PlanoTolerancia = 0.08f;
        private const float DespejeBajo = 0.4f;
        private const float DespejeAlto = 2.4f;
        private const float DistLugares = 14f;
        private const float DistRinas = 40f;

        [MenuItem("Mecha Cyberpunk/T1108/Colocar jefe SNAKES")]
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

            GameObject prefabJefe = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            GameObject prefabSnakes = AssetDatabase.LoadAssetAtPath<GameObject>(ColocarT1107Rina.RutaPrefabSnakes);
            DatosPandilleroAPie datosJefe = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatos);
            DatosPandilleroAPie viboras = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatosViboras);
            if (prefabJefe == null || prefabSnakes == null || datosJefe == null || viboras == null)
            {
                Debug.LogError("[Arte] T-1108: faltan el prefab o los datos del jefe SNAKES.");
                return;
            }

            List<Vector3> evitar = ColocarT1107Rina.Lugares();
            evitar.AddRange(ColocarT1107Rina.PandillasExistentes());
            List<Vector3> rinas = Object.FindObjectsByType<RinaPandillas>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(r => r.transform.position).ToList();
            (Vector3 centro, Vector3 eje)? sitio = Buscar(new Vector2(47f, -60f), evitar, rinas);
            if (sitio == null)
            {
                ColocarS2.Informe.AppendLine("FALTA un lote para el jefe SNAKES");
                ColocarS2.Guardar("t1108-jefe");
                return;
            }
            GameObject raiz = new GameObject(Raiz);
            // Mira a lo largo de la calle; los refuerzos esperan detrás de él.
            ConstructorJefeSnakes.Crear(raiz.transform, "Puesto_JefeSnakes", sitio.Value.centro, sitio.Value.eje, prefabJefe, datosJefe, prefabSnakes, viboras);
            ColocarS2.Informe.AppendLine("Jefe SNAKES con 2 escoltas y 2 refuerzos en " + sitio.Value.centro.ToString("F1") + " eje " + sitio.Value.eje + ", a "
                + ColocarT1107Rina.DistanciaALaRuta(sitio.Value.centro).ToString("F0") + " m de la ruta de la demo");
            ColocarS2.Guardar("t1108-jefe");
        }

        // ------------------------------------------------------------------ datos y prefab

        /// <summary>Datos del jefe SNAKES: vida 260 y velocidad 4.2; sale de los del jefe de Nova (que usa el modo cuerpo a cuerpo que el pandillero exige sin pistola).</summary>
        public static void AsegurarDatos()
        {
            if (AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatos) == null)
            {
                AssetDatabase.CopyAsset(RutaDatosBase, RutaDatos);
            }
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatos);
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("nombre").stringValue = "JEFE SNAKE";
            so.FindProperty("vidaMaxima").floatValue = 260f;
            so.FindProperty("velocidad").floatValue = 4.2f;
            so.FindProperty("dinero").intValue = 300;
            so.FindProperty("seDefiende").boolValue = false;
            so.FindProperty("calorAlGolpear").floatValue = 0f;
            so.FindProperty("calorAlMatar").floatValue = 0f;
            so.FindProperty("calorAlVencer").floatValue = 8f;
            so.FindProperty("usaPistola").boolValue = false;
            so.FindProperty("jefeCuerpoACuerpo").boolValue = true;
            so.FindProperty("multiplicadorCastigo").floatValue = 1.5f;
            so.FindProperty("colorNormal").colorValue = Color.white; // sin retinte: el aviso lo sube hacia el verde
            so.FindProperty("colorAviso").colorValue = new Color(0.45f, 1f, 0.1f);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssets();
            ColocarS2.Informe.AppendLine("datos " + RutaDatos + " (vida 260, velocidad 4.2)");
        }

        /// <summary>Prefab del jefe: PandilleroAPie con el visual E11 y la conducta <see cref="ConductaJefeSnakes"/>.</summary>
        public static void AsegurarPrefab()
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefabBase);
            try
            {
                Transform viejo = contenido.transform.Find("Modelo");
                MechaCyberpunk.Personaje.Visual.Editor.ConectarVisualesT520.Reemplazar(contenido, viejo, AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisualE11), "Modelo", false);
                contenido.name = "JefeSnakes_E11";
                if (contenido.GetComponent<ConductaJefeSnakes>() == null)
                {
                    contenido.AddComponent<ConductaJefeSnakes>();
                }
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

        // ------------------------------------------------------------------ sitio

        // Un tramo de calle plano y despejado (de 13 x 10 m) en Neón Alto, fuera de la ruta de la demo, de los lugares, de las otras pandillas y de las riñas,
        // con piso libre también donde esperan los refuerzos (12 m detrás del jefe). Devuelve el centro y el eje (X o Z) en el que mira el jefe.
        private static (Vector3 centro, Vector3 eje)? Buscar(Vector2 preferido, List<Vector3> evitar, List<Vector3> rinas)
        {
            List<(Vector3 centro, Vector3 eje)> validos = new List<(Vector3, Vector3)>();
            for (float x = 50f; x <= 118f; x += PasoMalla)
            {
                for (float z = -90f; z <= 90f; z += PasoMalla)
                {
                    Vector3 c = new Vector3(x, 0f, z);
                    if (ColocarT1107Rina.DistanciaALaRuta(c) < ColocarT1107Rina.DistanciaRuta || ColocarT1107Rina.EnZonaProhibida(c)
                        || evitar.Any(e => Vector3.Distance(new Vector3(e.x, 0f, e.z), c) < DistLugares)
                        || rinas.Any(e => Vector3.Distance(new Vector3(e.x, 0f, e.z), c) < DistRinas))
                    {
                        continue;
                    }
                    foreach (Vector3 eje in new[] { Vector3.right, Vector3.forward })
                    {
                        if (Plano(c, eje, out float y) && ColocarT1107Rina.Despejado(new Vector3(x, y, z), eje, MitadEje, MitadLado, 3.5f, 2.5f)
                            && ConstructorJefeSnakes.DireccionRefuerzos(new Vector3(x, y, z), eje) != Vector3.zero && HayRefuerzos(new Vector3(x, y, z), eje))
                        {
                            validos.Add((new Vector3(x, y, z), eje));
                            break;
                        }
                    }
                }
            }
            ColocarS2.Informe.AppendLine("Sitios válidos para el jefe: " + validos.Count);
            if (validos.Count == 0)
            {
                return null;
            }
            return validos.OrderBy(v => Vector2.Distance(new Vector2(v.centro.x, v.centro.z), preferido)).First();
        }

        private static bool Plano(Vector3 centro, Vector3 eje, out float y)
        {
            y = 0f;
            if (!Physics.Raycast(new Vector3(centro.x, AlturaSuelo, centro.z), Vector3.down, out RaycastHit h0, 3f, ~0, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            y = h0.point.y;
            if (y < SueloMinY || y > SueloMaxY)
            {
                return false;
            }
            Vector3 lado = Vector3.Cross(Vector3.up, eje);
            for (float de = -MitadEje; de <= MitadEje + 0.01f; de += MitadEje / 2f)
            {
                for (float dl = -MitadLado; dl <= MitadLado + 0.01f; dl += MitadLado / 2f)
                {
                    Vector3 o = centro + eje * de + lado * dl;
                    if (!Physics.Raycast(new Vector3(o.x, AlturaSuelo, o.z), Vector3.down, out RaycastHit h, 3f, ~0, QueryTriggerInteraction.Ignore)
                        || Mathf.Abs(h.point.y - y) > PlanoTolerancia || h.normal.y < 0.95f)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        // Hay un lado con piso libre para los refuerzos.
        private static bool HayRefuerzos(Vector3 centro, Vector3 mira)
        {
            return ConstructorJefeSnakes.HayPisoLibre(centro + ConstructorJefeSnakes.DireccionRefuerzos(centro, mira) * ConstructorJefeSnakes.DistanciaRefuerzos);
        }
    }
}

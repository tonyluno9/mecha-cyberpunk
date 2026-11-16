using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1107: crea el prefab PandilleroArmado_Snakes (visual E10) y coloca dos riñas SNAKES contra SKULLS en la ciudad: una en la frontera de Neón Alto
    /// con el tianguis y otra dentro de Neón Alto. Sitios planos y despejados (sondeo de física) a 45 m o más de la ruta de la demo. También pasa a
    /// los SNAKES que ya había al visual E10. Idempotente: rehace la raíz <see cref="Raiz"/>.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT1107Rina.Colocar
    /// </summary>
    public static class ColocarT1107Rina
    {
        public const string Raiz = "T1107_Rinas";
        public const string RutaPrefabSnakes = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Snakes.prefab";
        public const string RutaPrefabSkulls = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Skulls.prefab";
        private const string RutaPrefabBase = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        private const string RutaVisualE10 = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_E10_PandilleroSnakes.prefab";
        private const string RutaDatosViboras = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Viboras.asset";
        private const string RutaDatosCalaveras = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset";

        /// <summary>Puntos de la ruta de la demo (casa, inventor, Doc, tianguis, hacker) y el orden en que el bot los recorre.</summary>
        public static readonly Vector3[] RutaDemo =
        {
            new Vector3(-59f, 0f, -77f), new Vector3(-44.3f, 0f, -77f), new Vector3(-100.3f, 0f, -13.4f), new Vector3(-44.3f, 0f, -77f),
            new Vector3(-36f, 0f, 85f), new Vector3(74.5f, 0f, 47f)
        };

        public const float DistanciaRuta = 45f;
        private const float LimiteZonaX = 47f;
        private const float PasoMalla = 2f;
        private const float MitadEje = 5.2f;
        private const float MitadLado = 2.6f;
        private const float AlturaSuelo = 1f;
        private const float SueloMinY = -0.1f;
        private const float SueloMaxY = 0.45f;
        private const float PlanoTolerancia = 0.08f;
        private const float DespejeBajo = 0.4f;
        private const float DespejeAlto = 2.4f;
        private const float DistLugares = 25f;
        private const float DistPandillas = 25f;
        private const float DistEntreRinas = 40f;

        private static readonly string[] TiposLugar =
        {
            "PersonajeApoyo", "Reclutable", "MostradorTienda", "PuntoAparicion", "PuntoHistoriaMundo", "JefePandilla", "MarcaLugar",
            "MostradorPeleas", "DoctorClinica"
        };

        /// <summary>Distancia en el plano de <paramref name="p"/> al punto o segmento más cercano de la ruta de la demo.</summary>
        public static float DistanciaALaRuta(Vector3 p)
        {
            float menor = float.MaxValue;
            for (int i = 0; i < RutaDemo.Length; i++)
            {
                menor = Mathf.Min(menor, DistanciaASegmento(p, RutaDemo[i], RutaDemo[Mathf.Min(i + 1, RutaDemo.Length - 1)]));
            }
            return menor;
        }

        public static float DistanciaASegmento(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector2 pp = new Vector2(p.x, p.z);
            Vector2 aa = new Vector2(a.x, a.z);
            Vector2 ab = new Vector2(b.x, b.z) - aa;
            float t = ab.sqrMagnitude < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(pp - aa, ab) / ab.sqrMagnitude);
            return Vector2.Distance(pp, aa + ab * t);
        }

        [MenuItem("Mecha Cyberpunk/T1107/Colocar riñas SNAKES contra SKULLS")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            AsegurarPrefabSnakes();
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            Transform vieja = ColocarS2.Buscar(Raiz);
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja.gameObject);
            }
            Physics.SyncTransforms();

            GameObject snakes = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabSnakes);
            GameObject skulls = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabSkulls);
            DatosPandilleroAPie viboras = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatosViboras);
            DatosPandilleroAPie calaveras = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatosCalaveras);
            if (snakes == null || skulls == null || viboras == null || calaveras == null)
            {
                Debug.LogError("[Arte] T-1107: faltan los prefabs o los datos de SNAKES y SKULLS.");
                return;
            }

            List<Vector3> evitar = Lugares();
            evitar.AddRange(PandillasExistentes());
            GameObject raiz = new GameObject(Raiz);
            List<Vector3> hechas = new List<Vector3>();
            // Frontera: pegada al límite (x = 47) del lado de Neón Alto; dentro: más al este.
            (string nombre, float xMin, float xMax, Vector2 preferida)[] sitios =
            {
                ("Rina_Frontera", LimiteZonaX + 6f, LimiteZonaX + 40f, new Vector2(LimiteZonaX + 10f, -40f)),
                ("Rina_NeonAlto", 100f, 190f, new Vector2(130f, -110f))
            };
            foreach ((string nombre, float xMin, float xMax, Vector2 preferida) in sitios)
            {
                (Vector3 centro, Vector3 eje)? sitio = Buscar(xMin, xMax, preferida, evitar, hechas);
                if (sitio == null)
                {
                    ColocarS2.Informe.AppendLine("FALTA un sitio para " + nombre);
                    continue;
                }
                hechas.Add(sitio.Value.centro);
                ConstructorRina.Crear(raiz.transform, nombre, sitio.Value.centro, sitio.Value.eje, snakes, viboras, IdPandilla.Viboras, skulls, calaveras,
                    IdPandilla.Calaveras);
                ColocarS2.Informe.AppendLine(nombre + " en " + sitio.Value.centro.ToString("F1") + " eje " + sitio.Value.eje + ", a " + DistanciaALaRuta(sitio.Value.centro).ToString("F0") + " m de la ruta de la demo");
            }
            ColocarS2.Informe.AppendLine("SNAKES viejos pasados a E10: " + PasarSnakesViejosAE10(snakes, viboras, raiz.transform));
            ColocarS2.Guardar("t1107-rinas");
        }


        // ------------------------------------------------------------------ prefab

        /// <summary>Crea (o rehace) el prefab del SNAKES con el visual E10 y la pistola W01 en la mano, con los datos de Viboras.</summary>
        public static void AsegurarPrefabSnakes()
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefabBase);
            try
            {
                Transform viejo = contenido.transform.Find("Modelo");
                GameObject nuevo = MechaCyberpunk.Personaje.Visual.Editor.ConectarVisualesT520.Reemplazar(contenido, viejo,
                    AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisualE10), "Modelo", false);
                PonerPistola(nuevo);
                contenido.name = "PandilleroArmado_Snakes";
                SerializedObject so = new SerializedObject(contenido.GetComponent<PandilleroAPie>());
                so.FindProperty("datos").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatosViboras);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefabSnakes);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            AssetDatabase.SaveAssets();
            ColocarS2.Informe.AppendLine("prefab " + RutaPrefabSnakes + " con el visual E10");
        }

        // La misma pistola W01 y la misma pose en la mano que los SKULLS.
        private static void PonerPistola(GameObject modelo)
        {
            Transform mano = modelo.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == MechaCyberpunk.Personaje.Visual.Editor.CreadorVisualTripo.NombreSocket);
            GameObject skulls = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabSkulls);
            Transform ejemplo = skulls != null ? skulls.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == ColocarT545.NombreModelo) : null;
            if (mano == null || ejemplo == null)
            {
                Debug.LogWarning("[Arte] T-1107: el visual E10 no tiene socket de mano o los SKULLS no tienen pistola.");
                return;
            }
            Transform viejo = mano.Find(ColocarT545.NombreModelo);
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            GameObject w = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ColocarT545.RutaPrefab), mano);
            w.name = ColocarT545.NombreModelo;
            w.transform.localPosition = ejemplo.localPosition;
            w.transform.localRotation = ejemplo.localRotation;
            w.transform.localScale = ejemplo.localScale;
        }

        // Los SNAKES que ya estaban (de pandillas de ciudad) pasan al visual E10: se reemplaza cada pandillero por una instancia del prefab nuevo.
        private static int PasarSnakesViejosAE10(GameObject prefabSnakes, DatosPandilleroAPie viboras, Transform raizRinas)
        {
            int cambiados = 0;
            foreach (PandilleroAPie p in Object.FindObjectsByType<PandilleroAPie>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToArray())
            {
                if (p.transform.IsChildOf(raizRinas))
                {
                    continue;
                }
                SerializedObject so = new SerializedObject(p);
                if (so.FindProperty("datos").objectReferenceValue != viboras)
                {
                    continue;
                }
                GameObject origen = PrefabUtility.GetCorrespondingObjectFromSource(p.gameObject);
                if (origen != null && AssetDatabase.GetAssetPath(origen) == RutaPrefabSnakes)
                {
                    continue;
                }
                Transform padre = p.transform.parent;
                int indice = p.transform.GetSiblingIndex();
                GameObject nuevo = (GameObject)PrefabUtility.InstantiatePrefab(prefabSnakes, padre);
                nuevo.name = p.name;
                nuevo.transform.SetPositionAndRotation(p.transform.position, p.transform.rotation);
                nuevo.transform.SetSiblingIndex(indice);
                nuevo.SetActive(p.gameObject.activeSelf);
                Object.DestroyImmediate(p.gameObject);
                cambiados++;
            }
            return cambiados;
        }

        // ------------------------------------------------------------------ sitios

        // Devuelve el centro y el eje (X o Z) de un sitio plano y despejado: calles y banquetas del tamaño de la riña, sin paredes ni props de por medio.
        private static (Vector3 centro, Vector3 eje)? Buscar(float xMin, float xMax, Vector2 preferida, List<Vector3> evitar, List<Vector3> hechas)
        {
            List<(Vector3 centro, Vector3 eje)> validos = new List<(Vector3, Vector3)>();
            for (float x = xMin; x <= xMax; x += PasoMalla)
            {
                for (float z = -190f; z <= 190f; z += PasoMalla)
                {
                    Vector3 c = new Vector3(x, 0f, z);
                    if (DistanciaALaRuta(c) < DistanciaRuta
                        || evitar.Any(e => Vector3.Distance(new Vector3(e.x, 0f, e.z), c) < DistLugares)
                        || hechas.Any(e => Vector3.Distance(e, c) < DistEntreRinas))
                    {
                        continue;
                    }
                    foreach (Vector3 eje in new[] { Vector3.right, Vector3.forward })
                    {
                        if (Plano(c, eje, out float y) && Despejado(new Vector3(x, y, z), eje))
                        {
                            validos.Add((new Vector3(x, y, z), eje));
                            break;
                        }
                    }
                }
            }
            ColocarS2.Informe.AppendLine("Sitios válidos entre x=" + xMin + " y " + xMax + ": " + validos.Count);
            if (validos.Count == 0)
            {
                return null;
            }
            return validos.OrderBy(v => Vector2.Distance(new Vector2(v.centro.x, v.centro.z), preferida)).First();
        }

        // Piso plano en toda la huella (la calle sirve: es el espacio libre de las manzanas).
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

        private static bool Despejado(Vector3 centro, Vector3 eje)
        {
            Vector3 c = new Vector3(centro.x, centro.y + (DespejeBajo + DespejeAlto) * 0.5f, centro.z);
            Vector3 mitad = new Vector3(MitadLado, (DespejeAlto - DespejeBajo) * 0.5f, MitadEje); // z local = eje
            return !Physics.CheckBox(c, mitad, Quaternion.LookRotation(eje, Vector3.up), ~0, QueryTriggerInteraction.Ignore);
        }

        private static List<Vector3> Lugares()
        {
            List<Vector3> puntos = new List<Vector3>();
            foreach (Transform t in ColocarS2.Todos())
            {
                if (t.name.StartsWith("Lugar_") || t.GetComponents<MonoBehaviour>().Any(m => m != null && TiposLugar.Contains(m.GetType().Name)))
                {
                    puntos.Add(t.position);
                }
            }
            return puntos;
        }

        private static List<Vector3> PandillasExistentes()
        {
            List<Vector3> puntos = new List<Vector3>();
            foreach (PandilleroAPie p in Object.FindObjectsByType<PandilleroAPie>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                puntos.Add(p.transform.position);
            }
            return puntos;
        }
    }
}

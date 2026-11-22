using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T23 / T-1647: en Ciudad_Prologo, objetos que estorban la ruta de banqueta de los peatones: los toldos T1523_Prop_TL01_Toldo_Toldo_2 y _3 (T501_Suburbios/Toldos, en (-94.5, 9.25) y (-80.5, 9.25)),
    /// el corro Gente_Platica_suburbios_2 y _2b (junto al toldo 3) y Cobertura_CB/CB02_Costales_b (en (91, -10)). Cada uno se aleja de la ruta de banqueta más cercana (las rutas de
    /// GrupoPeatones) por el eje que más lo aparta, sin bajarlo a la calle ni meterlo en otro objeto, hasta dejar <see cref="Libre"/> m o lo más que quepa (máximo 2 m).
    /// Idempotente: si ya no está donde estaba (más de <see cref="YaMovido"/> m), no se vuelve a mover.
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.RutasPeatonesT1647.Diagnostico (no cambia nada) | Aplicar. Informe en Logs/t1647.txt.
    /// </summary>
    public static class RutasPeatonesT1647
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string RutaInforme = "Logs/t1647.txt";

        /// <summary>Final de la ruta en la jerarquía y dónde estaba (x, z) antes de moverlo.</summary>
        public static readonly (string ruta, Vector2 original)[] Objetos =
        {
            ("Toldos/T1523_Prop_TL01_Toldo_Toldo_2", new Vector2(-94.5f, 9.25f)),
            ("Toldos/T1523_Prop_TL01_Toldo_Toldo_3", new Vector2(-80.5f, 9.25f)),
            ("Gente_Platica_suburbios_2", new Vector2(-79.95f, 8.85f)),
            ("Gente_Platica_suburbios_2b", new Vector2(-79.05f, 8.85f)),
            ("Cobertura_CB/CB02_Costales_b", new Vector2(91.01f, -10.01f)),
        };

        public const float YaMovido = 0.05f;
        private const float RadioOriginal = 2.5f;
        public const float Libre = 0.9f;
        public const float LibreMinimoUtil = 0.4f;
        private const float MaximoMover = 2f;
        private const float PasoMuestra = 0.25f;
        private const float DesnivelMaximo = 0.05f;
        private const float Paso = 0.25f;
        private const float AlcancePared = 10f;
        private const float AlturaRayo = 1.2f;
        private const float AltoMinimoPared = 2.5f;
        private const float AlturaDeSuelo = 0.3f;
        private const float Encogido = 0.05f;

        [MenuItem("Mecha Cyberpunk/T-1647/Diagnostico rutas de peatones")]
        public static void Diagnostico() => Ejecutar(false);

        [MenuItem("Mecha Cyberpunk/T-1647/Aplicar rutas de peatones")]
        public static void Aplicar() => Ejecutar(true);

        private static void Ejecutar(bool mover)
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            Physics.SyncTransforms();
            StringBuilder informe = new StringBuilder("T-1647 " + (mover ? "Aplicar" : "Diagnostico") + "\n");
            List<Transform> todos = escena.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToList();
            List<(Vector3 a, Vector3 b)> rutas = Rutas(escena);
            informe.AppendLine("Tramos de ruta de peatones: " + rutas.Count);
            bool cambio = false;
            List<Transform> objetivos = new List<Transform>();
            foreach ((string ruta, Vector2 original) in Objetos)
            {
                objetivos.AddRange(todos.Where(t => (Ruta(t).EndsWith("/" + ruta) || Ruta(t) == ruta) && Vector2.Distance(new Vector2(t.position.x, t.position.z), original) <= RadioOriginal));
            }
            foreach ((string ruta, Vector2 original) in Objetos)
            {
                List<Transform> hallados = todos.Where(t => Ruta(t).EndsWith("/" + ruta) || Ruta(t) == ruta).ToList();
                if (hallados.Count == 0)
                {
                    informe.AppendLine(ruta + ": NO encontrado");
                    continue;
                }
                foreach (Transform t in hallados)
                {
                    Vector2 xz = new Vector2(t.position.x, t.position.z);
                    if (Vector2.Distance(xz, original) > RadioOriginal)
                    {
                        informe.AppendLine(Ruta(t) + ": en " + t.position.ToString("0.00") + ", lejos de " + original + ": no es el de la tarea");
                        continue;
                    }
                    informe.AppendLine(Describir(t) + "\n    de la ruta de banqueta: " + DistanciaARuta(Caja(t, out _), rutas).ToString("0.00") + " m, tramos cerca: " + TramosCerca(Caja(t, out _), rutas));
                    string r = Mover(t, original, rutas, mover, objetivos.Where(o => o != t).ToList());
                    informe.AppendLine("  -> " + (mover ? "" : "(plan) ") + r);
                    cambio |= mover && r.StartsWith("movido");
                }
            }
            if (cambio)
            {
                EditorSceneManager.MarkSceneDirty(escena);
                EditorSceneManager.SaveScene(escena);
                informe.AppendLine("Escena guardada: " + (new FileInfo(RutaEscena).Length / 1048576f).ToString("0.0") + " MB.");
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, informe.ToString());
            Debug.Log("[Mundo] " + informe);
        }

        private static string Describir(Transform t)
        {
            StringBuilder s = new StringBuilder(Ruta(t) + ": pos " + t.position.ToString("0.00") + " hijos " + t.childCount);
            Bounds b = Caja(t, out bool hayMallas);
            s.Append(" caja " + b.center.ToString("0.00") + " tam " + b.size.ToString("0.00") + (hayMallas ? "" : " (sin mallas: hijos)"));
            s.Append(" componentes [" + string.Join(",", t.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name)) + "]");
            foreach (Vector3 dir in Direcciones())
            {
                if (Pared(t, b, dir, out RaycastHit hit))
                {
                    s.Append("\n    pared " + dir.ToString("0") + " a " + hit.distance.ToString("0.00") + " m: " + Ruta(hit.collider.transform) + " (alto " + hit.collider.bounds.size.y.ToString("0.0") + ")");
                }
            }
            return s.ToString();
        }

        private static string Mover(Transform t, Vector2 original, List<(Vector3 a, Vector3 b)> rutas, bool aplicar, List<Transform> otros)
        {
            if (Vector2.Distance(new Vector2(t.position.x, t.position.z), original) > YaMovido)
            {
                return "ya movido antes";
            }
            Bounds b = Caja(t, out _);
            float antes = DistanciaARuta(b, rutas);
            float sueloAntes = Suelo(t, b.center, b.min.y);
            float mejorLibre = antes;
            Vector3 mejorMovida = Vector3.zero;
            Dictionary<string, int> razones = new Dictionary<string, int>();
            // Todos los desplazamientos de la rejilla hasta MaximoMover: el más corto que deja la ruta libre (o el que más la aleja). Sin cruzar paredes (línea libre desde donde
            // estaba), sin bajar a la calle, sin chocar con nada ni encimarse con los otros objetos de la lista (los corros no tienen colliders).
            for (float dx = -MaximoMover; dx <= MaximoMover + 0.001f; dx += Paso)
            {
                for (float dz = -MaximoMover; dz <= MaximoMover + 0.001f; dz += Paso)
                {
                    Vector3 movida3 = new Vector3(dx, 0f, dz);
                    float d = movida3.magnitude;
                    if (d < 0.001f || d > MaximoMover + 0.001f)
                    {
                        continue;
                    }
                    Bounds movida = Desplazada(b, movida3);
                    float libre = DistanciaARuta(movida, rutas);
                    bool yaBasta = mejorLibre >= Libre;
                    bool sirve = yaBasta ? libre >= Libre && d < mejorMovida.magnitude - 0.001f : libre > mejorLibre + 0.01f;
                    if (!sirve)
                    {
                        continue;
                    }
                    string razon = Tope(t, b, movida, sueloAntes, otros);
                    if (razon != null)
                    {
                        razones[razon] = razones.TryGetValue(razon, out int n) ? n + 1 : 1;
                        continue;
                    }
                    mejorLibre = libre;
                    mejorMovida = movida3;
                }
            }
            if (mejorMovida == Vector3.zero || mejorLibre < LibreMinimoUtil)
            {
                return "no se puede alejar de la ruta (libre " + antes.ToString("0.00") + " m; lo más que cabe deja " + mejorLibre.ToString("0.00") + " m): " + string.Join("; ", razones.OrderByDescending(r => r.Value).Take(4).Select(r => r.Key + " x" + r.Value));
            }
            if (aplicar)
            {
                Undo.RecordObject(t, "T-1647");
                t.position += mejorMovida;
                Physics.SyncTransforms();
            }
            return "movido " + mejorMovida.ToString("0.00") + ": de la ruta " + antes.ToString("0.00") + " -> " + mejorLibre.ToString("0.00") + " m, queda en " + (t.position + (aplicar ? Vector3.zero : mejorMovida)).ToString("0.00") + " (topes: " + string.Join("; ", razones.OrderByDescending(r => r.Value).Take(3).Select(r => r.Key + " x" + r.Value)) + ")";
        }

        // null si se puede poner ahí; si no, por qué.
        private static string Tope(Transform t, Bounds desde, Bounds hacia, float sueloAntes, List<Transform> otros)
        {
            float suelo = Suelo(t, hacia.center, desde.min.y);
            if (float.IsNaN(suelo))
            {
                return "sin piso";
            }
            if (suelo < sueloAntes - DesnivelMaximo)
            {
                return "baja a la calle";
            }
            foreach (Vector3 esquina in new[] { new Vector3(1f, 0f, 1f), new Vector3(1f, 0f, -1f), new Vector3(-1f, 0f, 1f), new Vector3(-1f, 0f, -1f) })
            {
                float s = Suelo(t, hacia.center + Vector3.Scale(hacia.extents * 0.9f, esquina), desde.min.y);
                if (float.IsNaN(s) || s < sueloAntes - DesnivelMaximo)
                {
                    return "una esquina baja a la calle";
                }
            }
            Vector3 alto = Vector3.up * (sueloAntes + AlturaRayo - desde.center.y);
            foreach (RaycastHit h in Physics.RaycastAll(desde.center + alto, hacia.center - desde.center, Vector3.Distance(desde.center, hacia.center), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (!h.collider.transform.IsChildOf(t))
                {
                    return "pared en medio: " + h.collider.name;
                }
            }
            List<string> choca = Choques(t, hacia, sueloAntes);
            if (choca.Count > 0)
            {
                return "choca con " + choca[0];
            }
            foreach (Transform o in otros)
            {
                Bounds caja = Caja(o, out _);
                caja.Expand(new Vector3(0.1f, 0f, 0.1f));
                Bounds encogida = hacia;
                encogida.Expand(new Vector3(-0.1f, 0f, -0.1f));
                if (caja.Intersects(encogida))
                {
                    return "encima de " + o.name;
                }
            }
            return null;
        }

        // Piso bajo un punto, sin contar el propio objeto.
        private static float Suelo(Transform t, Vector3 punto, float fondo)
        {
            foreach (RaycastHit hit in Physics.RaycastAll(new Vector3(punto.x, fondo + 1.5f, punto.z), Vector3.down, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
            {
                if (!hit.collider.transform.IsChildOf(t))
                {
                    return hit.point.y;
                }
            }
            return float.NaN;
        }

        // Las rutas de banqueta de los GrupoPeatones de la escena, en tramos del mundo (sin referenciar Trafico: se leen serializadas).
        private static List<(Vector3 a, Vector3 b)> Rutas(Scene escena)
        {
            List<(Vector3 a, Vector3 b)> tramos = new List<(Vector3 a, Vector3 b)>();
            foreach (MonoBehaviour mb in escena.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)))
            {
                if (mb == null || mb.GetType().Name != "GrupoPeatones")
                {
                    continue;
                }
                SerializedProperty lista = new SerializedObject(mb).FindProperty("rutas");
                for (int i = 0; lista != null && i < lista.arraySize; i++)
                {
                    SerializedProperty puntos = lista.GetArrayElementAtIndex(i).FindPropertyRelative("puntos");
                    for (int k = 0; puntos != null && k + 1 < puntos.arraySize; k++)
                    {
                        tramos.Add((mb.transform.TransformPoint(puntos.GetArrayElementAtIndex(k).vector3Value), mb.transform.TransformPoint(puntos.GetArrayElementAtIndex(k + 1).vector3Value)));
                    }
                }
            }
            return tramos;
        }

        private static string TramosCerca(Bounds caja, List<(Vector3 a, Vector3 b)> rutas)
        {
            return string.Join(" | ", rutas.Where(r => DistanciaARuta(caja, new List<(Vector3 a, Vector3 b)> { r }) < 3f).Select(r => r.a.ToString("0.0") + "->" + r.b.ToString("0.0")));
        }

        // Distancia en planta de la ruta más cercana al borde de la caja (0 si la ruta la cruza).
        private static float DistanciaARuta(Bounds caja, List<(Vector3 a, Vector3 b)> rutas)
        {
            float minimo = float.MaxValue;
            foreach ((Vector3 a, Vector3 b) in rutas)
            {
                float largo = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
                int pasos = Mathf.Max(1, Mathf.CeilToInt(largo / PasoMuestra));
                for (int i = 0; i <= pasos; i++)
                {
                    Vector3 p = Vector3.Lerp(a, b, i / (float)pasos);
                    float dx = Mathf.Max(0f, Mathf.Max(caja.min.x - p.x, p.x - caja.max.x));
                    float dz = Mathf.Max(0f, Mathf.Max(caja.min.z - p.z, p.z - caja.max.z));
                    minimo = Mathf.Min(minimo, Mathf.Sqrt(dx * dx + dz * dz));
                }
            }
            return minimo;
        }

        private static Bounds Desplazada(Bounds b, Vector3 d)
        {
            b.center += d;
            return b;
        }

        private static IEnumerable<Vector3> Direcciones() => new[] { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };

        private static bool Pared(Transform t, Bounds b, Vector3 dir, out RaycastHit pared)
        {
            pared = default;
            Vector3 origen = new Vector3(b.center.x, b.min.y + AlturaRayo, b.center.z);
            RaycastHit[] hits = Physics.RaycastAll(origen, dir, AlcancePared, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit h in hits.OrderBy(h => h.distance))
            {
                if (h.collider.transform.IsChildOf(t) || h.collider.bounds.size.y < AltoMinimoPared)
                {
                    continue;
                }
                pared = h;
                return true;
            }
            return false;
        }

        // Lo que queda dentro de la caja, de la altura del piso + AlturaDeSuelo para arriba (banquetas y pisos no cuentan).
        private static List<string> Choques(Transform t, Bounds b, float suelo)
        {
            float desde = suelo + AlturaDeSuelo;
            float hasta = Mathf.Max(b.max.y, desde + 0.2f);
            Vector3 semi = new Vector3(Mathf.Max(0.05f, b.extents.x - Encogido), (hasta - desde) * 0.5f, Mathf.Max(0.05f, b.extents.z - Encogido));
            Vector3 centro = new Vector3(b.center.x, (hasta + desde) * 0.5f, b.center.z);
            return Physics.OverlapBox(centro, semi, Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                .Where(c => !c.transform.IsChildOf(t) && c.bounds.max.y > desde)
                .Select(c => Ruta(c.transform)).ToList();
        }

        // Caja del objeto: sus mallas; si no tiene (un corro que nace en el juego), sus hijos con 0.6 m de radio y 1.8 m de alto.
        private static Bounds Caja(Transform t, out bool hayMallas)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
            hayMallas = rs.Length > 0;
            if (hayMallas)
            {
                Bounds b = rs[0].bounds;
                foreach (Renderer r in rs)
                {
                    b.Encapsulate(r.bounds);
                }
                return b;
            }
            Bounds caja = new Bounds(t.position + Vector3.up * 0.9f, new Vector3(1.2f, 1.8f, 1.2f));
            foreach (Transform h in t.GetComponentsInChildren<Transform>(true))
            {
                caja.Encapsulate(new Bounds(h.position + Vector3.up * 0.9f, new Vector3(1.2f, 1.8f, 1.2f)));
            }
            return caja;
        }

        /// <summary>Fotos de los dos puntos (toldos y corro de la calle de suburbios; costales) en docs/capturas/_tareas/T24/rutas-peatones.png, con la ruta de banqueta marcada en rojo. Corrida aparte.</summary>
        [MenuItem("Mecha Cyberpunk/T-1647/Fotos rutas de peatones")]
        public static void Fotos()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            List<GameObject> marcas = new List<GameObject>();
            Material rojo = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            rojo.SetColor("_BaseColor", Color.red);
            foreach ((Vector3 a, Vector3 b) in Rutas(escena))
            {
                GameObject linea = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(linea.GetComponent<Collider>());
                linea.GetComponent<Renderer>().sharedMaterial = rojo;
                linea.transform.position = (a + b) * 0.5f + Vector3.up * 0.03f;
                linea.transform.rotation = Quaternion.LookRotation(b - a);
                linea.transform.localScale = new Vector3(0.08f, 0.02f, Vector3.Distance(a, b));
                marcas.Add(linea);
            }
            const int Ancho = 960;
            const int Alto = 600;
            (Vector3 ojo, Vector3 mira)[] vistas =
            {
                (new Vector3(-87f, 9f, -2f), new Vector3(-87f, 0.5f, 8.5f)),
                (new Vector3(-80f, 3.5f, 3f), new Vector3(-80f, 0.8f, 8.5f)),
                (new Vector3(91f, 9f, 2f), new Vector3(91f, 0.5f, -10f)),
                (new Vector3(91f, 3.5f, -3f), new Vector3(91f, 0.8f, -10f)),
            };
            Texture2D hoja = new Texture2D(Ancho * 2, Alto * 2, TextureFormat.RGB24, false);
            RenderTexture rt = new RenderTexture(Ancho, Alto, 24);
            GameObject camObjeto = new GameObject("CamFotosT1647");
            Camera cam = camObjeto.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.farClipPlane = 400f;
            for (int i = 0; i < vistas.Length; i++)
            {
                camObjeto.transform.position = vistas[i].ojo;
                camObjeto.transform.LookAt(vistas[i].mira);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                hoja.ReadPixels(new Rect(0f, 0f, Ancho, Alto), (i % 2) * Ancho, (1 - i / 2) * Alto);
            }
            hoja.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            Directory.CreateDirectory("docs/capturas/_tareas/T24");
            File.WriteAllBytes("docs/capturas/_tareas/T24/rutas-peatones.png", hoja.EncodeToPNG());
            Object.DestroyImmediate(hoja);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camObjeto);
            foreach (GameObject m in marcas)
            {
                Object.DestroyImmediate(m);
            }
            Object.DestroyImmediate(rojo);
        }

        public static string Ruta(Transform t)
        {
            string r = t.name;
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                r = p.name + "/" + r;
            }
            return r;
        }
    }
}

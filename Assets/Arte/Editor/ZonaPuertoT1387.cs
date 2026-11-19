using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1387: deja el Puerto (prefab T1357_Puerto) como K15 y A15 SIN rehacerlo con ColocarT1357Puerto (conserva los arreglos de T-1360..T-1377).
    /// Edita el prefab con LoadPrefabContents. Idempotente: lo nuevo vive bajo "T1387_Puerto" y en hijos "T1387_Grafiti_*" de cada bloque, y lo que
    /// reemplaza se renombra "T1387_Reemplazado_*" y se apaga (en cada corrida se restaura antes de rehacer).
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ZonaPuertoT1387.Aplicar, luego .Validar, y en otra corrida .Fotos
    /// </summary>
    public static class ZonaPuertoT1387
    {
        public const string RutaPrefab = "Assets/Arte/Mundo/Zonas/T1357_Puerto.prefab";
        public const string Raiz = "T1387_Puerto";
        public const string RutaInforme = "Logs/t1387-aplicar.txt";
        public const string RutaValidar = "Logs/t1387-validar.txt";

        /// <summary>Variante de PU08_ContenedorGraffiti: la _a (naranja oxidado con grafiti turquesa y morado) combina con los 4 colores del patio y con K15; la _b es verde azulada con refuerzos y se pierde entre los contenedores verdes.</summary>
        public const string Variante = "_a";
        /// <summary>Variante de PU07_PlumaCaseta: la _a (caseta clara, pluma recta de franjas rojas y blancas, reflector naranja) lee mejor de noche y deja el brazo en una sola dirección.</summary>
        public const string VariantePluma = "_a";
        /// <summary>El grafiti del modelo mira hacia local -z (como PT01: puertas en +/-x). Si en la foto sale al revés, cambiar aquí.</summary>
        public const bool GrafitiEnMenosZLocal = true;

        private const string PrefijoViejo = "T1387_Reemplazado_";
        private const float AnchoCalle = ColocarT1357Puerto.AnchoCalle;
        private const float YPatio = 0f;
        private const string RutaConcreto = "Assets/Arte/Materiales/Calle18nov/M_T09_ConcretoManchado.mat";
        private const string RutaAsfalto = "Assets/Arte/Materiales/Calle18nov/M_T07_AsfaltoMojado.mat";
        private const string RutaLamina = "Assets/Arte/Materiales/Calle18nov/M_T08_LaminaOxidada.mat";
        private static readonly string[] RutasBasura =
        {
            "Assets/Arte/Calle/BS01a_Basura/BS01a_Basura.fbx", "Assets/Arte/Calle/BS01b_Basura/BS01b_Basura.fbx",
            "Assets/Arte/Calle/BS01c_Basura/BS01c_Basura.fbx", "Assets/Arte/Calle/BS01d_Basura/BS01d_Basura.fbx",
        };

        private static readonly StringBuilder informe = new StringBuilder();

        // ---------------------------------------------------------------- Medidas (con mallas: no necesita escena ni física)

        public static Bounds Limites(Transform raiz, bool soloActivos = true)
        {
            bool primero = true;
            Bounds total = new Bounds(raiz.position, Vector3.zero);
            foreach (MeshFilter mf in raiz.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null || mf.GetComponent<MeshRenderer>() == null || (soloActivos && !mf.gameObject.activeInHierarchy))
                {
                    continue;
                }
                Bounds b = CajaEnMundo(mf.transform, mf.sharedMesh.bounds);
                if (primero)
                {
                    total = b;
                    primero = false;
                }
                else
                {
                    total.Encapsulate(b);
                }
            }
            return total;
        }

        private static Bounds CajaEnMundo(Transform t, Bounds local)
        {
            Vector3 e = local.extents;
            Bounds r = new Bounds(t.TransformPoint(local.center + new Vector3(-e.x, -e.y, -e.z)), Vector3.zero);
            for (int i = 1; i < 8; i++)
            {
                Vector3 s = new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                r.Encapsulate(t.TransformPoint(local.center + s));
            }
            return r;
        }

        private static Rect Huella(Bounds b) => Rect.MinMaxRect(b.min.x, b.min.z, b.max.x, b.max.z);

        private static Rect Expandir(Rect r, float m) => Rect.MinMaxRect(r.xMin - m, r.yMin - m, r.xMax + m, r.yMax + m);

        /// <summary>Distancia en planta de un punto al eje de la calle más cercana del Puerto.</summary>
        private static float DistanciaACalles(Vector2 p)
        {
            float m = float.MaxValue;
            foreach (Vector3[] calle in ColocarT1357Puerto.Calles())
            {
                m = Mathf.Min(m, ColocarT1357Puerto.DistanciaACalle(new Vector3(p.x, 0f, p.y), calle));
            }
            return m;
        }

        /// <summary>Si algún punto de la huella (rejilla de 0.5 m y bordes) queda a menos de ancho/2 + margen de un eje de calle.</summary>
        private static bool InvadeCalle(Rect r, float margen)
        {
            float limite = AnchoCalle * 0.5f + margen;
            for (float x = r.xMin; x <= r.xMax + 0.001f; x += Mathf.Max(0.5f, Mathf.Min(0.5f, r.width)))
            {
                for (float z = r.yMin; z <= r.yMax + 0.001f; z += Mathf.Max(0.5f, Mathf.Min(0.5f, r.height)))
                {
                    if (DistanciaACalles(new Vector2(Mathf.Min(x, r.xMax), Mathf.Min(z, r.yMax))) < limite)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static GameObject Cargar(string ruta) => AssetDatabase.LoadAssetAtPath<GameObject>(ruta);

        private static GameObject BuscarPrefab(string codigo)
        {
            return AssetDatabase.FindAssets(codigo + " t:Prefab").Select(AssetDatabase.GUIDToAssetPath)
                .Where(r => { string n = Path.GetFileNameWithoutExtension(r); return n == codigo; })
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>).FirstOrDefault();
        }

        private static void Nota(string texto)
        {
            informe.AppendLine(texto);
        }

        // ---------------------------------------------------------------- Aplicar

        public static void Aplicar()
        {
            informe.Clear();
            Nota("T-1387 Puerto");
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                int lucesAntes = raiz.GetComponentsInChildren<Light>(true).Length;
                int objetosAntes = raiz.GetComponentsInChildren<Transform>(true).Length;
                Restaurar(raiz.transform);
                Transform nuevo = new GameObject(Raiz).transform;
                nuevo.SetParent(raiz.transform, false);
                KitS2c.Obra o = new KitS2c.Obra { Raiz = nuevo, Azar = new Random(13870), Nombre = Raiz };

                Piso(raiz.transform);
                Entrada(raiz.transform, nuevo);
                Grafiti(raiz.transform, o);
                Bodegas(raiz.transform);
                Gruas(raiz.transform, nuevo, o);
                Barco(raiz.transform);
                Basura(raiz.transform, nuevo, o);

                foreach (Transform t in nuevo.GetComponentsInChildren<Transform>(true))
                {
                    if (t.GetComponent<Light>() == null)
                    {
                        GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic);
                    }
                }
                int lucesDespues = raiz.GetComponentsInChildren<Light>(true).Length;
                Nota("Luces: " + lucesAntes + " antes, " + lucesDespues + " despues (no se agregan Light nuevas).");
                Nota("Objetos: " + objetosAntes + " antes, " + raiz.GetComponentsInChildren<Transform>(true).Length + " despues.");
                PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, informe.ToString());
            Debug.Log("[ZonaPuertoT1387] " + informe);
        }

        // Deshace una corrida anterior: borra lo nuevo y reactiva lo que se reemplazó.
        private static void Restaurar(Transform raiz)
        {
            Transform viejo = raiz.Find(Raiz);
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true).Where(x => x.name.StartsWith("T1387_Grafiti_")).ToList())
            {
                if (t != null)
                {
                    Object.DestroyImmediate(t.gameObject);
                }
            }
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true).Where(x => x.name.StartsWith(PrefijoViejo)).ToList())
            {
                t.name = t.name.Substring(PrefijoViejo.Length);
                t.gameObject.SetActive(true);
            }
        }

        private static void Reemplazar(GameObject go)
        {
            go.name = PrefijoViejo + go.name;
            go.SetActive(false);
        }

        // ---------------------------------------------------------------- Piso (K15: concreto mojado y manchado, no blanco granulado)

        private static void Piso(Transform raiz)
        {
            Material concreto = AssetDatabase.LoadAssetAtPath<Material>(RutaConcreto);
            Material asfalto = AssetDatabase.LoadAssetAtPath<Material>(RutaAsfalto);
            if (concreto == null)
            {
                Nota("FALTA " + RutaConcreto + ": el patio queda con su material de T-1357 (T-1382 no ha llegado a esta carpeta).");
            }
            if (asfalto == null)
            {
                Nota("FALTA " + RutaAsfalto + ": las calles quedan con su asfalto.");
            }
            int patios = 0, tramos = 0;
            foreach (Transform t in raiz)
            {
                MeshRenderer mr = t.GetComponent<MeshRenderer>();
                if (mr != null && concreto != null && (t.name == "Patio" || t.name == "MuelleSur" || t.name == "MuelleEste"))
                {
                    mr.sharedMaterial = concreto;
                    patios++;
                }
                if (asfalto != null && t.name.StartsWith("Calle_"))
                {
                    foreach (Transform h in t)
                    {
                        MeshRenderer mh = h.GetComponent<MeshRenderer>();
                        if (mh != null && h.name == "Tramo")
                        {
                            mh.sharedMaterial = asfalto;
                            tramos++;
                        }
                    }
                }
            }
            Nota("Piso: " + patios + " losas con M_T09_ConcretoManchado, " + tramos + " tramos de calle con M_T07_AsfaltoMojado.");
        }

        // ---------------------------------------------------------------- Entrada: PU07 (caseta con pluma) en U5 y U8, sin tapar el paso

        private static List<Rect> Obstaculos(Transform raiz, Transform excepto)
        {
            List<Rect> r = new List<Rect>();
            foreach (string grupo in new[] { "Contenedores", "Bodegas", "Entrada", "Postes", "Patio", "Muelle" })
            {
                foreach (Transform g in raiz)
                {
                    if (g.name != grupo || g == excepto)
                    {
                        continue;
                    }
                    foreach (Transform h in g)
                    {
                        if (!h.gameObject.activeInHierarchy || h.GetComponent<Light>() != null)
                        {
                            continue;
                        }
                        Bounds b = Limites(h);
                        if (b.size.sqrMagnitude > 0.01f && b.size.y > 0.3f)
                        {
                            r.Add(Huella(b));
                        }
                    }
                }
            }
            return r;
        }

        private static void Entrada(Transform raiz, Transform nuevo)
        {
            Transform padre = new GameObject("Entrada_PU07").transform;
            padre.SetParent(nuevo, false);
            GameObject prefab = BuscarPrefab("PU07_PlumaCaseta" + VariantePluma);
            if (prefab == null)
            {
                Nota("FALTA PU07_PlumaCaseta" + VariantePluma + ": quedan las casetas LO04 y el hueco PluminaEntrada (T-1382 no ha llegado).");
                return;
            }
            Transform entrada = raiz.Find("Entrada");
            Transform[] casetas = entrada == null ? new Transform[0] : entrada.Cast<Transform>().Where(t => t.name.StartsWith("LO04_Caseta")).ToArray();
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true).Where(x => x.name == "Hueco_PluminaEntrada").ToList())
            {
                Nota("Hueco_PluminaEntrada en " + t.position.ToString("F1") + ": lo cubre la pluma de PU07 (se deja el hueco vacio).");
            }
            // U5: la caseta va al sur del carril de Calle_2 (z -215), brazo hacia +x pegado al borde. U8: al este del carril (x 300), brazo hacia el sur.
            List<Rect> obstaculos = Obstaculos(raiz, nuevo);
            bool u5 = PonerPluma(padre, prefab, 0f, Enumerable.Range(0, 8).Select(i => new Vector2(168f + i * 2f, 0f)).ToArray(), true, ColocarT1357Puerto.U5.z - AnchoCalle * 0.5f - 0.6f, obstaculos, "U5");
            bool u8 = PonerPluma(padre, prefab, 90f, Enumerable.Range(0, 8).Select(i => new Vector2(0f, -50f - i * 2f)).ToArray(), false, ColocarT1357Puerto.U8.x + AnchoCalle * 0.5f + 0.6f, obstaculos, "U8");
            // Las casetas LO04 viejas (la de U8 en (310, -59)) se reemplazan por PU07 solo donde PU07 sí cupo.
            foreach (Transform c in casetas)
            {
                bool esU5 = c.position.x < 230f;
                if ((esU5 && u5) || (!esU5 && u8))
                {
                    Nota("Caseta LO04 en " + c.position.ToString("F1") + " reemplazada por PU07 (renombrada y apagada).");
                    Reemplazar(c.gameObject);
                }
            }
            // Nada de la entrada puede quedar sobre el carril de las puertas.
            if (entrada != null)
            {
                foreach (Transform h in entrada.Cast<Transform>().ToList())
                {
                    if (!h.gameObject.activeInHierarchy || h.name.StartsWith("Hueco_"))
                    {
                        continue;
                    }
                    Bounds b = Limites(h);
                    foreach (Vector3 u in new[] { ColocarT1357Puerto.U5, ColocarT1357Puerto.U8 })
                    {
                        Rect paso = u == ColocarT1357Puerto.U8
                            ? Rect.MinMaxRect(u.x - AnchoCalle * 0.5f, u.z - 16f, u.x + AnchoCalle * 0.5f, u.z + 0.5f)
                            : Rect.MinMaxRect(u.x - 20f, u.z - AnchoCalle * 0.5f, u.x + 0.5f, u.z + AnchoCalle * 0.5f);
                        if (b.size.y > 0.3f && Huella(b).Overlaps(paso))
                        {
                            Nota("AVISO: " + h.name + " en " + h.position.ToString("F1") + " invade el paso de la puerta " + (u == ColocarT1357Puerto.U8 ? "U8" : "U5") + ": se aparta de la calle.");
                            float desplazamiento = u == ColocarT1357Puerto.U8 ? (b.center.x >= u.x ? paso.xMax - b.min.x + 0.3f : paso.xMin - b.max.x - 0.3f)
                                : (b.center.z >= u.z ? paso.yMax - b.min.z + 0.3f : paso.yMin - b.max.z - 0.3f);
                            h.position += u == ColocarT1357Puerto.U8 ? new Vector3(desplazamiento, 0f, 0f) : new Vector3(0f, 0f, desplazamiento);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Pone PU07 con el borde junto al carril: en U5 (<paramref name="enZ"/>) el borde norte queda en z = <paramref name="borde"/> y la caseta se desliza en x por los candidatos;
        /// en U8 el borde oeste queda en x = <paramref name="borde"/> y se desliza en z. Toma el primer candidato sin invadir calles ni obstáculos.
        /// </summary>
        private static bool PonerPluma(Transform padre, GameObject prefab, float yaw, Vector2[] candidatos, bool enZ, float borde, List<Rect> obstaculos, string nombre)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
            go.name = "PU07_Pluma_" + nombre;
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            go.transform.localScale = Vector3.one;
            Bounds b0 = Limites(go.transform);
            go.transform.localScale = Vector3.one * (3.4f / Mathf.Max(0.01f, b0.size.y));
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            foreach (Vector2 c in candidatos)
            {
                Bounds b = Limites(go.transform);
                Vector3 d = new Vector3(0f, YPatio - b.min.y, 0f);
                if (enZ)
                {
                    d.x = c.x - b.center.x;
                    d.z = borde - b.max.z;
                }
                else
                {
                    d.z = c.y - b.center.z;
                    d.x = borde - b.min.x;
                }
                go.transform.position += d;
                Rect h = Huella(Limites(go.transform));
                if (!InvadeCalle(h, 0.2f) && !obstaculos.Any(r => r.Overlaps(h)))
                {
                    Nota("PU07 " + nombre + " en " + go.transform.position.ToString("F1") + ", huella " + h + ": pegada al borde del carril, colliders propios del modelo, brazo fuera del carril.");
                    return true;
                }
            }
            Nota("PU07 " + nombre + ": no cupo sin tapar el paso; no se pone (se queda la caseta LO04).");
            Object.DestroyImmediate(go);
            return false;
        }

        // ---------------------------------------------------------------- Contenedores con grafiti (PU08)

        private static void Grafiti(Transform raiz, KitS2c.Obra o)
        {
            GameObject prefab = BuscarPrefab("PU08_ContenedorGraffiti" + Variante);
            if (prefab == null)
            {
                Nota("FALTA PU08_ContenedorGraffiti" + Variante + ": no se mezclan contenedores con grafiti (T-1382 no ha llegado).");
                return;
            }
            Transform padre = raiz.Find("Contenedores");
            if (padre == null)
            {
                Nota("Grafiti: no hay grupo Contenedores.");
                return;
            }
            int cambiados = 0, uniformes = 0;
            foreach (Transform bloque in padre.Cast<Transform>().Where(t => t.name.StartsWith("Bloque_")).ToList())
            {
                List<Transform> conts = bloque.Cast<Transform>().Where(t => t.name.StartsWith("PT01_Contenedor") && t.gameObject.activeSelf).ToList();
                if (conts.Count == 0)
                {
                    continue;
                }
                List<(Transform t, Bounds b)> cajas = conts.Select(t => (t, Limites(t))).ToList();
                float zMin = cajas.Min(c => c.b.center.z), zMax = cajas.Max(c => c.b.center.z);
                foreach ((Transform t, Bounds b) in cajas)
                {
                    bool sur = Mathf.Abs(b.center.z - zMin) < 0.6f, norte = Mathf.Abs(b.center.z - zMax) < 0.6f;
                    if (!(sur || norte) || !o.Chance(0.55f))
                    {
                        continue;
                    }
                    GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, bloque);
                    g.name = "T1387_Grafiti_" + prefab.name;
                    g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    g.transform.localScale = Vector3.one;
                    Bounds b0 = Limites(g.transform);
                    bool giro = b0.size.z > b0.size.x;
                    float yaw = giro ? 90f : 0f;
                    // La cara con grafiti (local -z) mira hacia afuera del bloque: al sur (-z) en la fila sur, al norte (+z) en la fila norte.
                    bool haciaMenosZ = sur;
                    if (haciaMenosZ != GrafitiEnMenosZLocal)
                    {
                        yaw += 180f;
                    }
                    float lx = giro ? b.size.z : b.size.x, lz = giro ? b.size.x : b.size.z;
                    Vector3 e = new Vector3(lx / b0.size.x, b.size.y / b0.size.y, lz / b0.size.z);
                    if (e.x < 0.8f || e.x > 1.25f || e.y < 0.8f || e.y > 1.25f || e.z < 0.8f || e.z > 1.25f)
                    {
                        e = Vector3.one * (lx / b0.size.x);
                        uniformes++;
                    }
                    g.transform.localScale = e;
                    g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                    Bounds n = Limites(g.transform);
                    g.transform.position += new Vector3(b.center.x - n.center.x, b.min.y - n.min.y, b.center.z - n.center.z);
                    foreach (Collider c in g.GetComponentsInChildren<Collider>(true))
                    {
                        c.enabled = false; // la pila ya tiene su BoxCollider exacto en el Bloque
                    }
                    Reemplazar(t.gameObject);
                    cambiados++;
                }
                Renderer[] rend = bloque.GetComponentsInChildren<Renderer>(false).Where(r => r.gameObject.activeInHierarchy).ToArray();
                LODGroup lg = bloque.GetComponent<LODGroup>();
                if (lg == null && rend.Length > 0)
                {
                    lg = bloque.gameObject.AddComponent<LODGroup>();
                }
                if (lg != null && rend.Length > 0)
                {
                    lg.SetLODs(new[] { new LOD(0.012f, rend) });
                    lg.RecalculateBounds();
                }
            }
            Nota("Grafiti: " + cambiados + " contenedores de las orillas (fila sur y norte de cada bloque, 55%) cambiados por PU08" + Variante + ", mismos limites que el original ("
                + uniformes + " con escala uniforme por forma distinta); colliders del modelo apagados, la pila conserva su BoxCollider.");
        }

        // ---------------------------------------------------------------- Bodegas de lámina

        private static void Bodegas(Transform raiz)
        {
            Transform padre = raiz.Find("Bodegas");
            if (padre == null)
            {
                return;
            }
            Material lamina = AssetDatabase.LoadAssetAtPath<Material>(RutaLamina);
            if (lamina == null)
            {
                Nota("FALTA " + RutaLamina + ": las bodegas quedan con su material.");
            }
            int cambiados = 0;
            HashSet<string> nombres = new HashSet<string>();
            List<Transform> bodegas = padre.Cast<Transform>().Where(t => (t.name.StartsWith("PU03_") || t.name.StartsWith("PU04_")) && t.gameObject.activeInHierarchy).ToList();
            foreach (Transform b in bodegas)
            {
                if (lamina != null)
                {
                    foreach (Renderer r in b.GetComponentsInChildren<Renderer>(true))
                    {
                        Material[] ms = r.sharedMaterials;
                        for (int i = 0; i < ms.Length; i++)
                        {
                            Material m = ms[i];
                            bool liso = m != null && m != lamina && m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") == null
                                && !(m.IsKeywordEnabled("_EMISSION") && m.GetColor("_EmissionColor").maxColorComponent > 0.1f);
                            if (liso)
                            {
                                nombres.Add(m.name);
                                ms[i] = lamina;
                                cambiados++;
                            }
                        }
                        r.sharedMaterials = ms;
                    }
                }
            }
            Nota("Bodegas: " + bodegas.Count + "; " + cambiados + " materiales lisos (sin textura ni emision) cambiados por M_T08_LaminaOxidada: " + string.Join(", ", nombres));
            // La fila norte no debe morder el arroyo de las calles (atoro en (295, 0, -85)): se aparta hacia el norte con sus detalles.
            foreach (Transform b in bodegas)
            {
                Bounds caja = Limites(b);
                bool norte = caja.center.z > -100f;
                Rect h = Huella(caja);
                if (!InvadeCalle(h, 1.0f))
                {
                    continue;
                }
                Vector3 dir = norte ? Vector3.forward : Vector3.left;
                Rect zona = Expandir(h, 1f);
                zona = norte ? Rect.MinMaxRect(zona.xMin, zona.yMin - 6f, zona.xMax, zona.yMax) : Rect.MinMaxRect(zona.xMin, zona.yMin, zona.xMax + 6f, zona.yMax);
                List<Transform> grupo = padre.Cast<Transform>().Where(t => t.gameObject.activeInHierarchy && !(t.name.StartsWith("PU03_") || t.name.StartsWith("PU04_")) && !t.name.StartsWith("Extra_")
                    && zona.Contains(new Vector2(t.position.x, t.position.z))).ToList();
                grupo.Add(b);
                float s = 0f;
                float limite = norte ? -47.5f - caja.max.z : 6f;
                for (; s <= limite && InvadeCalle(Rect.MinMaxRect(h.xMin + dir.x * s, h.yMin + dir.z * s, h.xMax + dir.x * s, h.yMax + dir.z * s), 1.0f); s += 0.25f)
                {
                }
                if (s > limite)
                {
                    Nota("AVISO: bodega " + b.name + " en " + b.position.ToString("F1") + " roza el arroyo y no hay sitio hasta " + limite.ToString("F1") + " m: se deja.");
                    continue;
                }
                foreach (Transform t in grupo)
                {
                    t.position += dir * s;
                }
                Nota("Bodega " + b.name + " apartada " + s.ToString("F2") + " m del arroyo con " + (grupo.Count - 1) + " detalles.");
            }
        }

        // ---------------------------------------------------------------- Grúas: luces rojas en lo alto, sobre las grúas donde quedaron

        private static void Gruas(Transform raiz, Transform nuevo, KitS2c.Obra o)
        {
            Transform muelle = raiz.Find("Muelle");
            if (muelle == null)
            {
                return;
            }
            // Las esferas rojas de T-1357 se quedaron en la posición original aunque T-1366 movió y giró las grúas (flotaban): se quitan.
            List<Transform> viejas = muelle.Cast<Transform>().Where(t => t.name == "LuzRojaGrua").ToList();
            foreach (Transform t in viejas)
            {
                Object.DestroyImmediate(t.gameObject);
            }
            Shader shader = Shader.Find("MechaCyberpunk/Mundo/HaloLuz");
            Material rojo = shader == null ? null : KitS2c.Mat("T1387_HaloRojo", () =>
            {
                Material m = new Material(shader);
                m.SetColor("_Color", new Color(1f, 0.12f, 0.1f));
                m.SetFloat("_Intensidad", 0.9f);
                m.SetFloat("_Borde", 3.4f);
                m.SetFloat("_Caida", 2f);
                return m;
            });
            Transform padre = new GameObject("LucesGruas").transform;
            padre.SetParent(nuevo, false);
            int gruas = 0, balizas = 0;
            foreach (Transform g in muelle.Cast<Transform>().Where(t => t.name.StartsWith("PU01_Grua") && t.gameObject.activeInHierarchy))
            {
                gruas++;
                Bounds b = Limites(g);
                float y = b.max.y - 0.3f;
                Vector2[] esquinas =
                {
                    new Vector2(b.min.x + 1.5f, b.min.z + 1.5f), new Vector2(b.max.x - 1.5f, b.min.z + 1.5f),
                    new Vector2(b.min.x + 1.5f, b.max.z - 1.5f), new Vector2(b.max.x - 1.5f, b.max.z - 1.5f),
                    new Vector2(b.center.x, b.center.z),
                };
                foreach (Vector2 p in esquinas)
                {
                    KitS2c.Halo(o, padre, new Vector3(p.x, y, p.y), y - 3.5f, new Color(1f, 0.12f, 0.1f));
                    Transform h = padre.GetChild(padre.childCount - 1);
                    MeshRenderer mr = h.GetComponent<MeshRenderer>();
                    if (mr != null && rojo != null)
                    {
                        mr.sharedMaterial = rojo;
                    }
                    h.name = "BalizaRoja";
                    balizas++;
                }
            }
            Nota("Grúas: " + viejas.Count + " esferas rojas viejas (primitivos, flotando en su posicion original) quitadas; " + balizas + " balizas rojas (halo emisivo) sobre " + gruas + " grúas activas, en lo alto de su caja.");
        }

        private static void Barco(Transform raiz)
        {
            Transform muelle = raiz.Find("Muelle");
            Transform barco = muelle == null ? null : muelle.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("PU02_Barco"));
            if (barco == null)
            {
                Nota("Barco: no hay PU02_Barco en el Muelle del prefab. No hay otro modelo de barco o carguero en Assets/Arte (solo PU02_Barco): queda anotado, sin primitivos.");
                return;
            }
            Bounds b = Limites(barco);
            Nota("Barco: PU02_Barco existe (activo " + barco.gameObject.activeInHierarchy + "), caja x " + b.min.x.ToString("F0") + ".." + b.max.x.ToString("F0") + ", z " + b.min.z.ToString("F0") + ".." + b.max.z.ToString("F0")
                + ", y " + b.min.y.ToString("F1") + ".." + b.max.y.ToString("F1") + ". No hay modelo de carguero distinto; el barco no se mueve (T-1366 lo dejo a 1..3 m del muelle).");
        }

        // ---------------------------------------------------------------- Basura en el piso (BS01a..d, sin collider)

        private static void Basura(Transform raiz, Transform nuevo, KitS2c.Obra o)
        {
            List<GameObject> modelos = RutasBasura.Select(Cargar).Where(g => g != null).ToList();
            if (modelos.Count == 0)
            {
                Nota("FALTA BS01a..d_Basura (Assets/Arte/Calle): sin basura en el piso.");
                return;
            }
            Transform padre = new GameObject("Basura").transform;
            padre.SetParent(nuevo, false);
            List<Rect> obstaculos = Obstaculos(raiz, nuevo).Select(r => Expandir(r, 0.8f)).ToList();
            int puestos = 0;
            for (int intento = 0; intento < 600 && puestos < 40; intento++)
            {
                Vector2 p = new Vector2(o.Rango(ColocarT1357Puerto.X0 + 8f, 312f), o.Rango(-235f, -52f));
                Rect h = Rect.MinMaxRect(p.x - 0.8f, p.y - 0.8f, p.x + 0.8f, p.y + 0.8f);
                if (obstaculos.Any(r => r.Overlaps(h)) || DistanciaACalles(p) < AnchoCalle * 0.5f + 0.8f)
                {
                    continue;
                }
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(o.Uno(modelos), padre);
                go.name = "Basura_" + puestos;
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                go.transform.localScale = Vector3.one;
                Bounds b = Limites(go.transform);
                go.transform.localScale = Vector3.one * (o.Rango(0.5f, 0.9f) / Mathf.Max(0.01f, b.size.y));
                go.transform.rotation = Quaternion.Euler(0f, o.Rango(0f, 360f), 0f);
                b = Limites(go.transform);
                go.transform.position += new Vector3(p.x - b.center.x, YPatio - b.min.y, p.y - b.center.z);
                foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
                {
                    c.enabled = false;
                }
                puestos++;
            }
            Nota("Basura: " + puestos + " piezas BS01 sobre el patio (y = 0), fuera de carriles y de cualquier pieza (0.8 m), sin collider.");
        }

        // ---------------------------------------------------------------- Validar

        public static void Validar()
        {
            StringBuilder v = new StringBuilder("T-1387 validar Puerto\n");
            int problemas = 0;
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Transform nuevo = raiz.transform.Find(Raiz);
                if (nuevo == null)
                {
                    v.AppendLine("FALLA: no hay T1387_Puerto (corre Aplicar).");
                    problemas++;
                }
                else
                {
                    foreach (Transform t in nuevo.GetComponentsInChildren<Transform>(true).Where(x => x.name.StartsWith("Basura_") || x.name.StartsWith("PU07_Pluma_")))
                    {
                        Bounds b = Limites(t);
                        if (Mathf.Abs(b.min.y - YPatio) > 0.05f)
                        {
                            v.AppendLine("FALLA asiento: " + t.name + " base en y " + b.min.y.ToString("F2"));
                            problemas++;
                        }
                        if (t.name.StartsWith("PU07_Pluma_") && InvadeCalle(Huella(b), 0f))
                        {
                            v.AppendLine("FALLA: " + t.name + " invade el carril");
                            problemas++;
                        }
                    }
                }
                foreach (Transform bloque in raiz.transform.Find("Contenedores").Cast<Transform>().Where(t => t.name.StartsWith("Bloque_")))
                {
                    List<Transform> activos = bloque.Cast<Transform>().Where(t => t.gameObject.activeInHierarchy && (t.name.StartsWith("PT01_Contenedor") || t.name.StartsWith("T1387_Grafiti_"))).ToList();
                    foreach (Transform g in activos.Where(t => t.name.StartsWith("T1387_Grafiti_")))
                    {
                        Bounds a = Limites(g);
                        a.Expand(-0.1f);
                        foreach (Transform otro in activos.Where(t => t != g))
                        {
                            if (a.Intersects(Limites(otro)))
                            {
                                v.AppendLine("FALLA encimado: " + bloque.name + "/" + g.name + " con " + otro.name);
                                problemas++;
                            }
                        }
                        float min = Limites(g).min.y;
                        bool apoyado = Mathf.Abs(min - YPatio) < 0.05f || activos.Any(o2 => o2 != g && Mathf.Abs(Limites(o2).max.y - min) < 0.06f && Huella(Limites(o2)).Overlaps(Huella(Limites(g))));
                        if (!apoyado)
                        {
                            v.AppendLine("FALLA flotando: " + bloque.name + "/" + g.name + " base en y " + min.ToString("F2"));
                            problemas++;
                        }
                    }
                }
                Transform bodegas = raiz.transform.Find("Bodegas");
                if (bodegas != null)
                {
                    foreach (Transform b in bodegas.Cast<Transform>().Where(t => (t.name.StartsWith("PU03_") || t.name.StartsWith("PU04_")) && t.gameObject.activeInHierarchy))
                    {
                        if (InvadeCalle(Huella(Limites(b)), 0f))
                        {
                            v.AppendLine("FALLA: bodega " + b.name + " en " + b.position.ToString("F1") + " sobre el arroyo");
                            problemas++;
                        }
                    }
                }
                Rect puertaU8 = Rect.MinMaxRect(ColocarT1357Puerto.U8.x - 6f, ColocarT1357Puerto.U8.z - 16f, ColocarT1357Puerto.U8.x + 6f, ColocarT1357Puerto.U8.z + 0.5f);
                foreach (Transform g in raiz.transform)
                {
                    if (g.name != "Entrada" && g.name != "Bodegas")
                    {
                        continue;
                    }
                    foreach (Transform h in g)
                    {
                        Bounds b = Limites(h);
                        if (h.gameObject.activeInHierarchy && h.GetComponent<Light>() == null && b.size.y > 0.3f && Huella(b).Overlaps(puertaU8))
                        {
                            v.AppendLine("FALLA: " + g.name + "/" + h.name + " tapa la puerta U8");
                            problemas++;
                        }
                    }
                }
                int luces = raiz.GetComponentsInChildren<Light>(true).Length;
                int objetos = raiz.GetComponentsInChildren<Transform>(true).Length;
                v.AppendLine("Luces: " + luces + " (tope: las de antes + 0); objetos: " + objetos);
                if (nuevo != null)
                {
                    v.AppendLine("Objetos nuevos bajo T1387_Puerto: " + nuevo.GetComponentsInChildren<Transform>(true).Length + " (tope 3000)");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
            v.AppendLine(problemas == 0 ? "OK: sin hallazgos." : "HALLAZGOS: " + problemas);
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaValidar, v.ToString());
            Debug.Log("[ZonaPuertoT1387] " + v);
        }

        // ---------------------------------------------------------------- Fotos (no guardan nada; en una corrida aparte de Aplicar)

        /// <summary>Cámara de K15: a la altura de los ojos en el pasillo de x = 160 (a la derecha están las bodegas, a la izquierda los contenedores), lejos de los pallets de la primera bodega.</summary>
        public static readonly Vector3 CamaraK15 = new Vector3(158.6f, 2.3f, -84f);
        public static readonly Vector3 MiraK15 = new Vector3(172f, 24f, -255f);

        public static void Fotos()
        {
            Directory.CreateDirectory("docs/capturas/t-1387");
            ColocarS2.Abrir();
            ColocarS2cLook.Foto(CamaraK15, MiraK15, "docs/capturas/t-1387/puerto-juego-k15.png", 55f);
            ColocarS2.LadoALado("imageassets/referencias/look/K15_Puerto.png", "docs/capturas/t-1387/puerto-juego-k15.png", "docs/capturas/t-1387/puerto-k15.png");
            ColocarS2cLook.Foto(new Vector3(322f, 72f, -48f), new Vector3(225f, 0f, -175f), "docs/capturas/t-1387/puerto-juego-a15.png", 60f);
            ColocarS2.LadoALado("imageassets/referencias/look/A15_Puerto_Aerea.png", "docs/capturas/t-1387/puerto-juego-a15.png", "docs/capturas/t-1387/puerto-a15.png");
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }
    }
}

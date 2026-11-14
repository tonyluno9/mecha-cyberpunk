using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-995: recorrido por Ciudad_Prologo. Busca piezas del kit encimadas (metidas unas en otras, duplicadas o dentro de fachadas) y renderers con
    /// textura rota (material nulo, shader de error, material liso sin _BaseMap en piezas del kit, textura con guid roto). <see cref="Arreglar"/> quita
    /// duplicados, apaga encimados y corrige materiales con el .mat de la carpeta del modelo. Informe: Logs/t995-revision.txt.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.RevisarCiudadT995.Informe | Arreglar
    /// </summary>
    public static class RevisarCiudadT995
    {
        /// <summary>Fracción del volumen de la pieza menor que debe quedar dentro de la otra para contarla como encimada.</summary>
        public const float FraccionEncimada = 0.5f;
        /// <summary>Distancia (m) bajo la cual dos instancias del mismo prefab cuentan como duplicadas.</summary>
        public const float DistanciaDuplicada = 0.1f;
        private const float GrosorMinimo = 0.05f;
        private const float MinimoRenderer = 0.15f;
        private const float MitadSonda = 0.08f;
        private const float MaximoProp = 8f;
        private const float MaximoPieza = 40f;
        private const float AlcanceSonda = 60f;
        private const string RutaInforme = "Logs/t995-revision.txt";

        private static readonly string[] SaltarRaiz = { "JugadorAPie", "AMB_", "GTA_T405", "GTA_T406", "Trafico", "JaulaClandestino", "MechaEstacionado", "T541_Torres", "T513", "S2_PandillasAcoso" };
        private static readonly string[] SaltarNombre = { "Cable", "Nube", "Luz", "Charco", "Linea", "Cebra", "Raya", "Sombra", "Brillo", "Neon", "Cielo", "Fondo", "Halo", "Decal", "Personaje", "Vehiculo", "Carro", "Dron", "Letrero", "Pantalla", "Antena", "Cornisa", "Franja", "Banqueta", "Pretil", "Halo" };
        /// <summary>Piezas de estructura: se apilan y se pegan entre sí a propósito, no se comparan entre ellas.</summary>
        private static readonly string[] Estructura = { "Fachada", "Piso", "Cornisa", "Franja", "Esquina", "Pretil", "Muro", "Banqueta", "Edificio", "Torre", "Techo", "Escalera", "Pasarela", "Puente", "Calle", "Suelo", "Rampa", "Acera", "Interior", "Cuarto", "Casa", "Taller" };
        private static readonly string[] SaltarRutaAsset = { "Assets/Arte/Personajes", "Assets/Arte/Vehiculos", "Assets/Arte/Armas" };

        /// <summary>
        /// Pares que se dejan aunque las cajas se crucen, a propósito: postes y transformadores (su malla es un brazo largo con la caja casi vacía),
        /// focos pegados bajo un toldo, el aire acondicionado sobre su base del tinaco, la basura regada junto a su bote, el banco de Tino (no se toca) y el callejón S2_Callejon (sus cajas y registros sirven de escalón a los accesos a techos: TechosS2Pruebas).
        /// </summary>
        private static readonly string[] ParejasPermitidas = { "Poste", "Transformador", "FocoPared", "BaseTinaco", "Basura", "TallerTino", "S2_Callejon/" };

        /// <summary>Excepciones legítimas (nombre de pieza que se deja aunque salga): vacío si no hay.</summary>
        public static readonly string[] Excepciones = { };

        public sealed class Encimado
        {
            public Transform A;
            public Transform B;
            public string Tipo;
            public string Detalle;
            public float Medida;
            public override string ToString() => Tipo + " " + Medida.ToString("F2") + ": " + Ruta(A) + (B != null ? "  <->  " + Ruta(B) : "") + " @" + A.position.ToString("F1") + (Detalle != null ? "  {" + Detalle + "}" : "");
        }

        public sealed class Rota
        {
            public Renderer Renderer;
            public int Indice;
            public string Motivo;
            public override string ToString() => Motivo + ": " + Ruta(Renderer.transform) + " [" + Indice + "]";
        }

        private static string FuenteAsset(Transform t)
        {
            GameObject f = PrefabUtility.GetCorrespondingObjectFromOriginalSource(t.gameObject);
            return f == null ? "" : AssetDatabase.GetAssetPath(f);
        }

        private static IEnumerable<Transform> Piezas()
        {
            return ColocarS2.Todos().Where(t => t.gameObject.activeInHierarchy && PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)
                    && !SaltarRaiz.Any(s => t.root.name.StartsWith(s)) && !SaltarNombre.Any(n => t.name.Contains(n)) && !Excepciones.Contains(t.name))
                .Where(t =>
                {
                    string ruta = FuenteAsset(t);
                    return ruta.StartsWith("Assets/Arte/") && !SaltarRutaAsset.Any(ruta.StartsWith);
                })
                .Where(t => !(t.name == "Visual" && t.parent != null && PrefabUtility.IsAnyPrefabInstanceRoot(t.parent.gameObject)));
        }

        private static bool Permitida(Transform t)
        {
            string ruta = Ruta(t);
            return ParejasPermitidas.Any(p => ruta.Contains(p));
        }

        private static bool EsEstructura(Transform t)
        {
            string n = t.name + " " + Path.GetFileNameWithoutExtension(FuenteAsset(t));
            return Estructura.Any(e => n.Contains(e));
        }

        private static bool Limites(Transform t, out Bounds b)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>().Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToArray();
            b = default;
            if (rs.Length == 0)
            {
                return false;
            }
            b = rs[0].bounds;
            foreach (Renderer r in rs)
            {
                b.Encapsulate(r.bounds);
            }
            return true;
        }

        private static float Volumen(Bounds b)
        {
            return Mathf.Max(b.size.x, GrosorMinimo) * Mathf.Max(b.size.y, GrosorMinimo) * Mathf.Max(b.size.z, GrosorMinimo);
        }

        private static float VolumenInterseccion(Bounds a, Bounds b)
        {
            float x = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
            float y = Mathf.Min(a.max.y, b.max.y) - Mathf.Max(a.min.y, b.min.y);
            float z = Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z);
            if (x <= 0f || y <= 0f || z <= 0f)
            {
                return 0f;
            }
            return x * y * z;
        }

        // Compara malla contra malla (renderers de cada pieza), no la caja de toda la pieza: un poste o un letrero tienen cajas enormes y vacías.
        private static string detalle = "";

        private static float MayorFraccion(Transform a, Transform b)
        {
            float mejor = 0f;
            detalle = "";
            Renderer[] ra = a.GetComponentsInChildren<Renderer>().Where(RenderizaMalla).ToArray();
            Renderer[] rb = b.GetComponentsInChildren<Renderer>().Where(RenderizaMalla).ToArray();
            foreach (Renderer x in ra)
            {
                foreach (Renderer y in rb)
                {
                    if (!x.bounds.Intersects(y.bounds))
                    {
                        continue;
                    }
                    float f = VolumenInterseccion(x.bounds, y.bounds) / Mathf.Min(Volumen(x.bounds), Volumen(y.bounds));
                    if (f > mejor)
                    {
                        mejor = f;
                        detalle = x.name + x.bounds.size.ToString("F1") + " x " + y.name + y.bounds.size.ToString("F1");
                    }
                }
            }
            return mejor;
        }

        private static bool RenderizaMalla(Renderer r)
        {
            return r.enabled && !(r is ParticleSystemRenderer) && r.bounds.size.magnitude >= MinimoRenderer && !SaltarNombre.Any(n => r.name.Contains(n));
        }

        /// <summary>Pares encimados (props que se meten unos en otros, duplicados) y props con el centro dentro de una fachada.</summary>
        public static List<Encimado> Encimados()
        {
            ColocarS2.Abrir();
            var salida = new List<Encimado>();
            var lista = new List<(Transform t, Bounds b, bool estructura, string prefab)>();
            foreach (Transform t in Piezas())
            {
                if (Limites(t, out Bounds b) && b.size.magnitude < MaximoPieza)
                {
                    lista.Add((t, b, EsEstructura(t), FuenteAsset(t)));
                }
            }
            for (int i = 0; i < lista.Count; i++)
            {
                for (int j = i + 1; j < lista.Count; j++)
                {
                    var a = lista[i];
                    var c = lista[j];
                    if (a.t.IsChildOf(c.t) || c.t.IsChildOf(a.t))
                    {
                        continue;
                    }
                    if (a.prefab == c.prefab && Vector3.Distance(a.t.position, c.t.position) < DistanciaDuplicada)
                    {
                        salida.Add(new Encimado { A = a.t, B = c.t, Tipo = "duplicado", Medida = Vector3.Distance(a.t.position, c.t.position) });
                        continue;
                    }
                    if (a.estructura || c.estructura || !a.b.Intersects(c.b) || Permitida(a.t) || Permitida(c.t))
                    {
                        continue;
                    }
                    float frac = MayorFraccion(a.t, c.t);
                    if (frac > FraccionEncimada)
                    {
                        salida.Add(new Encimado { A = a.t, B = c.t, Tipo = "encimado", Medida = frac, Detalle = detalle });
                    }
                }
            }
            salida.AddRange(PropsEnFachadas(lista.Where(l => !l.estructura).Select(l => (l.t, l.b)).ToList(), lista.Where(l => l.estructura).Select(l => l.t).ToList()));
            return salida;
        }

        // Props cuyo centro cae dentro de la malla de una fachada/edificio (colliders temporales de malla en las piezas de estructura).
        private static List<Encimado> PropsEnFachadas(List<(Transform t, Bounds b)> props, List<Transform> estructuras)
        {
            var salida = new List<Encimado>();
            var temporales = new List<MeshCollider>();
            var duenos = new Dictionary<Collider, Transform>();
            foreach (Transform e in estructuras)
            {
                foreach (MeshFilter mf in e.GetComponentsInChildren<MeshFilter>())
                {
                    Renderer r = mf.GetComponent<Renderer>();
                    if (mf.sharedMesh == null || r == null || !r.enabled || mf.GetComponent<Collider>() != null)
                    {
                        continue;
                    }
                    MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    temporales.Add(mc);
                    duenos[mc] = e;
                }
            }
            Physics.SyncTransforms();
            try
            {
                foreach (var (t, b) in props)
                {
                    if (b.size.magnitude > MaximoProp || b.size.y < 0.3f)
                    {
                        continue;
                    }
                    Collider[] cerca = Physics.OverlapBox(b.center, Vector3.one * MitadSonda, Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                    foreach (Collider c in cerca)
                    {
                        if (c is MeshCollider mc && duenos.TryGetValue(mc, out Transform dueno) && EstaDentro(mc, b.center) && !t.IsChildOf(dueno))
                        {
                            salida.Add(new Encimado { A = t, B = dueno, Tipo = "dentro-de-fachada", Medida = b.size.magnitude });
                            break;
                        }
                    }
                }
            }
            finally
            {
                foreach (MeshCollider mc in temporales)
                {
                    Object.DestroyImmediate(mc);
                }
            }
            return salida;
        }

        // Dentro de una malla: desde el punto, los rayos en 5 direcciones golpean una cara por su espalda (normal hacia el rayo).
        private static bool EstaDentro(MeshCollider mc, Vector3 p)
        {
            int dentro = 0;
            foreach (Vector3 d in new[] { Vector3.up, Vector3.right, Vector3.forward, Vector3.back, Vector3.left })
            {
                if (mc.Raycast(new Ray(p, d), out RaycastHit h, AlcanceSonda) && Vector3.Dot(h.normal, d) > 0f)
                {
                    dentro++;
                }
            }
            return dentro >= 4;
        }

        /// <summary>Renderers activos con material nulo, shader de error o no soportado, liso sin _BaseMap en piezas del kit, o textura con guid roto.</summary>
        public static List<Rota> TexturasRotas()
        {
            ColocarS2.Abrir();
            var salida = new List<Rota>();
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!r.enabled || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer || r.transform.root.name == "JugadorAPie")
                {
                    continue;
                }
                Material[] ms = r.sharedMaterials;
                bool delKit = EsDelKit(r);
                for (int i = 0; i < ms.Length; i++)
                {
                    string motivo = Motivo(ms[i], delKit);
                    if (motivo != null)
                    {
                        salida.Add(new Rota { Renderer = r, Indice = i, Motivo = motivo });
                    }
                }
            }
            return salida;
        }

        private static bool EsDelKit(Renderer r)
        {
            for (Transform t = r.transform; t != null; t = t.parent)
            {
                string ruta = FuenteAsset(t);
                if (ruta.StartsWith("Assets/Arte/") && !SaltarRutaAsset.Any(ruta.StartsWith))
                {
                    return true;
                }
            }
            return false;
        }

        private static string Motivo(Material m, bool delKit)
        {
            if (m == null)
            {
                return "material-nulo";
            }
            if (m.shader == null || m.shader.name == "Hidden/InternalErrorShader" || !m.shader.isSupported)
            {
                return "shader-error";
            }
            if (m.name.StartsWith("Default-Material"))
            {
                return "default-material";
            }
            if (TexturaRota(m))
            {
                return "textura-rota";
            }
            if (delKit && m.shader.name == "Universal Render Pipeline/Lit" && m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") == null
                && AssetDatabase.GetAssetPath(m).StartsWith("Assets/Arte/") && EsBlancoLiso(m))
            {
                return "liso-blanco-sin-textura";
            }
            return null;
        }

        private static bool EsBlancoLiso(Material m)
        {
            Color c = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
            return c.r > 0.9f && c.g > 0.9f && c.b > 0.9f && !m.IsKeywordEnabled("_EMISSION");
        }

        private static bool TexturaRota(Material m)
        {
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(m)))
            {
                return false;
            }
            SerializedProperty envs = new SerializedObject(m).FindProperty("m_SavedProperties.m_TexEnvs");
            if (envs == null)
            {
                return false;
            }
            for (int i = 0; i < envs.arraySize; i++)
            {
                SerializedProperty tex = envs.GetArrayElementAtIndex(i).FindPropertyRelative("second.m_Texture");
                if (tex != null && tex.objectReferenceValue == null && tex.objectReferenceInstanceIDValue != 0)
                {
                    return true;
                }
            }
            return false;
        }

        [MenuItem("Mecha Cyberpunk/T995/Informe")]
        public static void Informe()
        {
            Escribir("ANTES", Encimados(), TexturasRotas(), null);
        }

        [MenuItem("Mecha Cyberpunk/T995/Arreglar")]
        public static void Arreglar()
        {
            ColocarS2.Informe.Clear();
            var antesE = Encimados();
            var antesT = TexturasRotas();
            StringBuilder acc = new StringBuilder();
            var apagados = new HashSet<Transform>();
            foreach (Encimado e in antesE)
            {
                // Duplicado: se quita la segunda. Dentro de fachada: se apaga el prop. Encimado: se apaga la pieza menor.
                Transform quitar = e.Tipo == "duplicado" ? e.B : (e.Tipo == "dentro-de-fachada" ? e.A : MenorVolumen(e.A, e.B));
                if (e.Tipo == "duplicado" && apagados.Contains(e.A))
                {
                    continue; // el original ya se apagó por otro motivo: no apagar también la copia
                }
                if (apagados.Add(quitar))
                {
                    quitar.gameObject.SetActive(false);
                    acc.AppendLine("apagado (" + e.Tipo + "): " + Ruta(quitar));
                }
            }
            foreach (Rota r in antesT)
            {
                acc.AppendLine((ArreglarMaterial(r) ?? "sin arreglo") + " <- " + r);
            }
            ColocarS2.Informe.Append(acc);
            ColocarS2.Guardar("t995-arreglos");
            Escribir("DESPUES", Encimados(), TexturasRotas(), acc.ToString());
        }

        private static Transform MenorVolumen(Transform a, Transform b)
        {
            Limites(a, out Bounds ba);
            Limites(b, out Bounds bb);
            return Volumen(ba) <= Volumen(bb) ? a : b;
        }

        // Busca en la carpeta del modelo (y la de arriba) un .mat con el nombre del material roto que sí traiga _BaseMap.
        private static string ArreglarMaterial(Rota r)
        {
            Material actual = r.Renderer.sharedMaterials[r.Indice];
            string fuente = null;
            for (Transform t = r.Renderer.transform; t != null && string.IsNullOrEmpty(fuente); t = t.parent)
            {
                string p = FuenteAsset(t);
                if (p.StartsWith("Assets/Arte/"))
                {
                    fuente = p;
                }
            }
            if (string.IsNullOrEmpty(fuente))
            {
                return null;
            }
            string carpeta = Path.GetDirectoryName(fuente).Replace('\\', '/');
            string nombre = actual != null ? actual.name.Replace(" (Instance)", "") : r.Renderer.name;
            foreach (string c in new[] { carpeta, Path.GetDirectoryName(carpeta).Replace('\\', '/') })
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { c }))
                {
                    Material cand = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                    if (cand == null || cand == actual || cand.shader == null || !cand.shader.isSupported || TexturaRota(cand) || !cand.HasProperty("_BaseMap") || cand.GetTexture("_BaseMap") == null)
                    {
                        continue;
                    }
                    if (cand.name == nombre || cand.name.Contains(nombre) || nombre.Contains(cand.name))
                    {
                        Material[] ms = r.Renderer.sharedMaterials;
                        ms[r.Indice] = cand;
                        r.Renderer.sharedMaterials = ms;
                        return "material -> " + AssetDatabase.GetAssetPath(cand);
                    }
                }
            }
            return null;
        }

        private static void Escribir(string titulo, List<Encimado> e, List<Rota> t, string acciones)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-995 " + titulo + ": encimados " + e.Count + ", texturas rotas " + t.Count);
            foreach (var g in e.GroupBy(x => x.Tipo))
            {
                sb.AppendLine("  " + g.Key + ": " + g.Count());
            }
            foreach (var g in t.GroupBy(x => x.Motivo))
            {
                sb.AppendLine("  " + g.Key + ": " + g.Count());
            }
            sb.AppendLine("-- encimados");
            foreach (Encimado x in e.OrderBy(x => x.Tipo))
            {
                sb.AppendLine(x.ToString());
            }
            sb.AppendLine("-- texturas");
            foreach (Rota x in t.OrderBy(x => x.Motivo))
            {
                sb.AppendLine(x.ToString());
            }
            if (acciones != null)
            {
                sb.AppendLine("-- acciones");
                sb.Append(acciones);
            }
            Directory.CreateDirectory("Logs");
            File.AppendAllText(RutaInforme, sb + "\n");
            Debug.Log("[Arte] " + sb);
        }

        private static string Ruta(Transform t)
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

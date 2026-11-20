using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1501: barrido de la ciudad (Ciudad_Prologo.unity). Detecta y arregla objetos flotando, objetos y carros hundidos, y calles/banquetas
    /// que pierden la textura. Solo mueve en Y, nunca en XZ.
    ///
    /// Criterio de "unidad": cada MeshRenderer activo sube por la jerarquía hasta el ancestro más alto cuyo conjunto de renderers mida
    /// 30 m o menos en X y Z (un edificio, un poste, una banca). Esa unidad se evalúa por su base (la parte más baja de sus renderers).
    /// Flotante: desde 5 puntos de la base (centro y 4 esquinas metidas 10 %) cae un rayo hacia abajo ignorando los colliders de la propia
    /// unidad; si TODOS los puntos que encuentran algo quedan a más de 5 cm del piso y a 1 m o menos, flota. Si la distancia es mayor de 1 m
    /// se considera elevado a propósito (letreros en paredes, cosas en azoteas). Hundido: todos los puntos quedan más de 5 cm bajo la
    /// superficie que tienen encima (hasta 2 m). Se ignoran por nombre (ver <see cref="Ignorados"/>, en la unidad o cualquier ancestro) las
    /// cosas que deben flotar o moverse, y lo que tiene un Rigidbody dinámico (se asienta solo al jugar).
    /// Carros: objetos con hijos Rueda_DI/DD/TI/TD: se mide la parte más baja de cada llanta contra el piso que tiene debajo.
    /// Calles: renderers cuyo nombre (o el de su padre) es asfalto/calle/banqueta/acera/..., con material nulo, shader de error, textura
    /// base faltante en un material Barrio_*, escala de textura 0 o extrema (menos de 0.01 o más de 400), o malla sin UV.
    /// Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.BarridoCiudadT1501.Diagnostico  (escribe Logs/t1501-diagnostico.txt)
    ///            -executeMethod MechaCyberpunk.Mundo.Editor.BarridoCiudadT1501.Arreglar     (escribe Logs/t1501-arreglos.txt y guarda la escena)
    /// </summary>
    public static class BarridoCiudadT1501
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string RutaDiagnostico = "Logs/t1501-diagnostico.txt";
        public const string RutaArreglos = "Logs/t1501-arreglos.txt";
        private const string RutaAsfalto = "Assets/_Proyecto/Mundo/Materiales/Barrio_Asfalto.mat";
        private const string RutaBanqueta = "Assets/_Proyecto/Mundo/Materiales/Barrio_Banqueta.mat";

        public const float Tolerancia = 0.05f;
        public const float MaximoFlotante = 1f;
        public const float MaximoHundido = 2f;
        public const float ExtensionMaximaUnidad = 30f;
        private const float EscalaMinima = 0.01f;
        private const float EscalaMaxima = 400f;

        /// <summary>Fragmentos de nombre (minúsculas) de cosas que deben flotar, colgar o moverse: no cuentan como flotantes ni hundidas.</summary>
        public static readonly string[] Ignorados =
        {
            "letrero", "neon", "neón", "luz", "luces", "light", "foco", "cable", "dron", "nave", "toldo", "lampara", "lámpara", "holo",
            "aviso", "cartel", "anuncio", "pantalla", "ventana", "vapor", "humo", "niebla", "nube", "cielo", "sky", "luna", "marca", "raya",
            "flecha", "tendedero", "jugador", "peaton", "peatón", "npc", "policia", "policía", "enemigo", "centinela", "emboscada", "arena"
        };

        private static readonly string[] NombresRuedas = { "Rueda_DI", "Rueda_DD", "Rueda_TI", "Rueda_TD" };
        private static readonly Regex NombreCalle = new Regex("asfalto|calle(?!jon)|banqueta|acera|calzada|pavimento|cruce|camell|road|street|sidewalk", RegexOptions.IgnoreCase);
        private static readonly Regex NombreNoCalle = new Regex("marca|raya|linea|línea|charco|letrero|poste|luz", RegexOptions.IgnoreCase);
        private static readonly Regex NombreBanqueta = new Regex("banqueta|acera|sidewalk", RegexOptions.IgnoreCase);

        private static HashSet<Transform> carrosActuales = new HashSet<Transform>();

        public enum Tipo { Flotante, Hundido, CarroFlotante, CarroHundido, CalleSinMaterial, CalleShaderError, CalleSinTextura, CalleEscalaCero, CalleTilingExtremo, CalleSinUV }

        public class Hallazgo
        {
            public Tipo Tipo;
            public string Barrio;
            public string Nombre;
            public string Ruta;
            /// <summary>Metros (flotante: separación; hundido: profundidad) o la escala de textura.</summary>
            public float Medida;
            public Transform Objeto;
            public Renderer Renderer;
            public float AjusteY;
        }

        [MenuItem("Mecha Cyberpunk/T-1501 Diagnóstico de la ciudad")]
        public static void Diagnostico()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            List<Hallazgo> todos = Analizar(escena);
            Escribir(RutaDiagnostico, "T-1501 diagnóstico de " + RutaEscena, todos, null);
            Debug.Log("T-1501 diagnóstico: " + todos.Count + " hallazgos, ver " + RutaDiagnostico);
        }

        [MenuItem("Mecha Cyberpunk/T-1501 Arreglar la ciudad")]
        public static void Arreglar()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            List<Hallazgo> antes = Analizar(escena);
            StringBuilder movidos = new StringBuilder();
            int cambios = 0;

            foreach (Hallazgo h in antes.Where(x => x.Tipo <= Tipo.CarroHundido))
            {
                if (h.Objeto == null)
                {
                    continue;
                }
                Undo.RecordObject(h.Objeto, "T-1501");
                Vector3 p = h.Objeto.position;
                h.Objeto.position = new Vector3(p.x, p.y + h.AjusteY, p.z);   // solo Y
                EditorUtility.SetDirty(h.Objeto);
                movidos.AppendLine("MOVIDO\t" + h.Tipo + "\t" + h.Barrio + "\t" + h.Ruta + "\tY " + p.y.ToString("F3") + " -> " + h.Objeto.position.y.ToString("F3"));
                cambios++;
                if (h.Tipo == Tipo.CarroFlotante || h.Tipo == Tipo.CarroHundido)
                {
                    Rigidbody rb = h.Objeto.GetComponent<Rigidbody>();
                    if (rb != null && rb.collisionDetectionMode == CollisionDetectionMode.Discrete)
                    {
                        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;   // que el piso no se le cuele al arrancar
                        EditorUtility.SetDirty(rb);
                        movidos.AppendLine("FISICA\tCarro\t" + h.Ruta + "\tcollisionDetectionMode Discrete -> ContinuousDynamic");
                    }
                }
            }
            Physics.SyncTransforms();

            Material asfalto = AssetDatabase.LoadAssetAtPath<Material>(RutaAsfalto);
            Material banqueta = AssetDatabase.LoadAssetAtPath<Material>(RutaBanqueta);
            foreach (Hallazgo h in antes.Where(x => x.Tipo >= Tipo.CalleSinMaterial && x.Tipo != Tipo.CalleSinUV).GroupBy(x => x.Renderer).Select(g => g.First()))
            {
                if (h.Renderer == null)
                {
                    continue;
                }
                string contexto = h.Renderer.name + " " + (h.Renderer.transform.parent != null ? h.Renderer.transform.parent.name : "");
                Material correcto = NombreBanqueta.IsMatch(contexto) ? banqueta : asfalto;
                if (correcto == null)
                {
                    movidos.AppendLine("FALTA material de calle para " + h.Ruta);
                    continue;
                }
                Undo.RecordObject(h.Renderer, "T-1501");
                Material[] mats = h.Renderer.sharedMaterials;
                if (mats.Length == 0)
                {
                    mats = new Material[1];
                }
                for (int i = 0; i < mats.Length; i++)
                {
                    if (MaterialMalo(mats[i], out _) || FueraDeEscala(mats[i]))
                    {
                        mats[i] = correcto;
                    }
                }
                h.Renderer.sharedMaterials = mats;
                EditorUtility.SetDirty(h.Renderer);
                movidos.AppendLine("MATERIAL\t" + h.Tipo + "\t" + h.Barrio + "\t" + h.Ruta + "\t-> " + correcto.name);
                cambios++;
            }

            List<Hallazgo> despues = Analizar(escena);
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Escribir(RutaArreglos, "T-1501 arreglos en " + RutaEscena + " (" + cambios + " cambios)", despues, movidos.ToString());
            Debug.Log("T-1501 arreglos: " + cambios + " cambios, quedan " + despues.Count + ", ver " + RutaArreglos);
        }

        /// <summary>Todos los hallazgos de la escena abierta.</summary>
        public static List<Hallazgo> Analizar(Scene escena)
        {
            Physics.SyncTransforms();
            List<Hallazgo> res = new List<Hallazgo>();
            carrosActuales = new HashSet<Transform>();
            foreach (Transform t in escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(false)))
            {
                if (t.name == NombresRuedas[0] && t.parent != null)
                {
                    carrosActuales.Add(t.parent);
                }
            }
            foreach (Transform carro in carrosActuales)
            {
                AnalizarCarro(carro, res);
            }
            AnalizarUnidades(escena, res);
            AnalizarCalles(escena, res);
            return res;
        }

        // ---- carros ----

        private static void AnalizarCarro(Transform carro, List<Hallazgo> res)
        {
            float? minGap = null;
            foreach (string nombreRueda in NombresRuedas)
            {
                Transform rueda = carro.Find(nombreRueda);
                if (rueda == null)
                {
                    continue;
                }
                Renderer[] rs = rueda.GetComponentsInChildren<Renderer>(false);
                if (rs.Length == 0)
                {
                    continue;
                }
                Bounds b = rs[0].bounds;
                foreach (Renderer r in rs)
                {
                    b.Encapsulate(r.bounds);
                }
                float? gap = HuecoBajoPunto(new Vector3(b.center.x, b.min.y, b.center.z), carro);
                if (gap.HasValue)
                {
                    minGap = minGap.HasValue ? Mathf.Min(minGap.Value, gap.Value) : gap.Value;
                }
            }
            if (!minGap.HasValue)
            {
                return;
            }
            if (minGap.Value > Tolerancia && minGap.Value <= MaximoFlotante)
            {
                res.Add(NuevoHallazgo(Tipo.CarroFlotante, carro, minGap.Value, -minGap.Value));
            }
            else if (minGap.Value < -Tolerancia && minGap.Value >= -MaximoHundido)
            {
                // Se sube lo justo para que la llanta más hundida toque el piso.
                res.Add(NuevoHallazgo(Tipo.CarroHundido, carro, -minGap.Value, -minGap.Value));
            }
        }

        // ---- unidades (todo lo que no es carro) ----

        private static void AnalizarUnidades(Scene escena, List<Hallazgo> res)
        {
            Dictionary<Transform, Bounds?> cache = new Dictionary<Transform, Bounds?>();
            HashSet<Transform> unidades = new HashSet<Transform>();
            foreach (MeshRenderer r in escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(false)))
            {
                if (r.sharedMaterials.Length == 0 || BajoUnCarro(r.transform))
                {
                    continue;
                }
                Transform t = r.transform;
                while (t.parent != null)
                {
                    Bounds? bp = Limites(t.parent, cache);
                    if (!bp.HasValue || bp.Value.size.x > ExtensionMaximaUnidad || bp.Value.size.z > ExtensionMaximaUnidad)
                    {
                        break;
                    }
                    t = t.parent;
                }
                unidades.Add(t);
            }

            foreach (Transform u in unidades)
            {
                Rigidbody rb = u.GetComponentInParent<Rigidbody>();
                if (EsIgnorado(u) || (rb != null && !rb.isKinematic))
                {
                    continue;
                }
                Bounds? bn = Limites(u, cache);
                if (!bn.HasValue)
                {
                    continue;
                }
                Bounds b = bn.Value;
                float baseY = b.min.y;
                float ix = b.extents.x * 0.9f;
                float iz = b.extents.z * 0.9f;
                Vector3 c = b.center;
                Vector3[] puntos =
                {
                    new Vector3(c.x, baseY, c.z), new Vector3(c.x + ix, baseY, c.z + iz), new Vector3(c.x - ix, baseY, c.z + iz),
                    new Vector3(c.x + ix, baseY, c.z - iz), new Vector3(c.x - ix, baseY, c.z - iz)
                };
                float? min = null;
                float? max = null;
                foreach (Vector3 p in puntos)
                {
                    float? gap = HuecoBajoPunto(p, u);
                    if (gap.HasValue)
                    {
                        min = min.HasValue ? Mathf.Min(min.Value, gap.Value) : gap.Value;
                        max = max.HasValue ? Mathf.Max(max.Value, gap.Value) : gap.Value;
                    }
                }
                if (!min.HasValue)
                {
                    continue;
                }
                if (min.Value > Tolerancia && min.Value <= MaximoFlotante)
                {
                    res.Add(NuevoHallazgo(Tipo.Flotante, u, min.Value, -min.Value));
                }
                else if (max.Value < -Tolerancia && max.Value >= -MaximoHundido)
                {
                    res.Add(NuevoHallazgo(Tipo.Hundido, u, -max.Value, -max.Value));
                }
            }
        }

        private static bool BajoUnCarro(Transform t)
        {
            foreach (Transform carro in carrosActuales)
            {
                if (t == carro || t.IsChildOf(carro))
                {
                    return true;
                }
            }
            return false;
        }

        private static Bounds? Limites(Transform t, Dictionary<Transform, Bounds?> cache)
        {
            if (cache.TryGetValue(t, out Bounds? ya))
            {
                return ya;
            }
            Bounds? total = null;
            foreach (MeshRenderer r in t.GetComponentsInChildren<MeshRenderer>(false))
            {
                // Un letrero colgado o un carro no cambian la base ni la extensión de la unidad.
                if ((EsIgnorado(r.transform) && r.transform != t) || BajoUnCarro(r.transform))
                {
                    continue;
                }
                if (total.HasValue)
                {
                    Bounds b = total.Value;
                    b.Encapsulate(r.bounds);
                    total = b;
                }
                else
                {
                    total = r.bounds;
                }
            }
            cache[t] = total;
            return total;
        }

        public static bool EsIgnorado(Transform t)
        {
            for (Transform a = t; a != null; a = a.parent)
            {
                string n = a.name.ToLowerInvariant();
                foreach (string frag in Ignorados)
                {
                    if (n.Contains(frag))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Distancia del punto al piso que tiene debajo (positivo: está arriba; negativo: está hundido), ignorando los colliders de
        /// <paramref name="propio"/>. Null si no hay nada. Primero cae desde 0.5 m sobre el punto; si no encuentra nada sale de más arriba (3 m).
        /// </summary>
        private static float? HuecoBajoPunto(Vector3 punto, Transform propio)
        {
            foreach (float subida in new[] { 0.5f, 3f })
            {
                RaycastHit[] hits = Physics.RaycastAll(punto + Vector3.up * subida, Vector3.down, 200f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (RaycastHit h in hits)
                {
                    if (h.collider.transform == propio || h.collider.transform.IsChildOf(propio))
                    {
                        continue;
                    }
                    return punto.y - h.point.y;
                }
            }
            return null;
        }

        // ---- calles ----

        private static void AnalizarCalles(Scene escena, List<Hallazgo> res)
        {
            foreach (MeshRenderer r in escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(false)))
            {
                if (!EsCalle(r))
                {
                    continue;
                }
                Material[] mats = r.sharedMaterials;
                if (mats.Length == 0)
                {
                    res.Add(HallazgoCalle(Tipo.CalleSinMaterial, r, 0f));
                }
                foreach (Material m in mats)
                {
                    if (MaterialMalo(m, out Tipo tipo))
                    {
                        Escala(m, out float menor, out _);
                        res.Add(HallazgoCalle(tipo, r, menor));
                    }
                    else if (FueraDeEscala(m))
                    {
                        Escala(m, out float menor, out float mayor);
                        res.Add(HallazgoCalle(Tipo.CalleTilingExtremo, r, mayor > EscalaMaxima ? mayor : menor));
                    }
                }
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null && mf.sharedMesh.uv.Length == 0)
                {
                    res.Add(HallazgoCalle(Tipo.CalleSinUV, r, 0f));
                }
            }
        }

        public static bool EsCalle(Renderer r)
        {
            if (NombreNoCalle.IsMatch(r.name))
            {
                return false;
            }
            if (NombreCalle.IsMatch(r.name))
            {
                return true;
            }
            Transform padre = r.transform.parent;
            return padre != null && NombreCalle.IsMatch(padre.name) && !NombreNoCalle.IsMatch(padre.name);
        }

        /// <summary>Material nulo, shader nulo/de error, textura base faltante (en un material Barrio_*) o escala de textura 0.</summary>
        private static bool MaterialMalo(Material m, out Tipo tipo)
        {
            tipo = Tipo.CalleSinMaterial;
            if (m == null)
            {
                return true;
            }
            if (m.shader == null || m.shader.name.Contains("InternalError") || !m.shader.isSupported)
            {
                tipo = Tipo.CalleShaderError;
                return true;
            }
            if (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") == null && m.name.StartsWith("Barrio_"))
            {
                tipo = Tipo.CalleSinTextura;
                return true;
            }
            Escala(m, out float menor, out _);
            if (menor <= 0f)
            {
                tipo = Tipo.CalleEscalaCero;
                return true;
            }
            return false;
        }

        private static bool FueraDeEscala(Material m)
        {
            Escala(m, out float menor, out float mayor);
            return menor <= 0f || menor < EscalaMinima || mayor > EscalaMaxima;
        }

        private static void Escala(Material m, out float menor, out float mayor)
        {
            menor = 1f;
            mayor = 1f;
            if (m == null)
            {
                return;
            }
            string prop = m.HasProperty("_BaseMap") ? "_BaseMap" : (m.HasProperty("_MainTex") ? "_MainTex" : null);
            if (prop == null)
            {
                return;
            }
            Vector2 s = m.GetTextureScale(prop);
            menor = Mathf.Min(Mathf.Abs(s.x), Mathf.Abs(s.y));
            mayor = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));
        }

        // ---- ayudas ----

        private static Hallazgo NuevoHallazgo(Tipo tipo, Transform t, float medida, float ajusteY)
        {
            return new Hallazgo { Tipo = tipo, Barrio = Barrio(t), Nombre = t.name, Ruta = Ruta(t), Medida = medida, Objeto = t, AjusteY = ajusteY };
        }

        private static Hallazgo HallazgoCalle(Tipo tipo, Renderer r, float medida)
        {
            return new Hallazgo { Tipo = tipo, Barrio = Barrio(r.transform), Nombre = r.name, Ruta = Ruta(r.transform), Medida = medida, Renderer = r, Objeto = r.transform };
        }

        public static string Ruta(Transform t)
        {
            string s = t.name;
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                s = p.name + "/" + s;
            }
            return s;
        }

        /// <summary>Ancestro más cercano con "Barrio" en el nombre; si no hay, el nombre de la raíz.</summary>
        public static string Barrio(Transform t)
        {
            for (Transform p = t; p != null; p = p.parent)
            {
                if (p.name.IndexOf("Barrio", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return p.name;
                }
            }
            return t.root.name;
        }

        private static void Escribir(string ruta, string titulo, List<Hallazgo> restantes, string movidos)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(titulo);
            sb.AppendLine("Criterios: base a más de " + Tolerancia + " m del piso (hasta " + MaximoFlotante + " m) = flotante; bajo el piso más de " + Tolerancia
                + " m (hasta " + MaximoHundido + " m) = hundido; ignorados por nombre: " + string.Join(", ", Ignorados));
            if (movidos != null)
            {
                sb.AppendLine();
                sb.AppendLine("== Lo que se movió o cambió ==");
                sb.AppendLine(movidos.Length == 0 ? "(nada)" : movidos);
                sb.AppendLine("== Lo que queda después de arreglar ==");
            }
            sb.AppendLine("Total: " + restantes.Count);
            foreach (IGrouping<string, Hallazgo> barrio in restantes.GroupBy(h => h.Barrio).OrderBy(g => g.Key))
            {
                sb.AppendLine();
                sb.AppendLine("== " + barrio.Key + " (" + barrio.Count() + ") ==");
                foreach (Hallazgo h in barrio.OrderBy(x => x.Tipo))
                {
                    sb.AppendLine(h.Tipo + "\t" + h.Nombre + "\t" + h.Medida.ToString("F3") + "\t" + h.Ruta);
                }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllText(ruta, sb.ToString());
        }
    }
}

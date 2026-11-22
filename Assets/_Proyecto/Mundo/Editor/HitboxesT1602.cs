using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1602: las balas pegaban en postes y objetos en vez del personaje porque muchos colliders eran más grandes que su modelo.
    /// Diagnostico() solo informa (Logs/t1602-hitboxes.txt); Aplicar() ajusta. Idempotente.
    /// Postes, faroles, semáforos, bolardos y troncos: una cápsula delgada del radio real del tubo. Lo demás: una caja ajustada a la malla.
    /// Lo delgado o hueco (cables, toldos, lonas, letreros colgantes, rejas, ramas) pasa a la capa NoDetieneBalas: el disparo la ignora
    /// (ReglaImpactoBala.MascaraBala) pero sigue chocando con jugador y carros (la matriz de colisión no cambia).
    /// Las instancias de prefab se arreglan en su prefab. Batchmode: -executeMethod MechaCyberpunk.Mundo.Editor.HitboxesT1602.Diagnostico / .Aplicar
    /// </summary>
    public static class HitboxesT1602
    {
        public const string NombreCapa = "NoDetieneBalas";
        public const string RutaInforme = "Logs/t1602-hitboxes.txt";
        public const float Margen = 1.15f;
        public const float GrosorMinimo = 0.05f;
        public const float RadioMinimoTubo = 0.04f;
        public const float RadioMaximoTubo = 0.35f;
        public const float HuellaMaximaBaseCaja = 1.5f;
        public const long LimiteEscenaBytes = 100L * 1024 * 1024;

        public enum Accion { NoBalas, Capsula, Caja }

        private sealed class Categoria
        {
            public readonly string Nombre;
            public readonly Regex Patron;
            public readonly Accion Accion;

            public Categoria(string nombre, string patron, Accion accion)
            {
                Nombre = nombre;
                Patron = new Regex(patron, RegexOptions.IgnoreCase);
                Accion = accion;
            }
        }

        private sealed class Cuenta
        {
            public int Total, Grandes, EnCapa, Cambiados;
        }

        // El orden importa: gana la primera que coincide. Lo hueco va antes que lo sólido.
        private static readonly Categoria[] Categorias =
        {
            new Categoria("cables", "cable|CY03|tendedero|cuerda|tirante|ropa", Accion.NoBalas),
            new Categoria("toldos y lonas", "toldo|lona|manta|faldon|TL01", Accion.NoBalas),
            new Categoria("letreros colgantes", "bandera|colgante|C15b|NE01", Accion.NoBalas),
            new Categoria("rejas y barandales", "reja|PU03|barrote|malla|barandal|pasamanos|valla", Accion.NoBalas),
            new Categoria("ramas y copas", "rama|copa|follaje", Accion.NoBalas),
            new Categoria("semaforos", "semaforo|ES01", Accion.Capsula),
            new Categoria("faroles", "farol|lampara|linterna", Accion.Capsula),
            new Categoria("postes", "poste|C07", Accion.Capsula),
            new Categoria("bolardos", "bolardo|BL01", Accion.Capsula),
            new Categoria("arboles", "arbol|tronco", Accion.Capsula),
            new Categoria("letreros", "letrero|cartel|rotulo|se.al|C15|NE02", Accion.Caja),
            new Categoria("bancas", "banca|asiento|respaldo|mesa", Accion.Caja),
            new Categoria("botes y basura", "bote|basura|basurero|tambo|BT01|BS01|C11|C16", Accion.Caja),
            new Categoria("puestos", "puesto|C12|C13|CY09|CY06|expendedora|huacal|C17", Accion.Caja),
            new Categoria("barreras", "barrera|C18|tope", Accion.Caja),
            new Categoria("edificios", "edificio|fachada|C01|C02|C03|C04|NV0|casa|pared|muro", Accion.Caja),
        };

        private static readonly Regex ExcluidoPropio = new Regex(
            "^(piso|suelo|banqueta|losa|rampa|escalon|escalera[a-z]*|terreno|asfalto|calle|acera|techo|cebra|limite[a-z]*)[0-9]*$", RegexOptions.IgnoreCase);

        private static readonly Regex ExcluidoCadena = new Regex(
            "jugador|mecha|pandillero|civil|enemigo|policia|npc|personaje|carro|vehiculo|moto|patrulla|taxi|camion|dron", RegexOptions.IgnoreCase);

        [MenuItem("Mecha Cyberpunk/T-1602/Diagnostico hitboxes")]
        public static void Diagnostico() => Ejecutar(false);

        [MenuItem("Mecha Cyberpunk/T-1602/Aplicar hitboxes")]
        public static void Aplicar() => Ejecutar(true);

        private static void Ejecutar(bool aplicar)
        {
            UnityEngine.SceneManagement.Scene escena = ReemplazarEdificiosT1502.Abrir();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("T-1602 hitboxes (" + (aplicar ? "Aplicar" : "Diagnostico") + ") en " + escena.path);
            int capa = aplicar ? AsegurarCapa() : LayerMask.NameToLayer(NombreCapa);
            sb.AppendLine("Capa " + NombreCapa + ": " + capa);
            sb.AppendLine();

            Dictionary<string, Cuenta> antes = Barrer(false, capa, null);
            sb.AppendLine("== Antes ==");
            Tabla(sb, antes);

            if (aplicar)
            {
                HashSet<string> prefabs = new HashSet<string>();
                Barrer(true, capa, prefabs);
                Queue<string> cola = new Queue<string>(prefabs);
                int arreglados = 0;
                while (cola.Count > 0)
                {
                    string ruta = cola.Dequeue();
                    if (ProcesarPrefab(ruta, capa, cola, prefabs))
                    {
                        arreglados++;
                        sb.AppendLine("Prefab ajustado: " + ruta);
                    }
                }
                EditorSceneManager.MarkSceneDirty(escena);
                EditorSceneManager.SaveScene(escena);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                sb.AppendLine("Prefabs revisados " + prefabs.Count + ", ajustados " + arreglados);
                sb.AppendLine();
                sb.AppendLine("== Despues ==");
                Tabla(sb, Barrer(false, capa, null));
            }

            FileInfo info = new FileInfo(escena.path);
            sb.AppendLine();
            sb.AppendLine("Tamano de la escena: " + (info.Exists ? info.Length / (1024 * 1024) : -1) + " MB"
                + (info.Exists && info.Length > LimiteEscenaBytes ? "  !!! PASA DE 100 MB" : ""));
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, sb.ToString());
            Debug.Log("[Mundo] T-1602 " + (aplicar ? "aplicado" : "diagnostico") + ": " + RutaInforme);
        }

        private static void Tabla(StringBuilder sb, Dictionary<string, Cuenta> cuentas)
        {
            sb.AppendLine("categoria\tcolliders\tmas grandes que su malla (>15%)\ten capa " + NombreCapa + "\tcambiados");
            foreach (KeyValuePair<string, Cuenta> par in cuentas.OrderBy(p => p.Key))
            {
                sb.AppendLine(par.Key + "\t" + par.Value.Total + "\t" + par.Value.Grandes + "\t" + par.Value.EnCapa + "\t" + par.Value.Cambiados);
            }
            sb.AppendLine();
        }

        /// <summary>Crea la capa NoDetieneBalas en TagManager si no existe (primer hueco libre desde la 8). Devuelve su número.</summary>
        public static int AsegurarCapa()
        {
            int existente = LayerMask.NameToLayer(NombreCapa);
            if (existente >= 0)
            {
                return existente;
            }
            Object[] activos = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            SerializedObject tm = new SerializedObject(activos[0]);
            SerializedProperty capas = tm.FindProperty("layers");
            for (int i = 8; i < capas.arraySize; i++)
            {
                SerializedProperty el = capas.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(el.stringValue))
                {
                    el.stringValue = NombreCapa;
                    tm.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();
                    return i;
                }
            }
            Debug.LogError("[Mundo] T-1602: no hay capas libres para " + NombreCapa);
            return -1;
        }

        // Recorre los colliders de la escena. Con aplicar, junta en prefabsAfectados los prefabs de las instancias (se arreglan aparte).
        private static Dictionary<string, Cuenta> Barrer(bool aplicar, int capa, HashSet<string> prefabsAfectados)
        {
            Dictionary<string, Cuenta> cuentas = new Dictionary<string, Cuenta>();
            Collider[] todos = Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Collider c in todos)
            {
                if (c == null || !Considerar(c))
                {
                    continue;
                }
                if (aplicar && PrefabUtility.IsPartOfPrefabInstance(c.gameObject))
                {
                    string ruta = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(c.gameObject);
                    if (RutaValida(ruta))
                    {
                        prefabsAfectados?.Add(ruta);
                    }
                    continue;
                }
                Procesar(c, aplicar, capa, cuentas);
            }
            return cuentas;
        }

        private static bool RutaValida(string ruta) => !string.IsNullOrEmpty(ruta) && ruta.StartsWith("Assets/") && ruta.EndsWith(".prefab");

        private static bool ProcesarPrefab(string ruta, int capa, Queue<string> cola, HashSet<string> vistos)
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
            bool cambio = false;
            try
            {
                Dictionary<string, Cuenta> descarte = new Dictionary<string, Cuenta>();
                foreach (Collider c in contenido.GetComponentsInChildren<Collider>(true))
                {
                    if (c == null || !Considerar(c))
                    {
                        continue;
                    }
                    if (PrefabUtility.IsPartOfPrefabInstance(c.gameObject))
                    {
                        string anidado = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(c.gameObject);
                        if (RutaValida(anidado) && vistos.Add(anidado))
                        {
                            cola.Enqueue(anidado);
                        }
                        continue;
                    }
                    cambio |= Procesar(c, true, capa, descarte);
                }
                if (cambio)
                {
                    PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
            return cambio;
        }

        private static bool Considerar(Collider c)
        {
            if (c.isTrigger || c is WheelCollider || c is TerrainCollider || c is CharacterController || c.attachedRigidbody != null)
            {
                return false;
            }
            if (ExcluidoPropio.IsMatch(c.name))
            {
                return false;
            }
            return !ExcluidoCadena.IsMatch(Cadena(c.transform, 64));
        }

        private static string Cadena(Transform t, int niveles)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < niveles && t != null; i++, t = t.parent)
            {
                sb.Append(t.name).Append('/');
            }
            return sb.ToString();
        }

        private static string NombresPropios(Collider c)
        {
            StringBuilder sb = new StringBuilder(c.name);
            foreach (MeshFilter mf in c.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh != null)
                {
                    sb.Append(' ').Append(mf.sharedMesh.name);
                }
            }
            if (c is MeshCollider mc && mc.sharedMesh != null)
            {
                sb.Append(' ').Append(mc.sharedMesh.name);
            }
            return sb.ToString();
        }

        // Primero el nombre del objeto y de su malla; si no coincide, los tres ancestros (el prefab de origen suele nombrar el tipo).
        private static Categoria Clasificar(Collider c)
        {
            string propio = NombresPropios(c);
            foreach (Categoria cat in Categorias)
            {
                if (cat.Patron.IsMatch(propio))
                {
                    return cat;
                }
            }
            string ancestros = Cadena(c.transform.parent, 3);
            foreach (Categoria cat in Categorias)
            {
                if (cat.Patron.IsMatch(ancestros))
                {
                    return cat;
                }
            }
            return null;
        }

        // Devuelve true si cambió algo.
        private static bool Procesar(Collider c, bool aplicar, int capa, Dictionary<string, Cuenta> cuentas)
        {
            Categoria cat = Clasificar(c);
            string nombre = cat != null ? cat.Nombre : "otros";
            Accion accion = cat != null ? cat.Accion : Accion.Caja;
            if (!cuentas.TryGetValue(nombre, out Cuenta cuenta))
            {
                cuenta = new Cuenta();
                cuentas[nombre] = cuenta;
            }
            cuenta.Total++;
            if (capa >= 0 && c.gameObject.layer == capa)
            {
                cuenta.EnCapa++;
            }

            bool hayRender = BoundsRender(c.transform, out Bounds render);
            bool hayCollider = BoundsCollider(c, out Bounds caja);
            bool grande = hayRender && hayCollider && Excede(caja, render);
            if (grande)
            {
                cuenta.Grandes++;
            }

            bool cambio = false;
            if (accion == Accion.NoBalas)
            {
                if (aplicar && capa >= 0 && c.gameObject.layer != capa)
                {
                    c.gameObject.layer = capa;
                    cambio = true;
                }
            }
            else if (hayRender && accion == Accion.Capsula)
            {
                cambio = aplicar && AjustarCapsula(c, render, grande);
            }
            else if (hayRender && accion == Accion.Caja && grande && c is BoxCollider bc)
            {
                if (aplicar)
                {
                    bc.center = render.center;
                    bc.size = Vector3.Max(render.size, Vector3.one * GrosorMinimo);
                    cambio = true;
                }
            }
            if (cambio)
            {
                cuenta.Cambiados++;
            }
            return cambio;
        }

        private static bool Excede(Bounds collider, Bounds render)
        {
            for (int i = 0; i < 3; i++)
            {
                float limite = Mathf.Max(render.size[i], GrosorMinimo) * Margen + 0.02f;
                if (collider.size[i] > limite)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool CapsulaValida(CapsuleCollider cap) => cap.radius <= RadioMaximoTubo + 0.01f;

        // Postes, faroles, semáforos, bolardos, troncos: una cápsula delgada. Devuelve true si cambió.
        private static bool AjustarCapsula(Collider c, Bounds render, bool grande)
        {
            if (c is CapsuleCollider capsula && CapsulaValida(capsula) && !grande)
            {
                return false;
            }
            // Una caja chica de base junto a un tronco de cápsula ya está bien.
            bool tieneCapsula = c.GetComponents<CapsuleCollider>().Any(x => x != c && CapsulaValida(x));
            if (c is BoxCollider baja && !grande && tieneCapsula)
            {
                Vector3 s = baja.size;
                if (Mathf.Max(s.x, s.z) <= HuellaMaximaBaseCaja)
                {
                    return false;
                }
            }
            if (tieneCapsula)
            {
                // Ya hay una cápsula buena en este objeto: este collider grande sobra.
                Object.DestroyImmediate(c);
                return true;
            }

            int dir = EjeLargo(render.size);
            int a = (dir + 1) % 3;
            int b = (dir + 2) % 3;
            float largo = render.size[dir];
            float desde = render.min[dir] + largo * 0.3f;
            float hasta = render.min[dir] + largo * 0.6f;
            List<Vector3> tubo = VerticesLocales(c.transform);
            List<Vector3> rebanada = tubo.Where(v => v[dir] >= desde && v[dir] <= hasta).ToList();
            if (rebanada.Count < 4)
            {
                rebanada = tubo;
            }
            float centroA = render.center[a];
            float centroB = render.center[b];
            float radio = Mathf.Min(render.size[a], render.size[b]) * 0.5f;
            if (rebanada.Count > 0)
            {
                centroA = rebanada.Average(v => v[a]);
                centroB = rebanada.Average(v => v[b]);
                float ca = centroA;
                float cb = centroB;
                radio = rebanada.Max(v => Mathf.Sqrt((v[a] - ca) * (v[a] - ca) + (v[b] - cb) * (v[b] - cb)));
            }
            radio = Mathf.Clamp(radio, RadioMinimoTubo, RadioMaximoTubo);

            PhysicsMaterial material = c.sharedMaterial;
            GameObject objeto = c.gameObject;
            Object.DestroyImmediate(c);
            CapsuleCollider nueva = objeto.AddComponent<CapsuleCollider>();
            nueva.direction = dir;
            nueva.radius = radio;
            nueva.height = Mathf.Max(largo, radio * 2f);
            Vector3 centro = Vector3.zero;
            centro[dir] = render.min[dir] + nueva.height * 0.5f;
            centro[a] = centroA;
            centro[b] = centroB;
            nueva.center = centro;
            nueva.sharedMaterial = material;
            return true;
        }

        private static int EjeLargo(Vector3 s) => s.y >= s.x && s.y >= s.z ? 1 : (s.x >= s.z ? 0 : 2);

        private static IEnumerable<MeshFilter> Mallas(Transform t)
        {
            foreach (MeshFilter mf in t.GetComponentsInChildren<MeshFilter>(true))
            {
                MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                if (mf.sharedMesh != null && mr != null && mr.enabled)
                {
                    yield return mf;
                }
            }
        }

        private static List<Vector3> VerticesLocales(Transform t)
        {
            List<Vector3> lista = new List<Vector3>();
            foreach (MeshFilter mf in Mallas(t))
            {
                Matrix4x4 m = t.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                foreach (Vector3 v in mf.sharedMesh.vertices)
                {
                    lista.Add(m.MultiplyPoint3x4(v));
                }
            }
            return lista;
        }

        // Los bounds de todas las mallas visibles bajo t, en el espacio local de t.
        private static bool BoundsRender(Transform t, out Bounds resultado)
        {
            resultado = default;
            bool primero = true;
            foreach (MeshFilter mf in Mallas(t))
            {
                Matrix4x4 m = t.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                Bounds mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 signo = new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f);
                    Vector3 p = m.MultiplyPoint3x4(mb.center + Vector3.Scale(mb.extents, signo));
                    if (primero)
                    {
                        resultado = new Bounds(p, Vector3.zero);
                        primero = false;
                    }
                    else
                    {
                        resultado.Encapsulate(p);
                    }
                }
            }
            return !primero;
        }

        // Los bounds del collider en su espacio local (el mismo de BoundsRender).
        private static bool BoundsCollider(Collider c, out Bounds resultado)
        {
            resultado = default;
            switch (c)
            {
                case BoxCollider b:
                    resultado = new Bounds(b.center, b.size);
                    return true;
                case SphereCollider s:
                    resultado = new Bounds(s.center, Vector3.one * (s.radius * 2f));
                    return true;
                case CapsuleCollider cap:
                    Vector3 tam = Vector3.one * (cap.radius * 2f);
                    tam[Mathf.Clamp(cap.direction, 0, 2)] = Mathf.Max(cap.height, cap.radius * 2f);
                    resultado = new Bounds(cap.center, tam);
                    return true;
                case MeshCollider mc when mc.sharedMesh != null:
                    resultado = mc.sharedMesh.bounds;
                    return true;
                default:
                    return false;
            }
        }
    }
}

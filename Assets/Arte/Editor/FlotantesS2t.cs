using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-859 (playtest 6-nov tarde): busca piezas del kit que flotan (letreros, cámaras, adornos del tianguis, props) en Ciudad_Prologo. Una pieza
    /// flota si debajo de ella no hay nada a menos de <see cref="HuecoMaximo"/> m y a sus lados (a media altura) tampoco hay pared a menos de
    /// <see cref="ParedMaxima"/> m: ni está en el piso ni colgada de un muro. Usa rayos contra las mallas (colliders temporales de malla en todo lo
    /// que se ve), así no depende de que las piezas tengan collider. Salida: Logs/s2t-flotantes.txt.
    /// <see cref="Arreglar"/> baja al apoyo de abajo las que están cerca del piso (hasta <see cref="BajadaMaxima"/> m) y apaga las demás.
    /// </summary>
    public static class FlotantesS2t
    {
        public const float HuecoMaximo = 0.12f;
        public const float ParedMaxima = 0.35f;
        public const float BajadaMaxima = 3f;
        /// <summary>Raíces que no se revisan: personajes, vehículos, cielo, luces y cables (cuelgan a propósito).</summary>
        private static readonly string[] Saltar = { "JugadorAPie", "AMB_", "GTA_T405", "GTA_T406", "Trafico", "JaulaClandestino", "MechaEstacionado", "T541_Torres", "T513", "S2_PandillasAcoso" };
        private static readonly string[] NombresSaltar = { "Cable", "Nube", "Luz", "Charco", "Linea", "Cebra", "Raya", "Sombra", "Brillo", "Neon", "Cielo", "Fondo", "Halo", "CY07b_Dron" };
        /// <summary>Hasta dónde (m) se busca una pared a los lados para pegarle una pieza que flota.</summary>
        public const float BuscarPared = 4f;

        private sealed class Pieza
        {
            public Transform Raiz;
            public Bounds Limites;
            public float Hueco;
        }

        /// <summary>Las piezas que flotan, con el hueco (m) que tienen abajo (∞ si no hay nada).</summary>
        public static List<(Transform pieza, Bounds limites, float hueco)> Buscar()
        {
            ColocarS2.Abrir();
            List<MeshCollider> temporales = new List<MeshCollider>();
            foreach (MeshFilter mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Renderer r = mf.GetComponent<Renderer>();
                if (mf.sharedMesh != null && r != null && r.enabled && mf.GetComponent<Collider>() == null)
                {
                    MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    temporales.Add(mc);
                }
            }
            Physics.SyncTransforms();
            var salida = new List<(Transform, Bounds, float)>();
            try
            {
                foreach (Transform raiz in Piezas())
                {
                    Renderer[] rs = raiz.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
                    if (rs.Length == 0)
                    {
                        continue;
                    }
                    Bounds b = rs[0].bounds;
                    foreach (Renderer r in rs)
                    {
                        b.Encapsulate(r.bounds);
                    }
                    if (b.min.y < 0.3f || b.size.magnitude > 12f || b.size.magnitude < 0.15f)
                    {
                        continue; // en el piso, o es un edificio, o es una basurita
                    }
                    HashSet<Collider> propios = new HashSet<Collider>(raiz.GetComponentsInChildren<Collider>(true));
                    float hueco = Hueco(b, propios);
                    if (hueco <= HuecoMaximo || Pegada(b, propios))
                    {
                        continue;
                    }
                    salida.Add((raiz, b, hueco));
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

        // Una pieza = la raíz de cada instancia de prefab de Assets/Arte (el kit de Tony), fuera de lo que no se revisa.
        private static IEnumerable<Transform> Piezas()
        {
            return ColocarS2.Todos().Where(t => t.gameObject.activeInHierarchy && PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)
                    && !Saltar.Any(s => t.root.name.StartsWith(s)) && !NombresSaltar.Any(n => t.name.Contains(n)))
                .Where(t =>
                {
                    GameObject fuente = PrefabUtility.GetCorrespondingObjectFromOriginalSource(t.gameObject);
                    return fuente != null && AssetDatabase.GetAssetPath(fuente).StartsWith("Assets/Arte/");
                })
                // El "Visual" de un modelo del kit es a su vez instancia del FBX: cuenta la pieza, no su malla.
                .Where(t => !(t.name == "Visual" && t.parent != null && PrefabUtility.IsAnyPrefabInstanceRoot(t.parent.gameObject)));
        }

        // Lo que hay abajo: rayos desde 5 puntos de la base hacia abajo; el menor hueco.
        private static float Hueco(Bounds b, HashSet<Collider> propios)
        {
            float mejor = float.PositiveInfinity;
            Vector3 e = b.extents * 0.7f;
            foreach (Vector3 d in new[] { Vector3.zero, new Vector3(e.x, 0f, e.z), new Vector3(-e.x, 0f, e.z), new Vector3(e.x, 0f, -e.z), new Vector3(-e.x, 0f, -e.z) })
            {
                Vector3 o = new Vector3(b.center.x + d.x, b.min.y + 0.05f, b.center.z + d.z);
                foreach (RaycastHit h in Physics.RaycastAll(o, Vector3.down, 50f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
                {
                    if (!propios.Contains(h.collider))
                    {
                        mejor = Mathf.Min(mejor, h.distance - 0.05f);
                        break;
                    }
                }
            }
            return mejor;
        }

        // ¿Toca algo de lado? Rayos horizontales desde el centro y desde arriba hacia las 4 direcciones y las diagonales.
        private static bool Pegada(Bounds b, HashSet<Collider> propios)
        {
            foreach (float y in new[] { b.center.y, b.max.y - 0.05f, b.min.y + 0.05f })
            {
                for (int k = 0; k < 8; k++)
                {
                    Vector3 dir = Quaternion.Euler(0f, k * 45f, 0f) * Vector3.forward;
                    float alcance = Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(dir.x), 0f, Mathf.Abs(dir.z)))) + ParedMaxima;
                    Vector3 o = new Vector3(b.center.x, y, b.center.z);
                    if (Physics.RaycastAll(o, dir, alcance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).Any(h => !propios.Contains(h.collider)))
                    {
                        return true;
                    }
                }
            }
            // Colgada de algo de arriba (un toldo, un cable con collider).
            return Physics.RaycastAll(new Vector3(b.center.x, b.max.y - 0.05f, b.center.z), Vector3.up, ParedMaxima, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                .Any(h => !propios.Contains(h.collider));
        }

        [MenuItem("Mecha Cyberpunk/S2t/Flotantes: lista")]
        public static void Lista()
        {
            var l = Buscar();
            StringBuilder sb = new StringBuilder("flotantes: " + l.Count + "\n");
            foreach (var (p, b, h) in l.OrderBy(x => RutaCorta(x.pieza)))
            {
                sb.AppendLine(RutaCorta(p) + " c" + b.center.ToString("F1") + " s" + b.size.ToString("F1") + " hueco " + (float.IsInfinity(h) ? "∞" : h.ToString("F2")));
            }
            File.WriteAllText("Logs/s2t-flotantes.txt", sb.ToString());
        }

        /// <summary>Baja al apoyo las que flotan poco y apaga las que flotan sin nada cerca. Idempotente (las arregladas ya no salen).</summary>
        [MenuItem("Mecha Cyberpunk/S2t/6 Flotantes: arreglar")]
        public static void Arreglar()
        {
            ColocarS2.Informe.Clear();
            var l = Buscar();
            int bajadas = 0, apagadas = 0;
            int pegadas = 0;
            foreach (var (p, b, h) in l)
            {
                if (PegarAPared(p, b))
                {
                    pegadas++;
                    ColocarS2.Informe.AppendLine("pegada a la pared: " + RutaCorta(p));
                }
                else if (!float.IsInfinity(h) && h <= BajadaMaxima)
                {
                    p.position += Vector3.down * h;
                    bajadas++;
                    ColocarS2.Informe.AppendLine("bajada " + h.ToString("F2") + " m: " + RutaCorta(p));
                }
                else
                {
                    p.gameObject.SetActive(false);
                    apagadas++;
                    ColocarS2.Informe.AppendLine("apagada (sin apoyo): " + RutaCorta(p) + " en " + b.center.ToString("F1"));
                }
            }
            ColocarS2.Informe.AppendLine("T-859: " + pegadas + " pegadas a la pared, " + bajadas + " bajadas al apoyo, " + apagadas + " apagadas");
            ColocarS2.Guardar("s2t-flotantes");
        }

        // La pared más cercana a los lados (a media altura): la pieza se recorre hasta tocarla. False si no hay pared cerca.
        private static bool PegarAPared(Transform p, Bounds b)
        {
            HashSet<Collider> propios = new HashSet<Collider>(p.GetComponentsInChildren<Collider>(true));
            float mejor = float.PositiveInfinity;
            Vector3 dirMejor = Vector3.zero;
            for (int k = 0; k < 8; k++)
            {
                Vector3 dir = Quaternion.Euler(0f, k * 45f, 0f) * Vector3.forward;
                foreach (RaycastHit hit in Physics.RaycastAll(b.center, dir, BuscarPared + b.extents.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).OrderBy(x => x.distance))
                {
                    if (propios.Contains(hit.collider))
                    {
                        continue;
                    }
                    float medio = Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(dir.x), 0f, Mathf.Abs(dir.z))));
                    float hueco = hit.distance - medio;
                    if (hueco < mejor)
                    {
                        mejor = hueco;
                        dirMejor = dir;
                    }
                    break;
                }
            }
            if (float.IsInfinity(mejor) || mejor > BuscarPared)
            {
                return false;
            }
            p.position += dirMejor * Mathf.Max(0f, mejor - 0.02f);
            return true;
        }

        public static void Fotos()
        {
            ColocarS2.Abrir();
            ColocarS2t.FotoHora(13f, new Vector3(-31f, 2f, 1f), new Vector3(-35f, 4f, 9.5f), ColocarS2t.CarpetaCapturas + "s2t-flotantes-dron.png", 60f);
            ColocarS2t.FotoHora(22f, new Vector3(62f, 2f, 2f), new Vector3(59f, 4.5f, 10f), ColocarS2t.CarpetaCapturas + "s2t-flotantes-camara.png", 60f);
            ColocarS2t.FotoHora(22f, new Vector3(-68f, 1.7f, -58f), new Vector3(-73f, 2f, -54f), ColocarS2t.CarpetaCapturas + "s2t-flotantes-callejon.png", 60f);
        }

        private static string RutaCorta(Transform t)
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

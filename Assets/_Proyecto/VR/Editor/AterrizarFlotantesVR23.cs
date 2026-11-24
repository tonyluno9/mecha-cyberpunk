using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.VR.Editor
{
    /// <summary>
    /// VR23: baja al piso (o al techo) los props decorativos que quedaron flotando en la ciudad (tinacos, huacales, tambos, macetas, cajas...).
    /// Conservador: solo objetos cuyo nombre es de un prop conocido, chicos (&lt; <see cref="TamanoMaximo"/> m), sin scripts, Rigidbody, Animator ni luces,
    /// fuera de Trafico, vehículos y personajes. Mide el fondo de sus bounds, lanza 5 rayos hacia abajo (ignorando sus propios colliders) y si el hueco
    /// hasta la superficie más alta está entre <see cref="HuecoMinimo"/> y <see cref="HuecoMaximo"/> m lo baja; si entre el prop y esa superficie hay otra
    /// malla visible (un techo sin collider, una base), no lo toca. Escribe la lista en docs/capturas/_tareas/VR23/movidos.txt.
    /// Batch: <c>-executeMethod MechaCyberpunk.VR.Editor.AterrizarFlotantesVR23.Aplicar</c> (con Unity cerrado).
    /// </summary>
    public static class AterrizarFlotantesVR23
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string RutaLista = "docs/capturas/_tareas/VR23/movidos.txt";
        private const float HuecoMinimo = 0.05f;
        private const float HuecoMaximo = 3f;
        private const float TamanoMaximo = 4f;
        private const float InicioRayo = 0.1f;
        private const float FraccionEsquinas = 0.5f;
        private const int RayosMinimos = 3;
        private const float LimiteMegasEscena = 95f;

        // Nombres de props que deben ir apoyados (sin distinguir mayúsculas). Nada que vaya colgado: ni letreros, neón, cables, focos, aires ni transformadores.
        private static readonly string[] Props =
        {
            "tinaco", "huacal", "tambo", "bolardo", "maceta", "caja", "banca", "mesa", "silla", "basura", "barril", "cubeta", "garrafon", "barrera", "cono", "llanta", "costal", "hielera",
        };

        // Ramas que no se tocan (tráfico es de Alan; vehículos, personajes, NPC, luces, interfaz).
        private static readonly string[] Excluidos =
        {
            "trafico", "vehicul", "carro", "moto", "peaton", "pandill", "policia", "jugador", "npc", "dron", "vendedor", "personaje", "letrero", "neon", "cable", "colgad", "luz",
            "foco", "lampara", "aire", "transformador", "antena", "canvas", "pantalla",
        };

        private static readonly RaycastHit[] golpes = new RaycastHit[32];

        private struct Candidato
        {
            public Transform Objeto;
            public Bounds Caja;
        }

        [MenuItem("Mecha Cyberpunk/VR/VR23 Aterrizar props flotantes (ciudad)")]
        public static void Aplicar()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            Physics.SyncTransforms();
            var candidatos = new List<Candidato>();
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                Buscar(raiz.transform, candidatos);
            }
            // Primero los más bajos: si una base baja, lo que va encima la encuentra ya en su lugar.
            candidatos.Sort((a, b) => a.Caja.min.y.CompareTo(b.Caja.min.y));
            MeshRenderer[] todas = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            // Colliders temporales en las mallas sin collider que quedan bajo un candidato (techos y cuerpos de casa sin collider):
            // así el rayo mide contra el techo real. Se quitan antes de guardar.
            var temporales = new List<MeshCollider>();
            foreach (MeshRenderer r in todas)
            {
                if (r == null || r.GetComponent<Collider>() != null)
                {
                    continue;
                }
                MeshFilter f = r.GetComponent<MeshFilter>();
                if (f == null || f.sharedMesh == null)
                {
                    continue;
                }
                Bounds b = r.bounds;
                bool bajoAlguno = false;
                foreach (Candidato c in candidatos)
                {
                    if (c.Objeto == null || r.transform.IsChildOf(c.Objeto))
                    {
                        continue;
                    }
                    if (b.min.x <= c.Caja.max.x && b.max.x >= c.Caja.min.x && b.min.z <= c.Caja.max.z && b.max.z >= c.Caja.min.z
                        && b.max.y <= c.Caja.min.y + 0.3f && b.max.y >= c.Caja.min.y - HuecoMaximo - 0.5f)
                    {
                        bajoAlguno = true;
                        break;
                    }
                }
                if (bajoAlguno)
                {
                    MeshCollider mc = r.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = f.sharedMesh;
                    temporales.Add(mc);
                }
            }
            Physics.SyncTransforms();
            Debug.Log("[VR23] colliders temporales: " + temporales.Count);

            var lista = new StringBuilder();
            lista.AppendLine("VR23 · props bajados en " + RutaEscena + " (" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + ")");
            lista.AppendLine("ruta del objeto | metros bajados | superficie");
            int movidos = 0;
            int omitidos = 0;
            foreach (Candidato c in candidatos)
            {
                if (c.Objeto == null)
                {
                    continue;
                }
                Bounds caja = Medir(c.Objeto);
                if (!Hueco(c.Objeto, caja, out float hueco, out Collider apoyo))
                {
                    continue;
                }
                if (hueco <= HuecoMinimo || hueco > HuecoMaximo)
                {
                    continue;
                }
                string superficie = apoyo != null ? apoyo.name : "?";
                if (HayMallaDebajo(c.Objeto, caja, caja.min.y - hueco, apoyo, todas))
                {
                    omitidos++;
                    lista.AppendLine("OMITIDO (malla sin collider debajo) " + RutaDe(c.Objeto) + " | " + hueco.ToString("0.00") + " | " + superficie);
                    continue;
                }
                Undo.RecordObject(c.Objeto, "VR23 aterrizar");
                c.Objeto.position += Vector3.down * hueco;
                PrefabUtility.RecordPrefabInstancePropertyModifications(c.Objeto);
                Physics.SyncTransforms();
                movidos++;
                lista.AppendLine(RutaDe(c.Objeto) + " | " + hueco.ToString("0.00") + " | " + superficie);
            }
            foreach (MeshCollider mc in temporales)
            {
                if (mc != null)
                {
                    UnityEngine.Object.DestroyImmediate(mc);
                }
            }
            lista.AppendLine("Candidatos: " + candidatos.Count + " · movidos: " + movidos + " · omitidos: " + omitidos);
            Debug.Log("[VR23] " + lista);

            if (movidos > 0)
            {
                EditorSceneManager.MarkSceneDirty(escena);
                EditorSceneManager.SaveScene(escena);
            }
            string raizProyecto = Path.GetDirectoryName(Application.dataPath);
            string rutaLista = Path.Combine(raizProyecto, RutaLista);
            Directory.CreateDirectory(Path.GetDirectoryName(rutaLista));
            File.WriteAllText(rutaLista, lista.ToString(), new UTF8Encoding(false));
            float megas = new FileInfo(Path.Combine(raizProyecto, RutaEscena)).Length / (1024f * 1024f);
            if (megas > LimiteMegasEscena)
            {
                Debug.LogError("[VR23] La escena pesa " + megas.ToString("0.0") + " MB: GitHub rechaza más de 100 MB.");
            }
            else
            {
                Debug.Log("[VR23] Escena guardada: " + megas.ToString("0.0") + " MB. Lista en " + RutaLista);
            }
        }

        // Recorre la jerarquía; el primer objeto con nombre de prop y tamaño chico es el candidato (se mueve entero, con sus hijos).
        private static void Buscar(Transform t, List<Candidato> candidatos)
        {
            if (!t.gameObject.activeInHierarchy || Contiene(t.name, Excluidos) || TieneComportamiento(t))
            {
                return;
            }
            if (Contiene(t.name, Props))
            {
                Bounds caja = Medir(t);
                if (caja.size != Vector3.zero && Mathf.Max(caja.size.x, caja.size.y, caja.size.z) <= TamanoMaximo && SoloMallas(t))
                {
                    candidatos.Add(new Candidato { Objeto = t, Caja = caja });
                    return;
                }
            }
            for (int i = 0; i < t.childCount; i++)
            {
                Buscar(t.GetChild(i), candidatos);
            }
        }

        // Rigidbody, CharacterController o Animator: algo que se mueve solo; no se toca (ni sus hijos). Los scripts del prop mismo los descarta SoloMallas.
        private static bool TieneComportamiento(Transform t)
        {
            return t.GetComponent<Rigidbody>() != null || t.GetComponent<CharacterController>() != null || t.GetComponent<Animator>() != null;
        }

        // Todo el prop es solo mallas y colliders (sin scripts ni nada vivo en sus hijos).
        private static bool SoloMallas(Transform t)
        {
            foreach (Component c in t.GetComponentsInChildren<Component>(true))
            {
                if (c == null || c is Transform || c is MeshFilter || c is MeshRenderer || c is Collider || c is LODGroup)
                {
                    continue;
                }
                return false;
            }
            return t.GetComponentInChildren<MeshRenderer>() != null;
        }

        private static Bounds Medir(Transform t)
        {
            Bounds caja = default;
            bool hay = false;
            foreach (MeshRenderer r in t.GetComponentsInChildren<MeshRenderer>())
            {
                if (!r.enabled)
                {
                    continue;
                }
                if (!hay)
                {
                    caja = r.bounds;
                    hay = true;
                }
                else
                {
                    caja.Encapsulate(r.bounds);
                }
            }
            return hay ? caja : new Bounds(t.position, Vector3.zero);
        }

        // 5 rayos (centro y esquinas hacia dentro) desde el fondo: el hueco hasta la superficie más alta que tocan. Necesita el centro y 3 rayos.
        private static bool Hueco(Transform propio, Bounds caja, out float hueco, out Collider superficie)
        {
            hueco = 0f;
            superficie = null;
            Vector3 e = caja.extents * FraccionEsquinas;
            Vector3 c = caja.center;
            Vector3[] origenes =
            {
                new Vector3(c.x, caja.min.y + InicioRayo, c.z),
                new Vector3(c.x - e.x, caja.min.y + InicioRayo, c.z - e.z),
                new Vector3(c.x + e.x, caja.min.y + InicioRayo, c.z - e.z),
                new Vector3(c.x - e.x, caja.min.y + InicioRayo, c.z + e.z),
                new Vector3(c.x + e.x, caja.min.y + InicioRayo, c.z + e.z),
            };
            float alto = float.NegativeInfinity;
            int tocaron = 0;
            for (int i = 0; i < origenes.Length; i++)
            {
                if (!PrimerGolpe(propio, origenes[i], out RaycastHit golpe))
                {
                    if (i == 0)
                    {
                        return false;
                    }
                    continue;
                }
                tocaron++;
                if (golpe.point.y > alto)
                {
                    alto = golpe.point.y;
                    superficie = golpe.collider;
                }
            }
            if (tocaron < RayosMinimos)
            {
                return false;
            }
            hueco = caja.min.y - alto;
            return true;
        }

        private static bool PrimerGolpe(Transform propio, Vector3 origen, out RaycastHit mejor)
        {
            mejor = default;
            int n = Physics.RaycastNonAlloc(origen, Vector3.down, golpes, HuecoMaximo + InicioRayo + 0.5f, ~0, QueryTriggerInteraction.Ignore);
            float menor = float.MaxValue;
            bool hay = false;
            for (int i = 0; i < n; i++)
            {
                RaycastHit g = golpes[i];
                if (g.collider == null || g.collider.transform.IsChildOf(propio) || g.collider.attachedRigidbody != null)
                {
                    continue;
                }
                if (g.distance < menor)
                {
                    menor = g.distance;
                    mejor = g;
                    hay = true;
                }
            }
            return hay;
        }

        // Otra malla visible entre el fondo del prop y la superficie: (a) su tope queda en el hueco (una base o un techo sin collider) o
        // (b) sube desde la superficie hasta más arriba del fondo del prop (el prop se hundiría en ella: una casa con el techo sin collider),
        // salvo que sea la misma pieza del collider donde se apoya. En los dos casos no se toca.
        private static bool HayMallaDebajo(Transform propio, Bounds caja, float superficie, Collider apoyo, MeshRenderer[] todas)
        {
            Transform piezaApoyo = apoyo != null ? (apoyo.transform.parent != null ? apoyo.transform.parent : apoyo.transform) : null;
            Vector3 e = caja.extents * FraccionEsquinas;
            var columna = new Bounds();
            columna.SetMinMax(new Vector3(caja.center.x - e.x, superficie + 0.03f, caja.center.z - e.z), new Vector3(caja.center.x + e.x, caja.min.y - 0.02f, caja.center.z + e.z));
            foreach (MeshRenderer r in todas)
            {
                if (r == null || !r.enabled || r.transform.IsChildOf(propio))
                {
                    continue;
                }
                Bounds b = r.bounds;
                if (!b.Intersects(columna))
                {
                    continue;
                }
                bool topeEnElHueco = b.max.y > superficie + 0.03f && b.max.y < caja.min.y - 0.02f;
                bool atraviesa = b.min.y <= superficie + 0.3f && b.max.y >= caja.min.y - 0.02f;
                bool esElApoyo = piezaApoyo != null && r.transform.IsChildOf(piezaApoyo);
                if (topeEnElHueco || (atraviesa && !esElApoyo))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool Contiene(string nombre, string[] partes)
        {
            string n = nombre.ToLowerInvariant();
            foreach (string p in partes)
            {
                if (n.Contains(p))
                {
                    return true;
                }
            }
            return false;
        }

        private static string RutaDe(Transform t)
        {
            var sb = new StringBuilder(t.name);
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                sb.Insert(0, p.name + "/");
            }
            sb.Append(" @ ").Append(t.position.ToString("F2"));
            return sb.ToString();
        }
    }
}

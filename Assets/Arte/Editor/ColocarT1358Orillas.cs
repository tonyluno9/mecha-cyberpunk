using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Mundo.Ambiente;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1358: abre las orillas de la ciudad hacia las zonas nuevas y pone los límites del mundo nuevos (plan-zonas-18nov.md).
    /// Idempotente: borra y rehace solo la raíz T1358_Orillas. No borra nada de T530_Afueras: solo apaga (SetActive false) los edificios de
    /// fondo, los LimiteInvisible y lo que esté encima de las uniones U1, U2, U4, U9 y U7. Lo apagado queda en Logs/t1358-orillas.txt.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT1358Orillas.Aplicar
    /// </summary>
    public static class ColocarT1358Orillas
    {
        public const string NombreRaiz = "T1358_Orillas";
        public const string NombreLimite = "LimiteInvisible";
        public const float AltoLimite = 30f;
        public const float AnchoUnion = 14f;

        /// <summary>Rectángulos (x mín, z mín, x máx, z máx) de las uniones con las zonas nuevas, en planta.</summary>
        public static readonly (string nombre, Vector4 rect)[] Uniones =
        {
            ("U1", new Vector4(-54f, -240f, -40f, -187f)),
            ("U2", new Vector4(40f, -240f, 54f, -187f)),
            ("U4", new Vector4(-320f, -7f, -249f, 7f)),
            ("U9", new Vector4(249f, -7f, 310f, 7f)),
            ("U7", new Vector4(-125f, 187f, -100f, 240f)),
        };

        /// <summary>Límites del mundo nuevos: nombre, centro y tamaño.</summary>
        public static readonly (string nombre, Vector3 centro, Vector3 tam)[] Limites =
        {
            ("Norte", new Vector3(-67.5f, 15f, 290f), new Vector3(825f, AltoLimite, 1f)),
            ("Oeste", new Vector3(-480f, 15f, -20f), new Vector3(1f, AltoLimite, 620f)),
            ("Este", new Vector3(345f, 15f, 122.5f), new Vector3(1f, AltoLimite, 335f)),
            ("MarSur", new Vector3(-71.75f, 15f, -268f), new Vector3(817.5f, AltoLimite, 1f)),
            ("MarEste", new Vector3(336f, 15f, -156.5f), new Vector3(1f, AltoLimite, 223f)),
        };

        /// <summary>Puntos de orilla para SalidaDelAgua: la calle del Malecón (z = -215), a 0.3 m del piso.</summary>
        private static readonly Vector3[] PuntosOrilla =
        {
            new Vector3(-300f, 0.3f, -215f), new Vector3(-260f, 0.3f, -215f), new Vector3(-220f, 0.3f, -215f), new Vector3(-180f, 0.3f, -215f),
            new Vector3(-140f, 0.3f, -215f), new Vector3(-100f, 0.3f, -215f), new Vector3(-47f, 0.3f, -226f), new Vector3(0f, 0.3f, -215f),
            new Vector3(47f, 0.3f, -226f), new Vector3(100f, 0.3f, -215f), new Vector3(160f, 0.3f, -215f),
        };

        private static readonly List<string> Apagados = new List<string>();

        public static void Aplicar()
        {
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            ColocarS2.Informe.Clear();
            Apagados.Clear();

            ApagarAfueras();
            CrearRaiz();

            ColocarS2.Informe.AppendLine("APAGADOS (SetActive false) en " + ColocarT530.NombreRaiz + ": " + Apagados.Count);
            foreach (string a in Apagados)
            {
                ColocarS2.Informe.AppendLine("  " + a);
            }
            ColocarS2.Guardar("t1358-orillas");
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

        private static bool EnUnion(Bounds b)
        {
            foreach ((string _, Vector4 r) in Uniones)
            {
                if (b.max.x > r.x && b.min.x < r.z && b.max.z > r.y && b.min.z < r.w)
                {
                    return true;
                }
            }
            return false;
        }

        private static void Apagar(Transform t, string motivo)
        {
            t.gameObject.SetActive(false);
            Apagados.Add(motivo + ": " + Ruta(t));
        }

        private static void ApagarAfueras()
        {
            GameObject raizAfueras = GameObject.Find(ColocarT530.NombreRaiz);
            if (raizAfueras == null)
            {
                ColocarS2.Informe.AppendLine("ADVERTENCIA: no existe " + ColocarT530.NombreRaiz);
                return;
            }
            Transform[] todos = raizAfueras.GetComponentsInChildren<Transform>(false);
            List<Bounds> caidos = new List<Bounds>();
            foreach (Transform t in todos)
            {
                if (t == raizAfueras.transform)
                {
                    continue;
                }
                Collider c = t.GetComponent<Collider>();
                if (c == null || !c.enabled)
                {
                    continue;
                }
                bool limite = t.name == NombreLimite;
                bool edificio = t.name == "Edificio";
                bool fondo = edificio && Mathf.Max(c.bounds.size.x, c.bounds.size.z) >= 100f && Mathf.Min(c.bounds.size.x, c.bounds.size.z) <= 31f;
                if (limite)
                {
                    Apagar(t, "limite del final");
                }
                else if (fondo)
                {
                    caidos.Add(c.bounds);
                    Apagar(t, "edificio de fondo");
                }
                else if (edificio && EnUnion(c.bounds))
                {
                    caidos.Add(c.bounds);
                    Apagar(t, "edificio sobre union");
                }
            }
            // Las ventanas y el kit de fachada de cada edificio apagado se apagan con él (si no, quedan flotando).
            foreach (Transform t in raizAfueras.GetComponentsInChildren<Transform>(false).Where(x => x.name == "Ventanas").ToArray())
            {
                Renderer r = t.GetComponent<Renderer>();
                if (r != null && caidos.Any(b => Contiene(b, r.bounds.center)))
                {
                    Apagar(t, "ventanas del edificio apagado");
                }
            }
            GameObject calles = GameObject.Find("S2_Calles");
            if (calles != null)
            {
                foreach (LODGroup lod in calles.GetComponentsInChildren<LODGroup>(false).ToArray())
                {
                    Renderer[] rs = lod.GetComponentsInChildren<Renderer>();
                    if (rs.Length == 0)
                    {
                        continue;
                    }
                    Bounds kit = rs[0].bounds;
                    foreach (Renderer r in rs)
                    {
                        kit.Encapsulate(r.bounds);
                    }
                    if (caidos.Any(b => Contiene(b, kit.center)))
                    {
                        Apagar(lod.transform, "kit de fachada del edificio apagado");
                    }
                }
            }
            Physics.SyncTransforms();
            foreach ((string nombre, Vector4 r) in Uniones)
            {
                Vector3 centro = new Vector3((r.x + r.z) * 0.5f, 2f, (r.y + r.w) * 0.5f);
                Vector3 mitad = new Vector3((r.z - r.x) * 0.5f, 1f, (r.w - r.y) * 0.5f);
                Collider[] resto = Physics.OverlapBox(centro, mitad, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                foreach (Collider c in resto)
                {
                    ColocarS2.Informe.AppendLine("PENDIENTE en " + nombre + " (collider activo que no es de T530): " + Ruta(c.transform));
                }
            }
        }

        private static bool Contiene(Bounds b, Vector3 p)
        {
            return p.x > b.min.x - 2f && p.x < b.max.x + 2f && p.z > b.min.z - 2f && p.z < b.max.z + 2f;
        }

        private static void CrearRaiz()
        {
            GameObject vieja = GameObject.Find(NombreRaiz);
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja);
            }
            Transform raiz = new GameObject(NombreRaiz).transform;
            foreach ((string nombre, Vector3 centro, Vector3 tam) in Limites)
            {
                GameObject l = new GameObject(NombreLimite);
                l.name = NombreLimite;
                l.transform.SetParent(raiz, false);
                l.transform.position = centro;
                l.AddComponent<BoxCollider>().size = tam;
                l.isStatic = true;
                ColocarS2.Informe.AppendLine("Limite " + nombre + " en " + centro + " tam " + tam);
            }
            GameObject agua = new GameObject("SalidaDelAgua");
            agua.transform.SetParent(raiz, false);
            agua.AddComponent<SalidaDelAgua>().Definir(new Vector3(-71.75f, -4f, -300f), new Vector3(817.5f, 4f, 70f), PuntosOrilla);

            // Aviso si algo de las zonas ya colocadas toca los muros del mar.
            foreach (string prefijo in new[] { "T1354", "T1355", "T1356", "T1357" })
            {
                foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects().Where(x => x.name.StartsWith(prefijo)))
                {
                    foreach (Collider c in g.GetComponentsInChildren<Collider>())
                    {
                        foreach (Collider muro in raiz.GetComponentsInChildren<BoxCollider>())
                        {
                            if (c.bounds.Intersects(muro.bounds))
                            {
                                ColocarS2.Informe.AppendLine("ADVERTENCIA: " + Ruta(c.transform) + " toca un limite nuevo");
                            }
                        }
                    }
                }
            }
        }
    }
}

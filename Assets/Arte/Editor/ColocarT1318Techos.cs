using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1318: azoteas con material propio, muros bajos saltables y sin paredes invisibles al borde de los techos.
    /// 1) Losa de 2 cm sin collider con <see cref="KitS2c.MatAzotea"/> sobre la cara de arriba de cada azotea caminable (casas del kit y
    ///    edificios bajos del barrio); nunca en muros ni fachadas.
    /// 2) Los pretiles C05 chocan solo hasta <see cref="ArteT250.AltoColliderPretil"/> (menos que alturaSalto 0.45 m del jugador); la malla visible no cambia.
    /// 3) Quita colliders invisibles de muro o borde sobre los techos y recorta cualquier collider de azotea que suba más que su malla visible.
    /// Idempotente: se puede correr varias veces. No toca el jugador ni Core.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT1318Techos.Aplicar
    /// </summary>
    public static class ColocarT1318Techos
    {
        public const string NombreLosa = "AzoteaT1318";
        public const string NombrePretil = "C05_PretilAzotea";
        public const string RutaPrefabPretil = "Assets/Arte/Calle/Prefabs/C05_PretilAzotea.prefab";
        public const string RutaMallas = KitS2c.CarpetaMallas + "/T1318_Azoteas.asset";
        public const float EspesorLosa = 0.02f;

        /// <summary>Cuerpos con azotea caminable: cajas "Cuerpo" de casas del kit y edificios bajos del barrio (mismo criterio que ColocarS2Techos).</summary>
        public static bool EsAzotea(BoxCollider c)
        {
            if (c == null || !c.enabled || c.isTrigger || !c.gameObject.activeInHierarchy || c.transform.parent == null || c.GetComponent<Renderer>() == null)
            {
                return false;
            }
            string padre = c.transform.parent.name;
            Bounds b = Limites(c);
            if (c.name == "Cuerpo" && (padre.StartsWith("Casa_") || (c.transform.root.name == "T1102_CalleDoc" && (padre.StartsWith("Fachada_") || padre.StartsWith("Remate_")))))
            {
                return b.max.y >= 3f && b.max.y <= 11.01f;
            }
            if (c.name.StartsWith("Cuerpo") && padre.StartsWith("Edificio_") && c.transform.root.name == "Barrio")
            {
                if (!(b.max.y <= 11.01f && b.max.y >= 3f && b.size.x >= 3f && b.size.z >= 3f))
                {
                    return false;
                }
                // Con lámina o segundo piso encima la cara de arriba no es la azotea que se camina.
                foreach (Transform h in c.transform.parent)
                {
                    if ((h.name.StartsWith("Lamina") || h.name.StartsWith("Piso2")) && h.GetComponentInChildren<Renderer>() != null)
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>Caja del collider en el mundo calculada con el transform (Collider.bounds sale en cero en una escena abierta sin física).</summary>
        public static Bounds Limites(Collider c)
        {
            if (c is BoxCollider bc)
            {
                Bounds b = new Bounds(c.transform.TransformPoint(bc.center), Vector3.zero);
                for (int i = 0; i < 8; i++)
                {
                    Vector3 esquina = bc.center + Vector3.Scale(bc.size * 0.5f, new Vector3(i % 2 == 0 ? -1f : 1f, (i / 2) % 2 == 0 ? -1f : 1f, i / 4 == 0 ? -1f : 1f));
                    b.Encapsulate(c.transform.TransformPoint(esquina));
                }
                return b;
            }
            return c.bounds;
        }

        /// <summary>Capturas de T-1318 en docs/capturas/t-1318. No guarda la escena.</summary>
        public static void Capturas()
        {
            ColocarS2.Abrir();
            Directory.CreateDirectory("docs/capturas/t-1318");
            Transform casa = ColocarS2.Buscar("T1102_CalleDoc/Casas/Fachada_P_01_3p_c1");
            Transform losa = casa.Cast<Transform>().First(h => h.name == NombreLosa);
            Vector3 c = losa.position;
            Transform pretil = casa.Cast<Transform>().First(h => h.name == NombrePretil);
            ColocarS2cLook.Foto(c + new Vector3(9f, 4.5f, 7f), c, "docs/capturas/t-1318/azotea-desde-otro-techo.png", 55f, 16f);
            Vector3 dentro = new Vector3(c.x - pretil.position.x, 0f, c.z - pretil.position.z).normalized;
            Vector3 lado = Vector3.Cross(Vector3.up, dentro);
            ColocarS2cLook.Foto(pretil.position + dentro * 1.6f + lado * 1.2f + Vector3.up * 1.1f, pretil.position + Vector3.up * 0.25f, "docs/capturas/t-1318/pretil-de-cerca.png", 55f, 16f);
            ColocarS2cLook.Foto(c + new Vector3(9f, 4.5f, 7f), c, "docs/capturas/t-1318/azotea-noche.png", 55f);
            ColocarS2cLook.Foto(new Vector3(c.x + 10f, 48f, c.z - 38f), new Vector3(c.x, 5f, c.z - 8f), "docs/capturas/t-1318/techos-desde-arriba.png", 55f, 16f);
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }

        public static void Aplicar()
        {
            ColocarS2.Informe.Clear();
            AjustarPrefabPretil();
            ColocarS2.Abrir();
            Physics.SyncTransforms();
            int losas = PonerLosas();
            int pretiles = BajarPretiles();
            int recortes = RecortarColliders();
            int invisibles = QuitarInvisibles();
            Debug.Log("[T1318] losas " + losas + ", pretiles bajados " + pretiles + ", colliders recortados " + recortes + ", invisibles quitados " + invisibles);
            ColocarS2.Guardar("t1318-techos");
        }

        /// <summary>Alto en el mundo de todas las mallas de un objeto, desde sus vértices (Renderer.bounds sale en cero en prefabs cargados).</summary>
        private static float AltoDeMalla(GameObject raiz)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            foreach (MeshFilter f in raiz.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null))
            {
                Bounds b = f.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 esquina = b.center + Vector3.Scale(b.extents, new Vector3(i % 2 == 0 ? -1f : 1f, (i / 2) % 2 == 0 ? -1f : 1f, i / 4 == 0 ? -1f : 1f));
                    float y = f.transform.TransformPoint(esquina).y;
                    min = Mathf.Min(min, y);
                    max = Mathf.Max(max, y);
                }
            }
            return max > min ? max - min : 0f;
        }

        /// <summary>El prefab C05 queda con el collider bajo; la malla no se toca.</summary>
        private static void AjustarPrefabPretil()
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefabPretil);
            try
            {
                Transform visual = raiz.transform.Find("Visual");
                Transform escala = raiz.transform.Find("EscalaAlto");
                if (escala == null && visual != null)
                {
                    // Un padre sin giro: escalarlo en Y baja la malla en vertical aunque el FBX traiga giros dentro.
                    escala = new GameObject("EscalaAlto").transform;
                    escala.SetParent(raiz.transform, false);
                    visual.SetParent(escala, true);
                }
                if (escala != null)
                {
                    float alto = AltoDeMalla(raiz);
                    if (alto > ArteT250.AltoMallaPretil + 0.01f)
                    {
                        // La base queda en el piso (el pivote del prefab está en la base).
                        escala.localScale = new Vector3(escala.localScale.x, escala.localScale.y * ArteT250.AltoMallaPretil / alto, escala.localScale.z);
                    }
                }
                BoxCollider c = raiz.GetComponent<BoxCollider>();
                if (c != null)
                {
                    c.size = new Vector3(c.size.x, ArteT250.AltoColliderPretil, c.size.z);
                    c.center = new Vector3(c.center.x, ArteT250.AltoColliderPretil * 0.5f, c.center.z);
                    PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabPretil);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        private static int PonerLosas()
        {
            foreach (GameObject g in ColocarS2.Todos().Where(t => t.name == NombreLosa).Select(t => t.gameObject).ToList())
            {
                Object.DestroyImmediate(g);
            }
            if (File.Exists(RutaMallas))
            {
                AssetDatabase.DeleteAsset(RutaMallas);
            }
            Directory.CreateDirectory(KitS2c.CarpetaMallas);
            Material mat = KitS2c.MatAzotea();
            List<Mesh> mallas = new List<Mesh>();
            List<BoxCollider> cuerpos = Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(EsAzotea).ToList();
            cuerpos.Sort((a, b) => string.CompareOrdinal(ColocarS2.Ruta(a.transform), ColocarS2.Ruta(b.transform)));
            foreach (BoxCollider c in cuerpos)
            {
                Transform t = c.transform;
                Vector3 s = t.lossyScale;
                Vector3 tam = new Vector3(Mathf.Abs(c.size.x * s.x), Mathf.Abs(c.size.y * s.y), Mathf.Abs(c.size.z * s.z));
                Vector3 arriba = t.TransformPoint(c.center + Vector3.up * (c.size.y * 0.5f));
                Renderer r = c.GetComponent<Renderer>();
                float y = Mathf.Max(arriba.y, r.bounds.max.y);
                Mesh m = KitS2c.MallaCaja(new Vector3(Mathf.Max(0.1f, tam.x - 0.08f), EspesorLosa, Mathf.Max(0.1f, tam.z - 0.08f)), 2f);
                m.name = "T1318_Azotea_" + mallas.Count;
                mallas.Add(m);
                GameObject go = new GameObject(NombreLosa);
                go.transform.SetParent(t.parent, false);
                go.transform.SetPositionAndRotation(new Vector3(arriba.x, y + EspesorLosa * 0.5f, arriba.z), t.rotation);
                go.AddComponent<MeshFilter>().sharedMesh = m;
                go.AddComponent<MeshRenderer>().sharedMaterial = mat;
                GameObjectUtility.SetStaticEditorFlags(go, GameObjectUtility.GetStaticEditorFlags(c.gameObject));
            }
            if (mallas.Count > 0)
            {
                AssetDatabase.CreateAsset(mallas[0], RutaMallas);
                foreach (Mesh m in mallas.Skip(1))
                {
                    AssetDatabase.AddObjectToAsset(m, RutaMallas);
                }
            }
            return mallas.Count;
        }

        private static int BajarPretiles()
        {
            int n = 0;
            foreach (Transform t in ColocarS2.Todos().Where(t => t.name == NombrePretil).ToList())
            {
                BoxCollider c = t.GetComponent<BoxCollider>();
                if (c == null)
                {
                    continue;
                }
                float sy = Mathf.Abs(t.lossyScale.y);
                if (c.size.y * sy <= ArteT250.AltoColliderPretil + 0.001f)
                {
                    continue;
                }
                c.size = new Vector3(c.size.x, ArteT250.AltoColliderPretil / sy, c.size.z);
                c.center = new Vector3(c.center.x, ArteT250.AltoColliderPretil * 0.5f / sy, c.center.z);
                PrefabUtility.RecordPrefabInstancePropertyModifications(c);
                n++;
            }
            return n;
        }

        /// <summary>Un collider de azotea no debe subir por encima de la malla visible de su cuerpo.</summary>
        private static int RecortarColliders()
        {
            int n = 0;
            foreach (BoxCollider c in Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(EsAzotea))
            {
                float sobra = Limites(c).max.y - c.GetComponent<Renderer>().bounds.max.y;
                if (sobra <= 0.03f)
                {
                    continue;
                }
                float sy = Mathf.Abs(c.transform.lossyScale.y);
                c.size = new Vector3(c.size.x, c.size.y - sobra / sy, c.size.z);
                c.center = new Vector3(c.center.x, c.center.y - sobra * 0.5f / sy, c.center.z);
                PrefabUtility.RecordPrefabInstancePropertyModifications(c);
                n++;
            }
            return n;
        }

        /// <summary>Colliders sin malla visible, con forma de muro, a la altura de un techo, fuera de las rutas de parkour y accesos.</summary>
        public static IEnumerable<Collider> InvisiblesDeBorde()
        {
            string[] nombres = { "Muro", "Pared", "Borde", "Valla", "Barandal", "Baranda", "Pretil" };
            return Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(c =>
                c.enabled && !c.isTrigger && c.GetComponentInChildren<Renderer>() == null && c.name != "Barda"
                && nombres.Any(x => c.name.Contains(x)) && Limites(c).max.y >= 3f && Limites(c).max.y <= 14f && Limites(c).size.y <= 3f
                && (c.transform.root.name == "S2_Techos" || c.transform.root.name == "Barrio" || c.transform.root.name.StartsWith("S2c_") || c.transform.root.name == "T1102_CalleDoc")
                && c.GetComponentsInParent<Transform>().All(p => p.name != "Parkour" && p.name != "Accesos" && p.name != "Tablones"));
        }

        private static int QuitarInvisibles()
        {
            int n = 0;
            foreach (Collider c in InvisiblesDeBorde().ToList())
            {
                Debug.Log("[T1318] collider invisible de borde quitado: " + ColocarS2.Ruta(c.transform));
                Object.DestroyImmediate(c);
                n++;
            }
            return n;
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-541 (plan de la noche 3-nov, paso 2): Neón Alto con torres del kit T20-T23. Cada edificio de barrio de Neón Alto (la manzana del Casino y la
    /// columna de la manzana del Mercado que da a la calle de Neón Alto) se cambia por una torre: 1 base T20 + 6 a 20 pisos T21 con T22 (pantalla)
    /// de vez en cuando, nunca dos T22 seguidos, + 1 remate T23 (de los dos zips de T23 se usa el primero: misma malla, textura más oscura que va
    /// con los pisos). La torre ocupa la planta del edificio (los pisos se ajustan en X y Z) y su cara de pantallas da a la calle. Se quitan las torres
    /// de bloques de T-505 y lo que colgaba arriba de las casas viejas (letreros de azotea, balcones, tinacos). Los postes de la calle de Neón Alto
    /// pasan a N09 (poste rico) con luz blanca fría que sale del foco.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT541.Colocar
    /// </summary>
    public static class ColocarT541
    {
        public const string NombreRaiz = "T541_Torres";
        public const float AltoBase = 12f;
        public const float AltoPiso = 6f;
        public const float AltoRemate = 12f;
        public const int PisosMinimos = 6;
        public const int PisosMaximos = 20;
        public static readonly Color LuzFria = new Color(0.86f, 0.93f, 1f);
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float CalleNeonX = 47f;

        private static readonly StringBuilder Informe = new StringBuilder();

        /// <summary>Edificios de Neón Alto (rutas bajo Barrio).</summary>
        public static readonly string[] Edificios =
        {
            "Manzana_2/Edificios/Edificio_20", "Manzana_2/Edificios/Edificio_21", "Manzana_2/Edificios/Edificio_22",
            "Manzana_3/Edificios/Edificio_23", "Manzana_3/Edificios/Edificio_24", "Manzana_3/Edificios/Edificio_25", "Manzana_3/Edificios/Edificio_26",
            "Manzana_3/Edificios/Edificio_27", "Manzana_3/Edificios/Edificio_28", "Manzana_3/Edificios/Edificio_29",
            "Manzana_3/Edificios/Edificio_30", "Manzana_3/Edificios/Edificio_31", "Manzana_3/Edificios/Edificio_32",
        };

        [MenuItem("Mecha Cyberpunk/T-541/Torres y postes de Neón Alto")]
        public static void Colocar()
        {
            Informe.Clear();
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            GameObject vieja = GameObject.Find(NombreRaiz);
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja);
            }
            Transform raiz = new GameObject(NombreRaiz).transform;
            Transform barrio = GameObject.Find("Barrio").transform;
            GameObject t20 = Prefab("T20");
            GameObject t21 = Prefab("T21");
            GameObject t22 = Prefab("T22");
            GameObject t23 = Prefab("T23");
            System.Random azar = new System.Random(541);
            int torres = 0;
            int pisosTotal = 0;
            List<Bounds> plantas = new List<Bounds>();
            foreach (string ruta in Edificios)
            {
                Transform edificio = barrio.Find(ruta);
                Transform cuerpo = edificio?.Find("Cuerpo");
                if (cuerpo == null)
                {
                    Informe.AppendLine("FALTA " + ruta);
                    continue;
                }
                Bounds planta = cuerpo.GetComponent<Renderer>().bounds;
                plantas.Add(planta);
                edificio.gameObject.SetActive(false);
                // Los pisos de arriba del kit de casas (T-419) viven aparte, en ArteNoche/Pisos/<edificio>_<lado>.
                Transform pisosViejos = barrio.Find("ArteNoche/Pisos");
                if (pisosViejos != null)
                {
                    foreach (Transform lado in pisosViejos.Cast<Transform>().Where(x => x.name.StartsWith(edificio.name + "_")))
                    {
                        lado.gameObject.SetActive(false);
                    }
                }
                bool aLaCalle = Mathf.Abs(planta.center.x - CalleNeonX) < planta.extents.x + 14f;
                int pisos = aLaCalle ? azar.Next(8, 15) : azar.Next(PisosMinimos, PisosMaximos + 1);
                Torre(raiz, edificio.name, planta, HaciaLaCalle(planta), pisos, t20, t21, t22, t23, azar);
                torres++;
                pisosTotal += pisos;
            }
            Informe.AppendLine("Torres de Neón Alto: " + torres + " (" + pisosTotal + " pisos)");
            QuitarLoQueColgaba(plantas);
            Transform t505 = GameObject.Find("T505_NeonAlto")?.transform.Find("Torres");
            if (t505 != null)
            {
                t505.gameObject.SetActive(false);
                Informe.AppendLine("Torres de bloques de T-505 apagadas: " + t505.childCount);
            }
            Postes(raiz);

            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t541-colocar.txt", Informe.ToString());
            Debug.Log("[Arte] T-541 listo:\n" + Informe);
        }

        private static GameObject Prefab(string codigo) => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Exteriores/Prefabs/" + codigo + ".prefab");

        /// <summary>Hacia dónde está la calle más cercana a la planta (la calle de Neón Alto en x = 47, la principal en z = 0, o las orillas).</summary>
        public static Vector3 HaciaLaCalle(Bounds planta)
        {
            (float distancia, Vector3 dir)[] opciones =
            {
                (Mathf.Abs(planta.min.x - CalleNeonX), Vector3.left),
                (Mathf.Abs(planta.max.x - CalleNeonX), Vector3.right),
                (Mathf.Abs(planta.min.z), Vector3.back),
            };
            return opciones.OrderBy(o => o.distancia).First().dir;
        }

        private static void Torre(Transform padre, string nombre, Bounds planta, Vector3 calle, int pisos, GameObject t20, GameObject t21, GameObject t22,
            GameObject t23, System.Random azar)
        {
            Transform torre = new GameObject("Torre_" + nombre).transform;
            torre.SetParent(padre, false);
            torre.position = new Vector3(planta.center.x, planta.min.y, planta.center.z);
            // Se arma sin girar (las medidas de cada pieza salen en sus propios ejes) y al final se gira.
            bool girada = Mathf.Abs(calle.x) > 0.5f;
            float ancho = girada ? planta.size.z : planta.size.x;
            float fondo = girada ? planta.size.x : planta.size.z;
            float y = 0f;
            y = Pieza(torre, t20, "Base", y, AltoBase, ancho, fondo);
            bool ultimaPantalla = false;
            for (int i = 0; i < pisos; i++)
            {
                bool pantalla = !ultimaPantalla && i > 0 && azar.NextDouble() < 0.25;
                y = Pieza(torre, pantalla ? t22 : t21, pantalla ? "PisoPantalla" : "Piso", y, AltoPiso, ancho, fondo);
                ultimaPantalla = pantalla;
            }
            Pieza(torre, t23, "Remate", y, AltoRemate, ancho, fondo);
            // La cara de pantallas del kit (la -Z del modelo) mira a la calle.
            torre.rotation = Quaternion.LookRotation(-calle, Vector3.up);
        }

        // Pone una pieza con su base a la altura 'y' de la torre, de 'alto' m y que llena la planta; devuelve la altura de su techo.
        private static float Pieza(Transform torre, GameObject prefab, string nombre, float y, float alto, float ancho, float fondo)
        {
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, torre);
            g.name = nombre;
            g.transform.localRotation = Quaternion.identity;
            g.transform.localScale = Vector3.one;
            Bounds b = ArteT250.Limites(g);
            Vector3 escala = new Vector3(ancho / Mathf.Max(0.01f, b.size.x), alto / Mathf.Max(0.01f, b.size.y), fondo / Mathf.Max(0.01f, b.size.z));
            g.transform.localScale = escala;
            g.transform.localPosition = new Vector3(0f, y, 0f);
            foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = nombre == "Base" ? ShadowCastingMode.On : ShadowCastingMode.Off;
            }
            GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            return y + alto;
        }

        // Letreros de azotea, balcones, macetas y tinacos que estaban pegados arriba de las casas viejas (más de 4.5 m): fuera. Lo de la banqueta se queda.
        private static void QuitarLoQueColgaba(List<Bounds> plantas)
        {
            int n = 0;
            foreach (Transform t in EditorSceneManager.GetActiveScene().GetRootGameObjects().Where(g => g.name != NombreRaiz)
                .SelectMany(g => g.GetComponentsInChildren<Transform>()).ToList())
            {
                if (t == null || !t.gameObject.activeInHierarchy || t.GetComponent<Renderer>() == null && t.GetComponent<Light>() == null)
                {
                    continue;
                }
                if (t.GetComponentInParent<LODGroup>() != null && t.GetComponentInParent<LODGroup>().transform != t || t.root.name == "Barrio" && t.parent != null && t.parent.name == "Calles")
                {
                    continue;
                }
                Vector3 p = t.position;
                if (p.y < 4.5f || p.y > 60f || DeUnLugar(t) || Excluida(t.root.name) || t.GetComponent<Light>() != null && t.GetComponent<Light>().type == LightType.Directional)
                {
                    continue;
                }
                foreach (Bounds planta in plantas)
                {
                    Bounds b = planta;
                    b.Expand(new Vector3(4f, 200f, 4f));
                    if (b.Contains(p))
                    {
                        Transform quitar = t;
                        // Se apaga el grupo del letrero completo, no pieza por pieza.
                        while (quitar.parent != null && quitar.parent.parent != null && quitar.parent.GetComponent<Renderer>() == null && quitar.parent.name.StartsWith("Letrero"))
                        {
                            quitar = quitar.parent;
                        }
                        quitar.gameObject.SetActive(false);
                        n++;
                        break;
                    }
                }
            }
            Informe.AppendLine("Piezas que colgaban arriba de las casas viejas, fuera: " + n);
        }

        private static readonly string[] RaicesQueSeQuedan = { "T513_Fondo", "T530_Afueras", "T531_Locales", "CicloDiaNoche", "LuzNoche", "PosprocesoNoche", "PosprocesoDia", "JugadorAPie", "RelojMundo" };

        private static bool Excluida(string raiz) => RaicesQueSeQuedan.Contains(raiz);

        private static bool DeUnLugar(Transform t)
        {
            for (Transform x = t; x != null; x = x.parent)
            {
                if (x.name.StartsWith("Lugar_") || x.name == "Nubes")
                {
                    return true;
                }
            }
            return false;
        }

        // ---------------------------------------------------------------- Postes

        private static void Postes(Transform raiz)
        {
            GameObject n09 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Calle/Prefabs/N09.prefab");
            Transform farolas = GameObject.Find("Barrio").transform.Find("Farolas");
            Transform grupo = new GameObject("PostesN09").transform;
            grupo.SetParent(raiz, false);
            int n = 0;
            foreach (Transform farola in farolas)
            {
                Vector3 p = farola.position;
                if (p.x < 36f || p.x > 58f || p.z < 10f || p.z > 90f)
                {
                    continue;
                }
                foreach (Renderer r in farola.GetComponentsInChildren<Renderer>())
                {
                    r.enabled = false;
                }
                GameObject poste = (GameObject)PrefabUtility.InstantiatePrefab(n09, grupo);
                poste.name = "N09_" + farola.name;
                foreach (Collider c in poste.GetComponentsInChildren<Collider>())
                {
                    // La caja del modelo abarca el brazo y tapaba la banqueta: el collider del poste viejo (el tubo) se queda.
                    Object.DestroyImmediate(c);
                }
                Vector3 haciaCalle = new Vector3(Mathf.Sign(CalleNeonX - p.x), 0f, 0f);
                poste.transform.SetPositionAndRotation(new Vector3(p.x, p.y, p.z), Quaternion.LookRotation(haciaCalle));
                MeshFilter malla = poste.GetComponentInChildren<MeshFilter>();
                if (ColocarT535.Foco(malla, out Vector3 foco))
                {
                    Vector3 brazo = foco - p;
                    brazo.y = 0f;
                    if (Vector3.Dot(brazo, haciaCalle) < 0f)
                    {
                        // El brazo del modelo apuntaba al revés: se gira para que la luz caiga sobre la calle.
                        poste.transform.rotation = Quaternion.LookRotation(-haciaCalle);
                        ColocarT535.Foco(malla, out foco);
                    }
                    Light luz = farola.GetComponentsInChildren<Light>(true).FirstOrDefault();
                    if (luz != null)
                    {
                        luz.transform.position = foco;
                        luz.color = LuzFria;
                    }
                }
                n++;
            }
            Informe.AppendLine("Postes N09 en la calle de Neón Alto con luz blanca fría: " + n);
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-855 (playtest 6-nov tarde): la luz de cada poste sale de su lámpara. Para cada malla de poste se buscan las cabezas de lámpara por sus
    /// vértices (<see cref="Cabezas"/>): el eje del tubo sale de los vértices de la base; arriba, lo más lejos del eje es la punta del brazo y la
    /// cabeza es el racimo de vértices alrededor de esa punta (si hay brazo del otro lado, otra cabeza); sin brazo, la lámpara es el remate.
    /// La luz va justo debajo de la cabeza, apuntando al piso.
    /// Fotos de revisión: -executeMethod MechaCyberpunk.Arte.Editor.PostesS2t.FotosMallas (docs/capturas/s2t-poste-malla-*.png)
    /// </summary>
    public static class PostesS2t
    {
        // Fracciones del alto del poste.
        private const float FraccionBase = 0.08f;
        private const float FraccionArriba = 0.55f;
        private const float FraccionRemate = 0.12f;
        /// <summary>Brazo mínimo (m) para que la cabeza sea la punta del brazo y no el remate.</summary>
        public const float BrazoMinimo = 0.35f;
        /// <summary>Qué tan lejos del brazo (m, de lado) entra un vértice en la cabeza.</summary>
        public const float RadioCabeza = 0.45f;
        /// <summary>La cabeza empieza a esta fracción del largo del brazo, desde el tubo.</summary>
        public const float FraccionBrazoCabeza = 0.55f;
        /// <summary>La luz queda así de abajo de los vértices más bajos de la cabeza (m).</summary>
        public const float BajoCabeza = 0.06f;

        /// <summary>Las cabezas de lámpara de una malla, en coordenadas del mundo (punto justo debajo de cada una).</summary>
        public static List<Vector3> Cabezas(Mesh malla, Matrix4x4 aMundo)
        {
            List<Vector3> salida = new List<Vector3>();
            Vector3[] vs = malla.vertices.Select(v => aMundo.MultiplyPoint3x4(v)).ToArray();
            if (vs.Length < 10)
            {
                return salida;
            }
            float minY = vs.Min(v => v.y);
            float maxY = vs.Max(v => v.y);
            float alto = maxY - minY;
            Vector3[] basev = vs.Where(v => v.y < minY + alto * FraccionBase).ToArray();
            Vector2 eje = new Vector2(basev.Average(v => v.x), basev.Average(v => v.z));
            float R(Vector3 v) => new Vector2(v.x - eje.x, v.z - eje.y).magnitude;
            Vector3[] arriba = vs.Where(v => v.y > minY + alto * FraccionArriba).ToArray();
            Vector3 punta = arriba.OrderByDescending(R).First();
            if (R(punta) < BrazoMinimo)
            {
                // Sin brazo: la lámpara es el remate (farol de copa). La luz, al centro del remate, a media altura de él.
                Vector3[] remate = vs.Where(v => v.y > maxY - alto * FraccionRemate).ToArray();
                salida.Add(new Vector3(remate.Average(v => v.x), remate.Min(v => v.y) + (maxY - remate.Min(v => v.y)) * 0.3f, remate.Average(v => v.z)));
                return salida;
            }
            salida.Add(Cabeza(arriba, eje, punta));
            // ¿Brazo del otro lado? La punta más lejana en la dirección contraria, si es casi tan larga.
            Vector2 dir = new Vector2(punta.x - eje.x, punta.z - eje.y).normalized;
            Vector3[] otroLado = arriba.Where(v => Vector2.Dot(new Vector2(v.x - eje.x, v.z - eje.y), dir) < -BrazoMinimo).ToArray();
            if (otroLado.Length > 0)
            {
                Vector3 otra = otroLado.OrderByDescending(R).First();
                if (R(otra) > R(punta) * 0.7f)
                {
                    salida.Add(Cabeza(arriba, eje, otra));
                }
            }
            return salida;
        }

        // La cabeza es la mitad de afuera del brazo (desde FraccionBrazoCabeza de su largo), cerca de la línea del brazo: la luz va al centro de
        // esa parte y justo debajo de su vértice más bajo (en la punta quedaba en la orilla de la pantalla).
        private static Vector3 Cabeza(Vector3[] arriba, Vector2 eje, Vector3 punta)
        {
            Vector2 dir = new Vector2(punta.x - eje.x, punta.z - eje.y);
            float largo = dir.magnitude;
            dir /= largo;
            Vector3[] cab = arriba.Where(v =>
            {
                Vector2 d = new Vector2(v.x - eje.x, v.z - eje.y);
                float a = Vector2.Dot(d, dir);
                float lado = Mathf.Abs(d.x * dir.y - d.y * dir.x);
                return a > largo * FraccionBrazoCabeza && lado < RadioCabeza;
            }).ToArray();
            if (cab.Length == 0)
            {
                cab = new[] { punta };
            }
            return new Vector3(cab.Average(v => v.x), cab.Min(v => v.y) - BajoCabeza, cab.Average(v => v.z));
        }

        /// <summary>True si el nombre o la malla parece de poste de luz.</summary>
        public static bool EsPoste(string texto)
        {
            string n = texto.ToLowerInvariant();
            return n.Contains("poste") || n.Contains("farol") || n.Contains("c07") || n.Contains("n09") || n.Contains("luminaria");
        }

        /// <summary>Las mallas de poste distintas que hay en la ciudad, con un ejemplo de cada una.</summary>
        public static List<MeshFilter> MallasEnLaCiudad()
        {
            ColocarS2.Abrir();
            return ColocarS2.Todos().Select(t => t.GetComponent<MeshFilter>())
                .Where(m => m != null && m.sharedMesh != null && m.GetComponent<Renderer>() != null
                    && EsPoste(m.sharedMesh.name + " " + AssetDatabase.GetAssetPath(m.sharedMesh) + " " + m.name))
                .GroupBy(m => m.sharedMesh).Select(g => g.First()).ToList();
        }

        /// <summary>El modelo que se ve de una farola (el C07 del kit o el N09): su MeshFilter con renderer prendido, o null.</summary>
        public static MeshFilter ModeloVisible(Transform poste)
        {
            return poste.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(m => m.sharedMesh != null && m.gameObject.activeInHierarchy
                && m.GetComponent<Renderer>() != null && m.GetComponent<Renderer>().enabled
                && (AssetDatabase.GetAssetPath(m.sharedMesh).Contains("C07_PosteLuz") || AssetDatabase.GetAssetPath(m.sharedMesh).Contains("/N09")));
        }

        /// <summary>Los postes de luz de la ciudad: las farolas (C07) y los N09.</summary>
        public static Transform[] Postes()
        {
            return ColocarS2.Todos().Where(t => t.gameObject.activeInHierarchy && (t.name.StartsWith("Farola_") || t.name.StartsWith("N09_"))
                && ModeloVisible(t) != null).ToArray();
        }

        /// <summary>Cono de la luz de un poste (°) y su centro sin difuminar (°): un charco claro bajo la lámpara, no toda la calle.</summary>
        public const float AnguloCono = 80f;
        public const float AnguloCentro = 45f;
        public const string NombreLuzNueva = "Luz_S2t";
        public static readonly Color ColorN09 = new Color(0.72f, 0.93f, 1f);

        /// <summary>
        /// Pone la luz de cada poste (farolas C07 y N09) justo debajo de su lámpara apuntando al piso. Las farolas ya traen su luz "Luz" y su
        /// LuzNocturna; a los N09 se les agrega una (con los mismos valores) y su LuzNocturna. Si un poste tiene dos lámparas, la segunda lleva su
        /// propia luz. El poste no recibe la luz de su lámpara (capa de render CapaPostes), así no parece que la luz sale del tubo. Idempotente.
        /// </summary>
        [MenuItem("Mecha Cyberpunk/S2t/2 Luz de los postes en la lampara")]
        public static void ColocarLuces()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Transform[] postes = Postes();
            Light modelo = postes.Select(p => p.Find("Luz")?.GetComponent<Light>()).FirstOrDefault(l => l != null);
            if (modelo == null)
            {
                Debug.LogError("[Arte] T-855: ninguna farola trae su luz de modelo");
                return;
            }
            int puestas = 0;
            foreach (Transform poste in postes)
            {
                MeshFilter mf = ModeloVisible(poste);
                List<Vector3> cabezas = Cabezas(mf.sharedMesh, mf.transform.localToWorldMatrix);
                if (cabezas.Count == 0)
                {
                    ColocarS2.Informe.AppendLine("SIN CABEZA " + poste.name);
                    continue;
                }
                for (int i = 0; i < cabezas.Count; i++)
                {
                    string nombre = i == 0 ? "Luz" : NombreLuzNueva + "_" + i;
                    Transform t = poste.Find(nombre) ?? (i == 0 ? poste.Find(NombreLuzNueva) : null);
                    if (t == null)
                    {
                        t = Object.Instantiate(modelo.gameObject, poste).transform;
                        t.name = i == 0 ? NombreLuzNueva : nombre;
                    }
                    Light l = t.GetComponent<Light>();
                    t.SetPositionAndRotation(cabezas[i], Quaternion.Euler(90f, 0f, 0f));
                    l.type = LightType.Spot;
                    l.spotAngle = AnguloCono;
                    l.innerSpotAngle = AnguloCentro;
                    if (poste.name.StartsWith("N09"))
                    {
                        l.color = ColorN09; // su pantalla es turquesa fría, no naranja como la del C07
                    }
                    UniversalAdditionalLightData d = l.GetUniversalAdditionalLightData();
                    d.renderingLayers = d.renderingLayers & ~(1u << ColocarS2.CapaPostes);
                    EditorUtility.SetDirty(d);
                    EditorUtility.SetDirty(l);
                    Encender(poste, l, i);
                    puestas++;
                }
                foreach (Renderer rp in mf.transform.root == poste.root ? poste.GetComponentsInChildren<Renderer>(true) : new Renderer[0])
                {
                    rp.renderingLayerMask = 1u << ColocarS2.CapaPostes;
                    EditorUtility.SetDirty(rp);
                }
            }
            ColocarS2.Informe.AppendLine("T-855: " + postes.Length + " postes, " + puestas + " luces bajo su lámpara");
            ColocarS2.Guardar("s2t-postes");
        }

        // La luz se prende de noche con una LuzNocturna. Las farolas ya traen la suya (con la luz "Luz"); las luces nuevas llevan la suya en su
        // propio objeto (no se toca la de la farola).
        private static void Encender(Transform poste, Light l, int i)
        {
            Mundo.Reloj.LuzNocturna propia = l.GetComponent<Mundo.Reloj.LuzNocturna>();
            bool laPrendeOtra = poste.GetComponentsInChildren<Mundo.Reloj.LuzNocturna>(true)
                .Any(n => n != propia && new SerializedObject(n).FindProperty("luz").objectReferenceValue == l);
            if (laPrendeOtra)
            {
                return;
            }
            if (propia == null)
            {
                propia = l.gameObject.AddComponent<Mundo.Reloj.LuzNocturna>();
            }
            SerializedObject so = new SerializedObject(propia);
            so.FindProperty("luz").objectReferenceValue = l;
            so.ApplyModifiedPropertiesWithoutUndo();
            l.enabled = false;
        }

        public static void FotosAntes() => FotosNoche("antes");

        public static void FotosDespues() => FotosNoche("despues");

        /// <summary>Fotos de noche de cerca de unos postes de cada tipo: docs/capturas/s2t-poste-noche-&lt;cuando&gt;-N.png.</summary>
        public static void FotosNoche(string cuando)
        {
            ColocarS2.Abrir();
            Transform[] postes = Postes();
            var elegidos = postes.Where(p => p.name.StartsWith("Farola_") && p.parent.name == "Farolas" && p.root.name == "Barrio").Take(2)
                .Concat(postes.Where(p => p.root.name == "S2n_Calles").Take(1))
                .Concat(postes.Where(p => p.name.StartsWith("N09")).Take(1)).ToArray();
            for (int i = 0; i < elegidos.Length; i++)
            {
                MeshFilter mf = ModeloVisible(elegidos[i]);
                Bounds b = mf.GetComponent<Renderer>().bounds;
                Vector3 baseP = new Vector3(elegidos[i].position.x, b.min.y, elegidos[i].position.z);
                List<Vector3> cab = Cabezas(mf.sharedMesh, mf.transform.localToWorldMatrix);
                Vector3 cabeza = cab.Count > 0 ? cab[0] : b.center;
                Vector3 brazo = new Vector3(cabeza.x - baseP.x, 0f, cabeza.z - baseP.z);
                Vector3 lado = brazo.sqrMagnitude > 0.01f ? Vector3.Cross(Vector3.up, brazo.normalized) : Vector3.right;
                Vector3 desde = new Vector3(cabeza.x, baseP.y + 2.2f, cabeza.z) + lado * 5.5f + brazo.normalized * 1.5f;
                Vector3 hacia = Vector3.Lerp(cabeza, new Vector3(cabeza.x, baseP.y, cabeza.z), 0.55f);
                ColocarS2t.FotoHora(22f, desde, hacia, ColocarS2t.CarpetaCapturas + "s2t-poste-noche-" + cuando + "-" + i + ".png", 70f);
            }
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }

        /// <summary>Foto de lado (dos ángulos) de cada malla de poste con una esfera roja donde iría la luz. Para revisar antes de mover luces.</summary>
        [MenuItem("Mecha Cyberpunk/S2t/Postes: fotos de las mallas")]
        public static void FotosMallas()
        {
            var mallas = MallasEnLaCiudad()
                .Select(m => (malla: m.sharedMesh, mats: m.GetComponent<Renderer>().sharedMaterials, ruta: AssetDatabase.GetAssetPath(m.sharedMesh) + " (ej " + m.name + ")",
                    giro: m.transform.rotation, escala: m.transform.lossyScale)).ToList();
            StringBuilder informe = new StringBuilder();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.7f, 0.7f, 0.75f);
            Material rojo = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = Color.red };
            int i = 0;
            foreach (var (malla, mats, ruta, giro, escala) in mallas)
            {
                GameObject g = new GameObject("Poste");
                g.transform.SetPositionAndRotation(Vector3.zero, giro);
                g.transform.localScale = escala;
                g.AddComponent<MeshFilter>().sharedMesh = malla;
                g.AddComponent<MeshRenderer>().sharedMaterials = mats;
                List<Vector3> cabezas = Cabezas(malla, g.transform.localToWorldMatrix);
                Bounds b = g.GetComponent<Renderer>().bounds;
                foreach (Vector3 c in cabezas)
                {
                    GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    s.transform.position = c;
                    s.transform.localScale = Vector3.one * Mathf.Max(0.12f, b.size.y * 0.025f);
                    s.GetComponent<Renderer>().sharedMaterial = rojo;
                }
                string nombre = "s2t-poste-malla-" + i + ".png";
                informe.AppendLine(nombre + "\t" + ruta + " :: " + malla.name + "\talto " + b.size.y.ToString("F2") + "\tcabezas " + string.Join(" ", cabezas.Select(c => (c - b.center).ToString("F2"))));
                float d = b.size.y * 1.3f;
                Texture2D a = Retratar(b.center + new Vector3(d, 0f, 0f), b.center);
                Texture2D z = Retratar(b.center + new Vector3(0f, 0f, d), b.center);
                Texture2D hoja = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                hoja.SetPixels(0, 0, 640, 720, a.GetPixels());
                hoja.SetPixels(640, 0, 640, 720, z.GetPixels());
                hoja.Apply();
                File.WriteAllBytes(ColocarS2t.CarpetaCapturas + nombre, hoja.EncodeToPNG());
                foreach (GameObject o in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Where(o => o.name != "Main Camera"))
                {
                    Object.DestroyImmediate(o);
                }
                i++;
            }
            File.WriteAllText("Logs/s2t-postes-mallas.txt", informe.ToString());
            EditorSceneManager.OpenScene(ColocarS2.RutaEscena, OpenSceneMode.Single);
        }

        private static Texture2D Retratar(Vector3 desde, Vector3 hacia)
        {
            GameObject camObj = new GameObject("Cam");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<UniversalAdditionalCameraData>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.25f, 0.27f, 0.32f);
            cam.fieldOfView = 50f;
            cam.transform.position = desde;
            cam.transform.LookAt(hacia);
            RenderTexture rt = new RenderTexture(640, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(640, 720, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, 640, 720), 0, 0);
            foto.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(camObj);
            return foto;
        }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-542 (plan de la noche 3-nov, paso 2): letreros 3D y brillo de noche. Idempotente sobre Ciudad_Prologo:
    /// - Materiales: las piezas de torre (T20-T23) con sus ventanas y tiras encendidas (mapa de emisión de Fuentes~/generar_emision.py); los
    ///   letreros L10-L14 con el texto que brilla solo de noche (LuzNocturna sobre su renderer).
    /// - L10 VivaCoin sobre la entrada del Casino (fuera el letrero plano, los foquitos y tubos de formas), L11 Hotel en lugar del letrero de
    ///   neón plano del hotel de la calle de tu casa, L12 El Mercado sobre la puerta de El Mercado, L13 I+D+I en el local del Doc y L14 CIC en el
    ///   de Tino (fuera sus letreros planos).
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarT542.Colocar
    /// </summary>
    public static class ColocarT542
    {
        public const string NombreRaiz = "T542_Letreros";
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string Carpeta = "Assets/Arte/Exteriores/";
        public const float BrilloTorre = 3f;
        public const float BrilloLetrero = 2.2f;

        /// <summary>Letrero, posición (centro) y hacia dónde mira su frente.</summary>
        public static readonly (string codigo, Vector3 pos, Vector3 frente)[] Lugares =
        {
            ("L10", new Vector3(56.85f, 5f, 47f), Vector3.left),
            ("L11", new Vector3(-37.7f, 10.6f, -66.4f), Vector3.left),
            ("L12", new Vector3(-24.6f, 4.6f, 26f), Vector3.right),
            ("L13", new Vector3(-100f, 3.35f, -9.55f), Vector3.forward),
            ("L14", new Vector3(-73.3f, 3.35f, 9.6f), Vector3.back),
        };

        private static readonly StringBuilder Informe = new StringBuilder();

        [MenuItem("Mecha Cyberpunk/T-542/Letreros 3D y brillo")]
        public static void Colocar()
        {
            Informe.Clear();
            foreach (string c in new[] { "T20", "T21", "T22", "T23" })
            {
                Emision(c, BrilloTorre);
            }
            foreach (var l in Lugares)
            {
                Emision(l.codigo, BrilloLetrero);
            }
            AssetDatabase.SaveAssets();

            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            GameObject vieja = GameObject.Find(NombreRaiz);
            if (vieja != null)
            {
                Object.DestroyImmediate(vieja);
            }
            Transform raiz = new GameObject(NombreRaiz).transform;
            Type tipoLuzNocturna = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("MechaCyberpunk.Mundo.Reloj.LuzNocturna")).FirstOrDefault(t => t != null);
            foreach (var l in Lugares)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Carpeta + "Prefabs/" + l.codigo + ".prefab");
                GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz);
                g.name = l.codigo;
                // El frente de los modelos de Tripo es su -Z.
                g.transform.rotation = Quaternion.LookRotation(-l.frente, Vector3.up);
                if (l.codigo == "L11")
                {
                    // El letrero vertical del hotel es de bandera: sus soportes (cara -X del modelo) van contra el muro y las letras se leen a lo largo
                    // de la calle.
                    g.transform.rotation = Quaternion.FromToRotation(Vector3.left, -l.frente);
                }
                Bounds b = ArteT250.Limites(g);
                g.transform.position += l.pos - b.center;
                // Pegado al muro de verdad: un rayo desde la calle hacia el muro, a la altura del letrero.
                Physics.SyncTransforms();
                float grueso = Mathf.Abs(Vector3.Dot(b.size, new Vector3(Mathf.Abs(l.frente.x), 0f, Mathf.Abs(l.frente.z))));
                RaycastHit muro = default;
                bool hay = false;
                foreach (RaycastHit h in Physics.RaycastAll(l.pos + l.frente * 6f, -l.frente, 12f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (h.collider.transform.IsChildOf(g.transform) || h.collider.bounds.size.y < 2.5f || (hay && h.distance >= muro.distance))
                    {
                        continue;
                    }
                    muro = h;
                    hay = true;
                }
                if (hay)
                {
                    Vector3 centro = new Vector3(l.pos.x, l.pos.y, l.pos.z);
                    Vector3 enMuro = muro.point + l.frente * (grueso * 0.5f + 0.05f);
                    centro = Mathf.Abs(l.frente.x) > 0.5f ? new Vector3(enMuro.x, centro.y, centro.z) : new Vector3(centro.x, centro.y, enMuro.z);
                    g.transform.position += centro - l.pos;
                    Informe.AppendLine(l.codigo + " pegado al muro " + muro.collider.name + " en " + centro);
                }
                foreach (Collider c in g.GetComponentsInChildren<Collider>())
                {
                    Object.DestroyImmediate(c); // van en la pared, sin tapar el paso
                }
                if (tipoLuzNocturna != null)
                {
                    Component luz = g.AddComponent(tipoLuzNocturna);
                    SerializedObject so = new SerializedObject(luz);
                    SerializedProperty rs = so.FindProperty("renderers");
                    Renderer[] renders = g.GetComponentsInChildren<Renderer>();
                    rs.arraySize = renders.Length;
                    for (int i = 0; i < renders.Length; i++)
                    {
                        rs.GetArrayElementAtIndex(i).objectReferenceValue = renders[i];
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            Informe.AppendLine("Letreros 3D colocados: " + Lugares.Length);
            QuitarPlanos();

            UnityEngine.SceneManagement.Scene escena = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/t542-colocar.txt", Informe.ToString());
            Debug.Log("[Arte] T-542 listo:\n" + Informe);
        }

        private static void Emision(string codigo, float brillo)
        {
            string ruta = Carpeta + codigo + "/" + codigo + "_emision.png";
            AssetDatabase.ImportAsset(ruta);
            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
            Material m = AssetDatabase.LoadAssetAtPath<Material>(Carpeta + codigo + "/" + codigo + ".mat");
            if (t == null || m == null)
            {
                Informe.AppendLine("FALTA emisión o material de " + codigo);
                return;
            }
            m.SetTexture("_EmissionMap", t);
            m.SetColor("_EmissionColor", Color.white * brillo);
            // T-546: con None el postprocesador de materiales de URP quita _EMISSION al reimportar (solo la deja si el GI es emisivo).
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetKeyword(new UnityEngine.Rendering.LocalKeyword(m.shader, "_EMISSION"), true);
            m.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssetIfDirty(m);
            Informe.AppendLine("Emisión " + codigo + " x" + brillo + " (palabra " + m.IsKeywordEnabled("_EMISSION") + ")");
        }

        private static void QuitarPlanos()
        {
            int n = 0;
            void Apagar(Transform t)
            {
                if (t != null && t.gameObject.activeSelf)
                {
                    t.gameObject.SetActive(false);
                    n++;
                }
            }
            Transform letreros = GameObject.Find("Barrio")?.transform.Find("LetrerosNeon");
            Apagar(letreros?.Find("LetreroNeon_3_Casino"));
            Apagar(letreros?.Find("LetreroNeon_5_Hotel"));
            // El letrero plano VIVACOIN de T-414 (fondo y texto) encima de la entrada.
            Transform t414 = GameObject.Find("GTA_T414")?.transform;
            Apagar(t414?.Find("LetreroVivaCoin_Fondo"));
            Apagar(t414?.Find("LetreroVivaCoin"));
            Transform casino = GameObject.Find("T505_NeonAlto")?.transform.Find("Piezas/Casino");
            if (casino != null)
            {
                foreach (Transform t in casino.Cast<Transform>().Where(t => t.name.StartsWith("Foco_") || t.name.StartsWith("Tubo_")).ToList())
                {
                    Apagar(t);
                }
            }
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(t => (t.name == "Letrero_HOTEL" || t.name == "LetreroCanvas") && Vector3.Distance(t.position, new Vector3(-37f, 0f, -66.4f)) < 6f).ToList())
            {
                Apagar(t);
            }
            // El letrero amarillo de lámina del Casino (T-414) que tapaba la fachada.
            foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                .Where(r => r.sharedMaterial != null && r.sharedMaterial.name.StartsWith("T414_NeonCasino") && Vector3.Distance(r.bounds.center, new Vector3(57f, 4f, 47f)) < 5f).ToList())
            {
                Apagar(r.transform);
            }
            Informe.AppendLine("Letreros planos y formas que reemplazan: " + n);
        }
    }
}

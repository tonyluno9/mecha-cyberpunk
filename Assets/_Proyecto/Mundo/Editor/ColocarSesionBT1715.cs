using System.Linq;
using System.Text;
using MechaCyberpunk.Mundo.Lugares;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>Sesión A 23-nov (ronda E de B): abre paso a los SKULLS del inicio y saca la armería de la historia.</summary>
    public static class ColocarSesionBT1715
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";

        public const string RutaPatrulla = "GTA_T1630_SoloPatrullas/Estacionados/Estacionado_2_V13";
        public const float ZPatrullaNueva = -60f;
        public static readonly Vector3 SalidaCallejon = new Vector3(-60f, 0f, -62f);
        public static readonly Vector3 EsperaSkulls = new Vector3(-46.5f, 0f, -76.5f);
        public static readonly Vector3 HuacalesAntes = new Vector3(-58.6f, 0.2f, -62.8f);
        public static readonly Vector3 HuacalesNuevo = new Vector3(-62.4f, 0f, -62.8f);
        public const string RutaReja = "Assets/Arte/Calle/Prefabs/PU03_Reja.prefab";
        public const string RaizCierre = "T1715_ArmeriaCerrada";
        /// <summary>Letreros de la armería que se apagan (no se borran).</summary>
        public static readonly string[] LetrerosArmeria =
        {
            "T1320_Calles/CalleArmeria/Tablero_ARMERIA", "T1320_Calles/CalleArmeria/Letrero_ARMERIA", "T1320_Calles/CalleArmeria/Luz",
            "GTA_T407/Mercado/LetreroArmasFondo", "GTA_T407/Mercado/NeonLetrero", "T542_Letreros/L12",
        };
        public const string RutaPuertaLocal = "Barrio/Manzana_2/Lugar_MercadoNegro/Puerta";
        public const string RutaMostrador = "Barrio/Manzana_2/Lugar_MercadoNegro/Interior/Servicios/MostradorMercadoNegro";

        /// <summary>True si el pasillo de <paramref name="ancho"/> m entre a y b no tiene colliders (el suelo no cuenta). Devuelve el primer estorbo.</summary>
        public static bool HuecoLibre(Vector3 a, Vector3 b, float ancho, out string estorbo)
        {
            Physics.SyncTransforms();
            Vector3 d = b - a; d.y = 0f;
            Vector3 centro = (a + b) * 0.5f; centro.y = 1.1f;
            Quaternion rot = Quaternion.LookRotation(d.normalized, Vector3.up);
            foreach (Collider c in Physics.OverlapBox(centro, new Vector3(ancho * 0.5f, 0.8f, d.magnitude * 0.5f), rot, ~0, QueryTriggerInteraction.Ignore))
            {
                if (c.bounds.max.y < 0.6f) { continue; }
                estorbo = c.name + " (" + c.transform.root.name + ") " + c.bounds.center.ToString("F1");
                return false;
            }
            estorbo = null;
            return true;
        }

        /// <summary>Hasta t=0.3 el camino va dentro del callejón (cajas y muros propios); desde ahí es calle y debe estar libre.</summary>
        public const float TramoCalleDesde = 0.3f;

        public static bool CaminoSkullsLibre(float ancho, out string estorbo)
        {
            return HuecoLibre(Vector3.Lerp(SalidaCallejon, EsperaSkulls, TramoCalleDesde), EsperaSkulls, ancho, out estorbo);
        }

        public static void Aplicar()
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            bool antes = HuecoLibre(SalidaCallejon, EsperaSkulls, 2f, out string estorboAntes);
            Debug.Log("[BT1715] antes: hueco=" + antes + " " + estorboAntes);
            // 1. La patrulla estacionada sale de la línea callejón -> espera, calle abajo (mismo carril y yaw).
            GameObject pat = GameObject.Find(RutaPatrulla);
            Vector3 p = pat.transform.position; p.z = ZPatrullaNueva; pat.transform.position = p;
            // 1b. Los huacales de la boca del callejón dejaban un bolsillo de 0.3 m contra el edificio donde se atoraba un SKULL: se corren junto a N06.
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == "C17_Huacales" && t.root.name == "S2_Callejon" && Vector3.Distance(t.position, HuacalesAntes) < 0.3f)
                {
                    t.position = new Vector3(HuacalesNuevo.x, t.position.y, HuacalesNuevo.z);
                    break;
                }
            }
            // 2. Armería: letreros apagados, puerta que ya no abre y una reja real delante.
            foreach (string r in LetrerosArmeria) { GameObject g = GameObject.Find(r); if (g != null) { g.SetActive(false); } }
            PuertaLocal puerta = GameObject.Find(RutaPuertaLocal).GetComponent<PuertaLocal>();
            puerta.enabled = false;
            GameObject viejo = GameObject.Find(RaizCierre);
            if (viejo != null) { Object.DestroyImmediate(viejo); }
            GameObject raiz = new GameObject(RaizCierre);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaReja);
            foreach (float z in new[] { 25.24f, 26.76f })
            {
                GameObject reja = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz.transform);
                reja.name = "Reja_" + z.ToString("F2");
                reja.transform.SetPositionAndRotation(new Vector3(-24.4f, 0.2f, z), Quaternion.Euler(0f, 90f, 0f));
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Physics.SyncTransforms();
            Debug.Log("[BT1715] aplicado. patrulla=" + pat.transform.position.ToString("F2") + " hueco=" + HuecoLibre(SalidaCallejon, EsperaSkulls, 2f, out string e) + " " + e);
        }

        public static void DespuesArmeria() { EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single); FotosArmeria("despues"); }

        /// <summary>Foto en edición desde una cámara temporal (sin HUD).</summary>
        public static void Foto(string ruta, Vector3 desde, Vector3 hacia, float fov)
        {
            GameObject g = new GameObject("CamTemp1715");
            Camera cam = g.AddComponent<Camera>();
            cam.fieldOfView = fov; cam.nearClipPlane = 0.1f; cam.farClipPlane = 300f;
            cam.transform.position = desde; cam.transform.LookAt(hacia);
            RenderTexture rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            Texture2D t = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); t.Apply();
            RenderTexture.active = null;
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ruta));
            System.IO.File.WriteAllBytes(ruta, t.EncodeToPNG());
            Object.DestroyImmediate(t); Object.DestroyImmediate(rt); Object.DestroyImmediate(g);
        }

        public const string CarpetaCapturas = "docs/capturas/_tareas/para-sesion-A-e/";

        public static void FotosArmeria(string prefijo)
        {
            Foto(CarpetaCapturas + prefijo + "-armeria-frente.png", new Vector3(-12f, 1.7f, 26f), new Vector3(-25f, 2.5f, 26f), 60f);
            Foto(CarpetaCapturas + prefijo + "-armeria-diagonal.png", new Vector3(-16f, 2.0f, 17f), new Vector3(-25f, 3f, 27f), 60f);
        }

        public static void AntesArmeria() { EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single); FotosArmeria("antes"); }

        static string Path(Transform t) { string s = t.name; while (t.parent) { t = t.parent; s = t.name + "/" + s; } return s; }
    }
}

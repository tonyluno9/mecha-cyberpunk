using System.IO;
using System.Linq;
using MechaCyberpunk.Mundo.Ambiente;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-854 (playtest 6-nov tarde): el espejo del cuarto refleja de verdad. Pone el espejo del kit (H03) contra la pared sur del cuarto, donde
    /// estaba el vidrio plano del EspejoJugador, y encima de su vidrio un quad con <see cref="EspejoReflejo"/> y el material de reflejo: el jugador
    /// camina hasta el espejo y ve SU personaje. El EspejoJugador (que con E abría la vista previa de apariencia, el "muñeco") se queda en la escena
    /// pero apagado, y su vidrio plano también. Idempotente (rehace su raíz S2t_Espejo).
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2tEspejo.Colocar
    /// </summary>
    public static class ColocarS2tEspejo
    {
        public const string NombreRaiz = "S2t_Espejo";
        public const string RutaEspejoViejo = "Barrio/Manzana_4/Lugar_Casa/Prologo/EspejoJugador";
        public const string RutaMaterial = "Assets/_Proyecto/Mundo/Materiales/EspejoReflejo.mat";
        public const string ShaderEspejo = "MechaCyberpunk/Mundo/EspejoReflejo";
        private const string RutaH03 = "Assets/Arte/TallerCasa/Prefabs/H03_Espejo.prefab";
        /// <summary>Pared sur del cuarto (z) y centro del espejo en ella (x); el cuarto queda hacia +Z.</summary>
        public const float ParedZ = -75.5f;
        public const float CentroX = -63.2f;
        private const float Piso = 0.225f;
        /// <summary>Alto del espejo del kit (m) y la parte de su frente que es vidrio (fracción de ancho y alto, centro en alto).</summary>
        public const float AltoEspejo = 1.75f;
        public static float AnchoVidrio = 0.74f, AltoVidrio = 0.8f, CentroVidrio = 0.5f;
        private const float DelanteDelMarco = 0.006f;

        [MenuItem("Mecha Cyberpunk/S2t/3 Espejo que refleja")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Object.DestroyImmediate(GameObject.Find(NombreRaiz));
            Transform raiz = new GameObject(NombreRaiz).transform;

            Transform viejo = ColocarS2.Buscar(RutaEspejoViejo);
            if (viejo != null)
            {
                foreach (MonoBehaviour m in viejo.GetComponents<MonoBehaviour>().Where(m => m != null && m.GetType().Name == "EspejoJugador"))
                {
                    m.enabled = false;
                }
                Transform superficie = viejo.Find("Superficie");
                if (superficie != null)
                {
                    superficie.GetComponent<Renderer>().enabled = false;
                }
            }

            // El espejo del kit, de frente al cuarto (+Z) y con la espalda en la pared.
            GameObject h03 = ColocarS2Interiores.Modelo(raiz, RutaH03, new Vector3(CentroX, 0f, ParedZ + 0.3f), 0f, Piso, 1f, sinCollider: true);
            if (h03 == null)
            {
                ColocarS2.Informe.AppendLine("FALTA " + RutaH03);
                ColocarS2.Guardar("s2t-espejo");
                return;
            }
            h03.name = "H03_Espejo";
            // Que el lado delgado quede a lo largo de Z (contra la pared).
            Bounds b = ArteT250.Limites(h03);
            if (b.size.x < b.size.z)
            {
                h03.transform.rotation = Quaternion.Euler(0f, 90f + GiroFrente, 0f) * h03.transform.rotation;
            }
            else if (!Mathf.Approximately(GiroFrente, 0f))
            {
                h03.transform.rotation = Quaternion.Euler(0f, GiroFrente, 0f) * h03.transform.rotation;
            }
            b = ArteT250.Limites(h03);
            h03.transform.localScale *= AltoEspejo / Mathf.Max(0.01f, b.size.y);
            b = ArteT250.Limites(h03);
            h03.transform.position += new Vector3(CentroX - b.center.x, Piso - b.min.y, ParedZ + 0.01f - b.min.z);
            b = ArteT250.Limites(h03);

            GameObject vidrio = GameObject.CreatePrimitive(PrimitiveType.Quad);
            vidrio.name = "Vidrio_Reflejo";
            Object.DestroyImmediate(vidrio.GetComponent<Collider>());
            vidrio.transform.SetParent(raiz, false);
            // El quad se ve por su -Z: su -Z hacia el cuarto (+Z del mundo).
            vidrio.transform.SetPositionAndRotation(new Vector3(b.center.x, b.min.y + b.size.y * CentroVidrio, b.max.z + DelanteDelMarco), Quaternion.LookRotation(Vector3.back));
            vidrio.transform.localScale = new Vector3(b.size.x * AnchoVidrio, b.size.y * AltoVidrio, 1f);
            Renderer rv = vidrio.GetComponent<Renderer>();
            rv.sharedMaterial = Material();
            rv.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            vidrio.AddComponent<EspejoReflejo>();
            ColocarS2.Informe.AppendLine("espejo H03 " + b.size.ToString("F2") + " en " + b.center.ToString("F2") + "; vidrio " + vidrio.transform.localScale.ToString("F2"));
            ColocarS2.Guardar("s2t-espejo");
        }

        /// <summary>Giro extra (°) si el H03 queda de espaldas: se ajusta con la foto.</summary>
        public static float GiroFrente = 0f;

        private static Material Material()
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial);
            if (m == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(RutaMaterial));
                m = new Material(Shader.Find(ShaderEspejo)) { name = "EspejoReflejo" };
                AssetDatabase.CreateAsset(m, RutaMaterial);
            }
            return m;
        }

        /// <summary>Fotos del espejo con el jugador parado enfrente, de noche: docs/capturas/s2t-espejo-frente.png y s2t-espejo-lado.png.</summary>
        [MenuItem("Mecha Cyberpunk/S2t/Fotos del espejo")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            EspejoReflejo espejo = Object.FindFirstObjectByType<EspejoReflejo>();
            GameObject jugador = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab"));
            jugador.transform.SetPositionAndRotation(new Vector3(CentroX - 0.15f, Piso, ParedZ + 1.3f), Quaternion.LookRotation(Vector3.forward)); // su modelo mira a -Z del prefab: así queda de frente al espejo
            // Solo para la foto: una luz de relleno (el cuarto de noche es muy oscuro para revisar el reflejo).
            Light relleno = new GameObject("RellenoFoto").AddComponent<Light>();
            relleno.type = LightType.Point;
            relleno.range = 6f;
            relleno.intensity = 6f;
            relleno.transform.position = new Vector3(CentroX + 0.6f, 2.2f, ParedZ + 2.2f);
            foreach (Camera c in jugador.GetComponentsInChildren<Camera>(true))
            {
                c.gameObject.SetActive(false);
            }
            var tomas = new[]
            {
                ("espejo-frente", new Vector3(CentroX + 0.75f, 1.6f, ParedZ + 2.6f), new Vector3(CentroX, 1.2f, ParedZ)),
                ("espejo-lado", new Vector3(CentroX + 1.0f, 1.6f, ParedZ + 1.9f), new Vector3(CentroX - 0.1f, 1.2f, ParedZ)),
            };
            // Una foto de prueba primero: deja puesta la noche y las luces antes de pintar el reflejo.
            ColocarS2t.FotoHora(22f, tomas[0].Item2, tomas[0].Item3, "Logs/s2t-espejo-previa.png", 60f);
            foreach (var (nombre, desde, hacia) in tomas)
            {
                GameObject ojo = new GameObject("OjoFoto");
                Camera cam = ojo.AddComponent<Camera>();
                cam.enabled = false;
                ojo.transform.SetPositionAndRotation(desde, Quaternion.LookRotation(hacia - desde));
                if (espejo != null && espejo.Actualizar(cam))
                {
                    espejo.Camara.Render();
                }
                Object.DestroyImmediate(ojo);
                ColocarS2t.FotoHora(22f, desde, hacia, ColocarS2t.CarpetaCapturas + "s2t-" + nombre + ".png", 60f);
            }
            espejo?.Soltar();
            Object.DestroyImmediate(relleno.gameObject);
            Object.DestroyImmediate(jugador);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ColocarS2.RutaEscena, UnityEditor.SceneManagement.OpenSceneMode.Single);
        }
    }
}

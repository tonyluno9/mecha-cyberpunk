using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1106: los 4 visuales nuevos (E10, E11, E12, E14) puestos sobre un PandilleroAPie caminan con los clips de PJ01 y sin hundir los pies.
    /// Con T1106_CAPTURAS=1 escribe docs/capturas/t-1106/ (cada uno de frente y de perfil, parado y caminando).
    /// </summary>
    public class EnemigosNuevosAnimT1106Pruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaPandillero = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        private const string RutaDatos = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Viboras.asset";
        private const float MargenPies = 0.12f;
        private const float VelocidadCaminar = 1.3f;

        private static readonly string[] Nombres = { "E10_PandilleroSnakes", "E11_JefeSnakes", "E12_GuardiaPesadoNova", "E14_JefeFinalNova" };

        private GameObject piso;
        private GameObject jugador;
        private GameObject objeto;
        private int frameRateAnterior;
        private const int FrameRatePrueba = 60;

        private sealed class CaminaAdelante : MonoBehaviour, IConductaAPie
        {
            public bool Controla => true;
            public void Actualizar(PandilleroAPie p, float dt) => p.MoverPlano(transform.forward * VelocidadCaminar);
        }

        public override void Setup()
        {
            base.Setup();
            // En batchmode los cuadros salen a miles por segundo y el paso de cada uno queda bajo el minMoveDistance del CharacterController: se fija a 60 cuadros.
            frameRateAnterior = Application.targetFrameRate;
            Application.targetFrameRate = FrameRatePrueba;
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, -60f), Quaternion.identity);
            jugador.GetComponent<EscudoJugador>()?.Iniciar(0f, 0f, 0f);
        }

        public override void TearDown()
        {
            Object.DestroyImmediate(objeto);
            Object.DestroyImmediate(jugador);
            Object.DestroyImmediate(piso);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            Application.targetFrameRate = frameRateAnterior;
            base.TearDown();
        }

        // El pandillero con el visual de la prueba en lugar del suyo.
        private PandilleroAPie CrearConVisual(string nombre)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandillero);
            prefab.SetActive(false);
            objeto = Object.Instantiate(prefab, new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            prefab.SetActive(true);
            SerializedObject so = new SerializedObject(objeto.GetComponent<PandilleroAPie>());
            so.FindProperty("datos").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MechaCyberpunk.IA.Datos.DatosPandilleroAPie>(RutaDatos);
            so.ApplyModifiedPropertiesWithoutUndo();
            Transform raizViejo = null;
            foreach (VisualPersonaje v in objeto.GetComponentsInChildren<VisualPersonaje>(true))
            {
                raizViejo = v.transform;
            }
            Vector3 pos = raizViejo != null ? raizViejo.localPosition : Vector3.zero;
            Quaternion rot = raizViejo != null ? raizViejo.localRotation : Quaternion.identity;
            if (raizViejo != null)
            {
                Object.DestroyImmediate(raizViejo.gameObject);
            }
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_" + nombre + ".prefab"), objeto.transform);
            visual.transform.localPosition = pos;
            visual.transform.localRotation = rot;
            objeto.AddComponent<CaminaAdelante>();
            objeto.SetActive(true);
            return objeto.GetComponent<PandilleroAPie>();
        }

        private static IEnumerator Esperar(float s)
        {
            float fin = Time.time + s;
            while (Time.time < fin) yield return null;
        }

        // Punto más bajo de la malla animada (m sobre el piso, que está en y = 0).
        public static float PuntoMasBajo(GameObject raiz)
        {
            float min = float.MaxValue;
            Mesh malla = new Mesh();
            foreach (SkinnedMeshRenderer r in raiz.GetComponentsInChildren<SkinnedMeshRenderer>(false))
            {
                if (!r.enabled || r.sharedMesh == null) continue;
                r.BakeMesh(malla, true);
                foreach (Vector3 v in malla.vertices)
                {
                    min = Mathf.Min(min, r.transform.TransformPoint(v).y);
                }
            }
            Object.DestroyImmediate(malla);
            return min;
        }

        private static bool ClipActual(Animator a, string prefijo)
        {
            foreach (AnimatorClipInfo c in a.GetCurrentAnimatorClipInfo(0))
            {
                if (c.clip != null && c.clip.name.StartsWith(prefijo)) return true;
            }
            foreach (AnimatorClipInfo c in a.GetNextAnimatorClipInfo(0))
            {
                if (c.clip != null && c.clip.name.StartsWith(prefijo)) return true;
            }
            return false;
        }

        [UnityTest]
        public IEnumerator CadaVisualNuevo_CaminaConClipsDePJ01_YNoHundeLosPies()
        {
            foreach (string nombre in Nombres)
            {
                Object.DestroyImmediate(objeto);
                PandilleroAPie p = CrearConVisual(nombre);
                yield return Esperar(1.5f);
                Vector3 inicio = objeto.transform.position;
                yield return Esperar(1.5f);
                VisualPersonaje visual = p.GetComponentInChildren<VisualPersonaje>(true);
                Assert.IsNotNull(visual, nombre + ": visual");
                Animator a = visual.Animador;
                Assert.IsNotNull(a, nombre + ": animador");
                Assert.IsTrue(a.avatar != null && a.avatar.isHuman, nombre + ": avatar humanoide");
                Assert.Greater(Vector3.Distance(inicio, objeto.transform.position), 1.0f, nombre + ": camina");
                Assert.Greater(a.GetFloat("Velocidad"), 0.3f, nombre + ": Velocidad del Animator > 0");
                Assert.IsTrue(a.GetCurrentAnimatorStateInfo(0).IsName("Caminar") || a.GetCurrentAnimatorStateInfo(0).IsName("Correr")
                    || a.GetNextAnimatorStateInfo(0).IsName("Caminar"), nombre + ": estado de locomoción");
                Assert.IsTrue(ClipActual(a, "PJ01_Caminar") || ClipActual(a, "PJ01_Correr"), nombre + ": clip de PJ01 de locomoción");
                float bajo = PuntoMasBajo(objeto);
                Assert.AreEqual(0f, bajo, MargenPies, nombre + ": pies en el piso (punto más bajo " + bajo.ToString("0.000") + " m)");
            }
        }

        [UnityTest]
        public IEnumerator Capturas_DeLosCuatro()
        {
            if (System.Environment.GetEnvironmentVariable("T1106_CAPTURAS") != "1")
            {
                Assert.Ignore("Solo escribe docs/capturas/t-1106/ con T1106_CAPTURAS=1");
            }
            System.IO.Directory.CreateDirectory("docs/capturas/t-1106");
            GameObject luz = new GameObject("LuzTemporal");
            Light l = luz.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.6f;
            luz.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            Object.DestroyImmediate(jugador);
            jugador = null;
            piso.GetComponent<Renderer>().sharedMaterial.color = new Color(0.35f, 0.35f, 0.38f);
            foreach (string nombre in Nombres)
            {
                foreach (bool caminando in new[] { false, true })
                {
                    Object.DestroyImmediate(objeto);
                    objeto = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_" + nombre + ".prefab"));
                    objeto.transform.SetPositionAndRotation(new Vector3(0f, 0.02f, 0f), Quaternion.identity);
                    VisualPersonaje v = objeto.GetComponent<VisualPersonaje>();
                    v.LeerDelPadre = false;
                    v.Velocidad = caminando ? VelocidadCaminar : 0f;
                    yield return Esperar(caminando ? 0.9f : 0.5f);
                    float alto = CreadorAlto(objeto);
                    yield return Foto(new Vector3(0f, alto * 0.5f, 4.2f), new Vector3(0f, alto * 0.5f, 0f), nombre + (caminando ? "_caminando" : "_parado") + "_frente");
                    yield return Foto(new Vector3(4.2f, alto * 0.5f, 0f), new Vector3(0f, alto * 0.5f, 0f), nombre + (caminando ? "_caminando" : "_parado") + "_perfil");
                }
            }
            Object.DestroyImmediate(luz);
        }

        private static float CreadorAlto(GameObject raiz)
        {
            float max = 0f;
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>())
            {
                max = Mathf.Max(max, r.bounds.max.y);
            }
            return max;
        }

        private static IEnumerator Foto(Vector3 desde, Vector3 hacia, string nombre)
        {
            GameObject go = new GameObject("CamaraFoto");
            Camera cam = go.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 100f;
            cam.fieldOfView = 40f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.65f, 0.75f);
            go.transform.position = desde;
            go.transform.LookAt(hacia);
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            yield return null;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes("docs/capturas/t-1106/" + nombre + ".png", tex.EncodeToPNG());
            cam.targetTexture = null;
            Object.Destroy(tex);
            Object.Destroy(rt);
            Object.Destroy(go);
        }
    }
}

using System.Collections;
using System.IO;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1110: en Ciudad_Prologo el recinto del jefe final de Nova está al fondo de la calle de Nova, con el jefe y los guardias de T-1109 en su lugar, sobre el
    /// piso, la barrera abierta y apagada al inicio, fuera de los carriles de tráfico, de las puertas y de la ruta de la demo, y sin estorbar el eje de la calle.
    /// Con T1110_CAPTURAS=1 escribe docs/capturas/t-1110/recinto.png.
    /// </summary>
    public class JefeFinalCiudadT1110Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string CarpetaFotos = "docs/capturas/t-1110";
        private const float DistanciaMinimaRuta = 30f;
        private const float DistanciaMinimaCarriles = 6.4f;
        private const float DistanciaMinimaPuertas = 7f;
        private const float ZCalle = -43f;

        private static readonly Vector3[] Ruta =
        {
            new Vector3(-59f, 0f, -77f), new Vector3(-44.3f, 0f, -77f), new Vector3(-100.3f, 0f, -13.4f), new Vector3(-44.3f, 0f, -77f),
            new Vector3(-36f, 0f, 85f), new Vector3(74.5f, 0f, 47f)
        };

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Descargar()
        {
            Scene ciudad = SceneManager.GetSceneByPath(RutaEscena);
            if (ciudad.IsValid() && ciudad.isLoaded)
            {
                Scene vacia = SceneManager.CreateScene("VaciaT1110");
                SceneManager.SetActiveScene(vacia);
                yield return SceneManager.UnloadSceneAsync(ciudad);
            }
        }

        private static float DistanciaASegmento(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector2 pp = new Vector2(p.x, p.z);
            Vector2 aa = new Vector2(a.x, a.z);
            Vector2 ab = new Vector2(b.x, b.z) - aa;
            float t = ab.sqrMagnitude < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(pp - aa, ab) / ab.sqrMagnitude);
            return Vector2.Distance(pp, aa + ab * t);
        }

        private static float DistanciaALaRuta(Vector3 p)
        {
            float menor = float.MaxValue;
            for (int i = 0; i < Ruta.Length - 1; i++) menor = Mathf.Min(menor, DistanciaASegmento(p, Ruta[i], Ruta[i + 1]));
            return menor;
        }

        private static RecintoJefe Recinto()
        {
            RecintoJefe[] recintos = Object.FindObjectsByType<RecintoJefe>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, recintos.Length, "un recinto del jefe final (T1110_JefeFinalNova)");
            return recintos[0];
        }

        [UnityTest]
        public IEnumerator Ciudad_ElJefeYLosGuardiasEstanEnSuLugarSobreElPiso()
        {
            RecintoJefe recinto = Recinto();
            PandilleroAPie jefe = recinto.Jefe.GetComponent<PandilleroAPie>();
            Assert.AreEqual(600f, jefe.Maxima, 0.001f, "vida 600");
            Assert.AreEqual(2.8f, jefe.Datos.Velocidad, 0.001f, "velocidad 2.8");
            Assert.IsNotNull(recinto.Guardias, "el recinto conoce a los guardias de T-1109");
            Assert.AreEqual(2, recinto.Guardias.Guardias.Length);
            Physics.SyncTransforms();
            PandilleroAPie[] todos = recinto.Guardias.Guardias.Select(g => g.GetComponent<PandilleroAPie>()).Concat(new[] { jefe }).ToArray();
            foreach (PandilleroAPie p in todos)
            {
                Assert.IsTrue(p.gameObject.activeInHierarchy, p.name + " activo");
                Vector3 pos = p.transform.position;
                Assert.IsTrue(Physics.Raycast(pos + Vector3.up * 1.5f, Vector3.down, out RaycastHit suelo, 4f, ~0, QueryTriggerInteraction.Ignore), p.name + " tiene piso");
                Assert.Less(Mathf.Abs(pos.y - suelo.point.y), 0.5f, p.name + " no flota");
                Collider[] dentro = Physics.OverlapCapsule(pos + Vector3.up * 0.5f, pos + Vector3.up * 1.5f, 0.25f, ~0, QueryTriggerInteraction.Ignore)
                    .Where(c => c.GetComponentInParent<PandilleroAPie>() == null).ToArray();
                Assert.IsEmpty(dentro, p.name + " no está dentro de una pared: " + string.Join(", ", dentro.Select(c => c.name)));
            }
            for (int i = 0; i < todos.Length; i++)
            {
                for (int j = i + 1; j < todos.Length; j++)
                {
                    Vector3 d = todos[i].transform.position - todos[j].transform.position;
                    d.y = 0f;
                    Assert.Greater(d.magnitude, 1.5f, todos[i].name + " y " + todos[j].name + " no están encimados");
                }
            }
            Assert.GreaterOrEqual(DistanciaALaRuta(jefe.transform.position), DistanciaMinimaRuta, "el jefe a 30 m o más de la ruta de la demo");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ciudad_LaBarreraEmpiezaAbiertaYApagada()
        {
            RecintoJefe recinto = Recinto();
            Assert.IsFalse(recinto.Cerrado);
            Assert.IsFalse(recinto.Barrera.activeSelf, "barrera abierta al inicio");
            Assert.IsFalse(recinto.Barrera.activeInHierarchy);
            Assert.IsFalse(recinto.Jefe.Activo, "el jefe espera");
            Assert.IsTrue(recinto.GetComponent<BoxCollider>().isTrigger, "la zona es un trigger");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ciudad_NadaDelRecintoEstorbaElEjeDeLaCalleDeNova()
        {
            Recinto();
            Physics.SyncTransforms();
            // La misma cápsula que pasa NovaS2cPruebas por la calle: ni el jefe, ni los guardias, ni la barrera la tocan.
            float piso = 0.2f;
            Vector3 abajo = new Vector3(55f, piso + 0.65f, ZCalle);
            RaycastHit[] golpes = Physics.CapsuleCastAll(abajo, abajo + Vector3.up, 0.4f, Vector3.right, 130f - 55f, ~0, QueryTriggerInteraction.Ignore);
            string[] mios = golpes.Where(g => g.collider.GetComponentInParent<PandilleroAPie>() != null || g.collider.GetComponentInParent<RecintoJefe>() != null
                || g.collider.GetComponentInParent<PuestoGuardiasPesados>() != null).Select(g => g.collider.name).ToArray();
            Assert.IsEmpty(mios, "ningún collider del recinto en el eje de la calle");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ciudad_ElJefeYLosGuardiasEstanLejosDeCarrilesYPuertas()
        {
            RecintoJefe recinto = Recinto();
            Transform manzana = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name.StartsWith("Manzana_"));
            Assert.IsNotNull(manzana);
            GrafoCalles grafo = GestorPersecucion.ArmarCallesDe(manzana.parent);
            TraficoCalle.AlargarHaciaLasAfueras(grafo);
            Vector3[] puertas = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<IPuerta>().Select(p => p.Posicion).ToArray();
            PandilleroAPie[] todos = recinto.Guardias.Guardias.Select(g => g.GetComponent<PandilleroAPie>()).Concat(new[] { recinto.Jefe.GetComponent<PandilleroAPie>() }).ToArray();
            foreach (PandilleroAPie p in todos)
            {
                float menor = float.MaxValue;
                for (int i = 0; i < grafo.Cantidad; i++)
                {
                    foreach (int j in grafo.Vecinos(i))
                    {
                        if (j > i) menor = Mathf.Min(menor, DistanciaASegmento(p.transform.position, grafo.Nodo(i), grafo.Nodo(j)));
                    }
                }
                Assert.GreaterOrEqual(menor, DistanciaMinimaCarriles, p.name + " a 4 m o más del carril más cercano");
                foreach (Vector3 puerta in puertas)
                {
                    Vector3 d = puerta - p.transform.position;
                    d.y = 0f;
                    Assert.GreaterOrEqual(d.magnitude, DistanciaMinimaPuertas - 3f, p.name + " no tapa una puerta de la ciudad");
                }
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Capturas_ElRecintoConElJefeYLosGuardias()
        {
            if (System.Environment.GetEnvironmentVariable("T1110_CAPTURAS") != "1")
            {
                Assert.Ignore("Solo escribe docs/capturas/t-1110/ con T1110_CAPTURAS=1");
            }
            Directory.CreateDirectory(CarpetaFotos);
            RecintoJefe recinto = Recinto();
            Vector3 jefe = recinto.Jefe.transform.position;
            GameObject go = new GameObject("CamaraFoto");
            foreach (Camera otra in Camera.allCameras) otra.enabled = false; // Camera.main es la que mira el texto de los gritos
            Camera camara = go.AddComponent<Camera>();
            go.tag = "MainCamera";
            camara.farClipPlane = 400f;
            camara.fieldOfView = 65f;
            go.transform.position = new Vector3(98f, 2.6f, -41f);
            go.transform.LookAt(new Vector3(116f, 1.8f, -45f));
            yield return null;
            yield return null;
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            foto.Apply();
            camara.targetTexture = null;
            RenderTexture.active = null;
            File.WriteAllBytes(CarpetaFotos + "/recinto.png", foto.EncodeToPNG());
            Assert.Greater(jefe.x, 100f, "el jefe está en el fondo de la calle");
            Object.Destroy(foto);
            Object.Destroy(rt);
            Object.Destroy(go);
        }
    }
}

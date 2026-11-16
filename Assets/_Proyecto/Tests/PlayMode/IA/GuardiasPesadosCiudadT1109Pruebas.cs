using System.Collections;
using System.IO;
using System.Linq;
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
    /// T-1109: en Ciudad_Prologo hay 2 guardias pesados a los lados de la entrada de la torre principal de Nova, en el piso, sin encimarse, fuera de los carriles
    /// de tráfico y sin tapar el paso. Con T1109_CAPTURAS=1 escribe docs/capturas/t-1109/guardias-en-la-torre.png.
    /// </summary>
    public class GuardiasPesadosCiudadT1109Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string CarpetaFotos = "docs/capturas/t-1109";
        private const float DistanciaMinimaCarriles = 6.4f;
        private const float RadioJugador = 0.4f;
        private const float XTorre = 110f;
        private const float ZCalle = -43f;

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
                Scene vacia = SceneManager.CreateScene("VaciaT1109");
                SceneManager.SetActiveScene(vacia);
                yield return SceneManager.UnloadSceneAsync(ciudad);
            }
        }

        private static PuestoGuardiasPesados Puesto()
        {
            PuestoGuardiasPesados[] puestos = Object.FindObjectsByType<PuestoGuardiasPesados>(FindObjectsSortMode.None);
            Assert.AreEqual(1, puestos.Length, "un puesto de guardias pesados (T1109_GuardiasPesados)");
            return puestos[0];
        }

        private static float DistanciaASegmento(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector2 pp = new Vector2(p.x, p.z);
            Vector2 aa = new Vector2(a.x, a.z);
            Vector2 ab = new Vector2(b.x, b.z) - aa;
            float t = ab.sqrMagnitude < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(pp - aa, ab) / ab.sqrMagnitude);
            return Vector2.Distance(pp, aa + ab * t);
        }

        private static IEnumerator Esperar(float s)
        {
            float fin = Time.time + s;
            while (Time.time < fin) yield return null;
        }

        [UnityTest]
        public IEnumerator Ciudad_ExistenDosGuardiasEnPisoSinEncimarse()
        {
            PuestoGuardiasPesados puesto = Puesto();
            Assert.AreEqual(2, puesto.Guardias.Length);
            Physics.SyncTransforms();
            foreach (ConductaGuardiaPesado g in puesto.Guardias)
            {
                Assert.IsNotNull(g, "guardia colocado");
                Assert.IsTrue(g.gameObject.activeInHierarchy);
                PandilleroAPie p = g.GetComponent<PandilleroAPie>();
                Assert.AreEqual(220f, p.Maxima, 0.001f);
                Vector3 pos = g.transform.position;
                Assert.IsTrue(Physics.Raycast(pos + Vector3.up * 1.5f, Vector3.down, out RaycastHit suelo, 4f, ~0, QueryTriggerInteraction.Ignore), g.name + " tiene piso");
                Assert.Less(Mathf.Abs(pos.y - suelo.point.y), 0.5f, g.name + " no flota");
                Collider[] dentro = Physics.OverlapCapsule(pos + Vector3.up * 0.5f, pos + Vector3.up * 1.5f, 0.25f, ~0, QueryTriggerInteraction.Ignore)
                    .Where(c => c.GetComponentInParent<PandilleroAPie>() == null).ToArray();
                Assert.IsEmpty(dentro, g.name + " no está dentro de una pared: " + string.Join(", ", dentro.Select(c => c.name)));
            }
            Vector3 d = puesto.Guardias[0].transform.position - puesto.Guardias[1].transform.position;
            d.y = 0f;
            Assert.Greater(d.magnitude, 6f, "no están encimados: dejan la entrada en medio");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ciudad_NoBloqueanLaEntradaNiLaCalle()
        {
            PuestoGuardiasPesados puesto = Puesto();
            Physics.SyncTransforms();
            // La cápsula del jugador cruza del centro de la calle hasta la fachada de la torre, entre los dos guardias.
            Vector3 abajo = new Vector3(XTorre, 0.55f, ZCalle);
            RaycastHit[] golpes = Physics.CapsuleCastAll(abajo, abajo + Vector3.up * 1.1f, RadioJugador, Vector3.back, 6f, ~0, QueryTriggerInteraction.Ignore);
            Assert.IsEmpty(golpes.Where(h => h.collider.GetComponentInParent<PandilleroAPie>() != null).Select(h => h.collider.name).ToArray(), "ningún guardia en la ruta a la entrada");
            // Y ninguno queda en la línea de la calle de Nova (la prueba de calle libre pasa una cápsula por z = -43).
            foreach (ConductaGuardiaPesado g in puesto.Guardias)
            {
                Assert.Greater(Mathf.Abs(g.transform.position.z - ZCalle), 3f, g.name + " fuera del eje de la calle");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ciudad_LosGuardiasEstanAMasDeCuatroMetrosDeCualquierCarril()
        {
            Transform manzana = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name.StartsWith("Manzana_"));
            Assert.IsNotNull(manzana);
            GrafoCalles grafo = GestorPersecucion.ArmarCallesDe(manzana.parent);
            TraficoCalle.AlargarHaciaLasAfueras(grafo);
            foreach (ConductaGuardiaPesado g in Puesto().Guardias)
            {
                float menor = float.MaxValue;
                for (int i = 0; i < grafo.Cantidad; i++)
                {
                    foreach (int j in grafo.Vecinos(i))
                    {
                        if (j > i) menor = Mathf.Min(menor, DistanciaASegmento(g.transform.position, grafo.Nodo(i), grafo.Nodo(j)));
                    }
                }
                Assert.GreaterOrEqual(menor, DistanciaMinimaCarriles, g.name + " a 4 m o más del carril más cercano");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ciudad_SeQuedanDeGuardiaMirandoALaCalle()
        {
            foreach (ConductaGuardiaPesado g in Puesto().Guardias)
            {
                Assert.IsFalse(g.Provocado);
                Assert.AreEqual(ReglaGuardiaPesado.Fase.Guardia, g.Regla.Actual);
                Assert.Greater(g.transform.forward.z, 0.9f, g.name + " mira hacia la calle (norte)");
            }
            yield return Esperar(1f);
            foreach (ConductaGuardiaPesado g in Puesto().Guardias)
            {
                Assert.Less(Vector3.Distance(g.transform.position, g.Puesto), 0.3f, g.name + " sigue en su puesto");
            }
        }

        [UnityTest]
        public IEnumerator Capturas_GuardiasEnLaEntradaDeLaTorre()
        {
            if (System.Environment.GetEnvironmentVariable("T1109_CAPTURAS") != "1")
            {
                Assert.Ignore("Solo escribe docs/capturas/t-1109/ con T1109_CAPTURAS=1");
            }
            Directory.CreateDirectory(CarpetaFotos);
            Vector3 centro = new Vector3(XTorre, 1.4f, -49.8f);
            GameObject go = new GameObject("CamaraFoto");
            foreach (Camera otra in Camera.allCameras) otra.enabled = false;
            Camera camara = go.AddComponent<Camera>();
            go.tag = "MainCamera";
            camara.farClipPlane = 400f;
            camara.fieldOfView = 60f;
            go.transform.position = new Vector3(XTorre, 2.3f, -37f);
            go.transform.LookAt(centro + Vector3.up * 1.5f);
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
            File.WriteAllBytes(CarpetaFotos + "/guardias-en-la-torre.png", foto.EncodeToPNG());
            Object.Destroy(foto);
            Object.Destroy(rt);
            Object.Destroy(go);
        }
    }
}

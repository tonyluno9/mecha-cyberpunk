using System.Collections;
using System.IO;
using System.Linq;
using MechaCyberpunk.IA.APie;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1108: en Ciudad_Prologo el jefe SNAKES existe en un lote de Neón Alto con 2 escoltas, sobre el piso, sin nadie encimado ni dentro de paredes,
    /// a 30 m o más de la ruta de la demo y lejos de las riñas. Con T1108_CAPTURAS=1 escribe docs/capturas/t-1108/jefe-en-su-lugar.png.
    /// </summary>
    public class JefeSnakesCiudadT1108Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string CarpetaFotos = "docs/capturas/t-1108";
        private const float DistanciaMinimaRuta = 30f;
        private const float TolerSuelo = 0.5f;
        private const float SeparacionMinima = 1f;
        private const float LimiteNeonAltoX = 47f;

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
                Scene vacia = SceneManager.CreateScene("VaciaT1108");
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

        [UnityTest]
        public IEnumerator Ciudad_ElJefeSnakesExisteConDosEscoltasSobreSuelo()
        {
            ConductaJefeSnakes[] jefes = Object.FindObjectsByType<ConductaJefeSnakes>(FindObjectsSortMode.None);
            Assert.AreEqual(1, jefes.Length, "Un jefe SNAKES en T1108_JefeSnakes");
            ConductaJefeSnakes jefe = jefes[0];
            PandilleroAPie pj = jefe.GetComponent<PandilleroAPie>();
            Assert.AreEqual(260f, pj.Maxima, 0.001f, "vida 260");
            Assert.AreEqual(4.2f, pj.Datos.Velocidad, 0.001f, "velocidad 4.2");
            Transform puesto = jefe.transform.parent;
            PandillaAPie escoltas = puesto.Find("Escoltas").GetComponent<PandillaAPie>();
            Assert.AreEqual(2, escoltas.Pandilleros.Length, "2 escoltas");
            Assert.AreEqual(2, escoltas.Presentes, "los 2 escoltas están en pie");
            Assert.IsNotNull(jefe.Refuerzos, "el jefe tiene su grupo de refuerzos");
            Assert.IsFalse(jefe.Refuerzos.gameObject.activeSelf, "los refuerzos esperan apagados");
            Assert.AreEqual(2, jefe.Refuerzos.GetComponentsInChildren<PandilleroAPie>(true).Length, "2 SNAKES de refuerzo");
            Assert.GreaterOrEqual(pj.transform.position.x, LimiteNeonAltoX, "está en Neón Alto");
            Physics.SyncTransforms();

            PandilleroAPie[] todos = escoltas.Pandilleros.Concat(new[] { pj }).ToArray();
            foreach (PandilleroAPie p in todos)
            {
                Vector3 pos = p.transform.position;
                Assert.IsTrue(Physics.Raycast(pos + Vector3.up * 1.5f, Vector3.down, out RaycastHit suelo, 4f, ~0, QueryTriggerInteraction.Ignore), p.name + " tiene piso debajo");
                Assert.Less(Mathf.Abs(pos.y - suelo.point.y), TolerSuelo, p.name + " no flota");
                Collider[] dentro = Physics.OverlapCapsule(pos + Vector3.up * 0.5f, pos + Vector3.up * 1.5f, 0.25f, ~0, QueryTriggerInteraction.Ignore)
                    .Where(c => c.GetComponentInParent<PandilleroAPie>() == null).ToArray();
                Assert.IsEmpty(dentro, p.name + " no está dentro de una pared: " + string.Join(", ", dentro.Select(c => c.name)));
                Assert.GreaterOrEqual(DistanciaALaRuta(pos), DistanciaMinimaRuta, p.name + " a 30 m o más de la ruta de la demo");
            }
            for (int i = 0; i < todos.Length; i++)
            {
                for (int j = i + 1; j < todos.Length; j++)
                {
                    Vector3 d = todos[i].transform.position - todos[j].transform.position;
                    d.y = 0f;
                    Assert.Greater(d.magnitude, SeparacionMinima, todos[i].name + " y " + todos[j].name + " no están encimados");
                }
            }
            foreach (RinaPandillas rina in Object.FindObjectsByType<RinaPandillas>(FindObjectsSortMode.None))
            {
                Assert.Greater(Vector3.Distance(rina.transform.position, pj.transform.position), 30f, "el jefe no está en el lugar de " + rina.name);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Capturas_JefeConSusEscoltasEnSuLugar()
        {
            if (System.Environment.GetEnvironmentVariable("T1108_CAPTURAS") != "1")
            {
                Assert.Ignore("Solo escribe docs/capturas/t-1108/ con T1108_CAPTURAS=1");
            }
            Directory.CreateDirectory(CarpetaFotos);
            ConductaJefeSnakes jefe = Object.FindObjectsByType<ConductaJefeSnakes>(FindObjectsSortMode.None)[0];
            Vector3 centro = jefe.transform.position;
            GameObject go = new GameObject("CamaraFoto");
            Camera camara = go.AddComponent<Camera>();
            camara.farClipPlane = 400f;
            Vector3 mira = centro + Vector3.up * 1.2f;
            bool puesta = false;
            foreach (float altura in new[] { 2.2f, 4f, 7f })
            {
                for (int a = 0; a < 360 && !puesta; a += 20)
                {
                    Vector3 pos = centro + Quaternion.Euler(0f, a, 0f) * Vector3.forward * 9f + Vector3.up * altura;
                    if (Physics.CheckSphere(pos, 0.5f, ~0, QueryTriggerInteraction.Ignore)
                        || Physics.Linecast(pos, mira, out RaycastHit h, ~0, QueryTriggerInteraction.Ignore) && h.collider.GetComponentInParent<PandilleroAPie>() == null)
                    {
                        continue;
                    }
                    go.transform.position = pos;
                    puesta = true;
                }
            }
            go.transform.LookAt(mira);
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
            File.WriteAllBytes(CarpetaFotos + "/jefe-en-su-lugar.png", foto.EncodeToPNG());
            Assert.IsTrue(puesta, "hubo una posición libre para la cámara");
            Object.Destroy(foto);
            Object.Destroy(rt);
            Object.Destroy(go);
        }
    }
}

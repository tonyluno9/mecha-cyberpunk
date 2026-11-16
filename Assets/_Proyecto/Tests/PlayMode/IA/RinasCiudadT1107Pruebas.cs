using System.Collections;
using System.IO;
using System.Linq;
using MechaCyberpunk.IA.APie;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1107: en Ciudad_Prologo hay 2 riñas SNAKES contra SKULLS (una en la frontera de Neón Alto con el tianguis y otra dentro de Neón Alto),
    /// sobre el piso, sin nadie dentro de paredes y a 30 m o más de la ruta de la demo. Con T1107_CAPTURAS=1 escribe docs/capturas/t-1107/.
    /// </summary>
    public class RinasCiudadT1107Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string CarpetaFotos = "docs/capturas/t-1107";
        private const float DistanciaMinimaRuta = 30f;
        private const float TolerSuelo = 0.5f;
        private const string RutaSnakes = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Snakes.prefab";

        // Casa, inventor, Doc, inventor, tianguis y hacker: los puntos que recorre RecorridoDemoPruebas, unidos en ese orden.
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
                Scene vacia = SceneManager.CreateScene("VaciaT1107");
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
            for (int i = 0; i < Ruta.Length - 1; i++)
            {
                menor = Mathf.Min(menor, DistanciaASegmento(p, Ruta[i], Ruta[i + 1]));
            }
            return menor;
        }

        [UnityTest]
        public IEnumerator Ciudad_TieneDosRinasConSeisPandillerosSobreSuelo()
        {
            RinaPandillas[] rinas = Object.FindObjectsByType<RinaPandillas>(FindObjectsSortMode.None).OrderBy(r => r.name).ToArray();
            Assert.AreEqual(2, rinas.Length, "Las dos riñas de T1107_Rinas");
            Physics.SyncTransforms();
            foreach (RinaPandillas r in rinas)
            {
                Assert.AreEqual(6, r.Miembros.Count, r.name + ": 3 SNAKES y 3 SKULLS");
                Assert.AreEqual(3, r.Miembros.Count(p => p.Pandilla == MechaCyberpunk.Core.IdPandilla.Viboras), r.name + ": 3 SNAKES");
                Assert.AreEqual(3, r.Miembros.Count(p => p.Pandilla == MechaCyberpunk.Core.IdPandilla.Calaveras), r.name + ": 3 SKULLS");
                foreach (PandilleroAPie p in r.Miembros)
                {
                    Vector3 pos = p.transform.position;
                    Assert.IsTrue(Physics.Raycast(pos + Vector3.up * 1.5f, Vector3.down, out RaycastHit suelo, 4f, ~0, QueryTriggerInteraction.Ignore),
                        p.name + " de " + r.name + " tiene piso debajo");
                    Assert.Less(Mathf.Abs(pos.y - suelo.point.y), TolerSuelo, p.name + " de " + r.name + " no flota");
                    Collider[] dentro = Physics.OverlapCapsule(pos + Vector3.up * 0.5f, pos + Vector3.up * 1.5f, 0.25f, ~0, QueryTriggerInteraction.Ignore)
                        .Where(c => c.GetComponentInParent<PandilleroAPie>() == null).ToArray();
                    Assert.IsEmpty(dentro, p.name + " de " + r.name + " no está dentro de una pared: " + string.Join(", ", dentro.Select(c => c.name)));
                    Assert.GreaterOrEqual(DistanciaALaRuta(pos), DistanciaMinimaRuta, p.name + " de " + r.name + " a 30 m o más de la ruta de la demo");
                }
                PandilleroAPie snake = r.Miembros.First(p => p.Pandilla == MechaCyberpunk.Core.IdPandilla.Viboras);
                Transform modelo = snake.transform.Find("Modelo");
                Assert.IsNotNull(modelo, "el SNAKE tiene su Modelo");
                Assert.IsTrue(modelo.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(r => r.sharedMaterial != null && r.sharedMaterial.name.StartsWith("E10_PandilleroSnakes")),
                    "el SNAKE usa el visual E10 (su material)");
            }
            Assert.Greater(Vector3.Distance(rinas[0].transform.position, rinas[1].transform.position), 30f, "las dos riñas están en lugares distintos");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ciudad_LasRinasEmpiezanPeleandoEntreSiSinElJugador()
        {
            RinaPandillas[] rinas = Object.FindObjectsByType<RinaPandillas>(FindObjectsSortMode.None);
            float antes = rinas.Sum(r => r.Miembros.Sum(p => p.Actual));
            float fin = Time.time + 12f;
            while (Time.time < fin) yield return null;
            Assert.IsTrue(rinas.All(r => r.EstadoActual == RinaPandillas.Estado.Rina), "el jugador lejos no las interrumpe");
            Assert.Less(rinas.Sum(r => r.Miembros.Sum(p => p.Actual)), antes, "se pegan entre ellos");
        }

        [UnityTest]
        public IEnumerator Capturas_RinaA8MetrosYA20Metros()
        {
            if (System.Environment.GetEnvironmentVariable("T1107_CAPTURAS") != "1")
            {
                Assert.Ignore("Solo escribe docs/capturas/t-1107/ con T1107_CAPTURAS=1");
            }
            Directory.CreateDirectory(CarpetaFotos);
            RinaPandillas[] rinas = Object.FindObjectsByType<RinaPandillas>(FindObjectsSortMode.None).OrderBy(r => r.name).ToArray();
            float fin = Time.time + 4f;
            while (Time.time < fin) yield return null;
            for (int i = 0; i < rinas.Length; i++)
            {
                Vector3 centro = rinas[i].Miembros.Aggregate(Vector3.zero, (s, p) => s + p.transform.position) / rinas[i].Miembros.Count;
                foreach (float d in new[] { 8f, 20f })
                {
                    yield return Foto(centro, d, CarpetaFotos + "/rina-" + (i + 1) + "-" + d + "m.png");
                }
            }
        }

        // La primera posición alrededor de la riña, a la distancia pedida, que no queda dentro de un edificio y la ve sin nada de por medio.
        private static IEnumerator Foto(Vector3 centro, float distancia, string ruta)
        {
            GameObject go = new GameObject("CamaraFoto");
            Camera camara = go.AddComponent<Camera>();
            camara.farClipPlane = 400f;
            camara.fieldOfView = 60f;
            Vector3 mira = centro + Vector3.up * 1.2f;
            bool puesta = false;
            foreach (float altura in new[] { 2.2f, 4f, 7f })
            {
                for (int a = 0; a < 360 && !puesta; a += 20)
                {
                    Vector3 pos = centro + Quaternion.Euler(0f, a, 0f) * Vector3.forward * distancia + Vector3.up * altura;
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
            File.WriteAllBytes(ruta, foto.EncodeToPNG());
            Debug.Log("[T-1107] captura " + ruta + (puesta ? "" : " (sin posición libre: cámara en el centro)"));
            Object.Destroy(foto);
            Object.Destroy(rt);
            Object.Destroy(go);
        }
    }
}

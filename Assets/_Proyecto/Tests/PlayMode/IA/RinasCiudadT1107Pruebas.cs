using System.Collections;
using System.IO;
using System.Linq;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Persecucion;
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
        private const float DistanciaMinimaCarriles = 6.4f; // carril a 2.4 m del eje de la calle + 4 m

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

        // Las calles por las que pasan los carros: el mismo grafo que arma el tráfico, con las calles de las afueras.
        private static GrafoCalles GrafoDelTrafico()
        {
            Transform manzana = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name.StartsWith("Manzana_"));
            Assert.IsNotNull(manzana, "la ciudad tiene manzanas");
            GrafoCalles grafo = GestorPersecucion.ArmarCallesDe(manzana.parent);
            Assert.IsNotNull(grafo);
            TraficoCalle.AlargarHaciaLasAfueras(grafo);
            return grafo;
        }

        [UnityTest]
        public IEnumerator Ciudad_NadieEstaEnUnCarrilDeTrafico_AlMenosCuatroMetrosDelCarril()
        {
            GrafoCalles grafo = GrafoDelTrafico();
            foreach (RinaPandillas r in Object.FindObjectsByType<RinaPandillas>(FindObjectsSortMode.None))
            {
                foreach (PandilleroAPie p in r.Miembros)
                {
                    float menor = float.MaxValue;
                    for (int i = 0; i < grafo.Cantidad; i++)
                    {
                        foreach (int j in grafo.Vecinos(i))
                        {
                            if (j > i) menor = Mathf.Min(menor, DistanciaASegmento(p.transform.position, grafo.Nodo(i), grafo.Nodo(j)));
                        }
                    }
                    Assert.GreaterOrEqual(menor, DistanciaMinimaCarriles, p.name + " de " + r.name + " a 4 m o más del carril más cercano");
                }
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ciudad_LosSnakesSeVenConSuTexturaE10_SinTinteDePandilla()
        {
            SkinnedMeshRenderer[] renderers = Object.FindObjectsByType<RinaPandillas>(FindObjectsSortMode.None)
                .SelectMany(r => r.Miembros.Where(m => m.Pandilla == MechaCyberpunk.Core.IdPandilla.Viboras))
                .SelectMany(p => p.transform.Find("Modelo").GetComponentsInChildren<SkinnedMeshRenderer>(true)).ToArray();
            Assert.Greater(renderers.Length, 0, "hay renderers de SNAKE");
            foreach (SkinnedMeshRenderer rend in renderers)
            {
                Assert.IsTrue(rend.sharedMaterial.name.StartsWith("E10_PandilleroSnakes"), "material E10");
                Assert.IsNotNull(rend.sharedMaterial.GetTexture("_BaseMap"), "el material E10 tiene su textura base");
            }
            // Tras 2 s de Play: nunca el verde de la pandilla y, entre un destello de golpe (rojo, 0.12 s) y otro, el color base del material.
            float fin = Time.time + 2f;
            while (Time.time < fin) yield return null;
            bool visto = false;
            fin = Time.time + 6f;
            while (Time.time < fin && !visto)
            {
                bool todosSinTinte = true;
                foreach (SkinnedMeshRenderer rend in renderers)
                {
                    MaterialPropertyBlock bloque = new MaterialPropertyBlock();
                    rend.GetPropertyBlock(bloque);
                    Color delMaterial = rend.sharedMaterial.GetColor("_BaseColor");
                    Color efectivo = bloque.isEmpty || !bloque.HasColor("_BaseColor") ? delMaterial : bloque.GetColor("_BaseColor");
                    Assert.IsFalse(Mathf.Abs(efectivo.r - 0.45f) < 0.02f && Mathf.Abs(efectivo.g - 1f) < 0.02f && Mathf.Abs(efectivo.b - 0.1f) < 0.02f, "tinte verde de Viboras sobre el SNAKE");
                    todosSinTinte &= Mathf.Abs(delMaterial.r - efectivo.r) < 0.01f && Mathf.Abs(delMaterial.g - efectivo.g) < 0.01f && Mathf.Abs(delMaterial.b - efectivo.b) < 0.01f;
                }
                visto = todosSinTinte;
                yield return null;
            }
            Assert.IsTrue(visto, "en algún cuadro todos los SNAKES se ven con el color base del material E10, sin tinte");
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
                    yield return Foto(centro, d, rinas[i].Miembros.Select(m => m.transform.position).ToArray(), CarpetaFotos + "/rina-" + (i + 1) + "-" + d + "m.png");
                }
            }
        }

        // La primera posición alrededor de la riña, a la distancia pedida, que no queda dentro de un edificio y la ve sin nada de por medio.
        private static IEnumerator Foto(Vector3 centro, float distancia, Vector3[] puntos, string ruta)
        {
            GameObject go = new GameObject("CamaraFoto");
            // Camera.main es la que mira el texto de los gritos: la de la captura lo es (las demás se apagan).
            foreach (Camera otra in Camera.allCameras) otra.enabled = false;
            Camera camara = go.AddComponent<Camera>();
            go.tag = "MainCamera";
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
                        || puntos.Any(q => Physics.Linecast(pos, q + Vector3.up * 1.2f, out RaycastHit h, ~0, QueryTriggerInteraction.Ignore) && h.collider.GetComponentInParent<PandilleroAPie>() == null))
                    {
                        continue;
                    }
                    go.transform.position = pos;
                    puesta = true;
                }
            }
            go.transform.LookAt(mira);
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
            File.WriteAllBytes(ruta, foto.EncodeToPNG());
            Debug.Log("[T-1107] captura " + ruta + (puesta ? "" : " (sin posición libre: cámara en el centro)"));
            Object.Destroy(foto);
            Object.Destroy(rt);
            Object.Destroy(go);
        }
    }
}

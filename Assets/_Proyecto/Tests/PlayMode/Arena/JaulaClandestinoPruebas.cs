using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Arena.Jaula;
using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Arena
{
    /// <summary>Pruebas de T-020 sobre el prefab real de la jaula del clandestino y sus escenas.</summary>
    public class JaulaClandestinoPruebas
    {
        private const string RutaPrefab = "Assets/_Proyecto/Arena/Prefabs/JaulaClandestino.prefab";
        private const string RutaClandestino = "Assets/_Proyecto/Escenas/Clandestino.unity";
        private const string RutaPruebasAlan = "Assets/_Proyecto/Escenas/Pruebas_Alan.unity";
        private const float LadoEsperado = 24f;
        private const float ToleranciaLado = 0.5f;
        private const float AlturaMinimaReja = 6f;
        private const float AlturaCamara = 3.5f;
        private const float RadioCapsula = 1f;
        private const float AlturaCapsula = 4f;
        private const float VelocidadEmpuje = 8f;
        private const float SegundosEmpuje = 3f;

        private static readonly string[] RutasEscenas = { RutaClandestino, RutaPruebasAlan };

        private static readonly Vector3[] Direcciones =
        {
            new Vector3(0f, 0f, 1f), new Vector3(0f, 0f, -1f), new Vector3(1f, 0f, 0f), new Vector3(-1f, 0f, 0f),
            new Vector3(1f, 0f, 1f), new Vector3(1f, 0f, -1f), new Vector3(-1f, 0f, 1f), new Vector3(-1f, 0f, -1f)
        };

        private GameObject jaula;

        [SetUp]
        public void SetUp()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            Assert.IsNotNull(prefab, "Falta el prefab " + RutaPrefab);
            jaula = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
        }

        [TearDown]
        public void TearDown()
        {
            if (jaula != null)
            {
                Object.Destroy(jaula);
            }
        }

        private Transform Hijo(string nombre)
        {
            foreach (Transform t in jaula.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == nombre)
                {
                    return t;
                }
            }
            Assert.Fail("Falta el hijo " + nombre);
            return null;
        }

        [UnityTest]
        public IEnumerator Cubo_SoltadoSobreJaula_QuedaSobreElPiso()
        {
            GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubo.transform.position = new Vector3(0f, 5f, 0f);
            Rigidbody cuerpo = cubo.AddComponent<Rigidbody>();
            float t = 0f;
            while (t < 3f)
            {
                t += Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual(0.5f, cubo.transform.position.y, 0.1f, "El cubo debe quedar sobre el piso (y = 0)");
            Assert.Less(cuerpo.linearVelocity.magnitude, 0.5f);
            Object.Destroy(cubo);
        }

        [UnityTest]
        public IEnumerator Capsula_EmpujadaContraLaReja_NuncaSaleDeLaJaula([ValueSource(nameof(Direcciones))] Vector3 direccion)
        {
            GameObject go = new GameObject("CapsulaPrueba");
            go.transform.position = new Vector3(0f, 0.05f, 0f);
            CharacterController cc = go.AddComponent<CharacterController>();
            cc.radius = RadioCapsula;
            cc.height = AlturaCapsula;
            cc.center = new Vector3(0f, AlturaCapsula / 2f, 0f);
            Vector3 dir = direccion.normalized;
            float maxExtension = 0f;
            float t = 0f;
            while (t < SegundosEmpuje)
            {
                float dt = Time.deltaTime;
                t += dt;
                cc.Move(dir * (VelocidadEmpuje * dt) + Vector3.down * (9.81f * dt * dt));
                Vector3 p = go.transform.position;
                maxExtension = Mathf.Max(maxExtension, Mathf.Abs(p.x), Mathf.Abs(p.z));
                yield return null;
            }
            Assert.LessOrEqual(maxExtension, LadoEsperado / 2f, "La cápsula salió de la jaula en la dirección " + direccion);
            Assert.Greater(maxExtension, 8f, "La cápsula debía llegar hasta la reja");
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Medidas_PisoRejaEInicios_SonLasDelDiseno()
        {
            yield return null;
            Collider piso = Hijo("Piso").GetComponent<Collider>();
            Assert.AreEqual(LadoEsperado, piso.bounds.size.x, ToleranciaLado);
            Assert.AreEqual(LadoEsperado, piso.bounds.size.z, ToleranciaLado);
            Assert.AreEqual(0f, piso.bounds.max.y, 0.05f, "La cara superior del piso debe estar en y = 0");

            float alturaReja = 0f;
            foreach (Collider c in Hijo("ColisionReja").GetComponentsInChildren<Collider>())
            {
                alturaReja = Mathf.Max(alturaReja, c.bounds.max.y);
            }
            Assert.GreaterOrEqual(alturaReja, AlturaMinimaReja);

            Transform jugador = Hijo("InicioJugador");
            Transform rival = Hijo("InicioRival");
            float distancia = Vector3.Distance(jugador.position, rival.position);
            Assert.That(distancia, Is.InRange(11f, 13f));
            Assert.AreEqual(0f, jugador.position.y, 0.05f);
            Assert.AreEqual(0f, rival.position.y, 0.05f);
            Assert.Less(Vector3.Dot(jugador.forward, rival.forward), -0.95f);
            Assert.Greater(Vector3.Dot(jugador.forward, (rival.position - jugador.position).normalized), 0.95f);
            Assert.Greater(Vector3.Dot(rival.forward, (jugador.position - rival.position).normalized), 0.95f);
        }

        [UnityTest]
        public IEnumerator Pantalla_DesdeLaCamara_EsVisibleYSuZApuntaHaciaElla()
        {
            yield return null;
            Transform pantalla = Hijo("PantallaArena");
            Vector3 camara = Hijo("InicioJugador").position + Vector3.up * AlturaCamara;
            Vector3 haciaPantalla = pantalla.position - camara;
            bool choca = Physics.Raycast(camara, haciaPantalla.normalized, out RaycastHit golpe, haciaPantalla.magnitude - 0.05f);
            Assert.IsFalse(choca, "Algo tapa la pantalla: " + (choca ? golpe.collider.name : ""));

            Vector3 centroAPantalla = (pantalla.position - jaula.transform.position).normalized;
            Assert.Greater(Vector3.Dot(pantalla.forward, centroAPantalla), 0.9f);
        }

        [UnityTest]
        public IEnumerator Luces_DeLaJaula_TienenNeonDistintoYUnaCenitalBlanca()
        {
            yield return null;
            Light[] luces = jaula.GetComponentsInChildren<Light>();
            Assert.GreaterOrEqual(luces.Length, 5);
            List<float> tonos = new List<float>();
            bool hayBlanca = false;
            foreach (Light luz in luces)
            {
                Color.RGBToHSV(luz.color, out float h, out float s, out float v);
                if (s > 0.6f)
                {
                    bool nuevo = true;
                    foreach (float t in tonos)
                    {
                        float d = Mathf.Abs(t - h);
                        if (Mathf.Min(d, 1f - d) < 0.05f)
                        {
                            nuevo = false;
                        }
                    }
                    if (nuevo)
                    {
                        tonos.Add(h);
                    }
                }
                else if (s < 0.3f && v > 0.8f && luz.transform.position.y > 8f)
                {
                    hayBlanca = true;
                }
            }
            Assert.GreaterOrEqual(tonos.Count, 3, "Se necesitan al menos 3 tonos de neón distintos");
            Assert.IsTrue(hayBlanca, "Falta la luz cenital blanca");
        }

        [UnityTest]
        public IEnumerator ArenaPelea_AlInstanciar_SeRegistraYSeQuitaAlDestruir()
        {
            yield return null;
            ArenaPelea arena = jaula.GetComponent<ArenaPelea>();
            Assert.IsNotNull(arena);
            Assert.IsTrue(Registro<IArenaPelea>.Todos.Contains(arena));
            IArenaPelea contrato = arena;
            Assert.AreEqual(Hijo("InicioJugador").position, contrato.InicioJugador.position);
            Assert.AreEqual(Hijo("InicioRival").position, contrato.InicioRival.position);
            Assert.AreSame(Hijo("PantallaArena"), contrato.PantallaArena);
            Assert.AreEqual(Hijo("Piso").position, contrato.Centro);
            Assert.AreEqual(new Vector2(8f, 4.5f), contrato.TamanoPantalla);
            Assert.IsNotEmpty(contrato.Nombre);

            Object.Destroy(jaula);
            yield return null;
            Assert.IsFalse(Registro<IArenaPelea>.Todos.Contains(arena));
        }

        [UnityTest]
        public IEnumerator Escenas_AlDarPlay_TienenJaulaNieblaYSinErrores([ValueSource(nameof(RutasEscenas))] string ruta)
        {
            Object.Destroy(jaula);
            yield return null;
            yield return EditorSceneManager.LoadSceneInPlayMode(ruta, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            Assert.IsNotNull(Object.FindFirstObjectByType<ArenaPelea>(), "La escena no tiene la jaula");
            if (ruta == RutaClandestino)
            {
                Assert.IsNotNull(GameObject.Find("CamaraVistaArena"));
            }
            Assert.IsTrue(RenderSettings.fog, "La niebla debe estar activa");
            Assert.AreEqual(1, Registro<IArenaPelea>.Cantidad);

            // Deja la escena vacía para que no estorbe a las pruebas siguientes.
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.DestroyImmediate(raiz);
            }
        }
    }
}

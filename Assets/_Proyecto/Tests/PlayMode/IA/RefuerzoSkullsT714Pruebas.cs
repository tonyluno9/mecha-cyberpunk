using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>
    /// T-714: los SKULLS de refuerzo llegan en carro. Con 3 SKULLS vencidos en su zona sale un carro del pool del tráfico fuera de la vista, llega manejando por las
    /// calles con un tiempo real, se estaciona cerca del jugador y de las dos puertas se bajan dos SKULLS que lo atacan. Un carro a la vez, con enfriamiento.
    /// Calles de prueba (6 manzanas de 80 m), el jugador real y el tráfico real (TraficoCalle con sus prefabs).
    /// </summary>
    public class RefuerzoSkullsT714Pruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaConfigPersecucion = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const string RutaConfigTrafico = "Assets/_Proyecto/Trafico/Datos/ConfigTraficoCalle_Trafico.asset";
        private const string RutaDron = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        private const string RutaSkull = "Assets/_Proyecto/Trafico/Resources/SkullRefuerzo.prefab";
        private const float Paso = 94f;
        private const float EscalaDeTiempo = 5f;
        private static readonly Vector2[] Centros =
        {
            new Vector2(-Paso, Paso / 2f), new Vector2(0f, Paso / 2f), new Vector2(Paso, Paso / 2f),
            new Vector2(-Paso, -Paso / 2f), new Vector2(0f, -Paso / 2f), new Vector2(Paso, -Paso / 2f)
        };

        private sealed class TerritorioDoble : ITerritorio
        {
            public int RespetoActual;
            public IReadOnlyList<string> Zonas { get; } = new[] { "tianguis" };
            public string ZonaEn(Vector3 posicion) => "tianguis";
            public string Nombre(string zona) => "EL TIANGUIS";
            public IdPandilla Dueno(string zona) => IdPandilla.Calaveras;
            public int Respeto(string zona) => RespetoActual;
            public bool JefeDisponible(string zona) => false;
            public void RegistrarVencido(Vector3 posicion, IdPandilla pandilla, bool jefe) => RespetoActual += 10;
            public void RegistrarCaida(Vector3 posicion) { }
            public event Action<string> Cambio { add { } remove { } }
            public event Action<string, IdPandilla> Conquistada { add { } remove { } }
        }

        private readonly List<GameObject> extras = new List<GameObject>();
        private GameObject barrio;
        private GameObject jugador;
        private ConfigPersecucion config;
        private GestorRefuerzos refuerzos;
        private TerritorioDoble territorio;
        private Camera camara;
        private Func<float> azarAnterior;
        private float escalaAnterior;
        private Vector3 puntoJugador;

        public override void Setup()
        {
            base.Setup();
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            CarroTrafico.Activos.Clear();
            escalaAnterior = Time.timeScale;
            azarAnterior = PistolaPandillero.Azar;
            PistolaPandillero.Azar = () => 0f; // si hay línea libre, acierta
            config = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfigPersecucion));
            typeof(ConfigPersecucion).GetField("segundosArresto", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(config, 9999f);
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(900f, 1f, 900f);
            extras.Add(piso);
            barrio = new GameObject("BarrioPrueba");
            for (int i = 0; i < Centros.Length; i++)
            {
                GameObject m = new GameObject("Manzana_" + (i + 1));
                m.transform.SetParent(barrio.transform, false);
                m.transform.position = new Vector3(Centros[i].x, 0f, Centros[i].y);
            }
            GrafoCalles grafo = GrafoCalles.DeManzanas(Centros, 80f, 0.2f);
            puntoJugador = grafo.Nodo(grafo.MasCercano(Vector3.zero));
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(puntoJugador.x, 0.05f, puntoJugador.z), Quaternion.identity);
            territorio = new TerritorioDoble();
            Registro<ITerritorio>.Agregar(territorio);
            GameObject camObjeto = new GameObject("CamaraPrueba");
            camObjeto.tag = "MainCamera";
            camara = camObjeto.AddComponent<Camera>();
            camara.fieldOfView = 70f;
            camara.aspect = 16f / 9f;
            camara.farClipPlane = 800f;
            camObjeto.transform.position = jugador.transform.position + Vector3.up * 1.7f;
            camObjeto.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            extras.Add(camObjeto);

            // Persecución (trae al GestorRefuerzos) y el tráfico real con su pool de carros.
            GameObject objeto = new GameObject("Persecucion");
            objeto.SetActive(false);
            objeto.transform.SetParent(barrio.transform, false);
            GestorPersecucion gestor = objeto.AddComponent<GestorPersecucion>();
            gestor.Configurar(config, AssetDatabase.LoadAssetAtPath<GameObject>(RutaDron).GetComponent<DronMundo>(),
                AssetDatabase.LoadAssetAtPath<GameObject>(RutaPatrulla).GetComponent<PatrullaMundo>());
            objeto.SetActive(true);
            refuerzos = gestor.GetComponent<GestorRefuerzos>();
            GameObject traficoObjeto = new GameObject("TraficoCalle");
            traficoObjeto.SetActive(false);
            traficoObjeto.transform.SetParent(barrio.transform, false);
            TraficoCalle trafico = traficoObjeto.AddComponent<TraficoCalle>();
            CarroTrafico[] moldes =
            {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V10.prefab").GetComponent<CarroTrafico>(),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V09.prefab").GetComponent<CarroTrafico>()
            };
            trafico.Configurar(AssetDatabase.LoadAssetAtPath<ConfigTraficoCalle>(RutaConfigTrafico), moldes, barrio.transform);
            traficoObjeto.SetActive(true);
        }

        public override void TearDown()
        {
            Time.timeScale = escalaAnterior;
            PistolaPandillero.Azar = azarAnterior;
            foreach (PandilleroAPie p in Object.FindObjectsByType<PandilleroAPie>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(p.gameObject);
            }
            foreach (GameObject extra in extras)
            {
                if (extra != null)
                {
                    Object.DestroyImmediate(extra);
                }
            }
            extras.Clear();
            Object.DestroyImmediate(barrio);
            Object.DestroyImmediate(jugador);
            Object.DestroyImmediate(config);
            CarroTrafico.Activos.Clear();
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private static float Plano(Vector3 a, Vector3 b)
        {
            a.y = b.y;
            return Vector3.Distance(a, b);
        }

        private CarroTrafico CarroDeRefuerzo() => Object.FindObjectsByType<CarroTrafico>(FindObjectsSortMode.None).FirstOrDefault(c => c.EnRefuerzo);

        private void VencerSkulls(int cuantos)
        {
            for (int i = 0; i < cuantos; i++)
            {
                territorio.RegistrarVencido(jugador.transform.position, IdPandilla.Calaveras, false);
            }
        }

        private static IEnumerator Hasta(Func<bool> condicion, string que, float limite)
        {
            float fin = Time.time + limite;
            while (!condicion() && Time.time < fin)
            {
                yield return null;
            }
            Assert.IsTrue(condicion(), "No llegó: " + que);
        }

        [UnityTest]
        public IEnumerator TresSkullsVencidos_MandanUnCarro_QueSaleOculto_LlegaManejando_YDeLasPuertasBajanDosSkulls_QuePelean()
        {
            yield return null;
            yield return null;
            Assert.IsNotNull(refuerzos, "El gestor de persecución trae el de refuerzos");
            Assert.IsNull(CarroDeRefuerzo(), "Al inicio no hay refuerzos");
            VencerSkulls(2);
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(0, refuerzos.CarrosEnviados, "Con dos vencidos todavía no llaman");
            VencerSkulls(1);
            yield return new WaitForSeconds(0.8f);
            
            yield return Hasta(() => refuerzos.CarrosEnviados == 1, "sale el carro con el tercer vencido", 3f);

            // Sale fuera de la vista y lejos del jugador.
            CarroTrafico carro = CarroDeRefuerzo();
            Assert.IsNotNull(carro, "Hay un carro de refuerzo");
            Vector3 salida = carro.transform.position;
            Assert.IsFalse(CarroTrafico.PuntoEnVista(camara, salida), "Nace fuera de la vista (con margen)");
            Assert.GreaterOrEqual(Plano(salida, jugador.transform.position), config.DistanciaMinimaSalida, "Nace lejos del jugador");
            Assert.IsTrue(carro.gameObject.activeSelf);

            // Llega manejando, con un tiempo real, sin que el pool lo apague ni cambie su ruta.
            float inicio = Time.time;
            Time.timeScale = EscalaDeTiempo;
            float vistoA = -1f;
            bool capturadoLlegando = false;
            while (Time.time < inicio + 150f && !carro.RefuerzoLlego)
            {
                Assert.IsTrue(carro.gameObject.activeSelf, "El pool no apaga al carro de refuerzo");
                if (vistoA < 0f && CarroTrafico.PuntoEnVista(camara, carro.transform.position))
                {
                    vistoA = Plano(carro.transform.position, jugador.transform.position);
                }
                if (!capturadoLlegando && Environment.GetEnvironmentVariable("CAPTURAR_S1") == "1" && carro.MetrosPorRecorrer < 40f)
                {
                    capturadoLlegando = true;
                    Time.timeScale = 1f;
                    yield return Capturar(carro, "s1c-T-714-carro-llegando.png", false);
                    Time.timeScale = EscalaDeTiempo;
                }
                yield return null;
            }
            float segundos = Time.time - inicio;
            Time.timeScale = 1f;
            Assert.IsTrue(carro.RefuerzoLlego, "El carro llega y se estaciona");
            Assert.GreaterOrEqual(segundos, config.SegundosMinimosLlegadaPatrulla * 0.8f, "Tarda un tiempo real, no aparece junto al jugador");
            Assert.AreEqual(0f, carro.MetrosPorRecorrer, 0.01f);
            Debug.Log("[T-714] salió a " + Plano(salida, jugador.transform.position).ToString("F0") + " m, llegó en " + segundos.ToString("F1") + " s; se vio a "
                + vistoA.ToString("F0") + " m; se estacionó a " + Plano(carro.transform.position, jugador.transform.position).ToString("F0") + " m del jugador");

            // De las dos puertas se bajan dos SKULLS, ya en alerta.
            List<GameObject> skulls = refuerzos.Pasajeros.ToList();
            Assert.AreEqual(2, skulls.Count, "Se bajan dos SKULLS");
            float lado0 = Vector3.Dot(skulls[0].transform.position - carro.transform.position, carro.transform.right);
            float lado1 = Vector3.Dot(skulls[1].transform.position - carro.transform.position, carro.transform.right);
            Assert.Less(lado0 * lado1, 0f, "Uno por cada puerta (lados opuestos del carro)");
            foreach (GameObject s in skulls)
            {
                Assert.LessOrEqual(Vector3.Distance(s.transform.position, carro.transform.position), 4f, "Salen por las puertas del carro");
                Assert.IsTrue(s.GetComponent<PandilleroAPie>().Alerta, "Bajan peleando");
            }
            if (Environment.GetEnvironmentVariable("CAPTURAR_S1") == "1")
            {
                yield return Capturar(carro, "s1c-T-714-se-bajan.png", true);
            }

            // Pelean: le disparan al jugador y le bajan la vida.
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            float antes = vida.Actual + (Registro<IEscudo>.Actual?.Actual ?? 0f);
            PistolaPandillero[] pistolas = skulls.Select(s => s.GetComponent<PistolaPandillero>()).ToArray();
            yield return Hasta(() => pistolas.Sum(p => p.Disparos) > 0, "los SKULLS de refuerzo disparan", 20f);
            yield return Hasta(() => vida.Actual + (Registro<IEscudo>.Actual?.Actual ?? 0f) < antes, "los SKULLS de refuerzo le dan al jugador", 20f);

            // Matarlos sigue siendo como siempre: caen.
            PandilleroAPie primero = skulls[0].GetComponent<PandilleroAPie>();
            primero.RecibirDanio(new InfoDanio(100000f, TipoDanio.Disparo, primero.transform.position + Vector3.up, Vector3.forward, null));
            Assert.IsTrue(primero.EstaKO, "Los de refuerzo caen al morir");
        }

        [UnityTest]
        public IEnumerator SoloUnCarroALaVez_YConEnfriamiento()
        {
            yield return null;
            yield return null;
            VencerSkulls(3);
            yield return Hasta(() => refuerzos.CarrosEnviados == 1, "sale el primer carro", 3f);
            VencerSkulls(6);
            yield return new WaitForSeconds(1.0f);
            Assert.AreEqual(1, refuerzos.CarrosEnviados, "Con uno en camino y enfriamiento, no sale otro");
            Assert.AreEqual(1, Object.FindObjectsByType<CarroTrafico>(FindObjectsSortMode.None).Count(c => c.EnRefuerzo));
        }

        [UnityTest]
        public IEnumerator ElPrefabDeRefuerzo_EsUnSkullArmado()
        {
            yield return null;
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(RutaSkull), "El prefab SkullRefuerzo existe en Trafico/Resources");
            GameObject molde = AssetDatabase.LoadAssetAtPath<GameObject>(RutaSkull);
            PandilleroAPie p = molde.GetComponent<PandilleroAPie>();
            Assert.IsNotNull(p, "Y trae un PandilleroAPie");
            Assert.IsTrue(p.Datos.UsaPistola, "Con pistola (SKULL armado)");
        }

        private IEnumerator Capturar(CarroTrafico carro, string archivo, bool desdeUnLado)
        {
            GameObject objeto = new GameObject("CamaraCaptura");
            extras.Add(objeto);
            Camera c = objeto.AddComponent<Camera>();
            c.farClipPlane = 400f;
            Vector3 centro = carro.transform.position + Vector3.up * 0.9f;
            if (desdeUnLado)
            {
                objeto.transform.position = centro + carro.transform.right * 7f + carro.transform.forward * -4f + Vector3.up * 1.6f;
            }
            else
            {
                Vector3 haciaJugador = (jugador.transform.position - carro.transform.position).normalized;
                objeto.transform.position = jugador.transform.position + Vector3.up * 1.7f - haciaJugador * 0.2f;
            }
            objeto.transform.LookAt(centro);
            yield return null;
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            c.targetTexture = rt;
            c.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            foto.Apply();
            c.targetTexture = null;
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(Application.dataPath, "..", "docs", "capturas", archivo), foto.EncodeToPNG());
            Object.DestroyImmediate(foto);
            Object.DestroyImmediate(rt);
        }
    }
}

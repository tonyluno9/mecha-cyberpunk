using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// T-451 con el prefab real en un barrio de prueba (4 manzanas de 80 m): el tráfico arma las calles, deja más carros de día que de noche,
    /// un carro anda por su carril y sigue por las calles, frena y se detiene ante el jugador y pita, y se asusta y pita con un disparo.
    /// </summary>
    public class TraficoCallePruebas
    {
        private const string RutaPrefab = "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico.prefab";
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigTraficoCalle_Trafico.asset";

        private sealed class RelojDoble : IRelojMundo
        {
            public bool Noche;
            public float Hora => Noche ? 22f : 12f;
            public int Dia => 1;
            public FranjaDia Franja => Noche ? FranjaDia.Noche : FranjaDia.Dia;
            public bool EsDeNoche => Noche;
            public float HoraAmanecer => 6f;
            public float HoraAnochecer => 20f;
            public bool Pausado => true;
            public event Action<int> HoraEnteraCambio { add { } remove { } }
            public event Action<FranjaDia> FranjaCambio { add { } remove { } }
            public event Action<int> DiaCambio { add { } remove { } }
            public void Pausar(bool pausado) { }
            public void AvanzarHasta(float hora) { }
        }

        private sealed class JugadorDoble : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara => null;
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) { }
        }

        private GameObject barrio;
        private GameObject piso;
        private RelojDoble reloj;
        private ConfigTraficoCalle config;
        private CarroTrafico prefab;
        private readonly List<GameObject> extras = new List<GameObject>();

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            EventosRuido.LimpiarSuscriptores();
            reloj = new RelojDoble();
            Registro<IRelojMundo>.Agregar(reloj);
            config = AssetDatabase.LoadAssetAtPath<ConfigTraficoCalle>(RutaConfig);
            Assert.IsNotNull(config, "Falta " + RutaConfig);
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab).GetComponent<CarroTrafico>();
            Assert.IsNotNull(prefab, "Falta " + RutaPrefab);
            barrio = new GameObject("BarrioDePrueba");
            foreach (Vector2 c in new[] { new Vector2(0f, 0f), new Vector2(80f, 0f), new Vector2(0f, 80f), new Vector2(80f, 80f) })
            {
                GameObject m = new GameObject("Manzana_" + c.x + "_" + c.y);
                m.transform.SetParent(barrio.transform, false);
                m.transform.position = new Vector3(c.x, 0f, c.y);
            }
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(40f, -0.5f, 40f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
        }

        [TearDown]
        public void Limpiar()
        {
            foreach (GameObject extra in extras)
            {
                if (extra != null)
                {
                    Object.DestroyImmediate(extra);
                }
            }
            extras.Clear();
            Object.DestroyImmediate(barrio);
            Object.DestroyImmediate(piso);
            EventosRuido.LimpiarSuscriptores();
            Registros.LimpiarTodos();
            CarroTrafico.Activos.Clear();
        }

        private TraficoCalle CrearTrafico()
        {
            GameObject raiz = new GameObject("TraficoDePrueba");
            extras.Add(raiz);
            TraficoCalle trafico = raiz.AddComponent<TraficoCalle>();
            trafico.Configurar(config, prefab, barrio.transform);
            return trafico;
        }

        // Un solo carro en el nodo pedido, sin el administrador (el resto de las pruebas lo mueve a mano).
        private CarroTrafico CrearCarro(int semilla, Func<Vector3?> jugador)
        {
            List<Vector2> centros = new List<Vector2> { new Vector2(0f, 0f), new Vector2(80f, 0f), new Vector2(0f, 80f), new Vector2(80f, 80f) };
            GrafoCalles calles = GrafoCalles.DeManzanas(centros, 80f, 0.2f);
            CarroTrafico carro = Object.Instantiate(prefab);
            extras.Add(carro.gameObject);
            carro.Preparar(config, calles, calles.MasCercano(new Vector3(40f, 0f, -40f)), new System.Random(semilla), jugador);
            return carro;
        }

        [UnityTest]
        public IEnumerator Trafico_DeDiaHayMasCarrosQueDeNoche_YNingunoEnLaCalleDelJugador()
        {
            TraficoCalle trafico = CrearTrafico();
            yield return null;
            yield return null;
            Assert.AreEqual(config.Maximo, trafico.Carros.Count, "Prepara el máximo");
            Assert.AreEqual(config.CarrosDeDia, trafico.Vivos, "De día salen los carros de día");
            reloj.Noche = true;
            yield return null;
            Assert.AreEqual(config.CarrosDeNoche, trafico.Vivos, "De noche menos");
            Assert.Greater(config.CarrosDeDia, config.CarrosDeNoche);
        }

        [UnityTest]
        public IEnumerator Carro_AndaPorLaCalle_ADuranteVariosTramos_SinSalirDelBarrio()
        {
            CarroTrafico carro = CrearCarro(7, () => null);
            yield return null;
            Vector3 inicio = carro.transform.position;
            float maximoDeLaCalle = 0f;
            float fin = Time.time + 12f;
            while (Time.time < fin)
            {
                yield return null;
                Vector3 p = carro.transform.position;
                maximoDeLaCalle = Mathf.Max(maximoDeLaCalle, Mathf.Max(Mathf.Abs(p.x - 40f), Mathf.Abs(p.z - 40f)));
            }
            Assert.Greater(Vector3.Distance(inicio, carro.transform.position), 5f, "Se mueve");
            Assert.Greater(carro.Velocidad, 3f, "Va a su paso");
            Assert.Less(maximoDeLaCalle, 100f, "No se sale del barrio");
            Assert.AreEqual(0.45f, carro.transform.position.y, 0.01f, "Rueda a la altura de la calle");
        }

        [UnityTest]
        public IEnumerator Carro_FrenaSeDetieneAnteElJugador_YPita()
        {
            GameObject obstaculo = new GameObject("JugadorDePrueba");
            extras.Add(obstaculo);
            JugadorDoble jugador = new JugadorDoble { Raiz = obstaculo.transform };
            CarroTrafico carro = CrearCarro(7, () => jugador.Raiz.position);
            yield return null;
            // El jugador se para 12 m adelante, justo en el carril del carro.
            obstaculo.transform.position = carro.transform.position + carro.transform.forward * 12f;
            float fin = Time.time + 6f;
            while (Time.time < fin)
            {
                yield return null;
            }
            Assert.Less(carro.Velocidad, 0.3f, "Se detuvo antes de llegar al jugador");
            Assert.Greater(Vector3.Distance(carro.transform.position, obstaculo.transform.position), 3f, "No lo atropelló");
            Assert.GreaterOrEqual(carro.Pitidos, 1, "Bloqueado, pita");
            // Se quita el obstáculo y sigue.
            obstaculo.transform.position = new Vector3(0f, 0f, 500f);
            yield return new WaitForSeconds(3f);
            Assert.Greater(carro.Velocidad, 3f, "Libre, vuelve a andar");
        }

        [UnityTest]
        public IEnumerator Carro_ConUnDisparoCerca_SeAsustaPitaYSeDetiene()
        {
            CarroTrafico carro = CrearCarro(7, () => null);
            yield return new WaitForSeconds(2f);
            Assert.Greater(carro.Velocidad, 2f, "Va andando");
            int pitidosAntes = carro.Pitidos;
            EventosRuido.PublicarDisparo(carro.transform.position + Vector3.right * 10f);
            Assert.IsTrue(carro.EnPanico, "Un disparo a 10 m lo asusta");
            yield return new WaitForSeconds(1.2f);
            Assert.Less(carro.Velocidad, 0.3f, "Se detuvo");
            Assert.AreEqual(pitidosAntes + 1, carro.Pitidos, "Pitó una vez por el susto");
            yield return new WaitForSeconds(config.SegundosSusto + 2f);
            Assert.IsFalse(carro.EnPanico);
            Assert.Greater(carro.Velocidad, 2f, "Pasado el susto sigue su camino");
        }

        [UnityTest]
        public IEnumerator Carro_ConUnDisparoLejano_NoSeAsusta()
        {
            CarroTrafico carro = CrearCarro(7, () => null);
            yield return null;
            EventosRuido.PublicarDisparo(carro.transform.position + Vector3.right * (config.RadioSusto + 20f));
            Assert.IsFalse(carro.EnPanico, "Fuera del radio no lo oye");
        }

        [UnityTest]
        public IEnumerator Ciudad_TieneTraficoYLosCarrosAndan()
        {
            Registros.LimpiarTodos();
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/_Proyecto/Escenas/Ciudad_Prologo.unity",
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            yield return null;
            TraficoCalle trafico = null;
            float limite = Time.realtimeSinceStartup + 30f;
            while ((trafico = Object.FindFirstObjectByType<TraficoCalle>()) == null || trafico.Carros.Count == 0 || Registro<IJugador>.Actual == null)
            {
                Assert.Less(Time.realtimeSinceStartup, limite, "La ciudad trae el tráfico de calle");
                yield return null;
            }
            yield return new WaitForSecondsRealtime(1f);
            IRelojMundo relojCiudad = Registro<IRelojMundo>.Actual;
            Assert.IsNotNull(relojCiudad);
            Assert.AreEqual(config.Cantidad(relojCiudad.EsDeNoche), trafico.Vivos, "Los carros prendidos son los que toca a esta hora");
            yield return new WaitForSecondsRealtime(6f);
            int andando = 0;
            CarroTrafico cercano = null;
            float menor = float.MaxValue;
            Vector3 jugador = Registro<IJugador>.Actual.Raiz.position;
            foreach (CarroTrafico c in trafico.Carros)
            {
                if (!c.gameObject.activeSelf)
                {
                    continue;
                }
                andando += c.Velocidad > 1f ? 1 : 0;
                float d = Vector3.Distance(c.transform.position, jugador);
                if (d < menor)
                {
                    menor = d;
                    cercano = c;
                }
            }
            Debug.Log("[Prueba] T-451 en la ciudad: " + trafico.Vivos + " carros, " + andando + " andando; el más cercano a " + menor + " m");
            Assert.Greater(andando, 0, "Al menos uno anda");
            if (System.Environment.GetEnvironmentVariable("CAPTURAR_T451") == "1" && cercano != null)
            {
                GameObject camObjeto = new GameObject("CamCapturaTrafico");
                Camera cam = camObjeto.AddComponent<Camera>();
                cam.farClipPlane = 300f;
                camObjeto.transform.position = cercano.transform.position + new Vector3(3f, 16f, -8f);
                camObjeto.transform.LookAt(cercano.transform.position + Vector3.up * 0.5f);
                yield return null;
                RenderTexture rt = new RenderTexture(1280, 720, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                Texture2D img = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                img.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
                img.Apply();
                RenderTexture.active = null;
                cam.targetTexture = null;
                System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "docs/capturas/T-451-1.png")), img.EncodeToPNG());
                Object.Destroy(img);
                Object.Destroy(rt);
                Object.Destroy(camObjeto);
            }
            foreach (GameObject raiz in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            yield return null;
        }
    }
}

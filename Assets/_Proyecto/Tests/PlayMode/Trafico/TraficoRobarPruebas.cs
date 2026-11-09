using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// T-511: los carros del tráfico usan los modelos reales y se pueden robar. Si el jugador se acerca a uno detenido y pulsa E, el conductor se baja
    /// y huye, el carro es suyo (sube el calor) y se maneja; uno en marcha no se puede robar. Jugador real y entrada simulada.
    /// </summary>
    public class TraficoRobarPruebas : InputTestFixture
    {
        private const string RutaPrefabTrafico = "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V02.prefab";
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaCiudad = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string NombreConductor = "ConductorRobado";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float DistanciaAlSubir = 1.5f;
        private const float RapidezDelCarroEnMarcha = 7f;
        private const float LimiteCarga = 30f;

        private Keyboard teclado;
        private GameObject piso;
        private readonly List<GameObject> extras = new List<GameObject>();

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosRuido.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            Mouse raton = InputSystem.AddDevice<Mouse>();
            MapasDePruebaTrafico.Reiniciar(teclado, raton);
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
        }

        public override void TearDown()
        {
            foreach (GameObject extra in extras)
            {
                if (extra != null)
                {
                    Object.Destroy(extra);
                }
            }
            extras.Clear();
            Object.Destroy(piso);
            Registros.LimpiarTodos();
            CarroTrafico.Activos.Clear();
            MapasDePruebaTrafico.AntesDelTearDown();
            base.TearDown();
            MapasDePruebaTrafico.DespuesDelTearDown();
        }

        // Nada se queda en la escena activa (la ciudad, el conductor que huyó): la prueba siguiente arma la suya.
        /// <summary>T-933: un testigo de la policía de prueba que siempre ve al jugador (para forzar que el delito cuente).</summary>
        private sealed class TestigoQueSiempreVe : MechaCyberpunk.Trafico.Testigos.ITestigoPolicia
        {
            public bool VeElPunto(Vector3 punto) => true;
        }

        [UnityTearDown]
        public IEnumerator VaciarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            Registros.LimpiarTodos();
            yield return null;
        }

        private static IEnumerator Calentar()
        {
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
        }

        private static string Candidato(IJugador jugador)
        {
            foreach (MonoBehaviour m in ((Component)jugador).GetComponentsInChildren<MonoBehaviour>())
            {
                System.Reflection.PropertyInfo p = m.GetType().GetProperty("MontableCandidato");
                if (p != null)
                {
                    object c = p.GetValue(m);
                    return c is Component comp ? comp.name : (c == null ? "ninguno" : c.ToString());
                }
            }
            return "?";
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private static void JugadorJuntoALaPuerta(IJugador jugador, CarroViejo carro)
        {
            Vector3 punto = carro.PuntoSubida;
            Vector3 posicion = punto - carro.transform.right * DistanciaAlSubir;
            posicion.y = punto.y + 0.1f;
            Vector3 mirada = punto - posicion;
            mirada.y = 0f;
            jugador.Colocar(new Pose(posicion, Quaternion.LookRotation(mirada.normalized, Vector3.up)));
        }

        private IEnumerator CrearCarroYJugador(System.Action<CarroTrafico, IJugador> listo)
        {
            yield return Calentar();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabTrafico);
            Assert.IsNotNull(prefab, "Falta " + RutaPrefabTrafico);
            GameObject carroObjeto = Object.Instantiate(prefab, new Vector3(0f, 0.3f, 10f), Quaternion.identity);
            extras.Add(carroObjeto);
            GameObject jugadorObjeto = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador));
            extras.Add(jugadorObjeto);
            yield return null;
            yield return null;
            IJugador jugador = Registro<IJugador>.Actual;
            Assert.IsNotNull(jugador, "El jugador real se registra");
            CarroTrafico trafico = carroObjeto.GetComponent<CarroTrafico>();
            Assert.IsNotNull(trafico, "El prefab de tráfico lleva CarroTrafico");
            JugadorJuntoALaPuerta(jugador, carroObjeto.GetComponent<CarroViejo>());
            yield return Esperar(0.5f);
            listo(trafico, jugador);
        }

        [UnityTest]
        public IEnumerator RobarDelTrafico_CarroDetenido_ElConductorHuyeYSubeElCalor()
        {
            CarroTrafico trafico = null;
            IJugador jugador = null;
            yield return CrearCarroYJugador((t, j) => { trafico = t; jugador = j; });
            CarroViejo carro = trafico.GetComponent<CarroViejo>();
            Rigidbody cuerpo = trafico.GetComponent<Rigidbody>();
            ICalor calor = Registro<ICalor>.Actual;
            float antes = calor.Valor;
            Assert.IsTrue(cuerpo.isKinematic, "En la calle es cinemático");
            Assert.IsTrue(carro.EstaEnTrafico);
            Assert.IsNull(GameObject.Find(NombreConductor), "Todavía no hay conductor afuera");

            yield return Tecla(teclado.eKey);

            Assert.AreEqual(ModoJugador.Carro, jugador.Modo, "E junto a un carro detenido lo roba (candidato " + Candidato(jugador) + ", puedeSubir " + carro.PuedeSubir + ", jugador " + jugador.Raiz.position.ToString("F1") + " punto " + carro.PuntoSubida.ToString("F1") + " cam " + jugador.Camara.transform.eulerAngles.y.ToString("F0") + ")");
            Assert.IsTrue(trafico.Robado, "Deja de ser tráfico");
            Assert.IsFalse(cuerpo.isKinematic, "Ahora tiene física de carro del jugador");
            Assert.GreaterOrEqual(calor.Valor, antes + 25f, "Robarlo sube el calor");
            GameObject conductor = GameObject.Find(NombreConductor);
            Assert.IsNotNull(conductor, "El conductor se baja");
            // T-705: primero lo sacan y cae al suelo, luego se levanta asustado, y recién entonces sale corriendo.
            yield return Esperar(0.6f);
            Assert.IsTrue(conductor.GetComponent<ConductorRobado>().Tirado, "Cae al suelo");
            yield return Esperar(3.2f);
            Assert.IsFalse(conductor.GetComponent<ConductorRobado>().Tirado, "Se levantó");
            Vector3 salida = conductor.transform.position;
            yield return Esperar(1.5f);
            Assert.IsNotNull(conductor, "Sigue huyendo");
            Assert.Greater(Vector3.Distance(salida, conductor.transform.position), 4f, "Corre lejos del carro");
            Vector3 amenaza = jugador.Raiz.position;
            Assert.Greater(Vector3.Distance(conductor.transform.position, amenaza), Vector3.Distance(salida, amenaza), "Se aleja del jugador");

            Vector3 inicio = trafico.transform.position;
            Press(teclado.wKey);
            yield return Esperar(2f);
            Release(teclado.wKey);
            Assert.Greater(Vector3.Distance(inicio, trafico.transform.position), 5f, "Es un carro suyo: lo maneja");
        }

        [UnityTest]
        public IEnumerator RobarDelTrafico_CarroEnMarcha_NoSePuede()
        {
            CarroTrafico trafico = null;
            IJugador jugador = null;
            yield return CrearCarroYJugador((t, j) => { trafico = t; jugador = j; });
            CarroViejo carro = trafico.GetComponent<CarroViejo>();
            // Un tramo de calle recto a 7 m/s mientras el jugador está junto a la puerta.
            float fin = Time.time + 1f;
            Vector3 jugadorFijo = jugador.Raiz.position;
            bool intento = false;
            while (Time.time < fin)
            {
                trafico.transform.position += Vector3.forward * (RapidezDelCarroEnMarcha * Time.deltaTime);
                jugador.Colocar(new Pose(trafico.transform.position - trafico.transform.right * (DistanciaAlSubir + 0.9f), jugador.Raiz.rotation));
                if (!intento && Time.time > fin - 0.5f)
                {
                    intento = true;
                    Press(teclado.eKey);
                }
                yield return null;
                if (intento)
                {
                    Release(teclado.eKey);
                }
            }
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "Un carro en marcha no se roba");
            Assert.IsFalse(carro.PuedeSubir, "No se puede subir mientras anda");
            Assert.IsFalse(trafico.Robado);
            Assert.IsNull(GameObject.Find(NombreConductor), "Nadie se baja");
            Assert.AreNotEqual(jugadorFijo, Vector3.zero);
        }

        [UnityTest]
        public IEnumerator Ciudad_ElTraficoUsaLosModelosReales_YSeRobaUnoDetenido()
        {
            Registros.LimpiarTodos();
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaCiudad, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            TraficoCalle trafico = null;
            float limite = Time.realtimeSinceStartup + LimiteCarga;
            while ((trafico = Object.FindFirstObjectByType<TraficoCalle>()) == null || trafico.Carros.Count == 0 || Registro<IJugador>.Actual == null)
            {
                Assert.Less(Time.realtimeSinceStartup, limite, "La ciudad trae el tráfico de calle");
                yield return null;
            }
            yield return new WaitForSecondsRealtime(1f);

            HashSet<string> modelos = new HashSet<string>();
            foreach (CarroTrafico c in trafico.Carros)
            {
                Assert.IsNotNull(c.GetComponent<CarroViejo>(), c.name + " es un carro manejable");
                Transform visual = c.transform.Find("Visual_T511");
                Assert.IsNotNull(visual, c.name + " lleva el modelo real");
                modelos.Add(visual.GetComponentInChildren<Renderer>(true).sharedMaterial.name);
                foreach (Renderer r in c.GetComponentsInChildren<Renderer>(true))
                {
                    if (r.transform.parent != null && r.transform.parent.name.StartsWith("Rueda_"))
                    {
                        Assert.IsFalse(r.enabled, c.name + ": rueda de greybox visible");
                    }
                }
            }
            Assert.GreaterOrEqual(modelos.Count, 3, "El tráfico mezcla modelos: " + string.Join(", ", modelos));

            // Un carro prendido se detiene (asustado) y el jugador le roba el lugar.
            CarroTrafico objetivo = trafico.Carros.First(c => c.gameObject.activeSelf);
            IJugador jugador = Registro<IJugador>.Actual;
            ICalor calor = Registro<ICalor>.Actual;
            objetivo.AlOirDisparo(objetivo.transform.position);
            float tope = Time.time + 6f;
            while (objetivo.Velocidad > 0.2f && Time.time < tope)
            {
                yield return null;
            }
            Assert.Less(objetivo.Velocidad, 0.5f, "El carro quedó detenido");
            CarroViejo carro = objetivo.GetComponent<CarroViejo>();
            JugadorJuntoALaPuerta(jugador, carro);
            yield return Esperar(0.3f);
            // T-933: robar un carro solo sube el calor si lo ve la policía; aquí un testigo de prueba siempre ve.
            TestigoQueSiempreVe testigo = new TestigoQueSiempreVe();
            Registro<MechaCyberpunk.Trafico.Testigos.ITestigoPolicia>.Agregar(testigo);
            float antes = calor.Valor;
            Assert.IsTrue(carro.PuedeSubir, "Detenido, se puede subir");
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.Carro, jugador.Modo, "E lo roba (candidato " + Candidato(jugador) + ", puedeSubir " + carro.PuedeSubir + ", jugador " + jugador.Raiz.position.ToString("F1") + " punto " + carro.PuntoSubida.ToString("F1") + " cam " + jugador.Camara.transform.eulerAngles.y.ToString("F0") + ")");
            Assert.IsTrue(objetivo.Robado);
            Assert.IsNotNull(GameObject.Find(NombreConductor), "El conductor huye");
            Assert.IsTrue(calor.Valor >= antes + 25f || calor.Valor >= 100f, "Sube el calor");
            Registro<MechaCyberpunk.Trafico.Testigos.ITestigoPolicia>.Quitar(testigo);
            Assert.AreEqual(trafico.Carros.Count(c => c.gameObject.activeSelf && !c.Robado), trafico.Vivos, "El carro robado ya no cuenta como tráfico");
        }
    }
}

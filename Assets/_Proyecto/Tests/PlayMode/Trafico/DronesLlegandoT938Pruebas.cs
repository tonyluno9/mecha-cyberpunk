using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MechaCyberpunk.Core;
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
    /// T-938: los drones solo aparecen con búsqueda de nivel 2 o más, salen lejos (90 m) y fuera de la vista, llegan volando (no de la nada) y no se
    /// atraviesan entre sí. Gestor real sobre un barrio de prueba.
    /// </summary>
    public class DronesLlegandoT938Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const string RutaDron = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        private const float Paso = 94f;
        private const float EscalaDeTiempo = 3f;
        private const float LlegadaSobreElJugador = 16f; // órbita de 4 m + los 6 m de seguimiento + margen
        private static readonly Vector2[] Centros =
        {
            new Vector2(-Paso, Paso / 2f), new Vector2(0f, Paso / 2f), new Vector2(Paso, Paso / 2f),
            new Vector2(-Paso, -Paso / 2f), new Vector2(0f, -Paso / 2f), new Vector2(Paso, -Paso / 2f)
        };

        private readonly List<GameObject> extras = new List<GameObject>();
        private GameObject barrio;
        private GameObject jugador;
        private ConfigPersecucion config;
        private GestorPersecucion gestor;
        private TestigosT661Pruebas.CalorReal calor;
        private float escalaAnterior;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPolicia.LimpiarSuscriptores();
            escalaAnterior = Time.timeScale;
            config = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig));
            Poner("patrullasDeRonda", 0);
            Poner("segundosArresto", 9999);
            Poner("segundosParaEscapar", 9999);
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(2000f, 1f, 2000f);
            extras.Add(piso);
            barrio = new GameObject("BarrioPrueba");
            for (int i = 0; i < Centros.Length; i++)
            {
                GameObject m = new GameObject("Manzana_" + (i + 1));
                m.transform.SetParent(barrio.transform, false);
                m.transform.position = new Vector3(Centros[i].x, 0f, Centros[i].y);
            }
            jugador = new GameObject("JugadorDePrueba");
            jugador.transform.position = Vector3.zero;
            Registro<IJugador>.Agregar(new TestigosT661Pruebas.JugadorDoble { Raiz = jugador.transform });
            calor = new TestigosT661Pruebas.CalorReal();
            Registro<ICalor>.Agregar(calor);
            GameObject camObjeto = new GameObject("CamaraPrueba");
            camObjeto.tag = "MainCamera";
            camObjeto.AddComponent<Camera>();
            camObjeto.transform.position = new Vector3(0f, 600f, 0f);
            camObjeto.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // mira al cielo: nada de lo que nace se ve
            extras.Add(camObjeto);
            GameObject objeto = new GameObject("Persecucion");
            objeto.SetActive(false);
            objeto.transform.SetParent(barrio.transform, false);
            gestor = objeto.AddComponent<GestorPersecucion>();
            gestor.Configurar(config, AssetDatabase.LoadAssetAtPath<GameObject>(RutaDron).GetComponent<DronMundo>(),
                AssetDatabase.LoadAssetAtPath<GameObject>(RutaPatrulla).GetComponent<PatrullaMundo>());
            objeto.SetActive(true);
        }

        [TearDown]
        public void Limpiar()
        {
            Time.timeScale = escalaAnterior;
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
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPolicia.LimpiarSuscriptores();
        }

        private void Poner(string campo, int valor)
        {
            FieldInfo info = typeof(ConfigPersecucion).GetField(campo, BindingFlags.NonPublic | BindingFlags.Instance);
            info.SetValue(config, System.Convert.ChangeType(valor, info.FieldType));
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private DronMundo[] Drones() => gestor.GetComponentsInChildren<DronMundo>();

        private float Plano(Vector3 p)
        {
            Vector3 d = p - jugador.transform.position;
            d.y = 0f;
            return d.magnitude;
        }

        [UnityTest]
        public IEnumerator ConBusquedaNivel1_NoHayDrones_YConNivel2_Y_3_Si()
        {
            calor.Sumar(15f, MotivoCalor.Mision);
            Assert.AreEqual(1, calor.Nivel);
            yield return Esperar(0.5f);
            Assert.AreEqual(0, gestor.Drones, "En el nivel 1 no sale ningún dron");
            calor.Sumar(30f, MotivoCalor.Mision);
            Assert.AreEqual(2, calor.Nivel);
            yield return null;
            Assert.AreEqual(1, gestor.Drones, "En el nivel 2 sale uno");
            calor.Sumar(30f, MotivoCalor.Mision);
            Assert.AreEqual(3, calor.Nivel);
            yield return null;
            Assert.AreEqual(2, gestor.Drones, "En el nivel 3 salen dos");
        }

        [UnityTest]
        public IEnumerator LosDrones_SalenLejosYAlto_FueraDeLaVista_YLleganVolando()
        {
            calor.Sumar(70f, MotivoCalor.Mision);
            yield return null;
            DronMundo[] drones = Drones();
            Assert.AreEqual(2, drones.Length);
            foreach (DronMundo d in drones)
            {
                Assert.AreEqual(config.DistanciaAparicionDron, Plano(d.transform.position), 1f, "Sale a la distancia de aparición (90 m)");
                Assert.AreEqual(config.AlturaDron, d.transform.position.y - jugador.transform.position.y, 0.1f, "Sale en el aire, a altura");
            }
            Assert.Greater(Vector3.Distance(drones[0].transform.position, drones[1].transform.position), 20f, "Cada dron sale por su lado");
            float antes = drones.Min(d => Plano(d.transform.position));
            yield return Esperar(3f);
            float despues = drones.Min(d => Plano(d.transform.position));
            Assert.Less(despues, antes - 10f, "Avanzan hacia el jugador volando");
            Assert.Greater(despues, antes - 3f * 12f, "No aparecen de golpe: van a la velocidad del dron");
        }

        [UnityTest]
        public IEnumerator LosDrones_AlLlegar_NuncaQuedanAMenosDe3Metros()
        {
            calor.Sumar(70f, MotivoCalor.Mision);
            System.Func<float> azarAnterior = DronMundo.Azar;
            DronMundo.Azar = () => 1f; // no aciertan: la prueba mide posiciones, no daño
            yield return null;
            Time.timeScale = EscalaDeTiempo;
            float menor = float.MaxValue;
            bool llegaron = false;
            float limite = Time.time + 40f;
            while (Time.time < limite)
            {
                yield return null;
                DronMundo[] drones = Drones();
                if (drones.Length < 2)
                {
                    continue;
                }
                float entre = Vector3.Distance(drones[0].transform.position, drones[1].transform.position);
                menor = Mathf.Min(menor, entre);
                llegaron |= drones.All(d => Plano(d.transform.position) < LlegadaSobreElJugador);
            }
            Time.timeScale = 1f;
            DronMundo.Azar = azarAnterior;
            Assert.IsTrue(llegaron, "Los dos llegaron sobre el jugador");
            Assert.GreaterOrEqual(menor, config.DistanciaMinimaEntreDrones - 0.1f, "Nunca se atravesaron (la menor distancia fue " + menor.ToString("F2") + " m)");
        }
    }
}

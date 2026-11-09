using System.Collections;
using System.Collections.Generic;
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
    /// T-933: la policía de ronda. Una patrulla circula sin sirena y sin ver al jugador; un delito que ve sube el calor y esa patrulla pasa a perseguir;
    /// si ve a un criminal atacando va y baja a sus policías. (El policía a pie que no ataca con nivel 0 se prueba en IA: PoliciaNivelCeroT933Pruebas.)
    /// </summary>
    public class PoliciaRondaT933Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const string RutaDron = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        private const float Paso = 94f;
        private const float EscalaDeTiempo = 3f;
        private static readonly Vector2[] Centros =
        {
            new Vector2(-Paso, Paso / 2f), new Vector2(0f, Paso / 2f), new Vector2(Paso, Paso / 2f),
            new Vector2(-Paso, -Paso / 2f), new Vector2(0f, -Paso / 2f), new Vector2(Paso, -Paso / 2f)
        };

        public sealed class CriminalDoble : MonoBehaviour, ICriminalCalle
        {
            public bool Ataca = true;
            public Vector3 Posicion => transform.position;
            public bool EstaAtacando => Ataca;
        }

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
            Poner("patrullasDeRonda", 1);
            Poner("segundosArresto", 9999);
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
            jugador.transform.position = new Vector3(0f, 0f, 0f);
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

        // Espera a que salga la patrulla de ronda y la devuelve.
        private IEnumerator EsperarRonda(System.Action<PatrullaMundo> listo)
        {
            float limite = Time.time + 15f;
            while (Time.time < limite && gestor.Rondas.Count == 0)
            {
                yield return null;
            }
            Assert.AreEqual(1, gestor.Rondas.Count, "Sale la patrulla de ronda");
            listo(gestor.Rondas[0]);
        }

        [UnityTest]
        public IEnumerator LaRonda_CirculaSinSirena_YNoVeAlJugadorNiLoPersigue()
        {
            PatrullaMundo ronda = null;
            yield return EsperarRonda(p => ronda = p);
            Assert.IsTrue(ronda.EnRonda);
            Assert.AreEqual(0, gestor.Patrullas, "No es una patrulla de persecución");
            Vector3 antes = ronda.transform.position;
            Time.timeScale = EscalaDeTiempo;
            yield return Esperar(6f);
            Time.timeScale = 1f;
            Assert.Greater(Vector3.Distance(antes, ronda.transform.position), 3f, "Circula por las calles");
            Assert.IsTrue(ronda.EnRonda, "Sigue de ronda");
            Assert.IsFalse(ronda.VaDirecto, "No va hacia el jugador");
            Assert.IsFalse(ronda.VeAlJugador, "No sabe dónde está el jugador");
            Assert.AreEqual(0f, calor.Valor, 0.001f, "Sin delito no hay calor");
            Assert.IsFalse(ronda.AtiendeCriminal);
        }

        [UnityTest]
        public IEnumerator UnDelitoVistoPorLaRonda_SubeElCalor_YLaRondaPasaAPerseguir()
        {
            PatrullaMundo ronda = null;
            yield return EsperarRonda(p => ronda = p);
            jugador.transform.position = ronda.transform.position + ronda.transform.right * 12f;
            Physics.SyncTransforms();
            yield return null;
            Assert.IsTrue(ronda.VeElPunto(jugador.transform.position), "Control: la ronda ve al jugador");
            Registro<ICalor>.Actual.Sumar(20f, MotivoCalor.Pelea);
            Assert.AreEqual(20f, calor.Valor, 0.001f, "La ronda lo vio: el calor sube");
            yield return null;
            Assert.IsFalse(ronda.EnRonda, "Esa ronda deja la ronda");
            Assert.GreaterOrEqual(gestor.Patrullas, 1, "Y ahora es una patrulla de persecución");
        }

        [UnityTest]
        public IEnumerator UnCriminalAtacandoALaVista_HaceQueLaRondaVayaYBajePolicias()
        {
            PatrullaMundo ronda = null;
            yield return EsperarRonda(p => ronda = p);
            GameObject objeto = new GameObject("CriminalDoble");
            extras.Add(objeto);
            CriminalDoble criminal = objeto.AddComponent<CriminalDoble>();
            Registro<ICriminalCalle>.Agregar(criminal);
            Vector3 hacia = ronda.transform.position + ronda.transform.forward * 40f;
            hacia.y = 0f;
            objeto.transform.position = hacia;
            Physics.SyncTransforms();
            float limite = Time.time + 5f;
            while (Time.time < limite && !ronda.AtiendeCriminal)
            {
                yield return null;
            }
            Assert.IsTrue(ronda.AtiendeCriminal, "La ronda ve al criminal atacando");
            Time.timeScale = EscalaDeTiempo;
            limite = Time.time + 40f;
            while (Time.time < limite && !ronda.PoliciasBajaron)
            {
                yield return null;
            }
            Time.timeScale = 1f;
            Assert.IsTrue(ronda.PoliciasBajaron, "Llega y baja a sus policías");
            Assert.AreEqual(config.PoliciasPorPatrulla, ronda.Policias.Count);
            Assert.IsTrue(ronda.EnRonda, "Sigue siendo ronda: no hay búsqueda");
            Assert.AreEqual(0f, calor.Valor, 0.001f);
            foreach (GameObject p in ronda.Policias)
            {
                Object.DestroyImmediate(p);
            }
        }
    }
}

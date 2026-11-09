using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Peatones;
using MechaCyberpunk.Trafico.Persecucion;
using MechaCyberpunk.Trafico.Testigos;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// T-661 y T-933: la policía solo viene si te lo ganaste y ella te vio. Sin testigo no hay calor; una patrulla (o un policía a pie de una patrulla) con
    /// línea de vista lo suma al instante; un civil ya no cuenta; con la policía ya encima suma sin preguntar.
    /// </summary>
    public class TestigosT661Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";

        public sealed class CalorReal : ICalor
        {
            public float Valor { get; private set; }
            public float Maximo => 100f;
            public int Nivel => Valor >= 70f ? 3 : (Valor >= 40f ? 2 : (Valor >= 10f ? 1 : 0));
            public event Action<float> ValorCambio;
            public event Action<int> NivelCambio;

            private void Poner(float valor)
            {
                int antes = Nivel;
                Valor = Mathf.Clamp(valor, 0f, Maximo);
                ValorCambio?.Invoke(Valor);
                if (Nivel != antes)
                {
                    NivelCambio?.Invoke(Nivel);
                }
            }

            public void Sumar(float cantidad, MotivoCalor motivo) => Poner(Valor + cantidad);
            public void Enfriar(float cantidad) => Poner(Valor - cantidad);
            public void Borrar() => Poner(0f);
        }

        public sealed class JugadorDoble : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara => null;
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) { }
        }

        private readonly List<GameObject> extras = new List<GameObject>();
        private ConfigPersecucion config;
        private ConfigPeatones configPeatones;
        private CalorReal real;
        private GameObject jugador;
        private TestigosDelito testigos;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosRuido.LimpiarSuscriptores();
            config = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig));
            configPeatones = ScriptableObject.CreateInstance<ConfigPeatones>();
            Poner(configPeatones, "velocidad", 0.05f);
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(900f, 1f, 900f);
            extras.Add(piso);
            jugador = new GameObject("JugadorDePrueba");
            jugador.transform.position = new Vector3(100f, 0f, 100f);
            extras.Add(jugador);
            Registro<IJugador>.Agregar(new JugadorDoble { Raiz = jugador.transform });
            real = new CalorReal();
            Registro<ICalor>.Agregar(real);
            GameObject objeto = new GameObject("Testigos");
            objeto.SetActive(false);
            testigos = objeto.AddComponent<TestigosDelito>();
            testigos.Configurar(config);
            objeto.SetActive(true);
            extras.Add(objeto);
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
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(configPeatones);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosRuido.LimpiarSuscriptores();
        }

        private static void Poner(ScriptableObject so, string campo, float valor)
        {
            so.GetType().GetField(campo, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(so, valor);
        }

        private void Delito(float cantidad = 20f) => Registro<ICalor>.Actual.Sumar(cantidad, MotivoCalor.Pelea);

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator DelitoSinTestigos_NoHayCalor()
        {
            yield return null;
            Delito();
            yield return Esperar(0.3f);
            Assert.AreEqual(0f, real.Valor, 0.001f, "Nadie lo vio: no hay calor");
            Assert.AreEqual(1, testigos.DelitosSinTestigos);
            Assert.AreEqual(0, real.Nivel);
        }

        [UnityTest]
        public IEnumerator PatrullaConLineaDeVista_SumaCalorAlInstante_Y_ConPared_No()
        {
            yield return null;
            Vector3 p = jugador.transform.position;
            PatrullaMundo patrulla = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPatrulla).GetComponent<PatrullaMundo>());
            extras.Add(patrulla.gameObject);
            GrafoCalles grafo = GrafoCalles.DeManzanas(new[] { new Vector2(p.x - 47f, p.z - 47f), new Vector2(p.x + 47f, p.z - 47f), new Vector2(p.x - 47f, p.z + 47f), new Vector2(p.x + 47f, p.z + 47f) }, 80f, 0.2f);
            int nodo = 0;
            jugador.transform.position = grafo.Nodo(nodo) + new Vector3(40f, 0f, 0f);
            p = jugador.transform.position;
            Assert.Greater(Vector3.Distance(grafo.Nodo(nodo), p), 25f, "Control: hay un nodo a distancia media");
            patrulla.Preparar(config.Crear(), config, grafo, nodo, () => p);
            Physics.SyncTransforms();
            yield return null;
            Assert.IsTrue(patrulla.VeElPunto(p), "Control: la patrulla lo ve");
            GameObject pared = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pared.transform.position = (p + patrulla.transform.position) / 2f + Vector3.up * 5f;
            Vector3 eje = patrulla.transform.position - p;
            eje.y = 0f;
            pared.transform.rotation = Quaternion.LookRotation(eje.normalized, Vector3.up);
            pared.transform.localScale = new Vector3(40f, 12f, 1f);
            extras.Add(pared);
            Physics.SyncTransforms();
            Assert.IsFalse(patrulla.VeElPunto(p), "Con una pared en medio no lo ve");
            Delito();
            Assert.AreEqual(0f, real.Valor, 0.001f, "Tapada por la pared: sin testigo");
            Object.DestroyImmediate(pared);
            Physics.SyncTransforms();
            Delito();
            Assert.AreEqual(20f, real.Valor, 0.001f, "Sin pared la patrulla es testigo");
        }

        [UnityTest]
        public IEnumerator ConLaPoliciaYaEncima_YEnMisiones_ElCalorSumaSinTestigos()
        {
            yield return null;
            Registro<ICalor>.Actual.Sumar(15f, MotivoCalor.Mision);
            Assert.AreEqual(15f, real.Valor, 0.001f, "Las misiones suben calor sin testigos");
            Delito(5f);
            Assert.AreEqual(20f, real.Valor, 0.001f, "Con calor ya hay policía encima: suma sin preguntar");
        }

        [UnityTest]
        public IEnumerator MientrasTeBuscan_ElCalorNoBajaSolo_PeroUnEsconditeSiLoEnfria()
        {
            yield return null;
            Registro<ICalor>.Actual.Sumar(30f, MotivoCalor.Mision);
            Assert.AreEqual(1, real.Nivel);
            real.Enfriar(5f); // el enfriamiento natural del servicio de calor
            yield return null;
            yield return null;
            Assert.AreEqual(30f, real.Valor, 0.001f, "El calor no baja solo mientras te buscan");
            Registro<ICalor>.Actual.Enfriar(10f); // un escondite sí
            yield return null;
            yield return null;
            Assert.AreEqual(20f, real.Valor, 0.001f);
            Registro<ICalor>.Actual.Borrar();
            Assert.AreEqual(0f, real.Valor, 0.001f);
        }

        private sealed class PoliciaQueVe : ITestigoPolicia
        {
            public bool Ve;
            public bool VeElPunto(Vector3 punto) => Ve;
        }

        [UnityTest]
        public IEnumerator UnTestigoDePolicia_QueVe_SumaAlInstante_Y_QueNoVe_No()
        {
            yield return null;
            PoliciaQueVe testigo = new PoliciaQueVe();
            Registro<ITestigoPolicia>.Agregar(testigo);
            Delito();
            Assert.AreEqual(0f, real.Valor, 0.001f, "Hay un policía pero no ve: sin calor");
            testigo.Ve = true;
            Delito();
            Assert.AreEqual(20f, real.Valor, 0.001f, "El policía ve: el calor sube de una vez");
        }

        [UnityTest]
        public IEnumerator UnPoliciaAPie_DeUnaPatrulla_TambienVe()
        {
            yield return null;
            Vector3 p = jugador.transform.position;
            PatrullaMundo patrulla = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPatrulla).GetComponent<PatrullaMundo>());
            extras.Add(patrulla.gameObject);
            GrafoCalles grafo = GrafoCalles.DeManzanas(new[] { new Vector2(p.x - 47f, p.z - 47f), new Vector2(p.x + 47f, p.z - 47f), new Vector2(p.x - 47f, p.z + 47f), new Vector2(p.x + 47f, p.z + 47f) }, 80f, 0.2f);
            patrulla.Preparar(config.Crear(), config, grafo, 0, () => p);
            patrulla.transform.position = p + new Vector3(config.RadioVista + 60f, 0f, 0f);
            patrulla.BajarPolicias();
            Assert.Greater(patrulla.Policias.Count, 0, "Control: se bajó al menos un policía");
            Delito();
            Assert.AreEqual(0f, real.Valor, 0.001f, "La patrulla y sus policías están lejos: nadie ve");
            patrulla.Policias[0].transform.position = p + new Vector3(8f, 0f, 0f);
            Physics.SyncTransforms();
            Delito();
            Assert.AreEqual(20f, real.Valor, 0.001f, "Un policía a pie cerca del delito lo ve");
        }

        [UnityTest]
        public IEnumerator UnCivilCerca_YaNoCuentaComoTestigo()
        {
            yield return null;
            GameObject objeto = new GameObject("Civil");
            objeto.SetActive(false);
            objeto.transform.position = jugador.transform.position + new Vector3(8f, 0f, 0f);
            PeatonMundo civil = objeto.AddComponent<PeatonMundo>();
            civil.Preparar(new[] { objeto.transform.position, objeto.transform.position + new Vector3(400f, 0f, 0f) }, configPeatones, new System.Random(1), () => jugador.transform.position, 0f);
            objeto.SetActive(true);
            extras.Add(objeto);
            yield return null;
            Delito();
            yield return Esperar(0.5f);
            Assert.AreEqual(0f, real.Valor, 0.001f, "Un civil no llama a la policía: sin calor");
            Assert.AreEqual(1, testigos.DelitosSinTestigos);
        }
    }
}

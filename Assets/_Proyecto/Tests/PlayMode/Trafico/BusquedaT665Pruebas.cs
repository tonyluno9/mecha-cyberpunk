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
    /// T-665: la policía no se va sola. Te persigue hasta arrestarte o hasta que escapes saliendo de su vista y del círculo de búsqueda durante unos
    /// segundos; el calor no baja solo mientras tanto, y el círculo de búsqueda se registra para que el minimapa lo dibuje.
    /// </summary>
    public class BusquedaT665Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const string RutaDron = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        private const float Paso = 94f;
        private const float RadioBusqueda = 40f;
        private const float SegundosEscapar = 3f;
        private const float SegundosArresto = 1.5f;
        private const float EscalaDeTiempo = 4f;
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
        private Camera camara;
        private float escalaAnterior;
        private int arrestos;
        private int escapes;

        private void AlArrestar() => arrestos++;

        private void AlEscapar() => escapes++;

        private static void Poner(ScriptableObject so, string campo, float valor)
        {
            FieldInfo campoInfo = so.GetType().GetField(campo, BindingFlags.NonPublic | BindingFlags.Instance);
            campoInfo.SetValue(so, System.Convert.ChangeType(valor, campoInfo.FieldType));
        }

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPolicia.LimpiarSuscriptores();
            escalaAnterior = Time.timeScale;
            arrestos = 0;
            escapes = 0;
            EventosPolicia.JugadorArrestado += AlArrestar;
            EventosPolicia.JugadorEscapo += AlEscapar;
            config = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig));
            Poner(config, "radioBusqueda", RadioBusqueda);
            // T-729: sin que te vean dejas de ser buscado; las pruebas de arresto esperan a la patrulla quietas, así que ahí no hay escape.
            bool prueba_arresto = NUnit.Framework.TestContext.CurrentContext.Test.Name.Contains("Arresta") || NUnit.Framework.TestContext.CurrentContext.Test.Name.Contains("SiCorres");
            Poner(config, "segundosParaEscapar", prueba_arresto ? 9999f : SegundosEscapar);
            Poner(config, "segundosArresto", SegundosArresto);
            Poner(config, "distanciaArresto", 12f);
            Poner(config, "radioVistaDron", 1f);
            Poner(config, "camarasPorCruce", 0);
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
            GrafoCalles grafo = GrafoCalles.DeManzanas(Centros, 80f, 0.2f);
            jugador.transform.position = grafo.Nodo(0) + new Vector3(0f, -0.2f, 12f);
            Registro<IJugador>.Agregar(new TestigosT661Pruebas.JugadorDoble { Raiz = jugador.transform });
            calor = new TestigosT661Pruebas.CalorReal();
            Registro<ICalor>.Agregar(calor);
            GameObject camObjeto = new GameObject("CamaraPrueba");
            camObjeto.tag = "MainCamera";
            camara = camObjeto.AddComponent<Camera>();
            camara.fieldOfView = 60f;
            camara.farClipPlane = 800f;
            camObjeto.transform.position = new Vector3(0f, 1.7f, 0f);
            camObjeto.transform.rotation = Quaternion.Euler(-80f, 0f, 0f);
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
            EventosPolicia.LimpiarSuscriptores();
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
        }

        private void Encerrar(Vector3 centro)
        {
            const float Medio = 3f;
            Vector3[] lados = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            foreach (Vector3 lado in lados)
            {
                GameObject pared = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bool enX = Mathf.Abs(lado.x) > 0.5f;
                pared.transform.position = centro + lado * Medio + Vector3.up * 5f;
                pared.transform.localScale = enX ? new Vector3(0.5f, 12f, 2f * Medio + 1f) : new Vector3(2f * Medio + 1f, 12f, 0.5f);
                extras.Add(pared);
            }
            Physics.SyncTransforms();
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ElCirculoDeBusqueda_SeRegistraAlTenerCalor_YSeApagaSinElSinCalor()
        {
            yield return null;
            Assert.IsFalse(Registro<IZonaBusqueda>.Actual.Activa, "Sin calor no hay búsqueda");
            Registro<ICalor>.Actual.Sumar(60f, MotivoCalor.Mision);
            yield return null;
            IZonaBusqueda zona = Registro<IZonaBusqueda>.Actual;
            Assert.IsNotNull(zona);
            Assert.IsTrue(zona.Activa, "Con calor te buscan");
            Assert.AreEqual(RadioBusqueda, zona.Radio, 0.001f);
            Assert.Less(Vector3.Distance(zona.Centro, jugador.transform.position), 1f, "Nace donde estás");
            Registro<ICalor>.Actual.Borrar();
            yield return null;
            Assert.IsFalse(zona.Activa);
        }

        [UnityTest]
        public IEnumerator SinQueTeVeaNadie_AunDentroDelCirculo_TrasLosSegundosDejanDeBuscarte_YElCalorNoBajaSolo()
        {
            // T-729: antes de que pasen los segundos sin que te vean, el calor no baja solo; pasados, te dan por escapado aunque sigas en el círculo.
            yield return null;
            Encerrar(jugador.transform.position);
            Registro<ICalor>.Actual.Sumar(60f, MotivoCalor.Mision);
            yield return null;
            Time.timeScale = EscalaDeTiempo;
            float valor = calor.Valor;
            float fin = Time.time + SegundosEscapar * 0.6f;
            while (Time.time < fin)
            {
                calor.Enfriar(3f * Time.deltaTime); // el enfriamiento natural del servicio de calor
                yield return null;
            }
            Assert.Greater(calor.Nivel, 0, "Todavía no pasaron los segundos: te siguen buscando");
            Assert.GreaterOrEqual(calor.Valor, valor - 0.5f, "El calor no baja solo mientras te buscan");
            Assert.Greater(gestor.Patrullas, 0, "Las patrullas siguen ahí");
            Assert.AreEqual(0, escapes);
            fin = Time.time + SegundosEscapar;
            while (Time.time < fin)
            {
                yield return null;
            }
            Assert.AreEqual(0, calor.Nivel, "Nadie te vio durante los segundos: ya no te buscan, aunque sigas dentro del círculo");
            Assert.AreEqual(1, escapes);
        }

        [UnityTest]
        public IEnumerator FueraDeLaVistaYDelCirculo_TrasLosSegundos_EscapasYSeRetiran()
        {
            yield return null;
            Registro<ICalor>.Actual.Sumar(60f, MotivoCalor.Mision);
            yield return Esperar(0.5f);
            Assert.Greater(gestor.Patrullas, 0);
            // El jugador se aleja corriendo: el círculo lo sigue un instante (lo estaban viendo) y luego queda atrás, fuera de la vista de todos.
            jugador.transform.position += new Vector3(300f, 0f, 0f);
            yield return Esperar(0.3f);
            jugador.transform.position += new Vector3(120f, 0f, 0f);
            yield return null;
            Assert.Greater(calor.Nivel, 0, "Al instante todavía te buscan");
            yield return Esperar(SegundosEscapar * 0.5f);
            Assert.Greater(calor.Nivel, 0, "A la mitad del tiempo todavía te buscan");
            yield return Esperar(SegundosEscapar * 0.5f + 0.5f);
            Assert.AreEqual(0, calor.Nivel, "Pasados los segundos fuera de la vista y del círculo, escapas (fuera " + gestor.SegundosFueraDeLaZona + " s, sinVer " + gestor.SegundosSinVer + ", zona " + Registro<IZonaBusqueda>.Actual.Centro + " visto " + Registro<IZonaBusqueda>.Actual.Visto + ")");
            Assert.AreEqual(1, escapes);
            Assert.AreEqual(0, gestor.Patrullas, "Las patrullas se retiran");
            Assert.IsFalse(Registro<IZonaBusqueda>.Actual.Activa);
        }

        [UnityTest]
        public IEnumerator UnaPatrullaCerca_QueTeVe_ConElJugadorQuieto_TeArresta()
        {
            yield return null;
            Registro<ICalor>.Actual.Sumar(60f, MotivoCalor.Mision);
            Time.timeScale = EscalaDeTiempo;
            float fin = Time.time + 40f;
            while (Time.time < fin && arrestos == 0)
            {
                yield return null;
            }
            Assert.AreEqual(1, arrestos, "Una patrulla llegó, te vio quieto y te arrestó");
            Assert.AreEqual(0, calor.Nivel, "Al arrestarte el calor se borra");
            Assert.AreEqual(0, escapes);
        }

        [UnityTest]
        public IEnumerator SiCorres_LaPatrullaNoTeArresta()
        {
            yield return null;
            Registro<ICalor>.Actual.Sumar(60f, MotivoCalor.Mision);
            Time.timeScale = 1f;
            float fin = Time.time + 12f;
            Vector3 dir = Vector3.right;
            while (Time.time < fin)
            {
                // Corre 10 m/s de un lado a otro dentro de unos 60 m.
                if (jugador.transform.position.x > 60f) { dir = Vector3.left; }
                if (jugador.transform.position.x < -60f) { dir = Vector3.right; }
                jugador.transform.position += dir * 10f * Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual(0, arrestos, "En movimiento no te arrestan");
        }
    }
}

using System;
using System.Collections;
using System.Linq;
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
    /// T-666: las patrullas manejan más natural. Van por su carril (a la derecha de la calle), rodean un obstáculo que les cierra el carril, se cruzan en
    /// sentidos contrarios sin chocar y en una persecución de 30 s con tres patrullas nunca se encima una con otra.
    /// </summary>
    public class ConduccionPatrullasT666Pruebas
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

        private readonly List<GameObject> extras = new List<GameObject>();
        private ConfigPersecucion config;
        private float escalaAnterior;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            escalaAnterior = Time.timeScale;
            config = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig));
            Poner("segundosArresto", 9999f);
            Poner("radioVistaDron", 1f);
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(2000f, 1f, 2000f);
            extras.Add(piso);
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
            Object.DestroyImmediate(config);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
        }

        private void Poner(string campo, object valor)
        {
            FieldInfo info = typeof(ConfigPersecucion).GetField(campo, BindingFlags.NonPublic | BindingFlags.Instance);
            info.SetValue(config, Convert.ChangeType(valor, info.FieldType));
        }

        private PatrullaMundo CrearPatrulla(GrafoCalles grafo, int nodo, Func<Vector3?> donde)
        {
            PatrullaMundo patrulla = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPatrulla).GetComponent<PatrullaMundo>());
            extras.Add(patrulla.gameObject);
            patrulla.Preparar(config.Crear(), config, grafo, nodo, donde);
            return patrulla;
        }

        // Una calle recta del barrio: dos nodos vecinos a más de 60 m uno del otro.
        private static void CalleLarga(GrafoCalles grafo, out int a, out int b)
        {
            a = 0;
            b = grafo.Vecinos(0)[0];
            for (int i = 0; i < grafo.Cantidad; i++)
            {
                foreach (int v in grafo.Vecinos(i))
                {
                    if (Vector3.Distance(grafo.Nodo(i), grafo.Nodo(v)) > 70f)
                    {
                        a = i;
                        b = v;
                        return;
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator Patrulla_VaPorSuCarril_ADerechaDeLaCalle()
        {
            GrafoCalles grafo = GrafoCalles.DeManzanas(Centros, 80f, 0.2f);
            CalleLarga(grafo, out int a, out int b);
            Vector3 meta = grafo.Nodo(b);
            PatrullaMundo patrulla = CrearPatrulla(grafo, a, () => meta);
            Vector3 sentido = (grafo.Nodo(b) - grafo.Nodo(a)).normalized;
            Vector3 derecha = Vector3.Cross(Vector3.up, sentido);
            float fin = Time.time + 2f;
            while (Time.time < fin)
            {
                yield return null;
            }
            Vector3 desdeEje = patrulla.transform.position - grafo.Nodo(a);
            desdeEje.y = 0f;
            float lateral = Vector3.Dot(desdeEje, derecha);
            Assert.Greater(Vector3.Dot(desdeEje, sentido), 8f, "La patrulla avanzó por la calle");
            Assert.AreEqual(1.6f, lateral, 0.35f, "Va por el carril de la derecha, a 1.6 m del eje (fue " + lateral + ")");
            Assert.AreEqual(1.6f, patrulla.DesvioCarril, 0.01f);
        }

        [UnityTest]
        public IEnumerator Patrulla_RodeaUnObstaculoQueLeCierraElCarril_SinAtravesarlo()
        {
            GrafoCalles grafo = GrafoCalles.DeManzanas(Centros, 80f, 0.2f);
            CalleLarga(grafo, out int a, out int b);
            Vector3 inicio = grafo.Nodo(a);
            Vector3 sentido = (grafo.Nodo(b) - inicio).normalized;
            Vector3 derecha = Vector3.Cross(Vector3.up, sentido);
            Vector3 cajaEn = inicio + sentido * 30f + derecha * 1.6f;
            GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
            caja.transform.position = cajaEn + Vector3.up * 1f;
            caja.transform.localScale = new Vector3(2f, 2f, 2f);
            extras.Add(caja);
            Physics.SyncTransforms();
            Vector3 meta = grafo.Nodo(b);
            PatrullaMundo patrulla = CrearPatrulla(grafo, a, () => meta);
            float menorDistancia = float.MaxValue;
            float menorDesvio = float.MaxValue;
            float masLejos = 0f;
            float fin = Time.time + 9f;
            while (Time.time < fin)
            {
                yield return null;
                Vector3 p = patrulla.transform.position;
                Vector3 aLaCaja = p - cajaEn;
                aLaCaja.y = 0f;
                menorDistancia = Mathf.Min(menorDistancia, aLaCaja.magnitude);
                menorDesvio = Mathf.Min(menorDesvio, patrulla.DesvioCarril);
                masLejos = Mathf.Max(masLejos, Vector3.Dot(p - inicio, sentido));
            }
            Assert.Less(menorDesvio, -1f, "Cambió al carril de la izquierda al ver el obstáculo (desvío mínimo " + menorDesvio + ")");
            Assert.GreaterOrEqual(menorDistancia, 2.2f, "Pasó junto a la caja sin atravesarla (la rozó a " + menorDistancia + " m)");
            Assert.Greater(masLejos, 45f, "Y siguió adelante, más allá del obstáculo");
        }

        [UnityTest]
        public IEnumerator DosPatrullasEnSentidosContrarios_SePasanPorSusCarrilesSinEncimarse()
        {
            GrafoCalles grafo = GrafoCalles.DeManzanas(Centros, 80f, 0.2f);
            CalleLarga(grafo, out int a, out int b);
            Vector3 desdeA = grafo.Nodo(a);
            Vector3 desdeB = grafo.Nodo(b);
            PatrullaMundo ida = CrearPatrulla(grafo, a, () => desdeB);
            PatrullaMundo vuelta = CrearPatrulla(grafo, b, () => desdeA);
            float menor = float.MaxValue;
            float recorridoIda = 0f;
            float fin = Time.time + 8f;
            Vector3 sentido = (desdeB - desdeA).normalized;
            while (Time.time < fin)
            {
                yield return null;
                menor = Mathf.Min(menor, Vector3.Distance(ida.transform.position, vuelta.transform.position));
                recorridoIda = Mathf.Max(recorridoIda, Vector3.Dot(ida.transform.position - desdeA, sentido));
            }
            Assert.GreaterOrEqual(menor, 2.5f, "Al cruzarse van por carriles distintos y no se encima una con la otra (" + menor + " m)");
            Assert.Greater(recorridoIda, 50f, "Ninguna se quedó frenada esperando a la otra");
        }

        [UnityTest]
        public IEnumerator EnUnaPersecucionDe30s_TresPatrullas_NuncaSeEncimanNiSeChocan()
        {
            GameObject barrio = new GameObject("BarrioPrueba");
            extras.Add(barrio);
            for (int i = 0; i < Centros.Length; i++)
            {
                GameObject m = new GameObject("Manzana_" + (i + 1));
                m.transform.SetParent(barrio.transform, false);
                m.transform.position = new Vector3(Centros[i].x, 0f, Centros[i].y);
            }
            GrafoCalles grafo = GrafoCalles.DeManzanas(Centros, 80f, 0.2f);
            GameObject jugador = new GameObject("JugadorDePrueba");
            extras.Add(jugador);
            jugador.transform.position = grafo.Nodo(0) + new Vector3(0f, -0.2f, 12f);
            Registro<IJugador>.Agregar(new TestigosT661Pruebas.JugadorDoble { Raiz = jugador.transform });
            TestigosT661Pruebas.CalorReal calor = new TestigosT661Pruebas.CalorReal();
            Registro<ICalor>.Agregar(calor);
            GameObject camObjeto = new GameObject("CamaraPrueba");
            camObjeto.tag = "MainCamera";
            Camera camara = camObjeto.AddComponent<Camera>();
            camObjeto.transform.position = new Vector3(0f, 1.7f, 0f);
            camObjeto.transform.rotation = Quaternion.Euler(-80f, 0f, 0f);
            extras.Add(camObjeto);
            GameObject objeto = new GameObject("Persecucion");
            objeto.SetActive(false);
            objeto.transform.SetParent(barrio.transform, false);
            GestorPersecucion gestor = objeto.AddComponent<GestorPersecucion>();
            gestor.Configurar(config, AssetDatabase.LoadAssetAtPath<GameObject>(RutaDron).GetComponent<DronMundo>(),
                AssetDatabase.LoadAssetAtPath<GameObject>(RutaPatrulla).GetComponent<PatrullaMundo>());
            objeto.SetActive(true);
            yield return null;
            Registro<ICalor>.Actual.Sumar(100f, MotivoCalor.Mision);
            Time.timeScale = EscalaDeTiempo;
            yield return null;
            yield return null;
            Assert.AreEqual(3, calor.Nivel);
            float menor = float.MaxValue;
            float maximoJuntas = 0f;
            float fin = Time.time + 30f;
            int cuadros = 0;
            while (Time.time < fin)
            {
                yield return null;
                cuadros++;
                IReadOnlyList<PatrullaMundo> todas = PatrullaMundo.Activas.Where(r => !r.EnRonda).ToList(); // T-933: las rondas no cuentan
                for (int i = 0; i < todas.Count; i++)
                {
                    for (int j = i + 1; j < todas.Count; j++)
                    {
                        float d = Vector3.Distance(todas[i].transform.position, todas[j].transform.position);
                        menor = Mathf.Min(menor, d);
                    }
                }
                maximoJuntas = Mathf.Max(maximoJuntas, todas.Count);
            }
            Assert.AreEqual(3f, maximoJuntas, "Hubo tres patrullas en la persecución");
            Assert.Greater(cuadros, 100);
            Assert.GreaterOrEqual(menor, 3f, "En 30 s de persecución dos patrullas nunca estuvieron a menos de 3 m una de otra (la más cerca: " + menor + " m)");
        }
    }
}

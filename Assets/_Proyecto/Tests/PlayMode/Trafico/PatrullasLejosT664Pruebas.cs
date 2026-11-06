using System.Collections;
using System.Collections.Generic;
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
    /// T-664: las patrullas nunca aparecen de la nada a la vista del jugador. Nacen lejos y fuera del frustum de la cámara y llegan manejando; si no hay
    /// ningún punto de salida sin verse, esperan hasta que lo haya.
    /// </summary>
    public class PatrullasLejosT664Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const string RutaDron = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        private const float Paso = 94f;
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

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            config = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig));
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
            jugador = new GameObject("JugadorDePrueba");
            jugador.transform.position = new Vector3(0f, 0f, 0f);
            Registro<IJugador>.Agregar(new TestigosT661Pruebas.JugadorDoble { Raiz = jugador.transform });
            calor = new TestigosT661Pruebas.CalorReal();
            Registro<ICalor>.Agregar(calor);
            GameObject camObjeto = new GameObject("CamaraPrueba");
            camObjeto.tag = "MainCamera";
            camara = camObjeto.AddComponent<Camera>();
            camara.fieldOfView = 100f;
            camara.aspect = 16f / 9f;
            camara.farClipPlane = 800f;
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

        private void MirarDesdeArriba()
        {
            camara.transform.position = new Vector3(0f, 260f, 0f);
            camara.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private void MirarAlCielo()
        {
            camara.transform.position = new Vector3(0f, 1.7f, 0f);
            camara.transform.rotation = Quaternion.Euler(-80f, 0f, 0f);
        }

        [UnityTest]
        public IEnumerator SiTodasLasSalidasSeVen_LaPatrullaEspera_ySaleCuandoNoSeVe()
        {
            MirarDesdeArriba();
            yield return null;
            calor.Sumar(60f, MotivoCalor.Mision);
            yield return null;
            float fin = Time.time + 1.2f;
            while (Time.time < fin)
            {
                yield return null;
            }
            Assert.AreEqual(0, gestor.Patrullas, "Con todas las salidas a la vista no sale ninguna patrulla de la nada");
            Assert.AreEqual(0, gestor.GetComponentsInChildren<PatrullaMundo>().Length);

            MirarAlCielo();
            fin = Time.time + 1.2f;
            while (Time.time < fin && gestor.Patrullas < calor.Nivel)
            {
                yield return null;
            }
            Assert.Greater(gestor.Patrullas, 0, "Cuando ninguna salida se ve, las patrullas salen");
            foreach (PatrullaMundo p in gestor.GetComponentsInChildren<PatrullaMundo>())
            {
                Vector3 d = p.transform.position - jugador.transform.position;
                d.y = 0f;
                Assert.GreaterOrEqual(d.magnitude, config.DistanciaMinimaSalida, "Salió lejos del jugador");
                Assert.IsFalse(ReglaVistaPolicia.LoVe(camara, p.transform.position, 400f), "Salió fuera del frustum de la cámara");
            }
        }

        [UnityTest]
        public IEnumerator LaPatrulla_LlegaManejando_SinTeletransportes()
        {
            MirarAlCielo();
            yield return null;
            calor.Sumar(60f, MotivoCalor.Mision);
            yield return null;
            yield return null;
            PatrullaMundo[] patrullas = gestor.GetComponentsInChildren<PatrullaMundo>();
            Assert.Greater(patrullas.Length, 0);
            Vector3[] anterior = new Vector3[patrullas.Length];
            float[] inicial = new float[patrullas.Length];
            for (int i = 0; i < patrullas.Length; i++)
            {
                anterior[i] = patrullas[i].transform.position;
                inicial[i] = Vector3.Distance(anterior[i], jugador.transform.position);
            }
            float maximoPorCuadro = 0f;
            float fin = Time.time + 4f;
            while (Time.time < fin)
            {
                yield return null;
                for (int i = 0; i < patrullas.Length; i++)
                {
                    Vector3 p = patrullas[i].transform.position;
                    maximoPorCuadro = Mathf.Max(maximoPorCuadro, Vector3.Distance(p, anterior[i]) / Mathf.Max(Time.deltaTime, 0.0001f));
                    anterior[i] = p;
                }
            }
            Assert.LessOrEqual(maximoPorCuadro, config.Crear().Paso(false, 1000f, 1f) * 1.7f, "Nunca se mueve mucho más rápido que su velocidad (con el cambio de carril en las esquinas): viene manejando, no aparece de golpe");
            float cerca = float.MaxValue;
            for (int i = 0; i < patrullas.Length; i++)
            {
                cerca = Mathf.Min(cerca, Vector3.Distance(patrullas[i].transform.position, jugador.transform.position));
            }
            float lejosAntes = float.MaxValue;
            foreach (float d in inicial)
            {
                lejosAntes = Mathf.Min(lejosAntes, d);
            }
            Assert.Less(cerca, lejosAntes, "Se acercan al jugador por las calles");
        }
    }
}

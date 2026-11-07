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
    /// T-708: al subir la búsqueda la patrulla nace fuera de la vista (con el margen del pool del tráfico) y llega manejando por las calles con un tiempo real:
    /// nunca aparece junto al jugador ni llega al instante.
    /// </summary>
    public class PatrullaLlegaT708Pruebas
    {
        private const string RutaDron = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        private const float Paso = 94f;
        private const float DistanciaLlegada = 12f;
        private const float EscalaDeTiempo = 5f;
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

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            escalaAnterior = Time.timeScale;
            config = ConfigPersecucionPrueba.SinArresto();
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
            jugador.transform.position = GrafoCalles.DeManzanas(Centros, 80f, 0.2f).Nodo(0) + new Vector3(0f, -0.2f, 0f); // en una esquina de las calles
            Registro<IJugador>.Agregar(new TestigosT661Pruebas.JugadorDoble { Raiz = jugador.transform });
            calor = new TestigosT661Pruebas.CalorReal();
            Registro<ICalor>.Agregar(calor);
            GameObject camObjeto = new GameObject("CamaraPrueba");
            camObjeto.tag = "MainCamera";
            camara = camObjeto.AddComponent<Camera>();
            camara.fieldOfView = 70f;
            camara.aspect = 16f / 9f;
            camara.farClipPlane = 800f;
            camObjeto.transform.position = jugador.transform.position + Vector3.up * 1.7f;
            camObjeto.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
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
        }

        private static float Plano(Vector3 a, Vector3 b)
        {
            a.y = b.y;
            return Vector3.Distance(a, b);
        }

        [UnityTest]
        public IEnumerator Patrulla_NaceFueraDeLaVista_YTardaUnTiempoRealEnLlegar()
        {
            yield return null;
            calor.Sumar(15f, MotivoCalor.Mision);
            yield return null;
            PatrullaMundo patrulla = gestor.GetComponentInChildren<PatrullaMundo>();
            Assert.IsNotNull(patrulla, "Con el calor sube una patrulla");
            Vector3 salida = patrulla.transform.position;
            Assert.IsFalse(CarroTrafico.PuntoEnVista(camara, salida), "Nace fuera de la vista (con margen)");
            Assert.IsFalse(ReglaVistaPolicia.LoVe(camara, salida, 400f), "Nace fuera del frustum");
            Assert.GreaterOrEqual(Plano(salida, jugador.transform.position), config.DistanciaMinimaSalida, "Nace lejos del jugador");

            float inicio = Time.time;
            float limite = inicio + 120f;
            Time.timeScale = EscalaDeTiempo;
            float visibleA = -1f;
            while (Time.time < limite && Plano(patrulla.transform.position, jugador.transform.position) > DistanciaLlegada)
            {
                if (visibleA < 0f && CarroTrafico.PuntoEnVista(camara, patrulla.transform.position))
                {
                    visibleA = Plano(patrulla.transform.position, jugador.transform.position);
                }
                yield return null;
            }
            float segundos = Time.time - inicio;
            Time.timeScale = 1f;
            Assert.LessOrEqual(Plano(patrulla.transform.position, jugador.transform.position), DistanciaLlegada, "La patrulla llega manejando");
            Assert.GreaterOrEqual(segundos, config.SegundosMinimosLlegadaPatrulla * 0.8f, "Tarda un tiempo real en llegar, no aparece junto al jugador");
            Debug.Log("[T-708] salió a " + Plano(salida, jugador.transform.position).ToString("F0") + " m y llegó en " + segundos.ToString("F1") + " s; se vio a " + visibleA.ToString("F0") + " m");
        }
    }
}

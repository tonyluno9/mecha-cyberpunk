using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Calle;
using NUnit.Framework;
using UnityEditor;
using MechaCyberpunk.Trafico.Datos;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// T-588: el tráfico de la ciudad real (Ciudad_Prologo) corre dos minutos y medio de juego y se mide: ningún par de carros se encima
    /// (choque tonto) y ningún carro se queda atorado mucho rato (se destraba solo).
    /// </summary>
    public class TraficoFluidoT588Pruebas
    {
        private const string RutaPrefab = "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico.prefab";
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigTraficoCalle_Trafico.asset";

        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float SegundosDeJuego = 150f;
        private const float EscalaDeTiempo = 4f;
        private const float DistanciaDeChoque = 2.4f;
        private const float VelocidadDeAtorado = 0.3f;
        private const float SegundosDeAtoradoMaximos = 35f;

        private float escalaAnterior;

        [SetUp]
        public void Preparar()
        {
            escalaAnterior = Time.timeScale;
        }

        [TearDown]
        public void Limpiar()
        {
            Time.timeScale = escalaAnterior;
        }

        // Un carro detrás de una caja fija (un carro estacionado en su carril, una obra): tras unos segundos sin que nadie lo vea vuelve a salir en otra calle.
        [UnityTest]
        public IEnumerator Carro_DetrasDeAlgoFijo_SeDestrabaYSaleEnOtraCalle_YNoEncimaDeOtro()
        {
            Registros.LimpiarTodos();
            ConfigTraficoCalle config = AssetDatabase.LoadAssetAtPath<ConfigTraficoCalle>(RutaConfig);
            CarroTrafico prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab).GetComponent<CarroTrafico>();
            List<Vector2> centros = new List<Vector2> { new Vector2(0f, 0f), new Vector2(94f, 0f), new Vector2(0f, 94f), new Vector2(94f, 94f) };
            MechaCyberpunk.Trafico.Persecucion.GrafoCalles calles = MechaCyberpunk.Trafico.Persecucion.GrafoCalles.DeManzanas(centros, 80f, 0.2f);
            TraficoCalle.AlargarHaciaLasAfueras(calles);
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(47f, -0.5f, 47f);
            piso.transform.localScale = new Vector3(900f, 1f, 900f);
            GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Vector3 jugadorLejos = new Vector3(5000f, 0f, 5000f);
            CarroTrafico carro = Object.Instantiate(prefab);
            try
            {
                Vector3 lejos = jugadorLejos;
                carro.Preparar(config, calles, calles.MasCercano(new Vector3(47f, 0f, 0f)), new System.Random(7), () => lejos);
                Physics.SyncTransforms();
                caja.transform.position = carro.transform.position + carro.transform.forward * 9f + Vector3.up * 0.9f;
                caja.transform.localScale = new Vector3(2.4f, 1.6f, 2.4f);
                Physics.SyncTransforms();
                Vector3 inicio = carro.transform.position;
                Time.timeScale = 3f;
                float limite = Time.time + 30f;
                bool salio = false;
                while (Time.time < limite && !salio)
                {
                    yield return null;
                    Vector3 d = carro.transform.position - inicio;
                    d.y = 0f;
                    salio = d.magnitude > 40f;
                }
                Assert.IsTrue(salio, "El carro se destraba y sale en otra parte (causa: " + carro.Causa + ")");
                Assert.Greater(Vector3.Distance(carro.transform.position, caja.transform.position), 20f);
                Assert.AreEqual("libre", carro.Causa, "Ya no tiene nada adelante");
            }
            finally
            {
                Object.DestroyImmediate(carro.gameObject);
                Object.DestroyImmediate(caja);
                Object.DestroyImmediate(piso);
                Registros.LimpiarTodos();
                CarroTrafico.Activos.Clear();
            }
        }

        // Los carros aparecen (y se retiran al final de las calles de las afueras) fuera de la vista de la cámara y lejos del jugador.
        [UnityTest]
        public IEnumerator Carro_AlAparecer_SaleFueraDeLaVistaYLejosDelJugador()
        {
            Registros.LimpiarTodos();
            ConfigTraficoCalle config = AssetDatabase.LoadAssetAtPath<ConfigTraficoCalle>(RutaConfig);
            CarroTrafico prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab).GetComponent<CarroTrafico>();
            List<Vector2> centros = new List<Vector2> { new Vector2(0f, 0f), new Vector2(94f, 0f), new Vector2(0f, 94f), new Vector2(94f, 94f) };
            MechaCyberpunk.Trafico.Persecucion.GrafoCalles calles = MechaCyberpunk.Trafico.Persecucion.GrafoCalles.DeManzanas(centros, 80f, 0.2f);
            TraficoCalle.AlargarHaciaLasAfueras(calles);
            Vector3 jugador = new Vector3(47f, 0f, 47f);
            GameObject camObjeto = new GameObject("CamaraPrueba");
            camObjeto.tag = "MainCamera";
            Camera cam = camObjeto.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            camObjeto.transform.SetPositionAndRotation(jugador + Vector3.up * 1.7f, Quaternion.LookRotation(Vector3.right, Vector3.up));
            CarroTrafico carro = Object.Instantiate(prefab);
            try
            {
                carro.Preparar(config, calles, calles.MasCercano(jugador), new System.Random(11), () => jugador);
                yield return null;
                for (int i = 0; i < 25; i++)
                {
                    Assert.IsTrue(carro.Reaparecer(false, true), "Encuentra dónde salir");
                    Vector3 p = carro.transform.position;
                    Assert.IsFalse(MechaCyberpunk.Trafico.Persecucion.ReglaVistaPolicia.LoVe(cam, p, 160f), "Salió a la vista en " + p);
                    Vector3 d = p - jugador;
                    d.y = 0f;
                    Assert.GreaterOrEqual(d.magnitude, 45f, "Salió cerca del jugador en " + p);
                }
            }
            finally
            {
                Object.DestroyImmediate(carro.gameObject);
                Object.DestroyImmediate(camObjeto);
                Registros.LimpiarTodos();
                CarroTrafico.Activos.Clear();
            }
        }

        [UnityTest]
        public IEnumerator Ciudad_DosMinutosYMedio_SinChoquesNiAtorones()
        {
            Registros.LimpiarTodos();
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(RutaEscena,
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            yield return null;
            TraficoCalle trafico = null;
            float espera = Time.realtimeSinceStartup + 30f;
            while ((trafico = Object.FindFirstObjectByType<TraficoCalle>()) == null || trafico.Carros.Count == 0 || Registro<IJugador>.Actual == null)
            {
                Assert.Less(Time.realtimeSinceStartup, espera, "La ciudad trae el tráfico de calle");
                yield return null;
            }
            yield return new WaitForSecondsRealtime(1f);
            Time.timeScale = EscalaDeTiempo;
            Dictionary<CarroTrafico, float> quietos = new Dictionary<CarroTrafico, float>();
            float maximoAtorado = 0f;
            int choques = 0;
            string ejemplo = "";
            float inicio = Time.time;
            float limite = inicio + SegundosDeJuego;
            while (Time.time < limite)
            {
                yield return null;
                IReadOnlyList<CarroTrafico> carros = trafico.Carros;
                for (int i = 0; i < carros.Count; i++)
                {
                    CarroTrafico a = carros[i];
                    if (a == null || !a.gameObject.activeSelf || a.Robado)
                    {
                        continue;
                    }
                    float t = a.Velocidad < VelocidadDeAtorado ? (quietos.TryGetValue(a, out float previo) ? previo : 0f) + Time.deltaTime : 0f;
                    quietos[a] = t;
                    maximoAtorado = Mathf.Max(maximoAtorado, t);
                    for (int j = i + 1; j < carros.Count; j++)
                    {
                        CarroTrafico b = carros[j];
                        if (b == null || !b.gameObject.activeSelf || b.Robado)
                        {
                            continue;
                        }
                        Vector3 d = a.transform.position - b.transform.position;
                        d.y = 0f;
                        if (d.magnitude < DistanciaDeChoque)
                        {
                            choques++;
                            if (ejemplo.Length == 0)
                            {
                                ejemplo = a.name + " y " + b.name + " en " + a.transform.position + " a los " + (Time.time - inicio).ToString("F1") + " s";
                            }
                        }
                    }
                }
            }
            foreach (KeyValuePair<CarroTrafico, float> par in quietos)
            {
                if (par.Value > 8f)
                {
                    CarroTrafico c = par.Key;
                    string cerca = "";
                    foreach (Collider col in Physics.OverlapSphere(c.transform.position + c.transform.forward * 4f + Vector3.up, 3f))
                    {
                        cerca += col.name + "(" + col.transform.root.name + ") ";
                    }
                    Debug.Log("[T-588] quieto " + c.name + " " + par.Value.ToString("F0") + " s en " + c.transform.position + " bloqueado " + c.Bloqueado + " causa " + c.Causa + " cerca: " + cerca);
                }
            }
            Debug.Log("[T-588] carros " + trafico.Vivos + " choques(frames) " + choques + " maximoAtorado " + maximoAtorado.ToString("F1") + " s. " + ejemplo);
            Assert.AreEqual(0, choques, "Carros encimados: " + ejemplo);
            Assert.LessOrEqual(maximoAtorado, SegundosDeAtoradoMaximos, "Un carro se atoró " + maximoAtorado + " s");
        }
    }
}

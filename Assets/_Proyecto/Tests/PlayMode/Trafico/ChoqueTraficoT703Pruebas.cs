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
    /// T-703: un carro del tráfico que choca se queda detenido (y pita) y el conductor reacciona; si fue fuerte se baja enojado y luego vuelve.
    /// También: los carros del tráfico van a una velocidad normal (dentro del rango de la config), cada uno a la suya.
    /// </summary>
    public class ChoqueTraficoT703Pruebas
    {
        private const string RutaPrefab = "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V09.prefab";
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigTraficoCalle_Trafico.asset";

        private GameObject barrio;
        private GameObject piso;
        private ConfigTraficoCalle config;
        private CarroTrafico prefab;
        private readonly List<GameObject> extras = new List<GameObject>();

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            EventosRuido.LimpiarSuscriptores();
            CarroTrafico.Activos.Clear();
            config = AssetDatabase.LoadAssetAtPath<ConfigTraficoCalle>(RutaConfig);
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab).GetComponent<CarroTrafico>();
            Assert.IsNotNull(config);
            Assert.IsNotNull(prefab);
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

        private CarroTrafico CrearCarro(int semilla, Func<Vector3?> jugador)
        {
            List<Vector2> centros = new List<Vector2> { new Vector2(0f, 0f), new Vector2(80f, 0f), new Vector2(0f, 80f), new Vector2(80f, 80f) };
            GrafoCalles calles = GrafoCalles.DeManzanas(centros, 80f, 0.2f);
            CarroTrafico carro = Object.Instantiate(prefab);
            extras.Add(carro.gameObject);
            carro.Preparar(config, calles, calles.MasCercano(new Vector3(40f, 0f, -40f)), new System.Random(semilla), jugador);
            return carro;
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        // Cuánto se movió (plano) el carro mientras pasan los segundos.
        private static IEnumerator Recorrido(CarroTrafico carro, float segundos, Action<float> resultado)
        {
            Vector3 inicio = carro.transform.position;
            yield return Esperar(segundos);
            Vector3 d = carro.transform.position - inicio;
            d.y = 0f;
            resultado(d.magnitude);
        }

        // Una foto del carro y de quien se bajó, desde atrás y a un lado, para revisarla a ojo.
        private void CapturarA(Transform carro, string nombre)
        {
            GameObject luz = new GameObject("LuzDePrueba");
            extras.Add(luz);
            luz.AddComponent<Light>().type = LightType.Directional;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            GameObject c = new GameObject("CamaraDePrueba");
            extras.Add(c);
            Camera camara = c.AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.45f, 0.55f, 0.7f);
            c.transform.position = carro.position - carro.forward * 7f + carro.right * 5f + Vector3.up * 2.6f;
            c.transform.LookAt(carro.position + Vector3.up * 0.9f);
            CapturaPrueba.Guardar(camara, nombre);
        }

        [UnityTest]
        public IEnumerator ChoqueLeve_SeQuedaQuietoPitaYLuegoSigue()
        {
            CarroTrafico carro = CrearCarro(3, () => null);
            yield return Esperar(2.5f);
            Assert.Greater(carro.Velocidad, 5f, "Antes del choque anda a buena velocidad");
            int pitidos = carro.Pitidos;
            NivelChoque nivel = carro.AlChocar(5f, carro.transform.position + carro.transform.forward);
            Assert.AreEqual(NivelChoque.Leve, nivel);
            Assert.IsTrue(carro.Chocado);
            Assert.AreEqual(0f, carro.Velocidad, 0.01f, "Se para en seco");
            float movido = 0f;
            yield return Recorrido(carro, ReglaChoqueTrafico.SegundosLeve - 0.8f, m => movido = m);
            Assert.Less(movido, 0.1f, "Se queda detenido unos segundos y no sigue como si nada");
            Assert.Greater(carro.Pitidos, pitidos, "Pita");
            Assert.IsNull(carro.ConductorAfuera, "Un choque leve no baja al conductor");
            yield return Esperar(4f);
            Assert.IsFalse(carro.Chocado);
            Assert.Greater(carro.Velocidad, 2f, "Después sigue su camino");
        }

        [UnityTest]
        public IEnumerator ChoqueFuerte_ElConductorSeBajaEnojadoYVuelveAlCarro()
        {
            CarroTrafico carro = CrearCarro(4, () => null);
            yield return Esperar(2f);
            carro.AlChocar(14f, carro.transform.position + carro.transform.right * 6f);
            Assert.AreEqual(NivelChoque.Fuerte, carro.UltimoChoque);
            ConductorEnojado conductor = carro.ConductorAfuera;
            Assert.IsNotNull(conductor, "El conductor se baja");
            yield return Esperar(1.5f);
            Assert.IsTrue(conductor != null, "Sigue afuera");
            Vector3 d = conductor.transform.position - carro.transform.position;
            d.y = 0f;
            Assert.Less(d.magnitude, 4f, "Se queda junto al carro, no huye");
            Assert.Greater(conductor.Gestos, 0, "Le reclama con un gesto");
            CapturarA(carro.transform, "s1c-T-703-conductor-enojado.png");
            Assert.Less(carro.Velocidad, 0.01f, "El carro sigue detenido");
            yield return Esperar(ReglaChoqueTrafico.SegundosConductorAfuera);
            Assert.IsTrue(conductor == null, "Pasado el tiempo vuelve al carro (desaparece)");
            yield return Esperar(ReglaChoqueTrafico.SegundosFuerte - ReglaChoqueTrafico.SegundosConductorAfuera - 1.5f + 3f);
            Assert.IsFalse(carro.Chocado);
            Assert.Greater(carro.Velocidad, 2f, "Y el carro sigue");
        }

        [UnityTest]
        public IEnumerator ChoqueReal_UnCarroPesadoLoGolpeaYReacciona()
        {
            // Un "jugador" 8 m adelante lo deja parado; el golpe viene de atrás con un cuerpo pesado a 16 m/s.
            Vector3 jugadorPos = Vector3.zero;
            CarroTrafico carro = CrearCarro(5, () => jugadorPos);
            yield return null;
            jugadorPos = carro.transform.position + carro.transform.forward * 8f;
            yield return Esperar(4f);
            Assert.Less(carro.Velocidad, 0.3f, "Parado ante el obstáculo");
            Assert.AreEqual(0, carro.Choques);
            jugadorPos = carro.transform.position + carro.transform.forward * 300f;
            GameObject bala = GameObject.CreatePrimitive(PrimitiveType.Cube);
            extras.Add(bala);
            bala.transform.localScale = new Vector3(1.6f, 1f, 3f);
            bala.transform.SetPositionAndRotation(carro.transform.position - carro.transform.forward * 4.5f + Vector3.up * 0.8f, carro.transform.rotation);
            Rigidbody rb = bala.AddComponent<Rigidbody>();
            rb.mass = 1500f;
            rb.useGravity = false;
            rb.linearVelocity = carro.transform.forward * 16f;
            float fin = Time.time + 3f;
            while (Time.time < fin && carro.Choques == 0)
            {
                yield return null;
            }
            Assert.GreaterOrEqual(carro.Choques, 1, "El golpe real del otro carro lo hace reaccionar");
            Assert.AreNotEqual(NivelChoque.Ninguno, carro.UltimoChoque);
        }

        [UnityTest]
        public IEnumerator Velocidades_CadaCarroVaALaSuyaYNingunoMuyLento()
        {
            GameObject raiz = new GameObject("TraficoDePrueba");
            extras.Add(raiz);
            TraficoCalle trafico = raiz.AddComponent<TraficoCalle>();
            trafico.Configurar(config, prefab, barrio.transform);
            yield return null;
            yield return null;
            Assert.Greater(trafico.Carros.Count, 3);
            float menor = float.MaxValue;
            float mayor = 0f;
            foreach (CarroTrafico c in trafico.Carros)
            {
                menor = Mathf.Min(menor, c.VelocidadPropia);
                mayor = Mathf.Max(mayor, c.VelocidadPropia);
                Assert.GreaterOrEqual(c.VelocidadPropia, config.VelocidadMinima - 0.001f, c.name + " no va tan lento");
                Assert.LessOrEqual(c.VelocidadPropia, config.VelocidadMaxima + 0.001f);
            }
            Assert.Greater(mayor - menor, 0.5f, "No todos van a la misma velocidad");
        }

        [UnityTest]
        public IEnumerator UnCarroLibre_AlcanzaSuVelocidadNormal()
        {
            CarroTrafico carro = CrearCarro(9, () => null);
            float maxima = 0f;
            float fin = Time.time + 6f;
            while (Time.time < fin)
            {
                maxima = Mathf.Max(maxima, carro.Velocidad);
                yield return null;
            }
            Assert.GreaterOrEqual(maxima, config.VelocidadMinima - 0.5f, "Llega a una velocidad normal, máxima " + maxima.ToString("F1"));
        }
    }
}

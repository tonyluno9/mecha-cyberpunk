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
    /// T-1603 con el prefab real en redes chicas armadas aquí: un carro que va detrás de otro detenido guarda su distancia y no lo choca; dos carros que llegan a la vez
    /// a un cruce por calles distintas no están adentro al mismo tiempo; un carro atorado por un obstáculo sale en otro punto.
    /// </summary>
    public class TraficoOrdenadoT1603Pruebas
    {
        private const string RutaPrefab = "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico.prefab";
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigTraficoCalle_Trafico.asset";
        private const float Altura = 0.2f;
        private const float RadioNucleoDelCruce = 6f;

        private ConfigTraficoCalle config;
        private CarroTrafico prefab;
        private GameObject piso;
        private readonly List<GameObject> extras = new List<GameObject>();

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            EventosRuido.LimpiarSuscriptores();
            EstadisticasTrafico.Reiniciar();
            config = AssetDatabase.LoadAssetAtPath<ConfigTraficoCalle>(RutaConfig);
            Assert.IsNotNull(config, "Falta " + RutaConfig);
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab).GetComponent<CarroTrafico>();
            Assert.IsNotNull(prefab, "Falta " + RutaPrefab);
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(100f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(1000f, 1f, 1000f);
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
            Object.DestroyImmediate(piso);
            EventosRuido.LimpiarSuscriptores();
            Registros.LimpiarTodos();
            CarroTrafico.Activos.Clear();
            EstadisticasTrafico.Reiniciar();
        }

        private CarroTrafico Carro(GrafoCalles calles, int nodo, int semilla)
        {
            CarroTrafico carro = Object.Instantiate(prefab);
            extras.Add(carro.gameObject);
            carro.Preparar(config, calles, nodo, new System.Random(semilla), () => null);
            return carro;
        }

        // Prepara el carro con distintas semillas hasta que su primer destino sea el pedido (el nodo de salida puede tener varios vecinos).
        private CarroTrafico CarroHacia(GrafoCalles calles, int nodo, int destino)
        {
            CarroTrafico carro = Carro(calles, nodo, 1);
            for (int semilla = 2; semilla < 200 && carro.NodoDestino != destino; semilla++)
            {
                carro.Preparar(config, calles, nodo, new System.Random(semilla), () => null);
            }
            Assert.AreEqual(destino, carro.NodoDestino, "No pude apuntar el carro al nodo " + destino);
            return carro;
        }

        private static float Plano(Vector3 a, Vector3 b)
        {
            a.y = b.y;
            return Vector3.Distance(a, b);
        }

        [UnityTest]
        public IEnumerator DosCarrosEnElMismoCarril_ElDeAtrasGuardaDistanciaYNoChoca()
        {
            GrafoCalles calles = new GrafoCalles();
            int atras = calles.AgregarNodo(new Vector3(-40f, Altura, 0f));
            int origen = calles.AgregarNodo(new Vector3(0f, Altura, 0f));
            int medio = calles.AgregarNodo(new Vector3(120f, Altura, 0f));
            int fin = calles.AgregarNodo(new Vector3(260f, Altura, 0f));
            calles.Enlazar(atras, origen);
            calles.Enlazar(origen, medio);
            calles.Enlazar(medio, fin);
            // Una cámara que ve toda la calle: así el detenido no se recicla fuera de la vista ni el atorado sale a mitad de la prueba.
            GameObject camaraObjeto = new GameObject("CamaraPrueba");
            camaraObjeto.tag = "MainCamera";
            camaraObjeto.AddComponent<Camera>();
            camaraObjeto.transform.SetPositionAndRotation(new Vector3(60f, 150f, 0f), Quaternion.Euler(90f, 0f, 0f));
            extras.Add(camaraObjeto);
            CarroTrafico delante = CarroHacia(calles, origen, medio);
            CarroTrafico detras = Carro(calles, atras, 5);
            yield return null;
            delante.AlChocar(ReglaChoqueTrafico.UmbralFuerte + 1f, delante.transform.position); // se queda parado en seco unos segundos
            float menor = float.MaxValue;
            float final = 0f;
            float t = 0f;
            while (t < 7f)
            {
                yield return null;
                t += Time.deltaTime;
                EstadisticasTrafico.RevisarChoques(CarroTrafico.Activos, Time.time, true);
                menor = Mathf.Min(menor, Plano(delante.transform.position, detras.transform.position));
                final = Plano(delante.transform.position, detras.transform.position);
            }
            Assert.AreEqual(0, EstadisticasTrafico.Choques, "Los carros del tráfico no se tocan");
            Assert.GreaterOrEqual(menor, 4.5f + 5f, "Guarda al menos unos 5 m entre defensas (la parada de la config)");
            Assert.Less(detras.Velocidad, 1f, "Llegó junto al detenido y se quedó esperando");
            Assert.Less(final, 40f, "Y sí se acercó, no se quedó donde estaba");
        }

        [UnityTest]
        public IEnumerator DosCarrosPorCallesCruzadas_NoEstanALaVezEnElNucleoDelCruce()
        {
            GrafoCalles calles = new GrafoCalles();
            int centro = calles.AgregarNodo(new Vector3(0f, Altura, 0f));
            float velocidadA = ReglaChoqueTrafico.VelocidadDeCarro(config.VelocidadMinima, config.VelocidadMaxima, new System.Random(11).NextDouble());
            float velocidadB = ReglaChoqueTrafico.VelocidadDeCarro(config.VelocidadMinima, config.VelocidadMaxima, new System.Random(22).NextDouble());
            const float distanciaA = 70f;
            float llegadaA = distanciaA / velocidadA + velocidadA / (2f * 3f);
            float distanciaB = velocidadB * (llegadaA - velocidadB / (2f * 3f)); // llegan casi a la vez
            int oeste = calles.AgregarNodo(new Vector3(-distanciaA, Altura, 0f));
            int sur = calles.AgregarNodo(new Vector3(0f, Altura, -distanciaB));
            int este = calles.AgregarNodo(new Vector3(120f, Altura, 0f));
            int norte = calles.AgregarNodo(new Vector3(0f, Altura, 120f));
            calles.Enlazar(oeste, centro);
            calles.Enlazar(sur, centro);
            calles.Enlazar(centro, este);
            calles.Enlazar(centro, norte);
            CarroTrafico a = Carro(calles, oeste, 11);
            CarroTrafico b = Carro(calles, sur, 22);
            Vector3 puntoCentro = calles.Nodo(centro);
            bool pasoA = false;
            bool pasoB = false;
            bool juntos = false;
            float t = 0f;
            while (t < 25f && !(pasoA && pasoB))
            {
                yield return null;
                t += Time.deltaTime;
                EstadisticasTrafico.RevisarChoques(CarroTrafico.Activos, Time.time, true);
                bool aDentro = Plano(a.transform.position, puntoCentro) < RadioNucleoDelCruce;
                bool bDentro = Plano(b.transform.position, puntoCentro) < RadioNucleoDelCruce;
                juntos |= aDentro && bDentro;
                pasoA |= a.NodoDestino != centro;
                pasoB |= b.NodoDestino != centro;
            }
            Assert.IsFalse(juntos, "Nunca están los dos en el cruce al mismo tiempo");
            Assert.IsTrue(pasoA && pasoB, "Y los dos terminan pasando (nadie se queda trabado)");
            Assert.AreEqual(0, EstadisticasTrafico.Choques);
        }

        [UnityTest]
        public IEnumerator CarroAtoradoPorUnObstaculo_SaleEnOtroPunto()
        {
            GrafoCalles calles = new GrafoCalles();
            int n0 = calles.AgregarNodo(new Vector3(0f, Altura, 0f));
            int n1 = calles.AgregarNodo(new Vector3(100f, Altura, 0f));
            int n2 = calles.AgregarNodo(new Vector3(200f, Altura, 0f));
            int n3 = calles.AgregarNodo(new Vector3(300f, Altura, 0f));
            calles.Enlazar(n0, n1);
            calles.Enlazar(n1, n2);
            calles.Enlazar(n2, n3);
            GameObject obstaculo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            extras.Add(obstaculo);
            obstaculo.name = "ObstaculoFijo";
            obstaculo.transform.position = new Vector3(40f, 1f, -config.SeparacionCarril);
            obstaculo.transform.localScale = new Vector3(2f, 2f, 4f);
            CarroTrafico carro = Carro(calles, n0, 3);
            Vector3 lugarDelObstaculo = obstaculo.transform.position;
            bool atorado = false;
            bool salio = false;
            float t = 0f;
            float limite = config.SegundosParaDestrabar + 15f;
            while (t < limite && !salio)
            {
                yield return null;
                t += Time.deltaTime;
                float d = Plano(carro.transform.position, lugarDelObstaculo);
                atorado |= carro.Bloqueado && d < 20f;
                salio = atorado && d > 25f;
            }
            Assert.IsTrue(atorado, "Primero se atoró detrás del obstáculo");
            Assert.IsTrue(salio, "Y pasado el tiempo de destrabar salió en otro punto de la red");
        }
    }
}

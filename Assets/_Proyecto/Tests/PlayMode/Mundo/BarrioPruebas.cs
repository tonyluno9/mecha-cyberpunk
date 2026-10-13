using System.Collections;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Pruebas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>Estructura del barrio greybox (prefab), sus finales y un recorrido hasta el final con la cámara libre.</summary>
    public class BarrioPruebas : InputTestFixture
    {
        private const string RutaBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const string RutaCamara = "Assets/_Proyecto/Mundo/Prefabs/CamaraLibre.prefab";
        private const float AlturaRayo = 1f;
        private const int CantidadRayos = 64;
        private const float LargoRayo = 1000f;
        private const float MitadBarrioX = 134f;
        private const float MitadBarrioZ = 87f;
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;

        private static readonly string[] NombresLugares =
        {
            "Lugar_Casa", "Lugar_Clandestino", "Lugar_Bar", "Lugar_Casino", "Lugar_MercadoNegro"
        };

        private GameObject barrio;
        private Keyboard teclado;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            barrio = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaBarrio));
            Physics.SyncTransforms();
        }

        public override void TearDown()
        {
            Object.DestroyImmediate(barrio);
            base.TearDown();
        }

        private Transform Buscar(string nombre)
        {
            Transform resultado = barrio.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == nombre);
            Assert.IsNotNull(resultado, "Falta '" + nombre + "' en el barrio");
            return resultado;
        }

        private Vector3 Entrada(string lugar)
        {
            Transform entrada = Buscar(lugar).Find("Entrada");
            Assert.IsNotNull(entrada, lugar + " no tiene hijo Entrada");
            return entrada.position;
        }

        private Transform EntradaTransform(string lugar)
        {
            return Buscar(lugar).Find("Entrada");
        }

        private static IEnumerator Calentar()
        {
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        [Test]
        public void Barrio_TieneSeisManzanas()
        {
            for (int i = 1; i <= 6; i++)
            {
                Buscar("Manzana_" + i);
            }
            Assert.AreEqual(6, barrio.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("Manzana_")));
        }

        [Test]
        public void Barrio_TieneLosCincoLugaresConEntrada()
        {
            foreach (string nombre in NombresLugares)
            {
                Assert.IsNotNull(Buscar(nombre).Find("Entrada"), nombre + " no tiene Entrada");
            }
        }

        [Test]
        public void Casa_ALaPuertaDelClandestino_LaLineaRectaChocaConUnEdificio()
        {
            Vector3 desde = Entrada("Lugar_Casa");
            Vector3 hasta = Entrada("Lugar_Clandestino");
            desde.y = AlturaRayo;
            hasta.y = AlturaRayo;
            Assert.IsTrue(Physics.Linecast(desde, hasta), "Entre la casa y el clandestino debe haber un edificio (hay que dar vuelta)");
        }

        [Test]
        public void Clandestino_YBar_SeMiranDeFrenteYEstanCerca()
        {
            Transform clandestino = EntradaTransform("Lugar_Clandestino");
            Transform bar = EntradaTransform("Lugar_Bar");
            Assert.Less(Vector3.Distance(clandestino.position, bar.position), 30f);
            Assert.Less(Vector3.Dot(clandestino.forward, bar.forward), -0.7f);
        }

        [Test]
        public void Casino_Y_MercadoNegro_EstanCercaDelClandestino()
        {
            Vector3 clandestino = Entrada("Lugar_Clandestino");
            Assert.Less(Vector3.Distance(clandestino, Entrada("Lugar_Casino")), 140f);
            Assert.Less(Vector3.Distance(clandestino, Entrada("Lugar_MercadoNegro")), 80f);
        }

        [Test]
        public void Casa_EsElLugarMasLejanoDelCentro()
        {
            Vector3 centro = barrio.transform.position;
            float distanciaCasa = Vector3.Distance(Entrada("Lugar_Casa"), centro);
            foreach (string nombre in NombresLugares.Where(n => n != "Lugar_Casa"))
            {
                Assert.Greater(distanciaCasa, Vector3.Distance(Entrada(nombre), centro), "La casa debe estar más lejos del centro que " + nombre);
            }
        }

        [Test]
        public void Rayos_DesdeElCentro_ChocanTodosYElBordeSeVe()
        {
            Vector3 origen = barrio.transform.position + Vector3.up * AlturaRayo;
            for (int i = 0; i < CantidadRayos; i++)
            {
                float angulo = i * 360f / CantidadRayos;
                Vector3 direccion = Quaternion.Euler(0f, angulo, 0f) * Vector3.forward;
                Assert.IsTrue(Physics.Raycast(origen, direccion, out RaycastHit golpe, LargoRayo), "El rayo a " + angulo + "° no choca con nada");
                MeshRenderer visual = golpe.collider.GetComponentInChildren<MeshRenderer>(); // T-080: las piezas del kit llevan el collider en la raíz y el modelo en un hijo
                Assert.IsNotNull(visual, "El rayo a " + angulo + "° choca con '" + golpe.collider.name + "', que no tiene MeshRenderer");
                Assert.IsTrue(visual.enabled && visual.gameObject.activeInHierarchy, "'" + golpe.collider.name + "' no se ve");
            }
        }

        [Test]
        public void Finales_Reten_Obra_Y_Autopista_SonVisiblesYSolidos()
        {
            foreach (string nombre in new[] { "Final_Reten", "Final_Obra", "Final_Autopista" })
            {
                Transform final = Buscar(nombre);
                Collider[] colliders = final.GetComponentsInChildren<Collider>();
                Assert.IsNotEmpty(colliders, nombre + " no tiene colliders");
                foreach (Collider collider in colliders)
                {
                    Assert.IsNotNull(collider.GetComponentInChildren<MeshRenderer>(), nombre + "/" + collider.name + " tiene collider pero no se ve");
                }
            }
        }

        [Test]
        public void Farolas_SonVariasYTienenLuzApagada()
        {
            Transform[] farolas = barrio.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Farola_")).ToArray();
            Assert.GreaterOrEqual(farolas.Length, 20);
            foreach (Transform farola in farolas)
            {
                Light luz = farola.GetComponentInChildren<Light>(true);
                Assert.IsNotNull(luz, farola.name + " no tiene Light");
                Assert.IsFalse(luz.enabled, farola.name + " debe tener la luz apagada");
            }
        }

        [Test]
        public void PuntosAparicion_EstanRegistradosConIdYModo()
        {
            Assert.AreEqual(3, Registro<IPuntoAparicion>.Cantidad);
            IPuntoAparicion casaPuerta = Registro<IPuntoAparicion>.Todos.Single(p => p.Id == "Casa_Puerta");
            IPuntoAparicion cochera = Registro<IPuntoAparicion>.Todos.Single(p => p.Id == "Casa_Cochera");
            IPuntoAparicion clandestino = Registro<IPuntoAparicion>.Todos.Single(p => p.Id == "Clandestino_Puerta");
            Assert.AreEqual(ModoJugador.APie, casaPuerta.Modo);
            Assert.AreEqual(ModoJugador.Carro, cochera.Modo);
            Assert.AreEqual(ModoJugador.APie, clandestino.Modo);
            Assert.Less(Vector3.Distance(clandestino.Pose.position, Entrada("Lugar_Clandestino")), 0.01f);
        }

        [UnityTest]
        public IEnumerator Camara_ColocadaEnUnPuntoDeAparicion_NoAtraviesaNada()
        {
            GameObject camaraObjeto = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCamara), new Vector3(0f, 0.2f, 0f), Quaternion.identity);
            yield return Calentar();
            IJugador jugador = Registro<IJugador>.Actual;
            Assert.AreSame(camaraObjeto.GetComponent<CamaraLibre>(), jugador);
            IPuntoAparicion punto = Registro<IPuntoAparicion>.Todos.Single(p => p.Id == "Clandestino_Puerta");
            jugador.Colocar(punto.Pose);
            yield return Esperar(1f);
            Vector3 posicion = camaraObjeto.transform.position;
            Assert.Less(Vector3.Distance(new Vector3(posicion.x, 0f, posicion.z), new Vector3(punto.Pose.position.x, 0f, punto.Pose.position.z)), 0.3f);
            Assert.GreaterOrEqual(posicion.y, punto.Pose.position.y - 0.1f, "No debe atravesar la banqueta");
            Object.DestroyImmediate(camaraObjeto);
        }

        [UnityTest]
        public IEnumerator Camara_CaminandoPorLaCalleHastaElFinal_SeDetieneYNoSaleDelBarrio()
        {
            GameObject camaraObjeto = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCamara), new Vector3(90f, 0.1f, 0f), Quaternion.Euler(0f, 90f, 0f));
            yield return Calentar();
            Press(teclado.leftShiftKey);
            Press(teclado.wKey);
            yield return Esperar(9f);
            float x1 = camaraObjeto.transform.position.x;
            yield return Esperar(2f);
            float x2 = camaraObjeto.transform.position.x;
            Release(teclado.wKey);
            Release(teclado.leftShiftKey);
            Assert.Greater(x1, 100f, "Debió avanzar por la calle hacia el retén");
            Assert.AreEqual(x1, x2, 0.1f, "Debió detenerse en el final de la calle");
            Assert.Less(x2, MitadBarrioX, "No debe salir del barrio");
            Assert.Less(Mathf.Abs(camaraObjeto.transform.position.z), MitadBarrioZ);
            Object.DestroyImmediate(camaraObjeto);
        }
    }
}

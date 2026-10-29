using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-401: robar un carro estacionado con E, manejarlo con la cámara detrás y bajar. Jugador real y entrada simulada.</summary>
    public class RobarCarroPruebas : InputTestFixture
    {
        private const string RutaCarro = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;

        private Keyboard teclado;
        private GameObject piso;
        private GameObject carroObjeto;
        private GameObject jugadorObjeto;
        private CarroViejo carro;
        private CarroEstacionado estacionado;
        private IJugador jugador;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
        }

        public override void TearDown()
        {
            Object.Destroy(carroObjeto);
            Object.Destroy(jugadorObjeto);
            Object.Destroy(piso);
            Registros.LimpiarTodos();
            base.TearDown();
        }

        private IEnumerator Preparar()
        {
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
            carroObjeto = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro), new Vector3(0f, 0.3f, 10f), Quaternion.identity);
            carro = carroObjeto.GetComponent<CarroViejo>();
            estacionado = carroObjeto.AddComponent<CarroEstacionado>();
            jugadorObjeto = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador));
            yield return null;
            yield return null;
            jugador = Registro<IJugador>.Actual;
            Assert.IsNotNull(jugador, "El jugador real se registra");

            // A 2 m del punto de subida, mirándolo.
            Vector3 punto = carro.PuntoSubida;
            Vector3 posicion = punto - carroObjeto.transform.right * 2f;
            posicion.y = 0.05f;
            Vector3 mirada = punto - posicion;
            mirada.y = 0f;
            jugador.Colocar(new Pose(posicion, Quaternion.LookRotation(mirada.normalized, Vector3.up)));
            yield return Esperar(0.5f);
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RobarCarroSubeCalorYManeja()
        {
            yield return Preparar();
            ICalor calor = Registro<ICalor>.Actual;
            Assert.IsNotNull(calor, "El jugador trae el servicio de calor");
            float antes = calor.Valor;

            yield return Tecla(teclado.eKey);

            Assert.AreEqual(ModoJugador.Carro, jugador.Modo, "E a 2 m sube al carro");
            Assert.GreaterOrEqual(calor.Valor, antes + 25f, "Robarlo sube el calor");
            Assert.IsTrue(estacionado.EsTuyo);
            yield return Esperar(0.5f);
            Vector3 haciaCamara = (carro.Camara.transform.position - carroObjeto.transform.position).normalized;
            Assert.Less(Vector3.Dot(carroObjeto.transform.forward, haciaCamara), -0.5f, "La cámara del carro está detrás");

            Vector3 inicio = carroObjeto.transform.position;
            Press(teclado.wKey);
            yield return Esperar(2f);
            Release(teclado.wKey);
            Assert.GreaterOrEqual(Vector3.Distance(inicio, carroObjeto.transform.position), 5f, "En 2 s avanza al menos 5 m");
        }

        [UnityTest]
        public IEnumerator BajarParado()
        {
            yield return Preparar();
            ICalor calor = Registro<ICalor>.Actual;
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.Carro, jugador.Modo);
            float conCalor = calor.Valor;

            float limite = Time.time + 5f;
            while (!carro.PuedeBajar && Time.time < limite)
            {
                yield return null;
            }
            yield return Tecla(teclado.eKey);

            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "Parado, E baja");
            Assert.Less(Vector3.Distance(jugador.Raiz.position, carroObjeto.transform.position), 3f, "Queda junto al carro");
            Assert.IsFalse(carro.CamaraDelCarro.Camara.enabled, "La cámara del carro se apaga");
            Assert.IsTrue(jugador.Camara.enabled, "Vuelve la cámara de hombro");

            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.Carro, jugador.Modo, "Sube otra vez");
            yield return Esperar(0.3f);
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.APie, jugador.Modo);
            Assert.AreEqual(conCalor, calor.Valor, 0.01f, "Subir de nuevo no suma calor");
        }
    }
}

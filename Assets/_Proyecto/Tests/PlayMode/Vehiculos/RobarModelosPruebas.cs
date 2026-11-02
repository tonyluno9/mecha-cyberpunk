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
    /// <summary>T-511: cada uno de los 8 modelos de carro se puede robar con E (calor y alarma) y se maneja; un carro quieto se duerme y se despierta al subir.</summary>
    public class RobarModelosPruebas : InputTestFixture
    {
        private const string CarpetaCarros = "Assets/_Proyecto/Vehiculos/Prefabs/Carro_";
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float MetrosEnDosSegundos = 5f;

        private static readonly string[] Codigos = { "V01a", "V01b", "V02", "V03", "V04", "V05", "V06", "V08" };

        private Keyboard teclado;
        private GameObject piso;
        private GameObject carroObjeto;
        private GameObject jugadorObjeto;
        private CarroViejo carro;
        private IJugador jugador;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            Mouse raton = InputSystem.AddDevice<Mouse>();
            MapasDePruebaVehiculos.Reiniciar(teclado, raton);
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
            MapasDePruebaVehiculos.AntesDelTearDown();
            base.TearDown();
            MapasDePruebaVehiculos.DespuesDelTearDown();
        }

        private IEnumerator Preparar(string codigo, bool conJugador)
        {
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
            carroObjeto = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaCarros + codigo + ".prefab"), new Vector3(0f, 0.5f, 10f), Quaternion.identity);
            carro = carroObjeto.GetComponent<CarroViejo>();
            if (!conJugador)
            {
                yield break;
            }
            jugadorObjeto = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador));
            yield return null;
            yield return null;
            jugador = Registro<IJugador>.Actual;
            Assert.IsNotNull(jugador, "El jugador real se registra");
            Vector3 punto = carro.PuntoSubida;
            Vector3 posicion = punto - carroObjeto.transform.right * 1.5f;
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
        public IEnumerator Robar_CadaModelo_SubeCalorYAvanza([ValueSource(nameof(Codigos))] string codigo)
        {
            yield return Preparar(codigo, true);
            ICalor calor = Registro<ICalor>.Actual;
            float antes = calor.Valor;

            yield return Tecla(teclado.eKey);

            Assert.AreEqual(ModoJugador.Carro, jugador.Modo, codigo + ": E sube al carro");
            Assert.GreaterOrEqual(calor.Valor, antes + 25f, codigo + ": robarlo sube el calor");
            Assert.IsTrue(carroObjeto.GetComponent<CarroEstacionado>().EsTuyo);
            yield return Esperar(0.5f);
            Vector3 haciaCamara = (carro.Camara.transform.position - carroObjeto.transform.position).normalized;
            Assert.Less(Vector3.Dot(carroObjeto.transform.forward, haciaCamara), -0.5f, codigo + ": la cámara está detrás");

            Vector3 inicio = carroObjeto.transform.position;
            Press(teclado.wKey);
            yield return Esperar(2f);
            Release(teclado.wKey);
            Assert.GreaterOrEqual(Vector3.Distance(inicio, carroObjeto.transform.position), MetrosEnDosSegundos, codigo + ": en 2 s avanza");
            Assert.Greater(Vector3.Dot(carroObjeto.transform.position - inicio, carroObjeto.transform.forward), MetrosEnDosSegundos * 0.8f, codigo + ": avanza hacia su frente");
        }

        [UnityTest]
        public IEnumerator Estacionado_SeDuerme_YSubirLoDespierta()
        {
            yield return Preparar("V03", true);
            float limite = Time.time + 8f;
            while (!carro.EstaDormido && Time.time < limite)
            {
                yield return null;
            }
            Assert.IsTrue(carro.EstaDormido, "Un carro quieto sobre sus ruedas se duerme (no gasta física)");
            Vector3 quieto = carroObjeto.transform.position;
            yield return Esperar(1f);
            Assert.Less(Vector3.Distance(quieto, carroObjeto.transform.position), 0.02f, "Dormido no se mueve solo");

            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.Carro, jugador.Modo);
            Assert.IsFalse(carro.EstaDormido, "Subir lo despierta");
            Press(teclado.wKey);
            yield return Esperar(1f);
            Release(teclado.wKey);
            Assert.Greater(Vector3.Distance(quieto, carroObjeto.transform.position), 1.5f, "Despierto, maneja");
        }
    }
}

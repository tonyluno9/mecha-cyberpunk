using System;
using System.Collections;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>
    /// T-1488: el peso se siente en el juego. Frenar a fondo cabecea la carrocería (nariz abajo) y se asienta sin oscilar sin fin; la moto parada no se
    /// cae sola. Usa los prefabs reales; los de carro necesitan haber corrido CrearPerfilesManejoT1488 (si no, la prueba se marca como ignorada).
    /// </summary>
    public class PesoCarroT1488PlayPruebas : InputTestFixture
    {
        private const string RutaSedan = "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V02.prefab";
        private const string RutaMoto = "Assets/_Proyecto/Vehiculos/Prefabs/Carro_C19.prefab";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;

        private Keyboard teclado;
        private GameObject piso;
        private GameObject carroObjeto;
        private Rigidbody cuerpo;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.name = "PisoPrueba";
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
        }

        public override void TearDown()
        {
            if (carroObjeto != null)
            {
                UnityEngine.Object.DestroyImmediate(carroObjeto);
            }
            UnityEngine.Object.DestroyImmediate(piso);
            base.TearDown();
        }

        private CarroViejo Crear(string ruta, bool conControl)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            Assert.IsNotNull(prefab, "Falta el prefab " + ruta);
            carroObjeto = UnityEngine.Object.Instantiate(prefab, new Vector3(0f, 0.3f, 0f), Quaternion.identity);
            cuerpo = carroObjeto.GetComponent<Rigidbody>();
            CarroViejo carro = carroObjeto.GetComponent<CarroViejo>();
            carro.TomarControl(conControl);
            return carro;
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

        private static IEnumerator Simular(float segundos, Action porPaso = null)
        {
            float fin = Time.fixedTime + segundos;
            while (Time.fixedTime < fin)
            {
                yield return null;
                porPaso?.Invoke();
            }
        }

        // Cabeceo (°): positivo con la nariz hacia abajo.
        private float Cabeceo()
        {
            return -Mathf.Asin(Mathf.Clamp(carroObjeto.transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
        }

        [UnityTest]
        public IEnumerator Frenar_AFondo_LaNarizBajaYLuegoSeAsienta()
        {
            yield return Calentar();
            CarroViejo carro = Crear(RutaSedan, true);
            var configSerializada = new SerializedObject(carro);
            var config = (MechaCyberpunk.Vehiculos.ConfigCarro)configSerializada.FindProperty("config").objectReferenceValue;
            if (config.FactorCabeceo <= 0f)
            {
                Assert.Ignore("Falta correr CrearPerfilesManejoT1488.Crear: el carro todavía no tiene factor de cabeceo.");
            }
            yield return Simular(1.5f);
            float reposo = Cabeceo();
            cuerpo.linearVelocity = carroObjeto.transform.forward * 14f;
            InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.S));
            InputSystem.Update();
            float maximo = reposo;
            yield return Simular(1.0f, () => maximo = Mathf.Max(maximo, Cabeceo()));
            Assert.Greater(maximo - reposo, 0.8f, "La carrocería cabecea hacia adelante al frenar fuerte");
            Assert.Less(maximo - reposo, 8f, "Sin pasarse de cabeceo");
            InputSystem.QueueStateEvent(teclado, new KeyboardState());
            InputSystem.Update();
            yield return Simular(2.5f);
            Assert.AreEqual(reposo, Cabeceo(), 0.5f, "La suspensión se asienta de vuelta al reposo");
        }

        [UnityTest]
        public IEnumerator Moto_Parada_NoSeVuelcaSola()
        {
            yield return Calentar();
            Crear(RutaMoto, false);
            float peorInclinacion = 1f;
            yield return Simular(4f, () => peorInclinacion = Mathf.Min(peorInclinacion, carroObjeto.transform.up.y));
            Assert.Greater(peorInclinacion, 0.98f, "La moto parada sigue vertical");
        }
    }
}

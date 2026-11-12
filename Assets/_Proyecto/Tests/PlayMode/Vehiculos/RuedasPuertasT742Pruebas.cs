using System;
using System.Collections;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>
    /// T-742: V01a, V01b y V05 por piezas, jugando con entrada simulada: las llantas giran con la velocidad, las delanteras doblan con A y D, las
    /// traseras no y la física sigue igual de siempre. Sin puertas desde T-972.
    /// </summary>
    public class RuedasPuertasT742Pruebas : InputTestFixture
    {
        private static readonly string[] Codigos = { "V01a", "V01b", "V05" };
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;

        private Keyboard teclado;
        private GameObject piso;
        private GameObject carroObjeto;
        private CarroViejo carro;
        private RuedasVisualesCarro ruedas;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
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

        private void CrearCarro(string codigo, Vector3 posicion, bool conControl)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/Carro_" + codigo + ".prefab");
            Assert.IsNotNull(prefab, codigo);
            carroObjeto = UnityEngine.Object.Instantiate(prefab, posicion, Quaternion.identity);
            carro = carroObjeto.GetComponent<CarroViejo>();
            ruedas = carroObjeto.GetComponent<RuedasVisualesCarro>();
            carro.TomarControl(conControl);
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

        // Simula segundos de física. No usa WaitForFixedUpdate: con él el Input System pierde el estado del teclado.
        private static IEnumerator Simular(float segundos, Action porPaso = null)
        {
            float fin = Time.fixedTime + segundos;
            while (Time.fixedTime < fin)
            {
                yield return null;
                porPaso?.Invoke();
            }
        }

        // Con CAPTURAR_S1=1 guarda una foto del carro desde delante a la derecha (se ven las llantas delanteras dobladas).
        private void FotoDelCarro(string nombre)
        {
            if (Environment.GetEnvironmentVariable("CAPTURAR_S1") != "1")
            {
                return;
            }
            GameObject luz = new GameObject("LuzFoto");
            luz.AddComponent<Light>().type = LightType.Directional;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Camera camara = new GameObject("CamFoto").AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.45f, 0.55f, 0.7f);
            camara.fieldOfView = 40f;
            camara.transform.position = carroObjeto.transform.position + carroObjeto.transform.TransformDirection(new Vector3(4.2f, 1.2f, 6.2f));
            camara.transform.LookAt(carroObjeto.transform.position + carroObjeto.transform.TransformDirection(new Vector3(0f, 0.5f, 0.8f)));
            CapturaPruebasVehiculos.Tomar(camara, "docs/capturas/" + nombre + ".png", 1000, 700);
            UnityEngine.Object.DestroyImmediate(camara.gameObject);
            UnityEngine.Object.DestroyImmediate(luz);
        }

        private Transform Giro(string rueda) => carroObjeto.transform.Find(rueda + "/Direccion/Giro");

        private Transform Direccion(string rueda) => carroObjeto.transform.Find(rueda + "/Direccion");

        [UnityTest]
        public IEnumerator Acelerar_ConW_LasCuatroLlantasGiranHaciaAdelante([ValueSource(nameof(Codigos))] string codigo)
        {
            yield return Calentar();
            CrearCarro(codigo, new Vector3(0f, 0f, -90f), true);
            yield return Simular(1f);
            Assert.AreEqual(4, carro.RuedasEnPiso, "Se asienta con las cuatro ruedas");
            Assert.Less(Quaternion.Angle(Quaternion.identity, Giro("Rueda_DI").localRotation), 1f, "Parado, la llanta no gira");

            Press(teclado.wKey);
            yield return Simular(1.5f);
            float anterior = ruedas.RodadoGrados;
            float avanceTotal = 0f;
            int pasos = 0;
            yield return Simular(0.5f, () =>
            {
                float cambio = Mathf.DeltaAngle(anterior, ruedas.RodadoGrados);
                avanceTotal += cambio;
                anterior = ruedas.RodadoGrados;
                pasos++;
            });
            Assert.Greater(carro.Velocidad, 5f, codigo + " va rápido");
            Assert.Greater(avanceTotal, 360f, codigo + ": las llantas dieron más de una vuelta en medio segundo (" + avanceTotal + "°)");
            // Las cuatro llantas siguen el mismo giro.
            float rodado = ruedas.RodadoGrados;
            foreach (string rueda in new[] { "Rueda_DI", "Rueda_DD", "Rueda_TI", "Rueda_TD" })
            {
                Assert.Less(Quaternion.Angle(Quaternion.Euler(rodado, 0f, 0f), Giro(rueda).localRotation), 20f, rueda + " gira con las demás");
            }
        }

        [UnityTest]
        public IEnumerator Reversa_ConS_LasLlantasGiranAlReves([ValueSource(nameof(Codigos))] string codigo)
        {
            yield return Calentar();
            CrearCarro(codigo, new Vector3(0f, 0f, -90f), true);
            yield return Simular(1f);
            Press(teclado.sKey);
            yield return Simular(1.5f);
            float anterior = ruedas.RodadoGrados;
            float avanceTotal = 0f;
            yield return Simular(0.3f, () =>
            {
                avanceTotal += Mathf.DeltaAngle(anterior, ruedas.RodadoGrados);
                anterior = ruedas.RodadoGrados;
            });
            Assert.Less(carro.Velocidad, -1f, codigo + " va en reversa");
            Assert.Less(avanceTotal, -30f, codigo + ": las llantas giran al revés (" + avanceTotal + "°)");
        }

        [UnityTest]
        public IEnumerator Girar_ConDYConA_SoloLasDelanterasDoblanYAlSoltarVuelvenAlCentro([ValueSource(nameof(Codigos))] string codigo)
        {
            yield return Calentar();
            CrearCarro(codigo, new Vector3(0f, 0f, -90f), true);
            yield return Simular(1f);
            Press(teclado.wKey);
            yield return Simular(2f);

            Press(teclado.dKey);
            yield return Simular(1f);
            float derecha = Mathf.DeltaAngle(0f, Direccion("Rueda_DI").localEulerAngles.y);
            Assert.Greater(derecha, 5f, codigo + ": con D las delanteras doblan a la derecha (" + derecha + "°)");
            Assert.AreEqual(derecha, Mathf.DeltaAngle(0f, Direccion("Rueda_DD").localEulerAngles.y), 0.1f, "Las dos delanteras doblan igual");
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, Direccion("Rueda_TI").localEulerAngles.y), 0.01f, "La trasera izquierda no dobla");
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, Direccion("Rueda_TD").localEulerAngles.y), 0.01f, "La trasera derecha no dobla");
            Assert.Less(derecha, 40f, "No se pasa del ángulo máximo");
            FotoDelCarro("s1e-T-742-" + codigo + "-4-ruedas-dobladas-con-D");
            Release(teclado.dKey);

            Press(teclado.aKey);
            yield return Simular(1.2f);
            float izquierda = Mathf.DeltaAngle(0f, Direccion("Rueda_DI").localEulerAngles.y);
            Assert.Less(izquierda, -5f, codigo + ": con A doblan a la izquierda (" + izquierda + "°)");
            Release(teclado.aKey);

            yield return Simular(1.5f);
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, Direccion("Rueda_DI").localEulerAngles.y), 2f, "Sin tecla las delanteras vuelven al centro");
        }

        [UnityTest]
        public IEnumerator Fisica_ConPiezas_ElCarroAceleraComoSiempre([ValueSource(nameof(Codigos))] string codigo)
        {
            // Misma prueba que el carro viejo (ManejoCarroPruebas): a los 6 s de W desde parado va por lo menos a 14 m/s y nunca pasa de 16.5.
            yield return Calentar();
            CrearCarro(codigo, new Vector3(0f, 0f, -95f), true);
            yield return Simular(0.5f);
            Press(teclado.wKey);
            float maximo = 0f;
            yield return Simular(6f, () => maximo = Mathf.Max(maximo, carro.Velocidad));
            Assert.GreaterOrEqual(carro.Velocidad, 14f, codigo + " a los 6 s iba a " + carro.Velocidad + " m/s");
            yield return Simular(3f, () => maximo = Mathf.Max(maximo, carro.Velocidad));
            Assert.LessOrEqual(maximo, 16.5f, codigo + " pasó de la velocidad máxima: " + maximo);
            Assert.AreEqual(4, carro.RuedasEnPiso);
        }

        [UnityTest]
        public IEnumerator SinControl_ElCarroMovidoPorOtroScript_LasLlantasGiranYDelanterasDoblan()
        {
            // Tráfico y patrulla mueven el carro con un cuerpo cinemático: las ruedas se guían por cuánto avanza y gira.
            yield return Calentar();
            CrearCarro("V01a", new Vector3(0f, 0.3f, -60f), false);
            Rigidbody cuerpo = carroObjeto.GetComponent<Rigidbody>();
            cuerpo.isKinematic = true;
            cuerpo.interpolation = RigidbodyInterpolation.None; // la prueba mueve el transform a mano (como TraficoCalle)
            yield return null;
            Assert.IsFalse(carro.TieneControl);
            float anterior = ruedas.RodadoGrados;
            float avanceTotal = 0f;
            float direccionMaxima = 0f;
            // Por cuadros, no por segundos: en el modo batch el reloj de las pruebas y el deltaTime no van parejos. Cada cuadro avanza 2 cm y gira 0.5 grados.
            const int Cuadros = 300;
            const float MetrosPorCuadro = 0.02f;
            const float GradosPorCuadro = 0.5f;
            for (int i = 0; i < Cuadros; i++)
            {
                carroObjeto.transform.position += carroObjeto.transform.forward * MetrosPorCuadro;
                carroObjeto.transform.Rotate(0f, GradosPorCuadro, 0f);
                yield return null;
                avanceTotal += Mathf.DeltaAngle(anterior, ruedas.RodadoGrados);
                anterior = ruedas.RodadoGrados;
                direccionMaxima = Mathf.Max(direccionMaxima, ruedas.DireccionGrados);
            }
            float esperado = Cuadros * MetrosPorCuadro / ruedas.Radio * Mathf.Rad2Deg;
            Assert.AreEqual(esperado, avanceTotal, esperado * 0.1f, "Rodó lo que avanzó: " + avanceTotal + "° de " + esperado + "° (radio " + ruedas.Radio + " m)");
            Assert.Greater(direccionMaxima, 5f, "Las delanteras doblan hacia donde gira el carro (" + direccionMaxima + "°)");
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, Direccion("Rueda_TI").localEulerAngles.y), 0.01f, "Las traseras no doblan");
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }
    }
}

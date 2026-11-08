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
    /// T-743: las motos V07, V08 y C19 jugando con entrada simulada: las dos llantas rueden con W y al revés con S, solo la delantera dobla con A y D,
    /// la moto sigue andando igual (suspensión con las cuatro anclas, aceleración) y las llantas se inclinan con ella. Capturas con CAPTURAR_S1=1.
    /// </summary>
    public class RuedasMotosT743Pruebas : InputTestFixture
    {
        private static readonly string[] Codigos = { "V07", "V08", "C19" };
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;

        private Keyboard teclado;
        private GameObject piso;
        private GameObject motoObjeto;
        private CarroViejo moto;
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
            if (motoObjeto != null)
            {
                UnityEngine.Object.DestroyImmediate(motoObjeto);
            }
            UnityEngine.Object.DestroyImmediate(piso);
            base.TearDown();
        }

        private void CrearMoto(string codigo, Vector3 posicion)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/Carro_" + codigo + ".prefab");
            Assert.IsNotNull(prefab, codigo);
            motoObjeto = UnityEngine.Object.Instantiate(prefab, posicion, Quaternion.identity);
            moto = motoObjeto.GetComponent<CarroViejo>();
            ruedas = motoObjeto.GetComponent<RuedasVisualesCarro>();
            moto.TomarControl(true);
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

        private Transform Direccion(string rueda) => motoObjeto.transform.Find("Visual_T511/Ruedas_T743/" + rueda + "/Direccion");

        private Transform Giro(string rueda) => Direccion(rueda).Find("Giro");

        private void Foto(string nombre)
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
            camara.transform.position = motoObjeto.transform.position + motoObjeto.transform.TransformDirection(new Vector3(-3.4f, 1.0f, 4.0f));
            camara.transform.LookAt(motoObjeto.transform.position + motoObjeto.transform.TransformDirection(new Vector3(0f, 0.55f, 0.5f)));
            CapturaPruebasVehiculos.Tomar(camara, "docs/capturas/" + nombre + ".png", 1000, 700);
            UnityEngine.Object.DestroyImmediate(camara.gameObject);
            UnityEngine.Object.DestroyImmediate(luz);
        }

        [UnityTest]
        public IEnumerator Acelerar_ConW_LasDosLlantasGiranHaciaAdelante([ValueSource(nameof(Codigos))] string codigo)
        {
            yield return Calentar();
            CrearMoto(codigo, new Vector3(0f, 0f, -90f));
            yield return Simular(1f);
            Assert.AreEqual(4, moto.RuedasEnPiso, "Se asienta con las cuatro anclas de la suspensión");
            Assert.Less(Quaternion.Angle(Quaternion.identity, Giro("Rueda_Del").localRotation), 1f, "Parada, la llanta no gira");

            Press(teclado.wKey);
            yield return Simular(1.5f);
            float anterior = ruedas.RodadoGrados;
            float avanceTotal = 0f;
            yield return Simular(0.5f, () =>
            {
                avanceTotal += Mathf.DeltaAngle(anterior, ruedas.RodadoGrados);
                anterior = ruedas.RodadoGrados;
            });
            Assert.Greater(moto.Velocidad, 5f, codigo + " va rápido");
            Assert.Greater(avanceTotal, 360f, codigo + ": las llantas dieron más de una vuelta en medio segundo (" + avanceTotal + "°)");
            float rodado = ruedas.RodadoGrados;
            foreach (string rueda in new[] { "Rueda_Del", "Rueda_Tras" })
            {
                Assert.Less(Quaternion.Angle(Quaternion.Euler(rodado, 0f, 0f), Giro(rueda).localRotation), 20f, rueda + " gira con el rodado");
            }
        }

        [UnityTest]
        public IEnumerator Reversa_ConS_LasLlantasGiranAlReves([ValueSource(nameof(Codigos))] string codigo)
        {
            yield return Calentar();
            CrearMoto(codigo, new Vector3(0f, 0f, -90f));
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
            Assert.Less(moto.Velocidad, -1f, codigo + " va en reversa");
            Assert.Less(avanceTotal, -30f, codigo + ": las llantas giran al revés (" + avanceTotal + "°)");
        }

        [UnityTest]
        public IEnumerator Girar_ConDYConA_SoloLaDelanteraDobla([ValueSource(nameof(Codigos))] string codigo)
        {
            yield return Calentar();
            CrearMoto(codigo, new Vector3(0f, 0f, -90f));
            yield return Simular(1f);
            Press(teclado.wKey);
            yield return Simular(2f);

            Press(teclado.dKey);
            yield return Simular(1f);
            float derecha = Mathf.DeltaAngle(0f, Direccion("Rueda_Del").localEulerAngles.y);
            Assert.Greater(derecha, 4f, codigo + ": con D la delantera dobla a la derecha (" + derecha + "°)");
            Assert.Less(derecha, 35f, "No se pasa del ángulo máximo");
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, Direccion("Rueda_Tras").localEulerAngles.y), 0.01f, "La trasera no dobla");
            Foto("s1e-T-743-" + codigo + "-1-delantera-doblada-con-D");
            Release(teclado.dKey);

            Press(teclado.aKey);
            yield return Simular(1.2f);
            float izquierda = Mathf.DeltaAngle(0f, Direccion("Rueda_Del").localEulerAngles.y);
            Assert.Less(izquierda, -4f, codigo + ": con A dobla a la izquierda (" + izquierda + "°)");
            Release(teclado.aKey);

            yield return Simular(1.5f);
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, Direccion("Rueda_Del").localEulerAngles.y), 2f, "Sin tecla vuelve al centro");
        }

        [UnityTest]
        public IEnumerator Fisica_ConPiezas_LaMotoAceleraYSeInclinaComoSiempre([ValueSource(nameof(Codigos))] string codigo)
        {
            yield return Calentar();
            CrearMoto(codigo, new Vector3(0f, 0f, -95f));
            yield return Simular(0.5f);
            Press(teclado.wKey);
            float maximo = 0f;
            yield return Simular(5f, () => maximo = Mathf.Max(maximo, moto.Velocidad));
            Assert.GreaterOrEqual(moto.Velocidad, 9f, codigo + " acelera (va a " + moto.Velocidad + " m/s)");
            Press(teclado.dKey);
            float inclinacion = 0f;
            Transform envoltorio = motoObjeto.transform.Find("Visual_T511");
            Quaternion reposo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/Carro_" + codigo + ".prefab").transform.Find("Visual_T511").localRotation;
            yield return Simular(1f, () => inclinacion = Mathf.Max(inclinacion, Quaternion.Angle(reposo, envoltorio.localRotation)));
            Assert.Greater(inclinacion, 1f, codigo + ": la moto se inclina en la curva (" + inclinacion + "°) y las llantas con ella");
            Assert.AreEqual(4, moto.RuedasEnPiso);
        }

        [UnityTest]
        public IEnumerator SinControl_LaMotoMovidaPorOtroScript_LasLlantasGiran()
        {
            yield return Calentar();
            CrearMoto("V08", new Vector3(0f, 0.3f, -60f));
            moto.TomarControl(false);
            Rigidbody cuerpo = motoObjeto.GetComponent<Rigidbody>();
            cuerpo.isKinematic = true;
            cuerpo.interpolation = RigidbodyInterpolation.None; // la prueba mueve el transform a mano (como TraficoCalle)
            yield return null;
            float anterior = ruedas.RodadoGrados;
            float avanceTotal = 0f;
            const int Cuadros = 300;
            const float MetrosPorCuadro = 0.02f;
            for (int i = 0; i < Cuadros; i++)
            {
                motoObjeto.transform.position += motoObjeto.transform.forward * MetrosPorCuadro;
                yield return null;
                avanceTotal += Mathf.DeltaAngle(anterior, ruedas.RodadoGrados);
                anterior = ruedas.RodadoGrados;
            }
            float esperado = Cuadros * MetrosPorCuadro / ruedas.Radio * Mathf.Rad2Deg;
            Assert.AreEqual(esperado, avanceTotal, esperado * 0.1f, "Rodó lo que avanzó: " + avanceTotal + "° de " + esperado + "°");
        }
    }
}

using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Movimiento;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mecha
{
    /// <summary>Pruebas jugables del mecha: prefab real, piso y cubo creados en la prueba, entrada simulada.</summary>
    public class ControladorMechaPruebas : InputTestFixture
    {
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";

        private Keyboard teclado;
        private Mouse raton;
        private GameObject piso;
        private GameObject mecha;
        private ControladorMecha controlador;
        private DatosMecha datos;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();

            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(200f, 1f, 200f);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            mecha = Object.Instantiate(prefab, new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            controlador = mecha.GetComponent<ControladorMecha>();

            // Datos propios de la prueba para no tocar el asset del proyecto.
            datos = ScriptableObject.CreateInstance<DatosMecha>();
            SerializedObject so = new SerializedObject(controlador);
            so.FindProperty("datos").objectReferenceValue = datos;
            so.ApplyModifiedPropertiesWithoutUndo();
            // Con VidaMecha el controlador usa sus datos (T-165): se le dan los mismos.
            mecha.GetComponent<MechaCyberpunk.Combate.Vida.VidaMecha>().Configurar(datos);
        }

        public override void TearDown()
        {
            Object.Destroy(mecha);
            Object.Destroy(piso);
            Object.Destroy(datos);
            base.TearDown();
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        // Los primeros frames de PlayMode son lentos (deltaTime enorme): se espera a que los frames se estabilicen.
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

        private void FijarDatos(string campo, float valor)
        {
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty(campo).floatValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [UnityTest]
        public IEnumerator Mecha_PresionarW_AvanzaHaciaAdelante()
        {
            yield return Calentar();
            Vector3 inicio = mecha.transform.position;
            Press(teclado.wKey);
            yield return Esperar(1f);
            Release(teclado.wKey);
            Assert.Greater(mecha.transform.position.z - inicio.z, 1f);
            Assert.Less(Mathf.Abs(mecha.transform.position.x - inicio.x), 0.01f);
        }

        [UnityTest]
        public IEnumerator Mecha_CuatroDirecciones_CaminaEnCadaUna()
        {
            yield return Calentar();
            Key[] teclas = { Key.W, Key.S, Key.A, Key.D };
            Vector3[] esperado = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
            for (int i = 0; i < teclas.Length; i++)
            {
                Vector3 inicio = mecha.transform.position;
                Press(teclado[teclas[i]]);
                yield return Esperar(0.5f);
                Release(teclado[teclas[i]]);
                yield return Esperar(1f);
                Vector3 desplazamiento = mecha.transform.position - inicio;
                Assert.Greater(Vector3.Dot(desplazamiento, esperado[i]), 0.3f, "Tecla " + teclas[i]);
            }
        }

        [UnityTest]
        public IEnumerator Mecha_Diagonal_NoVaMasRapidoQueLaMaxima()
        {
            yield return Calentar();
            Press(teclado.wKey);
            Press(teclado.dKey);
            float maximaObservada = 0f;
            float t = 0f;
            while (t < 2f)
            {
                t += Time.deltaTime;
                maximaObservada = Mathf.Max(maximaObservada, controlador.VelocidadLocal.magnitude);
                yield return null;
            }
            Release(teclado.wKey);
            Release(teclado.dKey);
            Assert.LessOrEqual(maximaObservada, datos.VelocidadMaxima + 0.001f);
            Assert.Greater(maximaObservada, datos.VelocidadMaxima * 0.95f);
        }

        [UnityTest]
        public IEnumerator Mecha_SoltarW_TardaEnDetenerseYNoInvierte()
        {
            yield return Calentar();
            Press(teclado.wKey);
            yield return Esperar(1.5f);
            Release(teclado.wKey);
            yield return null;
            Assert.Greater(controlador.VelocidadLocal.y, 0.5f, "No debe frenar en seco");
            yield return Esperar(1f);
            Assert.AreEqual(0f, controlador.VelocidadLocal.magnitude, 0.001f);
        }

        [UnityTest]
        public IEnumerator Mecha_RatonMuyRapido_GiraConVelocidadMaximaYTerminaDespues()
        {
            yield return Calentar();
            float inicio = mecha.transform.eulerAngles.y;
            Set(raton.delta, new Vector2(1000f, 0f));
            yield return null;
            yield return null;
            float giroPrimerosFrames = Mathf.DeltaAngle(inicio, mecha.transform.eulerAngles.y);
            Assert.Less(giroPrimerosFrames, 20f, "No debe girar de golpe");
            Assert.Greater(controlador.GiroPendiente, 50f, "Queda giro pendiente");

            float t = 0f;
            float anterior = mecha.transform.eulerAngles.y;
            while (t < 3f)
            {
                yield return null;
                t += Time.deltaTime;
                float ahora = mecha.transform.eulerAngles.y;
                Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(anterior, ahora)), datos.VelocidadGiroMaxima * Time.deltaTime + 0.01f);
                anterior = ahora;
            }
            Assert.AreEqual(0f, controlador.GiroPendiente, 0.01f);
        }

        [UnityTest]
        public IEnumerator Mecha_MirarArribaYAbajo_SeDetieneEnElLimite()
        {
            yield return Calentar();
            Set(raton.delta, new Vector2(0f, 100000f));
            yield return null;
            yield return null;
            Assert.AreEqual(-35f, controlador.InclinacionVertical, 0.01f);
            Set(raton.delta, new Vector2(0f, -100000f));
            yield return null;
            yield return null;
            Assert.AreEqual(35f, controlador.InclinacionVertical, 0.01f);
        }

        [UnityTest]
        public IEnumerator Mecha_Caminando_LaCamaraSeMeceYQuietoNo()
        {
            yield return Calentar();
            float baseY = controlador.AlturaCamaraBase;
            Press(teclado.wKey);
            float minimo = float.MaxValue;
            float maximo = float.MinValue;
            float t = 0f;
            while (t < 3f)
            {
                t += Time.deltaTime;
                float y = controlador.Camara.localPosition.y;
                if (t > 1.5f)
                {
                    minimo = Mathf.Min(minimo, y);
                    maximo = Mathf.Max(maximo, y);
                }
                yield return null;
            }
            Release(teclado.wKey);
            Assert.Greater(maximo - minimo, 0.05f, "La vista debe mecerse al caminar");

            yield return Esperar(2f);
            Assert.AreEqual(baseY, controlador.Camara.localPosition.y, 0.001f);
        }

        [UnityTest]
        public IEnumerator Mecha_CaminaContraUnCubo_NoLoAtraviesa()
        {
            GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubo.transform.position = new Vector3(0f, 1.5f, 6f);
            cubo.transform.localScale = Vector3.one * 3f;
            yield return null;
            Press(teclado.wKey);
            yield return Esperar(4f);
            Release(teclado.wKey);
            float z = mecha.transform.position.z;
            float y = mecha.transform.position.y;
            Object.Destroy(cubo);
            // El cubo empieza en z = 4.5 y el radio del mecha es 1 m.
            Assert.Less(z, 4.5f, "No debe atravesar el cubo");
            Assert.Greater(z, 2f);
            Assert.GreaterOrEqual(y, -0.05f, "No debe caer del piso");
        }

        [UnityTest]
        public IEnumerator Mecha_CambiarVelocidadMaximaEnJuego_CambiaLaVelocidad()
        {
            yield return Calentar();
            FijarDatos("velocidadMaxima", 2f);
            Press(teclado.wKey);
            yield return Esperar(2f);
            Assert.AreEqual(2f, controlador.VelocidadLocal.magnitude, 0.01f);
            FijarDatos("velocidadMaxima", 8f);
            yield return Esperar(2f);
            Release(teclado.wKey);
            Assert.AreEqual(8f, controlador.VelocidadLocal.magnitude, 0.01f);
        }

        [UnityTest]
        public IEnumerator Mecha_EscapeLiberaElCursorYUnClicLoBloquea()
        {
            yield return Calentar();
            Assert.IsTrue(controlador.CursorBloqueado);
            Press(teclado.escapeKey);
            yield return null;
            Release(teclado.escapeKey);
            yield return null;
            Assert.IsFalse(controlador.CursorBloqueado);

            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return null;
            Assert.IsTrue(controlador.CursorBloqueado);
        }

        [UnityTest]
        public IEnumerator Mecha_CursorLiberado_IgnoraElRaton()
        {
            yield return Calentar();
            Press(teclado.escapeKey);
            yield return null;
            Release(teclado.escapeKey);
            yield return null;
            Set(raton.delta, new Vector2(500f, 500f));
            yield return null;
            yield return null;
            Assert.AreEqual(0f, controlador.GiroPendiente, 0.001f);
            Assert.AreEqual(0f, controlador.InclinacionVertical, 0.001f);
        }
    }
}

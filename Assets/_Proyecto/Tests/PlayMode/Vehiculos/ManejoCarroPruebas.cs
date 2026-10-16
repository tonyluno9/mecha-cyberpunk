using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>Pruebas jugables del carro viejo: prefab real, piso y obstáculos creados aquí, entrada simulada.</summary>
    public class ManejoCarroPruebas : InputTestFixture
    {
        private const string RutaPrefab = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";
        private const string RutaCaptura = "docs/capturas/T-074-1.png";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float GradosPorVuelta = 360f;

        private Keyboard teclado;
        private GameObject piso;
        private GameObject carroObjeto;
        private CarroViejo carro;
        private Rigidbody cuerpo;
        private readonly System.Collections.Generic.List<GameObject> extras = new System.Collections.Generic.List<GameObject>();

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.name = "PisoPrueba";
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(200f, 1f, 200f);
        }

        public override void TearDown()
        {
            foreach (GameObject extra in extras)
            {
                if (extra != null)
                {
                    UnityEngine.Object.DestroyImmediate(extra);
                }
            }
            extras.Clear();
            if (carroObjeto != null)
            {
                UnityEngine.Object.DestroyImmediate(carroObjeto);
            }
            UnityEngine.Object.DestroyImmediate(piso);
            base.TearDown();
        }

        private void CrearCarro(Vector3 posicion, Quaternion rotacion, bool conControl)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            Assert.IsNotNull(prefab, "Falta el prefab " + RutaPrefab);
            carroObjeto = UnityEngine.Object.Instantiate(prefab, posicion, rotacion);
            carro = carroObjeto.GetComponent<CarroViejo>();
            cuerpo = carroObjeto.GetComponent<Rigidbody>();
            carro.TomarControl(conControl);
        }

        private GameObject Caja(string nombre, Vector3 centro, Vector3 tamano)
        {
            GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
            caja.name = nombre;
            caja.transform.position = centro;
            caja.transform.localScale = tamano;
            extras.Add(caja);
            return caja;
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

        // Simula segundos de física (Time.fixedTime). No usa WaitForFixedUpdate: con él el Input System pierde el estado del teclado.
        private static IEnumerator Simular(float segundos, Action porPaso = null)
        {
            float fin = Time.fixedTime + segundos;
            while (Time.fixedTime < fin)
            {
                yield return null;
                porPaso?.Invoke();
            }
        }

        private void FijarVelocidadAdelante(float velocidad)
        {
            cuerpo.linearVelocity = carroObjeto.transform.forward * velocidad;
        }

        [UnityTest]
        public IEnumerator Suspension_SoltadoAMedioMetro_SeAsientaConLasCuatroRuedas()
        {
            yield return Calentar();
            CrearCarro(new Vector3(0f, 0.5f, 0f), Quaternion.identity, true);
            yield return Simular(2f);
            Assert.AreEqual(4, carro.RuedasEnPiso);

            float minimo = float.MaxValue;
            float maximo = float.MinValue;
            yield return Simular(1f, () =>
            {
                float y = carroObjeto.transform.position.y;
                minimo = Mathf.Min(minimo, y);
                maximo = Mathf.Max(maximo, y);
            });
            Assert.Less(maximo - minimo, 0.02f, "La altura oscila " + (maximo - minimo) + " m después de asentarse");
            Assert.AreEqual(4, carro.RuedasEnPiso);
            Assert.Greater(carroObjeto.transform.position.y, -0.01f);
        }

        [UnityTest]
        public IEnumerator Acelerar_WSeisSegundosDesdeParado_Llega14SinPasarDe16Y5()
        {
            yield return Calentar();
            CrearCarro(new Vector3(0f, 0f, -95f), Quaternion.identity, true);
            yield return Simular(0.5f);
            Press(teclado.wKey);
            float maximo = 0f;
            yield return Simular(6f, () => maximo = Mathf.Max(maximo, carro.Velocidad));
            float aLos6 = carro.Velocidad;
            yield return Simular(6f, () => maximo = Mathf.Max(maximo, carro.Velocidad));
            Assert.GreaterOrEqual(aLos6, 14f, "A los 6 s iba a " + aLos6 + " m/s");
            Assert.LessOrEqual(maximo, 16.5f, "Pasó de la velocidad máxima: " + maximo);
        }

        [UnityTest]
        public IEnumerator Acelerar_FlechaArriba_TambienAcelera()
        {
            yield return Calentar();
            CrearCarro(Vector3.zero, Quaternion.identity, true);
            yield return Simular(0.5f);
            Press(teclado.upArrowKey);
            yield return Simular(2f);
            Assert.Greater(carro.Velocidad, 4f);
        }

        [UnityTest]
        public IEnumerator Frenar_A16MetrosPorSegundo_SeDetieneEnMenosDe3Y5Segundos()
        {
            yield return Calentar();
            CrearCarro(Vector3.zero, Quaternion.identity, true);
            yield return Simular(0.5f);
            FijarVelocidadAdelante(16f);
            Press(teclado.sKey);
            float tiempo = 0f;
            float limite = 3.5f;
            while (carro.Velocidad >= 0.5f && tiempo < limite)
            {
                yield return Simular(Time.fixedDeltaTime);
                tiempo += Time.fixedDeltaTime;
            }
            Assert.Less(carro.Velocidad, 0.5f, "Tras " + tiempo + " s aún iba a " + carro.Velocidad + " m/s");
            Assert.Less(tiempo, limite);
        }

        [UnityTest]
        public IEnumerator Reversa_SostenerSYaParado_VaHaciaAtrasSinPasarDe5Y5()
        {
            yield return Calentar();
            CrearCarro(Vector3.zero, Quaternion.identity, true);
            yield return Simular(0.5f);
            Press(teclado.sKey);
            float minimo = 0f;
            yield return Simular(5f, () => minimo = Mathf.Min(minimo, carro.Velocidad));
            Assert.Less(carro.Velocidad, -2f, "No dio reversa: " + carro.Velocidad);
            Assert.GreaterOrEqual(minimo, -5.5f, "Reversa demasiado rápida: " + minimo);
        }

        [UnityTest]
        public IEnumerator Girar_Parado_NoGiraMasDeUnGrado()
        {
            yield return Calentar();
            CrearCarro(Vector3.zero, Quaternion.identity, true);
            yield return Simular(0.5f);
            float inicial = carroObjeto.transform.eulerAngles.y;
            Press(teclado.dKey);
            yield return Simular(2f);
            Release(teclado.dKey);
            Press(teclado.aKey);
            yield return Simular(2f);
            float giro = Mathf.Abs(Mathf.DeltaAngle(inicial, carroObjeto.transform.eulerAngles.y));
            Assert.Less(giro, 1f);
        }

        [UnityTest]
        public IEnumerator Girar_DerechaA10MetrosPorSegundo_DaVueltaConRadioDe8A20()
        {
            yield return Calentar();
            CrearCarro(Vector3.zero, Quaternion.identity, true);
            yield return Simular(0.5f);
            Press(teclado.dKey);

            float angulo = 0f;
            float anterior = carroObjeto.transform.eulerAngles.y;
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            float tiempo = 0f;
            while (angulo < GradosPorVuelta && tiempo < 15f)
            {
                yield return Simular(Time.fixedDeltaTime, () =>
                {
                    // Mantiene los 10 m/s hacia adelante para medir solo la geometría del giro.
                    Vector3 adelante = carroObjeto.transform.forward;
                    adelante.y = 0f;
                    adelante.Normalize();
                    cuerpo.linearVelocity = new Vector3(adelante.x * 10f, cuerpo.linearVelocity.y, adelante.z * 10f);
                });
                tiempo += Time.fixedDeltaTime;
                float actual = carroObjeto.transform.eulerAngles.y;
                angulo += Mathf.DeltaAngle(anterior, actual);
                anterior = actual;
                Vector3 p = carroObjeto.transform.position;
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minZ = Mathf.Min(minZ, p.z);
                maxZ = Mathf.Max(maxZ, p.z);
            }
            Assert.GreaterOrEqual(angulo, GradosPorVuelta, "No completó la vuelta: " + angulo + "°");
            float radio = ((maxX - minX) + (maxZ - minZ)) / 4f;
            Assert.That(radio, Is.InRange(8f, 20f), "Radio de giro: " + radio + " m");
        }

        [UnityTest]
        public IEnumerator Antivuelco_A16MetrosPorSegundoConDireccionATope_NoSeVoltea()
        {
            yield return Calentar();
            CrearCarro(Vector3.zero, Quaternion.identity, true);
            yield return Simular(0.5f);
            FijarVelocidadAdelante(16f);
            Press(teclado.wKey);
            Press(teclado.dKey);
            float minimoArriba = 1f;
            float sinRuedas = 0f;
            float peorSinRuedas = 0f;
            yield return Simular(3f, () =>
            {
                minimoArriba = Mathf.Min(minimoArriba, carroObjeto.transform.up.y);
                if (carro.RuedasEnPiso < 2)
                {
                    sinRuedas += Time.fixedDeltaTime;
                    peorSinRuedas = Mathf.Max(peorSinRuedas, sinRuedas);
                }
                else
                {
                    sinRuedas = 0f;
                }
            });
            Assert.GreaterOrEqual(minimoArriba, 0.7f, "Se inclinó hasta up.y = " + minimoArriba);
            Assert.LessOrEqual(peorSinRuedas, 0.3f, "Estuvo " + peorSinRuedas + " s con menos de 2 ruedas en el piso");
        }

        [UnityTest]
        public IEnumerator Banqueta_EscalonDe20cmA5MetrosPorSegundo_LaSubeYSigue()
        {
            yield return Calentar();
            // Escalón de 0.2 m de alto que empieza en z = 10 y sigue 20 m.
            Caja("Banqueta", new Vector3(0f, 0.1f, 20f), new Vector3(8f, 0.2f, 20f));
            CrearCarro(Vector3.zero, Quaternion.identity, true);
            yield return Simular(0.5f);
            FijarVelocidadAdelante(5f);
            Press(teclado.wKey);
            float tiempo = 0f;
            while (carroObjeto.transform.position.z < 22f && tiempo < 12f)
            {
                yield return Simular(Time.fixedDeltaTime);
                tiempo += Time.fixedDeltaTime;
            }
            Assert.GreaterOrEqual(carroObjeto.transform.position.z, 22f, "Se atoró en z = " + carroObjeto.transform.position.z);
            Assert.Greater(carroObjeto.transform.position.y, 0.15f, "No quedó arriba del escalón");
            Assert.Greater(carro.Velocidad, 2f);
        }

        [UnityTest]
        public IEnumerator Muro_A16MetrosPorSegundoContraMuroDe4m_NoLoAtraviesaYSeDetiene()
        {
            yield return Calentar();
            // Muro de 4 m de grueso, de z = 28 a z = 32.
            Caja("Muro", new Vector3(0f, 2.5f, 30f), new Vector3(20f, 5f, 4f));
            CrearCarro(new Vector3(0f, 0f, 10f), Quaternion.identity, true);
            yield return Simular(0.5f);
            FijarVelocidadAdelante(16f);
            Press(teclado.wKey);
            float maximoZ = float.MinValue;
            bool nan = false;
            yield return Simular(6f, () =>
            {
                Vector3 p = carroObjeto.transform.position;
                nan |= float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z);
                maximoZ = Mathf.Max(maximoZ, p.z);
            });
            Assert.IsFalse(nan, "La posición dio NaN");
            Assert.Less(maximoZ, 28f, "El carro llegó a z = " + maximoZ);
            Assert.Less(carro.Rapidez, 1f, "No se detuvo: " + carro.Rapidez + " m/s");
        }

        [UnityTest]
        public IEnumerator SinControl_WYFlechasNoLoMueven()
        {
            yield return Calentar();
            CrearCarro(Vector3.zero, Quaternion.identity, false);
            yield return Simular(0.5f);
            Vector3 inicio = carroObjeto.transform.position;
            Press(teclado.wKey);
            Press(teclado.upArrowKey);
            Press(teclado.dKey);
            yield return Simular(2f);
            Assert.IsFalse(carro.TieneControl);
            Assert.Less((carroObjeto.transform.position - inicio).magnitude, 0.1f);
        }

        [UnityTest]
        public IEnumerator SinControl_EnRampaDe5Grados_SeDeslizaMenosDe20cmEn5Segundos()
        {
            yield return Calentar();
            Quaternion inclinacion = Quaternion.Euler(5f, 0f, 0f);
            GameObject rampa = Caja("Rampa", -(inclinacion * Vector3.up) * 0.5f, new Vector3(60f, 1f, 60f));
            rampa.transform.rotation = inclinacion;
            // Se pone arriba del piso para que la rampa sea lo único bajo el carro.
            piso.transform.position = new Vector3(0f, -20f, 0f);
            CrearCarro(Vector3.zero, inclinacion, false);
            Vector3 inicio = carroObjeto.transform.position;
            Press(teclado.wKey);
            yield return Simular(5f);
            float desplazamiento = (carroObjeto.transform.position - inicio).magnitude;
            Assert.Less(desplazamiento, 0.2f, "Se deslizó " + desplazamiento + " m");
            Assert.GreaterOrEqual(carro.RuedasEnPiso, 4);
        }

        [UnityTest]
        public IEnumerator Captura_CarroManejandoEnElPiso()
        {
            yield return Calentar();
            CrearCarro(Vector3.zero, Quaternion.identity, true);
            yield return Simular(0.5f);
            Press(teclado.wKey);
            Press(teclado.dKey);
            yield return Simular(4f);

            GameObject luzObjeto = new GameObject("LuzPrueba");
            extras.Add(luzObjeto);
            luzObjeto.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light luz = luzObjeto.AddComponent<Light>();
            luz.type = LightType.Directional;

            GameObject camObjeto = new GameObject("CamaraPrueba");
            extras.Add(camObjeto);
            Camera camara = camObjeto.AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.4f, 0.55f, 0.75f);
            Transform t = carroObjeto.transform;
            camObjeto.transform.position = t.position - t.forward * 7f + t.right * 4f + Vector3.up * 3.5f;
            camObjeto.transform.LookAt(t.position + Vector3.up * 0.8f);

            RenderTexture rt = new RenderTexture(1280, 720, 24);
            camara.targetTexture = rt;
            yield return null;
            camara.Render();
            RenderTexture.active = rt;
            Texture2D imagen = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            imagen.ReadPixels(new Rect(0f, 0f, rt.width, rt.height), 0, 0);
            imagen.Apply();
            RenderTexture.active = null;
            camara.targetTexture = null;

            string ruta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", RutaCaptura));
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllBytes(ruta, imagen.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(imagen);
            UnityEngine.Object.DestroyImmediate(rt);
            Assert.IsTrue(File.Exists(ruta));
            Assert.Greater(carro.Velocidad, 5f);
        }
    }
}

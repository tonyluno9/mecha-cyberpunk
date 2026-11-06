using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Progresion.Apoyo;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.Tests.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-813: la silla del Doc en una escena armada aquí (piso, JugadorAPie.prefab, una silla y un brazo de cubos). Herido, E en la silla te sienta
    /// (pose puesta, control congelado, cámara cercana encima de la del jugador), el brazo baja, la barra se llena y al terminar tienes la vida
    /// llena, pagaste Ƀ50, te levantaste y el control volvió. Sano, la silla no se ofrece. Con CAPTURAR_S2N_SILLA=1 guarda
    /// docs/capturas/s2n-silla-clinica.png.
    /// </summary>
    public class SillaClinicaPruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaConfig = "Assets/_Proyecto/Progresion/Apoyo/Datos/ConfigSillaClinica_Base.asset";
        private const string VariableCaptura = "CAPTURAR_S2N_SILLA";
        private const int DineroInicial = 500;
        private const float SegundosEspera = 5f;

        private sealed class CarteraDoble : ICartera
        {
            public int Dinero { get; set; }
            public event Action<int, MotivoDinero> Cambio;
            public bool PuedePagar(int cantidad) => cantidad >= 0 && Dinero >= cantidad;

            public bool IntentarPagar(int cantidad, MotivoDinero motivo)
            {
                if (!PuedePagar(cantidad))
                {
                    return false;
                }
                Dinero -= cantidad;
                Cambio?.Invoke(-cantidad, motivo);
                return true;
            }

            public void Abonar(int cantidad, MotivoDinero motivo)
            {
                Dinero += cantidad;
                Cambio?.Invoke(cantidad, motivo);
            }

            public void CargarDeuda(int cantidad, MotivoDinero motivo) => Abonar(-cantidad, motivo);
        }

        private Keyboard teclado;
        private Mouse raton;
        private readonly HashSet<GameObject> raicesIniciales = new HashSet<GameObject>();

        public override void Setup()
        {
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
            raicesIniciales.Clear();
            raicesIniciales.UnionWith(SceneManager.GetActiveScene().GetRootGameObjects());
        }

        public override void TearDown()
        {
            Registros.LimpiarTodos();
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado, raton);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (!raicesIniciales.Contains(raiz))
                {
                    Object.Destroy(raiz);
                }
            }
            yield return null;
            Registros.LimpiarTodos();
        }

        private static IEnumerator EsperarHasta(Func<bool> condicion, string que, float segundos = SegundosEspera)
        {
            float limite = Time.realtimeSinceStartup + segundos;
            while (!condicion())
            {
                if (Time.realtimeSinceStartup > limite)
                {
                    Assert.Fail("No pasó a tiempo: " + que);
                }
                yield return null;
            }
        }

        private IEnumerator Pulsar(UnityEngine.InputSystem.Controls.ButtonControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        // Una silla de cubos (asiento y respaldo) y un brazo colgando encima, frente al jugador.
        private static SillaClinica CrearSilla(ConfigSillaClinica config)
        {
            GameObject sillon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sillon.name = "I07";
            sillon.transform.position = new Vector3(0f, 0.25f, 2.2f);
            sillon.transform.localScale = new Vector3(0.7f, 0.5f, 1.6f);
            Object.Destroy(sillon.GetComponent<Collider>());
            GameObject brazo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            brazo.name = "I06";
            brazo.transform.position = new Vector3(0f, 2.3f, 2.2f);
            brazo.transform.localScale = new Vector3(0.15f, 0.6f, 0.15f);
            Object.Destroy(brazo.GetComponent<Collider>());

            GameObject raiz = new GameObject("SillaClinica");
            raiz.SetActive(false);
            raiz.transform.position = new Vector3(0f, 0f, 1.3f);
            Transform asiento = new GameObject("Asiento").transform;
            asiento.SetParent(raiz.transform, false);
            asiento.SetPositionAndRotation(new Vector3(0f, 0.55f, 2.2f), Quaternion.LookRotation(Vector3.back));
            Transform salida = new GameObject("Salida").transform;
            salida.SetParent(raiz.transform, false);
            salida.SetPositionAndRotation(new Vector3(0.9f, 0.05f, 2.2f), Quaternion.identity);
            SillaClinica silla = raiz.AddComponent<SillaClinica>();
            silla.Configurar(config, asiento, salida, brazo.transform);
            raiz.SetActive(true);
            return silla;
        }

        [UnityTest]
        public IEnumerator Silla_HeridoPulsaE_SeSientaSeCuraCobraYDevuelveElControl()
        {
            ConfigSillaClinica config = AssetDatabase.LoadAssetAtPath<ConfigSillaClinica>(RutaConfig);
            Assert.IsNotNull(config, "Existe la config de la silla");
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Plane);
            piso.transform.localScale = Vector3.one * 3f;
            GameObject luz = new GameObject("Sol");
            luz.AddComponent<Light>().type = LightType.Directional;
            luz.transform.rotation = Quaternion.Euler(50f, 30f, 0f);
            GameObject jugadorGo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            SillaClinica silla = CrearSilla(config);
            yield return null;
            yield return null;
            if (Registro<ICartera>.Actual == null)
            {
                Registro<ICartera>.Agregar(new CarteraDoble());
            }
            ICartera cartera = Registro<ICartera>.Actual;
            cartera.Abonar(DineroInicial - cartera.Dinero, MotivoDinero.Otro);
            VidaAPie vida = jugadorGo.GetComponentInChildren<VidaAPie>();
            ControladorAPie controlador = jugadorGo.GetComponent<ControladorAPie>();
            Assert.IsNotNull(vida);
            Assert.IsNotNull(controlador);
            Assert.AreSame(silla, SillaClinica.Actual, "EL DOC encuentra la silla");

            // Sano: la silla no se ofrece.
            Assert.IsFalse(silla.PuedeUsar, "sano no te sientas");
            Assert.AreEqual(config.TextoSano, silla.Texto);

            vida.RecibirDanio(new InfoDanio(vida.Maxima * 0.5f, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.Less(vida.Actual, vida.Maxima);
            Assert.IsTrue(silla.PuedeUsar, "herido y con dinero se ofrece");
            StringAssert.Contains("Ƀ" + config.Costo, silla.Texto);

            float empezo = Time.time;
            yield return Pulsar(teclado.eKey);
            yield return EsperarHasta(() => silla.EnSecuencia, "sentarse con E");
            Assert.AreEqual(DineroInicial - config.Costo, cartera.Dinero, "cobra lo del Doc");
            Assert.IsFalse(controlador.ControlActivo, "el control queda congelado");
            Assert.IsTrue(silla.Sentado, "la pose de sentado está puesta");
            Assert.IsNotNull(silla.CamaraCercana, "cámara cercana");
            Assert.Greater(silla.CamaraCercana.depth, jugadorGo.GetComponentInChildren<Camera>().depth, "encima de la del jugador");
            Vector3 enSilla = jugadorGo.transform.position;
            Assert.Less(Vector2.Distance(new Vector2(enSilla.x, enSilla.z), new Vector2(0f, 2.2f)), 0.3f, "el jugador está en la silla");

            // A media secuencia: el brazo bajó y la barra va subiendo.
            yield return EsperarHasta(() => silla.Barra > 0.75f, "la barra se llena");
            Assert.Greater(silla.BajadaActual, config.BajadaBrazo * 0.9f, "el brazo bajó");
            if (Environment.GetEnvironmentVariable(VariableCaptura) == "1")
            {
                CapturaUI.Capturar("docs/capturas/s2n-silla-clinica.png", silla.CamaraCercana);
            }

            yield return EsperarHasta(() => !silla.EnSecuencia, "terminar la secuencia", config.Duracion + 3f);
            Assert.AreEqual(vida.Maxima, vida.Actual, 0.01f, "la vida quedó llena");
            Assert.AreEqual(DineroInicial - config.Costo, cartera.Dinero, "solo cobró una vez");
            Assert.IsFalse(silla.Sentado, "se levantó");
            Assert.IsNull(silla.CamaraCercana, "la cámara cercana se quitó");
            Assert.Less(Mathf.Abs(silla.BajadaActual), 0.01f, "el brazo subió");
            Assert.GreaterOrEqual(Time.time - empezo, config.Duracion * 0.9f, "la secuencia dura lo de la config");
            yield return null;
            Assert.IsTrue(controlador.ControlActivo, "el control volvió");
            Assert.Less(Vector3.Distance(jugadorGo.transform.position, new Vector3(0.9f, 0.05f, 2.2f)), 0.3f, "quedó parado junto a la silla");
            Assert.IsFalse(silla.PuedeUsar, "ya sano, no se ofrece otra vez");
        }
    }
}

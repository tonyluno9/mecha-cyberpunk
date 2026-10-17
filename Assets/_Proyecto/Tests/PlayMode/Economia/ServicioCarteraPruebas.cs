using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Progresion.Pruebas;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Economia
{
    /// <summary>La cartera del jugador: registro, guardado y las teclas de prueba (7 y 8) en Pruebas_Calle.</summary>
    public class ServicioCarteraPruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaCalle = "Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity";
        private const int DineroInicial = 500;
        private const int DineroGuardado = 1234;
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;

        private Keyboard teclado;
        private readonly HashSet<GameObject> raicesIniciales = new HashSet<GameObject>();

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            MapasDeAccionesDePrueba.Reiniciar(teclado);
            // El runner de pruebas vive como raíz de la escena activa: se anota para no destruirlo en el UnityTearDown.
            raicesIniciales.Clear();
            raicesIniciales.UnionWith(SceneManager.GetActiveScene().GetRootGameObjects());
        }

        public override void TearDown()
        {
            Registros.LimpiarTodos();
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (!raicesIniciales.Contains(raiz)) Object.Destroy(raiz);
            }
            yield return null;
            Registros.LimpiarTodos();
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

        private static GameObject CrearJugador()
        {
            return Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador));
        }

        private static IGuardable BuscarGuardable()
        {
            foreach (IGuardable g in Registro<IGuardable>.Todos)
            {
                if (g.ClaveGuardado == "cartera") return g;
            }
            return null;
        }

        [UnityTest]
        public IEnumerator Jugador_EnEscenaVacia_TraeUnaCarteraConElDineroInicial()
        {
            GameObject jugador = CrearJugador();
            yield return null;

            Assert.AreEqual(1, Registro<ICartera>.Cantidad, "Una sola cartera registrada");
            Assert.AreEqual(DineroInicial, Registro<ICartera>.Actual.Dinero);
            Assert.IsNotNull(BuscarGuardable(), "Hay un IGuardable con clave cartera");
            Assert.AreEqual(1, jugador.GetComponentsInChildren<ServicioCartera>(true).Length, "ServiciosEconomia anidado una vez");
        }

        [UnityTest]
        public IEnumerator Cargar_TrasGuardar_DevuelveElDineroYAvisaConCarga()
        {
            CrearJugador();
            yield return null;
            ICartera cartera = Registro<ICartera>.Actual;
            IGuardable guardable = BuscarGuardable();
            cartera.Abonar(DineroGuardado - DineroInicial, MotivoDinero.Otro);
            string texto = guardable.Guardar();
            cartera.CargarDeuda(1000, MotivoDinero.Otro);
            Assert.AreEqual(DineroGuardado - 1000, cartera.Dinero);

            int avisos = 0;
            MotivoDinero ultimo = MotivoDinero.Otro;
            int dif = 0;
            cartera.Cambio += (d, m) => { avisos++; dif = d; ultimo = m; };
            guardable.Cargar(texto);

            Assert.AreEqual(DineroGuardado, cartera.Dinero);
            Assert.AreEqual(1, avisos);
            Assert.AreEqual(1000, dif);
            Assert.AreEqual(MotivoDinero.Carga, ultimo);
        }

        [UnityTest]
        public IEnumerator Cargar_ConTextoQueNoSeEntiende_NoCambiaNadaYAvisa()
        {
            CrearJugador();
            yield return null;
            ICartera cartera = Registro<ICartera>.Actual;
            int avisos = 0;
            cartera.Cambio += (d, m) => avisos++;

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"\[Economia\].*cartera"));
            BuscarGuardable().Cargar("basura");

            Assert.AreEqual(DineroInicial, cartera.Dinero);
            Assert.AreEqual(0, avisos);
        }

        [UnityTest]
        public IEnumerator Destruir_ElJugador_QuitaLaCarteraDeLosRegistros()
        {
            GameObject jugador = CrearJugador();
            yield return null;
            Assert.AreEqual(1, Registro<ICartera>.Cantidad);

            Object.Destroy(jugador);
            yield return null;

            Assert.AreEqual(0, Registro<ICartera>.Cantidad);
            Assert.IsNull(BuscarGuardable(), "El IGuardable también se quitó");
        }

        [UnityTest]
        public IEnumerator PruebasCalle_TeclaSiete_Abona500_YTeclaOcho_Carga500()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaCalle, new LoadSceneParameters(LoadSceneMode.Single));
            yield return Calentar();
            ICartera cartera = Registro<ICartera>.Actual;
            Assert.IsNotNull(cartera, "Pruebas_Calle trae la cartera");
            int antes = cartera.Dinero;

            Press(teclado.digit7Key);
            yield return null;
            Release(teclado.digit7Key);
            yield return null;
            Assert.AreEqual(antes + 500, cartera.Dinero, "La tecla 7 abona 500");

            Press(teclado.digit8Key);
            yield return null;
            Release(teclado.digit8Key);
            yield return null;
            Assert.AreEqual(antes, cartera.Dinero, "La tecla 8 carga 500 de deuda");
        }
    }
}

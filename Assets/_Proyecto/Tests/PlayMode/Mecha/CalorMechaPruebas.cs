using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Calor;
using MechaCyberpunk.Mecha.Encendido;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.UI.Cabina;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mecha
{
    /// <summary>El calor policial por usar el mecha, en Pruebas_Calle con el jugador real y entrada simulada (M, E, W).</summary>
    public class CalorMechaPruebas : InputTestFixture
    {
        private const string RutaCalle = "Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity";
        private const string RutaTony = "Assets/_Proyecto/Escenas/Pruebas_Tony.unity";
        private const string RutaServicios = "Assets/_Proyecto/Progresion/Prefabs/ServiciosProgresion.prefab";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float MargenEspera = 2f;
        private const float DistanciaParaSubir = 2f;
        private const float MaxSegundosCaminar = 10f;
        private const float MaxSegundosEncender = 10f;
        private const float MaxSegundosFrenar = 6f;

        private Keyboard teclado;
        private Mouse raton;
        private JugadorAPie jugador;
        private MontajeMecha montaje;
        private LlamadoMecha llamado;
        private EncendidoMecha encendido;
        private ICalor calor;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            Registros.LimpiarTodos();
            base.TearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
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

        private IEnumerator CargarCalle()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaCalle, new LoadSceneParameters(LoadSceneMode.Single));
            yield return Calentar();
            jugador = Object.FindFirstObjectByType<JugadorAPie>();
            montaje = Object.FindFirstObjectByType<MontajeMecha>();
            llamado = montaje.GetComponent<LlamadoMecha>();
            encendido = montaje.GetComponent<EncendidoMecha>();
            calor = Registro<ICalor>.Actual;
            Assert.IsNotNull(jugador, "Pruebas_Calle trae el jugador");
            Assert.IsNotNull(llamado, "El mecha trae LlamadoMecha");
            Assert.IsNotNull(calor, "El jugador trae el servicio de calor");
            yield return null;
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private IEnumerator Llamar()
        {
            yield return Tecla(teclado.mKey);
            float fin = Time.time + llamado.Config.SegundosLlegada + MargenEspera;
            while (Time.time < fin && llamado.Estado != EstadoLlamado.EnCalle)
            {
                yield return null;
            }
            Assert.AreEqual(EstadoLlamado.EnCalle, llamado.Estado);
        }

        private IEnumerator SubirYEncender()
        {
            Press(teclado.wKey);
            float fin = Time.time + MaxSegundosCaminar;
            while (Time.time < fin && Vector3.Distance(jugador.Raiz.position, montaje.PuntoSubida) >= DistanciaParaSubir)
            {
                yield return null;
            }
            Release(teclado.wKey);
            yield return null;
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.Mecha, jugador.Modo, "La E frente al mecha lo sube");
            yield return Tecla(teclado.fKey);
            Press(teclado.spaceKey);
            fin = Time.time + MaxSegundosEncender;
            while (Time.time < fin && encendido.Paso != PasoEncendido.Paso3)
            {
                yield return null;
            }
            Release(teclado.spaceKey);
            yield return Tecla(teclado.qKey);
            while (Time.time < fin && !encendido.EstaEncendido)
            {
                yield return null;
            }
            Assert.IsTrue(encendido.EstaEncendido, "La cabina quedó encendida");
        }

        [UnityTest]
        public IEnumerator Llamar_AlLlegar_Suma10DeCalor()
        {
            yield return CargarCalle();
            Assert.AreEqual(0f, calor.Valor);

            yield return Tecla(teclado.mKey);

            Assert.AreEqual(10f, calor.Valor, 0.5f, "Llamar suma 10 al instante");
            Assert.AreEqual(0, calor.Nivel);
        }

        [UnityTest]
        public IEnumerator Llamar_FueraDeZona_NoSumaCalor()
        {
            yield return CargarCalle();
            jugador.Colocar(new Pose(new Vector3(0f, 0.05f, 90f), Quaternion.identity));
            yield return null;

            yield return Tecla(teclado.mKey);

            Assert.AreEqual(0f, calor.Valor);
        }

        [UnityTest]
        public IEnumerator Manejar_DiezSegundos_SumaVeinteYLaCabinaMuestraNivel1()
        {
            yield return CargarCalle();
            yield return Llamar();
            yield return SubirYEncender();
            float antes = calor.Valor;
            Assert.Greater(antes, 0f, "Ir dentro del mecha en la calle ya suma");

            Press(teclado.wKey);
            yield return Esperar(10f);
            Release(teclado.wKey);

            Assert.AreEqual(antes + 20f, calor.Valor, 1.5f, "Diez segundos manejando suman 2 por segundo");
            Assert.GreaterOrEqual(calor.Nivel, 1);
            PantallaCalorCabina pantalla = montaje.GetComponentInChildren<PantallaCalorCabina>(true);
            Assert.IsNotNull(pantalla, "El mecha trae la pantalla de calor");
            Assert.IsTrue(pantalla.Visible, "La cabina encendida la muestra");
            Assert.AreEqual(calor.Nivel, pantalla.MarcasLlenas);
            HudAPie hud = Object.FindFirstObjectByType<HudAPie>();
            Assert.IsFalse(hud.GrupoCalor.activeSelf, "En el mecha el HUD a pie no muestra el calor");
        }

        [UnityTest]
        public IEnumerator Bajar_YEsperar_ElCalorEnfria()
        {
            yield return CargarCalle();
            yield return Llamar();
            yield return SubirYEncender();
            Press(teclado.wKey);
            yield return Esperar(10f);
            Release(teclado.wKey);
            float fin = Time.time + MaxSegundosFrenar;
            while (Time.time < fin && !montaje.PuedeBajar)
            {
                yield return null;
            }
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "La E baja del mecha");
            float alBajar = calor.Valor;

            yield return Esperar(10f + 4f);
            Assert.AreEqual(alBajar - 20f, calor.Valor, 1.5f, "Tras 10 s de retraso, 4 s enfriando a 5 por segundo");

            HudAPie hud = Object.FindFirstObjectByType<HudAPie>();
            Assert.AreEqual(calor.Nivel > 0, hud.GrupoCalor.activeSelf, "A pie, el HUD muestra el calor solo si hay nivel");
        }

        [UnityTest]
        public IEnumerator Pelear_EnLaArena_NoSumaCalor()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaTony, new LoadSceneParameters(LoadSceneMode.Single));
            yield return Calentar();
            GameObject servicios = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaServicios));
            yield return null;
            ICalor arena = Registro<ICalor>.Actual;
            Assert.IsNotNull(arena, "Hay un ServicioCalor en la escena");
            Assert.IsNotNull(Object.FindFirstObjectByType<CalorPorMecha>(), "El mecha de la arena trae CalorPorMecha");

            float fin = Time.time + 10f;
            while (Time.time < fin)
            {
                Press(raton.leftButton);
                yield return null;
                Release(raton.leftButton);
                yield return null;
            }

            Assert.AreEqual(0f, arena.Valor, "En la arena no hay calor");
            Object.Destroy(servicios);
        }
    }
}

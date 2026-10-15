using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Encendido;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Mecha.Pruebas;
using MechaCyberpunk.Personaje.Jugador;
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
    /// <summary>Llamar al mecha desde el taller en Pruebas_Calle con el jugador real y entrada simulada (M, E, W).</summary>
    public class LlamadoMechaPruebas : InputTestFixture
    {
        private const string RutaCalle = "Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity";
        private const string RutaCarro = "Assets/_Proyecto/Personaje/Pruebas/CarroDePrueba.prefab";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float ToleranciaSegundos = 0.2f;
        private const float ToleranciaMetros = 0.1f;
        private const float MargenEspera = 2f;
        private const float DistanciaParaSubir = 2f;
        private const float MaxSegundosCaminar = 10f;
        private const float MaxSegundosEncender = 10f;
        private const float MaxSegundosFrenar = 6f;
        private const float ZFueraDeZona = 90f;
        private const float ZLejos = 85f;
        private const float AlturaJugadorFuera = 0.05f;
        private static readonly Vector3 PosicionCarro = new Vector3(2f, 0f, 1.1f);

        private Keyboard teclado;
        private Mouse raton;
        private JugadorAPie jugador;
        private MontajeMecha montaje;
        private LlamadoMecha llamado;
        private TallerDePrueba taller;
        private PuntoLlamadoDePrueba punto;
        private Pose poseInicialMecha;
        private readonly List<ResultadoLlamado> intentos = new List<ResultadoLlamado>();

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            intentos.Clear();
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

        private IEnumerator CargarCalle()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaCalle, new LoadSceneParameters(LoadSceneMode.Single));
            int estables = 0;
            int intentosCalentar = 0;
            while (estables < FramesEstables && intentosCalentar < MaxFramesCalentamiento)
            {
                yield return null;
                intentosCalentar++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
            jugador = Object.FindFirstObjectByType<JugadorAPie>();
            montaje = Object.FindFirstObjectByType<MontajeMecha>();
            llamado = montaje.GetComponent<LlamadoMecha>();
            taller = Object.FindFirstObjectByType<TallerDePrueba>();
            punto = Object.FindFirstObjectByType<PuntoLlamadoDePrueba>();
            Assert.IsNotNull(jugador, "Pruebas_Calle trae el jugador");
            Assert.IsNotNull(llamado, "El mecha trae LlamadoMecha");
            Assert.IsNotNull(taller, "Pruebas_Calle trae el taller");
            Assert.IsNotNull(punto, "Pruebas_Calle trae un punto de llamado");
            // La pose del mecha en la escena, antes de que el taller lo recoja, es su casa si no hay taller.
            poseInicialMecha = new Pose(new Vector3(0f, 0.05f, 8f), Quaternion.Euler(0f, 180f, 0f));
            llamado.LlamadoIntentado += resultado => intentos.Add(resultado);
            yield return null;
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private IEnumerator EsperarEstado(EstadoLlamado estado, float maxSegundos)
        {
            float fin = Time.time + maxSegundos;
            while (Time.time < fin && llamado.Estado != estado)
            {
                yield return null;
            }
            Assert.AreEqual(estado, llamado.Estado, "Llegó al estado " + estado);
        }

        private int RenderersPrendidos()
        {
            int cuenta = 0;
            foreach (Renderer render in montaje.GetComponentsInChildren<Renderer>(true))
            {
                if (render.enabled && render.gameObject.activeInHierarchy)
                {
                    cuenta++;
                }
            }
            return cuenta;
        }

        // Pulsa M y espera a que el mecha llegue; devuelve cuánto tardó (s).
        private IEnumerator LlamarYEsperar(System.Action<float> alTerminar)
        {
            float inicio = Time.time;
            yield return Tecla(teclado.mKey);
            Assert.AreEqual(EstadoLlamado.Llegando, llamado.Estado, "M en zona hace que el mecha venga");
            yield return EsperarEstado(EstadoLlamado.EnCalle, llamado.Config.SegundosLlegada + MargenEspera);
            alTerminar(Time.time - inicio);
        }

        private IEnumerator CaminarAlMecha()
        {
            Press(teclado.wKey);
            float fin = Time.time + MaxSegundosCaminar;
            while (Time.time < fin && Vector3.Distance(jugador.Raiz.position, montaje.PuntoSubida) >= DistanciaParaSubir)
            {
                yield return null;
            }
            Release(teclado.wKey);
            yield return null;
            Assert.Less(Vector3.Distance(jugador.Raiz.position, montaje.PuntoSubida), DistanciaParaSubir, "Llegó al mecha");
        }

        private IEnumerator SubirYEncender()
        {
            yield return CaminarAlMecha();
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.Mecha, jugador.Modo, "La E frente al mecha lo sube");
            EncendidoMecha encendido = montaje.GetComponent<EncendidoMecha>();
            yield return Tecla(teclado.fKey);
            Press(teclado.spaceKey);
            float fin = Time.time + MaxSegundosEncender;
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

        private IEnumerator Bajar()
        {
            float fin = Time.time + MaxSegundosFrenar;
            while (Time.time < fin && !montaje.PuedeBajar)
            {
                yield return null;
            }
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "La E baja del mecha");
        }

        [UnityTest]
        public IEnumerator Llamar_EnZona_MechaDejaElTallerYBajaDelCieloEnSeisSegundos()
        {
            yield return CargarCalle();
            Vector3 casa = taller.Estacionamiento.position;
            Vector3 llegada = punto.Llegada.position;
            Assert.Less(Vector3.Distance(montaje.transform.position, casa), ToleranciaMetros, "Arranca en el taller");
            Assert.AreEqual(EstadoLlamado.EnTaller, llamado.Estado);
            Assert.IsTrue(montaje.PuedeSubir);
            Assert.Greater(RenderersPrendidos(), 0, "Se ve en el taller");

            float inicio = Time.time;
            yield return Tecla(teclado.mKey);

            Assert.AreEqual(EstadoLlamado.Llegando, llamado.Estado);
            Assert.AreEqual(ResultadoLlamado.Llega, intentos[0]);
            Assert.IsFalse(montaje.PuedeSubir, "Mientras llega no se puede subir");
            Assert.AreEqual(0, RenderersPrendidos(), "Desaparece del taller");
            Assert.Greater(llamado.SegundosParaLlegar, 5f);

            // Una segunda M mientras viene no cambia nada.
            yield return Tecla(teclado.mKey);
            Assert.AreEqual(ResultadoLlamado.YaViene, intentos[1]);
            Assert.AreEqual(EstadoLlamado.Llegando, llamado.Estado);

            // A mitad de camino sigue sin verse ni poder subir.
            yield return Esperar(2f);
            Assert.AreEqual(0, RenderersPrendidos(), "Aún no se ve");
            Assert.IsFalse(montaje.PuedeSubir);

            // En el descenso se ve en el aire, encima de la llegada.
            float limite = Time.time + 6f;
            while (Time.time < limite && RenderersPrendidos() == 0)
            {
                yield return null;
            }
            Assert.Greater(RenderersPrendidos(), 0, "Baja del cielo a la vista");
            Assert.Greater(montaje.transform.position.y, llegada.y + 1f, "Todavía está en el aire");
            Assert.Less(Mathf.Abs(montaje.transform.position.x - llegada.x), 0.01f);
            Assert.IsFalse(montaje.PuedeSubir, "Bajando todavía no se puede subir");

            yield return EsperarEstado(EstadoLlamado.EnCalle, 3f);
            float tardo = Time.time - inicio;
            Assert.AreEqual(llamado.Config.SegundosLlegada, tardo, ToleranciaSegundos, "Tarda unos 6 s");
            Assert.Less(Vector3.Distance(montaje.transform.position, llegada), ToleranciaMetros, "Aterriza en la llegada");
            Assert.IsTrue(montaje.PuedeSubir);
            Assert.IsTrue(montaje.GetComponent<CharacterController>().enabled, "Su collider vuelve a estar prendido");
        }

        [UnityTest]
        public IEnumerator Subir_ConElMechaLlegado_ConEYEnciende()
        {
            yield return CargarCalle();
            yield return LlamarYEsperar(_ => { });
            yield return SubirYEncender();

            Assert.AreEqual(ModoJugador.Mecha, jugador.Modo);
            Assert.IsTrue(montaje.Ocupado);
            Assert.AreEqual(EstadoLlamado.EnCalle, llamado.Estado);
        }

        [UnityTest]
        public IEnumerator Devolver_ConMVacioEnLaCalle_RegresaAlTallerEnDosSegundos()
        {
            yield return CargarCalle();
            yield return LlamarYEsperar(_ => { });
            yield return SubirYEncender();
            yield return Bajar();
            intentos.Clear();

            float inicio = Time.time;
            yield return Tecla(teclado.mKey);

            Assert.AreEqual(ResultadoLlamado.Regresa, intentos[0]);
            Assert.AreEqual(EstadoLlamado.Regresando, llamado.Estado);
            Assert.IsFalse(montaje.PuedeSubir, "Mientras regresa no se puede subir");

            yield return EsperarEstado(EstadoLlamado.EnTaller, llamado.Config.SegundosRegreso + MargenEspera);
            Assert.AreEqual(llamado.Config.SegundosRegreso, Time.time - inicio, ToleranciaSegundos, "Tarda unos 2 s");
            Assert.Less(Vector3.Distance(montaje.transform.position, taller.Estacionamiento.position), ToleranciaMetros,
                "Quedó en el estacionamiento");
            Assert.IsTrue(montaje.PuedeSubir);
            Assert.Greater(RenderersPrendidos(), 0, "Se ve otra vez en el taller");
        }

        [UnityTest]
        public IEnumerator Llamar_FueraDeZona_ElMechaNoSeMueve()
        {
            yield return CargarCalle();
            Vector3 antes = montaje.transform.position;
            jugador.Colocar(new Pose(new Vector3(0f, AlturaJugadorFuera, ZFueraDeZona), Quaternion.identity));
            yield return null;

            yield return Tecla(teclado.mKey);

            Assert.AreEqual(ResultadoLlamado.ZonaProhibida, intentos[0]);
            Assert.AreEqual(EstadoLlamado.EnTaller, llamado.Estado);
            Assert.AreEqual(0f, Vector3.Distance(antes, montaje.transform.position), 0.001f, "El mecha sigue en el taller");
            Assert.Greater(RenderersPrendidos(), 0, "Sigue a la vista en el taller");
            HudAPie hud = Object.FindFirstObjectByType<HudAPie>();
            Assert.IsTrue(hud.GrupoLlamado.activeSelf, "El HUD avisa");
            Assert.AreEqual("AQUÍ NO PUEDE LLEGAR TU MECHA", hud.TextoLlamado.text);
        }

        [UnityTest]
        public IEnumerator Llamar_EnCarro_MNoHaceNada()
        {
            yield return CargarCalle();
            GameObject carroObj = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro), PosicionCarro, Quaternion.identity);
            yield return null;
            yield return null;
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(ModoJugador.Carro, jugador.Modo, "La E frente al carro lo sube");
            Vector3 antes = montaje.transform.position;

            yield return Tecla(teclado.mKey);
            yield return Esperar(0.5f);

            Assert.AreEqual(0, intentos.Count, "En carro la M no es un intento");
            Assert.AreEqual(EstadoLlamado.EnTaller, llamado.Estado);
            Assert.AreEqual(0f, Vector3.Distance(antes, montaje.transform.position), 0.001f);
            Object.Destroy(carroObj);
        }

        [UnityTest]
        public IEnumerator Llamar_DentroDelMecha_MNoHaceNada()
        {
            yield return CargarCalle();
            yield return LlamarYEsperar(_ => { });
            yield return SubirYEncender();
            Vector3 antes = montaje.transform.position;
            intentos.Clear();

            yield return Tecla(teclado.mKey);
            yield return Esperar(0.5f);

            Assert.AreEqual(0, intentos.Count, "Dentro del mecha la M no es un intento");
            Assert.AreEqual(EstadoLlamado.EnCalle, llamado.Estado);
            Assert.AreEqual(0f, Vector3.Distance(antes, montaje.transform.position), 0.2f, "El mecha no se fue");
        }

        [UnityTest]
        public IEnumerator Alejarse_ConElMechaVacioEnLaCalle_LoMandaAlTaller()
        {
            yield return CargarCalle();
            yield return LlamarYEsperar(_ => { });
            Assert.AreEqual(EstadoLlamado.EnCalle, llamado.Estado);
            yield return Esperar(0.5f);
            Assert.AreEqual(EstadoLlamado.EnCalle, llamado.Estado, "Cerca del jugador se queda");

            jugador.Colocar(new Pose(new Vector3(0f, AlturaJugadorFuera, ZLejos), Quaternion.identity));
            float horizontal = ReglaLlamado.DistanciaHorizontal(jugador.Raiz.position, montaje.transform.position);
            Assert.Greater(horizontal, llamado.Config.DistanciaRegresoAutomatico, "El jugador quedó a más de 60 m");

            float inicio = Time.time;
            yield return EsperarEstado(EstadoLlamado.Regresando, 1f);
            yield return EsperarEstado(EstadoLlamado.EnTaller, llamado.Config.SegundosRegreso + MargenEspera);
            Assert.AreEqual(llamado.Config.SegundosRegreso, Time.time - inicio, 0.5f);
            Assert.Less(Vector3.Distance(montaje.transform.position, taller.Estacionamiento.position), ToleranciaMetros);
        }

        [UnityTest]
        public IEnumerator Devolver_SinTallerRegistrado_RegresaASuPoseInicial()
        {
            yield return CargarCalle();
            taller.gameObject.SetActive(false);
            Assert.AreEqual(0, Registro<ITallerMecha>.Cantidad, "Sin taller registrado");
            yield return LlamarYEsperar(_ => { });

            yield return Tecla(teclado.mKey);
            Assert.AreEqual(EstadoLlamado.Regresando, llamado.Estado);
            yield return EsperarEstado(EstadoLlamado.EnTaller, llamado.Config.SegundosRegreso + MargenEspera);

            Assert.Less(Vector3.Distance(montaje.transform.position, poseInicialMecha.position), ToleranciaMetros,
                "Vuelve a donde arrancó en la escena");
            Assert.Less(Quaternion.Angle(montaje.transform.rotation, poseInicialMecha.rotation), 1f);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-143: Pruebas_Economia. Lo que la escena registra al arrancar y el recorrido del inicio al mostrador de peleas con entrada
    /// simulada (W), más las teclas de prueba de dinero (7) e inventario (9 y 0).
    /// </summary>
    public class EscenaEconomiaPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Economia.unity";
        private const string RutaBrazo = "Assets/_Proyecto/Economia/Datos/DatosObjeto_BrazoChatarra.asset";
        private const string RutaKit = "Assets/_Proyecto/Economia/Datos/DatosObjeto_KitReparacion.asset";
        private const string TextoOfrecido = "E · Peleas del clandestino"; // T-160: en Libre el mostrador ofrece la cartelera
        private const int AbonoDePrueba = 500;
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float SegundosSinErrores = 5f;
        private const float MaxSegundosRecorrido = 15f;
        private const float ToleranciaPose = 0.1f;
        private const float DistanciaBanco = 3f;
        private const float ToleranciaBanco = 0.05f;

        private Keyboard teclado;
        private Mouse raton;
        private string carpeta;
        private readonly List<string> errores = new List<string>();
        private HashSet<GameObject> raicesIniciales;


        public override void Setup()
        {
            raicesIniciales = RaicesDePrueba.Anotar();
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
            carpeta = Path.Combine(Application.temporaryCachePath, "escena_economia_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            errores.Clear();
            Application.logMessageReceived += RegistrarError;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= RegistrarError;
            ServicioGuardado.RutaForzada = null;
            if (Directory.Exists(carpeta))
            {
                Directory.Delete(carpeta, true);
            }
            Registros.LimpiarTodos();
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado, raton);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            RaicesDePrueba.DestruirNuevas(raicesIniciales);
            yield return null;
            Registros.LimpiarTodos();
        }

        private void RegistrarError(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert)
            {
                errores.Add(mensaje);
            }
        }

        private static IEnumerator CargarEscena()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
        }

        private IEnumerator PulsarTecla(UnityEngine.InputSystem.Controls.KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Arranca_RegistraLaEconomiaYElTallerConSuMecha()
        {
            yield return CargarEscena();

            Assert.AreEqual(1, Registro<ICartera>.Cantidad, "una cartera");
            Assert.AreEqual(1, Registro<ServicioInventario>.Cantidad, "un inventario");
            Assert.AreEqual(1, Registro<IRenta>.Cantidad, "una renta");
            Assert.AreEqual(1, Registro<IRelojMundo>.Cantidad, "un reloj");
            Assert.IsTrue(Registro<IRelojMundo>.Actual.EsDeNoche, "es de noche");
            Assert.AreEqual(1, Registro<IHistoria>.Cantidad, "una historia");
            Assert.AreEqual(EtapaPrologo.Libre, Registro<IHistoria>.Actual.Etapa, "la historia queda en Libre");

            int mechas = 0;
            Vector3 posicionMecha = Vector3.zero;
            foreach (IMontable montable in Registro<IMontable>.Todos)
            {
                if (montable.Modo == ModoJugador.Mecha)
                {
                    mechas++;
                    posicionMecha = ((Component)montable).transform.position;
                }
            }
            Assert.AreEqual(1, mechas, "un mecha montable");
            Assert.AreEqual(1, Registro<ITallerMecha>.Cantidad, "un taller");
            ITallerMecha taller = Registro<ITallerMecha>.Actual;
            Assert.Less(Vector3.Distance(posicionMecha, taller.Estacionamiento.position), ToleranciaPose, "el mecha está estacionado en el taller (m)");
            Assert.AreEqual(DistanciaBanco, Vector3.Distance(taller.PuntoInteraccion, taller.Estacionamiento.position), ToleranciaBanco, "el banco a 3 m del mecha (m)");

            bool mercado = false;
            foreach (ILugar lugar in Registro<ILugar>.Todos)
            {
                mercado |= lugar.Id == IdLugar.MercadoNegro && lugar.EstaAbierto;
            }
            Assert.IsTrue(mercado, "el mercado negro está abierto");
            Assert.IsNotNull(GameObject.Find("Sitio_Mercado"), "existe Sitio_Mercado");

            float fin = Time.time + SegundosSinErrores;
            while (Time.time < fin)
            {
                yield return null;
            }
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Recorrido_ConW_LlegaAlMostradorDePeleasYElHudOfreceLaPelea()
        {
            yield return CargarEscena();
            HudAPie hud = Object.FindFirstObjectByType<HudAPie>();
            Assert.IsNotNull(hud, "El jugador trae el HUD a pie");
            JugadorAPie jugador = Object.FindFirstObjectByType<JugadorAPie>();
            Assert.IsNotNull(jugador, "Hay jugador");

            float inicio = Time.time;
            Press(teclado.wKey);
            while (hud.TextoAvisoInteraccion.text != TextoOfrecido && Time.time - inicio < MaxSegundosRecorrido)
            {
                yield return null;
            }
            float tardo = Time.time - inicio;
            Release(teclado.wKey);
            yield return null;

            Assert.AreEqual(TextoOfrecido, hud.TextoAvisoInteraccion.text, "El HUD ofrece la pelea casera (tardó " + tardo + " s)");
            Assert.Less(tardo, MaxSegundosRecorrido, "Llega al mostrador en menos de 15 s de juego");
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Teclas_7_9_0_CambianDineroEInventario()
        {
            yield return CargarEscena();
            ICartera cartera = Registro<ICartera>.Actual;
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            DatosObjeto brazo = AssetDatabase.LoadAssetAtPath<DatosObjeto>(RutaBrazo);
            DatosObjeto kit = AssetDatabase.LoadAssetAtPath<DatosObjeto>(RutaKit);
            Assert.IsNotNull(cartera);
            Assert.IsNotNull(inventario);
            Assert.AreEqual(0, inventario.Cantidad(brazo), "sin brazo al inicio");
            Assert.AreEqual(0, inventario.Cantidad(kit), "sin kit al inicio");

            int dinero = cartera.Dinero;
            yield return PulsarTecla(teclado.digit7Key);
            Assert.AreEqual(dinero + AbonoDePrueba, cartera.Dinero, "7 abona $500");

            yield return PulsarTecla(teclado.digit9Key);
            Assert.AreEqual(1, inventario.Cantidad(brazo), "9 agrega un Brazo de chatarra");
            Assert.AreEqual(0, inventario.Cantidad(kit), "9 no agrega kits");

            yield return PulsarTecla(teclado.digit0Key);
            Assert.AreEqual(1, inventario.Cantidad(kit), "0 agrega un kit de reparación");
            Assert.AreEqual(1, inventario.Cantidad(brazo), "0 no toca el brazo");

            yield return PulsarTecla(teclado.digit8Key);
            Assert.AreEqual(dinero, cartera.Dinero, "8 carga $500 de deuda y vuelve al dinero inicial");
            Assert.IsEmpty(errores, "consola sin errores: " + string.Join(" | ", errores));
        }
    }
}

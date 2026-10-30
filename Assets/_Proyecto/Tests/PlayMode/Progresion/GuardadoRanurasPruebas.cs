using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.Tests.UI;
using MechaCyberpunk.UI.Guardado;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-412 en Ciudad_Prologo con una ruta temporal (<see cref="ServicioGuardado.RutaForzada"/>): con Ƀ340 y respeto 30 en el Tianguis camina
    /// con W; F5 abre las ranuras, ↓ hasta la RANURA 2 y Enter guarda; cambia el dinero y el respeto y camina más; F9, ↓ hasta la RANURA 2 y
    /// Enter carga (la escena se recarga por FlujoPartida); dinero, respeto, paso de la historia y posición vuelven. Al final conquistar el
    /// Tianguis escribe el AUTOGUARDADO y sale "GUARDADO". Atajos: el dinero, el respeto, la RANURA 1 y la conquista se ponen por código y
    /// los grupos de pandilleros se apagan. Con CAPTURAR_T412=1 guarda docs/capturas/T-412-1 (CARGAR con dos llenas) en 1080p y 1440p.
    /// </summary>
    public class GuardadoRanurasPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string VariableCaptura = "CAPTURAR_T412";
        private const string Tianguis = "tianguis";
        private static readonly Vector3 Inicio = new Vector3(-20f, 0.5f, 0f);
        private const int DineroRanura1 = 120;
        private const int DineroGuardado = 340;
        private const int RespetoGuardado = 30;
        private const float MetrosCaminar = 2f;
        private const float SegundosMaxCaminar = 5f;
        private const float ToleranciaPosicion = 0.5f;

        private Keyboard teclado;
        private Mouse raton;
        private string carpeta;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            // Si otra prueba dejó los mapas prendidos con los dispositivos viejos, W no llegaría al jugador (como en CallejonAlanPruebas).
            DescartarEstadoDeMapas(new InputDevice[] { teclado, raton });
            carpeta = Path.Combine(Path.GetTempPath(), "t412_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
        }

        public override void TearDown()
        {
            ServicioGuardado.RutaForzada = null;
            Time.timeScale = 1f;
            try
            {
                Directory.Delete(carpeta, true);
            }
            catch (Exception)
            {
                // La carpeta temporal se queda; no importa.
            }
            InputSystem.DisableAllEnabledActions();
            DescartarEstadoDeMapas(new InputDevice[] { teclado, raton });
            base.TearDown();
            DescartarEstadoDeMapas(new InputDevice[0]);
        }

        // Cambiar `devices` en un mapa apagado descarta su estado de resolución: la escena los resuelve de nuevo al prenderlos.
        private static void DescartarEstadoDeMapas(InputDevice[] dispositivos)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:InputActionAsset"))
            {
                InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    asset.Disable();
                    asset.devices = dispositivos;
                    asset.devices = null;
                }
            }
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                UnityEngine.Object.Destroy(raiz);
            }
            Registros.LimpiarTodos();
            yield return null;
        }

        private static IEnumerator EsperarHasta(Func<bool> condicion, string que, float segundos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            while (!condicion())
            {
                Assert.Less(Time.realtimeSinceStartup, fin, "Tiempo agotado esperando: " + que);
                yield return null;
            }
        }

        private IEnumerator Pulsar(ButtonControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
            yield return null;
        }

        // Camina con W hasta avanzar la distancia (con tiempo límite: el primer cuadro tras cargar la escena puede ser muy largo).
        private IEnumerator Caminar(float metros)
        {
            Transform raiz = Registro<IJugador>.Actual.Raiz;
            Vector3 desde = raiz.position;
            desde.y = 0f;
            Press(teclado.wKey);
            float fin = Time.realtimeSinceStartup + SegundosMaxCaminar;
            while (Time.realtimeSinceStartup < fin)
            {
                Vector3 ahora = raiz.position;
                ahora.y = 0f;
                if (Vector3.Distance(desde, ahora) >= metros)
                {
                    break;
                }
                yield return null;
            }
            Release(teclado.wKey);
            yield return null;
            yield return null;
        }

        private static IEnumerator EsperarCiudad(DirectorPandillas anterior, Action<DirectorPandillas> alListo)
        {
            DirectorPandillas director = null;
            yield return EsperarHasta(() =>
            {
                director = UnityEngine.Object.FindFirstObjectByType<DirectorPandillas>();
                return director != null && director != anterior && director.Iniciado && Registro<IJugador>.Actual != null
                    && Registro<ITerritorio>.Actual != null && Registro<ICartera>.Actual != null && ServicioGuardado.Actual != null;
            }, "la ciudad arrancó", 30f);
            alListo(director);
        }

        private static void ApagarPandillas()
        {
            foreach (PandillaAPie grupo in UnityEngine.Object.FindObjectsByType<PandillaAPie>(FindObjectsSortMode.None))
            {
                grupo.gameObject.SetActive(false);
            }
        }

        private static void FijarDinero(int cantidad)
        {
            ICartera cartera = Registro<ICartera>.Actual;
            int diferencia = cantidad - cartera.Dinero;
            if (diferencia > 0) cartera.Abonar(diferencia, MotivoDinero.Otro);
            else if (diferencia < 0) cartera.CargarDeuda(-diferencia, MotivoDinero.Otro);
            Assert.AreEqual(cantidad, cartera.Dinero);
        }

        private static void Capturar(string nombre, Camera camara)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                return;
            }
            CapturaUI.Capturar("docs/capturas/" + nombre + ".png", camara);
            CapturaUI.Capturar2K("docs/capturas/" + nombre + "-1440.png", camara);
        }

        [UnityTest]
        public IEnumerator RecorridoGuardarYCargar()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            DirectorPandillas director = null;
            yield return EsperarCiudad(null, d => director = d);
            yield return new WaitForSecondsRealtime(0.5f);
            ApagarPandillas();
            IJugador jugador = Registro<IJugador>.Actual;
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            ServicioGuardado servicio = ServicioGuardado.Actual;
            PantallaRanuras pantalla = UnityEngine.Object.FindFirstObjectByType<PantallaRanuras>();
            AvisoGuardado aviso = UnityEngine.Object.FindFirstObjectByType<AvisoGuardado>();
            Assert.IsNotNull(pantalla, "La ciudad trae la pantalla de ranuras");
            Assert.IsNotNull(aviso, "La ciudad trae el aviso GUARDADO");
            Assert.AreEqual(-1, servicio.RanuraMasReciente(), "La ruta temporal empieza sin partidas");

            // En la calle del Tianguis mirando al este, con respeto 30.
            jugador.Colocar(new Pose(Inicio, Quaternion.LookRotation(Vector3.right)));
            yield return null;
            Assert.AreEqual(Tianguis, territorio.ZonaEn(jugador.Raiz.position));
            while (territorio.Respeto(Tianguis) < RespetoGuardado)
            {
                territorio.RegistrarVencido(Inicio, IdPandilla.Calaveras, false);
            }
            Assert.AreEqual(RespetoGuardado, territorio.Respeto(Tianguis));

            // Atajo: la RANURA 1 se escribe por código con otro dinero, para que CARGAR muestre dos llenas.
            FijarDinero(DineroRanura1);
            Assert.IsTrue(servicio.Guardar(1));
            FijarDinero(DineroGuardado);

            // a. Camina con W y guarda en la RANURA 2 con F5, ↓ y Enter (la 1 está llena; la 2 vacía no pide confirmar).
            yield return Caminar(MetrosCaminar);
            Vector3 guardada = jugador.Raiz.position;
            Assert.Greater(Vector3.Distance(Inicio, guardada), 1f, "W movió al jugador");
            PasoHistoria pasoGuardado = director.Paso;
            // T-410 quitó F5 y F9: ahora las abre la pausa (PausaPruebas lo recorre); aquí se abren directo.
            pantalla.Abrir(ModoRanuras.Guardar);
            yield return null;
            Assert.IsTrue(pantalla.Abierta, "Se abren las ranuras en GUARDAR");
            Assert.AreEqual(ModoRanuras.Guardar, pantalla.Modo);
            Assert.IsFalse(pantalla.Fila(ServicioGuardado.RanuraAuto).interactable, "En GUARDAR el AUTOGUARDADO está desactivado");
            Assert.AreSame(pantalla.Fila(1), pantalla.Menu.Elegido, "Empieza en la RANURA 1");
            yield return Pulsar(teclado.downArrowKey);
            Assert.AreSame(pantalla.Fila(2), pantalla.Menu.Elegido, "↓ elige la RANURA 2");
            yield return Pulsar(teclado.enterKey);
            Assert.IsFalse(pantalla.Abierta, "Guardar cierra la pantalla");
            Assert.IsTrue(aviso.Visible, "Sale GUARDADO");
            ResumenRanura resumen = servicio.ResumenDe(2);
            Assert.IsFalse(resumen.Vacia);
            Assert.AreEqual(DineroGuardado, resumen.Dinero);
            Assert.AreEqual("EL TIANGUIS", resumen.Zona);
            Assert.AreEqual(2, servicio.RanuraMasReciente());

            // b. Cambia el dinero y el respeto y camina más.
            FijarDinero(DineroGuardado + 500);
            territorio.RegistrarVencido(Inicio, IdPandilla.Calaveras, false);
            Assert.AreNotEqual(RespetoGuardado, territorio.Respeto(Tianguis));
            yield return Caminar(MetrosCaminar);
            Assert.Greater(Vector3.Distance(guardada, jugador.Raiz.position), 1f, "Caminó más");

            // c. F9 abre CARGAR: AUTO y la 3 vacías y desactivadas, la 1 y la 2 llenas. Captura.
            pantalla.Abrir(ModoRanuras.Cargar);
            yield return null;
            Assert.IsTrue(pantalla.Abierta, "Se abren las ranuras en CARGAR");
            Assert.AreEqual(ModoRanuras.Cargar, pantalla.Modo);
            Assert.IsFalse(pantalla.Fila(ServicioGuardado.RanuraAuto).interactable);
            Assert.IsFalse(pantalla.Fila(3).interactable, "La vacía está desactivada");
            Assert.IsTrue(pantalla.Fila(1).interactable);
            Assert.IsTrue(pantalla.Fila(2).interactable);
            StringAssert.Contains("Ƀ340", pantalla.Fila(2).Texto.text);
            StringAssert.Contains("EL TIANGUIS", pantalla.Fila(2).Texto.text);
            StringAssert.Contains("VACÍA", pantalla.Fila(3).Texto.text);
            yield return null;
            Capturar("T-412-1", jugador.Camara);
            yield return Pulsar(teclado.downArrowKey);
            Assert.AreSame(pantalla.Fila(2), pantalla.Menu.Elegido, "↓ elige la RANURA 2");
            Press(teclado.enterKey);
            yield return null;
            Release(teclado.enterKey);

            // d. La escena se recarga y la ranura 2 deja todo como estaba.
            yield return EsperarCiudad(director, d => director = d);
            yield return new WaitForSecondsRealtime(0.5f);
            jugador = Registro<IJugador>.Actual;
            territorio = Registro<ITerritorio>.Actual;
            Assert.AreEqual(DineroGuardado, Registro<ICartera>.Actual.Dinero, "El dinero vuelve");
            Assert.AreEqual(RespetoGuardado, territorio.Respeto(Tianguis), "El respeto vuelve");
            Assert.AreEqual(pasoGuardado, director.Paso, "El paso de la historia vuelve");
            float distancia = Vector3.Distance(guardada, jugador.Raiz.position);
            Debug.Log("[Prueba] guardada " + guardada + " cargada " + jugador.Raiz.position + " distancia " + distancia);
            Assert.Less(distancia, ToleranciaPosicion, "La posición vuelve a menos de 0.5 m");

            // e. Conquistar el Tianguis escribe el AUTOGUARDADO, sale GUARDADO y es la más reciente.
            ApagarPandillas();
            servicio = ServicioGuardado.Actual;
            aviso = UnityEngine.Object.FindFirstObjectByType<AvisoGuardado>();
            Assert.IsTrue(servicio.ResumenDe(ServicioGuardado.RanuraAuto).Vacia);
            territorio.RegistrarVencido(Inicio, IdPandilla.Calaveras, true);
            yield return EsperarHasta(() => !servicio.ResumenDe(ServicioGuardado.RanuraAuto).Vacia, "el autoguardado", 5f);
            Assert.IsTrue(aviso.Visible, "Sale GUARDADO al conquistar");
            Assert.AreEqual(ServicioGuardado.RanuraAuto, servicio.RanuraMasReciente());
        }
    }
}

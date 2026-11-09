using System;
using System.Collections;
using System.IO;
using System.Reflection;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Aliados;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Opciones;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Creditos;
using MechaCyberpunk.UI.Guardado;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Mapa;
using MechaCyberpunk.UI.MenuPausa;
using MechaCyberpunk.UI.Opciones;
using MechaCyberpunk.UI.Tiendas;
using MechaCyberpunk.UI.Titulo;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-522: los menús con las piezas de UI (marco 9-slice, botones de UI02, logo UI13 y fondo UI14) y el pulido del HUD. Título y creación con
    /// el título en el editor; pausa, ranuras, opciones, mapa, créditos, tienda de El Mercado, CIC, aviso de zona conquistada y HUD en la ciudad.
    /// Con CAPTURAR_T522=1 guarda docs/capturas/T-522-&lt;pantalla&gt; (1080p) y -1440.
    /// </summary>
    public class MenusCyberfixPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string VariableCaptura = "CAPTURAR_T522";
        private static readonly Vector3 Inicio = new Vector3(-20f, 0.5f, 0f);

        private Keyboard teclado;
        private Mouse raton;
        private string carpeta;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            DescartarEstadoDeMapas(new InputDevice[] { teclado, raton });
            carpeta = Path.Combine(Path.GetTempPath(), "t522_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            ServicioOpciones.FijarRuta(Path.Combine(carpeta, ServicioOpciones.NombreArchivo));
        }

        public override void TearDown()
        {
            FlujoPartida.TituloEnEditor = false;
            ServicioGuardado.RutaForzada = null;
            ServicioOpciones.FijarRuta(null);
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Pausa.Fijar(false);
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
            Time.timeScale = 1f;
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

        private static void Capturar(string nombre, Camera camara)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                return;
            }
            CapturaUI.Capturar("docs/capturas/T-522-" + nombre + ".png", camara);
            CapturaUI.Capturar2K("docs/capturas/T-522-" + nombre + "-1440.png", camara);
        }

        private static void CapturarS2m(string archivo, Camera camara)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) == "1")
            {
                CapturaUI.Capturar("docs/capturas/" + archivo, camara);
            }
        }

        // T-897: los paneles de pausa, mapa y tienda ya no usan el marco naranja de UI01 sino el H10.
        private static void AssertTurquesa(Image panel, string que)
        {
            Assert.IsNotNull(panel, que);
            Assert.AreNotSame(PiezasUI.Actual.MarcoPanel, panel.sprite, que + " sin el marco naranja");
            Assert.IsNotNull(panel.transform.Find("MarcoH10"), que + " con el marco H10");
        }

        private static void AssertMarco(Image panel, string que)
        {
            Assert.IsNotNull(panel, que);
            Assert.AreSame(PiezasUI.Actual.MarcoPanel, panel.sprite, que + " usa el marco de UI01");
            Assert.AreEqual(Image.Type.Sliced, panel.type, que + " es 9-slice");
        }

        private static Image FondoDe(BotonCyberfix boton) => boton.GetComponent<Image>();

        [Test]
        public void Kit_PanelYBotonUsanLasPiezas()
        {
            GameObject raiz = new GameObject("Kit522");
            try
            {
                Canvas canvas = KitUI.CrearCanvas(raiz.transform, "C", 1);
                RectTransform panel = KitUI.CrearPanel(canvas.transform, "P", new Vector2(400f, 300f));
                AssertMarco(panel.GetComponent<Image>(), "El panel");
                Assert.IsNull(panel.Find("Borde"), "Con el marco no hay tiras de borde");
                BotonCyberfix boton = KitUI.CrearBoton(canvas.transform, "PRUEBA", null);
                Assert.AreSame(PiezasUI.Actual.BotonNormal, FondoDe(boton).sprite, "Normal");
                boton.Elegir();
                Assert.AreSame(PiezasUI.Actual.BotonResaltado, FondoDe(boton).sprite, "Elegido");
                Text texto = KitUI.CrearTexto(canvas.transform, "T", RolTexto.Hud);
                Assert.IsNotNull(texto.GetComponent<Outline>(), "El texto lleva contorno oscuro");
                Assert.AreEqual(Mathf.RoundToInt(texto.fontSize), texto.fontSize, "Tamaño entero");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(raiz);
            }
        }

        [UnityTest]
        public IEnumerator TituloYCreacion_ConLogoYFondo()
        {
            FlujoPartida.TituloEnEditor = true;
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            PantallaTitulo titulo = null;
            yield return EsperarHasta(() =>
            {
                titulo = UnityEngine.Object.FindFirstObjectByType<PantallaTitulo>();
                return titulo != null && titulo.Activo && titulo.Abierta;
            }, "el título", 30f);
            CamaraTitulo camara = UnityEngine.Object.FindFirstObjectByType<CamaraTitulo>();
            yield return new WaitForSecondsRealtime(0.5f);
            Transform fondo = titulo.transform.Find("CanvasTitulo/FondoMenu");
            Assert.IsNotNull(fondo, "El título trae el fondo UI14");
            Assert.AreSame(PiezasUI.Actual.FondoMenu, fondo.GetComponent<Image>().sprite);
            Assert.IsNotNull(titulo.transform.Find("CanvasTitulo/FranjaOscura"), "Hay una franja oscura detrás del menú");
            // T-524: la franja usa el degradado (alfa que baja a 0 hacia el centro), no un bloque sólido con corte vertical.
            Image franjaOscura = titulo.transform.Find("CanvasTitulo/FranjaOscura").GetComponent<Image>();
            Assert.AreSame(KitUI.SpriteDegradadoHorizontal(), franjaOscura.sprite, "La franja lleva el sprite del degradado");
            Assert.IsTrue(franjaOscura.enabled, "La franja se dibuja");
            Image logo = titulo.transform.Find("CanvasTitulo/Logo").GetComponent<Image>();
            Assert.IsTrue(logo.gameObject.activeInHierarchy, "El logo UI13 se ve");
            Assert.AreSame(PiezasUI.Actual.Logo, logo.sprite);
            AssertMarco(titulo.transform.Find("CanvasTitulo/Panel").GetComponent<Image>(), "El panel del menú");
            Assert.AreSame(PiezasUI.Actual.BotonResaltado, FondoDe(titulo.Menu.Elegido).sprite, "El elegido lleva el botón resaltado");
            Capturar("titulo", camara.Camara);

            yield return Pulsar(teclado.enterKey);
            PantallaCreacionPandilla creacion = UnityEngine.Object.FindFirstObjectByType<PantallaCreacionPandilla>();
            Assert.IsTrue(creacion.Abierta);
            yield return new WaitForSecondsRealtime(0.3f);
            Capturar("creacion", camara.Camara);
        }

        [UnityTest]
        public IEnumerator CiudadEnPantallas_PausaOpcionesMapaCreditosTiendaAvisoYHud()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return EsperarHasta(() =>
            {
                DirectorPandillas d = UnityEngine.Object.FindFirstObjectByType<DirectorPandillas>();
                return d != null && d.Iniciado && Registro<IJugador>.Actual != null && Registro<IPandillaJugador>.Actual != null;
            }, "la ciudad arrancó", 30f);
            yield return new WaitForSecondsRealtime(0.5f);
            IJugador jugador = Registro<IJugador>.Actual;
            Camera camara = jugador.Camara;
            Registro<IRelojMundo>.Actual?.AvanzarHasta(22f);
            foreach (PandillaAPie grupo in UnityEngine.Object.FindObjectsByType<PandillaAPie>(FindObjectsSortMode.None))
            {
                grupo.gameObject.SetActive(false);
            }
            jugador.Colocar(new Pose(Inicio, Quaternion.LookRotation(Vector3.right)));
            Assert.IsTrue(((PandillaJugador)Registro<IPandillaJugador>.Actual).Reclutar("EL CHATO"));
            Registro<ICartera>.Actual.Abonar(1250, MotivoDinero.Premio);
            Registro<ICalor>.Actual.Sumar(40f, MotivoCalor.Otro);
            yield return new WaitForSecondsRealtime(1f);

            // HUD pulido: panel de zona con fondo oscuro y minimapa de manzanas tenues.
            HudTerritorio zona = UnityEngine.Object.FindFirstObjectByType<HudTerritorio>();
            yield return EsperarHasta(() => zona.TextoZona.text.Length > 0, "la zona", 20f);
            Assert.IsNotNull(zona.TextoZona.transform.parent.Find("FondoOscuro"), "La zona lleva fondo oscuro");
            Minimapa minimapa = UnityEngine.Object.FindFirstObjectByType<Minimapa>();
            minimapa.Actualizar();
            foreach (Transform t in minimapa.Mundo.GetComponentsInChildren<Transform>())
            {
                if (t.name.StartsWith("Manzana_", StringComparison.Ordinal))
                {
                    Color c = t.GetComponent<Image>().color;
                    Assert.Less(Mathf.Max(c.r, Mathf.Max(c.g, c.b)), 0.45f, "Las manzanas del minimapa son tenues: " + t.name);
                }
            }
            Capturar("hud", camara);

            // Aviso de zona conquistada.
            MethodInfo avisar = typeof(HudTerritorio).GetMethod("Avisar", BindingFlags.NonPublic | BindingFlags.Instance);
            avisar.Invoke(zona, new object[] { "EL TIANGUIS ES DE TU PANDILLA", FormatoTerritorio.ColorDe(IdPandilla.Jugador), 30f, MechaCyberpunk.UI.Hud.TipoAviso.Pandilla });
            yield return null;
            yield return null;
            Assert.IsTrue(MechaCyberpunk.UI.Hud.ColaAvisos.Actual.Visible, "T-523: el aviso de zona sale por la cola del centro");
            StringAssert.Contains("TIANGUIS", zona.TextoAviso.text);
            Capturar("aviso-zona", camara);
            avisar.Invoke(zona, new object[] { string.Empty, Color.white, 0f, MechaCyberpunk.UI.Hud.TipoAviso.Neutro });

            // Mapa.
            PantallaMapa mapa = UnityEngine.Object.FindFirstObjectByType<PantallaMapa>();
            yield return Pulsar(teclado.mKey);
            Assert.IsTrue(mapa.Abierto);
            yield return new WaitForSecondsRealtime(0.3f);
            Capturar("mapa", camara);
            CapturarS2m("s2m-c4-mapa.png", camara);
            yield return Pulsar(teclado.escapeKey);
            Assert.IsFalse(mapa.Abierto);

            // Pausa, ranuras y opciones.
            PantallaPausa pausa = UnityEngine.Object.FindFirstObjectByType<PantallaPausa>();
            yield return Pulsar(teclado.escapeKey);
            Assert.IsTrue(pausa.Abierta);
            AssertTurquesa(pausa.transform.Find("CanvasPausa/Panel").GetComponent<Image>(), "El panel de pausa");
            Assert.IsNull(FondoDe(pausa.Continuar).sprite, "T-897: sin el botón naranja");
            Assert.AreEqual(TrazoH10.Turquesa, pausa.Continuar.Texto.color, "El elegido va en turquesa H10 (T-902)");
            yield return new WaitForSecondsRealtime(0.2f);
            Capturar("pausa", camara);
            CapturarS2m("s2m-c4-pausa.png", camara);
            yield return Pulsar(teclado.downArrowKey);
            yield return Pulsar(teclado.enterKey);
            Assert.IsTrue(pausa.Ranuras.Abierta, "Se abren las ranuras");
            yield return new WaitForSecondsRealtime(0.2f);
            Capturar("ranuras", camara);
            yield return Pulsar(teclado.escapeKey);
            for (int i = 0; i < 2; i++)
            {
                yield return Pulsar(teclado.downArrowKey);
            }
            Assert.AreSame(pausa.Opciones, pausa.Menu.Elegido);
            yield return Pulsar(teclado.enterKey);
            Assert.IsTrue(pausa.PantallaOpciones.Abierta, "Se abren las opciones");
            yield return new WaitForSecondsRealtime(0.2f);
            Capturar("opciones", camara);
            yield return Pulsar(teclado.escapeKey);
            yield return Pulsar(teclado.escapeKey);
            Assert.IsFalse(pausa.Abierta);

            // Créditos.
            PantallaCreditos creditos = PantallaCreditos.Abrir(null);
            yield return new WaitForSecondsRealtime(9f);
            AssertMarco(creditos.transform.Find("CanvasCreditos/Panel").GetComponent<Image>(), "El panel de créditos");
            Capturar("creditos", camara);
            yield return Pulsar(teclado.escapeKey);

            // Tiendas: El Mercado y el CIC.
            int numero = 0;
            foreach (MostradorTienda mostrador in UnityEngine.Object.FindObjectsByType<MostradorTienda>(FindObjectsSortMode.None))
            {
                PantallaTienda pantalla = mostrador.GetComponentInChildren<PantallaTienda>(true);
                if (pantalla == null || numero >= 2)
                {
                    continue;
                }
                jugador.Colocar(new Pose(mostrador.transform.position + new Vector3(0f, 0.5f, -1.5f), Quaternion.identity));
                yield return null;
                yield return null;
                mostrador.Usar();
                yield return new WaitForSecondsRealtime(0.3f);
                if (!pantalla.Visible)
                {
                    continue;
                }
                Assert.IsNotNull(pantalla.transform.Find("Contenido/MarcoTienda"), "La tienda lleva el marco");
                Capturar(numero == 0 ? "tienda" : "tienda-cic", camara);
                CapturarS2m(numero == 0 ? "s2m-c4-tienda.png" : "s2m-c4-tienda-cic.png", camara);
                numero++;
                yield return Pulsar(teclado.escapeKey);
                yield return Pulsar(teclado.eKey);
            }
        }
    }
}

using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Opciones;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Mapa;
using MechaCyberpunk.UI.MenuPausa;
using MechaCyberpunk.UI.Opciones;
using MechaCyberpunk.UI.Titulo;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-413 en Ciudad_Prologo con <see cref="FlujoPartida.TituloEnEditor"/> y rutas temporales. RecorridoJugadorNuevo: el título sale con la
    /// cámara moviéndose, el HUD apagado y Esc sin pausa; CONTINUAR desactivado; Enter en NUEVA PARTIDA; → elige B; escribe TONY y LOS NEÓN;
    /// → → → hasta AZUL; Enter en EMPEZAR; la historia está en DESPIERTA con ese perfil, el jugador lleva la máscara y SUBURBIOS es azul en
    /// el mapa. ContinuarConRaton: con un AUTOGUARDADO escrito antes, el ratón abre OPCIONES (Esc vuelve al título) y CONTINUAR carga el
    /// dinero y la posición. Con CAPTURAR_T413=1 guarda docs/capturas/T-413-1 (título) y T-413-2 (creación) en 1080p y 1440p.
    /// </summary>
    public class TituloCreacionPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string VariableCaptura = "CAPTURAR_T413";
        private const string Nombre = "TONY";
        private const string Pandilla = "LOS NEÓN";
        private const int Azul = 3;
        private const int DineroGuardado = 777;
        private static readonly Vector3 Lugar = new Vector3(-20f, 0.5f, 0f);
        private const float Tolerancia = 0.02f;
        private const float ToleranciaPosicion = 0.5f;

        private Keyboard teclado;
        private Mouse raton;
        private string carpeta;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            DescartarEstadoDeMapas(new InputDevice[] { teclado, raton });
            carpeta = Path.Combine(Path.GetTempPath(), "t413_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            ServicioOpciones.FijarRuta(Path.Combine(carpeta, ServicioOpciones.NombreArchivo));
            FlujoPartida.TituloEnEditor = true;
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

        private IEnumerator Escribir(string texto)
        {
            foreach (char letra in texto)
            {
                InputSystem.QueueTextEvent(teclado, letra);
            }
            yield return null;
            yield return null;
        }

        private IEnumerator Clic(BotonCyberfix boton)
        {
            RectTransform rect = (RectTransform)boton.transform;
            Vector2 punto = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            Set(raton.position, punto);
            yield return null;
            yield return null;
            Assert.IsTrue(boton.EstaElegido, "El ratón encima elige " + boton.Etiqueta);
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return null;
            yield return null;
        }

        private static IEnumerator EsperarTitulo(Action<PantallaTitulo> alListo)
        {
            PantallaTitulo titulo = null;
            yield return EsperarHasta(() =>
            {
                titulo = UnityEngine.Object.FindFirstObjectByType<PantallaTitulo>();
                return titulo != null && titulo.Activo && titulo.Abierta;
            }, "el título", 30f);
            alListo(titulo);
        }

        private static IEnumerator EsperarCiudad(DirectorPandillas anterior, Action<DirectorPandillas> alListo)
        {
            DirectorPandillas director = null;
            yield return EsperarHasta(() =>
            {
                director = UnityEngine.Object.FindFirstObjectByType<DirectorPandillas>();
                return director != null && director != anterior && director.Iniciado && Registro<IJugador>.Actual != null
                    && Registro<ICartera>.Actual != null && ServicioGuardado.Actual != null;
            }, "la ciudad arrancó", 30f);
            alListo(director);
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

        private static void AssertColor(Color esperado, Color real, string que)
        {
            Assert.AreEqual(esperado.r, real.r, Tolerancia, que + " (r)");
            Assert.AreEqual(esperado.g, real.g, Tolerancia, que + " (g)");
            Assert.AreEqual(esperado.b, real.b, Tolerancia, que + " (b)");
        }

        private static Color ManzanaDe(PantallaMapa mapa, string zona)
        {
            for (int i = 0; i < mapa.Etiquetas.Length; i++)
            {
                if (mapa.Etiquetas[i].text.Contains(zona))
                {
                    return mapa.Manzanas[i].color;
                }
            }
            Assert.Fail("El mapa no muestra " + zona);
            return Color.clear;
        }

        private static Transform Buscar(Transform raiz, string nombre)
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == nombre)
                {
                    return t;
                }
            }
            return null;
        }

        [UnityTest]
        public IEnumerator RecorridoJugadorNuevo()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            PantallaTitulo titulo = null;
            yield return EsperarTitulo(t => titulo = t);
            Assert.IsTrue(FlujoPartida.EnTitulo, "La ciudad abre en el título");
            CamaraTitulo camara = UnityEngine.Object.FindFirstObjectByType<CamaraTitulo>();
            Assert.IsNotNull(camara, "La ciudad trae la cámara del título");
            float antes = camara.Recorrido;
            Vector3 posicionAntes = camara.transform.position;
            yield return new WaitForSecondsRealtime(1f);
            Assert.Greater(camara.Recorrido - antes, 1f, "La cámara recorre la ciudad (unos 2 m/s)");
            Assert.Greater(Vector3.Distance(posicionAntes, camara.transform.position), 1f, "La cámara se movió");
            Assert.IsTrue(camara.Camara.enabled, "La cámara del título dibuja");

            // HUD, minimapa y demás canvas apagados; la historia espera; Esc no pausa.
            HudTerritorio hud = UnityEngine.Object.FindFirstObjectByType<HudTerritorio>();
            Assert.IsNotNull(hud);
            foreach (Canvas c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (c.isRootCanvas && c.enabled && c.renderMode != RenderMode.WorldSpace)
                {
                    Assert.IsTrue(c.transform.IsChildOf(titulo.transform), "Solo el título dibuja; " + c.name + " está prendido");
                }
            }
            DirectorPandillas director = UnityEngine.Object.FindFirstObjectByType<DirectorPandillas>();
            Assert.IsFalse(director.Iniciado, "La historia espera en el título");
            PantallaPausa pausa = UnityEngine.Object.FindFirstObjectByType<PantallaPausa>();
            yield return Pulsar(teclado.escapeKey);
            Assert.IsFalse(pausa.Abierta, "Esc no abre la pausa en el título");
            Assert.IsTrue(titulo.Abierta);
            Assert.AreEqual(NombreJuego.Nombre, titulo.Nombre.text, "El nombre sale de la constante única");

            // Sin partidas CONTINUAR está desactivado y NUEVA PARTIDA queda elegido.
            Assert.IsFalse(titulo.Continuar.interactable, "Sin partidas CONTINUAR está desactivado");
            Assert.AreSame(titulo.Nueva, titulo.Menu.Elegido);
            Capturar("T-413-1", camara.Camara);

            yield return Pulsar(teclado.enterKey);
            PantallaCreacionPandilla creacion = UnityEngine.Object.FindFirstObjectByType<PantallaCreacionPandilla>();
            Assert.IsTrue(creacion.Abierta, "Enter en NUEVA PARTIDA abre la creación");
            Assert.IsFalse(titulo.Abierta, "El título se esconde");
            Assert.AreSame(creacion.SelectorPersonaje, creacion.Menu.Elegido, "Empieza en PERSONAJE");

            // → elige B.
            yield return Pulsar(teclado.rightArrowKey);
            Assert.AreEqual(1, creacion.SelectorPersonaje.Indice, "→ elige B");
            Assert.IsNotNull(creacion.MunecoVisible);
            Assert.IsTrue(creacion.MunecoVisible.activeSelf);
            StringAssert.Contains("_B", creacion.MunecoVisible.name, "Se ve el muñeco B");

            // TU NOMBRE y TU PANDILLA se escriben (W, A, S, D y Espacio no navegan ahí).
            yield return Pulsar(teclado.downArrowKey);
            Assert.AreSame(creacion.Nombre, creacion.Menu.Elegido);
            yield return Escribir(Nombre);
            Assert.AreEqual(Nombre, creacion.Nombre.Valor);
            yield return Pulsar(teclado.downArrowKey);
            Assert.AreSame(creacion.Pandilla, creacion.Menu.Elegido);
            yield return Escribir(Pandilla);
            Assert.AreEqual(Pandilla, creacion.Pandilla.Valor);
            yield return Pulsar(teclado.sKey);
            Assert.AreSame(creacion.Pandilla, creacion.Menu.Elegido, "S no navega dentro de un campo");
            yield return Escribir("12345678901234567890");
            Assert.AreEqual(Pandilla, creacion.Pandilla.Valor, "Números y símbolos no entran");
            yield return Escribir("ABCDEFGHIJ");
            Assert.AreEqual(ColoresPandilla.LargoMaximo, creacion.Pandilla.Valor.Length, "No pasa de 16");
            for (int i = 0; i < ColoresPandilla.LargoMaximo - Pandilla.Length; i++)
            {
                yield return Pulsar(teclado.backspaceKey);
            }
            Assert.AreEqual(Pandilla, creacion.Pandilla.Valor, "Backspace borra");

            // → → → hasta AZUL.
            yield return Pulsar(teclado.downArrowKey);
            Assert.AreSame(creacion.SelectorColor, creacion.Menu.Elegido);
            for (int i = 0; i < 3; i++)
            {
                yield return Pulsar(teclado.rightArrowKey);
            }
            Assert.AreEqual(Azul, creacion.SelectorColor.Indice, "→ → → llega a AZUL");
            Assert.AreEqual(ColoresPandilla.Nombre(Azul), creacion.SelectorColor.Opcion);
            yield return new WaitForSecondsRealtime(0.3f);
            Capturar("T-413-2", camara.Camara);

            // Enter en EMPEZAR: sin AUTOGUARDADO no pregunta; la escena se recarga y la historia arranca en DESPIERTA.
            yield return Pulsar(teclado.downArrowKey);
            Assert.AreSame(creacion.Empezar, creacion.Menu.Elegido);
            Press(teclado.enterKey);
            yield return null;
            Release(teclado.enterKey);
            yield return EsperarCiudad(director, d => director = d);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsFalse(FlujoPartida.EnTitulo, "EMPEZAR sale del título");
            Assert.AreEqual(PasoHistoria.Despierta, director.Paso, "La historia empieza en DESPIERTA");
            Assert.AreEqual(0, FlujoPartida.PiezasPorEntregar, "El perfil llegó a la escena nueva");
            IPerfilJugador perfil = Registro<IPerfilJugador>.Actual;
            Assert.AreEqual(Nombre, perfil.Nombre);
            Assert.AreEqual(1, perfil.Apariencia.Cara, "Personaje B");
            Assert.AreEqual(Pandilla, perfil.NombrePandilla);
            Assert.AreEqual(Azul, perfil.ColorPandilla);
            Transform mascara = Buscar(Registro<IJugador>.Actual.Raiz, "Mascara");
            Assert.IsNotNull(mascara, "El jugador lleva la máscara de B");
            Assert.IsTrue(mascara.gameObject.activeInHierarchy);
            Transform cabello = Buscar(Registro<IJugador>.Actual.Raiz, "Cabello");
            Assert.IsTrue(cabello == null || !cabello.gameObject.activeSelf, "Sin el cabello de A");
            PantallaTitulo tituloNuevo = UnityEngine.Object.FindFirstObjectByType<PantallaTitulo>();
            Assert.IsFalse(tituloNuevo.Abierta, "Ya no se ve el título");

            // SUBURBIOS sale azul en el mapa.
            PantallaMapa mapa = UnityEngine.Object.FindFirstObjectByType<PantallaMapa>();
            yield return Pulsar(teclado.mKey);
            yield return EsperarHasta(() => mapa.Abierto, "abrir el mapa con M", 5f);
            yield return new WaitForSecondsRealtime(0.3f);
            AssertColor(ColoresPandilla.Color(Azul), ManzanaDe(mapa, "SUBURBIOS"), "SUBURBIOS en el mapa va azul");
        }

        [UnityTest]
        public IEnumerator ContinuarConRaton()
        {
            // Una partida AUTO escrita antes (sin título), con dinero y lugar conocidos.
            FlujoPartida.TituloEnEditor = false;
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            DirectorPandillas director = null;
            yield return EsperarCiudad(null, d => director = d);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsFalse(FlujoPartida.EnTitulo, "Sin TituloEnEditor no hay título");
            foreach (PandillaAPie grupo in UnityEngine.Object.FindObjectsByType<PandillaAPie>(FindObjectsSortMode.None))
            {
                grupo.gameObject.SetActive(false);
            }
            ICartera cartera = Registro<ICartera>.Actual;
            int diferencia = DineroGuardado - cartera.Dinero;
            if (diferencia > 0) cartera.Abonar(diferencia, MotivoDinero.Otro);
            else if (diferencia < 0) cartera.CargarDeuda(-diferencia, MotivoDinero.Otro);
            Assert.AreEqual(DineroGuardado, cartera.Dinero);
            Registro<IJugador>.Actual.Colocar(new Pose(Lugar, Quaternion.identity));
            yield return null;
            yield return null;
            Vector3 guardada = Registro<IJugador>.Actual.Raiz.position;
            Assert.IsTrue(ServicioGuardado.Actual.Guardar(ServicioGuardado.RanuraAuto));

            // Al título.
            FlujoPartida.TituloEnEditor = true;
            FlujoPartida.IrAlTitulo();
            PantallaTitulo titulo = null;
            yield return EsperarTitulo(t => titulo = t);
            Assert.IsTrue(titulo.Continuar.interactable, "Con una partida CONTINUAR se puede elegir");
            Assert.AreEqual(ServicioGuardado.RanuraAuto, titulo.RanuraContinuar);
            StringAssert.Contains("AUTOGUARDADO", titulo.Resumen.text, "Abajo sale el resumen de la más reciente");
            StringAssert.Contains("777", titulo.Resumen.text);

            // OPCIONES con clic; Esc vuelve al título.
            PantallaOpciones opciones = UnityEngine.Object.FindFirstObjectByType<PantallaOpciones>(FindObjectsInactive.Include);
            yield return Clic(titulo.Opciones);
            Assert.IsTrue(opciones.Abierta, "El clic en OPCIONES abre las opciones");
            Assert.IsFalse(titulo.Abierta);
            yield return Pulsar(teclado.escapeKey);
            Assert.IsFalse(opciones.Abierta, "Esc cierra las opciones");
            Assert.IsTrue(titulo.Abierta, "Esc vuelve al título");
            PantallaPausa pausa = UnityEngine.Object.FindFirstObjectByType<PantallaPausa>();
            Assert.IsFalse(pausa.Abierta, "Ese Esc no pausa");

            // CRÉDITOS (T-440) con clic; Esc vuelve al título.
            Assert.IsNotNull(titulo.Creditos, "El título tiene el botón CRÉDITOS");
            yield return Clic(titulo.Creditos);
            MechaCyberpunk.UI.Creditos.PantallaCreditos creditos = UnityEngine.Object.FindFirstObjectByType<MechaCyberpunk.UI.Creditos.PantallaCreditos>();
            Assert.IsNotNull(creditos, "El clic en CRÉDITOS abre la pantalla de créditos");
            Assert.IsFalse(titulo.Abierta);
            yield return Pulsar(teclado.escapeKey);
            Assert.IsTrue(titulo.Abierta, "Esc cierra los créditos y vuelve al título");

            // CONTINUAR con clic carga la partida: dinero y posición.
            yield return Clic(titulo.Continuar);
            yield return EsperarCiudad(null, d => director = d);
            yield return EsperarHasta(() => !FlujoPartida.EnTitulo && Registro<ICartera>.Actual.Dinero == DineroGuardado, "la partida cargada", 10f);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(DineroGuardado, Registro<ICartera>.Actual.Dinero, "El dinero es el de la partida");
            float distancia = Vector3.Distance(guardada, Registro<IJugador>.Actual.Raiz.position);
            Assert.Less(distancia, ToleranciaPosicion, "La posición es la de la partida");
        }
    }
}

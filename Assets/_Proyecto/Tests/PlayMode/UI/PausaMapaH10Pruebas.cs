using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Mapa;
using MechaCyberpunk.UI.MenuPausa;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-902: la pausa y el mapa grande con la guía H10, en Ciudad_Prologo y con la entrada simulada. La pausa lleva cabecera con el dinero real,
    /// panel de opciones y panel de datos (zona, objetivo, hora); sigue deteniendo el tiempo y la confirmación de salir sigue ahí. El mapa
    /// lleva cabecera con el dinero, la vista más grande y el panel lateral con la leyenda. Con CAPTURAR_UI=1 guarda docs/capturas/ui-pausa.png y ui-mapa.png.
    /// </summary>
    public class PausaMapaH10Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string VariableCaptura = "CAPTURAR_UI";
        private const float SegundosCiudad = 30f;
        private const float SegundosEntrada = 0.7f;

        private Keyboard teclado;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Pausa.Fijar(false);
            PuntoMarcadoMapa.Quitar();
            base.TearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            Time.timeScale = 1f;
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            Registros.LimpiarTodos();
            CatalogoDestinosMapa.Limpiar();
            yield return null;
        }

        private IEnumerator Pulsar(UnityEngine.InputSystem.Controls.ButtonControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
            yield return null;
        }

        private IEnumerator CargarCiudad()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            float fin = Time.realtimeSinceStartup + SegundosCiudad;
            while (Time.realtimeSinceStartup < fin)
            {
                DirectorPandillas director = Object.FindFirstObjectByType<DirectorPandillas>();
                if (director != null && director.Iniciado && Registro<IJugador>.Actual != null && Registro<IObjetivos>.Actual != null && Registro<ICartera>.Actual != null)
                {
                    break;
                }
                yield return null;
            }
            Assert.IsNotNull(Registro<IJugador>.Actual, "La ciudad arrancó");
            yield return new WaitForSecondsRealtime(1f);
        }

        private static void Capturar(string nombre)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                return;
            }
            CapturaUI.Capturar("docs/capturas/" + nombre + ".png", Registro<IJugador>.Actual.Camara);
        }

        [UnityTest]
        public IEnumerator Pausa_MuestraDatosRealesYConservaSusOpciones()
        {
            yield return CargarCiudad();
            PantallaPausa pausa = Object.FindFirstObjectByType<PantallaPausa>();
            ICartera cartera = Registro<ICartera>.Actual;
            yield return Pulsar(teclado.escapeKey);
            Assert.IsTrue(pausa.Abierta, "Esc pausa");
            Assert.AreEqual(0f, Time.timeScale, "El tiempo se detiene");
            Assert.AreEqual(Moneda.Texto(cartera.Dinero), pausa.TextoDinero.text, "La cabecera trae el dinero real");
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            if (reloj != null)
            {
                Assert.AreEqual(HorarioLugar.TextoHora(reloj.Hora), pausa.TextoHora.text, "La hora del juego");
            }
            IObjetivos objetivos = Registro<IObjetivos>.Actual;
            Assert.AreEqual(objetivos.HayObjetivo ? objetivos.Actual.Texto : PantallaPausa.TextoSinObjetivo, pausa.TextoObjetivo.text, "El objetivo actual");
            Assert.IsFalse(string.IsNullOrEmpty(pausa.TextoZona.text), "La fila de zona nunca queda vacía");
            Image panel = pausa.Menu.GetComponent<Image>();
            Assert.AreSame(TrazoH10.Relleno, panel.sprite, "El panel de opciones es H10");
            Assert.IsNotNull(panel.transform.Find(VestidoH10.NombreLinea), "con su línea fina");
            Assert.AreEqual(5, pausa.Menu.Botones.Count, "Conserva CONTINUAR, GUARDAR, CARGAR, OPCIONES y SALIR");
            foreach (BotonCyberfix boton in pausa.Menu.Botones)
            {
                Assert.GreaterOrEqual(boton.Texto.fontSize, TrazoH10.TamanoMicro, "Texto legible en " + boton.Etiqueta);
                Assert.IsNotNull(boton.GetComponent<HoverH10>(), "Hover H10 en " + boton.Etiqueta);
            }
            yield return new WaitForSecondsRealtime(SegundosEntrada);
            yield return null;
            OcultadorHudMenus ocultador = Object.FindFirstObjectByType<OcultadorHudMenus>();
            Assert.IsNotNull(ocultador, "El HUD trae su ocultador");
            Assert.Greater(ocultador.Ocultos, 0, "Con la pausa abierta el HUD no se dibuja");
            Capturar("ui-pausa");

            // SALIR AL TÍTULO sigue pidiendo confirmación, y Esc la cancela sin cerrar la pausa.
            for (int i = 0; i < 4; i++)
            {
                yield return Pulsar(teclado.downArrowKey);
            }
            Assert.AreSame(pausa.Salir, pausa.Menu.Elegido);
            yield return Pulsar(teclado.enterKey);
            Assert.IsTrue(pausa.ConfirmacionAbierta, "Pide confirmar");
            yield return Pulsar(teclado.escapeKey);
            Assert.IsFalse(pausa.ConfirmacionAbierta);
            Assert.IsTrue(pausa.Abierta);
            yield return Pulsar(teclado.escapeKey);
            Assert.IsFalse(pausa.Abierta, "Esc continúa");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsTrue(PilaPantallas.Vacia);
        }

        [UnityTest]
        public IEnumerator Mapa_LlevaCabeceraYPanelLateralConDatosReales()
        {
            yield return CargarCiudad();
            PantallaMapa mapa = Object.FindFirstObjectByType<PantallaMapa>();
            ICartera cartera = Registro<ICartera>.Actual;
            yield return Pulsar(teclado.mKey);
            Assert.IsTrue(mapa.Abierto, "M abre el mapa");
            Assert.AreEqual(PantallaMapa.NombreCiudad, mapa.Titulo.text);
            Transform panel = mapa.transform.Find("CanvasMapa/Panel");
            Assert.AreEqual(Moneda.Texto(cartera.Dinero), panel.Find("Dinero").GetComponent<Text>().text, "La cabecera trae el dinero real");
            Assert.Greater(mapa.Vista.lossyScale.x, 1.3f * mapa.Vista.parent.lossyScale.x, "El mapa es el protagonista: la vista crece");
            Assert.AreEqual(PantallaMapa.FilasLeyenda, mapa.TextosLeyenda.Count);
            foreach (Text texto in mapa.TextosLeyenda)
            {
                Assert.GreaterOrEqual(texto.fontSize, TrazoH10.TamanoMicro, "Leyenda legible: " + texto.text);
            }
            Assert.IsNotNull(panel.Find("PanelLateral"), "Panel lateral");
            Assert.AreEqual(PantallaMapa.FilasNegocios, mapa.TextosNegocios.Count);
            yield return new WaitForSecondsRealtime(SegundosEntrada);
            Capturar("ui-mapa");
            yield return Pulsar(teclado.escapeKey);
            Assert.IsFalse(mapa.Abierto);
        }
    }
}

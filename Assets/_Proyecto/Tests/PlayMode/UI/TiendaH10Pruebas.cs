using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Tiendas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-903: la tienda de El Mercado con la guía H10, en Ciudad_Prologo y con la entrada simulada. Cabecera con el dinero, pestañas COMPRAR y VENDER,
    /// lista con recuadro por fila (hover y clic) y detalle con datos reales (precio, lo que llevas, cuánto te quedaría). El precio va en amarillo
    /// y en coral si no alcanza. Con CAPTURAR_UI=1 guarda docs/capturas/ui-tienda.png.
    /// </summary>
    public class TiendaH10Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string VariableCaptura = "CAPTURAR_UI";
        private const float SegundosCiudad = 30f;
        private const float SegundosEntrada = 0.7f;
        private const int IndicePrecio = 0;
        private const int IndiceQuedaria = 3;

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
            base.TearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            Registros.LimpiarTodos();
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

        [UnityTest]
        public IEnumerator Tienda_MuestraDetalleRealYPrecioEnAmarilloOCoral()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            float fin = Time.realtimeSinceStartup + SegundosCiudad;
            while (Time.realtimeSinceStartup < fin)
            {
                DirectorPandillas director = Object.FindFirstObjectByType<DirectorPandillas>();
                if (director != null && director.Iniciado && Registro<IJugador>.Actual != null && Registro<ICartera>.Actual != null)
                {
                    break;
                }
                yield return null;
            }
            Assert.IsNotNull(Registro<IJugador>.Actual, "La ciudad arrancó");
            yield return new WaitForSecondsRealtime(1f);

            MostradorTienda mostrador = null;
            PantallaTienda pantalla = null;
            foreach (MostradorTienda m in Object.FindObjectsByType<MostradorTienda>(FindObjectsSortMode.None))
            {
                PantallaTienda p = m.GetComponentInChildren<PantallaTienda>(true);
                if (p != null && (mostrador == null || m.name.Contains("Mercado")))
                {
                    mostrador = m;
                    pantalla = p;
                }
            }
            Assert.IsNotNull(mostrador, "La ciudad trae una tienda");
            IJugador jugador = Registro<IJugador>.Actual;
            ICartera cartera = Registro<ICartera>.Actual;
            jugador.Colocar(new Pose(mostrador.transform.position + new Vector3(0f, 0.5f, -1.5f), Quaternion.identity));
            yield return null;
            yield return null;
            mostrador.Usar();
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsTrue(pantalla.Visible, "El mostrador abre la tienda");

            // Cabecera y lista con datos reales.
            StringAssert.Contains(FormatoDinero.Texto(cartera.Dinero), pantalla.Dinero, "La cabecera trae el dinero real");
            Assert.Greater(pantalla.CantidadFilas, 0, "Hay filas");
            Assert.AreEqual(PestanaTienda.Comprar, pantalla.Pestana);
            Assert.AreEqual(0, pantalla.IndiceElegido);
            StringAssert.Contains(pantalla.DetalleNombre, pantalla.TextoFila(0).ToUpperInvariant(), "El detalle es de la fila elegida");
            Assert.IsNotEmpty(pantalla.DetalleDato(IndicePrecio), "El detalle trae el precio");
            Assert.IsNotEmpty(pantalla.DetalleDato(IndiceQuedaria - 1) + pantalla.DetalleDato(IndiceQuedaria), "y cuánto te quedaría");
            Assert.AreNotEqual(TrazoH10.TextoTenue, pantalla.ColorDetallePrecio);
            Assert.IsTrue(pantalla.ColorDetallePrecio == TrazoH10.Amarillo || pantalla.ColorDetallePrecio == TrazoH10.Coral, "El precio va en amarillo o en coral");
            Assert.AreSame(TrazoH10.Relleno, pantalla.PanelLista.GetComponent<Image>().sprite, "El panel de la lista es H10");
            Assert.IsNotNull(pantalla.PanelDetalle, "Hay panel de detalle");
            yield return new WaitForSecondsRealtime(SegundosEntrada);
            if (Environment.GetEnvironmentVariable(VariableCaptura) == "1")
            {
                CapturaUI.Capturar("docs/capturas/ui-tienda.png", jugador.Camara);
            }

            // ↓ elige la siguiente y el detalle cambia con ella.
            string nombreAntes = pantalla.DetalleNombre;
            yield return Pulsar(teclado.downArrowKey);
            Assert.AreEqual(1, pantalla.IndiceElegido);
            Assert.AreNotEqual(nombreAntes, pantalla.DetalleNombre, "El detalle sigue a la elección");

            // Un clic en una fila la elige.
            EventTrigger[] filas = pantalla.PanelLista.GetComponentsInChildren<EventTrigger>(true);
            Assert.GreaterOrEqual(filas.Length, 3);
            ExecuteEvents.Execute(filas[2].gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            yield return null;
            Assert.AreEqual(2, pantalla.IndiceElegido, "El clic elige la fila");

            // Sin dinero no alcanza: el precio pasa a coral y comprar avisa.
            int dinero = cartera.Dinero;
            if (dinero > 0)
            {
                cartera.IntentarPagar(dinero, MotivoDinero.Compra);
            }
            yield return Pulsar(teclado.leftArrowKey);
            yield return Pulsar(teclado.upArrowKey);
            Assert.AreEqual(TrazoH10.Coral, pantalla.ColorDetallePrecio, "Si no alcanza, el precio va en coral");
            yield return Pulsar(teclado.enterKey);
            StringAssert.Contains("NO TE ALCANZA", pantalla.Aviso);

            yield return Pulsar(teclado.escapeKey);
        }
    }
}

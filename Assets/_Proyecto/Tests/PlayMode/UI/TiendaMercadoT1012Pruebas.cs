using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.UI.Tiendas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-1012: El Mercado como el mockup M32 en Ciudad_Prologo: lista angosta con el dinero grande, y a la derecha el nombre del arma, su holograma 3D,
    /// las barras DAÑO, CARGADOR, CADENCIA y ALCANCE y el botón COMPRAR. Con CAPTURAR_T991=1 deja docs/capturas/t1012/.
    /// </summary>
    public class TiendaMercadoT1012Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const int Dinero = 12450;
        private Keyboard teclado;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
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

        private static IEnumerator EsperarHasta(Func<bool> condicion, string que, float segundos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            while (!condicion())
            {
                Assert.Less(Time.realtimeSinceStartup, fin, "Tiempo agotado esperando: " + que);
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

        private static void Capturar(string nombre)
        {
            if (Environment.GetEnvironmentVariable("CAPTURAR_T991") != "1")
            {
                return;
            }
            Directory.CreateDirectory(Path.Combine("docs", "capturas", "t1012"));
            CapturaUI.Capturar(Path.Combine("docs", "capturas", "t1012", nombre), Camera.main);
        }

        [UnityTest]
        public IEnumerator ElMercado_ListaHologramaBarrasYComprar_ComoM32()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return EsperarHasta(() => Registro<IJugador>.Actual != null && Registro<ArsenalJugador>.Actual != null && Registro<ICartera>.Actual != null,
                "jugador, arsenal y cartera", 30f);
            yield return new WaitForSecondsRealtime(1f);
            ICartera cartera = Registro<ICartera>.Actual;
            cartera.Abonar(Dinero - cartera.Dinero, MotivoDinero.Otro);
            MostradorTienda mostrador = null;
            foreach (MostradorTienda m in Object.FindObjectsByType<MostradorTienda>(FindObjectsSortMode.None))
            {
                mostrador = m.name.Contains("MercadoNegro") ? m : mostrador;
            }
            Assert.IsNotNull(mostrador, "Está el mostrador de El Mercado");
            PantallaTienda pantalla = Object.FindFirstObjectByType<PantallaTienda>(FindObjectsInactive.Include);
            // T-1002: el juego empieza a las 10:00 y El Mercado abre de noche.
            Registro<IRelojMundo>.Actual?.AvanzarHasta(22f);
            yield return null;
            // La pantalla se abre con lo que vende el mostrador (abrirlo caminando ya lo prueba MercadoArmasT720Pruebas).
            pantalla.Mostrar(mostrador.Tienda, mostrador.EnVenta, () => pantalla.Ocultar(), mostrador.Compra, mostrador.AceptaFavorDescuento);
            yield return EsperarHasta(() => pantalla.Visible, "abrir El Mercado", 5f);
            yield return new WaitForSecondsRealtime(0.6f);

            Assert.IsTrue(pantalla.ModoMercado, "El Mercado vende armas: va con la vista M32");
            VistaMercadoArmas vista = pantalla.Mercado;
            Assert.IsNotNull(vista);
            Assert.IsTrue(vista.Activa);
            Assert.IsFalse(pantalla.PanelDetalle.gameObject.activeSelf, "El detalle de siempre se apaga");
            Assert.AreEqual(VistaMercadoArmas.AnchoLista, pantalla.PanelLista.rect.width, 1f, "La lista va angosta a la izquierda");
            StringAssert.Contains("12,450", vista.DineroMostrado.Replace(".", ","), "El dinero grande arriba de la lista");

            // Baja hasta la primera fila que sea un arma (no balas) y revisa el holograma y las barras.
            int fila = -1;
            for (int i = 0; i < pantalla.CantidadFilas && fila < 0; i++)
            {
                string texto = pantalla.TextoFila(i).ToUpperInvariant();
                Debug.Log("[T1012] fila " + i + ": " + texto);
                if (!texto.Contains("BALAS") && (texto.Contains("ESCOPETA") || texto.Contains("FUSIL") || texto.Contains("PISTOLA") || texto.Contains("FRANCOTIRADOR"))) // T-1314
                {
                    fila = i;
                }
            }
            Assert.GreaterOrEqual(fila, 0, "Hay un arma a la venta");
            for (int vueltas = 0; pantalla.IndiceElegido != fila && vueltas < 40; vueltas++)
            {
                yield return Pulsar(teclado.downArrowKey);
            }
            yield return new WaitForSecondsRealtime(1.2f);
            Capturar("mercado-m32.png");
            Debug.Log("[T1012] nombre " + vista.NombreMostrado + " arma " + vista.ArmaMostrada);
            Assert.IsTrue(vista.NombreMostrado.Length > 0, "El nombre del arma en grande");
            Assert.IsTrue(Enum.IsDefined(typeof(ArmaAPie), vista.ArmaMostrada), "Hay holograma del arma");
            Assert.IsTrue(vista.Holograma.ConModelo, "El holograma es el modelo 3D");
            for (int i = 0; i < ReglaVistaMercado.Barras; i++)
            {
                Assert.AreNotEqual("—", vista.ValorBarra(i), "La barra " + ReglaVistaMercado.Etiquetas[i] + " tiene número");
                Assert.Greater(vista.LlenadoBarra(i), 0f, "La barra " + ReglaVistaMercado.Etiquetas[i] + " se llena");
            }
            Assert.AreEqual("COMPRAR", vista.BotonMostrado, "Con dinero el botón dice COMPRAR");

            int antes = cartera.Dinero;
            yield return Pulsar(teclado.enterKey);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.Less(cartera.Dinero, antes, "Enter compra el arma elegida");

            // T-1013: las armas nuevas también salen con su holograma (el rifle de plasma).
            int plasma = -1;
            for (int i = 0; i < pantalla.CantidadFilas && plasma < 0; i++)
            {
                plasma = pantalla.TextoFila(i).ToUpperInvariant().Contains("RIFLE DE PLASMA") ? i : -1;
            }
            Assert.GreaterOrEqual(plasma, 0, "El Mercado vende el rifle de plasma");
            for (int vueltas = 0; pantalla.IndiceElegido != plasma && vueltas < 60; vueltas++)
            {
                yield return Pulsar(teclado.downArrowKey);
            }
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.AreEqual(ArmaAPie.RiflePlasma, vista.ArmaMostrada, "El holograma es el rifle de plasma");
            Assert.IsTrue(vista.Holograma.ConModelo, "Con su modelo 3D");
            Capturar("mercado-rifle-plasma.png");

            yield return Pulsar(teclado.escapeKey);
            yield return EsperarHasta(() => !pantalla.Visible, "cerrar con Esc", 5f);
            Assert.IsFalse(vista.Activa, "Al cerrar se apaga la vista");
        }
    }
}

using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.UI.Tiendas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-1314: la tienda de armas de El Mercado con el cursor libre, solo armas base y balas (sin "Pistola 1", mejoras ni niveles) y clic para elegir y comprar.
    /// Deja docs/capturas/t1314/tienda-armas.png.
    /// </summary>
    public class TiendaArmasT1314Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
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
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
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

        private static void Clic(Transform objetivo)
        {
            ExecuteEvents.Execute(objetivo.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
        }

        [UnityTest]
        public IEnumerator ElMercado_CursorLibre_SoloArmasYBalas_ClicEligeYCompra()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return EsperarHasta(() => Registro<IJugador>.Actual != null && Registro<ArsenalJugador>.Actual != null && Registro<ICartera>.Actual != null,
                "jugador, arsenal y cartera", 30f);
            yield return new WaitForSecondsRealtime(1f);
            ICartera cartera = Registro<ICartera>.Actual;
            cartera.Abonar(20000 - cartera.Dinero, MotivoDinero.Otro);
            MostradorTienda mostrador = null;
            foreach (MostradorTienda m in Object.FindObjectsByType<MostradorTienda>(FindObjectsSortMode.None))
            {
                mostrador = m.name.Contains("MercadoNegro") ? m : mostrador;
            }
            Assert.IsNotNull(mostrador, "Está el mostrador de El Mercado");
            PantallaTienda pantalla = Object.FindFirstObjectByType<PantallaTienda>(FindObjectsInactive.Include);
            // Ya tiene el rifle de plasma: la tienda le ofrece sus Celdas de plasma y no el arma ni sus mejoras.
            Registro<ServicioInventario>.Actual.Agregar(UnityEditor.AssetDatabase.LoadAssetAtPath<DatosArma>("Assets/_Proyecto/Personaje/Datos/Armas/DatosArma_rifleplasma_0.asset"), 1);
            Registro<IRelojMundo>.Actual?.AvanzarHasta(22f);
            yield return null;

            // El juego a pie tiene el cursor bloqueado.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            CursorLockMode bloqueoPrevio = Cursor.lockState; // en el editor sin foco el bloqueo puede no quedar fijo: se compara con lo leído
            pantalla.Mostrar(mostrador.Tienda, mostrador.EnVenta, () => pantalla.Ocultar(), mostrador.Compra, mostrador.AceptaFavorDescuento);
            yield return EsperarHasta(() => pantalla.Visible, "abrir El Mercado", 5f);
            yield return new WaitForSecondsRealtime(1.2f);

            Assert.IsTrue(Cursor.visible, "El cursor se ve en la tienda");
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState, "El cursor está libre en la tienda");

            // Solo armas base y balas: ningún nivel ni mejora en los nombres.
            Assert.Greater(pantalla.CantidadFilas, 0);
            bool hayPlasma = false;
            for (int i = 0; i < pantalla.CantidadFilas; i++)
            {
                string fila = pantalla.TextoFila(i).ToUpperInvariant();
                string nombre = fila.TrimStart('>', ' ').Split(new[] { "    " }, StringSplitOptions.None)[0].Trim();
                Debug.Log("[T1314] fila " + i + ": " + nombre);
                Assert.IsFalse(Regex.IsMatch(nombre, @"\b(I|II|III|[123])$"), "Sin nivel en el nombre: " + nombre);
                StringAssert.DoesNotContain("MEJORA", fila);
                StringAssert.DoesNotContain("RIFLE DE PLASMA     ", fila, "El arma que ya tienes no se ofrece");
                hayPlasma |= nombre == "CELDAS DE PLASMA";
            }
            Assert.IsTrue(hayPlasma, "Celdas de plasma, las balas del rifle de plasma");

            // Clic en una fila de arma: la elige; clic otra vez sobre la elegida: compra.
            int arma = -1;
            for (int i = 0; i < pantalla.CantidadFilas && arma < 0; i++)
            {
                string fila = pantalla.TextoFila(i).ToUpperInvariant();
                arma = fila.Contains("ESCOPETA") && !fila.Contains("BALAS") ? i : -1;
            }
            Assert.GreaterOrEqual(arma, 0, "Hay una escopeta a la venta");
            Clic(Buscar(pantalla.PanelLista, "Fila_" + arma));
            yield return null;
            Assert.AreEqual(arma, pantalla.IndiceElegido, "El clic elige la fila");
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            Assert.IsFalse(arsenal.Tiene(ArmaAPie.Escopeta));
            int antes = cartera.Dinero;
            Clic(Buscar(pantalla.PanelLista, "Fila_" + arma));
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.Less(cartera.Dinero, antes, "Un clic sobre la fila elegida compra");
            Assert.IsTrue(arsenal.Tiene(ArmaAPie.Escopeta));
            for (int i = 0; i < pantalla.CantidadFilas; i++)
            {
                StringAssert.DoesNotContain("ESCOPETA     ", pantalla.TextoFila(i).ToUpperInvariant().Replace("BALAS DE ESCOPETA", string.Empty), "El arma comprada ya no se ofrece ni sube de nivel");
            }
            yield return new WaitForSecondsRealtime(0.6f);

            Directory.CreateDirectory(Path.Combine("docs", "capturas", "t1314"));
            CapturaUI.Capturar(Path.Combine("docs", "capturas", "t1314", "tienda-armas.png"), Camera.main);

            // Al cerrar vuelve el estado de antes.
            yield return null;
            Press(teclado.escapeKey);
            yield return null;
            Release(teclado.escapeKey);
            yield return EsperarHasta(() => !pantalla.Visible, "cerrar con Esc", 5f);
            Assert.IsFalse(Cursor.visible, "Al cerrar el cursor vuelve a ocultarse");
            Assert.AreEqual(bloqueoPrevio, Cursor.lockState, "Al cerrar el cursor vuelve a como estaba (bloqueado en el juego)");
        }
    }
}

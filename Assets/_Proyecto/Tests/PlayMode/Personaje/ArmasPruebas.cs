using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Pruebas;
using MechaCyberpunk.UI.Tiendas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-403: comprar la escopeta en el mercado negro y usarla. Todo con entrada simulada (E, flechas, Enter, Esc, 3, clic).</summary>
    public class ArmasPruebas : BaseJugadorAPie
    {
        private const string RutaMostrador = "Assets/_Proyecto/UI/Prefabs/MostradorMercadoNegro.prefab";
        private const int DineroInicial = 400;
        private const int PrecioEscopeta = 350;
        private const float DistanciaMuneco = 4f;
        private const float DanioMinimo = 50f;
        private const float DistanciaMostrador = 1.6f;
        private const int MaxFilas = 40;

        private GameObject mostradorObj;
        private GameObject objetivo;

        public override void TearDown()
        {
            if (mostradorObj != null)
            {
                Object.Destroy(mostradorObj);
            }
            if (objetivo != null)
            {
                Object.Destroy(objetivo);
            }
            base.TearDown();
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ComprarYUsarEscopeta()
        {
            yield return Calentar();
            ICartera cartera = Registro<ICartera>.Actual;
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            Assert.IsNotNull(cartera, "El jugador trae cartera");
            Assert.IsNotNull(inventario, "El jugador trae inventario");
            int diferencia = DineroInicial - cartera.Dinero;
            if (diferencia > 0)
            {
                cartera.Abonar(diferencia, MotivoDinero.Otro);
            }
            else if (diferencia < 0)
            {
                cartera.CargarDeuda(-diferencia, MotivoDinero.Otro);
            }
            Assert.AreEqual(DineroInicial, cartera.Dinero, "Empieza con Ƀ400");

            ArmasAPie armas = jugador.GetComponent<ArmasAPie>();
            yield return Tecla(teclado.digit3Key);
            Assert.AreEqual(ArmaAPie.Punos, armas.ArmaActiva, "Sin escopeta, 3 no cambia de arma");

            // Mostrador del mercado negro delante del jugador.
            mostradorObj = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaMostrador),
                new Vector3(0f, 0f, DistanciaMostrador), Quaternion.identity);
            yield return null;
            MostradorTienda mostrador = mostradorObj.GetComponent<MostradorTienda>();
            PantallaTienda pantalla = mostradorObj.GetComponentInChildren<PantallaTienda>(true);
            yield return Esperar(0.3f);

            yield return Tecla(teclado.eKey);
            Assert.IsTrue(pantalla.Visible, "E abre el mostrador");

            int fila = -1;
            for (int i = 0; i < pantalla.CantidadFilas; i++)
            {
                if (pantalla.TextoFila(i).Contains("ESCOPETA"))
                {
                    fila = i;
                }
            }
            Assert.GreaterOrEqual(fila, 0, "La escopeta está a la venta");
            Assert.LessOrEqual(fila, MaxFilas);
            for (int i = 0; i < fila; i++)
            {
                yield return Tecla(teclado.downArrowKey);
            }
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(DineroInicial - PrecioEscopeta, cartera.Dinero, "Cobra Ƀ350");

            // De la escopeta ahora solo se ofrece la mejora siguiente (nivel I).
            bool nivel0 = false;
            bool nivel1 = false;
            for (int i = 0; i < pantalla.CantidadFilas; i++)
            {
                string texto = pantalla.TextoFila(i);
                nivel1 |= texto.Contains("ESCOPETA I ");
                nivel0 |= texto.Contains("ESCOPETA ") && !texto.Contains("ESCOPETA I");
            }
            Assert.IsTrue(nivel1, "Ofrece la mejora ESCOPETA I");
            Assert.IsFalse(nivel0, "Ya no ofrece la escopeta base");

            yield return Tecla(teclado.escapeKey);
            yield return Esperar(0.2f);
            Assert.IsFalse(pantalla.Visible, "Esc cierra la tienda");
            Object.Destroy(mostradorObj);
            mostradorObj = null;
            yield return null;

            yield return Tecla(teclado.digit3Key);
            Assert.AreEqual(ArmaAPie.Escopeta, armas.ArmaActiva, "3 pone la escopeta");

            objetivo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            objetivo.transform.position = new Vector3(0f, 4f, DistanciaMuneco);
            objetivo.transform.localScale = new Vector3(1f, 8f, 1f);
            MunecoAPie muneco = objetivo.AddComponent<MunecoAPie>();
            Physics.SyncTransforms();
            float antes = muneco.Actual;
            yield return null;

            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(0.2f);
            Assert.GreaterOrEqual(antes - muneco.Actual, DanioMinimo, "La escopeta a 4 m quita al menos 50 de vida");
        }
    }
}

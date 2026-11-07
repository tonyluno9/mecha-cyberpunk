using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.UI.Datos;
using MechaCyberpunk.UI.Tiendas;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-685: las máquinas expendedoras se colocan en la escena desde sus datos, la E las abre, se compra con Enter y se sale con Esc.
    /// Capturas con CAPTURAR_S1=1 en docs/capturas/s1t-T-685-*.png.
    /// </summary>
    public class ExpendedoraT685Pruebas : BaseJugadorAPie
    {
        private IEnumerator Pulsar(UnityEngine.InputSystem.Controls.KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Expendedora_EComprar_EntregaAlInventarioYCobra()
        {
            yield return Calentar();
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            Assert.IsNotNull(inventario, "Hay inventario");
            DatosExpendedoras datos = ScriptableObject.CreateInstance<DatosExpendedoras>();
            var original = Resources.Load<DatosExpendedoras>(ColocadorExpendedoras.NombreDatos);
            Assert.IsNotNull(original, "Los datos están en Resources");
            var so = new UnityEditor.SerializedObject(datos);
            so.FindProperty("modelo").objectReferenceValue = original.Modelo;
            so.FindProperty("pantalla").objectReferenceValue = original.Pantalla;
            so.FindProperty("tienda").objectReferenceValue = original.Tienda;
            so.FindProperty("catalogo").objectReferenceValue = original.Catalogo;
            var pos = so.FindProperty("posiciones");
            pos.arraySize = 1;
            pos.GetArrayElementAtIndex(0).FindPropertyRelative("escena").stringValue = SceneManager.GetActiveScene().name;
            pos.GetArrayElementAtIndex(0).FindPropertyRelative("posicion").vector3Value = new Vector3(0f, 0f, 1.7f);
            pos.GetArrayElementAtIndex(0).FindPropertyRelative("giro").floatValue = 180f;
            so.ApplyModifiedPropertiesWithoutUndo();

            Transform raiz = ColocadorExpendedoras.Colocar(SceneManager.GetActiveScene(), datos);
            Assert.IsNotNull(raiz, "El colocador puso las máquinas en la escena");
            Assert.AreEqual(1, raiz.GetComponentsInChildren<Expendedora>().Length);
            Assert.IsNull(ColocadorExpendedoras.Colocar(SceneManager.GetActiveScene(), datos), "No las pone dos veces");
            Expendedora maquina = raiz.GetComponentInChildren<Expendedora>();
            yield return Esperar(0.3f);
            CapturaS1.FotoMira("s1t-T-685-expendedora", jugador.transform, maquina.transform.position + Vector3.up * 1f, new Vector3(0.6f, 0.2f, 1f), 4f, 50f, 800);

            ICartera cartera = Registro<ICartera>.Actual;
            int dineroAntes = cartera.Dinero;
            // El jugador apunta a la máquina: es el candidato de la E y el aviso dice "Expendedora".
            var inter = jugador.GetComponent<MechaCyberpunk.Personaje.Interaccion.InteraccionAPie>();
            yield return null;
            Assert.AreSame(maquina, inter.InteractuableCandidato, "La E apunta a la expendedora");
            Assert.AreEqual("Expendedora", inter.InteractuableCandidato.Texto);
            for (int intento = 0; intento < 5 && !maquina.PantallaAbierta; intento++)
            {
                yield return Pulsar(teclado.eKey);
            }
            yield return Esperar(0.2f);
            Assert.IsTrue(maquina.PantallaAbierta, "El menú de la expendedora se abre");
            Assert.IsFalse(controlador.ControlActivo, "Congela el control");
            PantallaTienda pantalla = raiz.GetComponentInChildren<PantallaTienda>(true);
            Assert.IsTrue(pantalla.Visible);
            Assert.AreEqual(5, pantalla.CantidadFilas, "Cinco objetos a la venta");
            yield return Esperar(0.2f);
            CapturaS1.FotoCanvas("s1t-T-685-menu", pantalla.Canvas);
            yield return Pulsar(teclado.enterKey);
            DatosConsumible primero = (DatosConsumible)maquina.EnVenta[0];
            Assert.AreEqual(1, inventario.Cantidad(primero), "Compró el primero");
            Assert.AreEqual(dineroAntes - primero.Oferta.Precio, cartera.Dinero);
            yield return Pulsar(teclado.escapeKey);
            yield return Esperar(0.3f);
            Assert.IsFalse(maquina.PantallaAbierta);
            Assert.IsTrue(controlador.ControlActivo, "Devuelve el control");
            CapturaS1.FotoCanvas("s1t-T-685-hud", jugador.GetComponentInChildren<MechaCyberpunk.UI.Hud.HudAPie>(true).GetComponent<Canvas>());
            Object.Destroy(raiz.gameObject);
            Object.Destroy(datos);
        }
    }
}

using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.UI.Armas;
using MechaCyberpunk.UI.Tiendas;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-747: la tienda del CIC de Tino en la ciudad real. Con la entrada simulada: E junto al mostrador la abre, Enter compra (cobra) y Esc la cierra
    /// y devuelve el control. RecorridoDemoPruebas había reportado que no abría, no cobraba y Esc no cerraba.
    /// </summary>
    public class TiendaCicT747Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private Keyboard teclado;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
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

        private IEnumerator Pulsar(ButtonControl tecla, int cuadros = 1)
        {
            Press(tecla);
            for (int i = 0; i < cuadros; i++)
            {
                yield return null;
            }
            Release(tecla);
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

        [UnityTest]
        public IEnumerator Cic_AbreConE_CobraConEnter_YEscLaCierra()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return EsperarHasta(() => Registro<IJugador>.Actual != null && Registro<ICartera>.Actual != null, "jugador y cartera", 30f);
            yield return new WaitForSecondsRealtime(1f);
            GameObject expendedoras = GameObject.Find(ColocadorExpendedoras.NombreRaiz);
            if (expendedoras != null)
            {
                Object.DestroyImmediate(expendedoras);
            }
            IJugador jugador = Registro<IJugador>.Actual;
            ICartera cartera = Registro<ICartera>.Actual;
            ControladorAPie control = jugador.Raiz.GetComponentInChildren<ControladorAPie>();
            cartera.Abonar(5000 - cartera.Dinero, MotivoDinero.Otro);
            // Tino mejora armas de fuego en su banco: se entrega la pistola que la historia da más adelante.
            Registro<ServicioInventario>.Actual.Agregar(AssetDatabase.LoadAssetAtPath<DatosArma>("Assets/_Proyecto/Personaje/Datos/Armas/DatosArma_pistola_0.asset"), 1);

            BancoArmas banco = Object.FindFirstObjectByType<BancoArmas>();
            Assert.IsNotNull(banco, "El CIC de Tino tiene su banco de trabajo");
            // Frente al banco, a 1.2 m del punto de uso, mirándolo.
            Vector3 punto = banco.Posicion;
            Vector3 desde = punto + Vector3.back * 1.0f;
            Vector3 haciaBanco = punto - desde;
            haciaBanco.y = 0f;
            desde.y = punto.y + 0.1f;
            jugador.Colocar(new Pose(desde, Quaternion.LookRotation(haciaBanco.normalized, Vector3.up)));
            yield return new WaitForSecondsRealtime(0.6f);
            Debug.Log("[T-747] jugador en " + jugador.Raiz.position + " banco " + punto + " PuedeUsar " + banco.PuedeUsar + " control " + control.ControlActivo);

            // Un jugador vuelve a pulsar E si no abrió a la primera.
            for (int intento = 0; intento < 5 && !banco.PantallaAbierta; intento++)
            {
                yield return Pulsar(teclado.eKey, 3);
                yield return new WaitForSecondsRealtime(0.3f);
            }
            yield return EsperarHasta(() => banco.PantallaAbierta && banco.Pantalla != null && banco.Pantalla.Visible, "abrir el banco del CIC con E", 5f);
            Assert.IsFalse(control.ControlActivo, "Con el banco abierto el jugador no se mueve");

            int antes = cartera.Dinero;
            yield return new WaitForSecondsRealtime(0.8f);
            CapturaUI.Capturar("docs/capturas/s1e-T-747-tienda.png", jugador.Camara);
            yield return Pulsar(teclado.enterKey);
            Assert.Less(cartera.Dinero, antes, "Enter compra la mejora elegida y cobra (aviso: " + banco.Pantalla.Aviso + ")");

            yield return Pulsar(teclado.escapeKey);
            yield return EsperarHasta(() => !banco.Pantalla.Visible && !banco.PantallaAbierta, "cerrar el banco con Esc", 3f);
            yield return null;
            yield return null;
            Assert.IsTrue(control.ControlActivo, "Esc devuelve el control al jugador");
        }
    }
}

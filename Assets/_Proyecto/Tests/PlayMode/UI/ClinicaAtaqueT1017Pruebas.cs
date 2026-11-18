using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Habilidades;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Clinica;
using MechaCyberpunk.UI.Hud;
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
    /// T-1017: en la clínica del Doc, Tab abre "PRÓTESIS DE ATAQUE" (brazo láser y hombrera de misiles) en la pantalla de tienda; Enter compra, Esc vuelve
    /// a la clínica y la prótesis comprada aparece en la barra M31 con su tecla. Con CAPTURAR_T991=1 deja docs/capturas/t1017/.
    /// </summary>
    public class ClinicaAtaqueT1017Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const int Dinero = 9000;
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
            yield return null;
            Release(tecla);
            yield return null;
            yield return null;
        }

        private static void Capturar(string nombre)
        {
            if (Environment.GetEnvironmentVariable("CAPTURAR_T991") != "1")
            {
                return;
            }
            Directory.CreateDirectory(Path.Combine("docs", "capturas", "t1017"));
            CapturaUI.Capturar(Path.Combine("docs", "capturas", "t1017", nombre), Camera.main);
        }

        [UnityTest]
        public IEnumerator Clinica_TabAbreProtesisDeAtaque_ComprarYVerlaEnLaBarra()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            DirectorPandillas historia = null;
            yield return EsperarHasta(() =>
            {
                historia = Object.FindFirstObjectByType<DirectorPandillas>();
                return historia != null && historia.Iniciado && Registro<IJugador>.Actual != null && Registro<ICartera>.Actual != null
                    && Registro<ServicioInventario>.Actual != null;
            }, "la escena arrancó", 30f);
            for (int i = 0; i < 12 && historia.Paso != PasoHistoria.Libre; i++)
            {
                yield return Pulsar(teclado.f6Key);
            }
            yield return new WaitForSecondsRealtime(0.5f);
            ICartera cartera = Registro<ICartera>.Actual;
            cartera.Abonar(Dinero - cartera.Dinero, MotivoDinero.Otro);

            MostradorClinica mostrador = Object.FindFirstObjectByType<MostradorClinica>();
            PantallaClinica clinica = Object.FindFirstObjectByType<PantallaClinica>(FindObjectsInactive.Include);
            PantallaTienda tienda = mostrador.GetComponentInChildren<PantallaTienda>(true) ?? Object.FindFirstObjectByType<PantallaTienda>(FindObjectsInactive.Include); // T-1330: la del propio mostrador
            Assert.IsNotNull(mostrador, "Está la clínica del Doc");
            mostrador.AbrirDesdeDoc();
            yield return EsperarHasta(() => clinica.Visible, "abrir la clínica", 5f);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(MostradorClinica.TextoPestanaAtaque, clinica.TextoAtaque, "La clínica ofrece la pestaña de prótesis de ataque");
            Capturar("clinica-pestana.png");

            yield return Pulsar(teclado.tabKey);
            yield return EsperarHasta(() => tienda.Visible, "Tab abre las prótesis de ataque", 5f);
            Assert.IsFalse(clinica.Visible, "La clínica se esconde mientras");
            Assert.AreEqual(2, tienda.CantidadFilas, "Brazo láser y hombrera de misiles");
            StringAssert.Contains("BRAZO LÁSER", tienda.TextoFila(0).ToUpperInvariant());
            StringAssert.Contains("HOMBRERA DE MISILES", tienda.TextoFila(1).ToUpperInvariant());
            yield return new WaitForSecondsRealtime(0.4f);
            Capturar("protesis-ataque.png");

            int antes = cartera.Dinero;
            yield return Pulsar(teclado.enterKey);
            Assert.Less(cartera.Dinero, antes, "Enter compra el brazo láser");
            Assert.AreEqual(1, Registro<ServicioInventario>.Actual.Cantidad(CatalogoProtesisAtaque.Actual.De(HabilidadProtesis.Laser)), "Queda en el inventario");

            yield return Pulsar(teclado.escapeKey);
            yield return EsperarHasta(() => clinica.Visible && !tienda.Visible, "Esc vuelve a la clínica", 5f);
            yield return Pulsar(teclado.escapeKey);
            yield return EsperarHasta(() => !clinica.Visible && !mostrador.PantallaAbierta, "Esc sale de la clínica", 5f);
            yield return new WaitForSecondsRealtime(0.6f);

            Assert.AreEqual(1, HabilidadesProtesis.Actual.Nivel(HabilidadProtesis.Laser), "El láser ya está en las habilidades");
            BarraHabilidades barra = Object.FindFirstObjectByType<BarraHabilidades>();
            Assert.IsTrue(barra.CasillaVisible(HabilidadProtesis.Laser), "La barra M31 muestra el láser");
            Assert.AreEqual("LÁSER", barra.EtiquetaMostrada(HabilidadProtesis.Laser));
            Assert.IsFalse(barra.CasillaVisible(HabilidadProtesis.Misiles), "Sin la hombrera no sale su casilla");
            Assert.IsTrue(HabilidadesProtesis.Actual.Pulsar(HabilidadProtesis.Laser), "Se usa desde la barra");
            yield return new WaitForSecondsRealtime(0.8f);
            Capturar("barra-laser.png");
        }
    }
}

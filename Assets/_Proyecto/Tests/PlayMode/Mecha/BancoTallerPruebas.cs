using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Estado;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Mecha.Taller;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Taller;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mecha
{
    /// <summary>Reparar y pagar la renta en el banco del taller, en Pruebas_Calle con el jugador real y entrada simulada.</summary>
    public class BancoTallerPruebas : InputTestFixture
    {
        private const string RutaCalle = "Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity";
        private const string RutaTony = "Assets/_Proyecto/Escenas/Pruebas_Tony.unity";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float VidaDanada = 35f;
        private const int CostoEsperado = 650;
        private const int DineroInicial = 500;
        private const float MaxSegundosCaminar = 10f;
        private const float ZCercaDelBanco = -3f;
        private const float MargenLlegada = 2f;

        private Keyboard teclado;
        private JugadorAPie jugador;
        private MontajeMecha montaje;
        private LlamadoMecha llamado;
        private VidaMecha vida;
        private MechaPersistente persistente;
        private BancoTaller banco;
        private ICartera cartera;
        private HudAPie hud;
        private readonly List<(int, MotivoDinero)> cambios = new List<(int, MotivoDinero)>();

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            cambios.Clear();
        }

        public override void TearDown()
        {
            Registros.LimpiarTodos();
            base.TearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            yield return null;
            Registros.LimpiarTodos();
        }

        private IEnumerator Calentar()
        {
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
        }

        private IEnumerator CargarCalle()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaCalle, new LoadSceneParameters(LoadSceneMode.Single));
            yield return Calentar();
            jugador = Object.FindFirstObjectByType<JugadorAPie>();
            montaje = Object.FindFirstObjectByType<MontajeMecha>();
            llamado = montaje.GetComponent<LlamadoMecha>();
            vida = montaje.GetComponent<VidaMecha>();
            persistente = montaje.GetComponent<MechaPersistente>();
            banco = montaje.GetComponent<BancoTaller>();
            hud = Object.FindFirstObjectByType<HudAPie>();
            cartera = Registro<ICartera>.Actual;
            Assert.IsNotNull(jugador, "Pruebas_Calle trae el jugador");
            Assert.IsNotNull(banco, "El mecha trae el banco del taller");
            Assert.IsNotNull(cartera, "El jugador trae la cartera");
            Assert.IsTrue(banco.Registrado, "El banco se registra como interactuable");
            Assert.AreEqual(DineroInicial, cartera.Dinero);
            cartera.Cambio += (diferencia, motivo) => cambios.Add((diferencia, motivo));
            yield return null;
        }

        // Pone al jugador mirando al banco y camina con W hasta quedar a menos de 2.5 m.
        private IEnumerator IrAlBanco()
        {
            ITallerMecha taller = Registro<ITallerMecha>.Actual;
            Vector3 punto = taller.PuntoInteraccion;
            jugador.Colocar(new Pose(new Vector3(punto.x, 0.05f, punto.z + 4f), Quaternion.Euler(0f, 180f, 0f)));
            yield return null;
            Press(teclado.wKey);
            float fin = Time.time + MaxSegundosCaminar;
            while (Time.time < fin && jugador.Raiz.position.z > punto.z + MargenLlegada - 0.5f)
            {
                yield return null;
            }
            Release(teclado.wKey);
            yield return null;
            Assert.Less(Vector3.Distance(jugador.Raiz.position, punto), 2.5f, "Llegó al banco");
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private void Danar()
        {
            vida.FijarVida(VidaDanada);
            Assert.AreEqual(vida.Maxima - VidaDanada, persistente.Danio, 0.01f);
        }

        [UnityTest]
        public IEnumerator Reparar_ConDanio_CobraElCostoYDejaLaVidaAlMaximo()
        {
            yield return CargarCalle();
            Danar();
            yield return IrAlBanco();

            Assert.AreEqual("E · Reparar mecha · $650", hud.TextoAvisoInteraccion.text);
            Assert.AreEqual(CostoEsperado, banco.Costo);
            yield return Tecla(teclado.eKey);
            PantallaBancoTaller pantalla = banco.GetComponentInChildren<PantallaBancoTaller>(true);
            Assert.IsTrue(pantalla.Visible, "E abre la pantalla del banco y todavía no cobra");
            Assert.AreEqual(DineroInicial, cartera.Dinero);
            yield return Tecla(teclado.enterKey);

            Assert.IsFalse(pantalla.Visible);
            Assert.AreEqual(DineroInicial - CostoEsperado, cartera.Dinero, "Quedó en deuda: $500 - $650");
            Assert.AreEqual(1, cambios.Count);
            Assert.AreEqual((-CostoEsperado, MotivoDinero.Reparacion), cambios[0]);
            Assert.AreEqual(vida.Maxima, vida.Actual, 0.001f, "Vida al máximo");

            yield return null;
            yield return null; // el control del jugador vuelve un cuadro después de cerrar la pantalla
            Assert.AreEqual("E · Taller · piezas", hud.TextoAvisoInteraccion.text, "Sin daño el mismo E abre las piezas (T-168)");
            yield return Tecla(teclado.eKey);
            Assert.IsFalse(pantalla.Visible, "Sin daño no abre la pantalla");
            Assert.AreEqual(-150, cartera.Dinero, "La segunda E no cobra");
            Assert.AreEqual(1, cambios.Count);
            Assert.AreEqual(vida.Maxima, vida.Actual, 0.001f);
        }

        [UnityTest]
        public IEnumerator MechaLlamadoALaCalle_AvisaQueNoEstaYNoCobra()
        {
            yield return CargarCalle();
            Danar();
            yield return IrAlBanco();
            yield return Tecla(teclado.mKey);
            float fin = Time.time + llamado.Config.SegundosLlegada + MargenLlegada;
            while (Time.time < fin && llamado.Estado != EstadoLlamado.EnCalle)
            {
                yield return null;
            }
            Assert.AreEqual(EstadoLlamado.EnCalle, llamado.Estado, "El mecha llegó a la calle");
            yield return null;

            Assert.AreEqual("Tu mecha no está en el taller", hud.TextoAvisoInteraccion.text);
            Assert.AreEqual(EstadoBanco.MechaFuera, banco.Estado);
            yield return Tecla(teclado.eKey);

            Assert.AreEqual(DineroInicial, cartera.Dinero, "No cobró");
            Assert.AreEqual(0, cambios.Count);
            Assert.AreEqual(VidaDanada, vida.Actual, 0.001f, "No reparó");
        }

        [UnityTest]
        public IEnumerator ConRentaVencida_BloqueaHastaPagarYLuegoRepara()
        {
            yield return CargarCalle();
            Danar();
            IRenta renta = Registro<IRenta>.Actual;
            Assert.IsNotNull(renta, "El jugador trae la renta");
            ((IGuardable)renta).Cargar("{\"diaProximoCobro\":15,\"adeudo\":500}");
            cartera.CargarDeuda(DineroInicial, MotivoDinero.Otro);
            cambios.Clear();
            Assert.AreEqual(0, cartera.Dinero);
            Assert.IsTrue(renta.TallerBloqueado);
            yield return IrAlBanco();

            Assert.AreEqual("TALLER BLOQUEADO · debes $500 de renta", hud.TextoAvisoInteraccion.text);
            yield return Tecla(teclado.eKey);
            Assert.AreEqual(0, cartera.Dinero, "No cobró");
            Assert.AreEqual(VidaDanada, vida.Actual, 0.001f, "No reparó");
            Assert.IsTrue(renta.TallerBloqueado);

            cartera.Abonar(600, MotivoDinero.Otro);
            yield return null;
            Assert.AreEqual("E · Pagar renta · $500", hud.TextoAvisoInteraccion.text);
            yield return Tecla(teclado.eKey);

            Assert.AreEqual(100, cartera.Dinero, "Pagó la renta");
            Assert.IsFalse(renta.TallerBloqueado, "Taller desbloqueado");
            Assert.AreEqual(VidaDanada, vida.Actual, 0.001f, "Pagar la renta no repara");

            yield return null;
            Assert.AreEqual("E · Reparar mecha · $650", hud.TextoAvisoInteraccion.text);
        }

        [UnityTest]
        public IEnumerator MechaEnArranqueDirecto_NoRegistraInteractuables()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaTony, new LoadSceneParameters(LoadSceneMode.Single));
            yield return Calentar();
            BancoTaller bancoDirecto = Object.FindFirstObjectByType<BancoTaller>();
            Assert.IsNotNull(bancoDirecto, "El mecha de Pruebas_Tony trae el banco");
            Assert.IsFalse(bancoDirecto.Registrado);
            Assert.AreEqual(0, Registro<IInteractuable>.Cantidad, "Sin taller ni arranque Estacionado no hay interactuables");
        }
    }
}

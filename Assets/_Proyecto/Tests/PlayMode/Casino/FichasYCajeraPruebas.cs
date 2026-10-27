using System.Linq;
using System.Text.RegularExpressions;
using MechaCyberpunk.Casino.Cajera;
using MechaCyberpunk.Casino.Fichas;
using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Casino
{
    /// <summary>Fichas y cajera (T-302): guardado, cambio de dinero por fichas sin crear ni perder valor, y las dos opciones de la cajera.</summary>
    public class FichasYCajeraPruebas
    {
        private const string RutaServicio = "Assets/_Proyecto/Casino/Prefabs/ServicioFichas.prefab";
        private const string RutaCajera = "Assets/_Proyecto/Casino/Prefabs/Cajera.prefab";
        private const int DineroInicial = 1000;

        private sealed class CarteraDoble : ICartera
        {
            public int Dinero { get; set; }
            public MotivoDinero UltimoMotivo = MotivoDinero.Otro;
            public int Movimientos;

            public event System.Action<int, MotivoDinero> Cambio;

            public bool PuedePagar(int cantidad) => cantidad > 0 && Dinero >= cantidad;

            public bool IntentarPagar(int cantidad, MotivoDinero motivo)
            {
                if (!PuedePagar(cantidad))
                {
                    return false;
                }
                Dinero -= cantidad;
                Movimientos++;
                UltimoMotivo = motivo;
                Cambio?.Invoke(-cantidad, motivo);
                return true;
            }

            public void Abonar(int cantidad, MotivoDinero motivo)
            {
                if (cantidad <= 0)
                {
                    return;
                }
                Dinero += cantidad;
                Movimientos++;
                UltimoMotivo = motivo;
                Cambio?.Invoke(cantidad, motivo);
            }

            public void CargarDeuda(int cantidad, MotivoDinero motivo)
            {
                Dinero -= cantidad;
            }
        }

        private GameObject servicioObjeto;
        private GameObject cajeraObjeto;
        private ServicioFichas servicio;
        private CajeraMundo cajera;
        private CarteraDoble dinero;

        [SetUp]
        public void Preparar()
        {
            GameObject prefabServicio = AssetDatabase.LoadAssetAtPath<GameObject>(RutaServicio);
            GameObject prefabCajera = AssetDatabase.LoadAssetAtPath<GameObject>(RutaCajera);
            Assert.IsNotNull(prefabServicio, "Falta " + RutaServicio + "; corre CreadorFichas.Crear.");
            Assert.IsNotNull(prefabCajera, "Falta " + RutaCajera + "; corre CreadorFichas.Crear.");
            servicioObjeto = Object.Instantiate(prefabServicio);
            cajeraObjeto = Object.Instantiate(prefabCajera);
            servicio = servicioObjeto.GetComponent<ServicioFichas>();
            cajera = cajeraObjeto.GetComponent<CajeraMundo>();
            dinero = new CarteraDoble { Dinero = DineroInicial };
            cajera.Inyectar(dinero, servicio);
        }

        [TearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(cajeraObjeto);
            Object.DestroyImmediate(servicioObjeto);
        }

        [Test]
        public void Servicio_SeRegistraComoFichasYComoGuardableConClaveFichas()
        {
            Assert.IsTrue(Registro<IFichas>.Todos.Contains(servicio));
            Assert.IsTrue(Registro<IGuardable>.Todos.Contains(servicio));
            Assert.AreEqual("fichas", servicio.ClaveGuardado);
            servicioObjeto.SetActive(false);
            Assert.IsFalse(Registro<IFichas>.Todos.Contains(servicio));
            Assert.IsFalse(Registro<IGuardable>.Todos.Contains(servicio));
        }

        [Test]
        public void GuardarYCargar_ConservaElSaldo()
        {
            servicio.Abonar(370);
            string texto = servicio.Guardar();
            servicio.IntentarPagar(120);
            Assert.AreEqual(250, servicio.Saldo);
            servicio.Cargar(texto);
            Assert.AreEqual(370, servicio.Saldo);
        }

        [TestCase("esto no es json")]
        [TestCase("")]
        [TestCase("{\"fichas\":-5}")]
        public void Cargar_TextoInvalido_AvisaYNoCambiaNada(string texto)
        {
            servicio.Abonar(80);
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Casino\] Las fichas no entendieron"));
            servicio.Cargar(texto);
            Assert.AreEqual(80, servicio.Saldo);
        }

        [Test]
        public void Cajera_RegistraSusDosOpcionesYLasQuitaAlApagarse()
        {
            Assert.IsTrue(Registro<IInteractuable>.Todos.Contains(cajera.OpcionCambiar));
            Assert.IsTrue(Registro<IInteractuable>.Todos.Contains(cajera.OpcionCobrar));
            Assert.AreNotEqual(cajera.OpcionCambiar.Posicion, cajera.OpcionCobrar.Posicion, "Cada opción tiene su punto en el mostrador.");
            cajeraObjeto.SetActive(false);
            Assert.IsFalse(Registro<IInteractuable>.Todos.Contains(cajera.OpcionCambiar));
            Assert.IsFalse(Registro<IInteractuable>.Todos.Contains(cajera.OpcionCobrar));
        }

        [Test]
        public void Cambiar_CobraUnLoteDeDineroYDaUnLoteDeFichasConMotivoCasino()
        {
            DatosFichas datos = servicio.Datos;
            StringAssert.StartsWith("Cambiar $" + datos.DineroPorLote, cajera.OpcionCambiar.Texto);
            Assert.IsTrue(cajera.OpcionCambiar.PuedeUsar);
            cajera.OpcionCambiar.Usar();
            Assert.AreEqual(DineroInicial - datos.DineroPorLote, dinero.Dinero);
            Assert.AreEqual(datos.FichasPorLote, servicio.Saldo);
            Assert.AreEqual(MotivoDinero.Casino, dinero.UltimoMotivo);
        }

        [Test]
        public void Cambiar_SinDinero_NoHaceNadaYAvisa()
        {
            dinero.Dinero = servicio.Datos.DineroPorLote - 1;
            Assert.IsFalse(cajera.OpcionCambiar.PuedeUsar);
            StringAssert.StartsWith("Sin dinero", cajera.OpcionCambiar.Texto);
            cajera.OpcionCambiar.Usar();
            Assert.AreEqual(0, servicio.Saldo);
            Assert.AreEqual(0, dinero.Movimientos);
            Assert.AreEqual(servicio.Datos.DineroPorLote - 1, dinero.Dinero);
        }

        [Test]
        public void Cobrar_SinFichas_NoSePuedeUsar()
        {
            Assert.IsFalse(cajera.OpcionCobrar.PuedeUsar);
            Assert.AreEqual("Sin fichas para cobrar", cajera.OpcionCobrar.Texto);
            cajera.OpcionCobrar.Usar();
            Assert.AreEqual(0, dinero.Movimientos);
            Assert.AreEqual(DineroInicial, dinero.Dinero);
        }

        [Test]
        public void CambiarYCobrar_ConservaElValorSinCrearNiPerderDinero()
        {
            DatosFichas datos = servicio.Datos;
            cajera.OpcionCambiar.Usar();
            cajera.OpcionCambiar.Usar();
            cajera.OpcionCambiar.Usar();
            Assert.AreEqual(3 * datos.FichasPorLote, servicio.Saldo);
            Assert.AreEqual(DineroInicial - 3 * datos.DineroPorLote, dinero.Dinero);

            Assert.IsTrue(cajera.OpcionCobrar.PuedeUsar);
            cajera.OpcionCobrar.Usar();
            Assert.AreEqual(0, servicio.Saldo);
            Assert.AreEqual(DineroInicial, dinero.Dinero, "Cambiar y cobrar de vuelta deja el mismo dinero.");
            Assert.AreEqual(MotivoDinero.Casino, dinero.UltimoMotivo);
        }

        [Test]
        public void Cobrar_ConFichasQueNoCompletanUnLote_ConservaElResto()
        {
            DatosFichas datos = servicio.Datos;
            servicio.Abonar(datos.FichasPorLote + 30);
            cajera.OpcionCobrar.Usar();
            Assert.AreEqual(DineroInicial + datos.DineroPorLote, dinero.Dinero);
            Assert.AreEqual(30, servicio.Saldo);
            Assert.IsFalse(cajera.OpcionCobrar.PuedeUsar, "Con 30 fichas no hay un lote que cobrar.");
        }
    }
}

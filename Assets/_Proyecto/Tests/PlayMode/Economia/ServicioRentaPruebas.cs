using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Progresion.Pruebas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Economia
{
    /// <summary>La renta del jugador (T-107): registro, cobro con el reloj de prueba (tecla J) y guardado.</summary>
    public class ServicioRentaPruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const int DineroInicial = 500;
        private const int Renta = 500;
        private const int PrimerCobro = 8;
        private const int SaltosHastaElDia8 = 7;

        private Keyboard teclado;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
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

        private static GameObject CrearJugador() => Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador));

        private static RelojDePrueba CrearReloj() => new GameObject("RelojDePrueba").AddComponent<RelojDePrueba>();

        private static IGuardable BuscarGuardable(string clave)
        {
            foreach (IGuardable g in Registro<IGuardable>.Todos)
            {
                if (g.ClaveGuardado == clave) return g;
            }
            return null;
        }

        private IEnumerator PulsarJ()
        {
            Press(teclado.jKey);
            yield return null;
            Release(teclado.jKey);
            yield return null;
        }

        private IEnumerator PulsarJVeces(int veces)
        {
            for (int i = 0; i < veces; i++) yield return PulsarJ();
        }

        [UnityTest]
        public IEnumerator Jugador_EnEscenaVacia_TraeUnaRentaConElPrimerCobroElDia8()
        {
            GameObject jugador = CrearJugador();
            CrearReloj();
            yield return null;

            Assert.AreEqual(1, Registro<IRenta>.Cantidad, "Una sola renta registrada");
            Assert.AreEqual(1, jugador.GetComponentsInChildren<ServicioRenta>(true).Length);
            IRenta renta = Registro<IRenta>.Actual;
            Assert.AreEqual(Renta, renta.Monto);
            Assert.AreEqual(PrimerCobro, renta.DiaProximoCobro);
            Assert.AreEqual(0, renta.Adeudo);
            Assert.IsFalse(renta.TallerBloqueado);
            Assert.IsNotNull(BuscarGuardable("renta"));
        }

        [UnityTest]
        public IEnumerator TeclaJ_SieteVecesConDinero_CobraLaRentaElDia8()
        {
            CrearJugador();
            RelojDePrueba reloj = CrearReloj();
            yield return null;
            ICartera cartera = Registro<ICartera>.Actual;
            IRenta renta = Registro<IRenta>.Actual;

            yield return PulsarJVeces(SaltosHastaElDia8 - 1);
            Assert.AreEqual(PrimerCobro - 1, reloj.Dia);
            Assert.AreEqual(DineroInicial, cartera.Dinero, "Hasta el día 7 no cobra");

            MotivoDinero motivo = MotivoDinero.Otro;
            cartera.Cambio += (d, m) => motivo = m;
            yield return PulsarJ();

            Assert.AreEqual(PrimerCobro, reloj.Dia);
            Assert.AreEqual(DineroInicial - Renta, cartera.Dinero);
            Assert.AreEqual(MotivoDinero.Renta, motivo);
            Assert.AreEqual(0, renta.Adeudo);
            Assert.IsFalse(renta.TallerBloqueado);
            Assert.AreEqual(15, renta.DiaProximoCobro);
        }

        [UnityTest]
        public IEnumerator TeclaJ_SinDinero_BloqueaElTallerConAdeudoYPagarLoDesbloquea()
        {
            CrearJugador();
            CrearReloj();
            yield return null;
            ICartera cartera = Registro<ICartera>.Actual;
            IRenta renta = Registro<IRenta>.Actual;
            cartera.CargarDeuda(DineroInicial, MotivoDinero.Otro);
            Assert.AreEqual(0, cartera.Dinero);

            yield return PulsarJVeces(SaltosHastaElDia8);

            Assert.IsTrue(renta.TallerBloqueado);
            Assert.AreEqual(Renta, renta.Adeudo);
            Assert.AreEqual(0, cartera.Dinero);
            Assert.IsFalse(renta.PagarAdeudo(), "Sin dinero no puede pagar");

            cartera.Abonar(600, MotivoDinero.Otro);
            Assert.IsTrue(renta.PagarAdeudo());

            Assert.IsFalse(renta.TallerBloqueado);
            Assert.AreEqual(0, renta.Adeudo);
            Assert.AreEqual(100, cartera.Dinero);
            Assert.AreEqual(15, renta.DiaProximoCobro);
        }

        [UnityTest]
        public IEnumerator Reloj_QueApareceDespuesDelJugador_IgualCobra()
        {
            CrearJugador();
            yield return null;
            yield return null;
            Assert.AreEqual(0, Registro<IRelojMundo>.Cantidad);
            RelojDePrueba reloj = CrearReloj();
            yield return null;

            yield return PulsarJVeces(SaltosHastaElDia8);

            Assert.AreEqual(PrimerCobro, reloj.Dia);
            Assert.AreEqual(DineroInicial - Renta, Registro<ICartera>.Actual.Dinero);
        }

        [UnityTest]
        public IEnumerator SinReloj_LaRentaNoCobraNiDaError()
        {
            CrearJugador();
            yield return null;
            yield return null;

            Assert.AreEqual(DineroInicial, Registro<ICartera>.Actual.Dinero);
            Assert.AreEqual(0, Registro<IRenta>.Actual.Adeudo);
        }

        [UnityTest]
        public IEnumerator Reloj_QueSeQuita_YaNoCobra()
        {
            CrearJugador();
            RelojDePrueba reloj = CrearReloj();
            yield return null;
            IRenta renta = Registro<IRenta>.Actual;
            Object.Destroy(reloj.gameObject);
            yield return null;

            Assert.AreEqual(0, Registro<IRelojMundo>.Cantidad);
            Assert.AreEqual(PrimerCobro, renta.DiaProximoCobro);
        }

        [UnityTest]
        public IEnumerator GuardarYCargar_DevuelvenDiaHoraProximoCobroYAdeudo_SinCobrar()
        {
            CrearJugador();
            RelojDePrueba reloj = CrearReloj();
            yield return null;
            ICartera cartera = Registro<ICartera>.Actual;
            IRenta renta = Registro<IRenta>.Actual;
            cartera.CargarDeuda(DineroInicial, MotivoDinero.Otro);
            yield return PulsarJVeces(SaltosHastaElDia8);
            Assert.AreEqual(Renta, renta.Adeudo);

            string textoRenta = BuscarGuardable("renta").Guardar();
            string textoReloj = BuscarGuardable("reloj").Guardar();
            int dia = reloj.Dia;
            float hora = reloj.Hora;

            renta.PagarAdeudo();
            cartera.Abonar(5000, MotivoDinero.Otro);
            reloj.AvanzarHasta(12f);
            reloj.AvanzarHasta(8f);
            int diaOtro = reloj.Dia;
            Assert.AreNotEqual(dia, diaOtro);
            int dineroAntes = cartera.Dinero;

            BuscarGuardable("renta").Cargar(textoRenta);
            BuscarGuardable("reloj").Cargar(textoReloj);

            Assert.AreEqual(dia, reloj.Dia);
            Assert.AreEqual(hora, reloj.Hora, 0.01f);
            Assert.AreEqual(15, renta.DiaProximoCobro);
            Assert.AreEqual(Renta, renta.Adeudo);
            Assert.IsTrue(renta.TallerBloqueado);
            Assert.AreEqual(dineroAntes, cartera.Dinero, "Cargar no cobra nada");
        }

        [UnityTest]
        public IEnumerator Cargar_ConTextoQueNoSeEntiende_NoCambiaNadaYAvisa()
        {
            CrearJugador();
            yield return null;
            IRenta renta = Registro<IRenta>.Actual;

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"\[Economia\].*renta"));
            BuscarGuardable("renta").Cargar("basura");

            Assert.AreEqual(PrimerCobro, renta.DiaProximoCobro);
            Assert.AreEqual(0, renta.Adeudo);
        }
    }
}

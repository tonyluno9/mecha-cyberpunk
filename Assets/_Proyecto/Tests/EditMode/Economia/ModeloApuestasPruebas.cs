using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    public class ModeloApuestasPruebas
    {
        private sealed class AlmacenDeModelo : IAlmacenObjetos
        {
            public readonly ModeloInventario Modelo = new ModeloInventario();

            public bool PuedeAgregar(DatosObjeto objeto, int n) => Modelo.PuedeAgregar(objeto, n);

            public int Agregar(DatosObjeto objeto, int n) => Modelo.Agregar(objeto, n);
        }

        private DatosObjeto brazo;
        private DatosApuestaPelea datos;
        private ModeloCartera cartera;
        private AlmacenDeModelo inventario;
        private ModeloApuestas modelo;
        private readonly List<(int, MotivoDinero)> movimientos = new List<(int, MotivoDinero)>();
        private readonly List<int> resueltas = new List<int>();
        private int avisosCambio;

        [SetUp]
        public void SetUp()
        {
            brazo = DatosObjeto.Crear("brazo", "Brazo", TipoObjeto.Pieza, 300, 5);
            datos = DatosApuestaPelea.Crear(1f, 500, new[] { 100, 250, 500 }, brazo, 250);
            cartera = new ModeloCartera(500);
            inventario = new AlmacenDeModelo();
            modelo = new ModeloApuestas();
            movimientos.Clear();
            resueltas.Clear();
            avisosCambio = 0;
            cartera.Cambio += (monto, motivo) => movimientos.Add((monto, motivo));
            modelo.Resuelta += resueltas.Add;
            modelo.Cambio += () => avisosCambio++;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(datos);
            Object.DestroyImmediate(brazo);
        }

        [Test]
        public void Apostar_SinAnunciar_NoApuestaNiCambiaNada()
        {
            Assert.IsFalse(modelo.Abierta);
            Assert.IsFalse(modelo.PuedeApostar(100, cartera));
            Assert.IsFalse(modelo.Apostar(100, cartera));
            Assert.AreEqual(500, cartera.Dinero);
            Assert.AreEqual(0, modelo.Apostado);
            Assert.AreEqual(0, movimientos.Count);
        }

        [Test]
        public void Anunciar_ConDatos_AbreConRivalMomioYTope()
        {
            Assert.IsTrue(modelo.Anunciar("EL TUERTO", datos));

            Assert.IsTrue(modelo.Abierta);
            Assert.AreEqual("EL TUERTO", modelo.Rival);
            Assert.AreEqual(1f, modelo.Momio);
            Assert.AreEqual(500, modelo.ApuestaMaxima);
            Assert.AreEqual(0, modelo.Apostado);
        }

        [Test]
        public void Anunciar_SinDatos_NoAbre()
        {
            Assert.IsFalse(modelo.Anunciar("X", null));
            Assert.IsFalse(modelo.Abierta);
        }

        [Test]
        public void Apostar_Valido_CobraConMotivoApuestaYGuardaElMonto()
        {
            modelo.Anunciar("R", datos);

            Assert.IsTrue(modelo.Apostar(250, cartera));

            Assert.AreEqual(250, cartera.Dinero);
            Assert.AreEqual(250, modelo.Apostado);
            CollectionAssert.AreEqual(new[] { (-250, MotivoDinero.Apuesta) }, movimientos);
        }

        [Test]
        public void Apostar_DosVecesEnLaMismaPelea_LaSegundaFallaSinCambiarNada()
        {
            modelo.Anunciar("R", datos);
            modelo.Apostar(100, cartera);

            Assert.IsFalse(modelo.PuedeApostar(100, cartera));
            Assert.IsFalse(modelo.Apostar(100, cartera));

            Assert.AreEqual(400, cartera.Dinero);
            Assert.AreEqual(100, modelo.Apostado);
            Assert.AreEqual(1, movimientos.Count);
        }

        [TestCase(0)]
        [TestCase(-50)]
        [TestCase(501)]
        public void Apostar_MontoInvalido_FallaSinCambiarNada(int monto)
        {
            modelo.Anunciar("R", datos);

            Assert.IsFalse(modelo.Apostar(monto, cartera));

            Assert.AreEqual(500, cartera.Dinero);
            Assert.AreEqual(0, modelo.Apostado);
            Assert.AreEqual(0, movimientos.Count);
        }

        [Test]
        public void Apostar_SinDineroSuficiente_FallaSinCambiarNada()
        {
            var pobre = new ModeloCartera(99);
            modelo.Anunciar("R", datos);

            Assert.IsFalse(modelo.Apostar(100, pobre));

            Assert.AreEqual(99, pobre.Dinero);
            Assert.AreEqual(0, modelo.Apostado);
        }

        [Test]
        public void Apostar_ConDineroNegativo_Falla()
        {
            var endeudado = new ModeloCartera(-20);
            modelo.Anunciar("R", datos);

            Assert.IsFalse(modelo.Apostar(100, endeudado));

            Assert.AreEqual(-20, endeudado.Dinero);
        }

        [Test]
        public void Cancelar_AntesDeCerrar_DevuelveElDineroConMotivoApuesta()
        {
            modelo.Anunciar("R", datos);
            modelo.Apostar(250, cartera);
            movimientos.Clear();

            Assert.IsTrue(modelo.Cancelar(cartera));

            Assert.AreEqual(500, cartera.Dinero);
            Assert.AreEqual(0, modelo.Apostado);
            CollectionAssert.AreEqual(new[] { (250, MotivoDinero.Apuesta) }, movimientos);
            Assert.IsTrue(modelo.PuedeApostar(100, cartera), "Puede volver a apostar");
        }

        [Test]
        public void Cancelar_SinApuesta_DevuelveFalse()
        {
            modelo.Anunciar("R", datos);

            Assert.IsFalse(modelo.Cancelar(cartera));
            Assert.AreEqual(0, movimientos.Count);
        }

        [Test]
        public void Cerrar_ImpideApostarYCancelar()
        {
            modelo.Anunciar("R", datos);
            modelo.Apostar(250, cartera);
            modelo.Cerrar();

            Assert.IsFalse(modelo.Abierta);
            Assert.AreEqual(string.Empty, modelo.Rival);
            Assert.IsFalse(modelo.Cancelar(cartera));
            Assert.IsFalse(modelo.Apostar(100, cartera));
            Assert.AreEqual(250, cartera.Dinero);
            Assert.AreEqual(250, modelo.Apostado);
        }

        [Test]
        public void Resolver_Gana250ConMomioUno_Abona500YEntregaLaPieza()
        {
            modelo.Anunciar("R", datos);
            modelo.Apostar(250, cartera);
            modelo.Cerrar();
            movimientos.Clear();

            modelo.Resolver(true, cartera, inventario);

            Assert.AreEqual(750, cartera.Dinero);
            CollectionAssert.AreEqual(new[] { (500, MotivoDinero.Apuesta) }, movimientos);
            Assert.AreEqual(1, inventario.Modelo.Cantidad(brazo));
            CollectionAssert.AreEqual(new[] { 250 }, resueltas);
            Assert.IsFalse(modelo.Abierta);
            Assert.AreEqual(0, modelo.Apostado);
        }

        [Test]
        public void Resolver_Gana100_PagaPeroNoHayPieza()
        {
            modelo.Anunciar("R", datos);
            modelo.Apostar(100, cartera);
            modelo.Cerrar();

            modelo.Resolver(true, cartera, inventario);

            Assert.AreEqual(600, cartera.Dinero);
            Assert.AreEqual(0, inventario.Modelo.Cantidad(brazo));
            CollectionAssert.AreEqual(new[] { 100 }, resueltas);
        }

        [Test]
        public void Resolver_GanaConElInventarioLleno_PagaElValorDeVentaConMotivoVenta()
        {
            inventario.Modelo.Agregar(brazo, 5);
            modelo.Anunciar("R", datos);
            modelo.Apostar(250, cartera);
            modelo.Cerrar();
            movimientos.Clear();

            modelo.Resolver(true, cartera, inventario);

            Assert.AreEqual(5, inventario.Modelo.Cantidad(brazo));
            Assert.AreEqual(250 + 500 + 300, cartera.Dinero);
            CollectionAssert.AreEqual(new[] { (500, MotivoDinero.Apuesta), (300, MotivoDinero.Venta) }, movimientos);
        }

        [Test]
        public void Resolver_Pierde_NoDevuelveNadaYAvisaElNegativo()
        {
            modelo.Anunciar("R", datos);
            modelo.Apostar(250, cartera);
            modelo.Cerrar();
            movimientos.Clear();

            modelo.Resolver(false, cartera, inventario);

            Assert.AreEqual(250, cartera.Dinero);
            Assert.AreEqual(0, movimientos.Count);
            Assert.AreEqual(0, inventario.Modelo.Cantidad(brazo));
            CollectionAssert.AreEqual(new[] { -250 }, resueltas);
        }

        [Test]
        public void Resolver_SinApuesta_NoAvisaNiPaga()
        {
            modelo.Anunciar("R", datos);
            modelo.Cerrar();

            modelo.Resolver(true, cartera, inventario);

            Assert.AreEqual(500, cartera.Dinero);
            Assert.AreEqual(0, resueltas.Count);
            Assert.IsFalse(modelo.Pendiente);
        }

        [Test]
        public void Resolver_ConMomioFraccionario_RedondeaAlEnteroMasCercano()
        {
            DatosApuestaPelea otros = DatosApuestaPelea.Crear(1.5f, 500, new[] { 100 }, null, 0);
            modelo.Anunciar("R", otros);
            modelo.Apostar(101, cartera);
            modelo.Cerrar();

            modelo.Resolver(true, cartera, inventario);

            // 101 × 1.5 = 151.5 → 152; se devuelven 101 más 152.
            Assert.AreEqual(500 - 101 + 101 + 152, cartera.Dinero);
            CollectionAssert.AreEqual(new[] { 152 }, resueltas);
            Object.DestroyImmediate(otros);
        }

        [Test]
        public void Devolver_ConApuestaCerrada_DevuelveElDineroYNoAvisaResuelta()
        {
            modelo.Anunciar("R", datos);
            modelo.Apostar(250, cartera);
            modelo.Cerrar();

            modelo.Devolver(cartera);

            Assert.AreEqual(500, cartera.Dinero);
            Assert.AreEqual(0, modelo.Apostado);
            Assert.AreEqual(0, resueltas.Count);
            Assert.IsFalse(modelo.Pendiente);
        }

        [Test]
        public void Devolver_DosVeces_DevuelveUnaSolaVez()
        {
            modelo.Anunciar("R", datos);
            modelo.Apostar(250, cartera);

            modelo.Devolver(cartera);
            modelo.Devolver(cartera);

            Assert.AreEqual(500, cartera.Dinero);
        }

        [Test]
        public void Anunciar_ConApuestaSinResolver_NoReemplazaNiPierdeElDinero()
        {
            modelo.Anunciar("R", datos);
            modelo.Apostar(250, cartera);
            modelo.Cerrar();

            Assert.IsFalse(modelo.Anunciar("OTRO", datos));

            modelo.Devolver(cartera);
            Assert.AreEqual(500, cartera.Dinero);
        }

        [Test]
        public void DespuesDeResolver_NadaQuedaAbiertoHastaElSiguienteAnunciar()
        {
            modelo.Anunciar("R", datos);
            modelo.Apostar(250, cartera);
            modelo.Cerrar();
            modelo.Resolver(false, cartera, inventario);

            Assert.IsFalse(modelo.Abierta);
            Assert.IsFalse(modelo.PuedeApostar(100, cartera));
            Assert.IsFalse(modelo.Apostar(100, cartera));
            Assert.IsTrue(modelo.Anunciar("SIGUE", datos));
            Assert.IsTrue(modelo.Apostar(100, cartera));
        }

        [Test]
        public void Cambio_Avisa_AlAnunciarApostarCerrarYResolver()
        {
            modelo.Anunciar("R", datos);
            modelo.Apostar(100, cartera);
            modelo.Cerrar();
            modelo.Resolver(false, cartera, inventario);

            Assert.AreEqual(4, avisosCambio);
        }
    }
}

using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    public class ModeloInventarioPruebas
    {
        private const string CarpetaDatos = "Assets/_Proyecto/Economia/Datos/";

        private DatosObjeto brazo;
        private DatosObjeto kit;
        private ModeloInventario modelo;
        private int avisos;

        [SetUp]
        public void SetUp()
        {
            brazo = DatosObjeto.Crear("brazo", "Brazo", TipoObjeto.Pieza, 300, 5);
            kit = DatosObjeto.Crear("kit", "Kit", TipoObjeto.KitReparacion, 0, 3);
            modelo = new ModeloInventario();
            avisos = 0;
            modelo.Cambio += () => avisos++;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(brazo);
            Object.DestroyImmediate(kit);
        }

        [Test]
        public void Agregar_ConMasDelTope_DejaElTopeYDevuelveCuantosEntraron()
        {
            int entraron = modelo.Agregar(kit, 5);

            Assert.AreEqual(3, entraron);
            Assert.AreEqual(3, modelo.Cantidad(kit));
            Assert.AreEqual(1, avisos);
        }

        [Test]
        public void Agregar_ConElTopeLleno_NoEntraNadaYNoAvisa()
        {
            modelo.Agregar(kit, 3);
            avisos = 0;

            Assert.AreEqual(0, modelo.Agregar(kit, 1));
            Assert.AreEqual(3, modelo.Cantidad(kit));
            Assert.AreEqual(0, avisos);
        }

        [Test]
        public void PuedeAgregar_RespetaElTope()
        {
            modelo.Agregar(kit, 2);

            Assert.IsTrue(modelo.PuedeAgregar(kit, 1));
            Assert.IsFalse(modelo.PuedeAgregar(kit, 2));
            Assert.IsFalse(modelo.PuedeAgregar(kit, 0));
            Assert.IsFalse(modelo.PuedeAgregar(null, 1));
        }

        [Test]
        public void Quitar_MasDeLoQueHay_NoCambiaNadaYNoAvisa()
        {
            modelo.Agregar(brazo, 2);
            avisos = 0;

            Assert.IsFalse(modelo.Quitar(brazo, 3));
            Assert.AreEqual(2, modelo.Cantidad(brazo));
            Assert.AreEqual(0, avisos);
        }

        [Test]
        public void Quitar_Todos_SacaElObjetoDeTodos()
        {
            modelo.Agregar(brazo, 2);

            Assert.IsTrue(modelo.Quitar(brazo, 2));
            Assert.AreEqual(0, modelo.Cantidad(brazo));
            Assert.AreEqual(0, modelo.Todos.Count);
        }

        [Test]
        public void CantidadNoPositiva_NoHaceNada()
        {
            modelo.Agregar(brazo, 1);
            avisos = 0;

            Assert.AreEqual(0, modelo.Agregar(brazo, 0));
            Assert.AreEqual(0, modelo.Agregar(brazo, -2));
            Assert.IsFalse(modelo.Quitar(brazo, 0));
            Assert.IsFalse(modelo.Quitar(brazo, -1));
            Assert.AreEqual(1, modelo.Cantidad(brazo));
            Assert.AreEqual(0, avisos);
        }

        [Test]
        public void Cambio_AvisaUnaVezPorCadaCambioReal()
        {
            modelo.Agregar(brazo, 2);
            modelo.Quitar(brazo, 1);
            modelo.Agregar(kit, 1);

            Assert.AreEqual(3, avisos);
        }

        [Test]
        public void Todos_ListaSoloLosQueHayEnOrdenDeLlegada()
        {
            modelo.Agregar(kit, 1);
            modelo.Agregar(brazo, 1);
            modelo.Agregar(kit, 1);

            Assert.AreEqual(2, modelo.Todos.Count);
            Assert.AreSame(kit, modelo.Todos[0]);
            Assert.AreSame(brazo, modelo.Todos[1]);
        }

        [Test]
        public void Restablecer_ReemplazaTodoYAvisaUnaVez()
        {
            modelo.Agregar(kit, 1);
            avisos = 0;

            modelo.Restablecer(new[] { new KeyValuePair<DatosObjeto, int>(brazo, 2) });

            Assert.AreEqual(0, modelo.Cantidad(kit));
            Assert.AreEqual(2, modelo.Cantidad(brazo));
            Assert.AreEqual(1, avisos);
        }

        [Test]
        public void Catalogo_BuscaPorId()
        {
            var catalogo = CatalogoObjetos.Crear(brazo, kit);

            Assert.AreSame(kit, catalogo.Buscar("kit"));
            Assert.IsNull(catalogo.Buscar("otro"));
            Assert.IsNull(catalogo.Buscar(null));
            Object.DestroyImmediate(catalogo);
        }

        [Test]
        public void CatalogoBase_TieneIdsUnicosMaximoValidoYNingunObjetoSeVendePorSuPrecioDeCompra()
        {
            var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoObjetos>(CarpetaDatos + "CatalogoObjetos_Base.asset");
            Assert.IsNotNull(catalogo, "Existe CatalogoObjetos_Base");
            Assert.GreaterOrEqual(catalogo.Objetos.Count, 3);

            var ids = new HashSet<string>();
            foreach (DatosObjeto o in catalogo.Objetos)
            {
                Assert.IsNotNull(o, "Sin objetos vacíos en el catálogo");
                Assert.IsFalse(string.IsNullOrEmpty(o.Id), "id no vacío");
                Assert.IsTrue(ids.Add(o.Id), "id único: " + o.Id);
                Assert.GreaterOrEqual(o.Maximo, 1, o.Id);
                if (o.Oferta != null) Assert.Less(o.ValorVenta, o.Oferta.Precio, o.Id + " se vende por menos de lo que cuesta");
            }
        }

        [Test]
        public void CatalogoBase_TieneLosValoresDeD86()
        {
            var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoObjetos>(CarpetaDatos + "CatalogoObjetos_Base.asset");

            DatosObjeto brazoBase = catalogo.Buscar("brazo_chatarra");
            Assert.AreEqual(TipoObjeto.Pieza, brazoBase.Tipo);
            Assert.AreEqual(300, brazoBase.ValorVenta);
            Assert.AreEqual(5, brazoBase.Maximo);

            DatosObjeto chatarra = catalogo.Buscar("chatarra");
            Assert.AreEqual(TipoObjeto.Chatarra, chatarra.Tipo);
            Assert.AreEqual(50, chatarra.ValorVenta);
            Assert.AreEqual(20, chatarra.Maximo);

            DatosObjeto kitBase = catalogo.Buscar("kit_reparacion");
            Assert.AreEqual(TipoObjeto.KitReparacion, kitBase.Tipo);
            Assert.AreEqual(0, kitBase.ValorVenta);
            Assert.AreEqual(3, kitBase.Maximo);
            Assert.AreSame(AssetDatabase.LoadAssetAtPath<DatosOferta>(CarpetaDatos + "DatosOferta_KitReparacion.asset"), kitBase.Oferta);
        }
    }
}

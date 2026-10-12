using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    public class TiendaBasicaPruebas
    {
        private DatosOferta barata;
        private DatosOferta cara;
        private DatosOferta ajena;
        private DatosTienda datosTienda;
        private TiendaBasica tienda;
        private ModeloCartera cartera;

        private static DatosOferta Oferta(string id, int precio)
        {
            var o = ScriptableObject.CreateInstance<DatosOferta>();
            var so = new SerializedObject(o);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("precio").intValue = precio;
            so.ApplyModifiedPropertiesWithoutUndo();
            return o;
        }

        private static DatosTienda Tienda(IdLugar lugar, params DatosOferta[] ofertas)
        {
            var t = ScriptableObject.CreateInstance<DatosTienda>();
            var so = new SerializedObject(t);
            so.FindProperty("lugar").enumValueIndex = (int)lugar;
            var lista = so.FindProperty("ofertas");
            lista.arraySize = ofertas.Length;
            for (int i = 0; i < ofertas.Length; i++)
                lista.GetArrayElementAtIndex(i).objectReferenceValue = ofertas[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return t;
        }

        [SetUp]
        public void SetUp()
        {
            barata = Oferta("barata", 100);
            cara = Oferta("cara", 5000);
            ajena = Oferta("ajena", 10);
            datosTienda = Tienda(IdLugar.MercadoNegro, barata, cara);
            tienda = new TiendaBasica(datosTienda);
            cartera = new ModeloCartera(1000);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(barata);
            Object.DestroyImmediate(cara);
            Object.DestroyImmediate(ajena);
            Object.DestroyImmediate(datosTienda);
        }

        [Test]
        public void Comprar_ConDinero_CobraYAvisaUnaVez()
        {
            int avisos = 0;
            DatosOferta avisada = null;
            tienda.Comprado += o => { avisos++; avisada = o; };

            Assert.AreEqual(ResultadoCompra.Comprado, tienda.Comprar(barata, cartera));
            Assert.AreEqual(900, cartera.Dinero);
            Assert.AreEqual(1, avisos);
            Assert.AreSame(barata, avisada);
        }

        [Test]
        public void Comprar_SinDinero_NoCobraNiAvisa()
        {
            int avisos = 0;
            tienda.Comprado += _ => avisos++;

            Assert.AreEqual(ResultadoCompra.SinDinero, tienda.Comprar(cara, cartera));
            Assert.AreEqual(1000, cartera.Dinero);
            Assert.AreEqual(0, avisos);
        }

        [Test]
        public void Comprar_OfertaAjenaONula_NoDisponible()
        {
            Assert.AreEqual(ResultadoCompra.NoDisponible, tienda.Comprar(ajena, cartera));
            Assert.AreEqual(ResultadoCompra.NoDisponible, tienda.Comprar(null, cartera));
            Assert.AreEqual(1000, cartera.Dinero);
            Assert.IsFalse(tienda.EstaDisponible(ajena));
            Assert.IsTrue(tienda.EstaDisponible(barata));
        }

        [Test]
        public void Comprar_EnClinica_CobraConMotivoClinica()
        {
            var datosClinica = Tienda(IdLugar.Clinica, barata);
            try
            {
                var clinica = new TiendaBasica(datosClinica);
                MotivoDinero motivo = MotivoDinero.Otro;
                cartera.Cambio += (_, m) => motivo = m;
                clinica.Comprar(barata, cartera);
                Assert.AreEqual(MotivoDinero.Clinica, motivo);
            }
            finally { Object.DestroyImmediate(datosClinica); }
        }

        [Test]
        public void Comprar_EnOtroLugar_CobraConMotivoCompra()
        {
            MotivoDinero motivo = MotivoDinero.Otro;
            cartera.Cambio += (_, m) => motivo = m;
            tienda.Comprar(barata, cartera);
            Assert.AreEqual(MotivoDinero.Compra, motivo);
        }

        [Test]
        public void Propiedades_VienenDeLosDatos()
        {
            Assert.AreEqual(IdLugar.MercadoNegro, tienda.Lugar);
            Assert.AreEqual(2, tienda.Ofertas.Count);
        }
    }
}

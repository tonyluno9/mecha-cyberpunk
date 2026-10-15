using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests
{
    public class DatosEconomiaAssetsPruebas
    {
        private const string Carpeta = "Assets/_Proyecto/Economia/Datos/";

        private static DatosOferta Oferta(string archivo, int precio, TipoOferta tipo)
        {
            var o = AssetDatabase.LoadAssetAtPath<DatosOferta>($"{Carpeta}DatosOferta_{archivo}.asset");
            Assert.IsNotNull(o, archivo);
            Assert.AreEqual(precio, o.Precio, archivo);
            Assert.AreEqual(tipo, o.Tipo, archivo);
            Assert.AreEqual("Precio de prueba", o.Descripcion, archivo);
            return o;
        }

        private static void Tienda(string archivo, IdLugar lugar, params DatosOferta[] ofertas)
        {
            var t = AssetDatabase.LoadAssetAtPath<DatosTienda>($"{Carpeta}DatosTienda_{archivo}.asset");
            Assert.IsNotNull(t, archivo);
            Assert.AreEqual(lugar, t.Lugar, archivo);
            Assert.AreEqual(ofertas.Length, t.Ofertas.Count, archivo);
            for (int i = 0; i < ofertas.Length; i++)
                Assert.AreSame(ofertas[i], t.Ofertas[i], archivo);
        }

        [Test]
        public void DatosEconomiaBase_Existe_ConLosValoresDeLaDemo()
        {
            var d = AssetDatabase.LoadAssetAtPath<MechaCyberpunk.Economia.DatosEconomia>(Carpeta + "DatosEconomia_Base.asset");
            Assert.IsNotNull(d);
            Assert.AreEqual(500, d.DineroInicial);
            Assert.AreEqual(10, d.CostoReparacionPorPunto);
            Assert.AreEqual(50, d.CostoReparacionMinimo);
            Assert.AreEqual(500, d.Renta);
            Assert.AreEqual(7, d.DiasEntreRentas);
        }

        [Test]
        public void Assets_ExistenConLosValoresDeT033()
        {
            var kit = Oferta("KitReparacion", 300, TipoOferta.KitReparacion);
            var misil = Oferta("MunicionMisil", 500, TipoOferta.Municion);
            var brazo = Oferta("BrazoChatarra", 1200, TipoOferta.Pieza);
            var ojos = Oferta("ImplanteOjos", 5000, TipoOferta.Implante);
            var bebida = Oferta("Bebida", 50, TipoOferta.Consumible);

            Tienda("MercadoNegro", IdLugar.MercadoNegro, kit, misil, brazo);
            Tienda("Clinica", IdLugar.Clinica, ojos);
            Tienda("Bar", IdLugar.Bar, bebida);
        }
    }
}

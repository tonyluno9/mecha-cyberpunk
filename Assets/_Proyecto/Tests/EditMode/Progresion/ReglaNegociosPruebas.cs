using MechaCyberpunk.Progresion;
using MechaCyberpunk.UI.Kit;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>T-429: las cuentas de los negocios, los números del asset contra los precios de El Mercado y el lugar del aviso de zona.</summary>
    public class ReglaNegociosPruebas
    {
        private static readonly float[] Multiplicadores = { 1f, 1.6f, 2.4f };
        private static readonly float[] Factores = { 1.25f, 2f };

        [Test]
        public void Ingreso_PorNivel_SigueLosMultiplicadores()
        {
            Assert.AreEqual(0, ReglaNegocios.Ingreso(50, 0, Multiplicadores), "Sin tomar no paga");
            Assert.AreEqual(50, ReglaNegocios.Ingreso(50, 1, Multiplicadores));
            Assert.AreEqual(80, ReglaNegocios.Ingreso(50, 2, Multiplicadores));
            Assert.AreEqual(120, ReglaNegocios.Ingreso(50, 3, Multiplicadores));
            Assert.AreEqual(120, ReglaNegocios.Ingreso(50, 9, Multiplicadores), "Un nivel de más se queda en el máximo");
        }

        [Test]
        public void PrecioSiguiente_TomarMejorarYMaximo()
        {
            Assert.AreEqual(400, ReglaNegocios.PrecioSiguiente(400, 0, Factores, 3));
            Assert.AreEqual(500, ReglaNegocios.PrecioSiguiente(400, 1, Factores, 3));
            Assert.AreEqual(800, ReglaNegocios.PrecioSiguiente(400, 2, Factores, 3));
            Assert.AreEqual(0, ReglaNegocios.PrecioSiguiente(400, 3, Factores, 3), "Al máximo no hay más mejoras");
        }

        [Test]
        public void DiasParaRecuperar_SinIngreso_EsInfinito()
        {
            Assert.AreEqual(10f, ReglaNegocios.DiasParaRecuperar(400, 40));
            Assert.IsTrue(float.IsPositiveInfinity(ReglaNegocios.DiasParaRecuperar(400, 0)));
        }

        [Test]
        public void Asset_Equilibrado_NoRegalaArmasNiRenta()
        {
            DatosNegocios datos = AssetDatabase.LoadAssetAtPath<DatosNegocios>("Assets/_Proyecto/Progresion/Datos/DatosNegocios_Base.asset");
            Assert.IsNotNull(datos, "Falta DatosNegocios_Base (corre el creador T429)");
            Assert.AreEqual(6, datos.Negocios.Length);
            Assert.AreEqual(3, datos.NivelMaximo);
            int maximoPorDia = 0;
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (DatosNegocios.Negocio n in datos.Negocios)
            {
                Assert.IsTrue(ids.Add(n.id), "Id repetido " + n.id);
                Assert.IsNotEmpty(n.nombre);
                Assert.Greater(n.precio, 150, "Tomar un negocio cuesta más que la pistola más barata de El Mercado (Ƀ150)");
                Assert.GreaterOrEqual(ReglaNegocios.DiasParaRecuperar(n.precio, ReglaNegocios.Ingreso(n.ingresoBase, 1, datos.Multiplicadores)), 6f,
                    n.id + ": recuperar la compra tarda al menos 6 días de juego");
                int total = n.precio + ReglaNegocios.PrecioSiguiente(n.precio, 1, datos.FactoresMejora, 3) + ReglaNegocios.PrecioSiguiente(n.precio, 2, datos.FactoresMejora, 3);
                int ingreso = ReglaNegocios.Ingreso(n.ingresoBase, 3, datos.Multiplicadores);
                Assert.GreaterOrEqual(ReglaNegocios.DiasParaRecuperar(total, ingreso), 8f, n.id + ": con todas las mejoras tarda al menos 8 días");
                maximoPorDia += ingreso;
            }
            Assert.Less(maximoPorDia, 1000, "Todo al máximo rinde menos que el arma más cara de El Mercado (Ƀ900) por día de 20 min");
        }

        [Test]
        public void DisposicionHud_AvisoZona_NoSeEncimaConLaFilaDeLaZona()
        {
            Assert.GreaterOrEqual(DisposicionHud.AvisoZona.DesdeBorde, DisposicionHud.Zona.Fin, "El aviso de zona empieza debajo de la fila de la zona");
        }
    }
}

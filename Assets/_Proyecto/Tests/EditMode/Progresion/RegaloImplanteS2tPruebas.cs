using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-1005 (playtest 16-nov): el Doc ya no regala el exo de piernas directo; en la clínica el EXO DE PIERNAS I cuesta 0 (GRATIS) y se compra,
    /// y el nivel II de cualquier prótesis se abre en cuanto tienes el nivel I, sin requisito de liga.
    /// </summary>
    public class RegaloImplanteS2tPruebas
    {
        private const string RutaCatalogo = "Assets/_Proyecto/Progresion/Datos/Implantes/CatalogoImplantes_Base.asset";
        private const int LigaUno = 1;
        private const int DineroCero = 0;
        private const int DineroDeuda = -300;
        private const int DineroSobrado = 100000;

        private static CatalogoImplantes Catalogo()
        {
            CatalogoImplantes catalogo = AssetDatabase.LoadAssetAtPath<CatalogoImplantes>(RutaCatalogo);
            Assert.IsNotNull(catalogo, RutaCatalogo);
            return catalogo;
        }

        [Test]
        public void ExoDePiernasI_CuestaCeroYSeOfreceAunSinDineroNiLiga()
        {
            CatalogoImplantes catalogo = Catalogo();
            Assert.AreEqual(0, catalogo.Buscar(EspacioImplante.Piernas, 1).Precio);
            foreach (int dinero in new[] { DineroCero, DineroDeuda })
            {
                OfertaImplante o = ReglaImplantes.Oferta(catalogo, EspacioImplante.Piernas, 0, LigaUno, dinero);
                Assert.AreEqual(EstadoOfertaImplante.Instalar, o.Estado, "con dinero " + dinero);
                Assert.AreEqual(0, o.Costo);
            }
        }

        [Test]
        public void NivelII_DeCualquierProtesis_SeOfreceConElIEnLaLiga1()
        {
            CatalogoImplantes catalogo = Catalogo();
            foreach (EspacioImplante espacio in System.Enum.GetValues(typeof(EspacioImplante)))
            {
                DatosImplante nivel2 = catalogo.Buscar(espacio, 2);
                Assert.AreEqual(LigaUno, nivel2.LigaMinima, espacio + " II no pide liga");
                OfertaImplante o = ReglaImplantes.Oferta(catalogo, espacio, 1, LigaUno, DineroSobrado);
                Assert.AreEqual(EstadoOfertaImplante.Instalar, o.Estado, espacio + " II se puede comprar con el I");
            }
        }

        [Test]
        public void ElDoc_DiceElegirProtesis()
        {
            StringAssert.Contains("Elige tu prótesis, te regalo la de piernas", MechaCyberpunk.Progresion.Apoyo.RegaloImplanteDoc.TextoFrase);
        }
    }
}

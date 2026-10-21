using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Mecha;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests
{
    public class PiezasAssetsPruebas
    {
        private CatalogoPiezas catalogo;
        private CatalogoObjetos objetos;

        [SetUp]
        public void SetUp()
        {
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoPiezas>("Assets/_Proyecto/Mecha/Datos/Piezas/CatalogoPiezas_Base.asset");
            objetos = AssetDatabase.LoadAssetAtPath<CatalogoObjetos>("Assets/_Proyecto/Economia/Datos/CatalogoObjetos_Base.asset");
            Assert.NotNull(catalogo);
            Assert.NotNull(objetos);
        }

        [Test]
        public void Catalogo_Tiene85PiezasConIdUnicoYTipoPieza()
        {
            Assert.AreEqual(85, catalogo.Piezas.Count);
            Assert.AreEqual(85, catalogo.Piezas.Select(p => p.Id).Distinct().Count());
            Assert.IsTrue(catalogo.Piezas.All(p => p.Tipo == TipoObjeto.Pieza && p.Maximo == 2));
        }

        [Test]
        public void Catalogo_UnaDeSeriePorRanuraY16PorRanuraConRama()
        {
            for (int r = 0; r < 5; r++)
            {
                var ranura = (RanuraPieza)r;
                Assert.NotNull(catalogo.DeSerie(ranura), "serie " + ranura);
                List<DatosPieza> de = catalogo.De(ranura);
                Assert.AreEqual(1, de.Count(p => p.Rama == RamaPieza.DeSerie), "series " + ranura);
                Assert.AreEqual(16, de.Count(p => p.Rama != RamaPieza.DeSerie), "ramas " + ranura);
                Assert.AreEqual(100, catalogo.DeSerie(ranura).ValorVenta);
                Assert.AreEqual(0, catalogo.DeSerie(ranura).Precio);
            }
        }

        [Test]
        public void Piezas_EstanEnElCatalogoDeObjetos()
        {
            foreach (DatosPieza p in catalogo.Piezas) Assert.AreSame(p, objetos.Buscar(p.Id), p.Id);
        }

        [Test]
        public void Ids_SiguenElFormatoYEjemplo()
        {
            DatosPieza p = catalogo.Buscar("pieza_brazoder_militar_2");
            Assert.NotNull(p);
            Assert.AreEqual("BRAZO DER. MILITAR II", p.Nombre);
            Assert.AreEqual("TORSO DE SERIE", catalogo.DeSerie(RanuraPieza.Torso).Nombre);
        }

        private ValoresMecha ConCinco(RamaPieza rama, int nivel)
        {
            var puestas = new List<DatosPieza>();
            for (int r = 0; r < 5; r++)
            {
                puestas.Add(catalogo.De((RanuraPieza)r).Single(p => p.Rama == rama && p.Nivel == nivel));
            }
            return ReglaEstadisticasPiezas.Componer(AssetDatabase.LoadAssetAtPath<DatosMecha>("Assets/_Proyecto/Mecha/Datos/DatosMecha_Jugador.asset").Valores, puestas);
        }

        [Test]
        public void Ramas_CuerpoACuerpoAguantaYCorreMasQueMilitar_MilitarPegaMas_TechEnMedio()
        {
            for (int n = 1; n <= 4; n++)
            {
                ValoresMecha mil = ConCinco(RamaPieza.Militar, n);
                ValoresMecha tec = ConCinco(RamaPieza.Tech, n);
                ValoresMecha cac = ConCinco(RamaPieza.CuerpoACuerpo, n);
                Assert.Greater(cac.vidaMaxima, mil.vidaMaxima, "vida cac>mil " + n);
                Assert.Greater(cac.velocidadMaxima, mil.velocidadMaxima, "vel cac>mil " + n);
                Assert.Greater(mil.danioGolpe, cac.danioGolpe, "danio mil>cac " + n);
                Assert.Greater(tec.vidaMaxima, mil.vidaMaxima, "vida tech>mil " + n);
                Assert.Less(tec.vidaMaxima, cac.vidaMaxima, "vida tech<cac " + n);
                Assert.Less(tec.danioGolpe, mil.danioGolpe, "danio tech<mil " + n);
                Assert.Greater(tec.danioGolpe, cac.danioGolpe, "danio tech>cac " + n);
            }
        }

        [Test]
        public void Precio_SubeConElNivel_YLaVentaEsMenorQueElPrecio()
        {
            foreach (RamaPieza rama in new[] { RamaPieza.Militar, RamaPieza.Tech, RamaPieza.CuerpoACuerpo, RamaPieza.Fuego })
            {
                for (int r = 0; r < 5; r++)
                {
                    int anterior = 0;
                    for (int n = 1; n <= 4; n++)
                    {
                        DatosPieza p = catalogo.De((RanuraPieza)r).Single(x => x.Rama == rama && x.Nivel == n);
                        Assert.Greater(p.Precio, anterior, p.Id);
                        Assert.Less(p.ValorVenta, p.Precio, p.Id);
                        Assert.AreEqual(p.Precio / 2, p.ValorVenta, p.Id);
                        anterior = p.Precio;
                    }
                }
            }
        }
    }
}

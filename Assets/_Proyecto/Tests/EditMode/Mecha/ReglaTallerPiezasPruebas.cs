using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Mecha;
using MechaCyberpunk.Mecha.Taller;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests.Mecha
{
    /// <summary>T-168: las opciones del taller de piezas con las piezas reales del catálogo.</summary>
    public class ReglaTallerPiezasPruebas
    {
        private const string RutaCatalogo = "Assets/_Proyecto/Mecha/Datos/Piezas/CatalogoPiezas_Base.asset";

        private CatalogoPiezas catalogo;
        private DatosPieza deSerie;

        [SetUp]
        public void Preparar()
        {
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoPiezas>(RutaCatalogo);
            Assert.IsNotNull(catalogo, "Existe CatalogoPiezas_Base");
            deSerie = catalogo.DeSerie(RanuraPieza.Torso);
        }

        private List<OpcionPieza> Torso(int liga, int dinero, bool bloqueado = false, IEnumerable<DatosPieza> inventario = null,
            DatosPieza puesta = null)
        {
            return ReglaTallerPiezas.Opciones(RanuraPieza.Torso, puesta ?? deSerie, inventario, catalogo.De(RanuraPieza.Torso), liga, dinero,
                bloqueado);
        }

        [Test]
        public void Liga1_CompraNivelI_YElNivelIISeAbreEnLaLiga2()
        {
            List<OpcionPieza> opciones = Torso(1, 1000);

            List<OpcionPieza> compras = opciones.Where(o => o.Tipo == TipoOpcionPieza.Comprar).ToList();
            Assert.AreEqual(4, compras.Count, "Las 4 ramas de nivel I");
            Assert.IsTrue(compras.All(o => o.Pieza.Nivel == 1 && o.PuedeElegir && o.Motivo == MotivoNoElegible.Ninguno));
            Assert.AreEqual("COMPRAR $500", compras[0].Texto);

            List<OpcionPieza> cerradas = opciones.Where(o => o.Tipo == TipoOpcionPieza.Bloqueada).ToList();
            Assert.AreEqual(4, cerradas.Count, "Las 4 ramas de nivel II");
            Assert.IsTrue(cerradas.All(o => o.Pieza.Nivel == 2 && !o.PuedeElegir && o.Motivo == MotivoNoElegible.NivelBloqueado));
            Assert.IsTrue(cerradas.All(o => o.Texto == "SE ABRE EN LA LIGA 2"));
            Assert.AreEqual(8, opciones.Count, "Ni el nivel III ni el IV aparecen en la liga 1");
        }

        [Test]
        public void Liga3_CompraDelINivelIII_YElIVSeAbreEnLaLiga4()
        {
            List<OpcionPieza> opciones = Torso(3, 10000);

            List<OpcionPieza> compras = opciones.Where(o => o.Tipo == TipoOpcionPieza.Comprar).ToList();
            Assert.AreEqual(12, compras.Count);
            CollectionAssert.AreEqual(Enumerable.Repeat(1, 4).Concat(Enumerable.Repeat(2, 4)).Concat(Enumerable.Repeat(3, 4)),
                compras.Select(o => o.Pieza.Nivel), "De menor a mayor nivel");
            Assert.IsTrue(compras.All(o => o.PuedeElegir));
            List<OpcionPieza> cerradas = opciones.Where(o => o.Tipo == TipoOpcionPieza.Bloqueada).ToList();
            Assert.AreEqual(4, cerradas.Count);
            Assert.IsTrue(cerradas.All(o => o.Texto == "SE ABRE EN LA LIGA 4" && !o.PuedeElegir));
        }

        [Test]
        public void ConCuatrocientos_ElNivelIDiceNoAlcanza()
        {
            List<OpcionPieza> compras = Torso(1, 400).Where(o => o.Tipo == TipoOpcionPieza.Comprar).ToList();

            Assert.AreEqual(4, compras.Count);
            Assert.IsTrue(compras.All(o => !o.PuedeElegir && o.Motivo == MotivoNoElegible.NoAlcanza));
            Assert.IsTrue(Torso(1, 500).Where(o => o.Tipo == TipoOpcionPieza.Comprar).All(o => o.PuedeElegir), "Con $500 justos sí");
        }

        [Test]
        public void ConDineroNegativo_NoCompraNadaPeroSiPoneDelInventario()
        {
            DatosPieza guardada = catalogo.Buscar("pieza_torso_tech_1");
            List<OpcionPieza> opciones = Torso(2, -300, false, new[] { guardada });

            Assert.AreEqual(TipoOpcionPieza.Poner, opciones[0].Tipo, "Lo del inventario va primero");
            Assert.AreEqual("PONER", opciones[0].Texto);
            Assert.AreEqual(0, opciones[0].Precio);
            Assert.IsTrue(opciones[0].PuedeElegir, "Poner es gratis");
            Assert.IsTrue(opciones.Where(o => o.Tipo == TipoOpcionPieza.Comprar).All(o => !o.PuedeElegir
                && o.Motivo == MotivoNoElegible.NoAlcanza));
            Assert.AreEqual(1, opciones.Count(o => o.PuedeElegir));
        }

        [Test]
        public void ConElTallerBloqueado_NadaSeElige()
        {
            DatosPieza guardada = catalogo.Buscar("pieza_torso_tech_1");
            List<OpcionPieza> opciones = Torso(2, 99999, true, new[] { guardada });

            Assert.IsTrue(opciones.Count > 0);
            Assert.IsTrue(opciones.All(o => !o.PuedeElegir));
            Assert.AreEqual(MotivoNoElegible.TallerBloqueado, opciones[0].Motivo);
            Assert.IsTrue(opciones.Where(o => o.Tipo == TipoOpcionPieza.Comprar).All(o => o.Motivo == MotivoNoElegible.TallerBloqueado));
            Assert.IsTrue(opciones.Where(o => o.Tipo == TipoOpcionPieza.Bloqueada).All(o => o.Motivo == MotivoNoElegible.NivelBloqueado));
        }

        [Test]
        public void LaPiezaPuesta_NoAparece_NiEnLaTiendaNiEnElInventario()
        {
            DatosPieza puesta = catalogo.Buscar("pieza_torso_cuerpoacuerpo_1");
            List<OpcionPieza> opciones = Torso(1, 1000, false, new[] { puesta, deSerie }, puesta);

            Assert.IsFalse(opciones.Any(o => o.Pieza == puesta), "La puesta no aparece");
            Assert.AreEqual(deSerie, opciones[0].Pieza, "La de serie del inventario sí");
            Assert.AreEqual(3, opciones.Count(o => o.Tipo == TipoOpcionPieza.Comprar), "Quedan 3 ramas de nivel I");
        }

        [Test]
        public void Diferencia_TorsoCuerpoACuerpoIContraElDeSerie_SumaTreintaDeVida()
        {
            OpcionPieza opcion = Torso(1, 1000).First(o => o.Pieza.Id == "pieza_torso_cuerpoacuerpo_1");

            Assert.AreEqual(30f, opcion.DiferenciaVida, 1e-4f);
            Assert.AreEqual(opcion.Pieza.BonoDanio - deSerie.BonoDanio, opcion.DiferenciaDanio, 1e-4f);
            Assert.AreEqual(opcion.Pieza.BonoVelocidad - deSerie.BonoVelocidad, opcion.DiferenciaVelocidad, 1e-4f);
        }

        [Test]
        public void Diferencia_ContraUnaPiezaMejor_PuedeSerNegativa()
        {
            DatosPieza puesta = catalogo.Buscar("pieza_torso_cuerpoacuerpo_2");
            OpcionPieza opcion = Torso(1, 1000, false, null, puesta).First(o => o.Pieza.Id == "pieza_torso_cuerpoacuerpo_1");

            Assert.AreEqual(-30f, opcion.DiferenciaVida, 1e-4f);
        }

        [Test]
        public void LigaMenorAUno_SeTrataComoLiga1()
        {
            Assert.AreEqual(Torso(1, 1000).Count, Torso(0, 1000).Count);
        }

        [Test]
        public void SoloAparecenPiezasDeLaRanuraPedida()
        {
            DatosPieza piernas = catalogo.Buscar("pieza_piernas_tech_1");
            List<OpcionPieza> opciones = Torso(1, 1000, false, new[] { piernas });

            Assert.IsTrue(opciones.All(o => o.Pieza.Ranura == RanuraPieza.Torso));
        }
    }
}

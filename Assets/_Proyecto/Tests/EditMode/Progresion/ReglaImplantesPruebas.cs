using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>Reglas puras de los implantes (T-191): costo, estados de la opción y combinación.</summary>
    public class ReglaImplantesPruebas
    {
        private CatalogoImplantes catalogo;
        private DatosImplante ojos1;
        private DatosImplante ojos2;
        private DatosImplante brazos1;

        private static EfectosImplantes Golpe(float x)
        {
            EfectosImplantes e = EfectosImplantes.Ninguno;
            e.multiplicadorGolpeAPie = x;
            return e;
        }

        [SetUp]
        public void SetUp()
        {
            ojos1 = DatosImplante.Crear("o1", "O1", EspacioImplante.Ojos, 1, 1000, 1, "", Golpe(1.5f));
            ojos2 = DatosImplante.Crear("o2", "O2", EspacioImplante.Ojos, 2, 3000, 3, "", Golpe(2f));
            brazos1 = DatosImplante.Crear("b1", "B1", EspacioImplante.Brazos, 1, 500, 1, "", Golpe(1.2f));
            catalogo = CatalogoImplantes.Crear(ojos1, ojos2, brazos1);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(catalogo);
            Object.DestroyImmediate(ojos1);
            Object.DestroyImmediate(ojos2);
            Object.DestroyImmediate(brazos1);
        }

        [Test]
        public void Oferta_SinNada_OfreceNivel1ConPrecioCompleto()
        {
            OfertaImplante o = ReglaImplantes.Oferta(catalogo, EspacioImplante.Ojos, 0, 1, 5000);
            Assert.AreEqual(EstadoOfertaImplante.Instalar, o.Estado);
            Assert.AreSame(ojos1, o.Implante);
            Assert.AreEqual(1000, o.Costo);
        }

        [Test]
        public void Oferta_ConNivel1_CobraLaDiferenciaDelNivel2()
        {
            OfertaImplante o = ReglaImplantes.Oferta(catalogo, EspacioImplante.Ojos, 1, 3, 5000);
            Assert.AreEqual(EstadoOfertaImplante.Instalar, o.Estado);
            Assert.AreSame(ojos2, o.Implante);
            Assert.AreEqual(2000, o.Costo);
        }

        [Test]
        public void Oferta_LigaBaja_DiceLaLigaQueLoAbre()
        {
            OfertaImplante o = ReglaImplantes.Oferta(catalogo, EspacioImplante.Ojos, 1, 2, 99999);
            Assert.AreEqual(EstadoOfertaImplante.LigaBaja, o.Estado);
            Assert.AreEqual(3, o.LigaQueAbre);
        }

        [Test]
        public void Oferta_DineroCortoONegativo_EsSinDinero()
        {
            Assert.AreEqual(EstadoOfertaImplante.SinDinero, ReglaImplantes.Oferta(catalogo, EspacioImplante.Ojos, 0, 1, 999).Estado);
            Assert.AreEqual(EstadoOfertaImplante.SinDinero, ReglaImplantes.Oferta(catalogo, EspacioImplante.Ojos, 0, 1, -50).Estado);
            Assert.AreEqual(EstadoOfertaImplante.Instalar, ReglaImplantes.Oferta(catalogo, EspacioImplante.Ojos, 0, 1, 1000).Estado);
        }

        [Test]
        public void Oferta_ConNivel2_EsYaLoMejor()
        {
            OfertaImplante o = ReglaImplantes.Oferta(catalogo, EspacioImplante.Ojos, 2, 4, 99999);
            Assert.AreEqual(EstadoOfertaImplante.YaLoMejor, o.Estado);
            Assert.IsNull(o.Implante);
        }

        [Test]
        public void Combinar_DeCeroUnoYCuatro_MultiplicaLosEfectos()
        {
            Assert.AreEqual(1f, ReglaImplantes.Combinar(new DatosImplante[0]).multiplicadorGolpeAPie);
            Assert.AreEqual(1.5f, ReglaImplantes.Combinar(new[] { ojos1 }).multiplicadorGolpeAPie, 0.0001f);
            Assert.AreEqual(1f, ReglaImplantes.Combinar(null).multiplicadorGolpeAPie);

            var a = DatosImplante.Crear("a", "A", EspacioImplante.Piernas, 1, 1, 1, "", Golpe(2f));
            var b = DatosImplante.Crear("b", "B", EspacioImplante.SistemaNervioso, 1, 1, 1, "", Golpe(3f));
            try
            {
                float total = ReglaImplantes.Combinar(new[] { ojos1, brazos1, a, b }).multiplicadorGolpeAPie;
                Assert.AreEqual(1.5f * 1.2f * 2f * 3f, total, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
            }
        }
    }
}

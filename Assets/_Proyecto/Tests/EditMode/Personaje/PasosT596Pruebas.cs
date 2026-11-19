using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-596: los pasos caen por distancia recorrida (zancada) y el sonido depende del piso.</summary>
    public class PasosT596Pruebas
    {
        [Test]
        public void Zancada_CaminandoDiezMetros_DaLosPasosQueTocan()
        {
            ReglaZancada z = new ReglaZancada(1.4f, 2f);
            int total = 0;
            for (int i = 0; i < 100; i++) total += z.Avanzar(0.1f, false, true);
            Assert.AreEqual(7, total, "10 m / 1.4 m = 7 pasos");
        }

        [Test]
        public void Zancada_CorriendoSuenanMenosPasosPorMetroPeroMasPorSegundo()
        {
            ReglaZancada camina = new ReglaZancada(1.4f, 2f);
            ReglaZancada corre = new ReglaZancada(1.4f, 2f);
            int pasosCamina = 0, pasosCorre = 0;
            for (int i = 0; i < 1000; i++)
            {
                pasosCamina += camina.Avanzar(4.2f * 0.01f, false, true);
                pasosCorre += corre.Avanzar(7f * 0.01f, true, true);
            }
            Assert.Greater(pasosCorre, pasosCamina, "En 10 s a mas velocidad caen mas pasos.");
        }

        [Test]
        public void Zancada_QuietoOEnElAire_NoDaPasosYAlternaLosPies()
        {
            ReglaZancada z = new ReglaZancada(1f, 2f);
            Assert.AreEqual(0, z.Avanzar(0f, false, true));
            Assert.AreEqual(0, z.Avanzar(5f, false, false));
            z.Avanzar(1f, false, true);
            bool primero = z.UltimoFueDerecho;
            z.Avanzar(1f, false, true);
            Assert.AreNotEqual(primero, z.UltimoFueDerecho);
            Assert.AreEqual(2, z.Pasos);
        }

        [Test]
        public void Zancada_UnPasoGrandeEnUnFrame_DaVariosPasos()
        {
            ReglaZancada z = new ReglaZancada(1f, 2f);
            Assert.AreEqual(3, z.Avanzar(3.2f, false, true));
        }

        [TestCase(IdLugar.Calle, "Asfalto_Seco", false, "PAS_CONCRETO")]
        [TestCase(IdLugar.Calle, null, false, "PAS_CONCRETO")]
        [TestCase(IdLugar.Calle, "Asfalto_Mojado", false, "PAS_MOJADO")]
        [TestCase(IdLugar.Calle, "Charco", false, "PAS_MOJADO")]
        [TestCase(IdLugar.Calle, "Rejilla_Metal", false, "PAS_METAL")]
        [TestCase(IdLugar.Calle, "Pintura", true, "PAS_METAL")]
        [TestCase(IdLugar.Casa, "Asfalto", false, "PAS_MADERA")]
        [TestCase(IdLugar.Clinica, "Metal", false, "PAS_MADERA")]
        [TestCase(IdLugar.Bar, null, false, "PAS_MADERA")]
        public void Superficie_ElIdDependeDelLugarYDelMaterial(IdLugar lugar, string piso, bool montable, string esperado)
        {
            Assert.AreEqual(esperado, ReglaSuperficiePaso.IdPaso(lugar, piso, montable));
        }
    }
}

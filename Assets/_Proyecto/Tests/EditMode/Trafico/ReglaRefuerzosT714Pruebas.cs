using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-714: cuándo los SKULLS llaman refuerzos: tres vencidos, uno a la vez y con enfriamiento.</summary>
    public class ReglaRefuerzosT714Pruebas
    {
        [Test]
        public void ConMenosDeTresVencidos_NoLlama()
        {
            ReglaRefuerzos r = new ReglaRefuerzos();
            r.RegistrarMuertes(2);
            Assert.IsFalse(r.DebeLlamar(100f, 0));
        }

        [Test]
        public void ConTresVencidos_Llama_YVaciaLaCuenta()
        {
            ReglaRefuerzos r = new ReglaRefuerzos();
            r.RegistrarMuertes(3);
            Assert.IsTrue(r.DebeLlamar(100f, 0));
            r.Llamado(100f);
            Assert.AreEqual(0, r.Muertes);
            Assert.IsFalse(r.DebeLlamar(100f, 0), "Sin vencidos nuevos no llama otra vez");
        }

        [Test]
        public void MientrasOtroCarroVaEnCamino_NoLlama()
        {
            ReglaRefuerzos r = new ReglaRefuerzos(3, 0f, 1);
            r.RegistrarMuertes(5);
            Assert.IsFalse(r.DebeLlamar(100f, 1), "Un carro a la vez");
            Assert.IsTrue(r.DebeLlamar(100f, 0));
        }

        [Test]
        public void ConEnfriamiento_NoLlamaAntesDeTiempo()
        {
            ReglaRefuerzos r = new ReglaRefuerzos(3, 60f, 1);
            r.RegistrarMuertes(3);
            r.Llamado(100f);
            r.RegistrarMuertes(3);
            Assert.IsFalse(r.DebeLlamar(120f, 0), "A los 20 s todavía no");
            Assert.IsTrue(r.DebeLlamar(161f, 0), "Pasado el enfriamiento sí");
        }

        [Test]
        public void MuertesNegativasOCero_NoCuentan()
        {
            ReglaRefuerzos r = new ReglaRefuerzos();
            r.RegistrarMuertes(0);
            r.RegistrarMuertes(-4);
            Assert.AreEqual(0, r.Muertes);
        }
    }
}

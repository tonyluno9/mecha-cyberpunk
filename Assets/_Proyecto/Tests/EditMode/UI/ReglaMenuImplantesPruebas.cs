using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Clinica;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-815: la tabla antes/después del menú de implantes y las regiones del cuerpo del holograma.</summary>
    public class ReglaMenuImplantesPruebas
    {
        private static EfectosImplantes Brazos1()
        {
            EfectosImplantes e = EfectosImplantes.Ninguno;
            e.multiplicadorGolpeAPie = 1.5f;
            e.reduccionRetroceso = 0.3f;
            return e;
        }

        [Test]
        public void Comparar_SinImplanteContraBrazos_SoloGolpeYRetroceso()
        {
            var filas = new List<FilaEfectoImplante>();
            ReglaMenuImplantes.Comparar(EfectosImplantes.Ninguno, Brazos1(), filas);

            Assert.AreEqual(2, filas.Count, "Solo los efectos que toca el implante");
            Assert.AreEqual(EfectoImplante.GolpeAPie, filas[0].Efecto);
            Assert.AreEqual(1f, filas[0].Antes);
            Assert.AreEqual(1.5f, filas[0].Despues);
            Assert.AreEqual(1, filas[0].Cambio);
            Assert.AreEqual(EfectoImplante.Retroceso, filas[1].Efecto);
            Assert.AreEqual(1, filas[1].Cambio, "Más reducción de retroceso es mejor");
        }

        [Test]
        public void Comparar_SinapsisMenorEspera_EsMejora()
        {
            EfectosImplantes s = EfectosImplantes.Ninguno;
            s.multiplicadorReaccionMecha = 0.85f;
            s.reduccionRecarga = 0.2f;
            var filas = new List<FilaEfectoImplante>();
            ReglaMenuImplantes.Comparar(EfectosImplantes.Ninguno, s, filas);
            Assert.AreEqual(2, filas.Count);
            Assert.AreEqual(EfectoImplante.ReaccionMecha, filas[0].Efecto);
            Assert.AreEqual(1, filas[0].Cambio, "Menos espera en el mecha es mejor");
            Assert.AreEqual("-15%", ReglaMenuImplantes.TextoDiferencia(EfectoImplante.ReaccionMecha, 1f, 0.85f));
        }

        [Test]
        public void Comparar_LoMismoInstalado_SinCambio()
        {
            var filas = new List<FilaEfectoImplante>();
            ReglaMenuImplantes.Comparar(Brazos1(), Brazos1(), filas);
            Assert.AreEqual(2, filas.Count, "Se ven los efectos de lo instalado");
            Assert.AreEqual(0, filas[0].Cambio);
            Assert.AreEqual(string.Empty, ReglaMenuImplantes.TextoDiferencia(filas[0].Efecto, filas[0].Antes, filas[0].Despues));
        }

        [Test]
        public void Texto_CadaTipo_SeLeeComoEnLaTabla()
        {
            Assert.AreEqual("150%", ReglaMenuImplantes.Texto(EfectoImplante.GolpeAPie, 1.5f, "SÍ", "NO"));
            Assert.AreEqual("-30%", ReglaMenuImplantes.Texto(EfectoImplante.Retroceso, 0.3f, "SÍ", "NO"));
            Assert.AreEqual("+25%", ReglaMenuImplantes.Texto(EfectoImplante.AlcanceApuntado, 0.25f, "SÍ", "NO"));
            Assert.AreEqual("0.4 s", ReglaMenuImplantes.Texto(EfectoImplante.AnticipacionAtaque, 0.4f, "SÍ", "NO"));
            Assert.AreEqual("SÍ", ReglaMenuImplantes.Texto(EfectoImplante.VerVidaRival, 1f, "SÍ", "NO"));
            Assert.AreEqual("NO", ReglaMenuImplantes.Texto(EfectoImplante.VidaEnNumeros, 0f, "SÍ", "NO"));
            Assert.AreEqual("+50%", ReglaMenuImplantes.TextoDiferencia(EfectoImplante.GolpeAPie, 1f, 1.5f));
            Assert.AreEqual("-30%", ReglaMenuImplantes.TextoDiferencia(EfectoImplante.Retroceso, 0f, 0.3f));
            Assert.AreEqual("+0.4 s", ReglaMenuImplantes.TextoDiferencia(EfectoImplante.AnticipacionAtaque, 0f, 0.4f));
            Assert.AreEqual(string.Empty, ReglaMenuImplantes.TextoDiferencia(EfectoImplante.VerVidaRival, 0f, 1f));
        }

        [TestCase(0.95f, 0f, EspacioImplante.Ojos)]
        [TestCase(0.7f, 0.3f, EspacioImplante.Brazos)]
        [TestCase(0.7f, -0.2f, EspacioImplante.Brazos)]
        [TestCase(0.7f, 0.05f, EspacioImplante.SistemaNervioso)]
        [TestCase(0.2f, 0.06f, EspacioImplante.Piernas)]
        public void Region_PuntoDelCuerpo_EsSuEspacio(float altura, float lado, EspacioImplante esperado)
        {
            Assert.AreEqual(esperado, ReglaMenuImplantes.Region(altura, lado));
        }
    }
}

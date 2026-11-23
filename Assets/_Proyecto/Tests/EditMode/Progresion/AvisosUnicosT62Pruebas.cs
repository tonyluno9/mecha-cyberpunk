using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.Progresion.Recorrido;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-62 y T-59: los avisos del tutorial salen una sola vez y se guardan; la historia ya no pasa por el mercado ni por la cinemática del Inventor.</summary>
    public class AvisosUnicosT62Pruebas
    {
        [SetUp]
        public void Preparar() => AvisosUnicos.Reiniciar();

        [TearDown]
        public void Limpiar() => AvisosUnicos.Reiniciar();

        [Test]
        public void Marcar_LaPrimeraVezEsNuevo_LaSegundaNo()
        {
            Assert.IsFalse(AvisosUnicos.YaVisto(AvisosUnicos.Protesis));
            Assert.IsTrue(AvisosUnicos.Marcar(AvisosUnicos.Protesis));
            Assert.IsTrue(AvisosUnicos.YaVisto(AvisosUnicos.Protesis));
            Assert.IsFalse(AvisosUnicos.Marcar(AvisosUnicos.Protesis), "no vuelve a salir");
        }

        [Test]
        public void SerializarYCargar_ConservaLoVisto()
        {
            AvisosUnicos.Marcar(AvisosUnicos.Rueda);
            AvisosUnicos.Marcar(AvisosUnicos.Protesis);
            string texto = AvisosUnicos.Serializar();
            AvisosUnicos.Reiniciar();
            Assert.IsFalse(AvisosUnicos.YaVisto(AvisosUnicos.Rueda));
            AvisosUnicos.Cargar(texto);
            Assert.IsTrue(AvisosUnicos.YaVisto(AvisosUnicos.Rueda));
            Assert.IsTrue(AvisosUnicos.YaVisto(AvisosUnicos.Protesis));
            Assert.IsFalse(AvisosUnicos.YaVisto(AvisosUnicos.Maquina));
        }

        [Test]
        public void Cargar_NuloOVacio_DejaTodoSinVer()
        {
            AvisosUnicos.Marcar(AvisosUnicos.Maquina);
            AvisosUnicos.Cargar(null);
            Assert.IsFalse(AvisosUnicos.YaVisto(AvisosUnicos.Maquina));
        }

        [Test]
        public void Marcar_AvisaParaGuardar()
        {
            int avisos = 0;
            System.Action cuenta = () => avisos++;
            AvisosUnicos.Cambio += cuenta;
            AvisosUnicos.Marcar(AvisosUnicos.Rueda);
            AvisosUnicos.Marcar(AvisosUnicos.Rueda);
            AvisosUnicos.Cambio -= cuenta;
            Assert.AreEqual(1, avisos);
        }

        [Test]
        public void ElObjetivoDeTinoCabeEnElHud()
        {
            foreach (string objetivo in TablaRecorrido.Objetivos())
            {
                Assert.LessOrEqual(objetivo.Length, 32, objetivo);
            }
            StringAssert.Contains("TINO", TablaRecorrido.ObjetivoInventor);
        }
    }
}

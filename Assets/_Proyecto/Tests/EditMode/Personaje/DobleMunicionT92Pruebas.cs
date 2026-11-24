using MechaCyberpunk.Personaje;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T92: cada arma lleva el doble de balas que antes. Las cifras "antes" son las de antes de T92; los assets reales deben traer el doble
    /// (los puños y la granada de mano no tienen cargador de balas y siguen en 1).
    /// </summary>
    public class DobleMunicionT92Pruebas
    {
        private const string CarpetaArmas = "Assets/_Proyecto/Personaje/Datos/Armas/";
        private const string CarpetaBalas = "Assets/_Proyecto/Economia/Datos/";
        private const int Doble = 2;

        // Cargador de los niveles 0 a 3 antes de T92.
        [TestCase("pistola", 8, 10, 12, 15)]
        [TestCase("escopeta", 4, 5, 6, 8)]
        [TestCase("fusil", 30, 30, 35, 40)]
        [TestCase("sniper", 4, 5, 6, 7)]
        [TestCase("subfusil", 25, 30, 35, 40)]
        [TestCase("revolver", 6, 6, 6, 6)]
        [TestCase("lanzagranadas", 6, 6, 6, 6)]
        [TestCase("rifleplasma", 3, 3, 4, 5)]
        public void Cargador_DeCadaNivel_EsElDoble(string clave, int n0, int n1, int n2, int n3)
        {
            int[] antes = { n0, n1, n2, n3 };
            for (int nivel = 0; nivel < antes.Length; nivel++)
            {
                DatosArma arma = AssetDatabase.LoadAssetAtPath<DatosArma>(CarpetaArmas + "DatosArma_" + clave + "_" + nivel + ".asset");
                Assert.IsNotNull(arma, clave + " nivel " + nivel);
                Assert.AreEqual(antes[nivel] * Doble, arma.Cargador, clave + " nivel " + nivel + ": cargador al doble");
            }
        }

        [TestCase("punos")]
        [TestCase("granada")]
        public void Cargador_SinBalas_SigueEnUno(string clave)
        {
            for (int nivel = 0; nivel < 4; nivel++)
            {
                DatosArma arma = AssetDatabase.LoadAssetAtPath<DatosArma>(CarpetaArmas + "DatosArma_" + clave + "_" + nivel + ".asset");
                Assert.IsNotNull(arma, clave + " nivel " + nivel);
                Assert.AreEqual(1, arma.Cargador, clave + " nivel " + nivel);
            }
        }

        // Paquete de compra (y de drop), balas iniciales y tope de reserva antes de T92.
        [TestCase("escopeta", 8, 16, 40)]
        [TestCase("fusil", 60, 60, 240)]
        [TestCase("sniper", 10, 20, 40)]
        [TestCase("subfusil", 60, 60, 240)]
        [TestCase("revolver", 12, 24, 60)]
        [TestCase("lanzagranadas", 6, 6, 18)]
        [TestCase("granada", 3, 3, 10)]
        [TestCase("rifleplasma", 3, 3, 9)]
        public void Reserva_PaqueteInicialesYTope_SonElDoble(string clave, int paquete, int iniciales, int maximo)
        {
            DatosMunicionArma balas = AssetDatabase.LoadAssetAtPath<DatosMunicionArma>(CarpetaBalas + "DatosMunicionArma_" + clave + ".asset");
            Assert.IsNotNull(balas, clave);
            Assert.AreEqual(paquete * Doble, balas.BalasPorPaquete, clave + ": paquete de compra y drop al doble");
            Assert.AreEqual(iniciales * Doble, balas.BalasIniciales, clave + ": balas iniciales al doble");
            Assert.AreEqual(maximo * Doble, balas.Maximo, clave + ": tope de reserva al doble");
            Assert.IsNotNull(balas.Oferta, clave + " tiene oferta");
            Assert.AreEqual(balas.BalasPorPaquete + " balas.", balas.Oferta.Descripcion, clave + ": la oferta dice las balas nuevas");
        }

        [Test]
        public void PistolaBase_CargadorDe16()
        {
            DatosPistola pistola = AssetDatabase.LoadAssetAtPath<DatosPistola>("Assets/_Proyecto/Personaje/Datos/DatosPistola_Base.asset");
            Assert.IsNotNull(pistola);
            Assert.AreEqual(8 * Doble, pistola.BalasPorCargador);
        }
    }
}

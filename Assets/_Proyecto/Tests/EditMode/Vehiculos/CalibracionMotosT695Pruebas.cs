using MechaCyberpunk.Vehiculos;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-695: los números de las motos. La de reparto es más lenta que la normal y las dos frenan casi de golpe.</summary>
    public class CalibracionMotosT695Pruebas
    {
        private static ConfigCarro Cargar(string nombre) => AssetDatabase.LoadAssetAtPath<ConfigCarro>("Assets/_Proyecto/Vehiculos/Datos/" + nombre + ".asset");

        [Test]
        public void MotoDeReparto_EsMasLentaQueLaMoto()
        {
            ConfigCarro moto = Cargar("ConfigCarro_Moto");
            ConfigCarro reparto = Cargar("ConfigCarro_MotoReparto");
            Assert.IsNotNull(moto);
            Assert.IsNotNull(reparto);
            Assert.Less(reparto.Parametros.FuerzaMotor / reparto.Masa, moto.Parametros.FuerzaMotor / moto.Masa, "Acelera menos");
            Assert.Less(reparto.VelocidadMaxima, moto.VelocidadMaxima, "Tope más bajo");
        }

        [Test]
        public void Moto_ArrancaViva_PeroNoPasada()
        {
            ConfigCarro moto = Cargar("ConfigCarro_Moto");
            float aceleracion = moto.Parametros.FuerzaMotor / moto.Masa;
            Assert.That(aceleracion, Is.InRange(6f, 9f), "m/s² con la moto parada (antes 11)");
        }

        [Test]
        public void Motos_FrenanCasiDeGolpe()
        {
            foreach (string nombre in new[] { "ConfigCarro_Moto", "ConfigCarro_MotoReparto" })
            {
                ConfigCarro c = Cargar(nombre);
                Assert.GreaterOrEqual(c.Parametros.FuerzaFreno / c.Masa, 25f, nombre + ": el freno para a más de 25 m/s²");
            }
        }
    }
}

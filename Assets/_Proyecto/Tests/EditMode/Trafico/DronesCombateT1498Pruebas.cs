using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-1498: qué variante de dron sale con cada cantidad de estrellas.</summary>
    public class DronesCombateT1498Pruebas
    {
        // DR01 (1), DR04 (2), DR02 (4), E13 (5), como en CrearDronesCombateT1498.
        private static readonly int[] Minimos = { 1, 2, 4, 5 };

        [Test]
        public void Elegir_UnaEstrella_SoloElDronDeLaPolicia()
        {
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(0, ReglaVarianteDron.Elegir(Minimos, 1, i), "dron " + i);
            }
        }

        [Test]
        public void Elegir_DosEstrellas_ElExploradorVaPrimeroYLuegoAlternaConLaPolicia()
        {
            Assert.AreEqual(1, ReglaVarianteDron.Elegir(Minimos, 2, 0));
            Assert.AreEqual(0, ReglaVarianteDron.Elegir(Minimos, 2, 1));
            Assert.AreEqual(1, ReglaVarianteDron.Elegir(Minimos, 2, 2));
        }

        [Test]
        public void Elegir_CuatroEstrellas_ElPesadoPrimero()
        {
            Assert.AreEqual(2, ReglaVarianteDron.Elegir(Minimos, 4, 0));
            Assert.AreEqual(1, ReglaVarianteDron.Elegir(Minimos, 4, 1));
            Assert.AreEqual(0, ReglaVarianteDron.Elegir(Minimos, 4, 2));
        }

        [Test]
        public void Elegir_CincoEstrellas_ElDeAsaltoPrimero_YNoSaleBajoCinco()
        {
            Assert.AreEqual(3, ReglaVarianteDron.Elegir(Minimos, 5, 0));
            Assert.AreEqual(2, ReglaVarianteDron.Elegir(Minimos, 5, 1));
            for (int estrellas = 1; estrellas <= 4; estrellas++)
            {
                for (int i = 0; i < 8; i++)
                {
                    Assert.AreNotEqual(3, ReglaVarianteDron.Elegir(Minimos, estrellas, i), "E13 con " + estrellas + " estrellas");
                }
            }
        }

        [Test]
        public void Elegir_SinEstrellasONulo_NoHayVariante()
        {
            Assert.AreEqual(-1, ReglaVarianteDron.Elegir(Minimos, 0, 0));
            Assert.AreEqual(-1, ReglaVarianteDron.Elegir(null, 3, 0));
            Assert.AreEqual(-1, ReglaVarianteDron.Elegir(new int[0], 3, 0));
        }

        [Test]
        public void Elegir_IndiceNegativo_UsaElPrimero()
        {
            Assert.AreEqual(3, ReglaVarianteDron.Elegir(Minimos, 5, -4));
        }

        [Test]
        public void Config_ElegirDevuelveLaVarianteDeLosDatos()
        {
            ConfigDronesCombate config = ScriptableObject.CreateInstance<ConfigDronesCombate>();
            try
            {
                config.Configurar(new[]
                {
                    new VarianteDronCombate("A", null, 1, 0f, 0f, 0f, 0f, 0f, 1f, 0f),
                    new VarianteDronCombate("B", null, 4, 90f, 9f, 2f, 0.7f, 6f, 1f, 1f),
                });
                Assert.AreEqual("A", config.Elegir(2, 0).Nombre);
                Assert.AreEqual("B", config.Elegir(4, 0).Nombre);
                Assert.AreEqual(9f, config.Elegir(5, 0).Danio, 0.0001f);
                Assert.IsNull(config.Elegir(0, 0));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}

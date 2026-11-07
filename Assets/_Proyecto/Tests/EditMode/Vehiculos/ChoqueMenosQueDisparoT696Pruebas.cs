using MechaCyberpunk.Vehiculos.Datos;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-696: un choque, por fuerte que sea, quita menos que un disparo de pistola (25).</summary>
    public class ChoqueMenosQueDisparoT696Pruebas
    {
        private const float DisparoPistola = 25f;

        [Test]
        public void Choque_NuncaPasaDeUnDisparo()
        {
            ConfigDanioCarro c = ScriptableObject.CreateInstance<ConfigDanioCarro>();
            for (float v = 0f; v <= 44f; v += 1f)
            {
                Assert.Less(DanioCarro.DanioDeChoque(v, c.UmbralChoque, c.DanioPorChoque, c.VidaMaxima, c.DanioMaximoPorChoque), DisparoPistola, "a " + v + " m/s");
            }
            Object.DestroyImmediate(c);
        }

        [Test]
        public void ChoqueSuave_NoHaceDanio()
        {
            ConfigDanioCarro c = ScriptableObject.CreateInstance<ConfigDanioCarro>();
            Assert.AreEqual(0f, DanioCarro.DanioDeChoque(5f, c.UmbralChoque, c.DanioPorChoque, c.VidaMaxima, c.DanioMaximoPorChoque));
            Object.DestroyImmediate(c);
        }
    }
}

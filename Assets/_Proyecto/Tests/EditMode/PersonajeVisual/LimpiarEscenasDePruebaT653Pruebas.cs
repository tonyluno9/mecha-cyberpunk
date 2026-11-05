using System;
using System.IO;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>T-653: limpieza de InitTestScene abandonadas.</summary>
    public class LimpiarEscenasDePruebaT653Pruebas
    {
        private string carpeta;

        [SetUp]
        public void SetUp()
        {
            carpeta = Path.Combine(Path.GetTempPath(), "t653_" + Guid.NewGuid());
            Directory.CreateDirectory(carpeta);
        }

        [TearDown]
        public void TearDown() { Directory.Delete(carpeta, true); }

        [Test]
        public void Limpiar_EscenaVieja_SeBorraConSuMeta()
        {
            string ruta = Path.Combine(carpeta, "InitTestScenea1.unity");
            File.WriteAllText(ruta, "x"); File.WriteAllText(ruta + ".meta", "x");
            File.SetLastWriteTimeUtc(ruta, DateTime.UtcNow.AddHours(-3));
            Assert.AreEqual(1, LimpiarEscenasDePrueba.Limpiar(carpeta, DateTime.UtcNow));
            Assert.IsFalse(File.Exists(ruta)); Assert.IsFalse(File.Exists(ruta + ".meta"));
        }

        [Test]
        public void Limpiar_EscenaReciente_NoSeToca()
        {
            string ruta = Path.Combine(carpeta, "InitTestSceneb2.unity");
            File.WriteAllText(ruta, "x");
            Assert.AreEqual(0, LimpiarEscenasDePrueba.Limpiar(carpeta, DateTime.UtcNow));
            Assert.IsTrue(File.Exists(ruta));
        }

        [Test]
        public void Limpiar_OtraEscena_NoSeToca()
        {
            string ruta = Path.Combine(carpeta, "Pruebas_Tony.unity");
            File.WriteAllText(ruta, "x"); File.SetLastWriteTimeUtc(ruta, DateTime.UtcNow.AddDays(-3));
            Assert.AreEqual(0, LimpiarEscenasDePrueba.Limpiar(carpeta, DateTime.UtcNow));
            Assert.IsTrue(File.Exists(ruta));
        }
    }
}

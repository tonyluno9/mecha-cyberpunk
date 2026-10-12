using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Personaje
{
    public class PersonajeSinPeleadorPruebas
    {
        [Test]
        public void Ensamblado_NingunTipoImplementaIPeleador()
        {
            var tipos = typeof(DatosPersonaje).Assembly.GetTypes();
            Assert.Greater(tipos.Length, 5, "Debe leer los tipos del ensamblado Personaje");

            string[] culpables = tipos.Where(t => typeof(IPeleador).IsAssignableFrom(t)).Select(t => t.FullName).ToArray();

            Assert.IsEmpty(culpables, "DirectorPelea y RivalBasico confundirían a la persona con el mecha: " + string.Join(", ", culpables));
        }
    }
}

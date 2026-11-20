using MechaCyberpunk.Core;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-1528: el marcador de drones usa el indicador de daño y solo con el visor puesto.</summary>
    public class IndicadorDanioDronT1528Pruebas
    {
        [TearDown]
        public void Limpiar() => VisorJugador.ModoActivo = false;

        [Test]
        public void SinImplantesRegistrados_NoHayVisor_AunqueElModoEsteActivo()
        {
            VisorJugador.ModoActivo = true;
            Assert.IsFalse(VisorJugador.Puesto, "sin IImplantes (sin implante de ojos) no hay visor");
        }

        [Test]
        public void MarcarFuente_AvisaALosOyentes()
        {
            UnityEngine.Vector3? recibido = null;
            System.Action<UnityEngine.Vector3> oyente = v => recibido = v;
            EventosIndicadorDanio.FuenteMarcada += oyente;
            EventosIndicadorDanio.MarcarFuente(new UnityEngine.Vector3(1f, 2f, 3f));
            EventosIndicadorDanio.FuenteMarcada -= oyente;
            Assert.AreEqual(new UnityEngine.Vector3(1f, 2f, 3f), recibido);
        }
    }
}

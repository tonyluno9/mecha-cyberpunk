using System.Linq;
using MechaCyberpunk.Arte.Editor;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-995: Ciudad_Prologo no tiene piezas del kit encimadas ni duplicadas, ni renderers con textura o material roto.</summary>
    public class RevisarCiudadT995Pruebas
    {
        [Test]
        public void Ciudad_SinPiezasEncimadas()
        {
            CollectionAssert.IsEmpty(RevisarCiudadT995.Encimados().Select(e => e.ToString()).ToArray());
        }

        [Test]
        public void Ciudad_SinTexturasRotas()
        {
            CollectionAssert.IsEmpty(RevisarCiudadT995.TexturasRotas().Select(r => r.ToString()).ToArray());
        }
    }
}

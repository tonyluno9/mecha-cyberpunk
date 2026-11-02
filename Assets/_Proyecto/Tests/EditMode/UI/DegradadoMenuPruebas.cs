using MechaCyberpunk.UI.Kit;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-524: el fondo oscuro del menú es un degradado suave (sin corte vertical).</summary>
    public class DegradadoMenuPruebas
    {
        [Test]
        public void Degradado_EmpiezaOpacoYTerminaTransparente()
        {
            Texture2D t = KitUI.SpriteDegradadoHorizontal().texture;
            Assert.GreaterOrEqual(t.GetPixel(0, 0).a, 0.99f, "Opaco a la izquierda");
            Assert.LessOrEqual(t.GetPixel(t.width - 1, 0).a, 0.01f, "Transparente a la derecha");
        }

        [Test]
        public void Degradado_BajaSinSaltos()
        {
            Texture2D t = KitUI.SpriteDegradadoHorizontal().texture;
            float anterior = 1f;
            for (int x = 0; x < t.width; x++)
            {
                float a = t.GetPixel(x, 0).a;
                Assert.LessOrEqual(a, anterior + 0.001f, "Nunca sube (x=" + x + ")");
                Assert.LessOrEqual(anterior - a, 0.03f, "Sin escalones (x=" + x + ")");
                anterior = a;
            }
        }
    }
}

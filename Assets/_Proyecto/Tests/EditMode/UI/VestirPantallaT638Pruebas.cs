using MechaCyberpunk.UI.Kit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-638: las pantallas viejas (tienda, teléfono, taller...) se visten con el marco, la fuente y el contorno del kit.</summary>
    public class VestirPantallaT638Pruebas
    {
        [Test]
        public void VestirPantalla_PoneMarcoFuenteYContorno_ConservandoLaOpacidad()
        {
            var canvasGo = new GameObject("CanvasPrueba", typeof(Canvas));
            try
            {
                var fondo = new GameObject("Fondo", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                fondo.transform.SetParent(canvasGo.transform, false);
                var grande = new GameObject("Titulo", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                grande.transform.SetParent(canvasGo.transform, false);
                grande.fontSize = 60;
                var chico = new GameObject("Texto", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                chico.transform.SetParent(canvasGo.transform, false);
                chico.fontSize = 26;

                KitUI.VestirPantalla(fondo, new Color(0.04f, 0.04f, 0.07f, 0.9f));
                KitUI.VestirPantalla(fondo, new Color(0.04f, 0.04f, 0.07f, 0.9f)); // idempotente

                Assert.IsNotNull(PiezasUI.Actual.MarcoPanel);
                Assert.AreEqual(PiezasUI.Actual.MarcoPanel, fondo.sprite);
                Assert.AreEqual(Image.Type.Sliced, fondo.type);
                Assert.AreEqual(0.9f, fondo.color.a, 0.001f);
                Assert.AreEqual(1f, fondo.color.r, 0.001f, "El marco no se tiñe de oscuro");
                Assert.AreSame(KitUI.Estilo.FuenteDe(RolTexto.Encabezado), grande.font);
                Assert.AreSame(KitUI.Estilo.FuenteDe(RolTexto.Texto), chico.font);
                Assert.IsNotNull(chico.GetComponent<Outline>());
                Assert.AreEqual(1, chico.GetComponents<Shadow>().Length, "Un solo contorno");
            }
            finally
            {
                Object.DestroyImmediate(canvasGo);
            }
        }
    }
}

using System.IO;
using MechaCyberpunk.UI.Inventario;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1474: los IDs nuevos de UI, HUD, tienda e interacción están pedidos donde toca (comprobación del código fuente).</summary>
    public class InterfazSonidoT1474Pruebas
    {
        [Test]
        public void Inventario_TieneSonidosDePestanaYEquipo()
        {
            Assert.AreEqual("UI_PESTANA", PantallaInventario.SonidoPestana);
            Assert.AreEqual("TIE_EQUIPAR", PantallaInventario.SonidoEquipar);
            Assert.AreEqual("TIE_DESEQUIPAR", PantallaInventario.SonidoDesequipar);
        }

        [TestCase("Assets/_Proyecto/UI/Pausa/PantallaPausa.cs", "UI_MENU_ABRIR")]
        [TestCase("Assets/_Proyecto/UI/Pausa/PantallaPausa.cs", "UI_MENU_CERRAR")]
        [TestCase("Assets/_Proyecto/UI/Misiones/PantallaTelefono.cs", "UI_PESTANA")]
        [TestCase("Assets/_Proyecto/UI/Misiones/PantallaTelefono.cs", "UI_MENU_ABRIR")]
        [TestCase("Assets/_Proyecto/UI/Tiendas/PantallaTienda.cs", "UI_ERROR")]
        [TestCase("Assets/_Proyecto/Personaje/Vida/VidaAPie.cs", "HUD_VIDA_BAJA")]
        [TestCase("Assets/_Proyecto/Personaje/Combate/DineroEnPiso.cs", "INT_RECOGER_OBJETO")]
        [TestCase("Assets/_Proyecto/Progresion/Prologo/Pandillas/MaletinAsalto.cs", "INT_MALETIN_CERRAR")]
        [TestCase("Assets/_Proyecto/Progresion/Prologo/Cuartos/PuertaCuarto.cs", "INT_PUERTA_MADERA")]
        public void Codigo_PideElIdNuevo(string ruta, string id)
        {
            StringAssert.Contains("\"" + id + "\"", File.ReadAllText(ruta));
        }
    }
}

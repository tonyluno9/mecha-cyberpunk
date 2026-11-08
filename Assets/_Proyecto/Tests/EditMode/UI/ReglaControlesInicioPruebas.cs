using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Inicio;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-726: el texto de Opciones > Controles sale de las teclas que da el Input System y el letrero de saltar de la acción real.</summary>
    public class ReglaControlesInicioPruebas
    {
        [Test]
        public void Texto_UsaLasTeclasQueDaElInputSystemEnMayusculas()
        {
            string texto = ReglaControlesInicio.Texto(accion => accion == ReglaControlesInicio.Saltar ? "Z" : accion == ReglaControlesInicio.Recargar ? "g" : null);
            StringAssert.Contains("SALTAR  Z", texto);
            StringAssert.Contains("RECARGAR  G", texto);
            StringAssert.DoesNotContain("ESPACIO", texto, "No queda la tecla fija si el Input System dio otra");
        }

        [Test]
        public void Texto_SinTeclasUsaElRespaldo()
        {
            string texto = ReglaControlesInicio.Texto(accion => null);
            StringAssert.Contains("MOVER  W A S D", texto);
            StringAssert.Contains("SALTAR  ESPACIO", texto);
            StringAssert.Contains("ARMAS  1 A 5 O RUEDA", texto);
            StringAssert.Contains("PAUSA  ESC", texto);
        }

        [Test]
        public void Texto_TieneLasTresFilas()
        {
            string[] filas = ReglaControlesInicio.Texto(accion => "X").Split('\n');
            Assert.AreEqual(3, filas.Length);
            StringAssert.StartsWith("MOVER", filas[0]);
            StringAssert.StartsWith("INTERACTUAR", filas[1]);
            StringAssert.StartsWith("RECARGAR", filas[2]);
        }

        [Test]
        public void ControlesHud_ElAssetTrae_LasAccionesRealesDelJugadorYLaDeSaltar()
        {
            ControlesHud controles = ControlesHud.Actual;
            Assert.IsNotNull(controles, "UI/Resources/ControlesHud.asset");
            Assert.IsNotNull(controles.AccionAPie("Recargar"));
            Assert.AreEqual("R", TeclasHud.Texto(controles.AccionAPie("Recargar")));
            Assert.AreEqual("Espacio", TeclasHud.Texto(controles.AccionAPie("Saltar")));
            Assert.AreEqual("Enter", TeclasHud.Texto(controles.AccionInterfaz(ControlesHud.AccionSaltarCinematica)), "Enter salta la cinemática");
        }

        [Test]
        public void TextoSaltar_DaLaTeclaRealDeLaAccion()
        {
            Assert.AreEqual("ENTER: SALTAR", HudCinematica.TextoSaltar());
        }
    }
}

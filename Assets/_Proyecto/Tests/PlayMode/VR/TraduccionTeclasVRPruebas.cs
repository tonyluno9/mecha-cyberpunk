using MechaCyberpunk.VR;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>VR28, lógica pura: la tabla central cambia las teclas de PC de cualquier texto por el botón o gesto de VR, sin falsos positivos.</summary>
    public class TraduccionTeclasVRPruebas
    {
        [TestCase("E para hablar", "B para hablar")]
        [TestCase("E · Hablar", "B · Hablar")]
        [TestCase("[E] Subir al carro", "[B] Subir al carro")]
        [TestCase("PRESIONA E PARA CONFIGURAR", "PRESIONA B PARA CONFIGURAR")]
        [TestCase("ACÉRCATE A LA MÁQUINA Y PULSA E", "ACÉRCATE A LA MÁQUINA Y PULSA B")]
        [TestCase("Pulsa E junto a puertas, mostradores y personas para interactuar.", "Pulsa B junto a puertas, mostradores y personas para interactuar.")]
        [TestCase("<color=#5CE3D6>E</color> · Hablar", "<color=#5CE3D6>B</color> · Hablar")]
        [TestCase("R RECARGA EL ARMA", "A + CADERA IZQ. RECARGA EL ARMA")]
        [TestCase("MANTÉN SHIFT PARA CORRER", "MANTÉN CLIC STICK IZQ. PARA CORRER")]
        [TestCase("W A S D TE MUEVEN · ESPACIO SALTA", "STICK IZQ. TE MUEVEN · CLIC STICK DER. SALTA")]
        [TestCase("CLIC DER. APUNTA · CLIC DISPARA", "LA MANO APUNTA · GATILLO DER. DISPARA")]
        [TestCase("ESC abre la pausa: ahí guardas la partida y ajustas las opciones.", "Y abre la pausa: ahí guardas la partida y ajustas las opciones.")]
        [TestCase("USA TU PRÓTESIS (TAB)", "USA TU PRÓTESIS (GRIP IZQ. + GATILLO IZQ.)")]
        [TestCase("PASO 2/3 · MANTÉN [ESPACIO] · LEVANTAR", "PASO 2/3 · MANTÉN [CLIC STICK DER.] · LEVANTAR")]
        [TestCase("↑ ↓ ELEGIR · ENTER ACEPTAR · ESC VOLVER", TeclaVR.AyudaMenus)]
        public void Traducir_TeclasDePC_PasanABotonesVR(string pc, string vr)
        {
            Assert.AreEqual(vr, TeclaVR.Traducir(pc));
        }

        [TestCase("ELITE · E")]
        [TestCase("G")]
        [TestCase("E")]
        [TestCase("TE: algo")]
        [TestCase("ENCARGO: Robo")]
        [TestCase("ESPACIOS OCUPADOS")]
        [TestCase("CYBER CITY · SUBURBIOS")]
        [TestCase("YO Y TÚ, E INCLUSO ELLA")]
        [TestCase("OJOS · OJOS DE LECHUZA")]
        [TestCase("B · CONFIGURAR CAMPO")]
        [TestCase("CLIC STICK IZQ.: CORRER")]
        [TestCase("Pulsa A o el gatillo para seguir")]
        public void Traducir_SinTeclasOYaDeVR_NoCambia(string texto)
        {
            Assert.AreEqual(texto, TeclaVR.Traducir(texto));
        }

        [TestCase("MANTÉN RUEDA (CLIC) PARA ELEGIR UN ARMA O USAR UN OBJETO · E Y C USAN TUS DOS OBJETOS")]
        [TestCase("V TE CUBRE DETRÁS DE UNA CAJA O MURO")]
        [TestCase("AGACHADO (CTRL) + ESPACIO RUEDA PARA ESQUIVAR")]
        [TestCase("M O ESC PARA CERRAR · RUEDA: ZOOM · CLIC: MARCAR UN PUNTO O UN LUGAR")]
        [TestCase("Mantén TAB para elegir un arma o usar un objeto; la rueda del ratón también cambia de arma.")]
        [TestCase("ELIGE UNA ZONA: CLIC O SUELTA TAB")]
        public void Traducir_EsEstableYSinTeclasDePC(string pc)
        {
            string vr = TeclaVR.Traducir(pc);
            Assert.AreEqual(vr, TeclaVR.Traducir(vr), "Traducir dos veces da lo mismo.");
            StringAssert.DoesNotContain("TAB", vr.ToUpperInvariant());
            StringAssert.DoesNotContain("ESC ", vr.ToUpperInvariant() + " ");
            StringAssert.DoesNotContain("CTRL", vr.ToUpperInvariant());
            StringAssert.DoesNotContain("RATÓN", vr.ToUpperInvariant());
            StringAssert.DoesNotContain("ESPACIO", vr.ToUpperInvariant());
        }

        [Test]
        public void Traducir_NullOVacio_DevuelveVacio()
        {
            Assert.AreEqual(string.Empty, TeclaVR.Traducir(null));
            Assert.AreEqual(string.Empty, TeclaVR.Traducir(string.Empty));
        }
    }
}

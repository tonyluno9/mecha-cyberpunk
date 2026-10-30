using MechaCyberpunk.Progresion.Prologo.Pandillas;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>T-418: los 11 pasos de la historia de la demo, sus avisos alineados, el guardado por nombre y el largo de los objetivos.</summary>
    public class MaquinaHistoriaPandillasPruebas
    {
        [Test]
        public void PasosNuevosEnOrden()
        {
            PasoHistoria[] pasos =
            {
                PasoHistoria.Despierta, PasoHistoria.PrimeraPelea, PasoHistoria.Pistola, PasoHistoria.Chato, PasoHistoria.Doc, PasoHistoria.Inventor,
                PasoHistoria.Tianguis, PasoHistoria.Jefe, PasoHistoria.NeonAlto, PasoHistoria.Continuara, PasoHistoria.Libre
            };
            AvisoHistoria[] avisos =
            {
                AvisoHistoria.SalioDeCasa, AvisoHistoria.PeleaGanada, AvisoHistoria.PistolaTomada, AvisoHistoria.ChatoUnido, AvisoHistoria.DocUsado,
                AvisoHistoria.InventorUsado, AvisoHistoria.JefeDisponible, AvisoHistoria.TianguisConquistado, AvisoHistoria.HackerUsado,
                AvisoHistoria.ContinuaraCerrado
            };
            Assert.AreEqual(11, System.Enum.GetValues(typeof(PasoHistoria)).Length, "La historia tiene 11 pasos.");
            Assert.AreEqual(10, System.Enum.GetValues(typeof(AvisoHistoria)).Length, "Un aviso por paso, menos Libre.");

            MaquinaHistoriaPandillas m = new MaquinaHistoriaPandillas();
            for (int i = 0; i < avisos.Length; i++)
            {
                Assert.AreEqual(pasos[i], m.Paso);
                // Cualquier otro aviso no avanza.
                foreach (AvisoHistoria otro in avisos)
                {
                    if (otro != avisos[i])
                    {
                        Assert.IsFalse(m.Avisar(otro), otro + " no avanza el paso " + pasos[i]);
                        Assert.AreEqual(pasos[i], m.Paso);
                    }
                }
                Assert.IsTrue(m.Avisar(avisos[i]), avisos[i] + " avanza el paso " + pasos[i]);
            }
            Assert.AreEqual(PasoHistoria.Libre, m.Paso);
            Assert.IsTrue(m.Terminada);
            Assert.IsFalse(m.Avisar(AvisoHistoria.ContinuaraCerrado), "Libre ya no avanza.");

            // El guardado es por nombre: los pasos nuevos se leen; uno que ya no existe no.
            foreach (string nombre in new[] { "Tianguis", "Doc", "Inventor", "NeonAlto" })
            {
                Assert.IsTrue(MaquinaHistoriaPandillas.TryLeer(nombre, out PasoHistoria leido), nombre);
                Assert.AreEqual(nombre, leido.ToString());
            }
            Assert.IsFalse(MaquinaHistoriaPandillas.TryLeer("Mecha", out _), "Mecha no es un paso.");
            Assert.IsFalse(MaquinaHistoriaPandillas.TryLeer(string.Empty, out _));

            // Todos los objetivos caben en 32 caracteres (D-tony-240) y van en mayúsculas.
            foreach (string texto in DirectorPandillas.TextosObjetivo)
            {
                Assert.LessOrEqual(texto.Length, DirectorPandillas.LargoMaximoObjetivo, texto);
                Assert.AreEqual(texto.ToUpperInvariant(), texto, texto);
            }
            Assert.AreEqual(32, DirectorPandillas.LargoMaximoObjetivo);
            Assert.AreEqual("VENCE A LOS SKULLS 3/7", DirectorPandillas.TextoTianguis(3, 7));
            Assert.AreEqual("VENCE A LOS SKULLS 7/7", DirectorPandillas.TextoTianguis(9, 7), "No pasa del total.");
        }
    }
}

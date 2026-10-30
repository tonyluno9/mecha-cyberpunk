using MechaCyberpunk.Progresion.Apoyo;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>T-417: el aviso y la regla de uso de cada personaje de apoyo.</summary>
    public class ReglaPersonajeApoyoPruebas
    {
        [Test]
        public void AvisoYServicio()
        {
            // Inventor: siempre abre la tienda.
            var mejoras = ReglaPersonajeApoyo.Evaluar(ServicioApoyo.Mejoras, 0, 100f, 100f, 0, 0);
            Assert.AreEqual("MEJORAS DE ARMAS", mejoras.Texto);
            Assert.IsTrue(mejoras.PuedeUsar);

            // Doc: con vida baja y dinero cura; sin dinero no alcanza; con vida llena dice que estás bien.
            var cura = ReglaPersonajeApoyo.Evaluar(ServicioApoyo.Curar, 50, 40f, 100f, 0, 200);
            Assert.IsTrue(cura.PuedeUsar);
            StringAssert.Contains("CURARTE", cura.Texto);
            StringAssert.Contains("50", cura.Texto);
            var sinDinero = ReglaPersonajeApoyo.Evaluar(ServicioApoyo.Curar, 50, 40f, 100f, 0, 49);
            Assert.AreEqual("NO TE ALCANZA", sinDinero.Texto);
            Assert.IsFalse(sinDinero.PuedeUsar);
            var llena = ReglaPersonajeApoyo.Evaluar(ServicioApoyo.Curar, 50, 100f, 100f, 0, 200);
            Assert.AreEqual("ESTÁS BIEN, MIJO", llena.Texto);
            Assert.IsFalse(llena.PuedeUsar);

            // Mecánico: sin calor el carro está limpio; con calor y dinero pinta; sin dinero no alcanza.
            var limpio = ReglaPersonajeApoyo.Evaluar(ServicioApoyo.Taller, 100, 100f, 100f, 0, 500);
            Assert.AreEqual("TU CARRO ESTÁ LIMPIO", limpio.Texto);
            Assert.IsFalse(limpio.PuedeUsar);
            var pinta = ReglaPersonajeApoyo.Evaluar(ServicioApoyo.Taller, 100, 100f, 100f, 2, 200);
            Assert.IsTrue(pinta.PuedeUsar);
            StringAssert.Contains("PINTAR TU CARRO", pinta.Texto);
            Assert.IsFalse(ReglaPersonajeApoyo.Evaluar(ServicioApoyo.Taller, 100, 100f, 100f, 2, 99).PuedeUsar);

            // Hacker y Director siempre se pueden usar.
            Assert.AreEqual("INFORMACIÓN", ReglaPersonajeApoyo.Evaluar(ServicioApoyo.Informacion, 0, 0f, 100f, 0, 0).Texto);
            Assert.AreEqual("PLATICAR", ReglaPersonajeApoyo.Evaluar(ServicioApoyo.Platica, 0, 0f, 100f, 0, 0).Texto);

            // La plática nunca repite la última frase, con cualquier azar.
            for (int ultima = 0; ultima < 3; ultima++)
            {
                for (int azar = 0; azar < 10; azar++)
                {
                    int siguiente = ReglaPersonajeApoyo.SiguienteFrase(3, ultima, azar);
                    Assert.AreNotEqual(ultima, siguiente);
                    Assert.That(siguiente, Is.InRange(0, 2));
                }
            }
            Assert.AreEqual(-1, ReglaPersonajeApoyo.SiguienteFrase(0, -1, 0));

            // La pista: el jefe primero, luego el respeto que falta, luego la frase.
            StringAssert.Contains("30 M", ReglaPersonajeApoyo.Pista(true, 30, "EL TIANGUIS", "", 0, 70, false, "x"));
            StringAssert.Contains("20 DE RESPETO", ReglaPersonajeApoyo.Pista(false, 0, "", "EL TIANGUIS", 50, 70, false, "x"));
            Assert.AreEqual("x", ReglaPersonajeApoyo.Pista(false, 0, "", "SUBURBIOS", 50, 70, true, "x"));
            Assert.AreEqual("x", ReglaPersonajeApoyo.Pista(false, 0, "", "", 0, 70, false, "x"));
        }
    }
}

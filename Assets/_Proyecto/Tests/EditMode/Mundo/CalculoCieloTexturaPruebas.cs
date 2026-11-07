using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-635: estrellas, nubes, ambiente por gradiente y luz cálida del día.</summary>
    public class CalculoCieloTexturaPruebas
    {
        [Test]
        public void Estrellas_DeDiaNoSeVenYDeNocheSi()
        {
            Assert.AreEqual(0f, CalculoCieloTextura.OpacidadEstrellas(0f), 0.0001f);
            Assert.Greater(CalculoCieloTextura.OpacidadEstrellas(1f), 0.1f, "De noche hay estrellas");
            Assert.Less(CalculoCieloTextura.OpacidadEstrellas(1f), 0.3f, "T-880: pocas, el cielo de noche es liso como K01");
            Assert.Less(CalculoCieloTextura.OpacidadEstrellas(0.5f), CalculoCieloTextura.OpacidadEstrellas(1f));
        }

        [Test]
        public void Nubes_DeDiaBlancasYDeNocheConBrilloDeCiudad()
        {
            Color dia = CalculoCieloTextura.ColorNubes(0f);
            Color noche = CalculoCieloTextura.ColorNubes(1f);
            Assert.AreEqual(1f, dia.b, 0.001f, "De día son blancas");
            Assert.Greater(noche.r, noche.g, "De noche el brillo de la ciudad las tiñe de rosa");
            Assert.Greater(dia.a, 0.3f);
            Assert.Less(noche.a, 0.1f, "T-880: de noche el cielo es liso, casi sin nubes");
        }

        [Test]
        public void Ambiente_PorGradiente_CieloMasClaroQueSuelo()
        {
            Color ambiente = new Color(0.5f, 0.55f, 0.7f);
            Color cielo = new Color(0.3f, 0.5f, 0.95f);
            CalculoCieloTextura.Ambiente(ambiente, cielo, 0f, out Color arriba, out Color horizonte, out Color suelo);
            Assert.AreEqual(ambiente, horizonte);
            Assert.Greater(arriba.b, horizonte.b, "El cielo aporta azul de día");
            Assert.Less(suelo.grayscale, horizonte.grayscale, "El suelo es más oscuro");
            Assert.AreNotEqual(arriba, suelo, "No es plano");
        }

        [Test]
        public void LuzCalida_DeDiaSeTiñeYDeNocheNo()
        {
            Color blanco = new Color(1f, 0.97f, 0.9f);
            Color dia = CalculoCieloTextura.LuzCalida(blanco, 0f);
            Assert.Less(dia.b, blanco.b, "Menos azul: más cálida");
            Assert.AreEqual(blanco, CalculoCieloTextura.LuzCalida(blanco, 1f));
        }
    }
}

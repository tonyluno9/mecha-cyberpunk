using MechaCyberpunk.Personaje.Habilidades;
using MechaCyberpunk.UI.Rueda;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-57: las zonas de la silueta del menú de habilidades del EXO.</summary>
    public class ZonasRuedaExoT57Pruebas
    {
        [Test]
        public void ZonaEn_CentroDeCadaZona_DevuelveEsaHabilidad()
        {
            foreach (HabilidadProtesis h in System.Enum.GetValues(typeof(HabilidadProtesis)))
            {
                Assert.AreEqual((int)h, RuedaHabilidadesExo.ZonaEn(RuedaHabilidadesExo.PosicionDe(h)), h.ToString());
            }
        }

        [Test]
        public void ZonaEn_LejosDelCuerpo_DevuelveMenosUno()
        {
            Assert.AreEqual(-1, RuedaHabilidadesExo.ZonaEn(new Vector2(0f, -420f)));
            Assert.AreEqual(-1, RuedaHabilidadesExo.ZonaEn(new Vector2(0f, 60f))); // el torso no es zona
            Assert.AreEqual(-1, RuedaHabilidadesExo.ZonaEn(Vector2.zero)); // T-1760: el cursor arranca al centro y no debe caer en ninguna zona
        }
    }
}

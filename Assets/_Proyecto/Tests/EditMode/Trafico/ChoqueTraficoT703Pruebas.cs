using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Datos;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-703: reglas del choque del tráfico (nivel, tiempos, conductor) y el rango de velocidades de los carros.</summary>
    public class ChoqueTraficoT703Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigTraficoCalle_Trafico.asset";

        [Test]
        public void Nivel_RoceNada_LeveYFuerteSegunLaRapidez()
        {
            Assert.AreEqual(NivelChoque.Ninguno, ReglaChoqueTrafico.Nivel(0f));
            Assert.AreEqual(NivelChoque.Ninguno, ReglaChoqueTrafico.Nivel(ReglaChoqueTrafico.UmbralLeve - 0.01f));
            Assert.AreEqual(NivelChoque.Leve, ReglaChoqueTrafico.Nivel(ReglaChoqueTrafico.UmbralLeve));
            Assert.AreEqual(NivelChoque.Leve, ReglaChoqueTrafico.Nivel(ReglaChoqueTrafico.UmbralFuerte - 0.01f));
            Assert.AreEqual(NivelChoque.Fuerte, ReglaChoqueTrafico.Nivel(ReglaChoqueTrafico.UmbralFuerte));
            Assert.AreEqual(NivelChoque.Fuerte, ReglaChoqueTrafico.Nivel(40f));
        }

        [Test]
        public void Detenido_UnRoceNoParaYUnFuerteParaMasQueUnLeve()
        {
            Assert.AreEqual(0f, ReglaChoqueTrafico.SegundosDetenido(NivelChoque.Ninguno));
            Assert.Greater(ReglaChoqueTrafico.SegundosDetenido(NivelChoque.Leve), 2f, "Se queda quieto unos segundos");
            Assert.Greater(ReglaChoqueTrafico.SegundosDetenido(NivelChoque.Fuerte), ReglaChoqueTrafico.SegundosDetenido(NivelChoque.Leve));
            Assert.GreaterOrEqual(ReglaChoqueTrafico.SegundosDetenido(NivelChoque.Fuerte), ReglaChoqueTrafico.SegundosConductorAfuera,
                "El conductor vuelve al carro antes de que el carro arranque");
        }

        [Test]
        public void ConductorSeBaja_SoloEnElChoqueFuerte()
        {
            Assert.IsFalse(ReglaChoqueTrafico.ConductorSeBaja(NivelChoque.Ninguno));
            Assert.IsFalse(ReglaChoqueTrafico.ConductorSeBaja(NivelChoque.Leve));
            Assert.IsTrue(ReglaChoqueTrafico.ConductorSeBaja(NivelChoque.Fuerte));
        }

        [Test]
        public void PuedeReaccionar_NoSeRepiteElMismoGolpe()
        {
            Assert.IsFalse(ReglaChoqueTrafico.PuedeReaccionar(10.5f, 10f));
            Assert.IsTrue(ReglaChoqueTrafico.PuedeReaccionar(10f + ReglaChoqueTrafico.SegundosEntreChoques, 10f));
        }

        [Test]
        public void Detener_ElCalculoSeParaEnSecoPitaYVuelveAAcelerar()
        {
            CalculoTraficoCalle c = new CalculoTraficoCalle(10f, 4f, 9f, 14f, 4f, 1.5f, 3f);
            for (int i = 0; i < 50; i++)
            {
                c.Avanzar(0.1f, float.MaxValue);
            }
            Assert.AreEqual(10f, c.Velocidad, 0.01f);
            c.Detener(2f);
            Assert.AreEqual(0f, c.Velocidad, "Se para en seco");
            Assert.IsTrue(c.Chocado);
            Assert.IsTrue(c.TomarPitido(), "Pita");
            for (int i = 0; i < 15; i++)
            {
                c.Avanzar(0.1f, float.MaxValue);
            }
            Assert.AreEqual(0f, c.Velocidad, "Sigue quieto");
            Assert.IsFalse(c.Bloqueado, "Un choque no cuenta como atorón (no se reubica)");
            for (int i = 0; i < 20; i++)
            {
                c.Avanzar(0.1f, float.MaxValue);
            }
            Assert.IsFalse(c.Chocado);
            Assert.Greater(c.Velocidad, 3f, "Pasado el tiempo sigue su camino");
        }

        [Test]
        public void VelocidadDeCarro_SeQuedaDentroDelRango()
        {
            Assert.AreEqual(8f, ReglaChoqueTrafico.VelocidadDeCarro(8f, 12f, 0.0), 0.001f);
            Assert.AreEqual(12f, ReglaChoqueTrafico.VelocidadDeCarro(8f, 12f, 1.0), 0.001f);
            Assert.AreEqual(10f, ReglaChoqueTrafico.VelocidadDeCarro(8f, 12f, 0.5), 0.001f);
            Assert.AreEqual(12f, ReglaChoqueTrafico.VelocidadDeCarro(8f, 12f, 7.0), 0.001f, "Un azar fuera de rango se recorta");
        }

        [Test]
        public void Config_ElRangoDeVelocidadesEsNormalParaUnaCalle()
        {
            ConfigTraficoCalle config = AssetDatabase.LoadAssetAtPath<ConfigTraficoCalle>(RutaConfig);
            Assert.IsNotNull(config);
            Assert.GreaterOrEqual(config.VelocidadMinima, 8f, "Ningún carro va a menos de unos 29 km/h");
            Assert.LessOrEqual(config.VelocidadMaxima, 14f, "Ni a más de unos 50 km/h");
            Assert.Greater(config.VelocidadMaxima, config.VelocidadMinima, "Hay variedad entre carros");
        }
    }
}

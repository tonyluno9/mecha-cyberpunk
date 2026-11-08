using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-728: el carro de los asaltantes huye a velocidad de persecución real (no a 10 m/s) y arranca fuerte.</summary>
    public class AsaltoMundoT728Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigTraficoCalle_Trafico.asset";

        [Test]
        public void Huida_EsMasRapidaQueElTraficoYAlcanzableParaElCarroDelJugador()
        {
            ConfigTraficoCalle config = AssetDatabase.LoadAssetAtPath<ConfigTraficoCalle>(RutaConfig);
            Assert.IsNotNull(config);
            Assert.GreaterOrEqual(AsaltoMundo.VelocidadHuida, 13f, "Ya no son 10 m/s");
            Assert.Greater(AsaltoMundo.VelocidadHuida, config.VelocidadMaxima, "Más rápido que cualquier carro del tráfico");
            Assert.LessOrEqual(AsaltoMundo.VelocidadHuida, 16f, "El carro del jugador (16 m/s) lo puede alcanzar");
            Assert.Greater(AsaltoMundo.MetrosHuidaMinimos, 150f, "Una ruta lo bastante larga para que haya persecución");
            Assert.Less(AsaltoMundo.MetrosHuidaMinimos, AsaltoMundo.MetrosHuidaIdeales);
            Assert.Less(AsaltoMundo.MetrosHuidaIdeales, AsaltoMundo.MetrosHuidaMaximos);
        }

        [Test]
        public void CarroQueHuye_AlcanzaSuVelocidadMuchoAntesQueElTrafico()
        {
            ConfigTraficoCalle config = AssetDatabase.LoadAssetAtPath<ConfigTraficoCalle>(RutaConfig);
            CalculoTraficoCalle normal = config.Crear(AsaltoMundo.VelocidadHuida);
            CalculoTraficoCalle huida = config.Crear(AsaltoMundo.VelocidadHuida, 7f);
            for (int i = 0; i < 20; i++)
            {
                normal.Avanzar(0.1f, float.MaxValue);
                huida.Avanzar(0.1f, float.MaxValue);
            }
            Assert.Greater(huida.Velocidad, normal.Velocidad + 4f, "A los 2 s el que huye va mucho más rápido");
            Assert.Greater(huida.Velocidad, AsaltoMundo.VelocidadHuida * 0.9f);
        }
    }
}

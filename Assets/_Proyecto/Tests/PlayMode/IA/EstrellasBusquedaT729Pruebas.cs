using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Persecucion;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-729: el aviso del HUD (las estrellas parpadean) coincide con la regla de la policía (te dan por escapado a los 45 s sin que te vean, y avisan los últimos 8).</summary>
    public class EstrellasBusquedaT729Pruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";

        [Test]
        public void ElHudParpadeaCuandoFaltanLosSegundosDeAvisoParaPerderte()
        {
            ConfigPersecucion config = AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig);
            Assert.IsNotNull(config);
            Assert.AreEqual(45f, config.SegundosParaEscapar, 0.001f, "Sin que te vean 45 s, dejas de ser buscado");
            Assert.AreEqual(config.SegundosParaEscapar - ReglaBusquedaPolicial.SegundosAviso, ReglaEstrellasH10.SegundosSinVerParaParpadear, 0.001f,
                "El HUD empieza a parpadear justo cuando la regla de la policía avisa (UI no ve Trafico: las constantes se cuidan aquí)");
        }
    }
}

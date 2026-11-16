using MechaCyberpunk.Mundo.Datos;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-1002: una partida nueva y la demo empiezan de día. Falla si alguna config de reloj vuelve a empezar de noche.</summary>
    public class HoraInicialDeDiaPruebas
    {
        private const string RutaRelojCiudad = "Assets/_Proyecto/Mundo/Datos/ConfigRelojMundo_Mundo.asset";
        private const string RutaRelojDemo = "Assets/_Proyecto/Progresion/Prologo/Datos/ConfigRelojMundo_Pandillas.asset";

        [TestCase(RutaRelojCiudad)]
        [TestCase(RutaRelojDemo)]
        public void LaHoraInicialEsDeDia(string ruta)
        {
            ConfigRelojMundo config = AssetDatabase.LoadAssetAtPath<ConfigRelojMundo>(ruta);
            Assert.IsNotNull(config, ruta);
            Assert.GreaterOrEqual(config.HoraInicial, config.HoraAmanecer, "empieza antes del amanecer: " + ruta);
            Assert.Less(config.HoraInicial, config.HoraAnochecer, "empieza de noche: " + ruta);
        }
    }
}

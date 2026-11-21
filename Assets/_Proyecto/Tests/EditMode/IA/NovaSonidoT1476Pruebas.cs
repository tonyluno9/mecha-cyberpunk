using MechaCyberpunk.IA.Datos;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-1476: la llegada de Nova pide el aviso de la policía (NOV_CAIDA_Y_ENCENDIDO).</summary>
    public class NovaSonidoT1476Pruebas
    {
        [Test]
        public void ConfigPorOmision_SonidoLlegada_EsNOV_CAIDA_Y_ENCENDIDO()
        {
            ConfigRespuestaNova config = ScriptableObject.CreateInstance<ConfigRespuestaNova>();
            try
            {
                Assert.AreEqual("NOV_CAIDA_Y_ENCENDIDO", config.SonidoLlegada);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}

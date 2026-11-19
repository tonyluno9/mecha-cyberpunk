using MechaCyberpunk.IA.Datos;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-1476: la llegada de Nova pide el aviso de la policía (POL_NOVA_AVISO).</summary>
    public class NovaSonidoT1476Pruebas
    {
        [Test]
        public void ConfigPorOmision_SonidoLlegada_EsPOL_NOVA_AVISO()
        {
            ConfigRespuestaNova config = ScriptableObject.CreateInstance<ConfigRespuestaNova>();
            try
            {
                Assert.AreEqual("POL_NOVA_AVISO", config.SonidoLlegada);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}

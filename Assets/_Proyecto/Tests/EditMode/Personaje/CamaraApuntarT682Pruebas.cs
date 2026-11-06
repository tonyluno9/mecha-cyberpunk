using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-682: la cámara apuntando tiene el mismo encuadre que al caminar (altura y distancia), centrada, y solo cambia el zoom.</summary>
    public class CamaraApuntarT682EditPruebas
    {
        [Test]
        public void ConfigCamara_Apuntando_MismaAlturaYDistanciaQueCaminando_CentradaYConZoom()
        {
            ConfigCamaraHombroLector c = new ConfigCamaraHombroLector();
            Assert.AreEqual(0f, c.Valor("lateralApuntando"), 0.001f, "Apuntando queda centrada");
            Assert.AreEqual(c.Valor("alturaNormal"), c.Valor("alturaApuntando"), 0.001f, "Sin subir la cámara");
            Assert.AreEqual(c.Valor("distanciaNormal"), c.Valor("distanciaApuntando"), 0.001f, "Misma distancia");
            Assert.Less(c.Valor("fovApuntando"), c.Valor("fovNormal") - 5f, "Apuntando hace zoom (FOV menor)");
            Assert.Greater(c.Valor("lateralNormal"), 0.3f, "Sin apuntar sigue al hombro");
        }

        private class ConfigCamaraHombroLector
        {
            private readonly SerializedObject so = new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/_Proyecto/Personaje/Datos/ConfigCamaraHombro_Base.asset"));
            public float Valor(string campo) => so.FindProperty(campo).floatValue;
        }
    }
}

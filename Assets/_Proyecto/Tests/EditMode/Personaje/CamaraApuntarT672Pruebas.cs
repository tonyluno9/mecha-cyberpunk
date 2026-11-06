using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-672: la cámara apuntando va centrada, detrás y más alta que la cabeza.</summary>
    public class CamaraApuntarT672EditPruebas
    {
        [Test]
        public void ConfigCamara_Apuntando_CentradaDetrasYAltaSobreLaCabeza()
        {
            ConfigCamaraHombroLector c = new ConfigCamaraHombroLector();
            Assert.AreEqual(0f, c.Valor("lateralApuntando"), 0.001f, "Sin desplazamiento lateral");
            Assert.GreaterOrEqual(c.Valor("alturaApuntando"), 1.9f, "Más alta que la cabeza de 1.80 m");
            Assert.GreaterOrEqual(c.Valor("distanciaApuntando"), 1.5f, "Detrás del personaje");
            Assert.Greater(c.Valor("lateralNormal"), 0.3f, "Sin apuntar sigue al hombro");
        }

        private class ConfigCamaraHombroLector
        {
            private readonly SerializedObject so = new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/_Proyecto/Personaje/Datos/ConfigCamaraHombro_Base.asset"));
            public float Valor(string campo) => so.FindProperty(campo).floatValue;
        }
    }
}

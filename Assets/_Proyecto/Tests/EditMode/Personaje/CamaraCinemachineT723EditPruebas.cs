using System.IO;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-723: Cinemachine instalado y los valores de la cámara a pie y del carro medidos contra CAM01_APie.png y CAM03_Carro.png.</summary>
    public class CamaraCinemachineT723EditPruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Personaje/Datos/ConfigCamaraHombro_Base.asset";
        private const string RutaConfigCarro = "Assets/_Proyecto/Vehiculos/Datos/ConfigCamaraCarro_Viejo.asset";

        private static SerializedObject Abrir(string ruta) => new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ruta));

        [Test]
        public void Manifest_TieneCinemachine3()
        {
            string manifest = File.ReadAllText("Packages/manifest.json");
            Assert.IsTrue(manifest.Contains("\"com.unity.cinemachine\": \"3."), "Packages/manifest.json lleva com.unity.cinemachine 3.x");
        }

        [Test]
        public void ConfigAPie_DetrasYUnPocoArribaDelHombro_CentradaYSinZoomSalvoAlApuntar()
        {
            SerializedObject c = Abrir(RutaConfig);
            float camaraAltura = c.FindProperty("alturaNormal").floatValue + c.FindProperty("brazoVertical").floatValue;
            Assert.That(camaraAltura, Is.InRange(1.6f, 2.1f), "La cámara queda un poco arriba del hombro y la cabeza");
            Assert.That(c.FindProperty("distanciaNormal").floatValue, Is.InRange(3f, 4f), "Detrás del personaje (CAM01)");
            Assert.AreEqual(0f, c.FindProperty("lateralNormal").floatValue, 0.001f, "T-699: centrada");
            Assert.AreEqual(0f, c.FindProperty("lateralApuntando").floatValue, 0.001f, "T-682: apuntando centrada");
            Assert.AreEqual(c.FindProperty("distanciaNormal").floatValue, c.FindProperty("distanciaApuntando").floatValue, 0.001f, "Apuntar no cambia la distancia");
            Assert.AreEqual(c.FindProperty("alturaNormal").floatValue, c.FindProperty("alturaApuntando").floatValue, 0.001f, "Apuntar no sube la cámara");
            Assert.Less(c.FindProperty("fovApuntando").floatValue, c.FindProperty("fovNormal").floatValue - 5f, "Solo apuntar hace zoom");
        }

        [Test]
        public void ConfigCarro_LaCamaraMiraPorEncimaDelVehiculoParaQueQuedeAbajo()
        {
            Assert.Greater(Abrir(RutaConfigCarro).FindProperty("alturaMira").floatValue, 0.5f);
        }

        [Test]
        public void YaNoHayCamaraDeHombroPropiaQueMuevaLaCamara_SoloElBrain()
        {
            string codigo = File.ReadAllText("Assets/_Proyecto/Personaje/Movimiento/CamaraHombro.cs");
            Assert.IsTrue(codigo.Contains("CinemachineBrain") && codigo.Contains("CinemachineThirdPersonFollow") && codigo.Contains("CinemachineDeoccluder"));
            Assert.IsFalse(codigo.Contains("transform.SetPositionAndRotation(posicion"), "CamaraHombro ya no coloca la cámara a mano");
        }
    }
}

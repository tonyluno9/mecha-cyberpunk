using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1001: sin apuntar casi centrada (CAM04); apuntando se acerca y va sobre el hombro derecho (CAM05).</summary>
    public class CamaraHombroT1001EditPruebas
    {
        private static float Valor(SerializedObject so, string campo) => so.FindProperty(campo).floatValue;

        [Test]
        public void ConfigCamara_ApuntandoSeAcercaYVaAlHombroDerecho_SinApuntarCasiCentrada()
        {
            SerializedObject c = new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/_Proyecto/Personaje/Datos/ConfigCamaraHombro_Base.asset"));
            Assert.That(Valor(c, "lateralNormal"), Is.InRange(0.15f, 0.45f), "CAM04: un poco a la derecha, personaje casi centrado");
            Assert.That(Valor(c, "lateralApuntando"), Is.InRange(0.6f, 0.9f), "CAM05: sobre el hombro derecho");
            Assert.Less(Valor(c, "distanciaApuntando"), Valor(c, "distanciaNormal") - 1.5f, "CAM05: se acerca, de la cintura para arriba");
            Assert.Less(Valor(c, "fovApuntando"), Valor(c, "fovNormal") - 5f, "Apuntando hace un poco de zoom");
            Assert.Greater(Valor(c, "rapidezCambioHombro"), 1f, "El cambio de hombro es suave pero no lento");
        }
    }
}

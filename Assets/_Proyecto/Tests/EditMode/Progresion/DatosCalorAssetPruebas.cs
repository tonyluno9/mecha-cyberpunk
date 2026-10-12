using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests
{
    public class DatosCalorAssetPruebas
    {
        [Test]
        public void DatosCalorBase_TieneLosValoresDelDiseno()
        {
            var datos = AssetDatabase.LoadAssetAtPath<DatosCalor>("Assets/_Proyecto/Progresion/Datos/DatosCalor_Base.asset");
            Assert.IsNotNull(datos);
            Assert.AreEqual(100f, datos.Maximo, 0.0001f);
            CollectionAssert.AreEqual(new[] { 25f, 50f, 75f }, datos.UmbralesNivel);
            Assert.AreEqual(10f, datos.RetrasoEnfriamiento, 0.0001f);
            Assert.AreEqual(5f, datos.EnfriamientoPorSegundo, 0.0001f);
        }
    }
}

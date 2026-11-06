using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Sonido
{
    /// <summary>T-676: el disparo de cada arma suena siempre igual: mismo clip y sin variación de tono.</summary>
    public class DisparoSiempreIgualT676Pruebas
    {
        private const string RutaCatalogo = "Assets/_Proyecto/Progresion/Datos/CatalogoSonidos.asset";

        [TestCase("ARM_PIS_DISPARO")]
        [TestCase("ARM_ESC_DISPARO")]
        [TestCase("ARM_FUS_DISPARO")]
        [TestCase("ARM_SNI_DISPARO")]
        public void Disparo_NoVariaElTono(string id)
        {
            Assert.AreEqual(0f, ReglaMezclaSonidos.Para(id).VariacionTono, id);
        }

        [TestCase("ARM_PIS_DISPARO")]
        [TestCase("ARM_ESC_DISPARO")]
        [TestCase("ARM_FUS_DISPARO")]
        [TestCase("ARM_SNI_DISPARO")]
        public void Disparo_ElCatalogoDevuelveSiempreElMismoClip(string id)
        {
            CatalogoSonidos catalogo = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>(RutaCatalogo);
            Assert.IsNotNull(catalogo);
            AudioClip primero = catalogo.Elegir(id);
            Assert.IsNotNull(primero, id);
            for (int i = 0; i < 30; i++)
            {
                Assert.AreSame(primero, catalogo.Elegir(id), id);
            }
        }
    }
}

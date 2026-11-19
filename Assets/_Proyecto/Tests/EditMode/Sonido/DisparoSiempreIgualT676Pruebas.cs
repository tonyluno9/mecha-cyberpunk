using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Sonido
{
    /// <summary>T-676 (T-1471): el disparo de cada arma usa siempre el mismo clip, con variación chica de tono (±2.5 %) y de volumen (5 %): nada suena idéntico.</summary>
    public class DisparoSiempreIgualT676Pruebas
    {
        private const string RutaCatalogo = "Assets/_Proyecto/Progresion/Datos/CatalogoSonidos.asset";

        [TestCase("ARM_PIS_DISPARO")]
        [TestCase("ARM_ESC_DISPARO")]
        [TestCase("ARM_FUS_DISPARO")]
        [TestCase("ARM_SNI_DISPARO")]
        public void Disparo_VariaPocoElTonoYElVolumen(string id)
        {
            PoliticaSonido p = ReglaMezclaSonidos.Para(id);
            Assert.That(p.VariacionTono, Is.InRange(0.001f, 0.03f), id + ": tono ±2-3 %");
            Assert.That(p.VariacionVolumen, Is.InRange(0.001f, 0.05f), id + ": volumen hasta 5 %");
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

using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.UI.Tiendas;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-1314: El Mercado solo vende armas base y balas con nombre claro; las mejoras son del banco de Tino.</summary>
    public class MercadoSoloArmasBalasT1314Pruebas
    {
        private const string CarpetaArmas = "Assets/_Proyecto/Personaje/Datos/Armas/";
        private const string CarpetaBalas = "Assets/_Proyecto/Economia/Datos/";

        [TestCase("escopeta"), TestCase("fusil"), TestCase("revolver"), TestCase("rifleplasma"), TestCase("sniper"), TestCase("subfusil"), TestCase("lanzagranadas")]
        public void SeOfrece_ArmaBaseQueNoSeTiene_Si(string arma)
        {
            DatosArma datos = AssetDatabase.LoadAssetAtPath<DatosArma>(CarpetaArmas + "DatosArma_" + arma + "_0.asset");
            Assert.IsNotNull(datos);
            Assert.IsTrue(ReglaVistaMercado.SeOfrece(datos, false));
            Assert.IsFalse(ReglaVistaMercado.SeOfrece(datos, true), "Si ya la tienes no se ofrece");
        }

        [TestCase("escopeta", 1), TestCase("fusil", 2), TestCase("revolver", 3), TestCase("rifleplasma", 1)]
        public void SeOfrece_Mejoras_NuncaEnLaTienda(string arma, int nivel)
        {
            DatosArma datos = AssetDatabase.LoadAssetAtPath<DatosArma>(CarpetaArmas + "DatosArma_" + arma + "_" + nivel + ".asset");
            Assert.IsNotNull(datos);
            Assert.IsFalse(ReglaVistaMercado.SeOfrece(datos, false));
            Assert.IsFalse(ReglaVistaMercado.SeOfrece(datos, true));
        }

        [Test]
        public void Balas_ConNombreClaro_SinNivelNiMejora()
        {
            string[] guias = AssetDatabase.FindAssets("t:DatosMunicionArma", new[] { CarpetaBalas });
            Assert.AreEqual(8, guias.Length);
            bool plasma = false;
            foreach (string g in guias)
            {
                DatosMunicionArma balas = AssetDatabase.LoadAssetAtPath<DatosMunicionArma>(AssetDatabase.GUIDToAssetPath(g));
                string nombre = balas.Nombre.ToLowerInvariant();
                StringAssert.DoesNotMatch(@"\b[123]\b", nombre, balas.name);
                StringAssert.DoesNotContain("mejora", nombre);
                plasma |= balas.Arma == ArmaAPie.RiflePlasma && balas.Nombre == "Celdas de plasma";
            }
            Assert.IsTrue(plasma, "Las balas del rifle de plasma se llaman Celdas de plasma");
        }
    }
}

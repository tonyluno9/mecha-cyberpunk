using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-403: los 16 assets reales de armas (D-tony-214) mejoran de nivel en nivel y cumplen el balance contra 60 de vida.</summary>
    public class CatalogoArmasPruebas
    {
        private const string RutaCatalogo = "Assets/_Proyecto/Personaje/Datos/CatalogoArmas_Base.asset";
        private const float VidaDeReferencia = 60f;
        private const int Niveles = 4;

        private static int Disparos(DatosArma arma)
        {
            float porDisparo = arma.Danio * arma.Perdigones;
            return (int)System.Math.Ceiling(VidaDeReferencia / porDisparo);
        }

        [Test]
        public void NivelesMejoranYBalance()
        {
            CatalogoArmas catalogo = AssetDatabase.LoadAssetAtPath<CatalogoArmas>(RutaCatalogo);
            Assert.IsNotNull(catalogo, "Existe CatalogoArmas_Base");
            Assert.AreEqual(16, catalogo.Armas.Count, "4 armas por 4 niveles");

            foreach (ArmaAPie tipo in new[] { ArmaAPie.Punos, ArmaAPie.Pistola, ArmaAPie.Escopeta, ArmaAPie.Subfusil })
            {
                DatosArma anterior = null;
                for (int nivel = 0; nivel < Niveles; nivel++)
                {
                    DatosArma actual = catalogo.Buscar(tipo, nivel);
                    Assert.IsNotNull(actual, tipo + " nivel " + nivel);
                    if (anterior != null)
                    {
                        Assert.GreaterOrEqual(actual.Danio, anterior.Danio, tipo + " " + nivel + ": el daño no baja");
                        Assert.GreaterOrEqual(actual.Cargador, anterior.Cargador, tipo + " " + nivel + ": el cargador no baja");
                        Assert.LessOrEqual(actual.Recarga, anterior.Recarga, tipo + " " + nivel + ": la recarga no sube");
                    }
                    anterior = actual;
                }
            }

            Assert.AreEqual(3, Disparos(catalogo.Buscar(ArmaAPie.Pistola, 0)), "Pistola: 0 de 60 en 3 tiros");
            Assert.AreEqual(1, Disparos(catalogo.Buscar(ArmaAPie.Escopeta, 0)), "Escopeta: 0 en 1 disparo a quemarropa");
            Assert.AreEqual(5, Disparos(catalogo.Buscar(ArmaAPie.Subfusil, 0)), "Subfusil: 0 en 5 balas");
        }
    }
}

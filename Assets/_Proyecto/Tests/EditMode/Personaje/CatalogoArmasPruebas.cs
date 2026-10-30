using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEditor;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-403: los 20 assets reales de armas (D-tony-214 y D-tony-253) mejoran de nivel en nivel y cumplen el balance contra 60 de vida.</summary>
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
            Assert.AreEqual(20, catalogo.Armas.Count, "5 armas por 4 niveles");

            foreach (ArmaAPie tipo in new[] { ArmaAPie.Punos, ArmaAPie.Pistola, ArmaAPie.Escopeta, ArmaAPie.Fusil, ArmaAPie.Sniper })
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
            Assert.AreEqual(4, Disparos(catalogo.Buscar(ArmaAPie.Fusil, 0)), "Fusil: 0 en 4 balas");
            Assert.AreEqual(1, Disparos(catalogo.Buscar(ArmaAPie.Sniper, 0)), "Sniper: 0 en 1 bala");
        }

        /// <summary>T-416: 5 armas por 4 niveles (base y tres mejoras) que mejoran en algo, 19 ofertas de armas en El Mercado, el sniper al extremo.</summary>
        [Test]
        public void CincoArmasTresNiveles()
        {
            CatalogoArmas catalogo = AssetDatabase.LoadAssetAtPath<CatalogoArmas>(RutaCatalogo);
            Assert.AreEqual(20, catalogo.Armas.Count, "5 armas x 4 niveles");
            foreach (ArmaAPie tipo in System.Enum.GetValues(typeof(ArmaAPie)))
            {
                for (int nivel = 1; nivel < Niveles; nivel++)
                {
                    DatosArma antes = catalogo.Buscar(tipo, nivel - 1);
                    DatosArma ahora = catalogo.Buscar(tipo, nivel);
                    Assert.IsNotNull(ahora, tipo + " " + nivel);
                    bool mejora = ahora.Danio > antes.Danio || ahora.Cargador > antes.Cargador || ahora.Recarga < antes.Recarga;
                    Assert.IsTrue(mejora, tipo + " " + nivel + " mejora el daño, el cargador o la recarga");
                }
            }

            var tienda = AssetDatabase.LoadAssetAtPath<DatosTienda>("Assets/_Proyecto/Economia/Datos/DatosTienda_MercadoNegro.asset");
            int ofertasArmas = 0;
            foreach (DatosOferta oferta in tienda.Ofertas)
            {
                ofertasArmas += oferta != null && oferta.Id.StartsWith("oferta_arma_") ? 1 : 0;
                StringAssert.DoesNotContain("subfusil", oferta.Id.ToLowerInvariant());
            }
            Assert.AreEqual(19, ofertasArmas, "4 armas y 15 mejoras");
            Assert.AreEqual(19 + 6, tienda.Ofertas.Count, "Además del kit, las 4 municiones y el brazo");

            DatosArma sniper = catalogo.Buscar(ArmaAPie.Sniper, 0);
            Assert.AreEqual(4, sniper.Cargador, "Sniper base: 4 balas");
            Assert.AreEqual(5, catalogo.Buscar(ArmaAPie.Sniper, 1).Cargador, "SNIPER I: 5 balas");
            foreach (ArmaAPie tipo in System.Enum.GetValues(typeof(ArmaAPie)))
            {
                if (tipo == ArmaAPie.Sniper || tipo == ArmaAPie.Punos)
                {
                    continue;
                }
                DatosArma otra = catalogo.Buscar(tipo, 0);
                Assert.Greater(sniper.Danio, otra.Danio, "El sniper pega más por bala que " + tipo);
                Assert.LessOrEqual(sniper.Cargador, otra.Cargador, "El sniper tiene el menor cargador (o igual) que " + tipo);
            }

            Assert.IsNull(catalogo.Buscar(ArmaAPie.Punos, 0).Oferta, "Los puños no se venden");
            Assert.IsNotNull(catalogo.Buscar(ArmaAPie.Pistola, 0).Oferta, "La pistola base se vende");
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<Object>("Assets/_Proyecto/Personaje/Datos/Armas/DatosArma_subfusil_0.asset"), "No queda el subfusil");
        }
    }
}

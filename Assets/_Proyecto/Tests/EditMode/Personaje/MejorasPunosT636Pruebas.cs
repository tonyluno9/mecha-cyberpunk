using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-636: el CIC vende las mejoras de los puños (I a III), con precio, y cada nivel pega más fuerte.</summary>
    public class MejorasPunosT636Pruebas
    {
        private const string RutaCic = "Assets/_Proyecto/Economia/Datos/DatosTienda_CIC.asset";
        private const string RutaCatalogo = "Assets/_Proyecto/Personaje/Datos/CatalogoArmas_Base.asset";

        [Test]
        public void CicVendeTresNivelesDePunos_ConPrecioCreciente()
        {
            DatosTienda cic = AssetDatabase.LoadAssetAtPath<DatosTienda>(RutaCic);
            Assert.IsNotNull(cic);
            int previo = 0;
            for (int nivel = 1; nivel <= 3; nivel++)
            {
                string id = "oferta_arma_punos_" + nivel;
                DatosOferta oferta = null;
                foreach (DatosOferta o in cic.Ofertas)
                    if (o != null && o.Id == id) oferta = o;
                Assert.IsNotNull(oferta, "El CIC vende " + id);
                Assert.Greater(oferta.Precio, previo, "El precio sube con el nivel");
                previo = oferta.Precio;
            }
        }

        [Test]
        public void DanioDePunosSubeConElNivel()
        {
            CatalogoArmas catalogo = AssetDatabase.LoadAssetAtPath<CatalogoArmas>(RutaCatalogo);
            float anterior = 0f;
            for (int nivel = 0; nivel <= 3; nivel++)
            {
                DatosArma datos = catalogo.Buscar(ArmaAPie.Punos, nivel);
                Assert.IsNotNull(datos);
                Assert.Greater(datos.Danio, anterior, "Puños nivel " + nivel);
                anterior = datos.Danio;
            }
        }
    }
}

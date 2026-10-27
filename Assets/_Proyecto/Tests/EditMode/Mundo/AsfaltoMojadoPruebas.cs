using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T-312: asfalto mojado. Texturas generadas por código (variación tonal y brillo en el alfa), material de charco translúcido,
    /// charcos y líneas de borde sin collider, y el cubo de asfalto igual que antes (la caminabilidad no cambia).
    /// </summary>
    public class AsfaltoMojadoPruebas
    {
        private const string RutaTexturaAsfalto = "Assets/_Proyecto/Mundo/Texturas/Asfalto_Mojado.png";
        private const string RutaTexturaCharco = "Assets/_Proyecto/Mundo/Texturas/Charco_Mascara.png";
        private const string RutaMaterialAsfalto = "Assets/_Proyecto/Mundo/Materiales/Barrio_Asfalto.mat";
        private const string RutaMaterialCharco = "Assets/_Proyecto/Mundo/Materiales/Barrio_Charco.mat";
        private const string RutaPrefabBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const int LadoAsfalto = 512;
        private const int LadoCharco = 128;
        private const float DesviacionTonalMinima = 0.05f;
        private const float DesviacionBrilloMinima = 0.05f;
        private const float BrilloMedioMinimo = 0.6f;
        private const float BrilloPiso = 0.45f;
        private const float AlbedoEfectivoMaximo = 0.06f;
        private const float ToleranciaCostura = 1.5f;
        private const float HolguraCostura = 0.01f;
        private const float AlfaCentroMinimo = 0.95f;
        private const float AlfaEsquinaMaximo = 0.01f;
        private const float SuavidadCharcoMinima = 0.95f;
        private const int CharcosMinimos = 60;
        private const int CharcosMaximos = 80;
        private const int LineasDeBorde = 14;
        private const float MitadBarrioX = 134f;
        private const float MitadBarrioZ = 87f;
        private const float MargenAsfalto = 4f;
        private const float ToleranciaMedida = 0.001f;
        private const float CentroCalleNorteSur = 47f;
        private const float DespejeCebra = 11f;

        private static Texture2D Leer(string ruta)
        {
            Texture2D textura = new Texture2D(2, 2);
            Assert.IsTrue(textura.LoadImage(File.ReadAllBytes(ruta)), "No se pudo leer " + ruta);
            return textura;
        }

        private static float Desviacion(double suma, double sumaCuadrados, double cantidad)
        {
            double media = suma / cantidad;
            return (float)System.Math.Sqrt(sumaCuadrados / cantidad - media * media);
        }

        [Test]
        public void Texturas_ExistenConSuTamano_SinCompresion_YSeRepiten()
        {
            foreach ((string ruta, int lado) in new[] { (RutaTexturaAsfalto, LadoAsfalto), (RutaTexturaCharco, LadoCharco) })
            {
                Texture2D textura = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
                Assert.IsNotNull(textura, "Falta " + ruta);
                Assert.AreEqual(lado, textura.width, ruta + " ancho");
                Assert.AreEqual(lado, textura.height, ruta + " alto");
                TextureImporter importador = (TextureImporter)AssetImporter.GetAtPath(ruta);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importador.textureCompression, ruta);
                Assert.AreEqual(TextureWrapMode.Repeat, importador.wrapMode, ruta);
                Assert.IsTrue(importador.mipmapEnabled, ruta + " necesita mipmaps");
            }
        }

        [Test]
        public void Asfalto_TieneVariacionTonalYDeBrillo_SinSalirseDeLoOscuro()
        {
            Color[] pixeles = Leer(RutaTexturaAsfalto).GetPixels();
            double sumaTono = 0.0, sumaTono2 = 0.0, sumaBrillo = 0.0, sumaBrillo2 = 0.0;
            float brilloMinimo = 1f;
            foreach (Color pixel in pixeles)
            {
                float tono = pixel.grayscale;
                sumaTono += tono;
                sumaTono2 += tono * tono;
                sumaBrillo += pixel.a;
                sumaBrillo2 += pixel.a * pixel.a;
                brilloMinimo = Mathf.Min(brilloMinimo, pixel.a);
            }
            Assert.GreaterOrEqual(Desviacion(sumaTono, sumaTono2, pixeles.Length), DesviacionTonalMinima, "Poca variación tonal");
            Assert.GreaterOrEqual(Desviacion(sumaBrillo, sumaBrillo2, pixeles.Length), DesviacionBrilloMinima, "Poca variación de brillo");
            Assert.GreaterOrEqual((float)(sumaBrillo / pixeles.Length), BrilloMedioMinimo, "El asfalto sigue mojado: brillo medio alto");
            Assert.GreaterOrEqual(brilloMinimo, BrilloPiso, "Ninguna zona queda mate del todo");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialAsfalto);
            float albedoEfectivo = material.GetColor("_BaseColor").grayscale * (float)(sumaTono / pixeles.Length);
            Assert.Less(albedoEfectivo, AlbedoEfectivoMaximo, "El asfalto mojado sigue oscuro (color × textura)");
        }

        [Test]
        public void Asfalto_LaTexturaSeRepiteSinCostura()
        {
            Color[] pixeles = Leer(RutaTexturaAsfalto).GetPixels();
            double saltoBorde = 0.0, saltoInterior = 0.0;
            int mitad = LadoAsfalto / 2;
            for (int y = 0; y < LadoAsfalto; y++)
            {
                saltoBorde += Mathf.Abs(pixeles[y * LadoAsfalto].grayscale - pixeles[y * LadoAsfalto + LadoAsfalto - 1].grayscale);
                saltoInterior += Mathf.Abs(pixeles[y * LadoAsfalto + mitad - 1].grayscale - pixeles[y * LadoAsfalto + mitad].grayscale);
            }
            for (int x = 0; x < LadoAsfalto; x++)
            {
                saltoBorde += Mathf.Abs(pixeles[x].grayscale - pixeles[(LadoAsfalto - 1) * LadoAsfalto + x].grayscale);
                saltoInterior += Mathf.Abs(pixeles[(mitad - 1) * LadoAsfalto + x].grayscale - pixeles[mitad * LadoAsfalto + x].grayscale);
            }
            Assert.LessOrEqual(saltoBorde, saltoInterior * ToleranciaCostura + HolguraCostura * LadoAsfalto, "El borde de la textura no encaja con el opuesto");
        }

        [Test]
        public void MaterialAsfalto_UsaLaTexturaYTomaElBrilloDelAlfa()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialAsfalto);
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTexturaAsfalto), material.GetTexture("_BaseMap"));
            Assert.AreEqual(1f, material.GetFloat("_SmoothnessTextureChannel"), "El brillo sale del alfa del albedo");
            Assert.IsTrue(material.IsKeywordEnabled("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A"));
            Vector2 repeticiones = material.GetTextureScale("_BaseMap");
            Assert.Greater(repeticiones.x, 1f, "La textura se repite a lo largo del barrio");
            Assert.Greater(repeticiones.y, 1f);
        }

        [Test]
        public void MaterialCharco_EsTranslucidoMuyLisoYNoProyectaSombra()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialCharco);
            Assert.AreEqual(1f, material.GetFloat("_Surface"), "Debe ser translúcido");
            Assert.AreEqual(0f, material.GetFloat("_ZWrite"));
            Assert.AreEqual((int)RenderQueue.Transparent, material.renderQueue);
            Assert.GreaterOrEqual(material.GetFloat("_Smoothness"), SuavidadCharcoMinima);
            Assert.IsFalse(material.GetShaderPassEnabled("ShadowCaster"));

            Texture2D mascara = Leer(RutaTexturaCharco);
            Assert.AreEqual(material.GetTexture("_BaseMap"), AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTexturaCharco));
            Assert.GreaterOrEqual(mascara.GetPixel(LadoCharco / 2, LadoCharco / 2).a, AlfaCentroMinimo, "Centro opaco");
            Assert.LessOrEqual(mascara.GetPixel(0, 0).a, AlfaEsquinaMaximo, "Esquina transparente");
        }

        [Test]
        public void Prefab_CharcosYBordes_SinCollider_Y_AsfaltoIntacto()
        {
            GameObject barrio = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabBarrio);
            Transform calles = barrio.transform.Find("Calles");

            Transform charcos = calles.Find("Charcos");
            Assert.GreaterOrEqual(charcos.childCount, CharcosMinimos);
            Assert.LessOrEqual(charcos.childCount, CharcosMaximos);
            Assert.AreEqual(0, charcos.GetComponentsInChildren<Collider>().Length, "Los charcos no tienen collider");

            Transform bordes = calles.Find("BordesCalle");
            Assert.IsNotNull(bordes, "Faltan las líneas de borde");
            Assert.AreEqual(LineasDeBorde, bordes.childCount);
            Assert.AreEqual(0, bordes.GetComponentsInChildren<Collider>().Length, "Las líneas no tienen collider");
            foreach (Transform linea in bordes)
            {
                Vector3 p = linea.localPosition;
                bool enCalleEsteOeste = linea.localScale.x > linea.localScale.z;
                if (enCalleEsteOeste)
                {
                    float extremoCercano = Mathf.Min(Mathf.Abs(Mathf.Abs(p.x - linea.localScale.x / 2f) - CentroCalleNorteSur), Mathf.Abs(Mathf.Abs(p.x + linea.localScale.x / 2f) - CentroCalleNorteSur));
                    Assert.GreaterOrEqual(extremoCercano, DespejeCebra - ToleranciaMedida, linea.name + " invade un paso de cebra");
                }
            }

            Transform asfalto = calles.Find("Asfalto");
            BoxCollider caja = asfalto.GetComponent<BoxCollider>();
            Assert.IsNotNull(caja, "El asfalto sigue teniendo su collider");
            Vector3 tamano = Vector3.Scale(caja.size, asfalto.localScale);
            Assert.AreEqual(2f * MitadBarrioX + MargenAsfalto, tamano.x, ToleranciaMedida);
            Assert.AreEqual(2f * MitadBarrioZ + MargenAsfalto, tamano.z, ToleranciaMedida);
            Assert.AreEqual(0f, asfalto.localPosition.y + tamano.y / 2f, ToleranciaMedida, "La cara de arriba sigue en y = 0");
        }
    }
}

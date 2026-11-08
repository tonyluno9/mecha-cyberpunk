using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.UI.Mapa;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-747: las cuentas de mundo a imagen del mapa, los barrios, el anti-encimado de los nombres (T-721) y que la imagen generada está alineada con la ciudad
    /// (las avenidas del mundo caen en píxeles oscuros y los techos de las manzanas en píxeles más claros).
    /// </summary>
    public class MapaCiudadT747Pruebas
    {
        private const string RutaPng = "Assets/_Proyecto/UI/Resources/Mapa/MapaCiudad.png";

        [Test]
        public void MundoAPixel_YDeVuelta_SonLaMismaCuenta()
        {
            Vector2 mundo = new Vector2(-63f, -77f);
            Vector2 pixel = MapaImagen.MundoAPixel(mundo);
            Assert.AreEqual((-63f + 200f) * MapaImagen.PixelesPorMetro, pixel.x, 0.001f);
            Assert.AreEqual((-77f + 140f) * MapaImagen.PixelesPorMetro, pixel.y, 0.001f);
            Vector2 vuelta = MapaImagen.PixelAMundo(pixel);
            Assert.AreEqual(mundo.x, vuelta.x, 0.001f);
            Assert.AreEqual(mundo.y, vuelta.y, 0.001f);
        }

        [Test]
        public void LasEsquinasDelCuadro_VanALasEsquinasDeLaImagen()
        {
            Assert.AreEqual(Vector2.zero, MapaImagen.MundoAPixel(MapaImagen.Minimo));
            Assert.AreEqual(new Vector2(MapaImagen.AnchoPx, MapaImagen.AltoPx), MapaImagen.MundoAPixel(MapaImagen.Maximo));
            Assert.AreEqual(Vector2.zero, MapaImagen.MundoAUv(MapaImagen.Minimo));
            Assert.AreEqual(Vector2.one, MapaImagen.MundoAUv(MapaImagen.Maximo));
            Assert.AreEqual(new Vector2(0.5f, 0.5f), MapaImagen.MundoAUv(MapaImagen.Centro));
        }

        [Test]
        public void ElCentroDelCuadroEsElOrigenDelMundo_ComoLasCuentasViejasDelMapa()
        {
            // PantallaMapa y Minimapa ponen todo en (x, z) * px/m desde el origen: el centro de la imagen debe ser el origen.
            Assert.AreEqual(Vector2.zero, MapaImagen.Centro);
            Vector2 punto = new Vector2(30f, -12f);
            Assert.AreEqual(punto * 2f, MapaImagen.MundoADibujo(punto, 2f));
        }

        [Test]
        public void LaImagenEntera_LlenaLaVistaDelMapaGrande_ConZoomUno()
        {
            Vector2 px = MapaImagen.Tamano * MapaImagen.PixelesVistaPorMetro;
            Assert.AreEqual(800f, px.x, 0.01f);
            Assert.AreEqual(560f, px.y, 0.01f);
        }

        [Test]
        public void CadaBarrio_CubreSuZona_YTieneSuColor()
        {
            Assert.AreEqual("SUBURBIOS", BarriosMapa.Todos[BarriosMapa.De(new Vector2(-120f, -50f))].Nombre);
            Assert.AreEqual("EL MERCADO", BarriosMapa.Todos[BarriosMapa.De(new Vector2(0f, 40f))].Nombre);
            Assert.AreEqual("NEÓN ALTO", BarriosMapa.Todos[BarriosMapa.De(new Vector2(100f, 60f))].Nombre);
            Assert.AreEqual("NOVA", BarriosMapa.Todos[BarriosMapa.De(new Vector2(100f, -60f))].Nombre);
            // Dentro de un barrio el color es el suyo; cerca del borde se mezcla con el vecino.
            Assert.AreEqual(BarriosMapa.Todos[2].Color, BarriosMapa.ColorEn(new Vector2(150f, 100f)));
            Color borde = BarriosMapa.ColorEn(new Vector2(47f, 60f));
            Assert.AreNotEqual(BarriosMapa.Todos[1].Color, borde);
            Assert.AreNotEqual(BarriosMapa.Todos[2].Color, borde);
        }

        [Test]
        public void LosNombresDeLosBarrios_CaenDentroDeSuBarrioYDelCuadro()
        {
            foreach (BarriosMapa.Barrio b in BarriosMapa.Todos)
            {
                Assert.IsTrue(b.Rectangulo.Contains(b.PuntoNombre), b.Nombre + " va dentro de su barrio");
                Assert.IsTrue(MapaImagen.Contiene(b.PuntoNombre));
            }
        }

        // T-721: el caso de la captura: MECÁNICO y CASA a 26 m (52 px) uno del otro con nombres de unos 100 px.
        [Test]
        public void Nombres_DeIconosVecinos_NoSeEncimanNiTapanIconos()
        {
            Vector2[] iconos = { new Vector2(-37f, -72f) * 2f, new Vector2(-63f, -77f) * 2f, new Vector2(-45f, -60f) * 2f };
            Vector2[] tamanos = { new Vector2(100f, 25f), new Vector2(60f, 25f), new Vector2(90f, 25f) };
            bool[] arriba = { false, false, false };
            bool[] visibles = { true, true, true };
            Vector2[] lugares = CalculoEtiquetasMapa.Colocar(iconos, tamanos, arriba, visibles, new List<Rect>(), 32f);
            List<Rect> cajas = new List<Rect>();
            for (int i = 0; i < iconos.Length; i++)
            {
                cajas.Add(CalculoEtiquetasMapa.Caja(iconos[i] + lugares[i], tamanos[i]));
            }
            for (int i = 0; i < cajas.Count; i++)
            {
                for (int j = i + 1; j < cajas.Count; j++)
                {
                    Assert.AreEqual(0f, CalculoEtiquetasMapa.Traslape(cajas[i], cajas[j]), "Los nombres " + i + " y " + j + " no se traslapan");
                }
                for (int j = 0; j < iconos.Length; j++)
                {
                    Rect icono = CalculoEtiquetasMapa.Caja(iconos[j], Vector2.one * 32f);
                    Assert.AreEqual(0f, CalculoEtiquetasMapa.Traslape(cajas[i], icono), "El nombre " + i + " no tapa el icono " + j);
                }
            }
        }

        [Test]
        public void Nombres_RespetanLoFijo_YLosInvisiblesNoCuentan()
        {
            Vector2[] iconos = { Vector2.zero, new Vector2(10f, 0f) };
            Vector2[] tamanos = { new Vector2(80f, 25f), new Vector2(80f, 25f) };
            bool[] arriba = { false, false };
            bool[] visibles = { true, false };
            // Un nombre de barrio justo debajo del icono: el nombre se va a otro lado.
            Rect fijo = CalculoEtiquetasMapa.Caja(new Vector2(0f, -30f), new Vector2(200f, 30f));
            Vector2[] lugares = CalculoEtiquetasMapa.Colocar(iconos, tamanos, arriba, visibles, new List<Rect> { fijo }, 32f);
            Assert.AreEqual(0f, CalculoEtiquetasMapa.Traslape(CalculoEtiquetasMapa.Caja(iconos[0] + lugares[0], tamanos[0]), fijo));
        }

        [Test]
        public void LaImagenGenerada_EstaAlineadaConLaCiudad()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(RutaPng), "La captura existe y se importa como textura: " + RutaPng);
            // Se lee el PNG directo (la textura importada no es legible).
            Texture2D imagen = new Texture2D(2, 2);
            Assert.IsTrue(imagen.LoadImage(File.ReadAllBytes(RutaPng)));
            Assert.AreEqual(MapaImagen.AnchoPx, imagen.width);
            Assert.AreEqual(MapaImagen.AltoPx, imagen.height);
            // Las avenidas (z = 0 al poniente y x = 47 al norte) son asfalto oscuro; los techos de las manzanas grandes de Suburbios y Neón Alto, más claros.
            float avenida = (Brillo(imagen, new Vector2(-90f, 0f)) + Brillo(imagen, new Vector2(-100f, 0.5f)) + Brillo(imagen, new Vector2(47f, 60f)) + Brillo(imagen, new Vector2(47f, 40f))) / 4f;
            float techos = (Brillo(imagen, new Vector2(-95f, 50f)) + Brillo(imagen, new Vector2(-110f, 30f)) + Brillo(imagen, new Vector2(95f, 50f)) + Brillo(imagen, new Vector2(100f, 30f))) / 4f;
            Assert.Less(avenida, 0.25f, "Las avenidas del mundo caen sobre asfalto oscuro");
            Assert.Greater(techos, avenida * 2f, "Los techos de las manzanas caen sobre píxeles claros");
        }

        private static float Brillo(Texture2D imagen, Vector2 mundo)
        {
            Vector2 px = MapaImagen.MundoAPixel(mundo);
            Color c = imagen.GetPixel(Mathf.Clamp((int)px.x, 0, imagen.width - 1), Mathf.Clamp((int)px.y, 0, imagen.height - 1));
            return c.maxColorComponent;
        }
    }
}

using MechaCyberpunk.Progresion.Recorrido;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Video;

namespace MechaCyberpunk.Tests
{
    /// <summary>T97: tomas de imagen fija en las cinemáticas: su tiempo, su encuadre Ken Burns, los datos mixtos y el lienzo.</summary>
    public class FotosCinematicaT97Pruebas
    {
        [Test]
        public void Duracion_ConYSinNarracion_VozMasPausaOTresSegundos()
        {
            Assert.AreEqual(5.5f, ReglaFotoCinematica.Duracion(4.5f, 1f), 0.001f);
            Assert.AreEqual(ReglaFotoCinematica.SegundosSinNarracion, ReglaFotoCinematica.Duracion(0f, 1f), 0.001f);
            Assert.AreEqual(3f, ReglaFotoCinematica.SegundosSinNarracion, 0.001f);
        }

        [Test]
        public void Avanzar_FundidoCortoYListoAlFinal()
        {
            var regla = new ReglaFotoCinematica(3f);
            regla.Avanzar(ReglaFotoCinematica.SegundosFundido * 0.5f);
            Assert.AreEqual(0.5f, regla.AlfaEntrada, 0.01f);
            Assert.IsFalse(regla.Listo);
            regla.Avanzar(3f);
            Assert.AreEqual(1f, regla.AlfaEntrada, 0.001f);
            Assert.IsTrue(regla.Listo);
            Assert.Less(ReglaFotoCinematica.SegundosFundido, 1f, "Fundido corto, nunca negro largo");
        }

        [Test]
        public void Encuadre_DentroDeLaImagenYSeMueve()
        {
            Rect inicio = ReglaFotoCinematica.Encuadre(0f, 0, 16f / 9f, 16f / 9f);
            Rect fin = ReglaFotoCinematica.Encuadre(1f, 0, 16f / 9f, 16f / 9f);
            foreach (Rect r in new[] { inicio, fin, ReglaFotoCinematica.Encuadre(0.5f, 3, 1.5f, 16f / 9f) })
            {
                Assert.GreaterOrEqual(r.xMin, -0.0001f);
                Assert.GreaterOrEqual(r.yMin, -0.0001f);
                Assert.LessOrEqual(r.xMax, 1.0001f);
                Assert.LessOrEqual(r.yMax, 1.0001f);
            }
            Assert.Greater(inicio.width, fin.width, "La toma par se acerca (zoom)");
            Assert.AreEqual(1f / (1f + ReglaFotoCinematica.ZoomKenBurns), fin.width, 0.001f);
        }

        [Test]
        public void Datos_TomasMixtas_FotoYDuracion()
        {
            var datos = ScriptableObject.CreateInstance<DatosCinematica>();
            var a = new Texture2D(4, 4);
            var b = new Texture2D(4, 4);
            try
            {
                datos.ConfigurarTomas(new VideoClip[2], new[] { a, b }, 0f, string.Empty);
                datos.ConfigurarNarracion(new AudioClip[2], new[] { 1f, 1f });
                Assert.AreEqual(2, datos.Clips.Count, "Cada imagen fija es una toma");
                Assert.IsNull(datos.Clips[0]);
                Assert.AreSame(b, datos.Foto(1));
                Assert.IsTrue(datos.EsFoto(0));
                Assert.AreEqual(2, datos.CantidadFotos);
                Assert.AreEqual(0f, datos.DuracionVideos, 0.001f);
                Assert.AreEqual(6f, datos.DuracionEfectiva, 0.001f, "Dos imágenes sin narración: 3 s cada una");
            }
            finally
            {
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
                Object.DestroyImmediate(datos);
            }
        }

        [Test]
        public void Componer_ImagenDerechaYFundidoSobreLaAnterior()
        {
            // Arriba rojo, abajo azul: el lienzo no debe salir de cabeza.
            var foto = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            foto.SetPixels(new[] { Color.blue, Color.blue, Color.red, Color.red });
            foto.Apply();
            var lienzo = new RenderTexture(8, 8, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var anterior = new RenderTexture(8, 8, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var lectura = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            try
            {
                LienzoFotoCinematica.Componer(lienzo, null, foto, new Rect(0f, 0f, 1f, 1f), 1f);
                Color arriba = Leer(lienzo, lectura, 4, 6);
                Color abajo = Leer(lienzo, lectura, 4, 1);
                Assert.Greater(arriba.r, 0.8f, "Arriba rojo");
                Assert.Greater(abajo.b, 0.8f, "Abajo azul");
                // La anterior (ya compuesta) sale igual de derecha y, con alfa 0, la foto nueva no se ve.
                Graphics.Blit(lienzo, anterior);
                var verde = new Texture2D(1, 1);
                verde.SetPixel(0, 0, Color.green);
                verde.Apply();
                LienzoFotoCinematica.Componer(lienzo, anterior, verde, new Rect(0f, 0f, 1f, 1f), 0f);
                Assert.Greater(Leer(lienzo, lectura, 4, 6).r, 0.8f, "Con alfa 0 se ve la anterior, derecha");
                LienzoFotoCinematica.Componer(lienzo, anterior, verde, new Rect(0f, 0f, 1f, 1f), 0.5f);
                Color mitad = Leer(lienzo, lectura, 4, 6);
                Assert.Greater(mitad.g, 0.3f, "A medio fundido se mezclan");
                Assert.Greater(mitad.r, 0.3f, "A medio fundido se mezclan");
                Object.DestroyImmediate(verde);
            }
            finally
            {
                Object.DestroyImmediate(foto);
                Object.DestroyImmediate(lectura);
                lienzo.Release();
                anterior.Release();
                Object.DestroyImmediate(lienzo);
                Object.DestroyImmediate(anterior);
            }
        }

        // Lee un píxel (x, y con origen abajo a la izquierda) de la RenderTexture.
        private static Color Leer(RenderTexture rt, Texture2D lectura, int x, int y)
        {
            RenderTexture previa = RenderTexture.active;
            RenderTexture.active = rt;
            lectura.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            lectura.Apply();
            RenderTexture.active = previa;
            return lectura.GetPixel(x, y);
        }
    }
}

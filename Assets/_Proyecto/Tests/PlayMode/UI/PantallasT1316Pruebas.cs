using System.Collections;
using System.IO;
using MechaCyberpunk.UI.Historia;
using MechaCyberpunk.UI.Inicio;
using MechaCyberpunk.UI.Titulo;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-1316: ARRESTADO y HAS MUERTO como M33 y M34 (el juego se ve detrás, panel con marco, y se cubre antes de mover al jugador)
    /// y la pantalla de carga (T100: un frame de las cinemáticas por carga). Con CAPTURAR_T1316=1 deja capturas en docs/capturas/t-1316/ (sobre una foto real de
    /// la calle, como se vería encima del juego).
    /// </summary>
    public class PantallasT1316Pruebas
    {
        private const string FotoCalle = "docs/capturas/t920-despues-noche-av-coyote.png";
        private const int Ancho = 1280;
        private const int Alto = 720;

        private static bool Capturar => System.Environment.GetEnvironmentVariable("CAPTURAR_T1316") == "1";

        [UnityTearDown]
        public IEnumerator Limpiar()
        {
            foreach (PantallaAviso a in Object.FindObjectsByType<PantallaAviso>(FindObjectsSortMode.None))
            {
                Object.Destroy(a.gameObject);
            }
            if (PantallaCargando.Actual != null)
            {
                Object.Destroy(PantallaCargando.Actual.gameObject);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Arrestado_SeVeElJuegoDetras_YLuegoSeCubre()
        {
            PantallaAviso aviso = PantallaAviso.Crear("ARRESTADO", "<color=#FF7A7A>-25 %</color>     $ 3,112\nVuelves a casa", new Color(0.25f, 0.55f, 1f));
            aviso.MostrarYa();
            yield return EsperarReal(0.3f);
            Assert.AreEqual("ARRESTADO", aviso.Titulo);
            StringAssert.Contains("3,112", aviso.Detalle);
            Assert.Less(aviso.AlfaFondo, 0.7f, "recién salida, el juego se ve detrás");
            if (Capturar)
            {
                Foto(aviso.GetComponentInChildren<Canvas>(), "arrestado");
            }
            yield return EsperarReal(2.4f);
            Assert.AreEqual(1f, aviso.AlfaFondo, 0.01f, "antes de mover al jugador la pantalla ya cubre todo");
        }

        [UnityTest]
        public IEnumerator HasMuerto_PanelRojoConVineta()
        {
            PantallaAviso aviso = PantallaAviso.Crear("HAS MUERTO", "Despiertas en la clínica del Doc", PantallaAviso.RojoMuerte, 0.85f);
            yield return aviso.Aparecer(0.35f);
            yield return EsperarReal(0.3f);
            Assert.AreEqual(1f, aviso.Visibilidad);
            Assert.Less(aviso.AlfaFondo, 0.7f, "se ve dónde cayó");
            Image vineta = aviso.transform.Find(PantallaAviso.NombreCanvas + "/Vineta").GetComponent<Image>();
            Assert.Greater(vineta.color.a, 0.5f, "orillas rojas (la textura las deja solo en los bordes)");
            Assert.Greater(vineta.color.r, vineta.color.b, "la viñeta es roja");
            if (Capturar)
            {
                Foto(aviso.GetComponentInChildren<Canvas>(), "has-muerto");
            }
        }

        [UnityTest]
        public IEnumerator Carga_UnFrameAlAzarPorCarga_SinRepetir_YCubreLaPantalla()
        {
            // T100: un frame de las cinemáticas por carga, quieto, que cubre el lienzo sin deformarse; la carga siguiente trae otro.
            PantallaCargando pantalla = PantallaCargando.Mostrar();
            yield return null;
            FondoCarga fondo = pantalla.GetComponentInChildren<FondoCarga>();
            Assert.IsNotNull(fondo.Fotos, "hay frame de carga");
            Assert.AreEqual(FramesCarga.Cantidad, fondo.Fotos.Cantidad);
            Texture primera = fondo.Fotos.FotoActual;
            Assert.IsNotNull(primera);
            StringAssert.StartsWith("Carga_C", primera.name, "solo frames de las cinemáticas (no L01-L06)");
            int indicePrimera = fondo.Fotos.Indice;
            Assert.AreEqual(indicePrimera, FotosCarga.UltimaElegida);
            Rect lienzo = ((RectTransform)fondo.Fotos.transform).rect;
            Vector2 t = fondo.Fotos.TamanoFoto;
            Assert.GreaterOrEqual(t.x, lienzo.width - 0.5f, "cubre el ancho");
            Assert.GreaterOrEqual(t.y, lienzo.height - 0.5f, "cubre el alto");
            Assert.AreEqual((float)primera.width / primera.height, t.x / t.y, 1e-3f, "sin deformar");
            if (Capturar)
            {
                Foto(pantalla.GetComponentInChildren<Canvas>(), "carga-1", false);
            }
            yield return EsperarReal(ReglaFotosCarga.SegundosPorFoto + ReglaFotosCarga.SegundosFundido + 0.2f);
            Assert.AreSame(primera, fondo.Fotos.FotoActual, "en una carga se queda el mismo frame");
            Assert.AreEqual(1f, fondo.Fotos.EscalaActual, 1e-4f, "sin zoom");

            for (int i = 0; i < 4; i++)
            {
                Object.Destroy(PantallaCargando.Actual.gameObject);
                yield return null;
                PantallaCargando otra = PantallaCargando.Mostrar();
                yield return null;
                FotosCarga fotos = otra.GetComponentInChildren<FondoCarga>().Fotos;
                Assert.IsNotNull(fotos);
                Assert.AreNotEqual(indicePrimera, fotos.Indice, "la carga siguiente no repite el frame anterior");
                indicePrimera = fotos.Indice;
                if (Capturar && i == 0)
                {
                    Foto(otra.GetComponentInChildren<Canvas>(), "carga-2", false);
                }
            }
        }

        private static IEnumerator EsperarReal(float segundos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            while (Time.realtimeSinceStartup < fin)
            {
                yield return null;
            }
        }

        /// <summary>Renderiza el canvas sobre la foto de la calle (o sobre negro) en docs/capturas/t-1316/.</summary>
        private static void Foto(Canvas canvas, string nombre, bool conCalle = true)
        {
            GameObject go = new GameObject("CamFotoT1316");
            Camera c = go.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = Color.black;
            RenderTexture rt = new RenderTexture(Ancho, Alto, 24);
            c.targetTexture = rt;
            GameObject fondo = null;
            if (conCalle && File.Exists(FotoCalle))
            {
                Texture2D calle = new Texture2D(2, 2);
                calle.LoadImage(File.ReadAllBytes(FotoCalle));
                fondo = new GameObject("FondoCalle");
                Canvas cf = fondo.AddComponent<Canvas>();
                cf.renderMode = RenderMode.ScreenSpaceCamera;
                cf.worldCamera = c;
                cf.planeDistance = 5f;
                cf.sortingOrder = -1000;
                GameObject img = new GameObject("Foto", typeof(RectTransform));
                img.transform.SetParent(fondo.transform, false);
                RawImage ri = img.AddComponent<RawImage>();
                ri.texture = calle;
                RectTransform r = ri.rectTransform;
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = r.offsetMax = Vector2.zero;
            }
            RenderMode modo = canvas.renderMode;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = c;
            canvas.planeDistance = 1f;
            Canvas.ForceUpdateCanvases();
            c.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
            foto.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory("docs/capturas/t-1316");
            File.WriteAllBytes("docs/capturas/t-1316/" + nombre + ".png", foto.EncodeToPNG());
            canvas.renderMode = modo;
            canvas.worldCamera = null;
            c.targetTexture = null;
            Object.Destroy(rt);
            Object.Destroy(go);
            if (fondo != null)
            {
                Object.Destroy(fondo);
            }
        }
    }
}

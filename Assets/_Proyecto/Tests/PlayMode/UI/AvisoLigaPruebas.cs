using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-161: aviso de campeón vencido en el HUD a pie, con el ServicioLigas real.</summary>
    public class AvisoLigaPruebas : BaseHudObjetivo
    {
        private const float TolerAviso = 0.3f;
        private const float EsperaMaxima = 10f;
        private const int Sotano = 1;
        private const int UltimaLiga = 4;
        private const int RivalesPorLiga = 5;
        private const int CampeonesGuardados = 2;
        private const int LigaGuardada = 3;
        private const int PrimerRival = 1;
        private const int Premio = 600;
        private const string RutaCaptura = "docs/capturas/T-161-2.png";
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;

        private GameObject objetoLigas;
        private ServicioLigas ligas;
        private JugadorFalso falso;

        public override void TearDown()
        {
            Object.Destroy(objetoLigas);
            base.TearDown();
        }

        private IEnumerator Preparar(ModoJugador modo = ModoJugador.APie, bool ligasAntes = false)
        {
            if (ligasAntes)
            {
                CrearLigas();
            }
            falso = RegistrarFalso(modo);
            CrearHud();
            yield return Calentar();
            if (!ligasAntes)
            {
                CrearLigas();
            }
            yield return null;
        }

        private void CrearLigas()
        {
            objetoLigas = new GameObject("LigasDePrueba");
            ligas = objetoLigas.AddComponent<ServicioLigas>();
        }

        [UnityTest]
        public IEnumerator Aviso_CampeonDeLaLiga1_DiceSotanoYSeAbreCalleYSeVaALos5Segundos()
        {
            yield return Preparar();
            Assert.IsFalse(hud.GrupoAvisoLiga.activeSelf, "Sin campeón vencido no hay aviso");
            Assert.IsTrue(ligas.Poner(Sotano, RivalesPorLiga, 0));

            Assert.AreEqual(ResultadoVictoria.CampeonVencido, ligas.RegistrarVictoria(Sotano, RivalesPorLiga));
            float inicio = Time.time;
            yield return null;

            Assert.IsTrue(hud.GrupoAvisoLiga.activeSelf);
            Assert.AreEqual("¡CAMPEÓN DE LA LIGA SÓTANO!", hud.TextoAvisoLiga.text);
            Assert.AreEqual("SE ABRE LA LIGA CALLE", hud.TextoAvisoLiga2.text);
            Assert.IsTrue(hud.TextoAvisoLiga2.gameObject.activeSelf);
            Assert.AreEqual(config.ColorAvisoLiga, hud.TextoAvisoLiga.color);

            while (hud.GrupoAvisoLiga.activeSelf && Time.time - inicio < EsperaMaxima)
            {
                yield return null;
            }
            Assert.AreEqual(5f, config.SegundosAvisoLiga, 0.0001f);
            Assert.AreEqual(config.SegundosAvisoLiga, Time.time - inicio, TolerAviso, "El aviso dura 5 s");
        }

        [UnityTest]
        public IEnumerator Aviso_CampeonDeLaUltimaLiga_DiceCampeonDelClandestinoSinSegundaLinea()
        {
            yield return Preparar();
            Assert.IsTrue(ligas.Poner(UltimaLiga, RivalesPorLiga, UltimaLiga - 1));

            Assert.AreEqual(ResultadoVictoria.CampeonClandestino, ligas.RegistrarVictoria(UltimaLiga, RivalesPorLiga));
            yield return null;

            Assert.IsTrue(hud.GrupoAvisoLiga.activeSelf);
            Assert.AreEqual("¡CAMPEÓN DEL CLANDESTINO!", hud.TextoAvisoLiga.text);
            Assert.IsFalse(hud.TextoAvisoLiga2.gameObject.activeSelf, "Sin segunda línea");
        }

        [UnityTest]
        public IEnumerator Aviso_CargarPartidaConDosCampeones_NoMuestraNada()
        {
            yield return Preparar();
            ((IGuardable)ligas).Cargar("{\"liga\":" + LigaGuardada + ",\"siguiente\":" + PrimerRival + ",\"campeones\":" + CampeonesGuardados + "}");
            yield return null;
            yield return null;

            Assert.AreEqual(CampeonesGuardados, ligas.CampeonesVencidos);
            Assert.IsFalse(hud.GrupoAvisoLiga.activeSelf, "Cargar una partida no es vencer a un campeón");
        }

        [UnityTest]
        public IEnumerator Aviso_RevanchaDelCampeon_NoMuestraNada()
        {
            yield return Preparar();
            ligas.Poner(Sotano, RivalesPorLiga, 0);
            ligas.RegistrarVictoria(Sotano, RivalesPorLiga);
            yield return Esperar(config.SegundosAvisoLiga + TolerAviso);
            Assert.IsFalse(hud.GrupoAvisoLiga.activeSelf);

            Assert.AreEqual(ResultadoVictoria.Nada, ligas.RegistrarVictoria(Sotano, RivalesPorLiga));
            yield return null;
            Assert.IsFalse(hud.GrupoAvisoLiga.activeSelf, "Una revancha no avisa");
        }

        [UnityTest]
        public IEnumerator Aviso_ConElJugadorEnElMecha_EsperaYApareceAlBajar()
        {
            yield return Preparar(ModoJugador.Mecha);
            ligas.Poner(Sotano, RivalesPorLiga, 0);
            ligas.RegistrarVictoria(Sotano, RivalesPorLiga);
            yield return Esperar(0.5f);
            Assert.IsFalse(hud.GrupoAvisoLiga.activeSelf, "En el mecha no se ve");

            yield return Esperar(config.SegundosAvisoLiga);
            falso.CambiarModo(ModoJugador.APie);
            float inicio = Time.time;
            yield return null;

            Assert.IsTrue(hud.GrupoAvisoLiga.activeSelf, "Al bajar aparece, aunque hayan pasado más de 5 s");
            Assert.AreEqual("¡CAMPEÓN DE LA LIGA SÓTANO!", hud.TextoAvisoLiga.text);
            while (hud.GrupoAvisoLiga.activeSelf && Time.time - inicio < EsperaMaxima)
            {
                yield return null;
            }
            Assert.AreEqual(config.SegundosAvisoLiga, Time.time - inicio, TolerAviso);
        }

        [UnityTest]
        public IEnumerator Aviso_ServicioRegistradoAntesQueElHud_TambienFunciona()
        {
            yield return Preparar(ModoJugador.APie, true);
            ligas.Poner(Sotano, RivalesPorLiga, 0);
            ligas.RegistrarVictoria(Sotano, RivalesPorLiga);
            yield return null;

            Assert.IsTrue(hud.GrupoAvisoLiga.activeSelf);
        }

        [UnityTest]
        public IEnumerator Aviso_ConAvisoDeDineroYRenta_NoSeEncimanConElDeLiga()
        {
            yield return Preparar();
            ligas.Poner(Sotano, RivalesPorLiga, 0);
            ligas.RegistrarVictoria(Sotano, RivalesPorLiga);
            Registro<ICartera>.Actual?.Abonar(Premio, MotivoDinero.Premio);
            yield return null;

            Assert.IsTrue(hud.GrupoAvisoLiga.activeSelf);
            Rect liga = RectEnPantalla(hud.GrupoAvisoLiga);
            Rect objetivo = RectEnPantalla(hud.GrupoObjetivo);
            Rect renta = RectEnPantalla(hud.GrupoRenta);
            Assert.IsFalse(liga.Overlaps(renta), "No tapa la línea de renta");
            Assert.IsFalse(liga.Overlaps(RectEnPantalla(hud.GrupoAvisoDinero)), "No tapa el aviso de dinero");
            Assert.IsFalse(liga.Overlaps(RectEnPantalla(hud.GrupoDinero)), "No tapa el dinero");
            Assert.IsFalse(liga.Overlaps(objetivo), "No tapa el objetivo");
        }

        [UnityTest]
        public IEnumerator Captura_AvisoDeLiga_VistoEnElHudAPie()
        {
            yield return Preparar();
            ligas.Poner(Sotano, RivalesPorLiga, 0);
            ligas.RegistrarVictoria(Sotano, RivalesPorLiga);
            yield return null;
            Assert.IsTrue(hud.GrupoAvisoLiga.activeSelf);

            // El HUD es de pantalla completa: se pasa a una cámara de prueba para poder renderizarlo a una textura en batchmode.
            GameObject objetoCamara = new GameObject("CamaraDeCaptura");
            Camera camara = objetoCamara.AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.08f, 0.1f, 0.14f);
            Canvas lienzo = hud.GetComponentInParent<Canvas>().rootCanvas;
            lienzo.renderMode = RenderMode.ScreenSpaceCamera;
            lienzo.worldCamera = camara;
            lienzo.planeDistance = 1f;
            RenderTexture destino = new RenderTexture(AnchoCaptura, AltoCaptura, 24);
            camara.targetTexture = destino;
            camara.Render();
            RenderTexture.active = destino;
            Texture2D imagen = new Texture2D(AnchoCaptura, AltoCaptura, TextureFormat.RGB24, false);
            imagen.ReadPixels(new Rect(0f, 0f, AnchoCaptura, AltoCaptura), 0, 0);
            imagen.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory(Path.GetDirectoryName(RutaCaptura));
            File.WriteAllBytes(RutaCaptura, imagen.EncodeToPNG());
            Object.Destroy(imagen);
            Object.Destroy(destino);
            Object.Destroy(objetoCamara);
            Assert.IsTrue(File.Exists(RutaCaptura), "Se guardó la captura");
        }

        private static Rect RectEnPantalla(GameObject grupo)
        {
            Vector3[] esquinas = new Vector3[4];
            ((RectTransform)grupo.transform).GetWorldCorners(esquinas);
            return Rect.MinMaxRect(esquinas[0].x, esquinas[0].y, esquinas[2].x, esquinas[2].y);
        }
    }
}

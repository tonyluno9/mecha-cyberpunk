using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-684: el escudo absorbe el daño antes que la vida, se recarga solo, se puede mejorar por IEscudo y la barra del HUD lo muestra
    /// con un destello al absorber. Capturas con CAPTURAR_S1=1 en docs/capturas/s1t-T-684-*.png.
    /// </summary>
    public class EscudoT684Pruebas : BaseHudObjetivo
    {
        protected override bool QuitarEscudo => false;

        private VidaAPie vida;
        private EscudoJugador escudo;
        private HudEscudo barra;

        private IEnumerator Preparar()
        {
            CrearJugador();
            CrearHud();
            yield return Calentar();
            yield return Esperar(0.2f);
            vida = jugador.GetComponent<VidaAPie>();
            escudo = jugador.GetComponent<EscudoJugador>();
            barra = hud.GetComponent<HudEscudo>();
        }

        private static InfoDanio Bala(float cantidad) => new InfoDanio(cantidad, TipoDanio.Disparo, Vector3.zero, Vector3.forward, null);

        private void Foto(string nombre)
        {
            if (System.Environment.GetEnvironmentVariable("CAPTURAR_S1") != "1")
            {
                return;
            }
            Canvas canvas = hud.GetComponent<Canvas>();
            GameObject go = new GameObject("CamHudS1");
            Camera cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.2f);
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            foto.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory("docs/capturas");
            File.WriteAllBytes("docs/capturas/" + nombre + ".png", foto.EncodeToPNG());
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Object.Destroy(go);
            Object.Destroy(rt);
            Object.Destroy(foto);
        }

        [UnityTest]
        public IEnumerator Escudo_Nace_LlenoYRegistradoComoIEscudo()
        {
            yield return Preparar();
            Assert.IsNotNull(escudo, "VidaAPie crea el escudo");
            Assert.AreSame(escudo, Registro<IEscudo>.Actual);
            Assert.Greater(escudo.Maximo, 0f);
            Assert.AreEqual(escudo.Maximo, escudo.Actual, 0.001f);
        }

        [UnityTest]
        public IEnumerator Danio_PrimeroGastaElEscudo_ydespuesLaVida()
        {
            yield return Preparar();
            float maxEscudo = escudo.Maximo;
            float vidaInicial = vida.Actual;
            vida.RecibirDanio(Bala(10f));
            Assert.AreEqual(maxEscudo - 10f, escudo.Actual, 0.01f);
            Assert.AreEqual(vidaInicial, vida.Actual, 0.001f, "La vida no baja mientras el escudo absorbe");
            vida.RecibirDanio(Bala(maxEscudo));
            Assert.AreEqual(0f, escudo.Actual, 0.001f);
            Assert.AreEqual(vidaInicial - 10f, vida.Actual, 0.01f, "Lo que sobra pasa a la vida");
        }

        [UnityTest]
        public IEnumerator Recarga_DespuesDeUnosSegundosSinDanio_VuelveSola()
        {
            yield return Preparar();
            vida.RecibirDanio(Bala(escudo.Maximo));
            Assert.AreEqual(0f, escudo.Actual, 0.001f);
            yield return Esperar(2f);
            Assert.AreEqual(0f, escudo.Actual, 0.001f, "Durante la espera no recarga");
            yield return Esperar(4f);
            Assert.Greater(escudo.Actual, 1f, "Pasada la espera recarga");
        }

        [UnityTest]
        public IEnumerator Mejora_PorIEscudo_SubeMaximoYRecarga_YSeGuarda()
        {
            yield return Preparar();
            IEscudo contrato = Registro<IEscudo>.Actual;
            float max0 = contrato.Maximo;
            float rec0 = contrato.RecargaPorSegundo;
            contrato.MejorarMaximo(20f);
            contrato.MejorarRecarga(8f);
            Assert.AreEqual(max0 + 20f, contrato.Maximo, 0.001f);
            Assert.AreEqual(rec0 + 8f, contrato.RecargaPorSegundo, 0.001f);
            string texto = ((IGuardable)escudo).Guardar();
            escudo.MejorarMaximo(5f);
            ((IGuardable)escudo).Cargar(texto);
            Assert.AreEqual(max0 + 20f, contrato.Maximo, 0.001f, "Cargar devuelve las mejoras guardadas");
        }

        [UnityTest]
        public IEnumerator Hud_MuestraLaBarraJuntoALaVida_YDestellaAlAbsorber()
        {
            yield return Preparar();
            Assert.IsTrue(barra.Armado, "El HUD armó la barra");
            Assert.IsTrue(barra.Barra.gameObject.activeInHierarchy);
            Assert.AreEqual(1f, barra.Relleno.fillAmount, 0.001f);
            Foto("s1t-T-684-escudo-lleno");

            vida.RecibirDanio(Bala(12f));
            yield return null;
            Assert.AreEqual(escudo.Actual / escudo.Maximo, barra.Relleno.fillAmount, 0.01f);
            Assert.Greater(barra.DestelloRestante, 0f, "Destello al absorber");
            Assert.Greater(barra.Velo.color.a, 0.01f, "Velo azul breve");
            Foto("s1t-T-684-escudo-absorbe");

            yield return Esperar(0.5f);
            Assert.AreEqual(0f, barra.DestelloRestante, 0.001f);
            Assert.AreEqual(0f, barra.Velo.color.a, 0.001f, "El velo se apaga");

            vida.RecibirDanio(Bala(escudo.Maximo + 20f));
            yield return null;
            yield return Esperar(0.5f);
            Assert.AreEqual(0f, barra.Relleno.fillAmount, 0.001f);
            Foto("s1t-T-684-escudo-agotado");
        }

        [UnityTest]
        public IEnumerator Invulnerable_NoRecibeDanioNiDelEscudo()
        {
            yield return Preparar();
            vida.DarInvulnerabilidad(1f);
            float vidaInicial = vida.Actual;
            float escudoInicial = escudo.Actual;
            vida.RecibirDanio(Bala(50f));
            Assert.AreEqual(vidaInicial, vida.Actual, 0.001f);
            Assert.AreEqual(escudoInicial, escudo.Actual, 0.001f);
            yield return Esperar(1.2f);
            vida.RecibirDanio(Bala(5f));
            Assert.Less(escudo.Actual, escudoInicial, "Pasados los segundos vuelve a recibir daño");
        }
    }
}

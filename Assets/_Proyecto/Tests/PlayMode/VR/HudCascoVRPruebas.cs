using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.VR;
using MechaCyberpunk.VR.Menus;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// VR14: ya no hay HUD en la muñeca; el HUD de pantalla de PC pasa a Screen Space Camera sobre la cámara del HUD de casco (que no va al
    /// visor y dibuja a 1920 x 1080), y se ve en un panel curvo pegado a la cabeza que la sigue con un leve retraso.
    /// </summary>
    public class HudCascoVRPruebas : BaseJugadorVR
    {
        private const string NombreCanvas = "CanvasH10_PruebaVR";
        private static readonly Vector3 CabezaCuarto = new Vector3(0f, 1.6f, 0f);
        private const float GradosLado = 20f;

        // Un Canvas Overlay como los del HUD (nombre de HudH10, 1920 x 1080) con la mira de PC como hijo directo.
        private Canvas CrearCanvasHud(out Transform mira)
        {
            var objeto = new GameObject(NombreCanvas, typeof(RectTransform));
            extras.Add(objeto);
            Canvas canvas = objeto.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler escala = objeto.AddComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1920f, 1080f);
            escala.matchWidthOrHeight = 0.5f;
            objeto.AddComponent<GraphicRaycaster>();
            var vida = new GameObject("Vida", typeof(RectTransform), typeof(Image));
            vida.transform.SetParent(objeto.transform, false);
            var rect = (RectTransform)vida.transform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.sizeDelta = new Vector2(300f, 40f);
            var objetoMira = new GameObject("Mira", typeof(RectTransform));
            objetoMira.transform.SetParent(objeto.transform, false);
            mira = objetoMira.transform;
            return canvas;
        }

        private static float AnguloAlCentro(HudCascoVR casco, Transform cabeza)
        {
            return Vector3.Angle(cabeza.forward, casco.Centro - cabeza.position);
        }

        [Test]
        public void Clasificar_NombresRealesDelHud_VanAlCasco()
        {
            foreach (string nombre in new[] { "HudAPie", "CanvasH10", "CanvasMinimapa", "CanvasTerritorio", "CanvasMarcador", "PanelSubtitulos",
                "CanvasAvisoGuardado" })
            {
                Assert.AreEqual(TipoLienzoVR.Casco, ReglaLienzosVR.Clasificar(nombre, 10), nombre + " va al HUD de casco.");
            }
            Assert.AreEqual(TipoLienzoVR.Frente, ReglaLienzosVR.Clasificar("CanvasPausa", 50), "Los menús siguen al frente.");
            Assert.AreEqual(TipoLienzoVR.Frente, ReglaLienzosVR.Clasificar("CanvasCargando", 32000), "La carga sigue de panel.");
            Assert.AreEqual(TipoLienzoVR.PantallaCompleta, ReglaLienzosVR.Clasificar("Fundido", 32767));
            Assert.AreEqual(TipoLienzoVR.Oculto, ReglaLienzosVR.Clasificar("CanvasMiraSniper", 16));
            Assert.AreEqual(TipoLienzoVR.Oculto, ReglaLienzosVR.Clasificar("BarrasVidaEnemigos", 5));
            Assert.IsTrue(ReglaLienzosVR.EsGrupoOcultoCasco("Mira"));
            Assert.IsFalse(ReglaLienzosVR.EsGrupoOcultoCasco("Vida"));
            Assert.AreEqual(ReglaLienzosVR.RadioCasco * ReglaLienzosVR.GradosCasco * Mathf.Deg2Rad * 9f / 16f, ReglaLienzosVR.AltoCasco, 1e-4f,
                "El panel es 16:9 a lo largo del arco.");
        }

        [UnityTest]
        public IEnumerator SinMuneca_YLaCamaraDelCascoNoVaAlVisor()
        {
            yield return ArmarVR();
            yield return Segundos(0.5f);
            Assert.IsNotNull(MenusVR.Actual);
            Assert.IsNull(GameObject.Find(HudMunecaVR.NombreCanvas), "Ya no hay HUD en la muñeca.");
            HudCascoVR casco = MenusVR.Actual.Casco;
            Assert.IsTrue(casco.Existe, "El HUD de casco existe.");
            Assert.IsNotNull(casco.Camara);
            Assert.IsFalse(SistemaVR.VaAlVisor(casco.Camara), "La cámara del HUD no va al visor.");
            Assert.AreSame(casco.Textura, casco.Camara.targetTexture, "Dibuja a su textura.");
            Assert.AreEqual(1920, casco.Textura.width);
            Assert.AreEqual(1080, casco.Textura.height);
            Assert.AreEqual(0f, casco.Camara.backgroundColor.a, "Fondo transparente.");
            Assert.AreNotSame(casco.Camara, Camera.main, "No es la cámara principal.");
        }

        [UnityTest]
        public IEnumerator CanvasDelHud_PasaAScreenSpaceCameraDelCasco_YVuelveAlSalir()
        {
            yield return ArmarVR();
            Canvas canvas = CrearCanvasHud(out Transform mira);
            yield return Segundos(0.7f);
            HudCascoVR casco = MenusVR.Actual.Casco;
            Assert.AreEqual(TipoLienzoVR.Casco, MenusVR.Actual.TipoDe(canvas));
            Assert.AreEqual(RenderMode.ScreenSpaceCamera, canvas.renderMode, "El HUD pasa a Screen Space Camera.");
            Assert.AreSame(casco.Camara, canvas.worldCamera, "Sobre la cámara del casco.");
            Assert.IsTrue(casco.Usa(canvas));
            Rect r = ((RectTransform)canvas.transform).rect;
            Debug.Log("[VR] Canvas del casco: " + r.width.ToString("0") + "x" + r.height.ToString("0") + " px.");
            Assert.AreEqual(1920f, r.width, 2f, "Mismo tamaño que la pantalla de PC.");
            Assert.AreEqual(1080f, r.height, 2f);
            Assert.AreEqual(Vector3.zero, mira.localScale, "La mira de PC no va en el casco.");
            Assert.IsFalse(canvas.GetComponent<GraphicRaycaster>().enabled, "El ratón no le pega.");
            Assert.IsTrue(casco.Visible, "Con HUD y sin menú el casco se ve.");

            ModoVR.Fijar(false);
            yield return Cuadros(3);
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode, "Al salir de VR vuelve a Overlay.");
            Assert.AreEqual(Vector3.one, mira.localScale, "Y la mira vuelve.");
        }

        [UnityTest]
        public IEnumerator Panel_SigueALaCabezaConRetraso()
        {
            yield return ArmarVR();
            CrearCanvasHud(out _);
            yield return Segundos(0.7f);
            HudCascoVR casco = MenusVR.Actual.Casco;
            Transform cabeza = adaptador.Camara.transform;
            Assert.IsTrue(casco.Visible);
            Assert.Less(AnguloAlCentro(casco, cabeza), 2f, "El centro del panel está frente a la cabeza.");
            Assert.AreEqual(ReglaLienzosVR.RadioCasco, Vector3.Distance(casco.Centro, cabeza.position), 0.05f, "A 1.3 m.");

            Vector3 antes = cabeza.forward;
            PonerCabeza(CabezaCuarto, Quaternion.Euler(0f, 90f, 0f));
            yield return Cuadros(3);
            Assert.Greater(Vector3.Angle(antes, cabeza.forward), 60f, "La cabeza giró.");
            yield return Segundos(0.5f);
            float angulo = AnguloAlCentro(casco, cabeza);
            Debug.Log("[VR] Tras girar 90° y 0.5 s, el centro del panel queda a " + angulo.ToString("0.0") + "° de la vista.");
            Assert.Less(angulo, 3f, "Tras 0.5 s el panel vuelve a quedar frente a la cabeza.");
            Assert.Less(Vector3.Distance(casco.Panel.position, cabeza.position), 0.02f, "La posición va pegada a la cabeza.");
        }

        [UnityTest]
        public IEnumerator LaserSobreElPanel_DaElPuntoDelCanvas()
        {
            yield return ArmarVR();
            Canvas canvas = CrearCanvasHud(out _);
            yield return Segundos(0.7f);
            HudCascoVR casco = MenusVR.Actual.Casco;
            Transform cabeza = adaptador.Camara.transform;
            var rect = (RectTransform)canvas.transform;

            Assert.IsTrue(casco.PuntoEnHud(new Ray(cabeza.position, cabeza.forward), out Vector3 centro), "Mirando al frente toca el panel.");
            Vector3 local = rect.InverseTransformPoint(centro);
            Assert.AreEqual(rect.rect.center.x, local.x, 10f, "El centro del panel es el centro del Canvas.");
            Assert.AreEqual(rect.rect.center.y, local.y, 10f);

            Vector3 lado = Quaternion.AngleAxis(GradosLado, cabeza.up) * cabeza.forward;
            Assert.IsTrue(casco.PuntoEnHud(new Ray(cabeza.position, lado), out Vector3 derecha));
            float esperado = GradosLado / ReglaLienzosVR.GradosCasco * ReglaLienzosVR.AnchoReferencia;
            Assert.AreEqual(esperado, rect.InverseTransformPoint(derecha).x - rect.rect.center.x, 20f, "20° a la derecha = 20/70 del ancho.");

            Assert.IsFalse(casco.PuntoEnHud(new Ray(cabeza.position, -cabeza.forward), out _), "Hacia atrás no toca el panel.");
        }

        [UnityTest]
        public IEnumerator ConMenuAbierto_ElCascoSeEsconde()
        {
            yield return ArmarVR();
            CrearCanvasHud(out _);
            yield return Segundos(0.7f);
            HudCascoVR casco = MenusVR.Actual.Casco;
            Assert.IsTrue(casco.Visible);
            Pausa.Fijar(true);
            yield return Cuadros(3);
            Assert.IsTrue(MenusVR.MenuAbierto);
            Assert.IsFalse(casco.Visible, "La pausa (y los menús) no quedan tapados por el HUD.");
            Pausa.Fijar(false);
            yield return Cuadros(3);
            Assert.IsTrue(casco.Visible, "Al cerrar vuelve.");
        }
    }
}

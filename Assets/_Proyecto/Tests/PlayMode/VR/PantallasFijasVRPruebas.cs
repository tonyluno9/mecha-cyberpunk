using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.MenuPausa;
using MechaCyberpunk.VR.Menus;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// VR29: las pantallas completas de PC con orden alto (mostrador del Doc, tienda de Tino, máquinas...) van al panel fijo frente a la cabeza,
    /// no pegadas a ella; solo los fundidos van pegados. B cierra la pantalla de arriba (la pausa) por su Esc de PC.
    /// VR25: los degradados negros de las esquinas del H10 no van en el HUD de casco.
    /// </summary>
    public class PantallasFijasVRPruebas : BaseJugadorVR
    {
        [Test]
        public void Clasificar_PantallasDeOrdenAlto_VanAlPanelFijo()
        {
            Assert.AreEqual(TipoLienzoVR.Frente, ReglaLienzosVR.Clasificar("PantallaClinica", 30000), "El Doc va al panel fijo.");
            Assert.AreEqual(TipoLienzoVR.Frente, ReglaLienzosVR.Clasificar("PantallaTienda", 30000), "Tino y las máquinas van al panel fijo.");
            Assert.AreEqual(TipoLienzoVR.Frente, ReglaLienzosVR.Clasificar("PantallaPiezasTaller", 30001));
            Assert.AreEqual(TipoLienzoVR.PantallaCompleta, ReglaLienzosVR.Clasificar("Fundido", short.MaxValue));
            Assert.AreEqual(TipoLienzoVR.PantallaCompleta, ReglaLienzosVR.Clasificar("CanvasFundidoCampoTiro", 31000));
            Assert.AreEqual(TipoLienzoVR.PantallaCompleta, ReglaLienzosVR.Clasificar("NegroMomentoDia", 30002));
            Assert.AreEqual(TipoLienzoVR.Frente, ReglaLienzosVR.Clasificar("CanvasCargando", 32000));
            Assert.Greater(ReglaLienzosVR.Adelanto(30000, 1f), ReglaLienzosVR.Adelanto(120, 1f), "Las de orden alto quedan delante de los menús.");
            Assert.Greater(ReglaLienzosVR.Adelanto(30100, 1f), ReglaLienzosVR.Adelanto(30000, 1f));
            Assert.IsTrue(ReglaLienzosVR.EsSombraCasco("VinetaArribaIzquierda"));
            Assert.IsFalse(ReglaLienzosVR.EsSombraCasco("Vinieta"), "La viñeta roja de vida baja se queda.");
        }

        [UnityTest]
        public IEnumerator PantallaDeOrdenAlto_VaAlPanelFrenteALaCabeza()
        {
            yield return ArmarVR();
            var objeto = new GameObject("PantallaClinica", typeof(RectTransform));
            extras.Add(objeto);
            Canvas canvas = objeto.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            objeto.AddComponent<CanvasScaler>();
            objeto.AddComponent<GraphicRaycaster>();
            yield return Segundos(0.7f);
            Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode);
            Assert.AreEqual(TipoLienzoVR.Frente, MenusVR.Actual.TipoDe(canvas));
            float distancia = (canvas.transform.position - adaptador.Camara.transform.position).magnitude;
            Assert.AreEqual(ReglaLienzosVR.DistanciaFrente, distancia, 0.25f, "A la distancia del panel de menús, no pegada a la cara.");
            float ancho = ((RectTransform)canvas.transform).rect.width * canvas.transform.lossyScale.x;
            Assert.AreEqual(ReglaLienzosVR.AnchoFrente, ancho, 0.3f, "Del ancho del panel de menús: se ve completa.");
        }

        [UnityTest]
        public IEnumerator BotonB_CierraLaPausa()
        {
            yield return ArmarVR();
            var objeto = new GameObject("PantallaPausaPruebaVR");
            extras.Add(objeto);
            PantallaPausa pausa = objeto.AddComponent<PantallaPausa>();
            yield return Segundos(0.6f);
            pausa.Abrir();
            yield return Cuadros(6);
            Assert.IsTrue(pausa.Abierta);
            yield return Pulsar(controlDer.secondaryButton);
            yield return Cuadros(4);
            Assert.IsFalse(pausa.Abierta, "B = Esc: cierra la pausa.");
            Assert.IsFalse(Pausa.Activa);
        }
    }
}

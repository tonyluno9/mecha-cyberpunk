using System.Collections;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.VR.Menus;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// VR43: la vista 3D de una pantalla (el cuerpo del Doc, el arma de Tino) es un set lejos de la ciudad que cuelga del Canvas de la
    /// pantalla. En VR ese Canvas es un panel del mundo de ~0.0008 m/px: el set quedaba microscópico y su cámara no veía nada. Al abrir la
    /// pantalla, el set sale del Canvas con escala 1 en su lugar; lo que está pegado a la pantalla no se mueve.
    /// </summary>
    public class MenusVR43Pruebas : BaseJugadorVR
    {
        private static readonly Vector3 PosicionSet = new Vector3(40f, -500f, 0f);

        [UnityTest]
        public IEnumerator VistaDeUnaPantalla_ElSetSaleDelCanvasConEscalaUno()
        {
            yield return ArmarVR();
            var objeto = new GameObject("PantallaClinicaPruebaVR43", typeof(RectTransform));
            extras.Add(objeto);
            Canvas canvas = objeto.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            objeto.AddComponent<CanvasScaler>();
            objeto.AddComponent<GraphicRaycaster>();
            yield return Segundos(0.7f);
            Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode);
            Assert.Less(canvas.transform.lossyScale.x, 0.01f, "En VR el Canvas es un panel con escala de metros por px.");

            // Como VistaHologramaCuerpo.Mostrar: el set cuelga del objeto de la pantalla y se pone lejos; su cámara dibuja a una textura.
            Transform set = new GameObject("SetPruebaVR43").transform;
            set.SetParent(objeto.transform, false);
            set.position = PosicionSet;
            var textura = new RenderTexture(64, 64, 16);
            var objetoCamara = new GameObject("CamaraPruebaVR43");
            objetoCamara.transform.SetParent(set, false);
            objetoCamara.transform.localPosition = new Vector3(0f, 1f, -3f);
            Camera camara = objetoCamara.AddComponent<Camera>();
            camara.targetTexture = textura;
            // Una vista pegada a la pantalla (no puesta aparte): esa no se toca.
            Transform pegado = new GameObject("PegadoPruebaVR43").transform;
            pegado.SetParent(objeto.transform, false);
            var texturaPegada = new RenderTexture(16, 16, 0);
            Camera camaraPegada = new GameObject("CamaraPegadaVR43").AddComponent<Camera>();
            camaraPegada.transform.SetParent(pegado, false);
            camaraPegada.targetTexture = texturaPegada;

            var pantalla = new GameObject("PantallaPruebaVR43");
            extras.Add(pantalla);
            PilaPantallas.Abrir(pantalla);
            yield return Cuadros(3);

            Assert.IsNull(set.parent, "El set sale del Canvas del panel.");
            Assert.AreEqual(1f, set.lossyScale.x, 1e-4f, "Con escala 1, como en PC.");
            Assert.Less(Vector3.Distance(PosicionSet, set.position), 0.01f, "En su lugar lejos de la ciudad.");
            Assert.AreEqual(Quaternion.identity, set.rotation);
            Assert.AreEqual(set, camara.transform.parent, "La cámara sigue en su set.");
            Assert.AreEqual(objeto.transform, pegado.parent, "Lo pegado a la pantalla no se mueve.");

            PilaPantallas.Cerrar(pantalla);
            Object.Destroy(set.gameObject);
            Object.Destroy(pegado.gameObject);
            camara.targetTexture = null;
            camaraPegada.targetTexture = null;
            Object.Destroy(textura);
            Object.Destroy(texturaPegada);
        }
    }
}

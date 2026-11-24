using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.VR.Menus;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// VR44: en todos los paneles el botón bajo el láser se elige y se marca, el más cercano atrae al láser aunque caiga un poco fuera
    /// (magnetismo), el gatillo hace clic solo en el elegido, y el láser se detiene en el panel con un punto donde toca.
    /// </summary>
    public class MenusVR44Pruebas : BaseJugadorVR
    {
        private static readonly Vector3 ManoDerechaCuarto = new Vector3(0.2f, 1.2f, 0.3f);
        private const float AnchoBoton = 300f;
        private const float AltoBoton = 150f;
        private const float XBoton = 400f;
        private int clicsA;
        private int clicsB;

        [Test]
        public void Elegir_DirectoGana_ElMasCercanoAtrae_LejosNada()
        {
            var lista = new List<ReglaSeleccionVR.Candidato>
            {
                new ReglaSeleccionVR.Candidato(30f, 0, 0, 10000f),
                new ReglaSeleccionVR.Candidato(10f, 0, 0, 10000f),
                new ReglaSeleccionVR.Candidato(200f, 0, 0, 10000f),
            };
            Assert.AreEqual(2, ReglaSeleccionVR.Elegir(lista, 2, -1), "Lo que toca el láser de verdad gana.");
            Assert.AreEqual(1, ReglaSeleccionVR.Elegir(lista, -1, -1), "Sin toque directo, el más cercano dentro del radio.");
            var lejos = new List<ReglaSeleccionVR.Candidato> { new ReglaSeleccionVR.Candidato(ReglaSeleccionVR.RadioImanPx + 1f, 0, 0, 100f) };
            Assert.AreEqual(-1, ReglaSeleccionVR.Elegir(lejos, -1, -1), "Fuera del radio del imán no se elige nada.");
        }

        [Test]
        public void Elegir_ElDeAntesSeQuedaSiElOtroApenasEstaMasCerca()
        {
            var lista = new List<ReglaSeleccionVR.Candidato>
            {
                new ReglaSeleccionVR.Candidato(20f, 0, 0, 10000f),
                new ReglaSeleccionVR.Candidato(20f - ReglaSeleccionVR.HisteresisPx * 0.5f, 0, 0, 10000f),
            };
            Assert.AreEqual(0, ReglaSeleccionVR.Elegir(lista, -1, 0), "Sin temblar entre dos botones juntos.");
            lista[1] = new ReglaSeleccionVR.Candidato(0f, 0, 0, 10000f);
            Assert.AreEqual(1, ReglaSeleccionVR.Elegir(lista, -1, 0), "Si el otro está claramente más cerca, cambia.");
        }

        [Test]
        public void Elegir_LosFondosNoAtraen()
        {
            float pantalla = ReglaLienzosVR.AnchoReferencia * ReglaLienzosVR.AltoReferencia;
            var lista = new List<ReglaSeleccionVR.Candidato> { new ReglaSeleccionVR.Candidato(0f, 0, 0, pantalla) };
            Assert.IsTrue(ReglaSeleccionVR.EsFondo(pantalla));
            Assert.AreEqual(-1, ReglaSeleccionVR.Elegir(lista, -1, -1), "Un fondo a pantalla completa no es un botón al que se atraiga.");
            Assert.AreEqual(0f, ReglaSeleccionVR.DistanciaARect(new Vector2(5f, 5f), new Rect(0f, 0f, 10f, 10f)));
            Assert.AreEqual(5f, ReglaSeleccionVR.DistanciaARect(new Vector2(15f, 5f), new Rect(0f, 0f, 10f, 10f)), 1e-4f);
        }

        [UnityTest]
        public IEnumerator CercaDelBoton_SeEligeSeMarcaYElGatilloLoPulsa()
        {
            yield return ArmarVR();
            Canvas canvas = CrearPanel(out Button a, out Button b);
            var pantalla = new GameObject("PantallaPruebaVR44");
            extras.Add(pantalla);
            yield return Segundos(0.7f);
            PilaPantallas.Abrir(pantalla);
            yield return Cuadros(3);
            Assert.IsTrue(MenusVR.MenuAbierto);

            // Un poco fuera del borde izquierdo del botón A (lejos de B): el imán lo elige.
            float metrosPorPx = canvas.transform.lossyScale.x;
            Vector3 borde = a.transform.position - canvas.transform.right * ((AnchoBoton * 0.5f + 20f) * metrosPorPx);
            yield return ApuntarLaserA(canvas, borde);
            SeleccionLaserVR seleccion = MenusVR.Actual.Seleccion;
            Assert.AreEqual(a.gameObject, seleccion.Elegido, "El láser fuera del botón por poco lo elige (magnetismo).");
            Assert.IsTrue(MenusVR.UIApuntada);
            Assert.IsTrue(seleccion.MarcoVisible, "El elegido se marca con el marco cian.");
            Assert.IsTrue(seleccion.PuntoVisible, "El punto del láser se ve donde toca.");
            float distancia = Vector3.Distance(MenusVR.Actual.ManoLaser.position, borde);
            Assert.AreEqual(distancia, MenusVR.Actual.LargoLaser, 0.1f, "El láser se detiene en el panel.");

            yield return Pulsar(controlDer.triggerButton);
            yield return Cuadros(2);
            Assert.AreEqual(1, clicsA, "El gatillo hace clic en el elegido.");
            Assert.AreEqual(0, clicsB);

            // Al pasar al otro botón, cambia el elegido (y vibraría).
            int cambios = seleccion.Cambios;
            yield return ApuntarLaserA(canvas, b.transform.position);
            Assert.AreEqual(b.gameObject, seleccion.Elegido);
            Assert.Greater(seleccion.Cambios, cambios, "Pasar de botón cuenta como cambio (vibración leve).");
            PilaPantallas.Cerrar(pantalla);
        }

        [UnityTest]
        public IEnumerator EnUnaZonaVacia_ElLaserSeDetieneEnElPanelYElGatilloNoPulsaNada()
        {
            yield return ArmarVR();
            Canvas canvas = CrearPanel(out Button a, out _);
            var pantalla = new GameObject("PantallaPruebaVR44b");
            extras.Add(pantalla);
            yield return Segundos(0.7f);
            PilaPantallas.Abrir(pantalla);
            yield return Cuadros(3);
            float metrosPorPx = canvas.transform.lossyScale.x;
            Vector3 vacio = canvas.transform.position + canvas.transform.up * (380f * metrosPorPx);
            yield return ApuntarLaserA(canvas, vacio);
            Assert.IsNull(MenusVR.Actual.Seleccion.Elegido, "Lejos de todo botón no se elige nada.");
            Assert.IsTrue(MenusVR.Actual.LaserVisible, "Con un menú abierto el láser siempre se ve.");
            float distancia = Vector3.Distance(MenusVR.Actual.ManoLaser.position, vacio);
            Assert.AreEqual(distancia, MenusVR.Actual.LargoLaser, 0.1f, "El láser no atraviesa el panel aunque ahí no haya botón.");
            Assert.IsTrue(MenusVR.Actual.Seleccion.PuntoVisible);
            yield return Pulsar(controlDer.triggerButton);
            yield return Cuadros(2);
            Assert.AreEqual(0, clicsA + clicsB, "El gatillo solo hace clic en lo resaltado.");
            PilaPantallas.Cerrar(pantalla);
        }

        // Un Canvas Overlay como los del kit (1920x1080) sin fondo que reciba el rayo, con dos botones a los lados.
        private Canvas CrearPanel(out Button a, out Button b)
        {
            clicsA = 0;
            clicsB = 0;
            var objeto = new GameObject("CanvasPruebaVR44", typeof(RectTransform));
            extras.Add(objeto);
            Canvas canvas = objeto.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            CanvasScaler escala = objeto.AddComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1920f, 1080f);
            objeto.AddComponent<GraphicRaycaster>();
            a = CrearBoton(objeto.transform, "BotonA", -XBoton);
            b = CrearBoton(objeto.transform, "BotonB", XBoton);
            a.onClick.AddListener(() => clicsA++);
            b.onClick.AddListener(() => clicsB++);
            return canvas;
        }

        private static Button CrearBoton(Transform padre, string nombre, float x)
        {
            var objeto = new GameObject(nombre, typeof(RectTransform), typeof(Image), typeof(Button));
            objeto.transform.SetParent(padre, false);
            var rect = (RectTransform)objeto.transform;
            rect.sizeDelta = new Vector2(AnchoBoton, AltoBoton);
            rect.anchoredPosition = new Vector2(x, 0f);
            return objeto.GetComponent<Button>();
        }

        // Gira el control derecho hasta que el rayo del láser (su pose de apuntar) caiga en el punto del panel.
        private IEnumerator ApuntarLaserA(Canvas canvas, Vector3 punto)
        {
            Vector3 objetivo = punto;
            var plano = new Plane(canvas.transform.forward, canvas.transform.position);
            for (int i = 0; i < 4; i++)
            {
                Vector3 desde = adaptador.ManoDerecha.transform.position;
                Quaternion mundo = Quaternion.LookRotation(objetivo - desde);
                PonerMano(true, ManoDerechaCuarto, Quaternion.Inverse(adaptador.Rig.rotation) * mundo);
                yield return Cuadros(3);
                Transform laser = MenusVR.Actual.ManoLaser;
                if (laser == null)
                {
                    break;
                }
                var rayo = new Ray(laser.position, laser.forward);
                if (!plano.Raycast(rayo, out float d))
                {
                    break;
                }
                objetivo += punto - rayo.GetPoint(d);
            }
            yield return Cuadros(3);
        }
    }
}

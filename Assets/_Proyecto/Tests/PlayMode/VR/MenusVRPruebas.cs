using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.UI.MenuPausa;
using MechaCyberpunk.VR.Menus;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// VR4: los Canvas de pantalla pasan a un panel frente a la cabeza, el láser del control derecho con el gatillo pulsa botones,
    /// el botón Menú del control izquierdo abre y cierra la pausa real, (VR21) sí se sube a carros en VR y (VR14) ya no hay HUD en la muñeca: va en el casco (HudCascoVRPruebas).
    /// </summary>
    public class MenusVRPruebas : BaseJugadorVR
    {
        private const string RutaCarro = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";
        private static readonly Vector3 ManoDerechaCuarto = new Vector3(0.2f, 1.2f, 0.3f);
        private int clics;

        // Un Canvas Overlay como los del kit (1920x1080) con un botón de 400x200 px en el centro.
        private Canvas CrearCanvasPrueba(out Button boton)
        {
            var objeto = new GameObject("CanvasPruebaVR", typeof(RectTransform));
            extras.Add(objeto);
            Canvas canvas = objeto.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            CanvasScaler escala = objeto.AddComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1920f, 1080f);
            objeto.AddComponent<GraphicRaycaster>();
            var objetoBoton = new GameObject("BotonPruebaVR", typeof(RectTransform), typeof(Image), typeof(Button));
            objetoBoton.transform.SetParent(objeto.transform, false);
            ((RectTransform)objetoBoton.transform).sizeDelta = new Vector2(400f, 200f);
            boton = objetoBoton.GetComponent<Button>();
            boton.onClick.AddListener(() => clics++);
            return canvas;
        }

        // Gira el control derecho (en su lugar de siempre) para que apunte a un punto del mundo.
        private IEnumerator ApuntarA(Vector3 punto)
        {
            PonerMano(true, ManoDerechaCuarto, Quaternion.identity);
            yield return Cuadros(2);
            Vector3 desde = adaptador.ManoDerecha.transform.position;
            Quaternion mundo = Quaternion.LookRotation(punto - desde);
            PonerMano(true, ManoDerechaCuarto, Quaternion.Inverse(adaptador.Rig.rotation) * mundo);
            yield return Cuadros(4);
        }

        // En pausa el tiempo del juego está en 0: se espera con el tiempo real.
        private static IEnumerator SegundosReales(float s)
        {
            float fin = Time.unscaledTime + s;
            while (Time.unscaledTime < fin)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CanvasDePantalla_PasaAPanelFrenteALaCabeza()
        {
            yield return ArmarVR();
            Assert.IsNotNull(MenusVR.Actual, "MenusVR se crea solo con VR activo.");
            Canvas canvas = CrearCanvasPrueba(out _);
            yield return Segundos(0.7f);
            Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode, "El Canvas Overlay pasa a World Space.");
            Assert.AreEqual(TipoLienzoVR.Frente, MenusVR.Actual.TipoDe(canvas));
            Assert.IsFalse(canvas.GetComponent<GraphicRaycaster>().enabled, "El ratón ya no le pega al panel del mundo.");
            Transform cabeza = adaptador.Camara.transform;
            Vector3 hacia = canvas.transform.position - cabeza.position;
            Vector3 frente = cabeza.forward;
            frente.y = 0f;
            Debug.Log("[VR] Panel a " + hacia.magnitude.ToString("0.00") + " m de la cabeza; ancho " +
                (((RectTransform)canvas.transform).rect.width * canvas.transform.lossyScale.x).ToString("0.00") + " m.");
            Assert.AreEqual(ReglaLienzosVR.DistanciaFrente, hacia.magnitude, 0.12f, "A 1.5 m de la cabeza.");
            Assert.Greater(Vector3.Dot(frente.normalized, hacia.normalized), 0.95f, "Enfrente de la cabeza.");
            Assert.Greater(Vector3.Dot(canvas.transform.forward, hacia.normalized), 0.95f, "De cara a la cabeza (se lee derecho).");
            float ancho = ((RectTransform)canvas.transform).rect.width * canvas.transform.lossyScale.x;
            Assert.AreEqual(ReglaLienzosVR.AnchoFrente, ancho, 0.1f, "1920 px ≈ 1.6 m.");
        }

        [UnityTest]
        public IEnumerator ApuntarAlBotonYGatillo_LoPulsa()
        {
            yield return ArmarVR();
            clics = 0;
            CrearCanvasPrueba(out Button boton);
            yield return Segundos(0.7f);
            Assert.IsFalse(MenusVR.Actual.LaserVisible, "Sin menú y sin apuntar a la UI el láser no se ve.");
            yield return ApuntarA(boton.transform.position);
            Assert.IsTrue(MenusVR.UIApuntada, "El láser apunta al botón.");
            Assert.IsTrue(MenusVR.BloqueaDisparo, "Apuntando a la UI el gatillo no dispara.");
            Assert.IsTrue(MenusVR.Actual.LaserVisible, "Apuntando a la UI el láser se ve.");
            yield return Pulsar(controlDer.triggerButton);
            yield return Cuadros(2);
            Assert.AreEqual(1, clics, "El gatillo derecho hace clic en el botón.");

            // Apuntando lejos del panel, el gatillo no hace clic.
            yield return ApuntarA(boton.transform.position + Vector3.up * 3f);
            Assert.IsFalse(MenusVR.UIApuntada);
            yield return Pulsar(controlDer.triggerButton);
            yield return Cuadros(2);
            Assert.AreEqual(1, clics, "Fuera del botón no hay clic.");
        }

        [UnityTest]
        public IEnumerator BotonMenuIzquierdo_AbreYCierraLaPausaReal()
        {
            yield return ArmarVR();
            var objeto = new GameObject("PantallaPausaPruebaVR");
            extras.Add(objeto);
            PantallaPausa pausa = objeto.AddComponent<PantallaPausa>();
            yield return Segundos(0.6f);
            Assert.IsFalse(pausa.Abierta);
            yield return Pulsar(controlIzq.menuButton);
            yield return Cuadros(4);
            Assert.IsTrue(pausa.Abierta, "Menú izquierdo = Esc: abre la pausa.");
            Assert.IsTrue(Pausa.Activa);
            Assert.IsTrue(MenusVR.MenuAbierto);
            Assert.IsTrue(MenusVR.BloqueaDisparo, "Con la pausa abierta el gatillo no dispara.");
            Assert.IsTrue(MenusVR.Actual.LaserVisible, "Con un menú abierto el láser se ve.");
            Canvas canvas = pausa.GetComponentInChildren<Canvas>(true);
            Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode, "La pausa va en un panel del mundo.");
            Vector3 hacia = canvas.transform.position - adaptador.Camara.transform.position;
            Assert.AreEqual(ReglaLienzosVR.DistanciaFrente, hacia.magnitude, 0.15f);

            // CONTINUAR con el láser y el gatillo (cuando ya terminó la animación de entrada de los paneles).
            yield return SegundosReales(0.8f);
            yield return ApuntarA(pausa.Continuar.transform.position);
            Assert.IsTrue(MenusVR.UIApuntada, "El láser apunta a CONTINUAR.");
            yield return Pulsar(controlDer.triggerButton);
            yield return Cuadros(4);
            Assert.IsFalse(pausa.Abierta, "CONTINUAR con el gatillo cierra la pausa.");
            Assert.IsFalse(Pausa.Activa);

            // Y otra vez con el botón Menú: abre y cierra.
            yield return Pulsar(controlIzq.menuButton);
            yield return Cuadros(4);
            Assert.IsTrue(pausa.Abierta);
            yield return Pulsar(controlIzq.menuButton);
            yield return Cuadros(4);
            Assert.IsFalse(pausa.Abierta, "Menú otra vez = Esc: cierra la pausa.");
        }

        [UnityTest]
        public IEnumerator SubirAUnCarro_SubeYNoLoBaja()
        {
            yield return ArmarVR();
            GameObject carro = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro), new Vector3(0f, 0f, 4f), Quaternion.identity);
            extras.Add(carro);
            yield return Segundos(0.8f);
            IMontable montable = null;
            foreach (IMontable m in Registro<IMontable>.Todos)
            {
                if (m is Component c && c.transform.IsChildOf(carro.transform))
                {
                    montable = m;
                }
            }
            Assert.IsNotNull(montable, "El carro se registra.");
            int subidas = 0;
            jugadorAPie.ModoCambio += modo =>
            {
                if (modo == ModoJugador.Carro)
                {
                    subidas++;
                }
            };
            jugadorAPie.Colocar(new Pose(montable.PuntoSubida + new Vector3(-0.6f, 0.05f, 0f), Quaternion.LookRotation(Vector3.right)));
            InteraccionAPie interaccion = jugador.GetComponent<InteraccionAPie>();
            float fin = Time.time + 1.5f;
            while (interaccion.MontableCandidato == null && Time.time < fin)
            {
                yield return null;
            }
            Assert.IsNotNull(interaccion.MontableCandidato, "El carro es el candidato de B.");
            yield return Pulsar(controlDer.secondaryButton);
            yield return Segundos(0.5f);
            // VR21: en VR ya se maneja: sube, no sale "NO DISPONIBLE EN VR" y MenusVR ya no lo baja.
            Assert.AreEqual(ModoJugador.Carro, jugadorAPie.Modo, "En VR se maneja: B sube al carro.");
            Assert.AreSame(montable, jugadorAPie.MontableActual);
            Assert.AreEqual(1, subidas);
            Assert.IsFalse(MenusVR.Actual.AvisoVisible && MenusVR.Actual.TextoAviso == MenusVR.TextoNoDisponible, "Ya no sale NO DISPONIBLE EN VR.");
            yield return Segundos(1f);
            Assert.AreEqual(ModoJugador.Carro, jugadorAPie.Modo, "Nadie lo baja solo.");
        }

        [UnityTest]
        public IEnumerator SinHudEnLaMunecaIzquierda_ElHudVaEnElCasco()
        {
            yield return ArmarVR();
            yield return Segundos(0.5f);
            Assert.IsNull(GameObject.Find(HudMunecaVR.NombreCanvas), "VR14: ya no hay HUD en la muñeca.");
            foreach (Canvas c in adaptador.ManoIzquierda.GetComponentsInChildren<Canvas>(true))
            {
                Assert.AreNotEqual(HudMunecaVR.NombreCanvas, c.name, "Nada de la muñeca cuelga del control izquierdo.");
            }
            Assert.IsTrue(MenusVR.Actual.Casco.Existe, "El HUD de casco existe.");
        }
    }
}

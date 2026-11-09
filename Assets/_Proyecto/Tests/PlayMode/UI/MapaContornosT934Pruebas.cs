using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Mapa;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-934: el mapa grande transparente con contornos finos, el I+D+I siempre visible y el destino marcado que sigue al cerrar.</summary>
    public class MapaContornosT934Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";

        private PantallaMapa mapa;

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            PuntoMarcadoMapa.Quitar();
            CatalogoDestinosMapa.Limpiar();
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            float fin = Time.realtimeSinceStartup + 40f;
            while (Time.realtimeSinceStartup < fin)
            {
                DirectorPandillas director = Object.FindFirstObjectByType<DirectorPandillas>();
                if (director != null && director.Iniciado && Registro<IJugador>.Actual != null)
                {
                    break;
                }
                yield return null;
            }
            Assert.IsNotNull(Registro<IJugador>.Actual, "La ciudad arrancó");
            yield return new WaitForSecondsRealtime(2.5f);
            mapa = Object.FindFirstObjectByType<PantallaMapa>();
            Assert.IsNotNull(mapa);
        }

        [UnityTearDown]
        public IEnumerator Cerrar()
        {
            if (mapa != null)
            {
                mapa.Cerrar();
            }
            PuntoMarcadoMapa.Quitar();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Mapa_SoloLineasFinas_SinImagenNiRellenos()
        {
            mapa.Abrir();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.Greater(mapa.LineasCalles.Cantidad, 20, "Se dibujan los bordes de las calles");
            Assert.Greater(mapa.LineasEdificios.Cantidad, 20, "Se dibujan los contornos de los edificios");
            Assert.AreEqual(0f, mapa.ColorVista.a, 0.001f, "La vista no tiene fondo");
            Assert.IsEmpty(mapa.Contenido.GetComponentsInChildren<RawImage>(true), "No queda la imagen de la ciudad");
            foreach (Image manzana in mapa.Manzanas)
            {
                Assert.IsFalse(manzana.enabled, "Ninguna manzana se rellena ni lleva borde de color");
            }
            if (System.Environment.GetEnvironmentVariable("CAPTURAR_UI") == "1")
            {
                CapturaUI.Capturar("docs/capturas/ui-mapa-contornos.png", Registro<IJugador>.Actual.Camara);
            }
        }

        [UnityTest]
        public IEnumerator ElDoc_SiempreEnElMapa_AunqueSuObjetoEsteApagado()
        {
            mapa.Abrir();
            yield return null;
            Assert.IsTrue(CatalogoDestinosMapa.Posicion(IdDestinoMapa.ElDoc, out Vector3 antes), "Se conoce dónde está");
            GameObject camilla = GameObject.Find("Clinica_Camilla");
            Assert.IsNotNull(camilla, "Control: la camilla está en la ciudad");
            camilla.SetActive(false);
            CatalogoDestinosMapa.Limpiar();
            yield return null;
            Assert.IsTrue(CatalogoDestinosMapa.Posicion(IdDestinoMapa.ElDoc, out Vector3 despues), "Sigue encontrando el lugar con el objeto apagado");
            Assert.Less(Vector3.Distance(antes, despues), 30f, "En el mismo sitio");
            Assert.IsNotNull(mapa.IconoDestino(IdDestinoMapa.ElDoc), "El I+D+I se dibuja en el mapa con su icono");
            camilla.SetActive(true);
        }

        [UnityTest]
        public IEnumerator DestinoMarcado_SigueAlCerrar_YLoApuntaElMarcador()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            Vector3 lejos = jugador.Raiz.position + new Vector3(60f, 0f, 40f);
            mapa.Abrir();
            yield return null;
            PuntoMarcadoMapa.Marcar(lejos);
            yield return null;
            mapa.Cerrar();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsTrue(PuntoMarcadoMapa.Hay, "El destino sigue al cerrar el mapa");
            MarcadorObjetivo marcador = Object.FindFirstObjectByType<MarcadorObjetivo>();
            Assert.IsNotNull(marcador);
            Assert.AreEqual(Vector3.Distance(jugador.Raiz.position, new Vector3(lejos.x, 0f, lejos.z)), marcador.Distancia, 3f, "El marcador en pantalla apunta al destino");
        }
    }
}
